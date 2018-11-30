using System;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;

namespace workIT.Factories
{
	public class CacheManager : BaseFactory
	{
        /// <summary>
        /// Populate/rebuild all caches
        /// The rebuild all is slower and should not be done during day for production
        /// </summary>
        /// <param name="doingAll">False: only updates where entities have changed, True: rebuild all</param>
        public void PopulateAllCaches( bool doingAll)
        {
            string connectionString = MainConnection();
            try
            {
                using ( SqlConnection c = new SqlConnection( connectionString ) )
                {
                    c.Open();
                    int populateType = -1; //updates only, where handled
                    if ( doingAll )
                        populateType = 0;

                    using ( SqlCommand command = new SqlCommand( "[Populate_AllCaches]", c ) )
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add( new SqlParameter( "@PopulateType", populateType ) );
                        command.CommandTimeout = 300;
                        command.ExecuteNonQuery();
                        command.Dispose();
                        c.Close();

                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, "PopulateAllCaches", false );

            }

        }

        //public void PopulateEntitySearchCache( int entityId )
        //{

        //	string connectionString = MainConnection();
        //	using ( SqlConnection c = new SqlConnection( connectionString ) )
        //	{
        //		c.Open();

        //		using ( SqlCommand command = new SqlCommand( "[Populate_Entity_SearchIndex]", c ) )
        //		{
        //			command.CommandType = CommandType.StoredProcedure;
        //			command.Parameters.Add( new SqlParameter( "@EntityId", entityId ) );

        //			command.ExecuteNonQuery();
        //			command.Dispose();
        //			c.Close();

        //		}
        //	}
        //}
        public void PopulateEntityRelatedCaches( Guid entityUid )
		{
			Entity e = EntityManager.GetEntity( entityUid );
			if ( e == null || e.Id == 0 )
				return;


			string connectionString = MainConnection();
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				using ( SqlCommand command = new SqlCommand( "[Entity_Cache_Populate]", c ) )
				{
					c.Open();
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@EntityId", e.Id ) );
                    command.CommandTimeout = 300;
                    command.ExecuteNonQuery();
					command.Dispose();
					c.Close();

				}

				using ( SqlCommand command = new SqlCommand( "[Populate_Entity_SearchIndex]", c ) )
				{
					c.Open();
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@EntityId", e.Id ) );
                    command.CommandTimeout = 300;
                    command.ExecuteNonQuery();
					command.Dispose();
					c.Close();

				}
			}

			if ( e.EntityTypeId == 1 )
			{
				using ( SqlConnection c = new SqlConnection( connectionString ) )
				{
					c.Open();

					//using ( SqlCommand command = new SqlCommand( "[Credential.SummaryCache]", c ) )
					using ( SqlCommand command = new SqlCommand( "[Populate_Credential_SummaryCache]", c ) )
					{
						command.CommandType = CommandType.StoredProcedure;
						command.Parameters.Add( new SqlParameter( "@CredentialId", e.EntityBaseId ) );
                        command.CommandTimeout = 300;
                        command.ExecuteNonQuery();
						command.Dispose();
						c.Close();

					}
				}

				using ( SqlConnection c = new SqlConnection( connectionString ) )
				{
					c.Open();

					using ( SqlCommand command = new SqlCommand( "[Populate_Competencies_cache]", c ) )
					{
						command.CommandType = CommandType.StoredProcedure;
						command.Parameters.Add( new SqlParameter( "@CredentialId", e.EntityBaseId ) );
                        command.CommandTimeout = 300;
                        command.ExecuteNonQuery();
						command.Dispose();
						c.Close();

					}
				}
			}

			if ( e.EntityTypeId == 2 )
			{
				PopulateOrgRelatedCaches( e.EntityBaseId );
			}
		}//

		/// <summary>
		/// Call with orgId = 0 to update all
		/// Could do after completing an import
		/// </summary>
		/// <param name="orgId"></param>
		public void PopulateOrgRelatedCaches( int orgId )
		{
			string connectionString = MainConnection();
			try
			{
				//running this after add/update of an org will only be partly complete. 
				//would need to run it after any of cred, asmt, and lopp
				using ( SqlConnection c = new SqlConnection( connectionString ) )
				{
					c.Open();

					using ( SqlCommand command = new SqlCommand( "[Populate_Cache.Organization_ActorRoles]", c ) )
					{
						command.CommandType = CommandType.StoredProcedure;
						command.Parameters.Add( new SqlParameter( "@OrganizationId", orgId ) );
                        command.CommandTimeout = 300;
                        command.ExecuteNonQuery();
						command.Dispose();
						c.Close();


					}
				}
			} catch (Exception ex )
			{
				LoggingHelper.LogError( ex, "PopulateOrgRelatedCaches", false );

			}

		}

        public void UpdateCodeTableCounts()
        {
            string connectionString = MainConnection();
            try
            {
                //running this after add/update of an org will only be partly complete. 
                //would need to run it after any of cred, asmt, and lopp
                using ( SqlConnection c = new SqlConnection( connectionString ) )
                {
                    c.Open();

                    using ( SqlCommand command = new SqlCommand( "[CodeTables_UpdateTotals]", c ) )
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 300;
                        command.ExecuteNonQuery();
                        command.Dispose();
                        c.Close();
                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, "UpdateCodeTableCounts", false );

            }

        }
        #region credentials 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="key">The key could vary if for detail, compare, etc</param>
        /// <param name="?"></param>
        /// <returns></returns>
        public static bool IsCredentialAvailableFromCache( int id, string key, ref Credential credential )
		{
			
			int cacheMinutes = UtilityManager.GetAppKeyValue( "credentialCacheMinutes", 60 );
			DateTime maxTime = DateTime.Now.AddMinutes( cacheMinutes * -1 );

			//string key = "credential_" + id.ToString();

			if ( HttpRuntime.Cache[ key ] != null && cacheMinutes > 0 )
			{
				var cache = ( CachedCredential ) HttpRuntime.Cache[ key ];
				try
				{
					if ( cache.lastUpdated > maxTime )
					{
						LoggingHelper.DoTrace( 7, string.Format( "===CacheManager.IsCredentialAvailableFromCache === Using cached version of Credential, Id: {0}, {1}, key: {2}", cache.Item.Id, cache.Item.Name, key ) );

						//check if user can update the object
						//or move these checks to the manager
						//string status = "";
						//if ( !CanUserUpdateCredential( id, user, ref status ) )
						//	cache.Item.CanEditRecord = false;

						credential = cache.Item;
						return true;
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 6, "===CacheManager.IsCredentialAvailableFromCache === exception " + ex.Message );
				}
			}
			
			return false;
		}
		public static void AddCredentialToCache( Credential entity, string key )
		{
			int cacheMinutes = UtilityManager.GetAppKeyValue( "credentialCacheMinutes", 60 );

			//string key = "credential_" + entity.Id.ToString();

			if ( cacheMinutes > 0  )
			{
				var newCache = new CachedCredential()
				{
					Item = entity,
					lastUpdated = DateTime.Now
				};
				if ( HttpContext.Current != null )
				{
					if ( HttpContext.Current.Cache[ key ] != null )
					{
						HttpRuntime.Cache.Remove( key );
						HttpRuntime.Cache.Insert( key, newCache );

						LoggingHelper.DoTrace( 7, string.Format( "===CacheManager.AddCredentialToCache $$$ Updating cached version of credential, Id: {0}, {1}, key: {2}", entity.Id, entity.Name, key ) );

					}
					else
					{
						LoggingHelper.DoTrace( 7, string.Format( "===CacheManager.AddCredentialToCache ****** Inserting new cached version of credential, Id: {0}, {1}, key: {2}", entity.Id, entity.Name, key ) );

						System.Web.HttpRuntime.Cache.Insert( key, newCache, null, DateTime.Now.AddHours( cacheMinutes ), TimeSpan.Zero );
					}
				}
			}

			//return entity;
		}
		#endregion 

		#region LearningOpportunitys
		public static bool IsLearningOpportunityAvailableFromCache( int id, ref LearningOpportunityProfile entity )
		{
			int cacheMinutes = UtilityManager.GetAppKeyValue( "learningOppCacheMinutes", 0 );
			DateTime maxTime = DateTime.Now.AddMinutes( cacheMinutes * -1 );

			string key = "LearningOpportunity_" + id.ToString();

			if ( HttpRuntime.Cache[ key ] != null && cacheMinutes > 0 )
			{
				var cache = ( CachedLearningOpportunity ) HttpRuntime.Cache[ key ];
				try
				{
					if ( cache.lastUpdated > maxTime )
					{
						LoggingHelper.DoTrace( 7, string.Format( "%%%CacheManager.IsLearningOpportunityAvailableFromCache === Using cached version of LearningOpportunity, Id: {0}, {1}", cache.Item.Id, cache.Item.Name ) );

						//check if user can update the object
						//string status = "";
						//if ( !CanUserUpdateCredential( id, user, ref status ) )
						//	cache.Item.CanEditRecord = false;

						entity = cache.Item;
						return true;
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 6, "%%%CacheManager.IsLearningOpportunityAvailableFromCache === exception " + ex.Message );
				}
			}

			return false;
		}
		public static void AddLearningOpportunityToCache( LearningOpportunityProfile entity )
		{
			int cacheMinutes = UtilityManager.GetAppKeyValue( "learningOppCacheMinutes", 0 );

			string key = "LearningOpportunity_" + entity.Id.ToString();

			if ( cacheMinutes > 0 )
			{
				var newCache = new CachedLearningOpportunity()
				{
					Item = entity,
					lastUpdated = DateTime.Now
				};
				if ( HttpContext.Current != null )
				{
					if ( HttpContext.Current.Cache[ key ] != null )
					{
						HttpRuntime.Cache.Remove( key );
						HttpRuntime.Cache.Insert( key, newCache );

						LoggingHelper.DoTrace( 7, string.Format( "%%%CacheManager.AddLearningOpportunityToCache $$$ Updating cached version of LearningOpportunity, Id: {0}, {1}", entity.Id, entity.Name ) );

					}
					else
					{
						LoggingHelper.DoTrace( 6, string.Format( "%%%CacheManager.AddLearningOpportunityToCache ****** Inserting new cached version of LearningOpportunity, Id: {0}, {1}", entity.Id, entity.Name ) );

						System.Web.HttpRuntime.Cache.Insert( key, newCache, null, DateTime.Now.AddHours( cacheMinutes ), TimeSpan.Zero );
					}
				}
			}

			//return entity;
		}
		#endregion 
		#region ConditionProfiles
		public static bool IsConditionProfileAvailableFromCache( int id, ref ConditionProfile entity )
		{
			//use same as lopp for now
			int cacheMinutes = UtilityManager.GetAppKeyValue( "learningOppCacheMinutes", 0 );
			DateTime maxTime = DateTime.Now.AddMinutes( cacheMinutes * -1 );

			string key = "ConditionProfile_" + id.ToString();

			if ( HttpRuntime.Cache[ key ] != null && cacheMinutes > 0 )
			{
				var cache = ( CachedConditionProfile ) HttpRuntime.Cache[ key ];
				try
				{
					if ( cache.lastUpdated > maxTime )
					{
						LoggingHelper.DoTrace( 7, string.Format( "===CacheManager.IsConditionProfileAvailableFromCache === Using cached version of ConditionProfile, Id: {0}, {1}", cache.Item.Id, cache.Item.ProfileName ) );

						//check if user can update the object
						//string status = "";
						//if ( !CanUserUpdateCredential( id, user, ref status ) )
						//	cache.Item.CanEditRecord = false;

						entity = cache.Item;
						return true;
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 6, "===CacheManager.IsConditionProfileAvailableFromCache === exception " + ex.Message );
				}
			}

			return false;
		}
		public static void AddConditionProfileToCache( ConditionProfile entity )
		{
			int cacheMinutes = UtilityManager.GetAppKeyValue( "learningOppCacheMinutes", 0 );

			string key = "ConditionProfile_" + entity.Id.ToString();

			if ( cacheMinutes > 0 )
			{
				var newCache = new CachedConditionProfile()
				{
					Item = entity,
					lastUpdated = DateTime.Now
				};
				if ( HttpContext.Current != null )
				{
					if ( HttpContext.Current.Cache[ key ] != null )
					{
						HttpRuntime.Cache.Remove( key );
						HttpRuntime.Cache.Insert( key, newCache );

						LoggingHelper.DoTrace( 7, string.Format( "===CacheManager.AddConditionProfileToCache $$$ Updating cached version of ConditionProfile, Id: {0}, {1}", entity.Id, entity.ProfileName ) );

					}
					else
					{
						LoggingHelper.DoTrace( 6, string.Format( "===CacheManager.AddConditionProfileToCache ****** Inserting new cached version of ConditionProfile, Id: {0}, {1}", entity.Id, entity.ProfileName ) );

						System.Web.HttpRuntime.Cache.Insert( key, newCache, null, DateTime.Now.AddHours( cacheMinutes ), TimeSpan.Zero );
					}
				}
			}

			//return entity;
		}
		#endregion 

		public static void RemoveItemFromCache( string type, int id )
		{
			string key = string.Format( "{0}_{1}", type, id );
			if ( HttpContext.Current != null
				&& HttpContext.Current.Cache[ key ] != null )
			{
				HttpRuntime.Cache.Remove( key );

				LoggingHelper.DoTrace( 7, string.Format( "===CacheManager.RemoveFromCache $$$ Removed cached version of a {0}, Id: {1}", type, id ) );

			}
		}
	}

	public class CachedItem
	{
		public CachedItem()
		{
			lastUpdated = DateTime.Now;
		}
		public DateTime lastUpdated { get; set; }

	}
	public class CachedCredential : CachedItem
	{
		public Credential Item { get; set; }

	}
	public class CachedLearningOpportunity : CachedItem
	{
		public LearningOpportunityProfile Item { get; set; }

	}
	public class CachedConditionProfile : CachedItem
	{
		public ConditionProfile Item { get; set; }

	}
}
