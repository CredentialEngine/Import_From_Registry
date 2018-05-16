using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Microsoft.AspNet.Identity.Core;
//using Microsoft.AspNet.Identity;
//using Microsoft.AspNet.Identity.Owin;
//using Microsoft.Owin.Security;

using workIT.Models;
using workIT.Models.Search;

using workIT.Data.Tables;
using workIT.Utilities;
using Views = workIT.Data.Accounts;
using ViewContext = workIT.Data.Accounts.AccountEntities;
using EntityContext = workIT.Data.Accounts.AccountEntities;

using EM = workIT.Data.Accounts;

namespace workIT.Factories
{
	public class AccountManager : BaseFactory
	{
		static string thisClassName = "AccountManager";

		//static int Administrator = 1;
		//static int SiteManager = 2;
		//static int SiteStaff = 3;
		//static int SitePartner = 4;
		static int SiteReader = 5;
		static string SessionLoginProxy = "Session Login Proxy";
		#region persistance 
		public int Add( AppUser entity, ref string statusMessage )
		{
			EM.Account efEntity = new EM.Account();
			using ( var context = new EntityContext() )
			{
				try
				{

					AppUser_FromMap( entity, efEntity );
					efEntity.RowId = Guid.NewGuid();

					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;

					context.Accounts.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						statusMessage = "successful";
						entity.Id = efEntity.Id;

						return efEntity.Id;
					}
					else
					{
						//?no info on error
						statusMessage = "Error - the add was not successful. ";
						string message = string.Format( "AccountManager. Account_Add Failed", "Attempted to add a Account. The process appeared to not work, but was not an exception, so we have no message, or no clue.Email: {0}", entity.Email );
						EmailManager.NotifyAdmin( "	Manager. Account_Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					//LoggingHelper.LogError( dbex, thisClassName + string.Format( ".ContentAdd() DbEntityValidationException, Type:{0}", entity.TypeId ) );
					string message = thisClassName + string.Format( ".Account_Add() DbEntityValidationException, Email: {0}", efEntity.Email );
					foreach ( var eve in dbex.EntityValidationErrors )
					{
						message += string.Format( "\rEntity of type \"{0}\" in state \"{1}\" has the following validation errors:",
							eve.Entry.Entity.GetType().Name, eve.Entry.State );
						foreach ( var ve in eve.ValidationErrors )
						{
							message += string.Format( "- Property: \"{0}\", Error: \"{1}\"",
								ve.PropertyName, ve.ErrorMessage );
						}

						LoggingHelper.LogError( message, true );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Account_Add(), Email: {0}", efEntity.Email ) );
				}
			}

			return efEntity.Id;
		}

		public int Account_AddFromAspNetUser( string aspNetId, AppUser entity, ref string statusMessage )
		{
			EM.Account efEntity = new EM.Account();
			using ( var context = new EntityContext() )
			{
				try
				{
					EM.AspNetUser user = AspNetUser_Get( entity.Email );

					AppUser_FromMap( entity, efEntity );

					efEntity.RowId = Guid.NewGuid();

					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;

					context.Accounts.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						statusMessage = "successful";
						entity.Id = efEntity.Id;

						return efEntity.Id;
					}
					else
					{
						//?no info on error
						statusMessage = "Error - the add was not successful. ";
						string message = string.Format( "AccountManager. Account_Add Failed", "Attempted to add a Account. The process appeared to not work, but was not an exception, so we have no message, or no clue.Email: {0}", entity.Email );
						EmailManager.NotifyAdmin( "	Manager. Account_Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					//LoggingHelper.LogError( dbex, thisClassName + string.Format( ".ContentAdd() DbEntityValidationException, Type:{0}", entity.TypeId ) );
					string message = thisClassName + string.Format( ".Account_Add() DbEntityValidationException, Email: {0}", efEntity.Email );
					foreach ( var eve in dbex.EntityValidationErrors )
					{
						message += string.Format( "\rEntity of type \"{0}\" in state \"{1}\" has the following validation errors:",
							eve.Entry.Entity.GetType().Name, eve.Entry.State );
						foreach ( var ve in eve.ValidationErrors )
						{
							message += string.Format( "- Property: \"{0}\", Error: \"{1}\"",
								ve.PropertyName, ve.ErrorMessage );
						}

						LoggingHelper.LogError( message, true );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Account_Add(), Email: {0}", efEntity.Email ) );
				}
			}

			return efEntity.Id;
		}

		public bool Update( AppUser entity, ref string statusMessage )
		{
			using ( var context = new EntityContext() )
			{
				try
				{
					EM.Account efEntity = context.Accounts
							.SingleOrDefault( s => s.Id == entity.Id );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						AppUser_FromMap( entity, efEntity );
						context.Entry( efEntity ).State = System.Data.Entity.EntityState.Modified;

						if ( HasStateChanged( context ) )
						{
							efEntity.LastUpdated = System.DateTime.Now;

							// submit the change to database
							int count = context.SaveChanges();
							if ( count > 0 )
							{
								statusMessage = "successful";
								//arbitrarily update AspNetUsers???
								AspNetUsers_Update( entity, ref statusMessage );

								return true;
							}
							else
							{
								//?no info on error
								statusMessage = "Error - the update was not successful. ";
								string message = string.Format( thisClassName + ".Account_Update Failed", "Attempted to uddate a Account. The process appeared to not work, but was not an exception, so we have no message, or no clue.Email: {0}", entity.Email );
								EmailManager.NotifyAdmin( thisClassName + ". Account_Update Failed", message );
							}
						}
					}

				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Account_Update(), Email: {0}", entity.Email ) );
				}
			}

			return false;
		}

		public bool Delete( int userId, ref string statusMessage )
		{
			using ( var context = new EntityContext() )
			{
				try
				{
					EM.Account efEntity = context.Accounts
							.SingleOrDefault( s => s.Id == userId );
					if ( efEntity != null && efEntity.Id > 0 )
					{
						efEntity.IsActive = false;
						efEntity.LastUpdated = System.DateTime.Now;
						int count = context.SaveChanges();
						//add activity log (usually in caller method)

						//need to handle AspNetUsers
						AspNetUsers_LockOut( efEntity.AspNetId, ref statusMessage );
					}


				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Delete(), userId: {0}", userId ) );
				}
			}

			return false;
		}

		#endregion

		#region ====== AspNetUsers ======

		public static bool AspNetUsers_Update( AppUser entity, ref string statusMessage )
		{
			using ( var context = new EntityContext() )
			{
				try
				{
					EM.AspNetUser efEntity = context.AspNetUsers
							.SingleOrDefault( s => s.Id == entity.AspNetUserId );

					if ( efEntity != null && efEntity.UserId > 0 )
					{
						efEntity.FirstName = entity.FirstName;
						efEntity.LastName = entity.LastName;
						efEntity.Email = entity.Email;
						efEntity.EmailConfirmed = true;
						//could be dangerous, as hidden??
						//efEntity.UserName = entity.UserName;

						if ( HasStateChanged( context ) )
						{
							// submit the change to database
							int count = context.SaveChanges();
							if ( count > 0 )
							{
								statusMessage = "successful";
								return true;
							}
							else
							{
								//?no info on error
								statusMessage = "Error - the update was not successful. ";
								string message = string.Format( thisClassName + ".AspNetUsers_Update Failed", "Attempted to update AspNetUsers (sync with Account). The process appeared to not work, but was not an exception, so we have no message, or no clue.Email: {0}", entity.Email );
								EmailManager.NotifyAdmin( thisClassName + "AspNetUsers_Update Failed", message );
							}
						}

					}

				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".AspNetUsers_Update(), Email: {0}", entity.Email ) );
				}
			}

			return false;
		}
		public static bool AspNetUsers_LockOut( string aspNetUserId, ref string statusMessage )
		{
			using ( var context = new EntityContext() )
			{
				try
				{
					EM.AspNetUser efEntity = context.AspNetUsers
							.SingleOrDefault( s => s.Id == aspNetUserId );

					if ( efEntity != null && efEntity.UserId > 0 )
					{
						efEntity.LockoutEnabled = true;

						if ( HasStateChanged( context ) )
						{
							// submit the change to database
							int count = context.SaveChanges();
							if ( count > 0 )
							{
								statusMessage = "successful";
								return true;
							}
							else
							{
								//?no info on error
								statusMessage = "Error - the lockout was not successful. ";
								string message = string.Format( thisClassName + ".AspNetUsers_LockOut Failed", "Attempted to update AspNetUsers for lockout. The process appeared to not work, but was not an exception, so we have no message, or no clue.Email: {0}", efEntity.Email );
								EmailManager.NotifyAdmin( thisClassName + "AspNetUsers_LockOut Failed", message );
							}
						}

					}

				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".AspNetUsers_LockOut(), aspNetUserId: {0}", aspNetUserId ) );
				}
			}

			return false;
		}

		public bool AspNetUsers_UpdateEmailConfirmed( string id, ref string statusMessage )
		{
			using ( var context = new EntityContext() )
			{
				EM.AspNetUser efEntity = context.AspNetUsers
							.SingleOrDefault( s => s.Id == id );
				try
				{
					if ( efEntity != null && efEntity.UserId > 0 )
					{
						efEntity.EmailConfirmed = true;

						if ( HasStateChanged( context ) )
						{
							// submit the change to database
							int count = context.SaveChanges();
							if ( count > 0 )
							{
								statusMessage = "successful";
								return true;
							}
							else
							{
								//?no info on error
								statusMessage = "Error - the update of EmailConfirmed was not successful. ";
								string message = string.Format( thisClassName + ".AspNetUsers_Update Failed", "Attempted to update AspNetUsers.EmailConfirmed. The process appeared to not work, but was not an exception, so we have no message, or no clue.Email: {0}", efEntity.Email );
								EmailManager.NotifyAdmin( thisClassName + "AspNetUsers_UpdateEmailConfirmed Failed", message );
							}
						}
						return true;
					}

				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".AspNetUsers_UpdateEmailConfirmed(), Email: {0}", efEntity.Email ) );
				}
			}

			return false;
		}
		public bool AspNetUsers_UpdateEmailConfirmedByEmail( string email, ref string statusMessage )
		{
			using ( var context = new EntityContext() )
			{
				EM.AspNetUser efEntity = context.AspNetUsers
							.SingleOrDefault( s => s.Email == email );
				try
				{
					if ( efEntity != null && efEntity.UserId > 0 )
					{
						efEntity.EmailConfirmed = true;

						if ( HasStateChanged( context ) )
						{
							// submit the change to database
							int count = context.SaveChanges();
							if ( count > 0 )
							{
								statusMessage = "successful";
								return true;
							}
							else
							{
								//?no info on error
								statusMessage = "Error - the update of EmailConfirmed was not successful. ";
								string message = string.Format( thisClassName + ".AspNetUsers_UpdateEmailConfirmedByEmail Failed", "Attempted to update AspNetUsers.EmailConfirmed. The process appeared to not work, but was not an exception, so we have no message, or no clue.Email: {0}", efEntity.Email );
								EmailManager.NotifyAdmin( thisClassName + "AspNetUsers_UpdateEmailConfirmedByEmail Failed", message );
							}
						}
						return true;
					}

				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".AspNetUsers_UpdateEmailConfirmedByEmail(), Email: {0}", efEntity.Email ) );
				}
			}

			return false;
		}


		public static EM.AspNetUser AspNetUser_Get( string email )
		{
			EM.AspNetUser entity = new EM.AspNetUser();
			using ( var context = new EntityContext() )
			{
				entity = context.AspNetUsers
							.SingleOrDefault( s => s.Email.ToLower() == email.ToLower() );

				if ( entity != null && entity.Email != null && entity.Email.Length > 5 )
				{

				}
			}

			return entity;
		}

		#endregion

		#region retrieval
		/// <summary>
		/// Get user using View: Account_Summary
		/// </summary>
		/// <param name="Id"></param>
		/// <returns></returns>
		public static AppUser AppUser_Get( int Id )
		{
			AppUser entity = new AppUser();
			using ( var context = new ViewContext() )
			{
				Views.Account_Summary item = context.Account_Summary.SingleOrDefault( s => s.Id == Id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity );
				}
			}

			return entity;
		}
		public static AppUser Get( int Id )
		{
			AppUser entity = new AppUser();
			using ( var context = new EntityContext() )
			{
				//this method may be bringing back to much info - evaluate
				EM.Account item = context.Accounts
							.SingleOrDefault( s => s.Id == Id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity );
				}
			}

			return entity;
		}
		public static AppUser AppUser_GetByEmail( string email )
		{
			AppUser entity = new AppUser();
			using ( var context = new ViewContext() )
			{
				Views.Account_Summary efEntity = context.Account_Summary
							.FirstOrDefault( s => s.Email.ToLower() == email.ToLower() );

				if ( efEntity != null && efEntity.Id > 0 )
				{
					MapFromDB( efEntity, entity );
				}
			}

			return entity;
		}
		public static AppUser GetUserByUserName( string username )
		{
			AppUser entity = new AppUser();
			using ( var context = new ViewContext() )
			{
				Views.Account_Summary efEntity = context.Account_Summary
							.SingleOrDefault( s => s.UserName.ToLower() == username.ToLower() );

				if ( efEntity != null && efEntity.Id > 0 )
				{
					MapFromDB( efEntity, entity );
				}
			}

			return entity;
		}

		//public static List<Views.AspNetUserRoles_Summary> GetUserRoles( int userId )
		//{
		//	using ( var context = new ViewContext() )
		//	{
		//		return context.AspNetUserRoles_Summary.Where( x => x.Id == userId ).ToList();
		//	}
		//}

		public static AppUser AppUser_GetByKey( string aspNetId )
		{
			AppUser entity = new AppUser();
			using ( var context = new ViewContext() )
			{
				Views.Account_Summary efEntity = context.Account_Summary
							.SingleOrDefault( s => s.AspNetId == aspNetId );

				if ( efEntity != null && efEntity.Id > 0 )
				{
					MapFromDB( efEntity, entity );
				}
			}

			return entity;
		}
		public static AppUser AppUser_GetFromAspUser( string email )
		{
			AppUser entity = new AppUser();
			using ( var context = new EntityContext() )
			{
				EM.AspNetUser efEntity = context.AspNetUsers
							.SingleOrDefault( s => s.Email.ToLower() == email.ToLower() );

				if ( efEntity != null && efEntity.Email != null && efEntity.Email.Length > 5 )
				{
					entity = AppUser_GetByEmail( efEntity.Email );
				}
			}

			return entity;
		}

		public static List<AppUser> Search( string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows, int userId = 0 )
		{
			string connectionString = DBConnectionRO();
			AppUser item = new AppUser();
			List<AppUser> list = new List<AppUser>();
			var result = new DataTable();
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				if ( string.IsNullOrEmpty( pFilter ) )
				{
					pFilter = "";
				}

				using ( SqlCommand command = new SqlCommand( "AccountSearch", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", pFilter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", pOrderBy ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize == -1 ? 0 : pageSize ) );
					command.Parameters.Add( new SqlParameter( "@CurrentUserId", userId ) );

					SqlParameter totalRows = new SqlParameter( "@TotalRows", pTotalRows );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					using ( SqlDataAdapter adapter = new SqlDataAdapter() )
					{
						adapter.SelectCommand = command;
						adapter.Fill( result );
					}
					string rows = command.Parameters[ 5 ].Value.ToString();
					try
					{
						pTotalRows = Int32.Parse( rows );
					}
					catch
					{
						pTotalRows = 0;
					}
				}

				foreach ( DataRow dr in result.Rows )
				{
					item = new AppUser();
					item.Id = GetRowColumn( dr, "Id", 0 );
					item.FirstName = GetRowColumn( dr, "FirstName", "missing" );
					item.LastName = GetRowColumn( dr, "LastName", "" );
					string rowId = GetRowColumn( dr, "RowId" );
					item.RowId = new Guid( rowId );

					item.Email = GetRowColumn( dr, "Email", "" );
					item.Roles = GetRowColumn( dr, "Roles", "" );
					item.OrgMbrs = GetRowColumn( dr, "OrgMbrs", "" );
                    item.lastLogon = GetRowColumn( dr, "lastLogon", "" );
					if ( IsValidDate( item.lastLogon ) )
						item.lastLogon = item.lastLogon.Substring( 0, 10 );

					list.Add( item );
				}

				return list;

			}
		}


		//public void GetAllUsersInRole( string role )
		//{
		//	using ( var context = new EntityContext() )
		//	{
		//		var customers = context.AspNetUsers
		//			  .Where( u => u.AspNetUserRoles.Any( r => r..Name == role )  )
		//			  .ToList();
		//	}
		//}
		public static void MapFromDB( Views.Account_Summary fromEntity, AppUser to )
		{
			to.Id = fromEntity.Id;
			//to.RowId = fromEntity.RowId;
			to.UserName = fromEntity.UserName;

			to.AspNetUserId = fromEntity.AspNetId;
			//to.Password = fromEntity.Password;
			to.IsActive = fromEntity.IsActive == null ? false : ( bool ) fromEntity.IsActive;
			to.FirstName = fromEntity.FirstName;
			to.LastName = fromEntity.LastName;
			to.SortName = fromEntity.SortName;

			to.Email = fromEntity.Email;
			to.Created = fromEntity.Created;
			to.LastUpdated = fromEntity.LastUpdated;
			to.LastUpdatedById = fromEntity.LastUpdatedById == null ? 0 : ( int ) fromEntity.LastUpdatedById;
			if ( IsValidDate( fromEntity.lastLogon ) )
			{
				to.lastLogon = ( ( DateTime ) fromEntity.lastLogon ).ToString( "yyyy-MM-dd" );
			}
			else
				to.lastLogon = "None";

			to.UserRoles = new List<string>();
			if ( string.IsNullOrWhiteSpace( fromEntity.Roles ) == false )
			{
				var roles = fromEntity.Roles.Split( ',' );
				foreach ( string role in roles )
				{
					to.UserRoles.Add( role );
				}
			}

		} //
		public static void MapFromDB( EM.Account fromEntity, AppUser to )
		{
			to.Id = fromEntity.Id;
			//to.RowId = fromEntity.RowId;
			to.UserName = fromEntity.UserName;

			to.AspNetUserId = fromEntity.AspNetId;
			//to.Password = fromEntity.Password;
			to.IsActive = fromEntity.IsActive == null ? false : ( bool ) fromEntity.IsActive;
			to.FirstName = fromEntity.FirstName;
			to.LastName = fromEntity.LastName;

			to.Email = fromEntity.Email;
			to.Created = fromEntity.Created;
			to.LastUpdated = fromEntity.LastUpdated;
			to.LastUpdatedById = fromEntity.LastUpdatedById == null ? 0 : ( int ) fromEntity.LastUpdatedById;

			to.UserRoles = new List<string>();
			if ( fromEntity.AspNetUser != null )
			{
				foreach ( EM.AspNetUserRole role in fromEntity.AspNetUser.AspNetUserRoles )
				{
					to.UserRoles.Add( role.AspNetRole.Name );
				}
			}

			if ( fromEntity.Organization_Member != null && fromEntity.Organization_Member.Count > 0 )
			{
				foreach ( EM.Organization_Member mbrs in fromEntity.Organization_Member )
				{
					//just a light version for now
					to.Organizations.Add( new CodeItem()
					{
						Id = mbrs.Organization.Id,
						Name = mbrs.Organization.Name
					} );
				}
			}

		} //
		  //NOTE: AspNetRoles is to be a guid, so not likely to use this version
		//public void GetAllUsersInRole( int roleId )
		//{
		//	using ( var context = new EntityContext() )
		//	{
		//		var customers = context.AspNetUsers
		//			  .Where( u => u.AspNetUserRoles.Any( r => r.RoleId == roleId ) )
		//			  .ToList();
		//	}
		//}
		private static void AppUser_FromMap( AppUser fromEntity, EM.Account to )
		{
			to.Id = fromEntity.Id;
			//to.RowId = fromEntity.RowId;
			to.UserName = fromEntity.UserName;
			to.AspNetId = fromEntity.AspNetUserId;
			to.Password = fromEntity.Password;
			to.IsActive = fromEntity.IsActive;

			to.FirstName = fromEntity.FirstName;
			to.LastName = fromEntity.LastName;
			to.Email = fromEntity.Email;

			to.Created = fromEntity.Created;
			to.LastUpdated = fromEntity.LastUpdated;
			to.LastUpdatedById = fromEntity.LastUpdatedById;

		}

		#endregion

		#region Proxies

		public bool Store_ProxyCode( string proxyCode, int userId, string proxyType, int expiryDays, ref string statusMessage )
		{
			bool isValid = true;
			EM.System_ProxyCodes efEntity = new EM.System_ProxyCodes();
			//string proxyId = "";
			try
			{
				using ( var context = new EntityContext() )
				{
					efEntity.UserId = userId;
					efEntity.ProxyCode = proxyCode;
					//assuming if storing existing, likely for identity
					efEntity.IsIdentityProxy = true;
					efEntity.Created = System.DateTime.Now;
					efEntity.ExpiryDate = System.DateTime.Now.AddDays( expiryDays );

					efEntity.IsActive = true;
					efEntity.ProxyType = proxyType;

					context.System_ProxyCodes.Add( efEntity );

					int count = context.SaveChanges();
					if ( count > 0 )
					{
						statusMessage = "Successful";
						int id = efEntity.Id;

					}
					else
					{
						//?no info on error
						return false;
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Store_ProxyCode()" );
				statusMessage = ex.Message;
				return false;
			}
			return isValid;
		}

		/// <summary>
		/// Create a proxy guid for requested purpose
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="proxyType"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public string Create_ProxyLoginId( int userId, string proxyType, int expiryDays, ref string statusMessage )
		{
			EM.System_ProxyCodes efEntity = new EM.System_ProxyCodes();
			string proxyId = "";
			try
			{
				using ( var context = new EntityContext() )
				{
					efEntity.UserId = userId;
					efEntity.ProxyCode = Guid.NewGuid().ToString();
					//assuming if generated, not for identity
					efEntity.IsIdentityProxy = false;
					efEntity.Created = System.DateTime.Now;
					if ( proxyType == SessionLoginProxy )
					{
						//expire at midnight - not really good for night owls
						//efEntity.ExpiryDate = new System.DateTime( DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59 );
						efEntity.ExpiryDate = System.DateTime.Now.AddDays( expiryDays );
					}
					else
						efEntity.ExpiryDate = System.DateTime.Now.AddDays( expiryDays );

					efEntity.IsActive = true;
					efEntity.ProxyType = proxyType;

					context.System_ProxyCodes.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						statusMessage = "Successful";
						int id = efEntity.Id;
						return efEntity.ProxyCode;
					}
					else
					{
						//?no info on error
						return proxyId;
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Create_Proxy()" );
				statusMessage = ex.Message;
				return proxyId;
			}
		}

		public bool Proxy_IsCodeActive( string proxyCode )
		{
			bool isValid = false;
			using ( var context = new EntityContext() )
			{

				EM.System_ProxyCodes proxy = context.System_ProxyCodes.FirstOrDefault( s => s.ProxyCode == proxyCode );
				if ( proxy != null && proxy.Id > 0 )
				{
					if ( proxy.IsActive
						&& proxy.ExpiryDate > DateTime.Now )
						isValid = true;
				}
			}

			return isValid;

		}
		public bool InactivateProxy( string proxyCode, ref string statusMessage )
		{
			bool isValid = true;
			using ( var context = new EntityContext() )
			{

				EM.System_ProxyCodes proxy = context.System_ProxyCodes.FirstOrDefault( s => s.ProxyCode == proxyCode );
				if ( proxy != null && proxy.Id > 0 )
				{
					proxy.IsActive = false;
					proxy.AccessDate = System.DateTime.Now;

					context.SaveChanges();
				}
			}

			return isValid;

		}
		#endregion

		#region Roles
		public bool AddRole( int userId, int roleId, int createdByUserId, ref string statusMessage )
		{
			bool isValid = true;
			string aspNetUserId = "";
			if ( userId == 0 )
			{
				statusMessage = "Error - please provide a valid user";
				return false;
			}
			if ( roleId < 1 || roleId > SiteReader )
			{
				statusMessage = "Error - please provide a valid role identifier";
				return false;
			}

			AppUser user = AppUser_Get( userId );
			if ( user != null && user.Id > 0 )
				aspNetUserId = user.AspNetUserId;

			if ( !IsValidGuid( aspNetUserId ) )
			{
				statusMessage = "Error - please provide a valid user identifier";
				return false;
			}

			EM.AspNetUserRole efEntity = new EM.AspNetUserRole();
			using ( var context = new EntityContext() )
			{
				try
				{
					efEntity.UserId = aspNetUserId;
					efEntity.RoleId = roleId.ToString();
					efEntity.Created = System.DateTime.Now;

					context.AspNetUserRoles.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						statusMessage = "successful";
						//other, maybe notification
					}
					else
					{
						//?no info on error
						statusMessage = "Error - the Account_AddRole was not successful. ";
						string message = string.Format( "AccountManager. Account_AddRole Failed", "Attempted to add an Account_AddRole. The process appeared to not work, but was not an exception, so we have no message, or no clue. Email: {0}, roleId {1}, requestedBy: {2}", user.Email, roleId, createdByUserId );
						EmailManager.NotifyAdmin( "	Manager. Account_AddRole Failed", message );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Account_AddRole(), Email: {0}", user.Email ) );
					statusMessage = ex.Message;
					isValid = false;
				}
			}

			return isValid;
		}


		public bool DeleteRole( AppUser entity, int roleId, int updatedByUserId, ref string statusMessage )
		{
			bool isValid = true;

			if ( entity == null || !IsValidGuid( entity.AspNetUserId ) )
			{
				statusMessage = "Error - please provide a value user identifier";
				return false;
			}
			if ( roleId < 1 || roleId > SiteReader )
			{
				statusMessage = "Error - please provide a value role identifier";
				return false;
			}

			using ( var context = new EntityContext() )
			{
				try
				{
					EM.AspNetUserRole efEntity = context.AspNetUserRoles
							.SingleOrDefault( s => s.UserId == entity.AspNetUserId && s.RoleId == roleId.ToString() );

					if ( efEntity != null && !string.IsNullOrWhiteSpace( efEntity.RoleId ) )
					{
						context.AspNetUserRoles.Remove( efEntity );
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							isValid = true;
							//TODO - add logging here or in the services
						}
					}
					else
					{
						statusMessage = "Error - delete failed, as record was not found.";
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Account_DeleteRole(), Email: {0}", entity.Email ) );
					statusMessage = ex.Message;
					isValid = false;
				}
			}

			return isValid;
		}

		public void UpdateRoles( string aspNetUserId, string[] roles )
		{
			using ( var db = new EntityContext() )
			{
				try
				{
					var existRoles = db.AspNetUserRoles.Where( x => x.UserId == aspNetUserId.ToString() );
					var oldRoles = existRoles.Select( x => x.RoleId ).ToArray();

					if ( roles == null )
						roles = new string[] { };

					//Add New Roles Selected
					roles.Except( oldRoles ).ToList().ForEach( x =>
					{
						var userRole = new EM.AspNetUserRole { UserId = aspNetUserId, RoleId = x, Created = DateTime.Now };
						db.Entry( userRole ).State = System.Data.Entity.EntityState.Added;
					} );

					//Delete existing Roles unselected
					existRoles.Where( x => !roles.Contains( x.RoleId ) ).ToList().ForEach( x =>
					{
						db.Entry( x ).State = System.Data.Entity.EntityState.Deleted;
					} );

					db.SaveChanges();
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".UpdateRoles(), aspNetUserId: {0}", aspNetUserId ) );
					//statusMessage = ex.Message;

				}
			}
		}

		public static List<EM.AspNetRole> GetRoles()
		{
			using ( var context = new EntityContext() )
			{
				return context.AspNetRoles.Where(s => s.IsActive == true).ToList();
			}
		}


		#endregion
		public static bool HasStateChanged( EntityContext context )
		{
			if ( context.ChangeTracker.Entries().Any( e =>
					e.State == EntityState.Added ||
					e.State == EntityState.Modified ||
					e.State == EntityState.Deleted ) == true )
				return true;
			else
				return false;
		}

	}
}
