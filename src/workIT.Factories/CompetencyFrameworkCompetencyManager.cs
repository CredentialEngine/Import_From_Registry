using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using workIT.Models;
using workIT.Models.Common;
using MPM=workIT.Models.ProfileModels;
using workIT.Utilities;
//using workIT.Models.Helpers.Cass;
using ApiFramework = workIT.Models.API.CompetencyFramework;
using DBResource = workIT.Data.Tables.CompetencyFramework_Competency;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisResource = workIT.Models.ProfileModels.Competency;


namespace workIT.Factories
{
	public class CompetencyFrameworkCompetencyManager : BaseFactory
	{
		static string thisClassName = "CompetencyFrameworkCompetencyManager";
        string EntityType = "Competency";
        int EntityTypeId = CodesManager.ENTITY_TYPE_COMPETENCY;
        #region Persistance ===================

        public bool SaveList( MPM.CompetencyFramework competencyFramework, List<ThisResource> list, ref SaveStatus status )
		{
			//will need to do a delete all or take the approach for entity address:
			//- read
			//- look up by CTID
			//- update last updated
			//- at end delete records with an older last updated date
			DateTime updateDate = DateTime.Now;
			if ( IsValidDate( status.EnvelopeUpdatedDate ) )
			{
				//an individual competency can have a modified date, so???
				updateDate = status.LocalUpdatedDate;
			}
			bool isAllValid = true;

			foreach (var entity in list)
			{
				entity.FrameworkId = competencyFramework.Id;
				Save( competencyFramework, entity, updateDate, ref status );
			}

			//delete any records with last updated less than updateDate
			//if no change, then?
			DeleteAll( competencyFramework.Id, ref status, updateDate );
			return isAllValid;
		}

		/// <summary>
		/// Add/Update a CompetencyFramework
		/// </summary>
		/// <param name="resource"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Save( MPM.CompetencyFramework competencyFramework, ThisResource resource, DateTime updateDate, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;

			DBResource efEntity = new DBResource();
			try
			{
				using ( var context = new EntityContext() )
				{

					if ( ValidateProfile( resource, ref status ) == false )
					{
						//return false;
					}
					//check - what if no CTID?
					var exists = Get( competencyFramework.Id, resource.CTID );
					if ( exists != null && exists.Id > 0 )
						resource.Id = exists.Id;

					if ( resource.Id == 0 )
					{
						//add
						efEntity = new DBResource();
						MapToDB( resource, efEntity );

						if ( IsValidDate( resource.Created ) )
							efEntity.Created = ( DateTime ) resource.Created;  
						else if ( IsValidDate( status.EnvelopeCreatedDate ) )
							efEntity.Created = status.LocalCreatedDate;
						else
							efEntity.Created = DateTime.Now;
						//this is not the competency last updated
						efEntity.LastUpdated = updateDate;
						//future handling of last
						//if ( IsValidDate( entity.DateModified ) )
						//	efEntity.DateModified = (DateTime) entity.DateModified;

						if ( IsValidGuid( resource.RowId ) )
							efEntity.RowId = resource.RowId;
						else
							efEntity.RowId = Guid.NewGuid();

						context.CompetencyFramework_Competency.Add( efEntity );

						count = context.SaveChanges();

						resource.Id = efEntity.Id;
						resource.RowId = efEntity.RowId;
						if ( count == 0 )
						{
							status.AddWarning( string.Format( "CompetencyFrameworkId: {0}. Unable to add competency: {1} <br\\> ", resource.FrameworkId, string.IsNullOrWhiteSpace( resource.CompetencyText ) ? "no description" : resource.CompetencyText ) );
						}
						else
						{
                            resource.RowId = efEntity.RowId;
                            resource.Created = ( DateTime ) efEntity.Created;
                            resource.LastUpdated = ( DateTime ) efEntity.LastUpdated;
                            resource.Id = efEntity.Id;
                            UpdateEntityCache( competencyFramework, resource, ref status );
                        }
					}
					else
					{

						efEntity = context.CompetencyFramework_Competency.FirstOrDefault( s => s.Id == resource.Id );
						if ( efEntity != null && efEntity.Id > 0 )
						{
							resource.RowId = efEntity.RowId;
							//update
							MapToDB( resource, efEntity );

							//need to do the date check here, or may not be updated
							if ( IsValidDate( resource.Created ) )
								efEntity.Created = ( DateTime ) resource.Created;

							//if ( IsValidDate( status.EnvelopeCreatedDate ) && status.LocalCreatedDate < efEntity.Created )
							//{
							//	efEntity.Created = status.LocalCreatedDate;
							//}
							//future handling of last
							//if ( IsValidDate( entity.DateModified ) )
							//	efEntity.DateModified = (DateTime) entity.DateModified;

							efEntity.LastUpdated = updateDate;
							//has changed?
							if ( HasStateChanged( context ) )
							{
								//if ( IsValidDate( status.EnvelopeUpdatedDate ) )
								//	efEntity.LastUpdated = status.LocalUpdatedDate;
								//else
								//	efEntity.LastUpdated = DateTime.Now;

								count = context.SaveChanges();
                                resource.LastUpdated = updateDate;
								resource.Created = efEntity.Created;
                                UpdateEntityCache( competencyFramework, resource, ref status );
                            }
						} else
						{
							//what
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + $".Save() competencyFramework: {competencyFramework.Name}, cfCTID: {competencyFramework.CTID}, competencyCTID: {resource.CTID}" );
			}

			return isValid;
		}

        public void UpdateEntityCache( MPM.CompetencyFramework parent, ThisResource resource, ref SaveStatus status )
        {
			try
			{
				EntityCache ec = new EntityCache()
				{
					EntityTypeId = EntityTypeId,
					EntityType = EntityType,
					EntityStateId = 3,
					EntityUid = resource.RowId,
					ParentEntityType = "CompetencyFramework",
					ParentEntityTypeId = CodesManager.ENTITY_TYPE_COMPETENCY_FRAMEWORK,
					ParentEntityId = parent.RelatedEntityId,
					ParentEntityUid = parent.RowId,

					BaseId = resource.Id,
					Description = "Framework Competency",
					//a list
					//SubjectWebpage = document.SubjectWebpage,
					CTID = resource.CTID,
					//Created = ( DateTime ) document.Created,
					//LastUpdated = ( DateTime ) document.LastUpdated,
					Name = resource.CompetencyText,
					

				};
				if (IsValidDate( resource.Created ) )
				{
					ec.Created = ( DateTime ) resource.Created;
				}
				if ( IsValidDate( resource.LastUpdated ) )
				{
					ec.LastUpdated = ( DateTime ) resource.LastUpdated;
				}
				if (IsValidGuid(parent.RowId))
				{
					ec.OwningAgentUID = parent.PrimaryAgentUID;
				}

				var statusMessage = string.Empty;
				if ( new EntityManager().EntityCacheSave( ec, ref statusMessage ) == 0 )
				{
					status.AddError( $"{thisClassName}.UpdateEntityCache() competencyFramework: {parent.Name}, cfCTID: {parent.CTID}, competencyCTID: {resource.CTID}. Failed: {statusMessage}" );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, $"{thisClassName}.UpdateEntityCache() competencyFramework: {parent.Name}, cfCTID: {parent.CTID}, competencyCTID: {resource.CTID}" );
			}
		}
        public bool DeleteAll( int competencyFrameworkId, ref SaveStatus status, DateTime? lastUpdated = null )
		{
			bool isValid = true;
			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( competencyFrameworkId == 0 )
			{
				status.AddError( thisClassName + ". Error - the provided target parent competencyFrameworkId was not provided." );
				return false;
			}
			int expectedDeleteCount = 0;
			try
			{
				using ( var context = new EntityContext() )
				{

					var results = context.CompetencyFramework_Competency.Where( s => s.CompetencyFrameworkId == competencyFrameworkId && ( lastUpdated == null || s.LastUpdated < lastUpdated ) )
				.ToList();
					if ( results == null || results.Count == 0 )
						return true;
					expectedDeleteCount = results.Count;

					foreach ( var item in results )
					{
						context.CompetencyFramework_Competency.Remove( item );
						var count = context.SaveChanges();
						if ( count > 0 )
						{

						}
					}
				}
			}
			//catch ( System.Data.Entity.Infrastructure.DbUpdateConcurrencyException dbcex )
			//{
			//	if ( dbcex.Message.IndexOf( "an unexpected number of rows (0)" ) > 0 )
			//	{
			//		//don't know why this happens, quashing for now.
			//		LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. Parent type: {0}, ParentId: {1}, expectedDeletes: {2}. Message: {3}", competencyFrameworkId, expectedDeleteCount, dbcex.Message ) );
			//	}
			//	else
			//	{
			//		var msg = BaseFactory.FormatExceptions( dbcex );
			//		LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. ParentType: {0}, baseId: {1}, DbUpdateConcurrencyException: {2}", parent.EntityType, parent.EntityBaseId, msg ) );
			//	}

			//}
			catch ( Exception ex )
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. competencyFrameworkId: {0},  exception: {1}", competencyFrameworkId, msg ) );
			}
			return isValid;
		}
		//
		/// <summary>
		/// Delete 
		/// Will need to be careful with this type of delete
		/// Alternately, could take approach to update by ctid, set last updated, then delete anything older
		/// </summary>
		/// <param name="competencyFrameworkId"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool DeleteAll( int competencyFrameworkId, ref string statusMessage )
		{
			bool isOK = true;
			try
			{
				using ( var context = new EntityContext() )
				{
					var list = context.CompetencyFramework_Competency.Where( s => s.CompetencyFrameworkId == competencyFrameworkId ).ToList();
					if ( list != null && list.Any() )
					{
						context.CompetencyFramework_Competency.RemoveRange( list );
						int count = context.SaveChanges();
					}
					else
					{
						statusMessage = string.Format( "No competencies were not found for frameworkId: {0}", competencyFrameworkId );
						isOK = false;
					}
				}
			} catch (Exception ex)
			{
				LoggingHelper.LogError( ex, thisClassName + $".DeleteAll(). competencyFrameworkId: {competencyFrameworkId}" );

			}
			return isOK;

		}


		public bool ValidateProfile( ThisResource profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;

			if ( string.IsNullOrWhiteSpace( profile.CompetencyText ) )
			{
				status.AddError( "A CompetencyText name must be entered for competency: " + profile.CtdlId );
			}


			return status.WasSectionValid;
		}

		#endregion
		#region  retrieval ==================

		//should only be in one framework
		public static ThisResource Get( int frameworkId, string ctid )
		{
			ThisResource entity = new ThisResource();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					//
					var item = context.CompetencyFramework_Competency
							.FirstOrDefault( s => s.CompetencyFrameworkId == frameworkId && s.CTID.ToLower() == ctid.ToLower() );

					if ( item != null && item.Id > 0 )
					{
						MapFromDB( item, entity );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + $".GetByCtid: {ctid} " );
			}
			return entity;
		}//
		public static ThisResource GetByCtid( string ctid )
		{
			ThisResource entity = new ThisResource();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					//lookup by frameworkUri, or SourceUrl
					DBResource item = context.CompetencyFramework_Competency
							.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

					if ( item != null && item.Id > 0 )
					{
						MapFromDB( item, entity );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + $".GetByCtid: {ctid}" );
			}
			return entity;
		}//
		public static ThisResource Get( int profileId )
		{
			ThisResource entity = new ThisResource();
			if ( profileId == 0 )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					DBResource item = context.CompetencyFramework_Competency
							.SingleOrDefault( s => s.Id == profileId );

					if ( item != null && item.Id > 0 )
					{
						MapFromDB( item, entity );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Get" );
			}
			return entity;
		}//
		public static void MapToDB( ThisResource input, DBResource output )
		{
			//want to ensure fields from create are not wiped
			if ( output.Id == 0 )
			{
				output.CTID = input.CTID;
			}
			output.Id = input.Id;

			output.CompetencyFrameworkId = input.FrameworkId;
			
			output.CompetencyText = input.CompetencyText;
			output.CompetencyLabel = input.CompetencyLabel;
			output.CompetencyCategory = input.CompetencyCategory;
			output.CredentialRegistryURI = input.CtdlId ?? string.Empty;

			//this will have been serialized in the import step
			if ( !string.IsNullOrWhiteSpace( input.CompetencyDetailJson ) )
				output.CompetencyDetailJson = input.CompetencyDetailJson;
			else
			{
				//ensure we don't reset the store
				output.CompetencyDetailJson = null;
			}
		} //

		public static void MapFromDB( DBResource input, ThisResource output )
		{
			output.Id = input.Id;
			output.RowId = input.RowId;
			output.CTID = input.CTID;
			output.FrameworkId = input.CompetencyFrameworkId;

			output.CompetencyText = input.CompetencyText;
			output.CompetencyLabel = input.CompetencyLabel;
			output.CompetencyCategory = input.CompetencyCategory;
			output.CtdlId = input.CredentialRegistryURI ?? string.Empty;			

			if ( !string.IsNullOrEmpty( output.CompetencyDetailJson ) )
			{
				//details
				output.CompetencyDetail = JsonConvert.DeserializeObject<MPM.CompetencyDetail>( output.CompetencyDetailJson );
			}

			if ( input.Created != null )
				output.Created = ( DateTime )input.Created;
			if ( input.LastUpdated != null )
				output.LastUpdated = ( DateTime )input.LastUpdated;
		}

		#endregion


	}
}
