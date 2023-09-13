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

using ThisEntity = workIT.Models.Common.HoldersProfile;
using DBEntity = workIT.Data.Tables.HoldersProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;


namespace workIT.Factories
{
	public class HoldersProfileManager : BaseFactory
	{
		static readonly string thisClassName = "HoldersProfileManager";

		#region HoldersProfile - persistance ==================
		public bool SaveList( List<ThisEntity> input, Entity parentEntity, ref SaveStatus status )
		{
			bool allIsValid = true;
			try
			{
				//need to handle deletes for parent?
				//TODO - if one existing, and one input, do an update. We do have the CTID
				//DeleteAll( parentEntity, ref status );
				//
				//using ( var context = new EntityContext() )
				//{
				//	var existing = context.Entity_HoldersProfile.Where( s => s.EntityId == parentEntity.Id ).ToList();
				//	//


				//	//var result = existing.Where( ex => input.All( p2 => p2.CTID.ToLower() != ex.HoldersProfile.CTID.ToLower() ) ).ToList();
				//	//var messages = new List<string>();
				//	//foreach (var item in result )
				//	//{
				//	//	Delete( item.Id, ref messages );
				//	//}
				//	//if ( messages.Any() )
				//	//	status.AddErrorRange( messages );
				//}
				DeleteAll( parentEntity, ref status );
				if ( input == null || !input.Any() )
					return true;

				foreach ( var item in input )
				{
					var hp = GetByCtid( item.CTID );
					item.Id = hp.Id;
					if ( !Save( item, parentEntity, ref status ) )
					{
						allIsValid = false;
					}

				}
			}catch(Exception ex)
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
						//actually will always be an add
					if ( entity.Id > 0 )
					{
						//TODO - consider if necessary, or interferes with anything
						context.Configuration.LazyLoadingEnabled = false;
						DBEntity efEntity = context.HoldersProfile
								.FirstOrDefault( s => s.Id == entity.Id );

						if ( efEntity != null && efEntity.Id > 0 )
						{
							//fill in fields that may not be in entity
							entity.RowId = efEntity.RowId;

							MapToDB( entity, efEntity );

							if ( efEntity.EntityStateId == 0 )
							{
								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "HoldersProfile",
									Activity = "Import",
									Event = "Reactivate",
									Comment = string.Format( "HoldersProfile had been marked as deleted, and was reactivted by the import. CTID: {0}, SWP: {1}", entity.CTID, entity.Source ),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd( sa );
							}
							//assume and validate, that if we get here we have a full record
							if ( efEntity.EntityStateId  != 2 )
								efEntity.EntityStateId = 3;

							//if ( IsValidDate( status.EnvelopeCreatedDate ) && status.LocalCreatedDate < efEntity.Created )
							//{
							//	efEntity.Created = status.LocalCreatedDate;
							//}
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
									string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a HoldersProfile. The process appeared to not work, but was not an exception, so we have no message, or no clue. HoldersProfile: {0}, Id: {1}", entity.Name, entity.Id );
									status.AddError( "Error - the update was not successful. " + message );
									EmailManager.NotifyAdmin( thisClassName + ".Save Failed Failed", message );
								}

							}
							else
							{
								//update entity.LastUpdated - assuming there has to have been some change in related data
								//new EntityManager().UpdateModifiedDate( entity.RowId, ref status );
							}
							if ( isValid )
							{
								if ( !UpdateParts( entity, ref status ) )
									isValid = false;

								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "HoldersProfile",
									Activity = "Import",
									Event = "Update",
									Comment = string.Format( "HoldersProfile was updated by the import. CTID: {0}, Source: {1}", entity.CTID, entity.Source ),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd( sa );

								//if ( isValid || partsUpdateIsValid )
								new EntityManager().UpdateModifiedDate( entity.RowId, ref status, efEntity.LastUpdated );
							}
						}
						else
						{
							status.AddError( "Error - update failed, as record was not found." );
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
				string message = HandleDBValidationError( dbex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ), "HoldersProfile" );
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
		/// add a HoldersProfile
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
					//the envelope date may not reflect when the earning profile was added
					//if ( IsValidDate( status.EnvelopeCreatedDate ) )
					//{
					//	efEntity.Created = status.LocalCreatedDate;
					//	efEntity.LastUpdated = status.LocalCreatedDate;
					//}
					//else
					{
						efEntity.Created = System.DateTime.Now;
						efEntity.LastUpdated = System.DateTime.Now;
					}

					context.HoldersProfile.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						//add log entry
						SiteActivity sa = new SiteActivity()
						{
							ActivityType = "HoldersProfile",
							Activity = "Import",
							Event = "Add",
							Comment = string.Format( "Full HoldersProfile was added by the import. CTID: {0}, Desc: {1}", entity.CTID, entity.Description ),
							ActivityObjectId = entity.Id
						};
						new ActivityManager().SiteActivityAdd( sa );

						new Entity_HoldersProfileManager().Add( parentEntity.EntityUid, entity.Id, ref status );
						if ( UpdateParts( entity, ref status ) == false )
						{
						}

						return efEntity.Id;
					}
					else
					{
						//?no info on error

						string message = thisClassName + string.Format( ". Add Failed", "Attempted to add a HoldersProfile. The process appeared to not work, but was not an exception, so we have no message, or no clue. HoldersProfile: {0}, ctid: {1}", entity.Name, entity.CTID );
						status.AddError( thisClassName + ". Error - the add was not successful. " + message );
						EmailManager.NotifyAdmin( "HoldersProfileManager. Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "HoldersProfile" );
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
		//public int AddPendingRecord( Guid entityUid, string ctid, string registryAtId, ref string status )
		//{
		//	DBEntity efEntity = new DBEntity();
		//	try
		//	{
		//		using ( var context = new EntityContext() )
		//		{
		//			if ( !IsValidGuid( entityUid ) )
		//			{
		//				status = thisClassName + " - A valid GUID must be provided to create a pending entity";
		//				return 0;
		//			}
		//			//quick check to ensure not existing
		//			ThisEntity entity = GetByCtid( ctid );
		//			if ( entity != null && entity.Id > 0 )
		//				return entity.Id;

		//			//only add DB required properties
		//			//NOTE - an entity will be created via trigger
		//			efEntity.Name = "Placeholder until full document is downloaded";
		//			efEntity.Description = "Placeholder until full document is downloaded";
		//			efEntity.EntityStateId = 1;
		//			efEntity.RowId = entityUid;
		//			//watch that Ctid can be  updated if not provided now!!
		//			efEntity.CTID = ctid;
		//			efEntity.Source = registryAtId;

		//			efEntity.Created = System.DateTime.Now;
		//			efEntity.LastUpdated = System.DateTime.Now;

		//			context.HoldersProfile.Add( efEntity );
		//			int count = context.SaveChanges();
		//			if ( count > 0 )
		//				return efEntity.Id;

		//			status = thisClassName + " Error - the save was not successful, but no message provided. ";
		//		}
		//	}

		//	catch ( Exception ex )
		//	{
		//		string message = FormatExceptions( ex );
		//		LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddPendingRecord. entityUid:  {0}, ctid: {1}", entityUid, ctid ) );
		//		status = thisClassName + " Error - the save was not successful. " + message;

		//	}
		//	return 0;
		//}

		public bool ValidateProfile( ThisEntity profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;
			if ( string.IsNullOrWhiteSpace( profile.Description ) )
			{
				status.AddWarning( "An HoldersProfile Description must be entered" );
			}


			return status.WasSectionValid;
		}
		/// <summary>
		/// Delete all HoldersProfiles for parent
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool DeleteAll( Entity parent, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;

			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( thisClassName + ". Error - the provided target parent entity was not provided." );
				return false;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					//check if target is a reference object and is only in use here
					var results = context.Entity_HoldersProfile
								.Where( s => s.EntityId == parent.Id )
								.OrderBy( s => s.Created )
								.ToList();
					if ( results == null || results.Count == 0 )
					{
						return true;
					}

					foreach ( var item in results )
					{
						if ( item.HoldersProfile != null && item.HoldersProfile.Id > 0 )
						{
							var messages = new List<string>();
							//this will delete the Entity_HoldersProfile as well.
							Delete( item.HoldersProfile.Id, ref messages );
							if ( messages.Any() )
								status.AddErrorRange( messages );
						}
						//context.Entity_HoldersProfile.Remove( item );
						//count = context.SaveChanges();
						//if ( count > 0 )
						//{

						//}
					}
				}
			}
			catch ( Exception ex ) 
			{
				LoggingHelper.LogError( ex, thisClassName + ".DeleteAll" );
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
				messages.Add( thisClassName + ".Delete() Error - a valid holders profile id must be provided.");
				return false;
			}

			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					DBEntity efEntity = context.HoldersProfile
								.FirstOrDefault( s => s.Id == id );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						Guid rowId = efEntity.RowId;
						//need to remove from Entity.

						//need to trigger delete of relevant dataset, timeframe, and dataprofile
						new DataSetProfileManager().DeleteAll( rowId, ref messages );						
						
						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " HoldersProfile. Id: {0}, Ctid: {1}.", efEntity.Id, efEntity.CTID );
						//21-03-31 mp - just removing the profile will not remove its entity and the latter's children!
						string statusMessage = "";
						new EntityManager().Delete( rowId, string.Format( "HoldersProfile: {0} ({1})", efEntity.Name ?? "none", efEntity.Id ), ref statusMessage );
						//
						context.HoldersProfile.Remove( efEntity );
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							new ActivityManager().SiteActivityAdd( new SiteActivity()
							{
								ActivityType = "HoldersProfile",
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
						messages.Add( thisClassName + ".Delete() Warning No action taken, as the record was not found.");
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + ".Delete()" );
					isValid = false;
					var statusMessage = FormatExceptions( ex );
					if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
					{
						statusMessage = "Error: this HoldersProfile cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this HoldersProfile can be deleted.";
					}
					messages.Add( statusMessage );
				}
			}
			return isValid;
		}

		//public bool DeleteAll( Entity parent, ref SaveStatus status )
		//{
		//	bool isValid = true;
		//	//Entity parent = EntityManager.GetEntity( parentUid );
		//	if ( parent == null || parent.Id == 0 )
		//	{
		//		status.AddError( thisClassName + ".DeleteAll Error - the provided target parent entity was not provided." );
		//		return false;
		//	}
		//	using ( var context = new EntityContext() )
		//	{
		//		context.HoldersProfile.RemoveRange( context.HoldersProfile.Where( s => s.EntityId == parent.Id ) );
		//		int count = context.SaveChanges();
		//		if ( count > 0 )
		//		{
		//			isValid = true;
		//		}
		//		else
		//		{
		//			//if doing a delete on spec, may not have been any properties
		//		}
		//	}

		//	return isValid;
		//}
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

			//JurisdictionProfile 
			Entity_JurisdictionProfileManager jpm = new Entity_JurisdictionProfileManager();
			//do deletes - NOTE: other jurisdictions are added in: UpdateAssertedIns
			jpm.DeleteAll( relatedEntity, ref status );
			if ( !jpm.SaveList( entity.Jurisdiction, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status ) )
				isAllValid = false;
			//datasetProfiles
			if (!new DataSetProfileManager().SaveList( entity.RelevantDataSet, relatedEntity, ref status ))
				isAllValid = false;

			return isAllValid;
		}

		#endregion



		#region == Retrieval =======================
		public static ThisEntity GetByCtid( string ctid )
		{
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.HoldersProfile
						.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, false );
				}
			}

			return entity;
		}

		public static ThisEntity GetBasic( int id )
		{
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.HoldersProfile
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, false );
				}
			}

			return entity;
		}
		public static ThisEntity GetBasic( Guid guid )
		{
			ThisEntity entity = new ThisEntity();
			//Guid guid = new Guid( id );
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.HoldersProfile
						.SingleOrDefault( s => s.RowId == guid );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, false );
				}
			}

			return entity;
		}

		//
		public static ThisEntity GetDetails( int id )
		{
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.HoldersProfile
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					//check for virtual deletes
					if ( item.EntityStateId == 0 )
						return entity;

					MapFromDB( item, entity, true );
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
			output.Name = GetData( input.Name );
			output.Description = GetData( input.Description );
			output.NumberAwarded = input.NumberAwarded;
			output.Source = GetUrlData( input.Source );
			if ( IsValidDate( input.DateEffective ) )
				output.DateEffective = DateTime.Parse( input.DateEffective );
			else
				output.DateEffective = null;
			//======================================================================

		}

		public static void MapFromDB( DBEntity input, ThisEntity output,
				bool includingParts )
		{

			output.Id = input.Id;
			output.RowId = input.RowId;
			output.EntityStateId = input.EntityStateId;
			output.Name = input.Name == null ? "" : input.Name;
			output.Description = input.Description == null ? "" : input.Description;
			output.CTID = input.CTID;
			if ( IsValidDate( input.DateEffective ) )
				output.DateEffective = ( ( DateTime )input.DateEffective ).ToString("yyyy-MM-dd");
			else
				output.DateEffective = "";
			//
			output.NumberAwarded = (input.NumberAwarded ?? 0);
			output.Source = GetUrlData( input.Source );

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
				//
				output.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId );
				//get datasetprofiles
				output.RelevantDataSet = DataSetProfileManager.GetAll( output.RowId, true );			
			}
		} //


		#endregion


	}
}
