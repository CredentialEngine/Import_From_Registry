using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using workIT.Models;
using workIT.Models.Common;
using ThisResource = workIT.Models.Common.ResourceSummary;
using DBResource = workIT.Data.Tables.Entity_HasResource;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;
using RelatedResourceManager = workIT.Factories.EntityManager;
using workIT.Utilities;
using CM = workIT.Models.Common;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

namespace workIT.Factories
{
    public class Entity_HasResourceManager : BaseFactory
    {
        static string thisClassName = "Entity_HasResourceManager";
        static string ResourceType = "Entity_HasResource";
        static string RelatedResourceType = "Resource";
        static string ResourceLabel = "Entity_HasResource";

        //??
        public RelatedResourceManager relatedResourceManager = new RelatedResourceManager();
        #region Entity Resource Persistance ===================
        public bool SaveList( Entity parent, int entityTypeId, List<int> list, ref SaveStatus status, int subConnectionTypeId = 0 )
        {
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + ".SaveList. Error - the provided target parent entity was not provided." );
                return false;
            }
            //any value????
            DateTime updateDate = DateTime.Now;
            if ( IsValidDate( status.EnvelopeUpdatedDate ) )
            {
                updateDate = status.LocalUpdatedDate;
            }
            //TODO - USE A REPLACE PATTERN.
            //for now do a delete all
            var currentEntityList = GetAllEntityType( parent, entityTypeId );
            bool isAllValid = true;
            if ( list == null || list.Count == 0 )
            {
                if ( currentEntityList != null && currentEntityList.Any() )
                {
                    //no input, and existing records, delete all for the type
                    DeleteAllEntityType( parent, entityTypeId, ref status );
                }
                return true;
            }
            
            //may not need this if the new list version works
            //if ( list.Count == 1 && currentEntityList.Count == 1 )
            //{
            //    //One of each, just do update of one
            //    //NO - can miss changes to targets? OR can get duplicates for alternate conditions!
            //    var existingConditionProfile = currentConditions[0];
            //    var entity = list[0];
            //    entity.Id = existingConditionProfile.Id;
            //    entity.ConnectionProfileTypeId = conditionTypeId;
            //    entity.ConditionSubTypeId = subConnectionTypeId;
            //    if ( existingConditionProfile.AlternativeCondition != null && existingConditionProfile.AlternativeCondition.Any() )
            //    {
            //        DeleteAllAlternativeConditions( existingConditionProfile.RowId, ref status );
            //    }
            //    Save( entity, parent, updateDate, ref status );
            //}
            //else
            {


                foreach ( var item in list )
                {
                    Add( parent, entityTypeId, item, 1, true, ref status );
                }
                //delete any entities with last updated less than updateDate
                //DeleteAll( parent, ref status, updateDate );
            }
            //bool isAllValid = true;
            //foreach ( ThisEntity item in list )
            //{
            //	item.ConnectionProfileTypeId = conditionTypeId;
            //	item.ConditionSubTypeId = subConnectionTypeId;
            //	Save( item, parent, ref status );
            //}

            return isAllValid;
        }

        /// <summary>
        /// Add an Resource to a parent (typically a stub was created, so can be associated before completing the full profile)
        /// </summary>
        /// <param name="parentUid"></param>
        /// <param name="resourceId">The just create lopp</param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public int Add( Entity parent, int entityTypeId,
                    int resourceId, 
                    int relationshipTypeId,
                    bool allowMultiples,
                    ref SaveStatus status,
                    bool warnOnDuplicate = false
            )
        {
            int id = 0;
            int count = 0;
            if ( resourceId == 0 )
            {
                status.AddError( $"A valid {RelatedResourceType} identifier was not provided to the {thisClassName}.Add method." );
                return 0;
            }
            if ( relationshipTypeId == 0 )
                relationshipTypeId = 1;

//            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( "Error - the parent entity was not found." );
                return 0;
            }

            using ( var context = new EntityContext() )
            {
                DBResource efEntity = new DBResource();
                try
                {
                    //first check for duplicates
                    //including relationship?
                    efEntity = context.Entity_HasResource
                            .FirstOrDefault( s => s.EntityId == parent.Id 
                                        && s.EntityTypeId == entityTypeId
                                        && s.ResourceId == resourceId 
										&& s.RelationshipTypeId == relationshipTypeId
										);

                    if ( efEntity != null && efEntity.Id > 0 )
                    {
                        if ( warnOnDuplicate )
                        {
                            status.AddWarning( $"This {RelatedResourceType} has already been added to this profile {thisClassName}." );
                        }
                        id = efEntity.Id;
                        return id;
                    }

                    efEntity = new DBResource
                    {
                        EntityId = parent.Id,
                        EntityTypeId = entityTypeId,
                        ResourceId = resourceId,
						RelationshipTypeId = relationshipTypeId,
						Created = System.DateTime.Now
                    };

                    context.Entity_HasResource.Add( efEntity );

                    // submit the change to database
                    count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        id = efEntity.Id;
                        return efEntity.Id;
                    }
                    else
                    {
                        //?no info on error

                        string message = $"{thisClassName}.Add Failed. Attempted to add a {RelatedResourceType} for a connection profile. The process appeared to not work, but there was no exception, so we have no message, or no clue. Parent Profile: {parent.Id}, {ResourceType}.Id: {resourceId}";
                        status.AddError( thisClassName + ". Error - the add was not successful. " + message );
                        EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
                    }
                }
                catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
                {
                    string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", $"{ResourceType}" );
                    status.AddError( thisClassName + ". Error - the Add was not successful. " + message );
                    LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );

                }
                catch ( Exception ex )
                {
                    string message = FormatExceptions( ex );
                    status.AddError( thisClassName + "Error - the Add was not successful. " + message );
                    LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );
                }


            }
            return id;
        }

        public bool DeleteAllEntityType( Entity parent, int entityTypeId, ref SaveStatus status )
        {
            bool isValid = true;
            int count = 0;
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + ".DeleteAllEntityType. Error - the provided target parent entity was not provided." );
                return false;
            }
            if ( entityTypeId == 0 )
            {
                status.AddError( thisClassName + $".DeleteAllEntityType. Error - the entity type Id was not provided for entity: {parent.EntityType}/{parent.EntityBaseName} ({parent.EntityBaseId})." );
                return false;
            }
            using ( var context = new EntityContext() )
            {
                //check if target is a reference object and is only in use here
                var results = context.Entity_HasResource
                            .Where( s => s.EntityId == parent.Id && s.EntityTypeId == entityTypeId )
                            .OrderBy( s => s.EntityTypeId ).ThenBy( s => s.ResourceId )
                            .ToList();
                if ( results == null || results.Count == 0 )
                {
                    return true;
                }

                foreach ( var item in results )
                {
                    context.Entity_HasResource.Remove( item );
                    count = context.SaveChanges();
                    if ( count > 0 )
                    {

                    }
                }
            }

            return isValid;
        }


        /// <summary>
        /// Delete all relationships for parent
        /// NOTE: there should be a check for reference entities, and delete if no other references.
        /// OR: have a clean up process to delete orphans. 
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
            using ( var context = new EntityContext() )
            {
                //check if target is a reference object and is only in use here
                var results = context.Entity_HasResource
                            .Where( s => s.EntityId == parent.Id )
                            .OrderBy( s => s.EntityTypeId ).ThenBy( s => s.ResourceId )
                            .ToList();
                if ( results == null || results.Count == 0 )
                {
                    return true;
                }

                foreach ( var item in results )
                {
                    //may be better to use the view?
                        //do a getall. If only one, delete it.
                        var exists = context.Entity_HasResource
                            .Where( s => s.ResourceId == item.ResourceId )
                            .ToList();
                        if ( exists != null && exists.Count() == 1 )
                        {
                            var statusMsg = "";
                            //this method will also add pending request to remove from elastic.
                            //20-12-18 mp - Only done for a reference lopp but what about a full lopp that may now be an orphan? We are not allowing lopps without parent, but will still exist in registry!!!
                            //actually this delete will probably also delete the Entity_HasResource
                            //new ResourceManager().Delete( item.ResourceId, ref statusMsg );
                            //continue;
                        }
              
                    context.Entity_HasResource.Remove( item );
                    count = context.SaveChanges();
                    if ( count > 0 )
                    {

                    }
                }
            }

            return isValid;
        }
        #endregion

        /// <summary>
        /// Can be more efficient to just get all of the related records and then split up in the caller
        /// </summary>
        /// <param name="parent">Entity related to calling source</param>
        /// <param name="relationshipTypeId">Not currently used.</param>
        /// <returns></returns>
        public static List<ThisResource> GetAll( Entity parent, int relationshipTypeId = 1 )
        {

            if ( parent == null || parent.Id == 0 )
            {
                return null;
            }
            //note even the summary should include indicator of competencies
            return GetAllEntityType( parent, 0, relationshipTypeId );
        }
        /// <summary>
        /// Get all Resources for the parent
        /// Uses the parent Guid to retrieve the related Entity, then uses the EntityId to retrieve the child objects.
        /// </summary>
        /// <param name="parentUid"></param>
        /// <param name="relationshipTypeId">Not sure if will use this</param>
        /// <returns></returns>
        public static List<ThisResource> GetAllEntityType( Entity parent, int entityTypeId, int relationshipTypeId = 1 )
        {
            List<ThisResource> list = new List<ThisResource>();
            ThisResource entity = new ThisResource();

            //Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                return list;
            }

            LoggingHelper.DoTrace( 7, $"{thisClassName}.GetAllEntityType: parent:{parent.EntityBaseName}, parentId:{parent.EntityBaseId}, e.EntityTypeId:{parent.EntityTypeId}" );
            try
            {
                using ( var context = new ViewContext() )
                {
                    var results = context.Entity_HasResourceSummary
                            .Where( s => s.EntityId == parent.Id 
                                && ( entityTypeId== 0 || s.EntityTypeId == entityTypeId ) 
                                && (relationshipTypeId == 0 || s.RelationshipTypeId == relationshipTypeId )
                                )
                            .OrderBy( s => s.Name )
                            .ToList();

                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( var item in results )
                        {
                            entity = new ThisResource()
                            {
                                Id = item.Id,
                                EntityTypeId = (int)item.EntityTypeId,
                                Name = item.Name,
                                Description = item.Description,
                                CTID = item.CTID,
                                URI = item.SubjectWebpage,
                                Type = item.EntityType,
                                //Organization = item.Organization
                            };                            
                               
                            list.Add( entity );
                        }
                    }
                    return list;
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, $"{thisClassName}.GetAllEntityType: parent:{parent.EntityBaseName}, parentId:{parent.EntityBaseId}, e.EntityTypeId:{parent.EntityTypeId}" );
            }
            return list;
        }

    }
}
