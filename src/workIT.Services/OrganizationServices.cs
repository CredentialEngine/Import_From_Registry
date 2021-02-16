using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;

using workIT.Utilities;
using workIT.Models;
using workIT.Models.Node;
using workIT.Models.Common;
using MCD=workIT.Models.Detail;
using ME = workIT.Models.Elastic;
using workIT.Models.Search;
using ElasticHelper = workIT.Services.ElasticServices;

using ThisEntity = workIT.Models.Common.Organization;
using EntityMgr = workIT.Factories.OrganizationManager;
using CM = workIT.Models.Common;
using Mgr = workIT.Factories.OrganizationManager;
using workIT.Factories;

namespace workIT.Services
{
	public class OrganizationServices
	{
		static string thisClassName = "OrganizationServices";

		#region import
		/// <summary>
		/// Get bu CTID - will return pending records
		/// </summary>
		/// <param name="ctid"></param>
		/// <returns></returns>
		public static ThisEntity GetByCtid( string ctid )
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			return EntityMgr.GetByCtid( ctid );
		}

		public static ThisEntity GetDetailByCtid( string ctid, bool skippingCache = false )
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			var organization = EntityMgr.GetByCtid( ctid );
			return GetDetail( organization.Id, skippingCache );
		}


		public bool Import( ThisEntity entity, ref SaveStatus status )
		{
			//do a get, and add to cache before updating
			if ( entity.Id > 0 )
			{
				if ( UtilityManager.GetAppKeyValue( "organizationCacheMinutes", 0 ) > 0 )
				{
					if ( System.DateTime.Now.Hour > 7 && System.DateTime.Now.Hour < 18 )
						GetDetail( entity.Id );
				}
			}
			bool isValid = new EntityMgr().Save( entity, ref status );
			List<string> messages = new List<string>();
			if ( entity.Id > 0 )
			{
				if ( UtilityManager.GetAppKeyValue( "organizationCacheMinutes", 0 ) > 0 )
					CacheManager.RemoveItemFromCache( "organization", entity.Id );

				if ( UtilityManager.GetAppKeyValue( "delayingAllCacheUpdates", false ) == false )
				{
					//update cache
					ThreadPool.QueueUserWorkItem( UpdateCaches, entity );

				}
				else
				{
					new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, entity.Id, 1, ref messages );
					//add all credential in a verification profile
					if ( entity.VerificationServiceProfiles != null && entity.VerificationServiceProfiles.Count > 0 )
					{
						foreach ( var profile in entity.VerificationServiceProfiles )
						{
							if ( profile.TargetCredential != null
								&& profile.TargetCredential.Count > 0 )
							{
								new CredentialServices().AddCredentialsToPendingReindex( profile.TargetCredential );
							}
						}
					}
					//20-11-20 mp re: QA performed
					//				- may have to reindex all orgs etc that have QA performed by a QA org!!!
					if ( messages.Count > 0 )
						status.AddWarningRange( messages );
				}
			}

			return isValid;
		}
		static void UpdateCaches( Object entity )
		{
			if ( entity.GetType() != typeof( Models.Common.Organization ) )
				return;
			var document = ( entity as Models.Common.Organization );
			EntityCache ec = new EntityCache()
			{
				EntityTypeId = 2,
				EntityType = "Organization",
				EntityStateId = document.EntityStateId,
				EntityUid = document.RowId,
				BaseId = document.Id,
				Description = document.Description,
				SubjectWebpage = document.SubjectWebpage,
				CTID = document.CTID,
				Created = document.Created,
				LastUpdated = document.LastUpdated,
				ImageUrl = document.ImageUrl,
				Name = document.Name,
				OwningOrgId = document.OrganizationId
			};
			var statusMessage = "";
			new EntityManager().EntityCacheSave( ec, ref statusMessage );


			new CacheManager().PopulateEntityRelatedCaches( document.RowId );
			//may need to update elastic for creds, etc
			List<string> messages = new List<string>();
			//update Elastic
			if ( Utilities.UtilityManager.GetAppKeyValue( "updatingElasticIndexImmediately", false ) )
				ElasticHelper.Organization_UpdateIndex( document.Id );
			else
				new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_ORGANIZATION, document.Id, 1, ref messages );

		}
		public static ThisEntity GetBySubjectWebpage( string swp )
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( swp ) )
				return entity;
			return EntityMgr.GetBySubjectWebpage( swp );
		}
		#endregion

		#region Search
		public static List<CM.Organization> MicroSearch( MicroSearchInputV2 query, int pageNumber, int pageSize, ref int pTotalRows )
		{
			string pOrderBy = "";
			string filter = "";
			int userId = 0;
			string keywords = query.GetFilterValueString( "Keywords" );
			string orgMbrs = query.GetFilterValueString( "OrgFilters" );

			//user is used to determine if can edit results
			AppUser user = new AppUser();
			user = AccountServices.GetCurrentUser();
			if ( user != null && user.Id > 0 )
				userId = user.Id;
			//this is an option on micro searches to only target orgs associated to the user
			//if ( orgMbrs == "myOrgs" )
			//    SetAuthorizationFilter( user, ref filter, true, true );
			//else
			//    SetAuthorizationFilter( user, ref filter, true, false );

			SetKeywordFilter( keywords, true, ref filter );

			return Mgr.Search( filter, pOrderBy, pageNumber, pageSize, ref pTotalRows );
		}
		public static List<object> Autocomplete( string keyword = "", int maxTerms = 25, int widgetId = 0 )
		{
			int userId = 0;
			string where = "";
			int totalRows = 0;
			AppUser user = AccountServices.GetCurrentUser();
			if ( user != null && user.Id > 0 )
				userId = user.Id;
			//SetAuthorizationFilter( user, ref where );


			if ( UtilityManager.GetAppKeyValue( "usingElasticOrganizationSearch", false ) )
			{
				return ElasticHelper.OrganizationAutoComplete( keyword, maxTerms, ref totalRows );
			}
			else
			{
				SetKeywordFilter( keyword, true, ref where );
				//string keywords = ServiceHelper.HandleApostrophes( keyword );
				//if ( keywords.IndexOf( "%" ) == -1 )
				//	keywords = "%" + keywords.Trim() + "%";
				//where = string.Format( " (base.name like '{0}') ", keywords );

				return EntityMgr.Autocomplete( where, 1, maxTerms, userId, ref totalRows );
			}
		}
		public static List<MicroProfile> GetMicroProfile( List<int> organizationIDs )
		{

			List<MicroProfile> list = new List<MicroProfile>();

			if ( organizationIDs != null && organizationIDs.Count() > 0 )
			{
				string orgList = "";
				foreach ( var item in organizationIDs )
				{
					orgList += item + ",";
				}
				string filter = string.Format( " base.Id in ({0})", orgList.Trim( ',' ) );
				int pTotalRows = 0;
				List<OrganizationSummary> orgs = OrganizationManager.MainSearch( filter, "", 1, 500, ref pTotalRows );
				if ( orgs != null )
				{
					foreach ( var item in orgs )
					{
						list.Add( new MicroProfile() { Id = item.Id, Name = item.Name, RowId = item.RowId } );
					}
				}
			}

			return list;
		}

		public static List<OrganizationSummary> Search( MainSearchInput data, ref int pTotalRows )
		{
			if ( UtilityManager.GetAppKeyValue( "usingElasticOrganizationSearch", false ) )
			{
				return ElasticHelper.OrganizationSearch( data, ref pTotalRows );
			}
			else
			{
				return DoSearch( data, ref pTotalRows );
			}
		}
		/// <summary>
		/// Main search
		/// </summary>
		/// <param name="data"></param>
		/// <param name="pTotalRows"></param>
		/// <returns></returns>
		public static List<OrganizationSummary> DoSearch( MainSearchInput data, ref int pTotalRows )
		{
			string where = "";
			int userId = 0;

			AppUser user = AccountServices.GetCurrentUser();
			if ( user != null && user.Id > 0 )
				userId = user.Id;

			//only target full entities
			where = " ( base.EntityStateId = 3 ) ";

			SetKeywordFilter( data.Keywords, true, ref where );

			//SetAuthorizationFilter( user, ref where );
			SearchServices.HandleCustomFilters( data, 59, ref where );

			SetPropertiesFilter( data, ref where );
			SearchServices.SetRolesFilter( data, ref where );

			SetBoundariesFilter( data, ref where );
			SetFrameworksFilter( data, ref where );

			SetOrgServicesFilter( data, ref where );

			//check for org category (credentially, or QA). Only valid if one item
			//SetOrgCategoryFilter( data, ref where ); //Not updated - I'm not sure we're still using this. - NA 5/12/2017

			LoggingHelper.DoTrace( 5, thisClassName + ".Search(). Filter: " + where );
			return EntityMgr.MainSearch( where, data.SortOrder, data.StartPage, data.PageSize, ref pTotalRows );
		}

		private static void SetBoundariesFilter( MainSearchInput data, ref string where )
		{
			string AND = "";
			if ( where.Length > 0 )
				AND = " AND ";
			string template = " ( base.Id in ( SELECT  [EntityBaseId] FROM [dbo].[Entity_AddressSummary] where EntityTypeId = 2 AND  [Longitude] < {0} and [Longitude] > {1} and [Latitude] < {2} and [Latitude] > {3} ) ) ";

			var boundaries = SearchServices.GetBoundaries( data, "bounds" );
			if ( boundaries.IsDefined )
			{
				where = where + AND + string.Format( template, boundaries.East, boundaries.West, boundaries.North, boundaries.South );
			}
		}
		/// <summary>
		/// determine which results a user may view, and eventually edit
		/// </summary>
		/// <param name="data"></param>
		/// <param name="user"></param>
		/// <param name="where"></param>

		private static void SetPropertiesFilter( MainSearchInput data, ref string where )
		{
			string searchCategories = UtilityManager.GetAppKeyValue( "orgSearchCategories", "7,8,9,30," );
			SearchServices.SetPropertiesFilter( data, 1, searchCategories, ref where );

		}
		private static void SetFrameworksFilter( MainSearchInput data, ref string where )
		{
			string AND = "";
			//string codeTemplate2 = "  (base.Id in (SELECT c.id FROM [dbo].[Entity.FrameworkItemSummary] a inner join Entity b on a.EntityId = b.Id inner join Organization c on b.EntityUid = c.RowId where [CategoryId] = {0} and ([CodeGroup] in ({1})  OR ([CodeId] in ({2}) )  )) ) ";

			string codeTemplate = " (base.Id in (SELECT c.id FROM [dbo].[Entity_ReferenceFramework_Summary] a inner join Entity b on a.EntityId = b.Id inner join Organization c on b.EntityUid = c.RowId where [CategoryId] = {0} and ([CodeGroup] in ({1})  OR ([ReferenceFrameworkId] in ({2}) )  )) ) ";

			//Updated to use FiltersV2
			string next = "";
			string groups = "";
			if ( where.Length > 0 )
				AND = " AND ";
			var categoryID = 0;
			foreach ( var filter in data.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.FRAMEWORK ) )
			{
				var item = filter.AsCodeItem();
				var isTopLevel = filter.GetValueOrDefault<bool>( "IsTopLevel", false );
				if ( item.CategoryId == 10 || item.CategoryId == 11 )
				{
					categoryID = item.CategoryId;
					if ( isTopLevel )
						groups += item.Id + ",";
					else
						next += item.Id + ",";
				}
			}

			if ( next.Length > 0 )
				next = next.Trim( ',' );
			else
				next = "''";
			if ( groups.Length > 0 )
				groups = groups.Trim( ',' );
			else
				groups = "''";
			if ( groups != "''" || next != "''" )
			{
				where = where + AND + string.Format( codeTemplate, categoryID, groups, next );
			}

			/* //Retained for reference
			foreach ( MainSearchFilter filter in data.Filters.Where( s => s.CategoryId == 10 || s.CategoryId == 11 ) )
			{
				string next = "";
				if ( where.Length > 0 )
					AND = " AND ";
				foreach ( string item in filter.Items )
				{
					next += item + ",";
				}
				next = next.Trim( ',' );
				where = where + AND + string.Format( codeTemplate, filter.CategoryId, next );
			}
			*/
		}
		private static void SetKeywordFilter( string keywords, bool isBasic, ref string where )
		{
			if ( string.IsNullOrWhiteSpace( keywords ) )
				return;
			//OR base.Description like '{0}'  
			string text = " (base.name like '{0}'  OR base.SubjectWebpage like '{0}' OR base.id in ( select EntityBaseId from Organization_AlternatesNames where TextValue like '{0}') ) ";

			string orgDepts = "( base.Id in (SELECT o.Id FROM dbo.Entity e INNER JOIN dbo.[Entity.AgentRelationship] ear ON e.Id = ear.EntityId INNER JOIN dbo.Organization o ON e.EntityUid = o.RowId WHERE ear.RelationshipTypeId = {0} AND o.StatusId < 4) )";
			bool isCustomSearch = false;
			//use Entity.SearchIndex for all
			string indexFilter = " OR (base.Id in (SELECT c.id FROM [dbo].[Entity.SearchIndex] a inner join Entity b on a.EntityId = b.Id inner join Organization c on b.EntityUid = c.RowId where (b.EntityTypeId = 2 AND ( a.TextValue like '{0}' OR a.[CodedNotation] like '{0}' ) ))) ";

			//for ctid, needs a valid ctid or guid
			if ( keywords.IndexOf( "ce-" ) > -1 && keywords.Length == 39 )
			{
				text = " ( CTID = '{0}' ) ";
				isCustomSearch = true;
			}
			else if ( ServiceHelper.IsValidGuid( keywords ) )
			{
				text = " ( CTID = 'ce-{0}' ) ";
				isCustomSearch = true;
			}
			else if ( keywords.ToLower() == "[hascredentialregistryid]" )
			{
				text = " ( len(Isnull(CredentialRegistryId,'') ) = 36 ) ";
				isCustomSearch = true;
			}
			else if ( keywords.ToLower() == "has subsidiary" )
			{
				text = string.Format( orgDepts, 21 );
				isCustomSearch = true;
			}
			else if ( keywords.ToLower() == "has department" )
			{
				text = string.Format( orgDepts, 20 );
				isCustomSearch = true;
			}
			string AND = "";
			if ( where.Length > 0 )
				AND = " AND ";

			keywords = ServiceHelper.HandleApostrophes( keywords );
			if ( keywords.IndexOf( "%" ) == -1 && !isCustomSearch )
			{
				keywords = SearchServices.SearchifyWord( keywords );
				//keywords = "%" + keywords.Trim() + "%";
				//keywords = keywords.Replace( "&", "%" ).Replace( " and ", "%" ).Replace( " in ", "%" ).Replace( " of ", "%" );
				//keywords = keywords.Replace( " - ", "%" );
				//keywords = keywords.Replace( " % ", "%" );
			}

			//same for now, but will chg
			if ( isBasic || isCustomSearch )
				where = where + AND + string.Format( " ( " + text + " ) ", keywords );
			else
				where = where + AND + string.Format( " ( " + text + indexFilter + " ) ", keywords );
		}

		/// <summary>
		/// Note these are now properties!!!
		/// Changed the view to use the properties view, and proper category
		/// </summary>
		/// <param name="data"></param>
		/// <param name="where"></param>
		private static void SetOrgServicesFilter( MainSearchInput data, ref string where )
		{
			string AND = "";
			string template = " ( base.Id in ( SELECT  [OrganizationId] FROM [dbo].[Organization.ServiceSummary]  where [CodeId] in ({0}))) ";
			//don't really need categoryId - yet

			//Updated to use FiltersV2
			string next = "";
			if ( where.Length > 0 )
				AND = " AND ";
			foreach ( var filter in data.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ) )
			{
				var item = filter.AsCodeItem();
				if ( item.CategoryId == 6 )
				{
					next += item.Id + ",";
				}
			}
			next = next.Trim( ',' );
			if ( !string.IsNullOrWhiteSpace( next ) )
			{
				where = where + AND + string.Format( template, next );
			}

		}
		private static void SetOrgCategoryFilter( MainSearchInput data, ref string where )
		{
			string AND = "";
			//check for org category (credentially, or QA). Only valid if one item
			var qaSettings = data.GetFilterValues_Strings( "qualityAssurance" );
			if ( qaSettings.Count == 1 )
			{
				//ignore unless one filter
				string item = qaSettings[ 0 ];
				if ( where.Length > 0 )
					AND = " AND ";
				if ( item == "includeNormal" ) //IsAQAOrganization = false
					where = where + AND + " ([IsAQAOrganization] = 0 OR [CredentialCount] > 0) ";
				else if ( item == "includeQualityAssurance" )  //IsAQAOrganization = true
					where = where + AND + " ([IsAQAOrganization] = 1) ";
			}
		}
		#endregion
		#region methods for new API
		public static MCD.OrganizationDetail GetDetailForAPI( int id, bool skippingCache = false )
		{
			var org = GetDetail( id, skippingCache );
			return MapToAPI( org );

		}
		public static MCD.OrganizationDetail GetDetailByCtidForApi( string ctid, bool skippingCache = false )
		{
			var org = GetDetailByCtid( ctid, skippingCache );
			return MapToAPI( org );
		}
		private static MCD.OrganizationDetail MapToAPI( Organization org )
		{
			var baseSiteURL = UtilityManager.GetAppKeyValue( "baseSiteURL" );


			var output = new MCD.OrganizationDetail()
			{
				Id = org.Id,
				Name = org.Name,
				Description = org.Description,
				SubjectWebpage = org.SubjectWebpage,
				EntityTypeId = 2,
				//EntityType="Organization"

			};
			output.CTDLType = org.AgentDomainType;
			output.AgentSectorType = ServiceHelper.MapPropertyLabelLinks( org.AgentSectorType, "organization" );
			output.AgentType = ServiceHelper.MapPropertyLabelLinks( org.AgentType, "organization" );
			output.AgentPurpose = org.AgentPurpose;
			output.AgentPurposeDescription = org.AgentPurposeDescription;
			output.AlternateName = org.AlternateName;
			output.AvailabilityListing = org.AvailabilityListings;
			output.CTID = org.CTID;
			if ( org.Emails != null && org.Emails.Any() )
				output.Email = org.Emails.Select( s => s.TextValue ).ToList();

			output.EntityLastUpdated = org.EntityLastUpdated;
			output.EntityStateId = org.EntityStateId;
			output.EntityTypeId = org.EntityTypeId;
			output.FoundingDate = org.FoundingDate;
			output.FriendlyName = org.FriendlyName;
			//identifiers
			output.Identifier = org.Identifier;
			output.DUNS = org.ID_DUNS;
			output.FEIN = org.ID_FEIN;
			output.IPEDSID = org.ID_IPEDSID;
			output.ISICV4 = org.ID_ISICV4;
			output.LEICode = org.ID_LEICode;
			output.NECS = org.ID_NECS;
			output.OPEID = org.ID_OPEID;
			//
			output.ParentOrganization = ServiceHelper.MapToEntityReference( org.ParentOrganizations );
			output.Department = ServiceHelper.MapToEntityReference( org.OrganizationRole_Dept );
			output.ParentOrganization = ServiceHelper.MapToEntityReference( org.OrganizationRole_Subsidiary );
			//
			output.Image = org.ImageUrl;
			output.IndustryType = ServiceHelper.MapReferenceFrameworkLabelLink( org.IndustryType, "organization" );
			output.IsReferenceVersion = org.IsReferenceVersion;
			//output.Jurisdiction = org.Jurisdiction;
			if ( org.Keyword != null && org.Keyword.Any() )
				output.Keyword = ServiceHelper.MapPropertyLabelLinks( org.Keyword, "organization" );


			output.MissionAndGoalsStatement = org.MissionAndGoalsStatement;
			output.MissionAndGoalsStatementDescription = org.MissionAndGoalsStatementDescription;
			//this is NOT pertinent to organization
			//output.OrganizationId = org.OrganizationId;
			//output.OrganizationName = org.OrganizationName;
			//output.OrganizationSubjectWebpage = "";
			output.ServiceType = ServiceHelper.MapPropertyLabelLinks( org.ServiceType, "organization" );
			output.SameAs = ServiceHelper.MapTextValueProfileTextValue( org.SameAs );
			output.SocialMedia = ServiceHelper.MapTextValueProfileTextValue( org.SocialMediaPages );

			output.TransferValueStatement = org.TransferValueStatement;
			output.TransferValueStatementDescription = org.TransferValueStatementDescription;

			org.FriendlyName = HttpUtility.UrlEncode( org.Name );
			//searches
			var links = new List<MCD.LabelLink>();
			output.Connections = null;
			if ( org.TotalCredentials > 0 )
			{
				//output.CredentialsSearch = ServiceHelper.MapEntitySearchLink( org.Id, org.FriendlyName, org.TotalCredentials, "Owns/Offers {0} Credential(s)", "credential" );

				//output.Connections.Add( output.CredentialsSearch );
				ServiceHelper.MapEntitySearchLink( org.Id, org.FriendlyName, org.TotalCredentials, "Owns/Offers {0} Credential(s)", "credential", ref links );
			}
			if ( org.TotalLopps > 0 )
			{
				ServiceHelper.MapEntitySearchLink( org.Id, org.FriendlyName, org.TotalLopps, "Owns/Offers {0} Learning Opportunity(ies)", "learningopportunity", ref links );
			}
			if ( org.TotalAssessments > 0 )
				ServiceHelper.MapEntitySearchLink( org.Id, org.FriendlyName, org.TotalAssessments, "Owns/Offers {0} Assesment(s)", "assessment", ref links );

			if ( org.TotalPathwaySets > 0 )
			{
				ServiceHelper.MapEntitySearchLink( org.Id, org.FriendlyName, org.TotalPathwaySets, "Owns {0} Pathway Set(s)", "pathwayset", ref links );
			}
			if ( org.TotalPathways > 0 )
			{
				ServiceHelper.MapEntitySearchLink( org.Id, org.FriendlyName, org.TotalPathways, "Owns {0} Pathway(s)", "pathway", ref links );
			}
			if ( org.TotalTransferValueProfiles > 0 )
			{
				ServiceHelper.MapEntitySearchLink( org.Id, org.FriendlyName, org.TotalTransferValueProfiles, "Owns {0} Transfer Value Profiles(s)", "transfervalue", ref links );
			}

			if ( org.TotalFrameworks > 0 )
				ServiceHelper.MapEntitySearchLink( org.Id, org.FriendlyName, org.TotalFrameworks, "Owns {0} Competency Framework(s)", "competencyframework", ref links );

			if ( org.TotalConceptSchemes > 0 )
				ServiceHelper.MapEntitySearchLink( org.Id, org.FriendlyName, org.TotalConceptSchemes, "Owns {0} Concept Scheme(s)", "conceptscheme", ref links );

			if ( org.RevokesCredentials > 0 )
				ServiceHelper.MapEntitySearchLink( org.Id, org.FriendlyName, org.RevokesCredentials, "Revokes {0} Credential(s)", "credential", ref links, "11" );
			//if ( org.RegulatesCredentials > 0 )
			//	ServiceHelper.MapEntitySearchLink( org.Id, org.FriendlyName, org.RegulatesCredentials, "Regulates {0} Credential(s)", "credential", ref links, "12" );
			if ( org.RenewsCredentials > 0 )
				ServiceHelper.MapEntitySearchLink( org.Id, org.FriendlyName, org.RenewsCredentials, "Renews {0} Credential(s)", "credential", ref links, "13" );
			
			//
			if ( links.Any() )
				output.Connections = links;
			//need to handle other roles: renews, revokes, regulates
			//QA performed
			output.QAPerformed = new List<MCD.LabelLink>();
			links = new List<MCD.LabelLink>();
			if ( org.QAPerformedOnCredentialsCount > 0 )
				ServiceHelper.MapQAPerformedLink( org.Id, org.FriendlyName, org.QAPerformedOnCredentialsCount, "QA Identified as Performed on {0} Credential{s}", "credential", ref links );

			if ( org.QAPerformedOnOrganizationsCount > 0 )
				ServiceHelper.MapQAPerformedLink( org.Id, org.FriendlyName, org.QAPerformedOnOrganizationsCount, "QA Identified as Performed on {0} Organization(s)", "organization", ref links );
			if ( org.QAPerformedOnAssessmentsCount > 0 )
				ServiceHelper.MapQAPerformedLink( org.Id, org.FriendlyName, org.QAPerformedOnAssessmentsCount, "QA Identified as Performed on {0} Assessment(s)", "assessment", ref links );

			if ( org.QAPerformedOnLoppsCount > 0 )
				ServiceHelper.MapQAPerformedLink( org.Id, org.FriendlyName, org.QAPerformedOnLoppsCount, "QA Identified as Performed on {0} Learning Opportunity(ies)", "learningopportunity", ref links );

			if ( links.Any() )
				output.QAPerformed = links;
			//QA received
			//==> need to exclude 30-published by 
			if ( org.OrganizationRole_Recipient.Any() )
			{
				output.QAReceived = new List<MCD.OrganizationRoleProfile>();
				foreach ( var item in org.OrganizationRole_Recipient )
				{
					var orp = new MCD.OrganizationRoleProfile()
					{
						Label = string.IsNullOrWhiteSpace( item.ProfileName ) ? item.ParentSummary : item.ProfileName,
						Description = item.Description ?? ""
					};
					if ( string.IsNullOrWhiteSpace( orp.Label ) )
					{
						if ( item.ActingAgent != null && item.ActingAgent.Id > 0 )
						{
							orp.Label = item.ActingAgent.Name;
							orp.Description = item.ActingAgent.Description;
						}
					}
					if ( string.IsNullOrEmpty( item.ActingAgent.CTID ) )
						orp.URL = item.ActingAgent.SubjectWebpage;
					else
						orp.URL = baseSiteURL + string.Format( "organization/detail/{0}", org.Id );
					bool isPublishedByRole = false;
					if ( item.AgentRole != null && item.AgentRole.Items.Any() )
					{
						foreach ( var ar in item.AgentRole.Items )
						{
							//no link
							if ( ar.Id == 30 )
							{
								//if published by, probably will not have other roles!
								//continue;
								isPublishedByRole = true;
								break;
							}
							orp.Roles.Add( new MCD.LabelLink() { Label = ar.Name } );
						}
					}
					if ( !isPublishedByRole )
						output.QAReceived.Add( orp );
				}
			}
			//
			MapAddress( org, ref output );
			//cost 
			MapCostManifest( org, ref output );

			//conditions
			MapConditionManifest( org, ref output );

			//process profiles
			MapProcessProfiles( org, ref output );

			//
			MapJurisdictions( org, ref output );
			//
			MapVerificationServiceProfile( org, ref output );

			return output;
		}

		public static void MapCostManifest( Organization org, ref MCD.OrganizationDetail output )
		{
			if ( org.HasConditionManifest == null || !org.HasConditionManifest.Any() )
			{
				return;
			}
			output.HasCostManifest = ServiceHelper.MapToCostManifests( org.HasCostManifest );
			//foreach (var item in org.HasCostManifest)
			//{
			//	//just in case
			//	if ( string.IsNullOrWhiteSpace( item.CostDetails ) )
			//		continue;
			//	var cm = new ME.CostManifest()
			//	{
			//		Name = item.Name,
			//		Description = item.Description,
			//		CostDetails = item.CostDetails,
			//		StartDate = item.StartDate,
			//		EndDate = item.EndDate,
			//		CTID = item.CTID,
			//	};
			//	//CostProfiles
			//	if ( item.EstimatedCost != null && item.EstimatedCost.Any() )
			//	{
			//		cm.EstimatedCost = ServiceHelper.MapToCostProfiles( item.EstimatedCost );
			//	}

			//	output.HasCostManifest.Add( cm );

			//}
		}

		private static void MapConditionManifest( Organization org, ref MCD.OrganizationDetail output )
		{
			if ( org.HasCostManifest == null || !org.HasCostManifest.Any() )
			{
				return;
			}
			output.HasConditionManifest = new List<ME.ConditionManifest>();
			foreach ( var item in org.HasConditionManifest )
			{
				//just in case
				if ( string.IsNullOrWhiteSpace( item.Name ) )
					continue;
				var cm = new ME.ConditionManifest()
				{
					Name = item.Name,
					Description = item.Description,
					SubjectWebpage = item.SubjectWebpage,
					CTID = item.CTID,
				};
				//condition profiles


				output.HasConditionManifest.Add( cm );

			}
		}
		private static void MapAddress( Organization org, ref MCD.OrganizationDetail output )
		{
			//addresses
			if ( org.Addresses.Any() )
			{
				foreach ( var item in org.Addresses )
				{
					var address = new MCD.Address()
					{
						Address1 = item.Address1,
						PostOfficeBoxNumber = item.PostOfficeBoxNumber,
						City = item.City,
						SubRegion = item.SubRegion ?? "",
						AddressRegion = item.AddressRegion,
						PostalCode = item.PostalCode,
						Country = item.Country,
						Latitude = item.Latitude,
						Longitude = item.Longitude
					};
					if ( item.HasContactPoints() )
					{
						//???
						//output.ContactPoint = new List<MCD.ContactPoint>();
						address.ContactPoint = new List<MCD.ContactPoint>();
						foreach ( var cp in item.ContactPoint )
						{
							var cpOutput = new MCD.ContactPoint()
							{
								ContactType = cp.ContactType,
								Email = cp.Emails,
								PhoneNumber = cp.PhoneNumbers,
								SocialMediaPage = cp.SocialMediaPages
							};
							address.ContactPoint.Add( cpOutput );
						}
					}
					output.Address.Add( address );
				}

			}
			//handle 'orphans' contact points not associated with an address
			if ( org.ContactPoint.Any() )
			{
				//where to put these?
				var address = new MCD.Address();
				address.ContactPoint = new List<MCD.ContactPoint>();

				foreach ( var cp in org.ContactPoint )
				{
					var cpOutput = new MCD.ContactPoint()
					{
						ContactType = string.IsNullOrWhiteSpace( cp.ContactType ) ? cp.ProfileName : cp.ContactType,
						Email = cp.Emails,
						PhoneNumber = cp.PhoneNumbers,
						SocialMediaPage = cp.SocialMediaPages
					};
					if ( cp.PhoneNumber.Any() )
						cpOutput.PhoneNumber = ServiceHelper.MapTextValueProfileToStringList( cp.PhoneNumber );
					if ( cp.Email.Any() )
						cpOutput.Email = ServiceHelper.MapTextValueProfileToStringList( cp.Email );
					if ( cp.SocialMedia.Any() )
						cpOutput.SocialMediaPage = ServiceHelper.MapTextValueProfileToStringList( cp.SocialMedia );

					if ( cpOutput.Email.Any() || cpOutput.PhoneNumber.Any() || cpOutput.SocialMediaPage.Any() )
						address.ContactPoint.Add( cpOutput );
				}
				output.Address.Add( address );

			}

		}

		private static void MapProcessProfiles( Organization org, ref MCD.OrganizationDetail output)
		{
			//process profiles
			if ( org.AppealProcess.Any() )
			{
				output.AppealProcess = ServiceHelper.MapProcessProfile( org.Id, org.AppealProcess );
			}
			if ( org.AdministrationProcess.Any() )
			{
				output.AdministrationProcess = ServiceHelper.MapProcessProfile( org.Id, org.AdministrationProcess );
			}
			if ( org.ComplaintProcess.Any() )
			{
				output.ComplaintProcess = ServiceHelper.MapProcessProfile( org.Id, org.ComplaintProcess );
			}
			if ( org.DevelopmentProcess.Any() )
			{
				output.DevelopmentProcess = ServiceHelper.MapProcessProfile( org.Id, org.DevelopmentProcess );
			}
			if ( org.MaintenanceProcess.Any() )
			{
				output.MaintenanceProcess = ServiceHelper.MapProcessProfile( org.Id, org.MaintenanceProcess );
			}
			if ( org.ReviewProcess.Any() )
			{
				output.ReviewProcess = ServiceHelper.MapProcessProfile( org.Id, org.ReviewProcess );
			}
			if ( org.RevocationProcess.Any() )
			{
				output.RevocationProcess = ServiceHelper.MapProcessProfile( org.Id, org.RevocationProcess );
			}
		}
		private static void MapJurisdictions( Organization org, ref MCD.OrganizationDetail output )
		{
			if ( org.Jurisdiction != null && org.Jurisdiction.Any() )
			{
				output.Jurisdiction = ServiceHelper.MapJurisdiction( org.Jurisdiction );

			}
			//return if no assertions
			if ( org.JurisdictionAssertions == null || !org.JurisdictionAssertions.Any() )
			{
				return;
			}
			//TODO - return all in a group or individual?
			output.JurisdictionAssertion = ServiceHelper.MapJurisdiction( org.JurisdictionAssertions, "OfferedIn" );
			//OR
			
			var assertedIn = org.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_AccreditedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
			{
				output.AccreditedIn = ServiceHelper.MapJurisdiction( assertedIn, "AccreditedIn" );
			}
			//
			assertedIn = org.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_ApprovedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
				output.ApprovedIn = ServiceHelper.MapJurisdiction( assertedIn, "ApprovedIn" );
			//
			assertedIn = org.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
				output.RecognizedIn = ServiceHelper.MapJurisdiction( assertedIn, "RecognizedIn" );
			//
			assertedIn = org.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
				output.RegulatedIn = ServiceHelper.MapJurisdiction( assertedIn, "RegulatedIn" );
		}
		private static void MapVerificationServiceProfile( Organization org, ref MCD.OrganizationDetail output )
		{
			if ( org.VerificationServiceProfiles == null || !org.VerificationServiceProfiles.Any() )
				return;
			output.VerificationServiceProfiles = new List<MCD.VerificationServiceProfile>();
			foreach ( var item in org.VerificationServiceProfiles )
			{
				MCD.VerificationServiceProfile vsp = new MCD.VerificationServiceProfile()
				{
					DateEffective = item.DateEffective,
					Description = item.Description,
					HolderMustAuthorize = item.HolderMustAuthorize,
					SubjectWebpage = item.SubjectWebpage,
					VerificationDirectory = item.VerificationDirectory,
					VerificationMethodDescription = item.VerificationMethodDescription,
					VerificationService = item.VerificationServiceUrl
				};
				if ( item.Jurisdiction != null && item.Jurisdiction.Any() )
				{
					vsp.Jurisdiction = ServiceHelper.MapJurisdiction( item.Jurisdiction );
				}
				if ( item.Region != null && item.Region.Any() )
				{
					vsp.Region = ServiceHelper.MapJurisdiction(  item.Region );
				}
				//CostProfiles
				if(item.EstimatedCost != null && item.EstimatedCost.Any())
				{
					vsp.EstimatedCost = ServiceHelper.MapToCostProfiles( item.EstimatedCost );
				}

				//OfferredIn
				if ( item.JurisdictionAssertions != null && item.JurisdictionAssertions.Any() )
				{
					vsp.OfferedIn = ServiceHelper.MapJurisdiction( item.JurisdictionAssertions, "OfferedIn" );
				}
				//
				if ( item.OfferedByAgent!= null && !string.IsNullOrWhiteSpace( item.OfferedByAgent.Name ) )
					vsp.OfferedBy = ServiceHelper.MapToEntityReference( item.OfferedByAgent );

				vsp.VerifiedClaimType = ServiceHelper.MapPropertyLabelLinks( item.ClaimType, "organization",false );
				if ( item.TargetCredential != null && !item.TargetCredential.Any() )
				{
					foreach ( var target in item.TargetCredential )
					{
						if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
							vsp.TargetCredential.Add( ServiceHelper.MapToEntityReference( target ) );
					}
					//vsp.TargetCredential = ServiceHelper.MapToEntityReference( item.TargetCredential );
				}

				output.VerificationServiceProfiles.Add( vsp );
			}

		}

		#endregion
		public static CM.Organization GetBasic( int id )
        {
            CM.Organization entity = Mgr.GetForSummary( id );
            return entity;

        }
        public static CM.Organization GetForSummaryWithRoles( int id )
        {
            return Mgr.GetForSummary( id, true );
		}
		
        public static CM.Organization GetDetail( int id, bool skippingCache = false )
        {
            int cacheMinutes = UtilityManager.GetAppKeyValue( "organizationCacheMinutes", 0 );
            DateTime maxTime = DateTime.Now.AddMinutes( cacheMinutes * -1 );

            string key = "organization_" + id.ToString();

            if ( skippingCache == false
                && HttpRuntime.Cache[ key ] != null && cacheMinutes > 0 )
            {
                var cache = new CachedOrganization();
                try
                {
					cache = ( CachedOrganization )HttpRuntime.Cache[ key ];
                    if ( cache.lastUpdated > maxTime )
                    {
                        LoggingHelper.DoTrace( 6, string.Format( "===OrganizationServices.GetDetail === Using cached version of Organization, Id: {0}, {1}", cache.Item.Id, cache.Item.Name ) );

                        return cache.Item;
                    }
                }
                catch ( Exception ex )
                {
                    LoggingHelper.DoTrace( 6, thisClassName + ".GetDetail. Get OrganizationCache === exception " + ex.Message );
                }
            }
            else
            {
                LoggingHelper.DoTrace( 8, thisClassName + string.Format( ".GetDetail === Retrieving full version of Organization, Id: {0}", id ) );
            }

            DateTime start = DateTime.Now;

            CM.Organization entity = Mgr.GetDetail( id );

            DateTime end = DateTime.Now;
            int elasped = ( end - start ).Seconds;
			//Cache the output if more than specific seconds,
			//NOTE need to be able to force it for imports
			//&& elasped > 2
			if ( key.Length > 0 && cacheMinutes > 0 )
			{
				try
				{
					var newCache = new CachedOrganization()
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

							LoggingHelper.DoTrace( 5, string.Format( "===OrganizationServices.GetDetail $$$ Updating cached version of Organization, Id: {0}, {1}", entity.Id, entity.Name ) );

						}
						else
						{
							LoggingHelper.DoTrace( 5, string.Format( "===OrganizationServices.GetDetail ****** Inserting new cached version of Organization, Id: {0}, {1}", entity.Id, entity.Name ) );

							System.Web.HttpRuntime.Cache.Insert( key, newCache, null, DateTime.Now.AddMinutes( cacheMinutes ), TimeSpan.Zero );
						}
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 6, thisClassName + ".GetDetail. Updating OrganizationCache === exception " + ex.Message );
				}
			}
			else
            {
                LoggingHelper.DoTrace( 7, string.Format( "===OrganizationServices.GetDetail $$$$$$ skipping caching of Organization, Id: {0}, {1}, elasped:{2}", entity.Id, entity.Name, elasped ) );
            }

            return entity;
        }
    }
    public class CachedOrganization
    {
        public CachedOrganization()
        {
            lastUpdated = DateTime.Now;
        }
        public DateTime lastUpdated { get; set; }
        public Organization Item { get; set; }

    }
}
