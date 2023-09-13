using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;

using ThisEntity = workIT.Models.SearchPendingReindex;
using DBEntity = workIT.Data.Tables.SearchPendingReindex;
using EntityContext = workIT.Data.Tables.workITEntities;


using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;


namespace workIT.Factories
{
    public class SearchPendingReindexManager : BaseFactory
    {
        static string thisClassName = "SearchPendingReindexManager";
        public static int Reindex_Add_Request = 1;
        public static int Reindex_Delete_Request = 2;

        #region persistance ==================
        /// <summary>
        /// SearchPendingReindexes Add
        /// </summary>
        /// <param name="entityTypeId"></param>
        /// <param name="recordId"></param>
        /// <param name="actionTypeId">1-add; 2-delete</param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public int Add( int entityTypeId, int recordId, int actionTypeId, ref List<String> messages )
        {
            if (recordId < 1)
                return 0;

            ThisEntity entity = new ThisEntity() { EntityTypeId = entityTypeId, RecordId = recordId, IsUpdateOrDeleteTypeId = actionTypeId, StatusId = 1 };
            return Add( entity, ref messages );            
        }
        /// <summary>
        /// New: adding handling via CTID.
        /// Start with lookup, and later full ....
        /// </summary>
        /// <param name="entityTypeId"></param>
        /// <param name="ctid"></param>
        /// <param name="actionTypeId"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public int Add( string ctid, int actionTypeId, ref List<String> messages )
        {
            if ( string.IsNullOrWhiteSpace(ctid ))
                return 0;
            var record = EntityManager.EntityCacheGetByCTID( ctid );
            if ( record != null )
            {
                ThisEntity entity = new ThisEntity() { EntityTypeId = record.EntityTypeId, RecordId = record.BaseId, IsUpdateOrDeleteTypeId = actionTypeId, StatusId = 1 };
                return Add( entity, ref messages );
            }
            else
            {
                //probably need a message
                return 0;
            }
        }
        public int AddDeleteRequest( int entityTypeId, int recordId, ref List<String> messages )
        {
            if (recordId < 1)
                return 0;

            ThisEntity entity = new ThisEntity() { EntityTypeId = entityTypeId, RecordId = recordId, IsUpdateOrDeleteTypeId = Reindex_Delete_Request, StatusId = 1 };
            return Add( entity, ref messages );
        }
        /// <summary>
        /// SearchPendingReindexes Add
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        private int Add( ThisEntity entity, ref List<String> messages )
        {
            DBEntity efEntity = new DBEntity();

            if ( !IsValid( entity, ref messages ) )
                return 0;
            try
            {
                using ( var context = new EntityContext() )
                {

                    //check if a pending record exists
                    DBEntity exists = context.SearchPendingReindex
                                .FirstOrDefault( s => s.EntityTypeId == entity.EntityTypeId && s.RecordId == entity.RecordId && s.StatusId == 1 );
                    if ( exists != null && exists.Id > 0 )
                    {
                        //could ignore,or check for a change of request type of add or delete
                        if ( exists.IsUpdateOrDeleteTypeId == entity.IsUpdateOrDeleteTypeId )
                            return exists.Id;
                        //otherwise do an update?
                        Update( entity, ref messages );
                        return exists.Id;
                    }

                    MapToDB( entity, efEntity );
                    efEntity.Created = System.DateTime.Now;
                    efEntity.LastUpdated = System.DateTime.Now;

                    context.SearchPendingReindex.Add( efEntity );

                    // submit the change to database
                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        entity.Id = efEntity.Id;

                        return efEntity.Id;
                    }
                    else
                    {
                        //?no info on error
                        messages.Add( "Error - the profile was not saved. " );
                        string message = string.Format( thisClassName + ".Add. Failed. The process appeared to not work, but was not an exception, so we have no message, or no clue. EntityTypeId: {0}, RecordId: {1}", entity.EntityTypeId, entity.RecordId );
                        // EmailManager.NotifyAdmin( thisClassName + ". Add Failed", message );
                    }
                }


            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(). EntityTypeId: {0}, RecordId: {1}", entity.EntityTypeId, entity.RecordId ) );
            }


            return efEntity.Id;
        }
        /// <summary>
        /// Update a Record
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public bool Update( ThisEntity entity, ref List<String> messages )
        {
            bool isValid = false;
            int count = 0;
            try
            {
                if ( !IsValid( entity, ref messages ) )
                    return false;

                using ( var context = new EntityContext() )
                {
                    DBEntity efEntity = context.SearchPendingReindex
                                .FirstOrDefault( s => s.Id == entity.Id );

                    if ( efEntity != null && efEntity.Id > 0 )
                    {
                        //for updates, chances are some fields will not be in interface, don't map these (ie created stuff)
                        MapToDB( entity, efEntity );
                        if ( HasStateChanged( context ) )
                        {
                            efEntity.LastUpdated = System.DateTime.Now;
                            
                            count = context.SaveChanges();
                            //can be zero if no data changed
                            if ( count >= 0 )
                            {
                                isValid = true;
                            }
                            else
                            {
                                //?no info on error
                                messages.Add( "Error - the update was not successful. ");
                                string message = string.Format( thisClassName + ".Update Failed. The process appeared to not work, but was not an exception, so we have no message, or no clue. EntityTypeId: {0}, RecordId: {1}", entity.EntityTypeId, entity.RecordId );
                                //EmailManager.NotifyAdmin( thisClassName + ". ConditionProfile_Update Failed", message );
                            }
                        }
                      
                    }
                    else
                    {
						messages.Add( "Error - update failed, as record was not found.");
                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".Update. EntityTypeId: {0}, RecordId: {1}", entity.EntityTypeId, entity.RecordId) );
            }


            return isValid;
        }

		/// <summary>
		/// Update SearchPendingReindex
		/// </summary>
		/// <param name="requestTypeId">1-update, 2-delete</param>
		/// <param name="messages"></param>
		/// <param name="entityTypeId"></param>
		/// <returns></returns>
		public bool UpdateAll( int requestTypeId, ref List<String> messages, int entityTypeId = 0 )
        {
            bool isValid = false;
            int count = 0;
            if ( requestTypeId < 1 || requestTypeId > 2 )
                return false;
            try
            {
                //could be a proc
                using ( var context = new EntityContext() )
                {
                    List<DBEntity> results = context.SearchPendingReindex
                                .Where
								( 
									s => s.StatusId == 1 
								&&	s.IsUpdateOrDeleteTypeId == requestTypeId 
								&& ( entityTypeId  == 0 || s.EntityTypeId == entityTypeId )
								)
								.ToList();
                    if ( results != null && results.Count > 0)
                    {
                        foreach ( var efEntity in results)
                        {
                            efEntity.StatusId = 2;
                            if ( HasStateChanged( context ) )
                            {
                                efEntity.LastUpdated = System.DateTime.Now;
                                count = context.SaveChanges();
                                //can be zero if no data changed
                                if ( count >= 0 )
                                {
                                    isValid = true;
                                }
                                else
                                {
									//?no info on error
                                    messages.Add( string.Format( thisClassName + ".Update Failed. The process appeared to not work, but was not an exception, so we have no message, or no clue. EntityTypeId: {0}, RecordId: {1}", efEntity.EntityTypeId, efEntity.RecordId ));
                                    //EmailManager.NotifyAdmin( thisClassName + ". ConditionProfile_Update Failed", message );
                                }
                            }
                        }
                    }

                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".UpdateAll. requestTypeId: {0}", requestTypeId ) );
            }


            return isValid;
        }

        public bool UpdateAll( int requestTypeId, ref List<String> messages, List<int> entityTypeIds )
        {
            bool isValid = false;
            int count = 0;
            if ( requestTypeId < 1 || requestTypeId > 2 )
                return false;
            try
            {
                //could be a proc
                using ( var context = new EntityContext() )
                {
                    List<DBEntity> results = context.SearchPendingReindex
                                .Where
                                (
                                    s => s.StatusId == 1
                                && s.IsUpdateOrDeleteTypeId == requestTypeId
                                && ( entityTypeIds.Contains( s.EntityTypeId ) )
                                )
                                .ToList();
                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( var efEntity in results )
                        {
                            efEntity.StatusId = 2;
                            if ( HasStateChanged( context ) )
                            {
                                efEntity.LastUpdated = System.DateTime.Now;
                                count = context.SaveChanges();
                                //can be zero if no data changed
                                if ( count >= 0 )
                                {
                                    isValid = true;
                                }
                                else
                                {
                                    //?no info on error
                                    messages.Add( string.Format( thisClassName + ".Update Failed. The process appeared to not work, but was not an exception, so we have no message, or no clue. EntityTypeId: {0}, RecordId: {1}", efEntity.EntityTypeId, efEntity.RecordId ) );
                                    //EmailManager.NotifyAdmin( thisClassName + ". ConditionProfile_Update Failed", message );
                                }
                            }
                        }
                    }

                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".UpdateAll. requestTypeId: {0}", requestTypeId ) );
            }


            return isValid;
        }

        public bool Delete( int Id, ref string statusMessage )
        {
            bool isValid = false;
            if ( Id == 0 )
            {
                statusMessage = "Error - missing an identifier for the ConditionProfile";
                return false;
            }
            using ( var context = new EntityContext() )
            {
                DBEntity efEntity = context.SearchPendingReindex
                            .SingleOrDefault( s => s.Id == Id );

                if ( efEntity != null && efEntity.Id > 0 )
                {
                    context.SearchPendingReindex.Remove( efEntity );
                    int count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        isValid = true;
                    }
                }
                else
                {
                    statusMessage = "Error - delete failed, as record was not found.";
                }
            }

            return isValid;
        }

        private bool IsValid( ThisEntity item, ref List<string> messages )
        {
            bool isValid = true;
            int initialCount = messages.Count;
            if ( item.EntityTypeId == 0 )
                messages.Add( "An entityTypeId is required" );
            if ( item.RecordId == 0 )
                messages.Add( "An entity recordId is required" );
            if ( item.StatusId == 0 )
                messages.Add( "A valid StatusId is required (1 = pending, 2 = complete)" );
            if (item.IsUpdateOrDeleteTypeId < 1 || item.IsUpdateOrDeleteTypeId > 2)
                messages.Add( "A valid IsUpdateOrDeleteTypeId is required (1 = update, 2 = delete" );
            if ( messages.Count > initialCount)
                isValid = false;

            return isValid;
        }
        #endregion

        #region == Retrieval =======================
        public static List<ThisEntity> GetAllPendingDeletes( )
        {
            List<ThisEntity> list = new List<ThisEntity>();
            ThisEntity entity = new ThisEntity();
            using ( var context = new EntityContext() )
            {

                List<DBEntity> results = context.SearchPendingReindex
                        .Where( s => s.IsUpdateOrDeleteTypeId == 2 && s.StatusId == 1 )
                        .OrderBy( s => s.EntityTypeId).ThenBy(s => s.Created)
                        .ToList();

                if ( results != null && results.Count > 0 )
                {
                    foreach ( var item in results )
                    {
                        entity = new ThisEntity();
                        MapFromDB( item, entity, true );
                        list.Add( entity );
                    }
                }
            }
            return list;
        }
    
        public static List<ThisEntity> GetAllPendingReindex( ref List<String> messages, int entityTypeId = 0 )
        {
            List<ThisEntity> list = new List<ThisEntity>();
            ThisEntity entity = new ThisEntity();
            using ( var context = new EntityContext() )
            {

                List<DBEntity> results = context.SearchPendingReindex
                        .Where( s => s.IsUpdateOrDeleteTypeId == 1 && s.StatusId == 1
							&& ( entityTypeId == 0 || s.EntityTypeId == entityTypeId ) 
							)
                        .OrderBy( s => s.EntityTypeId ).ThenBy( s => s.Created )
                        .ToList();

                if ( results != null && results.Count > 0 )
                {
                    foreach ( var item in results )
                    {
                        entity = new ThisEntity();
                        MapFromDB( item, entity, true );
                        list.Add( entity );
                    }
                }
            }

            return list;
        }
        public static List<ThisEntity> GetAllPendingReindex( ref List<String> messages, List<int> entityTypeIds )
        {
            List<ThisEntity> list = new List<ThisEntity>();
            if ( entityTypeIds?.Count == 0 )
                return list;

            ThisEntity entity = new ThisEntity();
            using ( var context = new EntityContext() )
            {

                List<DBEntity> results = context.SearchPendingReindex
                        .Where( s => s.IsUpdateOrDeleteTypeId == 1 && s.StatusId == 1
                            && ( entityTypeIds.Contains( s.EntityTypeId ) )
                            )
                        .OrderBy( s => s.EntityTypeId ).ThenBy( s => s.Created )
                        .ToList();

                if ( results != null && results.Count > 0 )
                {
                    foreach ( var item in results )
                    {
                        entity = new ThisEntity();
                        MapFromDB( item, entity, true );
                        list.Add( entity );
                    }
                }
            }

            return list;
        }
        public static ThisEntity Get( int id, bool includeProperties = false )
        {
            ThisEntity entity = new ThisEntity();
            using ( var context = new EntityContext() )
            {

                DBEntity item = context.SearchPendingReindex
                        .SingleOrDefault( s => s.Id == id );

                if ( item != null && item.Id > 0 )
                {
                    MapFromDB( item, entity, true );
                }
            }

            return entity;
        }

        public static void MapToDB( ThisEntity fromEntity, DBEntity to )
        {

            //want to ensure fields from create are not wiped
            if ( to.Id < 1 )
            {
            }

            to.Id = fromEntity.Id;
            to.EntityTypeId = fromEntity.EntityTypeId;
            to.RecordId = fromEntity.RecordId;
            to.StatusId = fromEntity.StatusId;
            to.IsUpdateOrDeleteTypeId = fromEntity.IsUpdateOrDeleteTypeId;

        }
        public static void MapFromDB( DBEntity fromEntity, ThisEntity to, bool includingProperties = false )
        {
            to.Id = fromEntity.Id;
            to.EntityTypeId = fromEntity.EntityTypeId;
            to.RecordId = fromEntity.RecordId;
            to.StatusId = fromEntity.StatusId;
            to.IsUpdateOrDeleteTypeId = (int)fromEntity.IsUpdateOrDeleteTypeId;

            if ( IsValidDate( fromEntity.Created ) )
                to.Created = ( DateTime ) fromEntity.Created;

            if ( IsValidDate( fromEntity.LastUpdated ) )
                to.LastUpdated = ( DateTime ) fromEntity.LastUpdated;



        }

        #endregion

    }
}
