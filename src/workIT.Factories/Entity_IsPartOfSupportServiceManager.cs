using System;
using System.Collections.Generic;
using System.Linq;

using workIT.Models.Common;
using workIT.Models;

using workIT.Utilities;

using DBEntity = workIT.Data.Tables.Entity_IsPartOfSupportService;
//using EM = workIT.Data;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisResource = workIT.Models.Common.Entity_IsPartOfSupportService;
using MainResource = workIT.Models.Common.SupportService;

namespace workIT.Factories
{
    public class Entity_IsPartOfSupportServiceManager : BaseFactory
    {
        static string thisClassName = "Entity_IsPartOfSupportServiceManager";
        static string thisClassLabel = "Is Part of Support Service";
        /// <summary>
        /// if true, return an error message if the resource is already associated with the parent
        /// </summary>
        private bool ReturningErrorOnDuplicate { get; set; }
        public Entity_IsPartOfSupportServiceManager()
        {
            ReturningErrorOnDuplicate = false;
        }
        public Entity_IsPartOfSupportServiceManager( bool returnErrorOnDuplicate )
        {
            ReturningErrorOnDuplicate = returnErrorOnDuplicate;
        }

        #region Entity Persistance ===================
        /// <summary>
        /// Persist Entity IsPartOfSupportService
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
                messages.Add( "Error: a valid IsPartOfSupportService was not provided." );
            }
            if ( messages.Count > intialCount )
                return 0;

            DBEntity efEntity = new DBEntity();
            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                messages.Add( "Error - EntityIsPartOfSupportService.Add: The parent entity was not found: " + parentUid.ToString() );
                return 0;
            }

            using ( var context = new EntityContext() )
            {
                //first check for duplicates
                efEntity = context.Entity_IsPartOfSupportService
                        .FirstOrDefault( s => s.EntityId == parent.Id && s.SupportServiceId == supportServiceId );
                if ( efEntity != null && efEntity.Id > 0 )
                {
                    if ( ReturningErrorOnDuplicate )
                    {
                        messages.Add( "Error - the IsPartOfSupportService is already part of this profile." );
                    }
                    return efEntity.Id;
                }

                if ( allowMultiples == false )
                {
                    //check if one exists, and replace if found
                    efEntity = context.Entity_IsPartOfSupportService
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
                efEntity = new DBEntity();
                efEntity.SupportServiceId = supportServiceId;
                efEntity.EntityId = parent.Id;
                efEntity.Created = DateTime.Now;

                context.Entity_IsPartOfSupportService.Add( efEntity );
                count = context.SaveChanges();
                //update profile record so doesn't get deleted
                newId = efEntity.Id;

                if ( count == 0 )
                {
                    messages.Add( string.Format( " Unable to add the related IsPartOfSupportService: {0}  ", supportServiceId ) );
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
                DBEntity op = new DBEntity();

                //get all existing for the category
                var results = context.Entity_IsPartOfSupportService
                            .Where( s => s.EntityId == parentEntity.Id )
                            .OrderBy( s => s.SupportServiceId )
                            .ToList();

                #region deletes check
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
                        string statusMessage = string.Empty;
                        Delete( v.DeleteId, ref statusMessage );
                    }
                }
                #endregion

                #region new items
                //should only empty ids, where not in current list, so should be adds
                var newList = from item in input
                              join existing in results
                                    on item equals existing.SupportServiceId
                                    into joinTable
                              from result in joinTable.DefaultIfEmpty( new DBEntity { Id = 0, SupportServiceId = 0 } )
                              select new { AddId = item, ExistingId = result.SupportServiceId };

                foreach ( var v in newList )
                {
                    if ( v.ExistingId == 0 && v.AddId > 0 )
                    {
                        op = new DBEntity
                        {
                            EntityId = parentEntity.Id,
                            SupportServiceId = v.AddId,
                            Created = System.DateTime.Now,
                        };

                        context.Entity_IsPartOfSupportService.Add( op );
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
                DBEntity op = new DBEntity();

                //get all existing for the category
                var results = context.Entity_IsPartOfSupportService
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
                        string statusMessage = string.Empty;
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
                              from result in joinTable.DefaultIfEmpty( new DBEntity { Id = 0, SupportServiceId = 0 } )
                              select new { AddId = item.Id, ExistingId = result.SupportServiceId };

                foreach ( var v in newList )
                {
                    if ( v.ExistingId == 0 && v.AddId > 0 )
                    {
                        op = new DBEntity
                        {
                            EntityId = parentEntity.Id,
                            SupportServiceId = v.AddId,
                            Created = System.DateTime.Now,
                        };

                        context.Entity_IsPartOfSupportService.Add( op );
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
                DBEntity p = context.Entity_IsPartOfSupportService.FirstOrDefault( s => s.Id == recordId );
                if ( p != null && p.Id > 0 )
                {
                    context.Entity_IsPartOfSupportService.Remove( p );
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
        /// Delete a entity credentail via the entity id and IsPartOfSupportService id
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
                DBEntity p = context.Entity_IsPartOfSupportService.FirstOrDefault( s => s.EntityId == parentId && s.SupportServiceId == supportServiceId );
                if ( p != null && p.Id > 0 )
                {
                    context.Entity_IsPartOfSupportService.Remove( p );
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
        /// <param name="parentUid">Used to retrieve the parent Entity for Entity_IsPartOfSupportService</param>
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
        /// <param name="parent">Used to retrieve the parent Entity for Entity_IsPartOfSupportService</param>
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
                context.Entity_IsPartOfSupportService.RemoveRange( context.Entity_IsPartOfSupportService.Where( s => s.EntityId == parent.Id ) );
                int count = context.SaveChanges();
                if ( count > 0 )
                {
                    isValid = true;
                    status.AddError( string.Format( "removed {0} related IsPartOfSupportServices.", count ) );
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
        /// <summary>
        /// get all the base IsPartOfSupportServices for an EntityIsPartOfSupportService
        /// Uses the parent Guid to retrieve the related Entity, then uses the EntityId to retrieve the child objects.
        /// </summary>
        /// <param name="parentUid"></param>
        /// <returns></returns>
        public static List<TopLevelObject> GetAll( Guid parentUid, int relationshipTypeId = 1, bool isForDetailPageCondition = false )
        {
            ThisResource entity = new ThisResource();
            var list = new List<TopLevelObject>();
            var mainResource = new MainResource();
            var summaryResource = new TopLevelObject();

            Entity parent = EntityManager.GetEntity( parentUid );
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

                    List<DBEntity> results = context.Entity_IsPartOfSupportService
                            //.Include( "SupportService")
                            //.AsNoTracking()
                            .Where( s => s.EntityId == parent.Id )
                            .OrderBy( s => s.SupportService.Name )
                            .ToList();

                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( DBEntity item in results )
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
                LoggingHelper.LogError( ex, thisClassName + ".GetAll" );
            }
            return list;
        }//

        public static List<TopLevelObject> GetHasSpecificService( int supportServiceId, bool isForDetailPageCondition = false )
        {
            ThisResource resource = new ThisResource();
            var list = new List<TopLevelObject>();
           // var mainResource = new MainResource();
            var summaryResource = new TopLevelObject();

            try
            {
                using ( var context = new EntityContext() )
                {
                    var list1 = from item in context.Entity_IsPartOfSupportService
                                join entity in context.Entity on item.EntityId equals entity.Id
                                join supportSrv in context.SupportService on entity.EntityUid equals supportSrv.RowId
                                join org in context.Organization on supportSrv.PrimaryAgentUid equals org.RowId
                                //
                                where item.SupportServiceId == supportServiceId
                                    && supportSrv.EntityStateId == 3

                                select new
                                {
                                    supportSrv.Id,
                                    supportSrv.RowId,
                                    supportSrv.Name,
                                    supportSrv.SubjectWebpage,
                                    supportSrv.Description,
                                    supportSrv.CTID,
                                    supportSrv.EntityStateId,
                                    supportSrv.PrimaryAgentUid,
                                    PrimaryOrganization = org.Name,
                                    PrimaryOrganizationCTID = org.CTID,
                                    PrimaryOrganizationId = org.Id

                                };
                    var ssList = list1.OrderBy( m => m.Name ).ToList();
                    foreach ( var item in ssList )
                    {
                        if ( item.EntityStateId < 2 )
                            continue;
                        //skip if parent is the same as the request
                        if ( item.Id == supportServiceId )
                            continue;

                        summaryResource = new TopLevelObject()
                        {
                            EntityType = "SupportService",
                            EntityTypeId = CodesManager.ENTITY_TYPE_SUPPORT_SERVICE,
                            Id = item.Id,
                            Name = item.Name,
                            RowId = item.RowId,
                            Description = item.Description,
                            CTID = item.CTID,
                            SubjectWebpage = item.SubjectWebpage
                           
                        };
                        summaryResource.PrimaryOrganization = new Organization()
                        {
                            Id = item.PrimaryOrganizationId,
                            Name = item.PrimaryOrganization,
                            CTID = item.PrimaryOrganizationCTID,
                        };
                        
                        list.Add( summaryResource );
                    }

                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".GetHasSpecificService" );
            }
            return list;
        }//

        /*
        /// <summary>
        /// get all the base IsPartOfSupportServices for an EntityIsPartOfSupportService
        /// Uses the parent Guid to retrieve the related Entity, then uses the EntityId to retrieve the child objects.
        /// </summary>
        /// <param name="parentUid"></param>
        /// <returns></returns>
        public static List<MainResource> GetAll( Guid parentUid, int relationshipTypeId = 1, bool isForDetailPageCondition = false )
        {
            ThisResource entity = new ThisResource();
            var list = new List<MainResource>();
            var mainResource = new MainResource();
            Entity parent = EntityManager.GetEntity( parentUid );
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

                    List<DBEntity> results = context.Entity_IsPartOfSupportService
                            //.Include( "SupportService")
                            //.AsNoTracking()
                            .Where( s => s.EntityId == parent.Id )
                            .OrderBy( s => s.SupportService.Name )
                            .ToList();

                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( DBEntity item in results )
                        {
                            if ( item.SupportService != null && item.SupportService.EntityStateId >= CodesManager.ENTITY_STATEID_REFERENCE )
                            {
                                //need to only get summary/outline level
                                mainResource = new MainResource();
                                SupportServiceManager.MapFromDB_ForSummary( item.SupportService, mainResource );

                                list.Add( mainResource );
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

        public static List<MainResource> GetHasSpecificService( int supportServiceId, bool isForDetailPageCondition = false )
        {
            ThisResource resource = new ThisResource();
            var list = new List<MainResource>();
            var mainResource = new MainResource();

            try
            {
                using ( var context = new EntityContext() )
                {
                    var list1 = from item in context.Entity_IsPartOfSupportService
                                join entity in context.Entity on item.EntityId equals entity.Id
                                join supportSrv in context.SupportService on entity.EntityUid equals supportSrv.RowId
                                join org in context.Organization on supportSrv.PrimaryAgentUid equals org.RowId
                                //
                                where item.SupportServiceId == supportServiceId
                                    && supportSrv.EntityStateId == 3

                                select new
                                {
                                    supportSrv.Id,
                                    supportSrv.RowId,
                                    supportSrv.Name,
                                    supportSrv.SubjectWebpage,
                                    supportSrv.Description,
                                    supportSrv.CTID,
                                    supportSrv.EntityStateId,
                                    supportSrv.PrimaryAgentUid,
                                    PrimaryOrganization = org.Name,
                                    PrimaryOrganizationCTID = org.CTID,
                                    PrimaryOrganizationId = org.Id

                                };
                    var ssList = list1.OrderBy( m => m.Name ).ToList();
                    foreach ( var item in ssList )
                    {
                        if ( item.EntityStateId < 2 )
                            continue;
                        //skip if parent is the same as the request
                        if ( item.Id == supportServiceId )
                            continue;

                        mainResource = new MainResource
                        {
                            Id = item.Id,
                            Name = item.Name,
                            RowId = item.RowId,
                            Description = item.Description,
                            CTID = item.CTID,
                            SubjectWebpage = item.SubjectWebpage,
                            EntityStateId = ( int ) item.EntityStateId
                        };
                        mainResource.OwnedBy = new List<ResourceSummary>()
                        {
                            new ResourceSummary()
                            {
                                Id = item.PrimaryOrganizationId,
                                Name = item.PrimaryOrganization,
                                CTID= item.PrimaryOrganizationCTID,
                            }
                        };
                        list.Add( mainResource );
                    }

                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".GetHasSpecificService" );
            }
            return list;
        }//
        */
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
                    DBEntity item = context.Entity_IsPartOfSupportService
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
                    DBEntity item = context.Entity_IsPartOfSupportService
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

        public static void MapToDB( ThisResource from, DBEntity to )
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
        public static void MapFromDB( DBEntity from, ThisResource to, bool isForDetailPageCondition = false )
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
