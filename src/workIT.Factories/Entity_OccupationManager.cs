using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using workIT.Models;
using workIT.Models.Common;
using ThisResource = workIT.Models.Common.OccupationProfile;
using DBResource = workIT.Data.Tables.Entity_Occupation;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;
using RelatedResourceManager = workIT.Factories.OccupationManager;
using workIT.Utilities;
using CM = workIT.Models.Common;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

namespace workIT.Factories
{
    public class Entity_OccupationManager : BaseFactory
    {
        static string thisClassName = "Entity_OccupationManager";
        static string ResourceType = "Entity_Occupation";
        static string RelatedResourceType = "Occupation";
        static string ResourceLabel = "Occupation";
        public RelatedResourceManager relatedResourceManager = new RelatedResourceManager();
        #region Entity Occupation Persistance ===================

        /// <summary>
        /// Add an occupation to a parent (typically a stub was created, so can be associated before completing the full profile)
        /// </summary>
        /// <param name="parentUid"></param>
        /// <param name="resourceId">The just create lopp</param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public int Add( Guid parentUid,
                    int resourceId, int relationshipTypeId,
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

            Entity parent = EntityManager.GetEntity( parentUid );
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
                    efEntity = context.Entity_Occupation
                            .FirstOrDefault( s => s.EntityId == parent.Id && s.OccupationId == resourceId && s.RelationshipTypeId == relationshipTypeId );

                    if ( efEntity != null && efEntity.Id > 0 )
                    {
                        if ( warnOnDuplicate )
                        {
                            status.AddWarning( $"This {RelatedResourceType} has already been added to this profile {thisClassName}." );
                        }
                        id = efEntity.Id;
                        return id;
                    }

                    efEntity = new DBResource();
                    efEntity.EntityId = parent.Id;
                    efEntity.OccupationId = resourceId;
                    efEntity.RelationshipTypeId = relationshipTypeId > 0 ? relationshipTypeId : 1;
                    efEntity.Created = System.DateTime.Now;

                    context.Entity_Occupation.Add( efEntity );

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
                var results = context.Entity_Occupation
                            .Where( s => s.EntityId == parent.Id )
                            .OrderBy( s => s.OccupationProfile.Name )
                            .ToList();
                if ( results == null || results.Count == 0 )
                {
                    return true;
                }

                foreach ( var item in results )
                {
                    //if a reference, delete actual Occupation if not used elsewhere
                    if ( item.OccupationProfile != null && item.OccupationProfile.EntityStateId == 2 )
                    {
                        //do a getall. If only one, delete it.
                        var exists = context.Entity_Occupation
                            .Where( s => s.OccupationId == item.OccupationId )
                            .ToList();
                        if ( exists != null && exists.Count() == 1 )
                        {
                            var statusMsg = "";
                            //this method will also add pending request to remove from elastic.
                            //20-12-18 mp - Only done for a reference lopp but what about a full lopp that may now be an orphan? We are not allowing lopps without parent, but will still exist in registry!!!
                            //actually this delete will probably also delete the Entity_Occupation
                            //new OccupationManager().Delete( item.OccupationId, ref statusMsg );
                            //continue;
                        }
                    }
                    context.Entity_Occupation.Remove( item );
                    count = context.SaveChanges();
                    if ( count > 0 )
                    {

                    }
                }
            }

            return isValid;
        }
        #endregion

        public static List<ThisResource> GetAllSummary( Guid parentUid, int relationshipTypeId )
        {
            //note even the summary should include indicator of competencies
            return TargetResource_GetAll( parentUid, false, relationshipTypeId );
        }
        /// <summary>
        /// Get all occupations for the parent
        /// Uses the parent Guid to retrieve the related Entity, then uses the EntityId to retrieve the child objects.
        /// </summary>
        /// <param name="parentUid"></param>
        /// <param name="forProfilesList"></param>
        /// <returns></returns>
        public static List<ThisResource> TargetResource_GetAll( Guid parentUid,
                    bool forProfilesList,
                    int relationshipTypeId = 1 )
        {
            List<ThisResource> list = new List<ThisResource>();
            ThisResource entity = new ThisResource();

            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                return list;
            }
            bool includingProfiles = false;

            LoggingHelper.DoTrace( 7, $"{thisClassName}.TargetResource_GetAll: parentUid:{parentUid} entityId:{parent.Id}, e.EntityTypeId:{parent.EntityTypeId}" );
            try
            {
                using ( var context = new EntityContext() )
                {
                    List<DBResource> results = context.Entity_Occupation
                            .Where( s => s.EntityId == parent.Id && ( relationshipTypeId == 0 || s.RelationshipTypeId == relationshipTypeId ) )
                            .OrderBy( s => s.OccupationProfile.Name )
                            .ToList();

                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( DBResource item in results )
                        {
                            entity = new ThisResource();
                            if ( item.OccupationProfile != null && item.OccupationProfile.EntityStateId > 1 )
                            {
                                if ( forProfilesList  )
                                {
                                    //TODO - add a minimal mapper
                                    RelatedResourceManager.MapFromDB( item.OccupationProfile, entity, false );
                                    list.Add( entity );

                                }
                                else
                                {
                                    //if ( CacheManager.IsOccupationAvailableFromCache( item.OccupationId, ref entity ) )
                                    //{
                                    //    list.Add( entity );
                                    //}
                                    //else
                                    {
                                        //TODO - is this section used??
                                        //to determine minimum needed for a or detail page
                                        RelatedResourceManager.MapFromDB( item.OccupationProfile, entity, false );
                                        list.Add( entity );
                                        
                                    }
                                }
                            }
                        }
                    }
                    return list;
                }


            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".TargetResource_GetAll. Guid: {0}, parentType: {1} ({2}), ", parentUid, parent.EntityType, parent.EntityBaseId ) );
            }
            return list;
        }

    }
}
