using System;
using System.Collections.Generic;
using System.Linq;

using workIT.Models.Common;
using workIT.Models;

using workIT.Utilities;

using DBResource = workIT.Data.Tables.Entity_HasSupportService;
//using EM = workIT.Data;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisResource = workIT.Models.Common.Entity_HasSupportService;
using MainResource = workIT.Models.Common.SupportService;

namespace workIT.Factories
{
    public class Entity_HasSupportServiceManager : BaseFactory
    {
        static string thisClassName = "Entity_HasSupportServiceManager";
        static string thisClassLabel = "Has Support Service";
        /// <summary>
        /// if true, return an error message if the HasSupportService is already associated with the parent
        /// </summary>
        private bool ReturningErrorOnDuplicate { get; set; }
        public Entity_HasSupportServiceManager()
        {
            ReturningErrorOnDuplicate = false;
        }
        public Entity_HasSupportServiceManager( bool returnErrorOnDuplicate )
        {
            ReturningErrorOnDuplicate = returnErrorOnDuplicate;
        }

        #region Entity Persistance ===================
        /// <summary>
        /// Persist Entity HasSupportService
        /// </summary>
        /// <param name="supportServiceId"></param>
        /// <param name="parentUid"></param>
        /// <param name="userId"></param>
        /// <param name="allowMultiples"></param>
        /// <param name="newId">Return record id of the new record</param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public int Add( Guid parentUid, int supportServiceId, bool allowMultiples, ref int newId, ref List<string> messages )
        {
            int count = 0;
            newId = 0;
            int intialCount = messages.Count;

            if ( !IsValidGuid( parentUid ) )
            {
                messages.Add( "Error: the parent identifier was not provided." );
            }

            if ( supportServiceId < 1 )
            {
                messages.Add( "Error: a valid HasSupportService was not provided." );
            }
            if ( messages.Count > intialCount )
                return 0;

            DBResource efEntity = new DBResource();
            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                messages.Add( "Error - EntityHasSupportService.Add: The parent entity was not found: " + parentUid.ToString() );
                return 0;
            }

            using ( var context = new EntityContext() )
            {
                //first check for duplicates
                efEntity = context.Entity_HasSupportService
                        .FirstOrDefault( s => s.EntityId == parent.Id && s.SupportServiceId == supportServiceId );
                if ( efEntity != null && efEntity.Id > 0 )
                {
                    if ( ReturningErrorOnDuplicate )
                    {
                        messages.Add( "Error - the HasSupportService is already part of this profile." );
                    }
                    return efEntity.Id;
                }

                if ( allowMultiples == false )
                {
                    //check if one exists, and replace if found
                    efEntity = context.Entity_HasSupportService
                        .FirstOrDefault( s => s.EntityId == parent.Id );
                    if ( efEntity != null && efEntity.Id > 0 )
                    {
                        efEntity.SupportServiceId = supportServiceId;
                        count = context.SaveChanges();

                        return efEntity.Id;
                    }
                }

                //if ( entity.Id == 0 )
                //{	}
                //add
                efEntity = new DBResource();
                efEntity.SupportServiceId = supportServiceId;
                efEntity.EntityId = parent.Id;
                efEntity.Created = DateTime.Now;

                context.Entity_HasSupportService.Add( efEntity );
                count = context.SaveChanges();
                //update profile record so doesn't get deleted
                newId = efEntity.Id;

                if ( count == 0 )
                {
                    messages.Add( string.Format( " Unable to add the related HasSupportService: {0}  ", supportServiceId ) );
                }

            }

            return newId;
        }
        public bool Update( List<int> input, Entity parentEntity, ref SaveStatus status )
        {
            bool isAllValid = true;
            int updatedCount = 0;
            int deletedCount = 0;
            int count = 0;

            if ( parentEntity == null || parentEntity.Id == 0 )
            {
                status.AddError( $"{thisClassLabel}.Update A valid parent identifier was not provided to the Update method." );
                return false;
            }

            //
            if ( input == null || !input.Any() )
            {
                //do we do a delete just in case?
                DeleteAll( parentEntity, ref status );
                return true;
            }

            //for an update, we need to check for deleted Items, or just delete all and re-add
            //==> then interface would have to always return everything
            using ( var context = new EntityContext() )
            {
                DBResource op = new DBResource();

                //get all existing for the category
                var results = context.Entity_HasSupportService
                            .Where( s => s.EntityId == parentEntity.Id )
                            .OrderBy( s => s.SupportServiceId )
                            .ToList();

                #region deletes check
                if ( results != null && results.Any() )
                {
                    var deleteList = from existing in results
                                     join item in input
                                             on existing.SupportServiceId equals item
                                             into joinTable
                                     from result in joinTable.DefaultIfEmpty()
                                     select new { DeleteId = existing.Id, ItemId = result };

                    foreach ( var v in deleteList )
                    {
                        if ( v.ItemId == 0 )
                        {
                            //delete item
                            deletedCount++;
                            string statusMessage = "";
                            Delete( v.DeleteId, ref statusMessage );
                        }
                    }
                }
                #endregion

                #region new items
                //should only empty ids, where not in current list, so should be adds
                var newList = from item in input
                              join existing in results
                                    on item equals existing.SupportServiceId
                                    into joinTable
                              from result in joinTable.DefaultIfEmpty( new DBResource { Id = 0, SupportServiceId = 0 } )
                              select new { AddId = item, ExistingId = result.SupportServiceId };

                foreach ( var v in newList )
                {
                    if ( v.ExistingId == 0 && v.AddId > 0 )
                    {
                        op = new DBResource
                        {
                            EntityId = parentEntity.Id,
                            SupportServiceId = v.AddId,
                            Created = System.DateTime.Now,
                        };

                        context.Entity_HasSupportService.Add( op );
                        count = context.SaveChanges();
                        if ( count == 0 )
                        {
                            status.AddError( $"{thisClassLabel}.Update. For {parentEntity.EntityBaseName}, unable to add support service Id of: {v.AddId}" );
                            isAllValid = false;
                        }
                        else
                            updatedCount++;
                    }
                    else
                    {
                        //may need to check that schema of other has a value, or if not other, the value is ignored. Actually only for a dropdown. For multi select, need to record whether other was checked anywhere (but prob has to be the last entry!)
                    }
                }
                #endregion
            }

            if ( deletedCount > 0 || updatedCount > 0 )
            {
                //new EntityManager().UpdateTopLevelEntityLastUpdateDate( parentEntity.Id, string.Format( "Entity Update triggered by userId: {0} adding ({1})/removing ({2}) properties to/from EntityType: {3}, BaseId: {4}", userId, updatedCount, deletedCount, parent.EntityType, parent.EntityBaseId ), userId );
            }


            return isAllValid;
        }

        public bool Update( List<SupportService> input, Entity parentEntity, ref SaveStatus status )
        {
            bool isAllValid = true;
            int updatedCount = 0;
            int deletedCount = 0;
            int count = 0;

            if ( parentEntity == null || parentEntity.Id == 0 )
            {
                status.AddError( $"{thisClassLabel}.Update A valid parent identifier was not provided to the Update method." );
                return false;
            }

            //
            if ( input == null || !input.Any() )
            {
                //do we do a delete just in case?
                DeleteAll( parentEntity, ref status );
                return true;
            }

            //for an update, we need to check for deleted Items, or just delete all and re-add
            //==> then interface would have to always return everything
            using ( var context = new EntityContext() )
            {
                DBResource op = new DBResource();

                //get all existing for the category
                var results = context.Entity_HasSupportService
                            .Where( s => s.EntityId == parentEntity.Id )
                            .OrderBy( s => s.SupportServiceId )
                            .ToList();

                #region deletes check
                var deleteList = from existing in results
                                 join item in input
                                         on existing.SupportServiceId equals item.Id
                                         into joinTable
                                 from result in joinTable.DefaultIfEmpty( new SupportService { Name = "missing", Id = 0 } )
                                 select new { DeleteId = existing.Id, ItemId = ( result.Id ) };

                foreach ( var v in deleteList )
                {
                    if ( v.ItemId == 0 )
                    {
                        //delete item
                        deletedCount++;
                        string statusMessage = "";
                        Delete( v.DeleteId, ref statusMessage );
                    }
                }
                #endregion

                #region new items
                //should only empty ids, where not in current list, so should be adds
                var newList = from item in input
                              join existing in results
                                    on item.Id equals existing.SupportServiceId
                                    into joinTable
                              from result in joinTable.DefaultIfEmpty( new DBResource { Id = 0, SupportServiceId = 0 } )
                              select new { AddId = item.Id, ExistingId = result.SupportServiceId };

                foreach ( var v in newList )
                {
                    if ( v.ExistingId == 0 && v.AddId > 0 )
                    {
                        op = new DBResource
                        {
                            EntityId = parentEntity.Id,
                            SupportServiceId = v.AddId,
                            Created = System.DateTime.Now,
                        };

                        context.Entity_HasSupportService.Add( op );
                        count = context.SaveChanges();
                        if ( count == 0 )
                        {
                            status.AddError( $"{thisClassLabel}.Update. For {parentEntity.EntityBaseName}, unable to add support service Id of: {v.AddId}" );
                            isAllValid = false;
                        }
                        else
                            updatedCount++;
                    }
                    else
                    {
                        //may need to check that schema of other has a value, or if not other, the value is ignored. Actually only for a dropdown. For multi select, need to record whether other was checked anywhere (but prob has to be the last entry!)
                    }
                }
                #endregion
            }

            if ( deletedCount > 0 || updatedCount > 0 )
            {
                //new EntityManager().UpdateTopLevelEntityLastUpdateDate( parentEntity.Id, string.Format( "Entity Update triggered by userId: {0} adding ({1})/removing ({2}) properties to/from EntityType: {3}, BaseId: {4}", userId, updatedCount, deletedCount, parent.EntityType, parent.EntityBaseId ), userId );
            }


            return isAllValid;
        }

        /// <summary>
        /// Delete a specific record by Id
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public bool Delete( int recordId, ref string statusMessage )
        {
            bool isOK = true;
            using ( var context = new EntityContext() )
            {
                DBResource p = context.Entity_HasSupportService.FirstOrDefault( s => s.Id == recordId );
                if ( p != null && p.Id > 0 )
                {
                    context.Entity_HasSupportService.Remove( p );
                    int count = context.SaveChanges();
                }
                else
                {
                    //just ignore if not found?
                    //statusMessage = string.Format( thisClassName + ".Delete() Requested record was not found. recordId: {0}.", recordId );
                    //isOK = false;
                }
            }
            return isOK;
        }
        /// <summary>
        /// Delete a entity credentail via the entity id and HasSupportService id
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="supportServiceId"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public bool Delete( int parentId, int supportServiceId, ref string statusMessage )
        {
            bool isOK = true;
            using ( var context = new EntityContext() )
            {
                //20-06-16 mparsons - now need to include relationshiptypeid or could have issue
                //					- currently the microsearch has no context to determine the relationshiptypeid
                DBResource p = context.Entity_HasSupportService.FirstOrDefault( s => s.EntityId == parentId && s.SupportServiceId == supportServiceId );
                if ( p != null && p.Id > 0 )
                {
                    context.Entity_HasSupportService.Remove( p );
                    int count = context.SaveChanges();
                }
                else
                {
                    statusMessage = string.Format( thisClassName + ".Delete() Requested record was not found. parentId: {0}, supportServiceId: {1}", parentId, supportServiceId );
                    isOK = false;
                }
            }
            return isOK;
        }
        /// <summary>
        /// Delete all Entities using the parent entity
        /// </summary>
        /// <param name="parentUid">Used to retrieve the parent Entity for Entity_HasSupportService</param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public bool DeleteAll( Guid parentUid, ref SaveStatus status )
        {
            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + ".DeleteAll() Error - the parent entity was not found: " + parentUid.ToString() );
                return false;
            }
            return DeleteAll( parent, ref status );
        }
        /// <summary>
        /// Delete all Entities using the parent rowId
        /// </summary>
        /// <param name="parent">Used to retrieve the parent Entity for Entity_HasSupportService</param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public bool DeleteAll( Entity parent, ref SaveStatus status )
        {
            bool isValid = true;
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + ".DeleteAll() Error - the parent entity was not found: 0" );
                return false;
            }
            using ( var context = new EntityContext() )
            {
                context.Entity_HasSupportService.RemoveRange( context.Entity_HasSupportService.Where( s => s.EntityId == parent.Id ) );
                int count = context.SaveChanges();
                if ( count > 0 )
                {
                    isValid = true;
                    status.AddError( string.Format( "removed {0} related HasSupportServices.", count ) );
                }
            }
            return isValid;

        }


        public bool ValidateEntity( ThisResource profile, ref bool isEmpty, ref List<string> messages )
        {
            bool isValid = true;

            isEmpty = false;
            //check if empty
            if ( profile.SupportServiceId == 0 )
            {
                isEmpty = true;
                return isValid;
            }


            return isValid;
        }

        #endregion

        #region  retrieval ==================

        //public static List<MainResource> GetAllSummary( Guid parentUid, int relationshipTypeId )
        //{
        //    //note even the summary should include indicator of competencies
        //    return GetAll( parentUid, relationshipTypeId );
        //}

        /// <summary>
        /// get all the base HasSupportServices for an EntityHasSupportService
        /// Uses the parent Guid to retrieve the related Entity, then uses the EntityId to retrieve the child objects.
        /// </summary>
        /// <param name="parentUid"></param>
        /// <returns></returns>
        public static List<MainResource> GetAll( Entity parent, int relationshipTypeId = 1, int maxRecords = 0 )
        {
            ThisResource entity = new ThisResource();
            var list = new List<MainResource>();
            var mainResource = new MainResource();
            //Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                return list;
            }
            try
            {
                using ( var context = new EntityContext() )
                {
                    //commented out in order to get more data for detail page
                    //context.Configuration.LazyLoadingEnabled = false;

                    List<DBResource> results = context.Entity_HasSupportService
                            .Where( s => s.EntityId == parent.Id )
                            .OrderBy( s => s.SupportService.Name )
                            .ToList();

                    if ( results != null && results.Count > 0 )
                    {
                        int cntr = 0;
                        foreach ( DBResource item in results )
                        {
                            if ( item.SupportService != null && item.SupportService.EntityStateId >= CodesManager.ENTITY_STATEID_REFERENCE )
                            {
                                cntr++;
                                //need to only get summary/outline level
                                mainResource = new MainResource();
                                SupportServiceManager.MapFromDB_ForSummary( item.SupportService, mainResource );

                                list.Add( mainResource );
                                if ( maxRecords > 0 && cntr >= maxRecords )
                                    break;
                            }
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".GetAll" );
            }
            return list;
        }//
        public static List<ResourceSummary> GetAllAsResourceSummary( Entity parent )
        {
            ThisResource entity = new ThisResource();
            var list = new List<ResourceSummary>();
            var mainResource = new MainResource();
            var summaryResource = new ResourceSummary();
            //Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                return list;
            }
            try
            {
                using ( var context = new EntityContext() )
                {
                    //commented out in order to get more data for detail page
                    //context.Configuration.LazyLoadingEnabled = false;

                    List<DBResource> results = context.Entity_HasSupportService
                            .Where( s => s.EntityId == parent.Id )
                            .OrderBy( s => s.SupportService.Name )
                            .ToList();

                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( DBResource item in results )
                        {
                            if ( item.SupportService != null && item.SupportService.EntityStateId >= CodesManager.ENTITY_STATEID_REFERENCE )
                            {
                                //need to only get summary/outline level
                                mainResource = new MainResource();
                                SupportServiceManager.MapFromDB_ForSummary( item.SupportService, mainResource );
                                summaryResource = new ResourceSummary()
                                {
                                    Id = mainResource.Id,
                                    EntityTypeId = CodesManager.ENTITY_TYPE_SUPPORT_SERVICE,
                                    Name = mainResource.Name,
                                    Description = mainResource.Description,
                                    URI = mainResource.SubjectWebpage,
                                    CTID = mainResource.CTID,
                                };
                                list.Add( summaryResource );
                            }
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".GetAll" );
            }
            return list;
        }//
        public static List<TopLevelObject> GetAllSummary( Entity parent, bool includeNestedResources = false )
        {
            ThisResource entity = new ThisResource();
            var list = new List<TopLevelObject>();
            var mainResource = new MainResource();
            var summaryResource = new TopLevelObject();
            //Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                return list;
            }
            try
            {
                using ( var context = new EntityContext() )
                {
                    List<DBResource> results = context.Entity_HasSupportService
                            .Where( s => s.EntityId == parent.Id )
                            .OrderBy( s => s.SupportService.Name )
                            .ToList();

                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( DBResource item in results )
                        {
                            if ( item.SupportService != null && item.SupportService.EntityStateId >= CodesManager.ENTITY_STATEID_REFERENCE )
                            {
                                //need to only get summary/outline level
                                mainResource = new MainResource();
                                SupportServiceManager.MapFromDB_ForSummary( item.SupportService, mainResource );
                                summaryResource = new TopLevelObject()
                                {
                                    EntityType = "SupportService", 
                                    EntityTypeId = CodesManager.ENTITY_TYPE_SUPPORT_SERVICE,
                                    Id = mainResource.Id,
                                    Name = mainResource.Name,
                                    Description = mainResource.Description,
                                    SubjectWebpage = mainResource.SubjectWebpage,
                                    CTID = mainResource.CTID,
                                };
                                list.Add( summaryResource );
                            }
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".GetAllSummary" );
            }
            return list;
        }//
        /// <summary>
        /// Get all resources that reference the provided supportServiceId
        /// </summary>
        /// <param name="supportServiceId"></param>
        /// <returns></returns>
        public static List<TopLevelObject> GetAllTargets( int supportServiceId )
        {
            ThisResource entity = new ThisResource();
            var list = new List<TopLevelObject>();
            var mainResource = new MainResource();
            var summaryResource = new TopLevelObject();
            if (supportServiceId == 0)
            {
                return list;
            }
            try
            {
                using (var context = new EntityContext())
                {
                    var results = (from record in context.Entity_HasSupportService
                                   join cache in context.Entity_Cache on record.EntityId equals cache.Id
                                   join org in context.Organization on cache.OwningOrgId equals org.Id into gj
                                   from subOrg in gj.DefaultIfEmpty()
                                   where record.SupportServiceId == supportServiceId
                                   && cache.EntityStateId == 3

                                   select new
                                   {
                                       record.Id,
                                       cache.CTID,
                                       cache.BaseId,
                                       cache.EntityType,
                                       cache.EntityTypeId,
                                       cache.EntityUid,
                                       cache.Name,
                                       cache.EntityStateId,
                                       cache.OwningOrgId,
                                       //TBD
                                       Organization = subOrg.Name ?? "",
                                       OrganizationCTID = subOrg.CTID ?? "",
                                       cache.LastUpdated,
                                   }).ToList();

                    if (results != null && results.Count > 0)
                    {
                        foreach (var ec in results)
                        {
                            summaryResource = new TopLevelObject()
                            {
                                //OwningOrgCTID = ec.OrganizationCTID,

                                Id = ec.BaseId,
                                CTID = ec.CTID,
                                EntityType = ec.EntityType,
                                EntityTypeId = ec.EntityTypeId,
                                EntityStateId = (int) ec.EntityStateId,
                                
                                Name = ec.Name,
                                //OwningOrgId = ec.OwningOrgId ?? 0,
                                RowId = ec.EntityUid,
                                LastUpdated = (DateTime) ec.LastUpdated
                            };
                            list.Add( summaryResource );
                           
                        }
                    }
                    //List<DBResource> results = context.Entity_HasSupportService
                    //        .Where( s => s.SupportServiceId == supportServiceId )
                    //        .OrderBy( s => s.Entity.EntityTypeId)
                    //        .ThenBy( s => s.Entity.EntityBaseName)
                    //        .ToList();

                    //if (results != null && results.Count > 0)
                    //{
                    //    foreach (var item in results)
                    //    {
                    //        if (item.Entity != null )
                    //        {
                    //            summaryResource = new TopLevelObject()
                    //            {
                    //                EntityType = "TBD",
                    //                EntityTypeId = item.Entity.EntityTypeId,
                    //                Id = (int)item.Entity.EntityBaseId,
                    //                Name = item.Entity.EntityBaseName,
                    //                //Description = mainResource.Description,
                    //                //SubjectWebpage = mainResource.SubjectWebpage,
                    //                //CTID = mainResource.CTID,
                    //            };
                    //            list.Add( summaryResource );
                    //        }
                    //    }
                    //}
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError( ex, thisClassName + ".GetAllTargets" );
            }
            return list;
        }//

        public static ThisResource Get( int profileId )
        {
            ThisResource entity = new ThisResource();
            if ( profileId == 0 )
            {
                return entity;
            }
            try
            {
                using ( var context = new EntityContext() )
                {
                    DBResource item = context.Entity_HasSupportService
                            .SingleOrDefault( s => s.Id == profileId );

                    if ( item != null && item.Id > 0 )
                    {
                        MapFromDB( item, entity );
                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".Entity_Get" );
            }
            return entity;
        }//

        public static ThisResource Get( int parentId, int supportServiceId )
        {
            ThisResource entity = new ThisResource();
            if ( parentId < 1 || supportServiceId < 1 )
            {
                return entity;
            }
            try
            {
                using ( var context = new EntityContext() )
                {
                    DBResource item = context.Entity_HasSupportService
                            .FirstOrDefault( s => s.SupportServiceId == supportServiceId && s.EntityId == parentId );

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

        public static void MapToDB( ThisResource from, DBResource to )
        {
            //want to ensure fields from create are not wiped
            if ( to.Id == 0 )
            {
                if ( IsValidDate( from.Created ) )
                    to.Created = from.Created;
            }
            to.Id = from.Id;
            to.SupportServiceId = from.SupportServiceId;
            to.EntityId = from.EntityId;

        }
        public static void MapFromDB( DBResource from, ThisResource to, bool isForDetailPageCondition = false )
        {
            to.Id = from.Id;
            to.SupportServiceId = from.SupportServiceId;
            to.EntityId = from.EntityId;
            if ( from.SupportService != null && from.SupportService.EntityStateId >= CodesManager.ENTITY_STATEID_REFERENCE )
            {
                var mainResource = new MainResource();
                SupportServiceManager.MapFromDB_ForSummary( from.SupportService, mainResource );

                to.SupportService = mainResource;
            }

            if ( IsValidDate( from.Created ) )
                to.Created = ( DateTime ) from.Created;
        }

        #endregion

    }
}
