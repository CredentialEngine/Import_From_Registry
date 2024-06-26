using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;
//using workIT.Models.Helpers.Cass;
using ApiFramework = workIT.Models.API.Collection;
using DBResource = workIT.Data.Tables.Collection_Competency;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisResource = workIT.Models.ProfileModels.Competency;


namespace workIT.Factories
{
	public class CollectionCompetencyManager : BaseFactory
	{
		static string thisClassName = "CollectionCompetencyManager";
        string EntityType = "Competency";
        int EntityTypeId = CodesManager.ENTITY_TYPE_COLLECTION_COMPETENCY; 

        #region Persistance ===================

        public bool SaveList( Collection collection, List<ThisResource> list, ref SaveStatus status )
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

			foreach ( var item in list )
			{
				item.FrameworkId = collection.Id;
				Save( collection, item, updateDate, ref status );
			}

			//delete any records with last updated less than updateDate
			DeleteAll( collection.Id, ref status, updateDate );
			return isAllValid;
		}

		/// <summary>
		/// Add/Update a Collection
		/// </summary>
		/// <param name="resource"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Save( Collection collection, ThisResource resource, DateTime updateDate, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;

            DBResource efEntity = new DBResource();
			try
			{
				using ( var context = new EntityContext() )
				{
					//???? not existance checking
					if ( ValidateProfile( resource, ref status ) == false )
					{
						//return false;
					}
					//check 
					var exists = Get( collection.Id, resource.CTID );
					if ( exists != null && exists .Id > 0)
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

						context.Collection_Competency.Add( efEntity );

						count = context.SaveChanges();

						resource.Id = efEntity.Id;
						resource.RowId = efEntity.RowId;
						if ( count == 0 )
						{
							status.AddWarning( string.Format( "CollectionId: {0}. Unable to add competency: {1} <br\\> ", resource.FrameworkId, string.IsNullOrWhiteSpace( resource.CompetencyText ) ? "no description" : resource.CompetencyText ) );
						}
						else
						{
                            resource.RowId = efEntity.RowId;
                            resource.Created = ( DateTime ) efEntity.Created;
                            resource.LastUpdated = ( DateTime ) efEntity.LastUpdated;
                            resource.Id = efEntity.Id;
                            UpdateEntityCache( collection, resource, ref status );
                        }
					}
					else
					{

						efEntity = context.Collection_Competency.FirstOrDefault( s => s.Id == resource.Id );
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
								count = context.SaveChanges();
                                resource.LastUpdated = updateDate;
								resource.Created = efEntity.Created;
								UpdateEntityCache( collection, resource, ref status );
                            }
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + $".Save() collection: {collection.Name}, colCTID: {collection.CTID}, competencyCTID: {resource.CTID}" );
			}

			return isValid;
		}
        public void UpdateEntityCache( Collection parent, ThisResource resource, ref SaveStatus status )
        {
			try
			{
				EntityCache ec = new EntityCache()
				{
					EntityTypeId = EntityTypeId,
					EntityType = EntityType,
					EntityStateId = 3,
					EntityUid = resource.RowId,
					ParentEntityType = "Collection",
					ParentEntityTypeId = CodesManager.ENTITY_TYPE_COLLECTION,
					ParentEntityId = parent.RelatedEntityId,
					ParentEntityUid = parent.RowId,
					BaseId = resource.Id,
					Description = "Collection Competency",
					//a list
					//SubjectWebpage = document.SubjectWebpage,
					CTID = resource.CTID,
					Name = resource.CompetencyText,
					OwningAgentUID = parent.PrimaryAgentUID,

				};

				if ( IsValidDate( resource.Created ) )
				{
					ec.Created = ( DateTime ) resource.Created;
				}
				if ( IsValidDate( resource.LastUpdated ) )
				{
					ec.LastUpdated = ( DateTime ) resource.LastUpdated;
				}
				if ( IsValidGuid( parent.RowId ) )
				{
					ec.OwningAgentUID = parent.PrimaryAgentUID;
				}
				var statusMessage = string.Empty;
				if ( new EntityManager().EntityCacheSave( ec, ref statusMessage ) == 0 )
				{
					status.AddError( $"{thisClassName}.UpdateEntityCache() collection: {parent.Name}, colCTID: {parent.CTID}, competencyCTID: {resource.CTID}. Failed: {statusMessage}" );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + $".UpdateEntityCache() collection: {parent.Name}, colCTID: {parent.CTID}, competencyCTID: {resource.CTID}" );
			}
		}
        public bool DeleteAll( int CollectionId, ref SaveStatus status, DateTime? lastUpdated = null )
		{
			bool isValid = true;
			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( CollectionId == 0 )
			{
				status.AddError( thisClassName + ". Error - the provided target parent CollectionId was not provided." );
				return false;
			}
			int expectedDeleteCount = 0;
			try
			{
				using ( var context = new EntityContext() )
				{

					var results = context.Collection_Competency.Where( s => s.CollectionId == CollectionId && ( lastUpdated == null || s.LastUpdated < lastUpdated ) )
				.ToList();
					if ( results == null || results.Count == 0 )
						return true;
					expectedDeleteCount = results.Count;

					foreach ( var item in results )
					{
						context.Collection_Competency.Remove( item );
						var count = context.SaveChanges();
						if ( count > 0 )
						{

						}
					}
				}
			}
			catch ( Exception ex )
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. CollectionId: {0},  exception: {1}", CollectionId, msg ) );
			}
			return isValid;
		}
		//

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
        public static List<ThisResource> GetAll( int collectionId )
        {
            var output = new List<ThisResource>();
            ThisResource entity = new ThisResource();

            try
            {
                using ( var context = new EntityContext() )
                {
                    //
                    var list = context.Collection_Competency
                            .Where( s => s.CollectionId == collectionId ).ToList();
                    foreach ( var item in list )
                    {
                        entity = new ThisResource();
                        if ( item != null && item.Id > 0 )
                        {
                            MapFromDB( item, entity );
                            output.Add( entity );
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".GetAll( int collectionId)" );
            }
            return output;
        }//
        public static CodeItem GetCompetencyTypeTotal( int collectionId )
        {
            var codeItem = new CodeItem();

            try
            {
                using ( var context = new EntityContext() )
                {
                    //make sure indexed
                    var query = from cm in context.Collection_Competency
                                where ( cm.CollectionId == collectionId )
                                group new { cm } by new { cm.CollectionId }
                                    into cmgrp
                                select new { CollectionId = cmgrp.Key.CollectionId, EntityTypeId = CodesManager.ENTITY_TYPE_COLLECTION_COMPETENCY, EntityType = "Competency", Count = cmgrp.Count() };

                    var result = query.OrderBy( m => m.CollectionId ).ThenBy( m => m.EntityType ).ToList();

                    if ( result != null && result.Count() > 0 )
                    {
                        foreach ( var item in result )
                        {
                            codeItem = new CodeItem()
                            {
                                EntityType = item.EntityType,
                                EntityTypeId = item.EntityTypeId,
                                Totals = item.Count
                            };
							break;
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".GetCompetencyTypeTotal( int collectionId)" );
            }
            return codeItem;
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
                    DBResource item = context.Collection_Competency
							.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

					if ( item != null && item.Id > 0 )
					{
						MapFromDB( item, entity );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetByCtid" );
			}
			return entity;
		}//

		/// <summary>
		/// Retrieve by collectionId and ProxyFor
		/// **assuming for now there can not be duplicates**
		/// </summary>
		/// <param name="collectionId"></param>
		/// <param name="ctid"></param>
		/// <returns></returns>
		public static ThisResource Get( int collectionId, string ctid )
		{
			ThisResource entity = new ThisResource();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
                    //
                    DBResource item = context.Collection_Competency
							.FirstOrDefault( s => s.CollectionId == collectionId && s.CTID.ToLower() == ctid.ToLower() );

					if ( item != null && item.Id > 0 )
					{
						MapFromDB( item, entity );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetByCtid" );
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

			output.CollectionId = input.FrameworkId;

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

		public static void MapFromDB(DBResource input, ThisResource output )
		{
			output.Id = input.Id;
			output.RowId = input.RowId;
			output.CTID = input.CTID;
			output.FrameworkId = input.CollectionId;

			output.CompetencyText = input.CompetencyText;
			output.CompetencyLabel = input.CompetencyLabel;
			output.CompetencyCategory = input.CompetencyCategory;
			output.CtdlId = input.CredentialRegistryURI ?? string.Empty;

			if ( !string.IsNullOrEmpty( output.CompetencyDetailJson ) )
			{
				//details
				output.CompetencyDetail = JsonConvert.DeserializeObject<CompetencyDetail>( output.CompetencyDetailJson );
			}

			if ( input.Created != null )
				output.Created = ( DateTime ) input.Created;
			if ( input.LastUpdated != null )
				output.LastUpdated = ( DateTime ) input.LastUpdated;
		}

		#endregion


	}
}
