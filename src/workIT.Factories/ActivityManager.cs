using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.Entity;
using System.Linq;
using System.Web;

using workIT.Models;
using workIT.Models.Helpers.Reports;
using workIT.Models.Search;
//using workIT.Models.Helpers.Reports;
using workIT.Data.Tables;
using workIT.Utilities;
using Views = workIT.Data.Views;
using ViewContext = workIT.Data.Views.workITViews;
using EntityContext = workIT.Data.Tables.workITEntities;
using System.Runtime.InteropServices;

namespace workIT.Factories
{
    public class ActivityManager : BaseFactory
    {
        private static string thisClassName = "ActivityManager";


        #region Persistance
        public int SiteActivityAdd( SiteActivity entity )
        {
            ActivityLog log = new ActivityLog();
            MapToDB( entity, log );
            return SiteActivityAdd( log );
        } //

        private static int SiteActivityAdd( ActivityLog log )
        {
            int count = 0;
            string truncateMsg = "";
            bool isBot = false;
            string server = UtilityManager.GetAppKeyValue( "serverName", "" );

            string agent = GetUserAgent( ref isBot );

            if ( log.RelatedTargetUrl == null )
                log.RelatedTargetUrl = "";

            if ( log.Referrer == null )
                log.Referrer = "";
            if ( log.Comment == null )
                log.Comment = "";
            if ( log.SessionId == null || log.SessionId.Length < 10 )
                log.SessionId = GetCurrentSessionId();

            if ( log.IPAddress == null || log.IPAddress.Length < 10 )
                log.IPAddress = GetUserIPAddress();
            if ( log.IPAddress.Length > 50 )
                log.IPAddress = log.IPAddress.Substring( 0, 50 );

            //================================
            if ( isBot )
            {
                //LoggingHelper.DoBotTrace( 6, string.Format( ".SiteActivityAdd Skipping Bot: activity. Agent: {0}, Activity: {1}, Event: {2}, \r\nRelatedTargetUrl: {3}", agent, log.Activity, log.Event, log.RelatedTargetUrl ) );
                //should this be added with isBot attribute for referencing when crawled?
                return 0;
            }
            //================================
            if ( IsADuplicateRequest( log.Comment ) )
                return 0;

            StoreLastRequest( log.Comment );

            //----------------------------------------------
            if ( log.Referrer == null || log.Referrer.Trim().Length < 5 )
            {
                string referrer = GetUserReferrer();
                log.Referrer = referrer;
            }
            //if ( log.Referrer.Length > 1000 )
            //{
            //    truncateMsg += string.Format( "Referrer overflow: {0}; ", log.Referrer.Length );
            //    log.Referrer = log.Referrer.Substring( 0, 1000 );
            //}


            if ( log.RelatedTargetUrl != null && log.RelatedTargetUrl.Length > 500 )
            {
                truncateMsg += string.Format( "RelatedTargetUrl overflow: {0}; ", log.RelatedTargetUrl.Length );
                log.RelatedTargetUrl = log.RelatedTargetUrl.Substring( 0, 500 );
            }

            //if ( log.Referrer.Length > 0 )
            //    log.Comment += ", Referrer: " + log.Referrer;

            //log.Comment += GetUserAgent();

            //if ( log.Comment != null && log.Comment.Length > 1000 )
            //{
            //    truncateMsg += string.Format( "Comment overflow: {0}; ", log.Comment.Length );
            //    log.Comment = log.Comment.Substring( 0, 1000 );
            //}

            //the following should not be necessary but getting null related exceptions
            if ( log.TargetUserId == null )
                log.TargetUserId = 0;
            if ( log.ActionByUserId == null )
                log.ActionByUserId = 0;
            if ( log.ActivityObjectId == null )
                log.ActivityObjectId = 0;
            if ( log.ObjectRelatedId == null )
                log.ObjectRelatedId = 0;
            if ( log.TargetObjectId == null )
                log.TargetObjectId = 0;


            using ( var context = new EntityContext() )
            {
                try
                {
                    log.CreatedDate = System.DateTime.Now;
                    if ( log.ActivityType == null || log.ActivityType.Length < 3 )
                        log.ActivityType = "Audit";

                    context.ActivityLog.Add( log );

                    // submit the change to database
                    count = context.SaveChanges();

                    if ( truncateMsg.Length > 0 )
                    {
                        string msg = string.Format( "ActivityId: {0}, Message: {1}", log.Id, truncateMsg );

                        EmailManager.NotifyAdmin( "ActivityLog Field Overflow", msg );
                    }
                    if ( count > 0 )
                    {
                        return log.Id;
                    }
                    else
                    {
                        //?no info on error
                        return 0;
                    }
                }
                catch ( Exception ex )
                {

                    LoggingHelper.LogError( ex, thisClassName + ".SiteActivityAdd(EFDAL.ActivityLog) ==> SHOULD ADD RETRY via proc\n\r" + ex.StackTrace.ToString() );
                    //call stored proc as backup!

                    return count;
                }
            }
        } //
        private void MapToDB( SiteActivity from, ActivityLog to )
        {
            to.Id = from.Id;
            to.ActivityType = from.ActivityType;
            to.Activity = from.Activity;
            to.Event = from.Event;
            to.Comment = from.Comment;
            to.TargetUserId = from.TargetUserId;
            to.ActionByUserId = from.ActionByUserId;
            to.ActivityObjectId = from.ActivityObjectId;
            to.ObjectRelatedId = from.ObjectRelatedId;

            to.RelatedTargetUrl = from.RelatedTargetUrl;
            to.TargetObjectId = from.TargetObjectId;
            to.SessionId = from.SessionId;
            to.IPAddress = from.IPAddress;
			to.Referrer = from.Referrer;
            //to.Referrer = !string.IsNullOrEmpty(from.Referrer) && from.Referrer.lentg;
            to.IsBot = from.IsBot;

        }
        private static void MapFromDB( ActivityLog from, SiteActivity to )
        {
            to.Id = from.Id;
            to.Created = (DateTime)from.CreatedDate;
            to.ActivityType = from.ActivityType;
            to.Activity = from.Activity;
            to.Event = from.Event;
            to.Comment = from.Comment;
            to.TargetUserId = from.TargetUserId;
            to.ActionByUserId = from.ActionByUserId;
            to.ActivityObjectId = from.ActivityObjectId;
            to.ObjectRelatedId = from.ObjectRelatedId;

            to.RelatedTargetUrl = from.RelatedTargetUrl;
            to.TargetObjectId = from.TargetObjectId;
            to.SessionId = from.SessionId;
            to.IPAddress = from.IPAddress;
            to.Referrer = from.Referrer;
            to.IsBot = from.IsBot;

        }

        #endregion


        #region helpers

        public static void StoreLastRequest( string actionComment )
        {
            string sessionKey = GetCurrentSessionId() + "_lastHit";

			try
			{
				if ( HttpContext.Current != null )
				{
					if ( HttpContext.Current.Session != null )
					{
						HttpContext.Current.Session[ sessionKey ] = actionComment;
					}
				}
                
            }
            catch
            {
            }

        } //

        public static bool IsADuplicateRequest( string actionComment )
        {
            string sessionKey = GetCurrentSessionId() + "_lastHit";
            bool isDup = false;
			try
			{
				if ( HttpContext.Current != null )
				{
					if ( HttpContext.Current.Session != null )
					{
						string lastAction = HttpContext.Current.Session[ sessionKey ].ToString();
						if ( lastAction.ToLower() == actionComment.ToLower() )
						{
							LoggingHelper.DoTrace( 7, "ActivityServices. Duplicate action: " + actionComment );
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
				if( HttpContext.Current != null)
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

        public static CommonTotals SiteTotals_Get()
        {
            CommonTotals entity = new CommonTotals();
            using (var context = new ViewContext())
            {
				//21-01-05 mparsons - note that the view (SiteTotalsSummaries) states to no longer use it!!!!
				Views.SiteTotalsSummary item = context.SiteTotalsSummaries
                        .SingleOrDefault(s => s.Id == 1);

                if (item != null && item.Id > 0)
                {
                    entity.TotalOrganizations = GetField(item.TotalOrgs);
                    entity.TotalPartnerOrganizations = GetField(item.TotalDirectOrgs);
                    entity.TotalOtherOrganizations = entity.TotalOrganizations - entity.TotalPartnerOrganizations;
                    entity.TotalQAOrganizations = GetField(item.TotalQAOrgs);

                    entity.TotalEnteredCredentials = GetField(item.TotalEnteredCredentials);
                    entity.TotalPartnerCredentials = GetField(item.TotalPartnerCredentials);
                    entity.TotalPendingCredentials = GetField(item.TotalPendingCredentials);
                    entity.TotalDirectCredentials = GetField(item.TotalDirectCredentials);
                    entity.TotalOtherOrganizations = entity.TotalPartnerCredentials - entity.TotalDirectCredentials;


                    entity.TotalCredentialsAtCurrentCtdl = GetField(item.TotalCredentialsAtCurrentCtdl);
                    entity.TotalCredentialsToBeUpdatedToCurrentCtdl = GetField(item.TotalCredentialsToBeUpdatedToCurrentCtdl);
                }
            }

            return entity;
        }
        public static SiteActivity GetLastImport()
        {
            SiteActivity entity = new SiteActivity();
            using (var context = new EntityContext())
            {
                List<ActivityLog> list = context.ActivityLog
                        .Where( s => s.ActivityType == "System"
                        && s.Activity == "Import"
                        && s.Event == "Start")
                        .OrderByDescending(o => o.CreatedDate)
                        .Take(1)
                        .ToList();
                if (list != null && list.Count > 0)
                {
                    ActivityLog efentity = list[ 0 ];
                    MapFromDB( efentity, entity );
                }
            }
            return entity;
        }
        public static List<SiteActivity> SearchToday( BaseSearchModel parms )
        {
            string connectionString = DBConnectionRO();
            SiteActivity entity = new SiteActivity();
            List<SiteActivity> list = new List<SiteActivity>();
            if ( parms.PageSize == 0 )
                parms.PageSize = 25;
            int skip = 0;
            if ( parms.PageNumber > 1 )
                skip = ( parms.PageNumber - 1 ) * parms.PageSize;
            if ( string.IsNullOrWhiteSpace( parms.OrderBy ) )
            {
                parms.OrderBy = "CreatedDate";
                parms.IsDescending = true;
            }
            list = SearchAll( parms );
            return list;

        } //



        public static List<SiteActivity> SearchAll( BaseSearchModel parms )
        {
            string connectionString = DBConnectionRO();
            SiteActivity entity = new SiteActivity();
            List<SiteActivity> list = new List<SiteActivity>();
            if ( parms.PageSize == 0 )
                parms.PageSize = 25;
            int skip = 0;
            if ( parms.PageNumber > 1 )
                skip = ( parms.PageNumber - 1 ) * parms.PageSize;
            if ( string.IsNullOrWhiteSpace( parms.OrderBy ) )
            {
                parms.OrderBy = "CreatedDate";
                parms.IsDescending = true;
            }
            if ( parms.StartDate == null || parms.StartDate < new DateTime( 2015, 1, 1 ) )
                parms.StartDate = new DateTime( 2015, 1, 1 );
            if ( parms.EndDate == null || parms.EndDate < new DateTime( 2015, 1, 1 ) )
                parms.EndDate = DateTime.Now;

            using ( var context = new ViewContext() )
            {
                var query = from Results in context.Activity_Summary
                            .Where( s => s.Activity != "Session" )
                            select Results;
                if ( !string.IsNullOrWhiteSpace( parms.Keyword ) )
                {
                    query = from Results in query
                            .Where( s => ( s.Activity.Contains( parms.Keyword )
                            || ( s.Event.Contains( parms.Keyword ) )
                            || ( s.Comment.Contains( parms.Keyword ) )
                            ) )
                            select Results;
                }
                parms.TotalRows = query.Count();
                if ( parms.IsDescending )
                {
                    if ( parms.OrderBy == "CreatedDate" )
                        query = query.OrderByDescending( p => p.CreatedDate );
                    else if ( parms.OrderBy == "Activity" )
                        query = query.OrderByDescending( p => p.Activity );
                    else if ( parms.OrderBy == "Event" )
                        query = query.OrderByDescending( p => p.Event );
                    else if ( parms.OrderBy == "ActionByUser" )
                        query = query.OrderByDescending( p => p.ActionByUser );
                    else
                        query = query.OrderByDescending( p => p.CreatedDate );
                }
                else
                {
                    if ( parms.OrderBy == "CreatedDate" )
                        query = query.OrderBy( p => p.CreatedDate );
                    else if ( parms.OrderBy == "Activity" )
                        query = query.OrderBy( p => p.Activity );
                    else if ( parms.OrderBy == "Event" )
                        query = query.OrderBy( p => p.Event );
                    else if ( parms.OrderBy == "ActionByUser" )
                        query = query.OrderBy( p => p.ActionByUser );

                    else
                        query = query.OrderBy( p => p.CreatedDate );
                }

                var results = query.Skip( skip ).Take( parms.PageSize )
                    .ToList();
                if ( results != null && results.Count > 0 )
                {
                    foreach ( Views.Activity_Summary item in results )
                    {
                        entity = new SiteActivity();
                        entity.Id = item.Id;
                        entity.Activity = item.Activity;
                        entity.Event = item.Event;
                        entity.Comment = item.Comment;
                        entity.Created = ( DateTime )item.CreatedDate;
                        entity.ActionByUser = item.ActionByUser;
                        entity.Referrer = entity.Referrer;
                        list.Add( entity );
                    }
                }
            }



            return list;

        } //

        public static List<SiteActivity> Search( string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows, int userId = 0 )
        {
            string connectionString = DBConnectionRO();
            SiteActivity item = new SiteActivity();
            List<SiteActivity> list = new List<SiteActivity>();
            var result = new DataTable();
            using ( SqlConnection c = new SqlConnection( connectionString ) )
            {
                c.Open();

                if ( string.IsNullOrEmpty( pFilter ) )
                {
                    pFilter = "";
                }

                using ( SqlCommand command = new SqlCommand( "Activity_Search", c ) )
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
                    string rows = command.Parameters[4].Value.ToString();
                    try
                    {
                        pTotalRows = Int32.Parse( rows );
                    }
                    catch ( Exception ex )
                    {
                        pTotalRows = 0;
                        LoggingHelper.LogError( ex, thisClassName + string.Format( ".Search() - Execute proc, Message: {0} \r\n Filter: {1} \r\n", ex.Message, pFilter ) );

                        item = new SiteActivity
                        {
                            ActivityType = "Unexpected error encountered. System administration has been notified. Please try again later. ",
                            Comment = ex.Message,
                            Event = "error"
                        };
                        list.Add( item );
                        return list;
                    }
                }

                foreach ( DataRow dr in result.Rows )
                {
                    item = new SiteActivity();
                    item.Id = GetRowColumn( dr, "Id", 0 );
                    item.CreatedDate = GetRowColumn( dr, "CreatedDate", DateTime.Now );
                    item.ActivityType = GetRowColumn( dr, "ActivityType", "ActivityType" );
                    item.Activity = GetRowColumn( dr, "Activity", "" );
                    item.Event = GetRowColumn( dr, "Event", "" );
                    item.Comment = GetRowColumn( dr, "Comment", "" );
                    item.ActionByUser = GetRowColumn( dr, "ActionByUser", "" );
                    item.ActionByUserId = GetRowColumn( dr, "ActionByUserId", 0 );
                    item.Referrer = GetRowColumn( dr, "Referrer", "" );
                    item.ActivityObjectId = GetRowColumn( dr, "ActivityObjectId", 0 );
                    item.IPAddress = GetRowColumn( dr, "IPAddress", "" );
                    item.SessionId = GetRowColumn( dr, "SessionId", "" );
                    item.IsBot = GetRowColumn( dr, "IsBot", false );
					//item.EntityTypeId = GetRowColumn( dr, "EntityTypeId", 0 );
					//item.OwningOrgId = GetRowColumn( dr, "OwningOrgId", 0 );
					item.Organization = GetRowColumn( dr, "Organization", "" );
					//N/A
					//item.ParentObject = GetRowColumn( dr, "ParentObject", "" );
					//item.ParentEntityTypeId = GetRowColumn( dr, "ParentEntityTypeId", 0 );
					//item.ParentRecordId = GetRowColumn( dr, "ParentRecordId", 0 );

					list.Add( item );
                }

                return list;

            }
        }

        //public static CommonTotals SiteTotals_Get()
        //{
        //	CommonTotals entity = new CommonTotals();
        //	using ( var context = new ViewContext() )
        //	{
        //		Views.SiteTotalsSummary item = context.SiteTotalsSummaries
        //				.SingleOrDefault( s => s.Id == 1 );

        //		if ( item != null && item.Id > 0 )
        //		{
        //			entity.TotalOrganizations = GetField(item.TotalOrgs);
        //			entity.TotalPartnerOrganizations = GetField( item.TotalDirectOrgs );
        //			entity.TotalOtherOrganizations = entity.TotalOrganizations - entity.TotalPartnerOrganizations;
        //			entity.TotalQAOrganizations = GetField(item.TotalQAOrgs);

        //			entity.TotalEnteredCredentials = GetField( item.TotalEnteredCredentials );
        //			entity.TotalPartnerCredentials = GetField( item.TotalPartnerCredentials );
        //			entity.TotalPendingCredentials = GetField( item.TotalPendingCredentials );
        //			entity.TotalDirectCredentials = GetField( item.TotalDirectCredentials );
        //			entity.TotalOtherOrganizations = entity.TotalPartnerCredentials - entity.TotalDirectCredentials;


        //			entity.TotalCredentialsAtCurrentCtdl = GetField( item.TotalCredentialsAtCurrentCtdl );
        //			entity.TotalCredentialsToBeUpdatedToCurrentCtdl = GetField( item.TotalCredentialsToBeUpdatedToCurrentCtdl );
        //		}
        //	}

        //	return entity;
        //}
    }

}
