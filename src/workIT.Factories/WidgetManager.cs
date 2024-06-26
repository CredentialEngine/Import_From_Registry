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
						if ( IsValidGuid( entity.RowId ) )
							efEntity.RowId = entity.RowId;
						else
							efEntity.RowId = Guid.NewGuid();
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
							messages.Add( thisClassName + "Error - the add was not successful. " );
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
		public static List<ThisEntity> GetAllWidgets()
		{
			List<ThisEntity> list = new List<ThisEntity>();
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				List<DBEntity> results = context.Widget
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
			if ( id < 1 )
				return entity;

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
			to.RowId = from.RowId;
			to.CreatedById = ( int )( from.CreatedById ?? 0 );
			to.LastUpdatedById = ( int )( from.LastUpdatedById ?? 0 );

			if ( IsValidDate( from.Created ) )
			{
				to.Created = ( DateTime ) from.Created;
				to.CreatedDisplay = to.Created.ToString( "MMM d, yyyy" );
			}
			if ( IsValidDate( from.LastUpdated ) )
			{
				to.LastUpdated = ( DateTime ) from.LastUpdated;
				to.LastUpdatedDisplay = to.LastUpdated.ToString( "MMM d, yyyy" );
			}

			to.WidgetAlias = from.WidgetAlias;

			to.CustomStyles = from.CustomStyles;
			if ( !string.IsNullOrEmpty( to.CustomStyles ) )
			{
				to.WidgetStyles = JsonConvert.DeserializeObject<WidgetStyles>( to.CustomStyles );
				//????? how do the filters get set?
				//the controller deserializes to V2 - terrible
				//WidgetV2 w2 = new WidgetV2();
				//w2 = JsonConvert.DeserializeObject<WidgetV2>( to.CustomStyles );
			}
			else
				to.WidgetStyles = new WidgetStyles();

			to.AllowsCSVExport = from.AllowsCSVExport != null ? ( bool ) from.AllowsCSVExport : false;

			if ( !string.IsNullOrWhiteSpace( to.CustomStylesFileName ) )
				to.CustomStylesURL = UtilityManager.GetAppKeyValue( "widgetResourceUrl" ) + to.CustomStylesFileName;
			to.WidgetStylesUrl = from.WidgetStylesUrl;
			//LogoUrl is part of the json, so not acutally used?
			to.LogoUrl = from.LogoUrl;
			//to.LogoFileName = from.LogoFileName;
			//not clear this is used since that latter is null
			if ( !string.IsNullOrWhiteSpace( to.LogoFileName ) )
				to.LogoUrl = UtilityManager.GetAppKeyValue( "widgetResourceUrl" ) + to.LogoFileName;

			//old properties no longer used
			to.CountryFilters = from.CountryFilters;
			to.RegionFilters = from.RegionFilters;
			to.CityFilters = from.CityFilters;
			to.IncludeIfAvailableOnline = from.IncludeIfAvailableOnline ?? false;

			if ( ( from.OwningOrganizationIds ?? string.Empty ) == "0" )
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

			//22-05-11 mp these are stored in the db, everything is in CustomStyles.
			to.SearchFilters = from.SearchFilters;
			if ( !string.IsNullOrEmpty( to.SearchFilters ) )
			{
				to.WidgetFilters = JsonConvert.DeserializeObject<WidgetFilters>( to.SearchFilters );
			}
			else
				to.WidgetFilters = new WidgetFilters();

			//determine if there should be a widget resource filter
			//may not want to arbitrarily get a large list, rather use on demand
			//var list = WidgetSelectionGetAll( to.Id, "CredentialFilters", 1 );
			//OR
			to.HasCredentialPotentialResults = WidgetHasSectionSelections( to.Id, "CredentialFilters", 1 );

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
			//TODO delete from database
			//to.LogoFileName = from.LogoFileName;

			//LogoUrl is part of the json, so not acutally used? - confirm
			to.LogoUrl = from.LogoUrl;

			if ( !string.IsNullOrEmpty( from.SearchFilters ) )
			{
				to.SearchFilters = from.SearchFilters;
			}
			//until in interface, do not attempt to map
			if ( to.AllowsCSVExport == null || from.AllowsCSVExport )
			{
				to.AllowsCSVExport = from.AllowsCSVExport;
			}
			else
			{
				//to.AllowsCSVExport = from.AllowsCSVExport;
			}

			//if ( IsDevEnv() )
			//{
			//	//old properties no longer used
			//	to.CountryFilters = from.CountryFilters;
			//	to.RegionFilters = from.RegionFilters;
			//	to.CityFilters = from.CityFilters;
			//	to.IncludeIfAvailableOnline = from.IncludeIfAvailableOnline;


			//	//are we assuming serialization is always done before here?
			//	if ( string.IsNullOrWhiteSpace( from.SearchFilters ) && from.WidgetFilters != null )
			//	{
			//		from.SearchFilters = JsonConvert.SerializeObject( from.WidgetFilters );
			//	}
			//	to.SearchFilters = from.SearchFilters;

			//	to.WidgetStylesUrl = from.WidgetStylesUrl;

			//	if ( ( from.OwningOrganizationIds ?? string.Empty ).IndexOf( "workIT.Models" ) > -1 )
			//		from.OwningOrganizationIds = null;
			//	else if ( ( from.OwningOrganizationIds ?? string.Empty ) == "0" )
			//		from.OwningOrganizationIds = null;

			//	to.OwningOrganizationIds = from.OwningOrganizationIds;
			//	if ( from.OwningOrganizationIdsList != null && from.OwningOrganizationIdsList.Count > 0 )
			//	{
			//		to.OwningOrganizationIds = string.Join( ",", from.OwningOrganizationIdsList.Select( n => n.ToString() ).ToArray() );
			//	}
			//}
		}

		#endregion


		#region WidgetSelection ===================
		/// <summary>
		/// Potential usage
		/// Search will need to 'know' if to filter on widget has resources
		/// </summary>
		/// <param name="widgetId"></param>
		/// <param name="widgetSection"></param>
		/// <param name="entityTypeId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public static List<WidgetResource> WidgetSelectionGetAll( int widgetId, string widgetSection, int entityTypeId )
		{
			var output = new List<WidgetResource>();
			var entity = new WidgetResource();
			using ( var context = new EntityContext() )
			{
				var ws = context.Widget_Selection.Where( s => s.WidgetId == widgetId
				&& s.WidgetSection == widgetSection
				&& s.EntityTypeId == entityTypeId);

				if ( ws != null && ws.Count() > 0 )
				{
					foreach ( var item in ws ) {
						entity = new WidgetResource()
						{
							WidgetId = item.WidgetId,
							EntityTypeId = item.EntityTypeId,
							WidgetSection = item.WidgetSection,
							RecordId = item.RecordId,
							ResourceName = item.ResourceName
						};
						output.Add( entity );
					}
				}
			}

			return output;
		}

		public static bool WidgetHasSectionSelections( int widgetId, string widgetSection, int entityTypeId )
		{
			using ( var context = new EntityContext() )
			{
				var ws = context.Widget_Selection.Where( s => s.WidgetId == widgetId
				&& s.WidgetSection == widgetSection
				&& s.EntityTypeId == entityTypeId );

				if ( ws != null && ws.Count() > 0 )
				{
					return true;
				}
			}

			return false;
		}
		public bool WidgetSelectionAdd( int widgetId, string widgetSection, int entityTypeId, int recordId, string resourceName, ref List<string> messages, ref bool alreadyExists )
		{
			bool isValid = true;
			int count = 0;
			alreadyExists = false;
			using ( var context = new EntityContext() )
			{
				try
				{
					if ( widgetId == 0 )
						messages.Add( "Error: a widgetId was not provided" );
					if ( entityTypeId == 0 )
						messages.Add( "Error: an entityTypeId was not provided" );
					if ( recordId == 0 )
						messages.Add( "Error: a recordId was not provided" );

					if ( string.IsNullOrWhiteSpace(widgetSection))
						messages.Add( "Error: a widgetSection was not provided" ); 
					if (string.IsNullOrWhiteSpace(resourceName))
						messages.Add( "Error: a resourceName was not provided" );

					if ( messages.Count() > 0 )
						return false;

					var widget = Get( widgetId );
					if (widget == null || widget.Id == 0 )
					{
						messages.Add( "Error: a widget was not found for id: " + widgetId.ToString() );
						return false;
					}
					//check if selection already exists
					var p = context.Widget_Selection.FirstOrDefault( s => s.WidgetId == widgetId
									&& s.WidgetSection == widgetSection
									&& s.EntityTypeId == entityTypeId
									&& s.RecordId == recordId );
					if ( p != null && p.Id > 0 )
					{
						//already exists, so skip
						alreadyExists = true;
						return true;
					}
					//
					EM.Widget_Selection efEntity = new EM.Widget_Selection()
					{
						WidgetId = widgetId,
						WidgetSection = widgetSection,
						EntityTypeId = entityTypeId,
						RecordId = recordId,
						ResourceName = resourceName,
						Created = System.DateTime.Now
					};
					context.Widget_Selection.Add( efEntity );
					// submit the change to database
					count = context.SaveChanges();
					if ( count > 0 )
					{
						//this resource will need to be reindexed! ==> done in caller
						return true;
					}
					else
					{
						//?no info on error
						messages.Add( "Error: the Widget_Selection add was not successful. " );
						string message = string.Format( "WidgetManager. Add Failed", "Attempted to add an Widget_Selection. The process appeared to not work, but was not an exception, so we have no message, or no clue. widgetId: (0) ", widgetId );
						EmailManager.NotifyAdmin( "WidgetManager.Add Failed", message );
						return false;
					}

				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".WidgetSelectionAdd(). widgetId:(0) ", widgetId ) );
					isValid = false;
					messages.Add( ex.Message );
				}
			}

			return isValid;
		}


		public bool WidgetSelectionDelete( int widgetId, string widgetSection, int entityTypeId, int recordId, ref List<string> messages )
		{
			bool isOK = true;
			try
			{
				using ( var context = new EntityContext() )
				{
					var p = context.Widget_Selection.FirstOrDefault( s => s.WidgetId == widgetId
									&& s.WidgetSection == widgetSection
									&& s.EntityTypeId == entityTypeId
									&& s.RecordId == recordId );
					if ( p != null && p.Id > 0 )
					{
						context.Widget_Selection.Remove( p );
						int count = context.SaveChanges();
					}
					else
					{
						messages.Add( "Warning - the record was not found." );
						isOK = true;
					}
				}
			} catch(Exception ex)
			{
				LoggingHelper.LogError( ex, thisClassName + "WidgetSelectionDelete" );
				messages.Add( ex.Message );
				isOK = false;
			}
			return isOK;

		}
		public bool WidgetSelectionDelete( int recordId, ref string statusMessage )
		{
			bool isOK = true;
			using ( var context = new EntityContext() )
			{
				var p = context.Widget_Selection.FirstOrDefault( s => s.Id == recordId );
				if ( p != null && p.Id > 0 )
				{
					context.Widget_Selection.Remove( p );
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

	}
}
