using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;

using ThisEntity = workIT.Models.QData.DataSetProfile;
using DBEntity = workIT.Data.Tables.DataSetProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;


namespace workIT.Factories
{
	public class DataSetProfileManager : BaseFactory
	{
		static readonly string thisClassName = "DataSetProfileManager";

		#region DataSetProfile - persistance ==================

		public bool SaveList( List<ThisEntity> input, Entity parentEntity, ref SaveStatus status )
		{
			bool allIsValid = true;

			try { 
			//need to handle deletes for parent?
			//DeleteAll( parentEntity, ref status );
			using ( var context = new EntityContext() )
			{
				var existing = context.Entity_DataSetProfile.Where( s => s.EntityId == parentEntity.Id ).ToList();
				//
				var result = existing.Where( ex => input.All( p2 => p2.CTID != ex.DataSetProfile.CTID ) );
				var messages = new List<string>();
				foreach ( var item in result )
				{
					Delete( item.Id, ref messages );
				}
				if ( messages.Any() )
					status.AddErrorRange( messages );
			}
			if ( input == null || !input.Any() )
				return true;

			foreach ( var item in input )
			{
				var e = GetByCtid( item.CTID );
				item.Id = e.Id;
				if ( !Save( item, parentEntity, ref status ) )
				{
					allIsValid = false;
					
				}
			}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".SaveList()" );
			}
			return allIsValid;
		}
		public bool Save( ThisEntity entity, Entity parentEntity, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;
			try
			{
				using ( var context = new EntityContext() )
				{
					if ( ValidateProfile( entity, ref status ) == false )
					{
						//return false;
					}

					if ( entity.Id > 0 )
					{
						//TODO - consider if necessary, or interferes with anything
						context.Configuration.LazyLoadingEnabled = false;
						DBEntity efEntity = context.DataSetProfile
								.SingleOrDefault( s => s.Id == entity.Id );

						if ( efEntity != null && efEntity.Id > 0 )
						{
							//fill in fields that may not be in entity
							entity.RowId = efEntity.RowId;

							MapToDB( entity, efEntity );

							if ( efEntity.EntityStateId == 0 )
							{
								//var url = string.Format( UtilityManager.GetAppKeyValue( "credentialFinderSite" ) + "DataSetProfile/{0}", efEntity.Id );
								//notify, and???
								//EmailManager.NotifyAdmin( "Previously Deleted DataSetProfile has been reactivated", string.Format( "<a href='{2}'>DataSetProfile: {0} ({1})</a> was deleted and has now been reactivated.", efEntity.Name, efEntity.Id, url ) );
								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "DataSetProfile",
									Activity = "Import",
									Event = "Reactivate",
									Comment = string.Format( "DataSetProfile had been marked as deleted, and was reactivted by the import. CTID: {0}, SWP: {1}", entity.CTID, entity.Source ),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd( sa );
							}
							//assume and validate, that if we get here we have a full record
							if ( efEntity.EntityStateId != 2 )
								efEntity.EntityStateId = 3;

							if ( IsValidDate( status.EnvelopeCreatedDate ) && status.LocalCreatedDate < efEntity.Created )
							{
								efEntity.Created = status.LocalCreatedDate;
							}
							//will always use the envelop last updated?
							if ( IsValidDate( status.EnvelopeUpdatedDate ) && status.LocalUpdatedDate != efEntity.LastUpdated )
							{
								efEntity.LastUpdated = status.LocalUpdatedDate;
							}
							//has changed?
							if ( HasStateChanged( context ) )
							{
								if ( IsValidDate( status.EnvelopeUpdatedDate ) )
									efEntity.LastUpdated = status.LocalUpdatedDate;
								else
									efEntity.LastUpdated = DateTime.Now;
								//NOTE efEntity.EntityStateId is set to 0 in delete method )
								count = context.SaveChanges();
								//can be zero if no data changed
								if ( count >= 0 )
								{
									isValid = true;
								}
								else
								{
									//?no info on error

									isValid = false;
									string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a DataSetProfile. The process appeared to not work, but was not an exception, so we have no message, or no clue. DataSetProfile: {0}, Id: {1}", entity.Name, entity.Id );
									status.AddError( "Error - the update was not successful. " + message );
									EmailManager.NotifyAdmin( thisClassName + ".Save Failed Failed", message );
								}

							}
							
							if ( isValid )
							{
								if ( !UpdateParts( entity, ref status ) )
									isValid = false;

								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "DataSetProfile",
									Activity = "Import",
									Event = "Update",
									Comment = string.Format( "DataSetProfile was updated by the import. CTID: {0}, Source: {1}", entity.CTID, entity.Source ),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd( sa );

								//if ( isValid || partsUpdateIsValid )
								//new EntityManager().UpdateModifiedDate( entity.RowId, ref status, efEntity.LastUpdated );
							}
						}
						else
						{
							status.AddError( "Error - update failed, as DataSetProfile was not found." );
						}
					}
					else
					{
						//add
						int newId = Add( entity, parentEntity, ref status );
						if ( newId == 0 || status.HasErrors )
							isValid = false;
					}
				}
			}
			catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
			{
				string message = HandleDBValidationError( dbex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ), "DataSetProfile" );
				status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ) );
				status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
				isValid = false;
			}


			return isValid;
		}

		/// <summary>
		/// add a DataSetProfile
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		private int Add( ThisEntity entity, Entity parentEntity, ref SaveStatus status )
		{
			DBEntity efEntity = new DBEntity();
			using ( var context = new EntityContext() )
			{
				try
				{
					MapToDB( entity, efEntity );

					if ( IsValidGuid( entity.RowId ) )
						efEntity.RowId = entity.RowId;
					else
						efEntity.RowId = Guid.NewGuid();
					efEntity.EntityStateId = 3;
					if ( IsValidDate( status.EnvelopeCreatedDate ) )
					{
						efEntity.Created = status.LocalCreatedDate;
						efEntity.LastUpdated = status.LocalCreatedDate;
					}
					else
					{
						efEntity.Created = System.DateTime.Now;
						efEntity.LastUpdated = System.DateTime.Now;
					}

					context.DataSetProfile.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						//add log entry
						SiteActivity sa = new SiteActivity()
						{
							ActivityType = "DataSetProfile",
							Activity = "Import",
							Event = "Add",
							Comment = string.Format( "Full DataSetProfile was added by the import. CTID: {0}, Source: {1}", entity.CTID, entity.Source ),
							ActivityObjectId = entity.Id
						};
						new ActivityManager().SiteActivityAdd( sa );
						//
						new Entity_DataSetProfileManager().Add( parentEntity.EntityUid, entity.Id, ref status );
						//
						if ( UpdateParts( entity, ref status ) == false )
						{
						}

						return efEntity.Id;
					}
					else
					{
						//?no info on error

						string message = thisClassName + string.Format( ". Add Failed", "Attempted to add a DataSetProfile. The process appeared to not work, but was not an exception, so we have no message, or no clue. DataSetProfile: {0}, ctid: {1}", entity.Name, entity.CTID );
						status.AddError( thisClassName + ". Error - the add was not successful. " + message );
						EmailManager.NotifyAdmin( "DataSetProfileManager. Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "DataSetProfile" );
					status.AddError( thisClassName + ".Add(). Error - the save was not successful. " + message );

					LoggingHelper.LogError( message, true );
				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), CTID: {0}\r\n", efEntity.CTID ) );
					status.AddError( thisClassName + ".Add(). Error - the save was not successful. \r\n" + message );
				}
			}

			return efEntity.Id;
		}


		/// <summary>
		/// Parts:
		/// - Jurisdiction
		/// - DataSetProfile
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool UpdateParts( ThisEntity entity, ref SaveStatus status )
		{
			bool isAllValid = true;
			Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( "Error - the related Entity was not found." );
				return false;
			}

			//ProcessProfile
			Entity_ProcessProfileManager ppm = new Factories.Entity_ProcessProfileManager();
			ppm.DeleteAll( relatedEntity, ref status );
			try
			{
				ppm.SaveList( entity.AdministrationProcess, Entity_ProcessProfileManager.ADMIN_PROCESS_TYPE, entity.RowId, ref status );
			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddProfiles() - ProcessProfiles. id: {0}", entity.Id ) );
				status.AddWarning( thisClassName + ".AddProfiles(). Exceptions encountered handling ProcessProfiles. " + message );
			}
			//
			Entity_ReferenceFrameworkManager erfm = new Entity_ReferenceFrameworkManager();
			erfm.DeleteAll( relatedEntity, ref status );
			if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_CIP, entity.InstructionalProgramTypes, ref status ) == false )
				isAllValid = false;

			//JurisdictionProfile 
			Entity_JurisdictionProfileManager jpm = new Entity_JurisdictionProfileManager();
			//do deletes - NOTE: other jurisdictions are added in: UpdateAssertedIns
			jpm.DeleteAll( relatedEntity, ref status );
			jpm.SaveList( entity.Jurisdiction, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status );
			//datasetProfiles
			new DataSetTimeFrameManager().SaveList( entity.DataSetTimePeriod, entity.Id, ref status );


			return isAllValid;
		}

		public bool ValidateProfile( ThisEntity profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;
			if ( string.IsNullOrWhiteSpace( profile.Description ) )
			{
				status.AddWarning( "An DataSetProfile Description must be entered" );
			}


			return status.WasSectionValid;
		}
		/// <summary>
		/// Delete all profiles for parent
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool DeleteAll( Guid parentUid, ref List<string> messages )
		{
			bool isValid = true;
			int count = 0;
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				messages.Add( thisClassName + ".DeleteAll Error - the provided target parent entity was not provided." );
				return false;
			}

			try
			{
				using ( var context = new EntityContext() )
				{
					//check if target is a reference object and is only in use here
					var results = context.Entity_DataSetProfile
								.Where( s => s.EntityId == parent.Id )
								.OrderBy( s => s.Created )
								.ToList();
					if ( results == null || results.Count == 0 )
					{
						return true;
					}

					foreach ( var item in results )
					{
						if ( item.DataSetProfile != null && item.DataSetProfile.Id > 0 )
						{
							//this will delete the Entity_DataSetProfile as well.
							Delete( item.DataSetProfile.Id, ref messages );
						}
						context.Entity_DataSetProfile.Remove( item );
						count = context.SaveChanges();
						if ( count > 0 )
						{

						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".DeleteAll" );
				messages.Add( ex.Message);
			}
			return isValid;
		}
		/// <summary>
		/// Delete profile 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int id, ref List<string> messages )
		{
			bool isValid = true;
			if ( id < 1 )
			{
				messages.Add( thisClassName + ".Delete() Error - a valid dataSet profile id must be provided.");
				return false;
			}

			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					DBEntity efEntity = context.DataSetProfile
								.FirstOrDefault( s => s.Id == id );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						Guid rowId = efEntity.RowId;
						//need to remove from Entity.

						//need to remove timeframe 
						new DataSetTimeFrameManager().DeleteAll( efEntity.Id, ref messages );

						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " DataSetProfile. Id: {0}, Ctid: {1}.", efEntity.Id, efEntity.CTID );
						//
						context.DataSetProfile.Remove( efEntity );
						//efEntity.EntityStateId = 0;
						//efEntity.LastUpdated = System.DateTime.Now;
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							new ActivityManager().SiteActivityAdd( new SiteActivity()
							{
								ActivityType = "DataSetProfile",
								Activity = "Import",
								Event = "Delete",
								Comment = msg,
								ActivityObjectId = efEntity.Id
							} );
							isValid = true;
						}
					}
					else
					{
						messages.Add (thisClassName + ".Delete() Warning No action taken, as the record was not found.");
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + ".Delete(id)" );
					isValid = false;
					var statusMessage = FormatExceptions( ex );
					if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
					{
						statusMessage = "Error: this DataSetProfile cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this DataSetProfile can be deleted.";
					}
					messages.Add( statusMessage );
				}
			}
			return isValid;
		}

		#endregion



		#region == Retrieval =======================
		public static List<ThisEntity> GetAll( Guid parentUid, bool includingParts = true )
		{
			var list = new List<ThisEntity>();
			var entity = new ThisEntity();

			Entity parent = EntityManager.GetEntity( parentUid );
			LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );

			try
			{
				using ( var context = new EntityContext() )
				{
					var results = context.Entity_DataSetProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( var item in results )
						{
							entity = new ThisEntity();

							//need to distinguish between on a detail page for conditions and Holders detail
							//would usually only want basics here??
							//17-05-26 mp- change to MapFromDB_Basic
							if ( item.DataSetProfile != null && item.DataSetProfile.EntityStateId > 1 )
							{
								MapFromDB( item.DataSetProfile, entity, includingParts );
								list.Add( entity );
							}
						}
					}
					return list;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAll" );
			}
			return list;
		}

		public static ThisEntity GetByCtid( string ctid )
		{
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.DataSetProfile
						.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, false );
				}
			}

			return entity;
		}

		public static void MapToDB( ThisEntity input, DBEntity output )
		{

			//want output ensure fields input create are not wiped
			if ( output.Id == 0 )
			{
				output.CTID = input.CTID;
			}
			//if ( !string.IsNullOrWhiteSpace( input.CredentialRegistryId ) )
			//	output.CredentialRegistryId = input.CredentialRegistryId;

			output.Id = input.Id;
			//output.EntityStateId = input.EntityStateId;
			output.Description = GetData( input.Description );
			output.Name = GetData( input.Name );
			output.Source = GetUrlData( input.Source );
			output.DataSuppressionPolicy = input.DataSuppressionPolicy;
			output.SubjectIdentification = input.SubjectIdentification;
			if ( input.DataProviderUID == null || input.DataProviderUID.ToString() == DEFAULT_GUID )
			{
				output.DataProviderUID = null;//			
			}
			else
			{
				output.DataProviderUID = input.DataProviderUID;
			}
			//
			if ( input.DistributionFile != null && input.DistributionFile.Any() )
			{
				output.DistributionFile = string.Join( "|", input.DistributionFile );
			}
			else
				output.DistributionFile = null;

		}

		public static void MapFromDB( DBEntity input, ThisEntity output,
				bool includingParts )
		{

			output.Id = input.Id;
			output.RowId = input.RowId;
			output.EntityStateId = input.EntityStateId;
			output.Description = input.Description == null ? "" : input.Description;
			output.CTID = input.CTID;
			output.DataSuppressionPolicy = input.DataSuppressionPolicy;
			output.SubjectIdentification = input.SubjectIdentification;
			output.Source = GetUrlData( input.Source );
			//
			if ( IsGuidValid( input.DataProviderUID ) )
			{
				output.DataProviderUID = ( Guid )input.DataProviderUID;
				output.DataProvider = OrganizationManager.GetForSummary( output.DataProviderUID );
			}
			//
			output.InstructionalProgramType = Reference_FrameworksManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_CIP );
			//
			if ( input.DistributionFile != null )
			{
				var list = input.DistributionFile.Split( '|' );
				foreach ( var item in list )
				{
					if (!string.IsNullOrWhiteSpace(item))
						output.DistributionFile.Add( item );
				}
			}
			else
				output.DistributionFile = new List<string>();

			//
			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime )input.Created;
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime )input.LastUpdated;


			if ( string.IsNullOrWhiteSpace( output.CTID ) || output.EntityStateId < 3 )
			{
				output.IsReferenceVersion = true;
				return;
			}
			//=====
			var relatedEntity = EntityManager.GetEntity( output.RowId, false );
			if ( relatedEntity != null && relatedEntity.Id > 0 )
				output.EntityLastUpdated = relatedEntity.LastUpdated;

			//components
			if ( includingParts )
			{
				var processProfiles = Entity_ProcessProfileManager.GetAll( input.RowId );
				foreach ( ProcessProfile item in processProfiles )
				{
					if ( item.ProcessTypeId == Entity_ProcessProfileManager.ADMIN_PROCESS_TYPE )
						output.AdministrationProcess.Add( item );
				}
				//
				output.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId );
				//get DataSetTimePeriod
				output.DataSetTimePeriod = DataSetTimeFrameManager.GetAll( output.Id );
				
			}
		} //


		#endregion


	}
}
