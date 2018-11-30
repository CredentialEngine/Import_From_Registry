using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;

using workIT.Utilities;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

using ThisEntity = workIT.Models.Common.Widget;
using DBEntity = workIT.Data.Tables.Widget;

using EntityContext = workIT.Data.Tables.workITEntities;

namespace workIT.Factories
{
    public class WidgetManager : BaseFactory
    {
        string thisClassName = "WidgetManager";
        #region Entity Persistance ===================
        public bool Save( ThisEntity entity, int userId, ref List<string> messages )
        {
            bool isValid = true;
            int count = 0;
            DBEntity efEntity = new DBEntity();

            using ( var context = new EntityContext() )
            {
                try
                {
                    if ( ValidateProfile( entity, ref messages ) == false )
                    {
                        return false;
                    }

                    if ( entity.Id > 0 )
                    {
                        efEntity = context.Widget
                                .SingleOrDefault( s => s.Id == entity.Id );
                        if ( efEntity != null && efEntity.Id > 0 )
                        {
                            MapToDB( entity, efEntity );

                            if ( HasStateChanged( context ) )
                            {
                                efEntity.LastUpdatedById = userId;
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
                                    messages.Add( string.Format( "Error - the update was not successful for Widget: {0}, Id: {1}. But no reason was provided.", entity.Name, entity.Id ) );
                                    isValid = false;
                                    string message = string.Format( thisClassName + ". Save Failed", "Attempted to update a Widget. The process appeared to not work, but there was not an exception, so we have no message, or no clue. Widget: {0}, Id: {1}", entity.Name, entity.Id );
                                    EmailManager.NotifyAdmin( thisClassName + ". Save Failed", message );
                                }
                            }
                        }
                    }
                    else
                    {
                        MapToDB( entity, efEntity );

                        efEntity.CreatedById = userId;
                        efEntity.LastUpdatedById = userId;
                        efEntity.Created = efEntity.LastUpdated = System.DateTime.Now;

                        context.Widget.Add( efEntity );
                        // submit the change to database
                        count = context.SaveChanges();
                        if ( count > 0 )
                        {
                            entity.Id = efEntity.Id;
                            return true;
                        }
                        else
                        {
                            //?no info on error
                            messages.Add( "Error - the add was not successful. " );
                            string message = string.Format( "WidgetManager. Add Failed", "Attempted to add an Widget. The process appeared to not work, but was not an exception, so we have no message, or no clue. name:(0) ", entity.Name );
                            EmailManager.NotifyAdmin( "WidgetManager. Add Failed", message );
                            return false;
                        }
                    }
                }
                catch ( Exception ex )
                {
                    LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save(). name:(0) ", entity.Name ) );
					isValid = false;
					messages.Add( ex.Message );
                }
            }

            return isValid;
        }

        public bool ValidateProfile( ThisEntity profile, ref List<string> messages )
        {
            if ( string.IsNullOrWhiteSpace( profile.Name ) )
            {
                messages.Add( "A widget name must be included" );
            }
            if ( string.IsNullOrWhiteSpace( profile.OrgCTID ) )
            {
                messages.Add( "An organization must be selected." );
            }
            if ( !IsUrlValid( profile.LogoUrl, ref commonStatusMessage, true ) )
            {
                messages.Add( "The 'Logo URL' format is invalid. " + commonStatusMessage );
            }
            if ( !IsUrlValid( profile.WidgetStylesUrl, ref commonStatusMessage, true ) )
            {
                messages.Add( "The 'Search Style Sheet' URL format is invalid. " + commonStatusMessage );
            }
            if ( messages.Count > 0 )
                return false;

            return true;
        }

        public bool Delete( int recordId, ref string statusMessage )
        {
            bool isOK = true;
            using ( var context = new EntityContext() )
            {
                DBEntity p = context.Widget.FirstOrDefault( s => s.Id == recordId );
                if ( p != null && p.Id > 0 )
                {
                    context.Widget.Remove( p );
                    int count = context.SaveChanges();
                }
                else
                {
                    statusMessage = "Warning - the record was not found.";
                    isOK = true;
                }
            }
            return isOK;

        }

        #endregion

        #region Retrieval
        public static List<ThisEntity> GetWidgetsForOrganization( string ctid )
        {
            List<ThisEntity> list = new List<ThisEntity>();
            ThisEntity entity = new ThisEntity();
            using ( var context = new EntityContext() )
            {
                List<DBEntity> results = context.Widget
                        .Where( s => s.OrgCTID == ctid )
                        .OrderBy( s => s.Name )
                        .ToList();
                if ( results != null && results.Count > 0 )
                {
                    foreach ( var item in results )
                    {
                        entity = new ThisEntity();
                        MapFromDB( item, entity, false );
                        list.Add( entity );
                    }
                }
                return list;
            }
        }
        public static List<Organization> GetAllOrganizationsWithWidgets()
        {
            List<Organization> list = new List<Organization>();
            List<Organization> work = new List<Organization>();
            Organization entity = new Organization();
            using ( var context = new EntityContext() )
            {
                work = context.Widget
                        .GroupBy( a => new
                        {
                            a.OrganizationName,
                            orgCTID = a.OrgCTID
                        } )
                    .Select( g => new Organization
                    {
                        Name = g.Key.OrganizationName,
                        CTID = g.Key.orgCTID
                    } )
                    .OrderBy( a => a.Name )
                    .ToList();

                if ( work != null && work.Count > 0 )
                {
                    //probably OK
                    foreach ( var item in work )
                    {
                        if ( !string.IsNullOrWhiteSpace( item.Name ) && !string.IsNullOrWhiteSpace( item.CTID ) )
                        {
                            list.Add( new Organization
                            {
                                Name = item.Name,
                                CTID = item.CTID
                            } );
                        }
                    }
                }
                return list;
            }
        }

        public static ThisEntity GetFirstWidgetForUser( int userId )
        {
            ThisEntity entity = new ThisEntity();
            List<ThisEntity> list = GetWidgetsForUser( userId );
            if ( list != null && list.Count > 0 )
                return list[ 0 ];

            return entity;

        }
        public static List<ThisEntity> GetWidgetsForUser( int userId )
        {
            List<ThisEntity> list = new List<ThisEntity>();
            ThisEntity entity = new ThisEntity();
            using ( var context = new EntityContext() )
            {
                List<DBEntity> results = context.Widget
                        .Where( s => s.CreatedById == userId )
                        .OrderBy( s => s.Created )
                        .ToList();
                if ( results != null && results.Count > 0 )
                {
                    foreach ( var item in results )
                    {
                        entity = new ThisEntity();
                        MapFromDB( item, entity, false );
                        list.Add( entity );
                    }
                }
                return list;
            }
        }

        public static ThisEntity Get( int id )
        {
            ThisEntity entity = new ThisEntity();
            using ( var context = new EntityContext() )
            {
                DBEntity item = context.Widget
                        .FirstOrDefault( s => s.Id == id );

                if ( item != null && item.Id > 0 )
                {
                    MapFromDB( item, entity );
                }
                return entity;
            }
        }
        //TODO - add check that widget alias is unique
        public static ThisEntity GetByAlias( string alias )
        {
            ThisEntity entity = new ThisEntity();
            if ( string.IsNullOrEmpty( alias ) )
                return entity;

            using ( var context = new EntityContext() )
            {
                DBEntity item = context.Widget
                        .FirstOrDefault( s => s.WidgetAlias == alias );

                if ( item != null && item.Id > 0 )
                {
                    MapFromDB( item, entity );
                }
                return entity;
            }
        } //

        public static void MapFromDB( DBEntity from, ThisEntity to, bool includingFilters = true )
        {
            to.Id = from.Id;
            to.OrgCTID = from.OrgCTID;
            to.OrganizationName = from.OrganizationName;
            to.Name = from.Name;

			to.CreatedById = ( int )( from.CreatedById ?? 0 );
			to.LastUpdatedById = ( int )( from.LastUpdatedById ?? 0 );

			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime )from.Created;
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime )from.LastUpdated;

            to.WidgetAlias = from.WidgetAlias;
            
            to.CustomStyles = from.CustomStyles;
            if ( !string.IsNullOrEmpty( to.CustomStyles ) )
            {
                to.WidgetStyles = JsonConvert.DeserializeObject<WidgetStyles>( to.CustomStyles );
            }
            else
                to.WidgetStyles = new WidgetStyles();

			if ( !string.IsNullOrWhiteSpace( to.CustomStylesFileName ) )
				to.CustomStylesURL = UtilityManager.GetAppKeyValue( "customStylesUrl" ) + to.CustomStylesFileName;
			to.WidgetStylesUrl = from.WidgetStylesUrl;

			//??
			to.LogoUrl = from.LogoUrl;
			//old properties no longer used
			to.CountryFilters = from.CountryFilters;
			to.RegionFilters = from.RegionFilters;
			to.CityFilters = from.CityFilters;
			to.IncludeIfAvailableOnline = from.IncludeIfAvailableOnline ?? false;

			if ( ( from.OwningOrganizationIds ?? "" ) == "0" )
                from.OwningOrganizationIds = null;
            to.OwningOrganizationIds = from.OwningOrganizationIds;

            if ( !string.IsNullOrWhiteSpace( to.OwningOrganizationIds ) )
            {
                var parts = to.OwningOrganizationIds.Split( ',' );
                if ( parts.Count() > 0 )
                {
                    int orgId = 0;
                    foreach ( var item in parts )
                    {
                        if ( int.TryParse( item, out orgId ) )
                        {
                            to.OwningOrganizationIdsList.Add( orgId );
                        }
                    }
                }
            }


			to.SearchFilters = from.SearchFilters;
			if ( !string.IsNullOrEmpty( to.SearchFilters ) )
			{
				to.WidgetFilters = JsonConvert.DeserializeObject<WidgetFilters>( to.SearchFilters );
			}
			else
				to.WidgetFilters = new WidgetFilters();



		}
		public static void MapToDB( ThisEntity from, DBEntity to )
		{

			//don't want to allow accidently updates to org data. Confirm this works correctly.
			if ( from.Id == 0 || to.OrgCTID != from.OrgCTID )
			{
				to.OrgCTID = from.OrgCTID;
				to.OrganizationName = from.OrganizationName;
			}

			to.Name = from.Name;
			//are we assuming serialization is always done before here?
			if ( string.IsNullOrWhiteSpace( from.CustomStyles ) && from.WidgetStyles != null )
			{
				from.CustomStyles = JsonConvert.SerializeObject( from.WidgetStyles );
			}
			to.CustomStyles = from.CustomStyles;
			to.CustomStylesFileName = from.CustomStylesFileName;

			//??
			to.WidgetAlias = from.WidgetAlias;
			//??
			to.LogoUrl = from.LogoUrl;
			if ( !string.IsNullOrEmpty( from.SearchFilters ) )
			{
				to.SearchFilters = from.SearchFilters;
			}

			if ( IsDevEnv() )
			{
				//old properties no longer used
				to.CountryFilters = from.CountryFilters;
				to.RegionFilters = from.RegionFilters;
				to.CityFilters = from.CityFilters;
				to.IncludeIfAvailableOnline = from.IncludeIfAvailableOnline;


				//are we assuming serialization is always done before here?
				if ( string.IsNullOrWhiteSpace( from.SearchFilters ) && from.WidgetFilters != null )
				{
					from.SearchFilters = JsonConvert.SerializeObject( from.WidgetFilters );
				}
				to.SearchFilters = from.SearchFilters;

				to.WidgetStylesUrl = from.WidgetStylesUrl;

				if ( from.OwningOrganizationIds.IndexOf( "workIT.Models" ) > -1 )
					from.OwningOrganizationIds = null;
				else if ( ( from.OwningOrganizationIds ?? "" ) == "0" )
					from.OwningOrganizationIds = null;

				to.OwningOrganizationIds = from.OwningOrganizationIds;
				if ( from.OwningOrganizationIdsList != null && from.OwningOrganizationIdsList.Count > 0 )
				{
					to.OwningOrganizationIds = string.Join( ",", from.OwningOrganizationIdsList.Select( n => n.ToString() ).ToArray() );
				}
			}
        }

        #endregion
    }
}
