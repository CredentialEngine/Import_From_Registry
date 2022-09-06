using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

using workIT.Models.Search;
using workIT.Utilities;
using DBEntity = workIT.Data.Tables.MessageLog;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisEntity = workIT.Models.MessageLog;

namespace workIT.Factories
{
	public class MessageManager : BaseFactory
	{
		private static string thisClassName = "MessageManager";


		#region Persistance
		public int Add( ThisEntity entity )
		{
			int count = 0;
			string truncateMsg = "";
			bool isBot = false;
			string server = UtilityManager.GetAppKeyValue( "serverName", "" );

			string agent = GetUserAgent( ref isBot );
			DBEntity efEntity = new DBEntity();
			MapToDB( entity, efEntity );

			//================================
			if ( IsADuplicateRequest( efEntity.Description ) )
				return 0;

			StoreLastRequest( efEntity.Description );

			//----------------------------------------------

			if ( efEntity.RelatedUrl != null && efEntity.RelatedUrl.Length > 600 )
			{
				truncateMsg += string.Format( "RelatedUrl overflow: {0}; ", efEntity.RelatedUrl.Length );
				efEntity.RelatedUrl = efEntity.RelatedUrl.Substring( 0, 600 );
			}


			//the following should not be necessary but getting null related exceptions
			if ( efEntity.ActionByUserId == null )
				efEntity.ActionByUserId = 0;


			using ( var context = new EntityContext() )
			{
				try
				{
					efEntity.Created = System.DateTime.Now;
					if ( efEntity.MessageType == null || efEntity.MessageType.Length < 3 )
						efEntity.MessageType = "Audit";

					context.MessageLog.Add( efEntity );

					// submit the change to database
					count = context.SaveChanges();

					if ( truncateMsg.Length > 0 )
					{
						string msg = string.Format( "MessageId: {0}, Message: {1}", efEntity.Id, truncateMsg );

						EmailManager.NotifyAdmin( "MessageLog Field Overflow", msg );
					}
					if ( count > 0 )
					{
						return efEntity.Id;
					}
					else
					{
						//?no info on error
						return 0;
					}
				}
				catch ( Exception ex )
				{

					LoggingHelper.LogError( ex, thisClassName + ".MessageLogAdd(EFDAL.MessageLog) ==> SHOULD ADD RETRY via proc\n\r" + ex.StackTrace.ToString() );
					//call stored proc as backup!

					return count;
				}
			}
		} //
		private void MapToDB( ThisEntity input, DBEntity output )
		{
			output.Id = input.Id;

			output.Application = input.Application;
			output.Activity = input.Activity;
			output.MessageType = input.MessageType;
			output.Message = input.Message;
			output.Description = input.Description;

			output.ActionByUserId = input.ActionByUserId;
			output.ActivityObjectId = input.ActivityObjectId;
			output.Tags = input.Tags;
			
			output.RelatedUrl = input.RelatedUrl;
			output.SessionId = input.SessionId;
			output.IPAddress = input.IPAddress;

			if ( output.SessionId == null || output.SessionId.Length < 10 )
				output.SessionId = GetCurrentSessionId();

			if ( output.IPAddress.Length > 50 )
				output.IPAddress = output.IPAddress.Substring( 0, 50 );
		}
		private static void MapFromDB( DBEntity input, ThisEntity output )
		{
			output.Id = input.Id;
			output.Created = input.Created;

			output.Application = input.Application;
			output.Activity = input.Activity;
			output.MessageType = input.MessageType;
			output.Message = input.Message;
			output.Description = input.Description;

			output.ActionByUserId = input.ActionByUserId;
			output.ActivityObjectId = input.ActivityObjectId;
			output.Tags = input.Tags;

			output.RelatedUrl = input.RelatedUrl;
			output.SessionId = input.SessionId;
			output.IPAddress = input.IPAddress;

		}

		#endregion


		#region helpers

		public static void StoreLastRequest( string actionDescription )
		{
			string sessionKey = GetCurrentSessionId() + "_LastError";
			try
			{
				if ( HttpContext.Current != null )
				{
					if ( HttpContext.Current.Session != null )
					{
						HttpContext.Current.Session[ sessionKey ] = actionDescription;
					}
				}
			}
			catch
			{
			}

		} //

		public static bool IsADuplicateRequest( string actionDescription )
		{
			string sessionKey = GetCurrentSessionId() + "_LastError";
			bool isDup = false;
			try
			{
				if ( HttpContext.Current != null )
				{
					if ( HttpContext.Current.Session != null )
					{
						string lastAction = HttpContext.Current.Session[ sessionKey ].ToString();
						if ( lastAction.ToLower() == actionDescription.ToLower() )
						{
							LoggingHelper.DoTrace( 7, "MessageServices. Duplicate action: " + actionDescription );
							return true;
						}
					}
				}

			}
			catch
			{

			}
			return isDup;
		}
		public static string GetCurrentSessionId()
		{
			string sessionId = "unknown";

			try
			{
				if ( HttpContext.Current != null )
				{
					if ( HttpContext.Current.Session != null )
					{
						sessionId = HttpContext.Current.Session.SessionID;
					}
				}

			}
			catch
			{
			}
			return sessionId;
		}

		public static string GetUserIPAddress()
		{
			string ip = "unknown";
			try
			{
				if ( HttpContext.Current != null )
				{
					ip = HttpContext.Current.Request.ServerVariables[ "HTTP_X_FORWARDED_FOR" ];
					if ( ip == null || ip == "" || ip.ToLower() == "unknown" )
					{
						ip = HttpContext.Current.Request.ServerVariables[ "REMOTE_ADDR" ];
					}
				}

			}
			catch ( Exception ex )
			{

			}

			return ip;
		} //
		private static string GetUserReferrer()
		{
			string lRefererPage = "";
			try
			{
				if ( HttpContext.Current != null )
				{
					if ( HttpContext.Current.Request.UrlReferrer != null )
					{
						lRefererPage = HttpContext.Current.Request.UrlReferrer.ToString();
						//check for link to us parm
						//??

					}
				}

			}
			catch ( Exception ex )
			{
				lRefererPage = "unknown";// ex.Message;
			}

			return lRefererPage;
		} //
		public static string GetUserAgent( ref bool isBot )
		{
			string agent = "";
			isBot = false;
			try
			{
				if ( HttpContext.Current != null )
				{
					if ( HttpContext.Current.Request.UserAgent != null )
					{
						agent = HttpContext.Current.Request.UserAgent;
					}

					if ( agent.ToLower().IndexOf( "bot" ) > -1
						|| agent.ToLower().IndexOf( "spider" ) > -1
						|| agent.ToLower().IndexOf( "slurp" ) > -1
						|| agent.ToLower().IndexOf( "crawl" ) > -1
						|| agent.ToLower().IndexOf( "addthis.com" ) > -1
						)
						isBot = true;
					if ( isBot )
					{
						//what should happen? Skip completely? Should add attribute to track
						//user agent may NOT be available in this context
					}
				}

			}
			catch ( Exception ex )
			{
				//agent = ex.Message;
			}

			return agent;
		} //

		#endregion

		//

		public static List<ThisEntity> SearchToday( BaseSearchModel parms )
		{
			parms.StartDate = new DateTime( DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day );

			if ( parms.PageSize == 0 )
				parms.PageSize = 25;
			int skip = 0;
			if ( parms.PageNumber > 1 )
				skip = ( parms.PageNumber - 1 ) * parms.PageSize;
			if ( string.IsNullOrWhiteSpace( parms.OrderBy ) )
			{
				parms.OrderBy = "Created";
				parms.IsDescending = true;
			}
			return SearchAll( parms );

		} //



		public static List<ThisEntity> SearchAll( BaseSearchModel parms )
		{
			string connectionString = DBConnectionRO();
			var entity = new ThisEntity();
			var list = new List<ThisEntity>();
			if ( parms.PageSize == 0 )
				parms.PageSize = 25;
			int skip = 0;
			if ( parms.PageNumber > 1 )
				skip = ( parms.PageNumber - 1 ) * parms.PageSize;
			if ( string.IsNullOrWhiteSpace( parms.OrderBy ) )
			{
				parms.OrderBy = "Created";
				parms.IsDescending = true;
			}
			if ( parms.StartDate == null || parms.StartDate < new DateTime( 2020, 1, 1 ) )
				parms.StartDate = new DateTime( 2020, 1, 1 );
			if ( parms.EndDate == null || parms.EndDate < new DateTime( 2020, 1, 1 ) )
				parms.EndDate = DateTime.Now;

			using ( var context = new EntityContext() )
			{
				var query = from Results in context.MessageLog
							.Where( s => s.Message != "Session" )
							select Results;
				if ( !string.IsNullOrWhiteSpace( parms.Keyword ) )
				{
					query = from Results in query
							.Where( s => ( s.Message.Contains( parms.Keyword )
							|| ( s.Activity.Contains( parms.Keyword ) )
							|| ( s.Description.Contains( parms.Keyword ) )
							) )
							select Results;
				}
				parms.TotalRows = query.Count();
				if ( parms.IsDescending )
				{
					if ( parms.OrderBy == "Created" )
						query = query.OrderByDescending( p => p.Created );
					else if ( parms.OrderBy == "Message" )
						query = query.OrderByDescending( p => p.Message );
					else if ( parms.OrderBy == "Activity" )
						query = query.OrderByDescending( p => p.Activity );
					else
						query = query.OrderByDescending( p => p.Created );
				}
				else
				{
					if ( parms.OrderBy == "Created" )
						query = query.OrderBy( p => p.Created );
					else if ( parms.OrderBy == "Message" )
						query = query.OrderBy( p => p.Message );
					else if ( parms.OrderBy == "Activity" )
						query = query.OrderBy( p => p.Activity );

					else
						query = query.OrderBy( p => p.Created );
				}

				var results = query.Skip( skip ).Take( parms.PageSize )
					.ToList();
				if ( results != null && results.Count > 0 )
				{
					foreach ( var item in results )
					{
						entity = new ThisEntity();
						MapFromDB( item, entity );
						list.Add( entity );
					}
				}
			}



			return list;

		} //

		public static List<ThisEntity> Search( string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows, int userId = 0 )
		{
			string connectionString = DBConnectionRO();
			var item = new ThisEntity();
			var list = new List<ThisEntity>();
			var result = new DataTable();
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				if ( string.IsNullOrEmpty( pFilter ) )
				{
					pFilter = "";
				}

				using ( SqlCommand command = new SqlCommand( "MessageSearch", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", pFilter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", pOrderBy ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );

					SqlParameter totalRows = new SqlParameter( "@TotalRows", pTotalRows );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					using ( SqlDataAdapter adapter = new SqlDataAdapter() )
					{
						adapter.SelectCommand = command;
						adapter.Fill( result );
					}
					string rows = command.Parameters[ 4 ].Value.ToString();
					try
					{
						pTotalRows = Int32.Parse( rows );
					}
					catch ( Exception ex )
					{
						pTotalRows = 0;
						LoggingHelper.LogError( ex, thisClassName + string.Format( ".Search() - Execute proc, Message: {0} \r\n Filter: {1} \r\n", ex.Message, pFilter ) );

						item = new ThisEntity
						{
							Activity = thisClassName+".Search()",
							Message = "Unexpected error encountered. System administration has been notified. Please try again later. ",
							Description = ex.Message,
							MessageType = "error"
						};
						list.Add( item );
						return list;
					}
				}

				foreach ( DataRow dr in result.Rows )
				{
					item = new ThisEntity();
					item.Id = GetRowColumn( dr, "Id", 0 );
					item.Created = GetRowColumn( dr, "Created", DateTime.Now );

					item.Application = GetRowColumn( dr, "Application", "CredentialFinder" );
					item.Activity = GetRowColumn( dr, "Activity", "" );
					item.MessageType = GetRowColumn( dr, "MessageType", "MessageType" );
					item.Message = GetRowColumn( dr, "Message", "" );					
					item.Description = GetRowColumn( dr, "Description", "" );
					//
					item.ActionByUserId = GetRowColumn( dr, "ActionByUserId", 0 );
					item.ActivityObjectId = GetRowColumn( dr, "ActivityObjectId", "" );
					item.RelatedUrl = GetRowColumn( dr, "RelatedUrl", "" );
					//
					item.IPAddress = GetRowColumn( dr, "IPAddress", "" );
					item.SessionId = GetRowColumn( dr, "SessionId", "" );
					

					list.Add( item );
				}

				return list;

			}
		}
	}

}
