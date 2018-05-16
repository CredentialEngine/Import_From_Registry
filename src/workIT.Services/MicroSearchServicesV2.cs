using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Search;
using workIT.Models.Node;
using workIT.Models.Node.Interface;

using MC = workIT.Models.Common;
using MP = workIT.Models.ProfileModels;
using workIT.Factories;
using System.Reflection;

namespace workIT.Services
{
	public class MicroSearchServicesV2
	{
		//Do a micro search and return results
		public static List<MicroProfile> DoMicroSearch( MicroSearchInputV2 query, ref int totalResults, ref bool valid, ref string status )
		{
			//Ensure there is a query
		if ( query.Filters.Count() == 0 )
			{
				valid = false;
				status = "No search parameters found!";
				return null;
			}

			//Sanitize query
			foreach ( var item in query.Filters )
			{
				item.Name = ServiceHelper.CleanText( item.Name ?? "" );
				item.Value = ServiceHelper.CleanText( item.Value as string ?? "" );
			}

			//Maybe useful. Based on notes in Search page.
			var mainSearchTypeCode = 0;
			switch ( ( query.ParentSearchType ?? "" ).ToLower() )
			{
				case "credential": mainSearchTypeCode = 1; break;
				case "organization": mainSearchTypeCode = 2; break;
				case "assessment": mainSearchTypeCode = 3; break;
				case "learningopportunity": mainSearchTypeCode = 7; break; 
				default:
					mainSearchTypeCode = 1;
					break;
			}

			totalResults = 0;
			switch ( query.SearchType )
			{
				case "RegionSearch":
					{
						var locationType = query.GetFilterValueString( "LocationType" ).Split( ',' ).ToList();
						var results = new ThirdPartyApiServices().GeoNamesSearch( query.GetFilterValueString( "Keywords" ), query.PageNumber, query.PageSize, locationType, ref totalResults, false );
						return results.ConvertAll( m => ConvertRegionToMicroProfile( m ) );
					}
				case "IndustrySearch":
					{
						//TODO - getAll should be set to false if used by a search view (ie credential)
						bool getAll = query.IncludeAllCodes;
						var results = EnumerationServices.Industry_Search( mainSearchTypeCode,
							query.GetFilterValueInt( "HeaderId" ), 
							query.GetFilterValueString( "Keywords" ), 
							query.PageNumber, 
							query.PageSize, 
							ref totalResults, 
							getAll );
						return results.ConvertAll( m => ConvertCodeItemToMicroProfile( m ) );
					}
				case "OccupationSearch":
					{
						//TODO - IncludeAllCodes should be set to false if used by a search view (ie credential)
						var results = EnumerationServices.Occupation_Search( query.GetFilterValueInt( "HeaderId" ), 
							query.GetFilterValueString( "Keywords" ), 
							query.PageNumber, 
							query.PageSize, ref totalResults, query.IncludeAllCodes );
						return results.ConvertAll( m => ConvertCodeItemToMicroProfile( m ) );
					}
				case "CIPSearch":
					{
						//TODO - need entity type

						var results = EnumerationServices.CIPS_Search( 
							mainSearchTypeCode, 
							query.GetFilterValueInt( "HeaderId" ),
							query.GetFilterValueString( "Keywords" ), 
							query.PageNumber, 
							query.PageSize, 
							ref totalResults, 
							query.IncludeAllCodes );
						return results.ConvertAll( m => ConvertCodeItemToMicroProfile( m ) );
					}
				
				//case "LearningOpportunitySearch":
				//case "LearningOpportunityHasPartSearch":
				//	{
				//		var results = LearningOpportunityServices.Search( query.GetFilterValueString( "Keywords" ), query.PageNumber, query.PageSize, ref totalResults );
				//		return results.ConvertAll( m => ConvertProfileToMicroProfile( m ) );
				//	}
				
				default:
					totalResults = 0;
					valid = false;
					status = "Unable to find Search Type";
					return null;
			}
		}
		//

		//public MicroProfile SaveMicroProfile( ProfileContext context, Dictionary<string, object> selectors, string searchType, string property, bool allowMultipleSavedItems, ref bool valid, ref string status )
		//{
		//	AppUser user = AccountServices.GetUserFromSession();
		//	switch ( searchType )
		//	{
		//		//case "RegionSearch":
		//		//	{
		//		//		var js = new JurisdictionServices();
		//		//		var region = new MC.GeoCoordinates()
		//		//		{
		//		//			ParentEntityId = context.Profile.RowId,
		//		//			GeoNamesId = GetIntValue( selectors[ "GeoNamesId" ] ),
		//		//			Name = ( string ) selectors[ "Name" ],
		//		//			IsException = ( bool ) selectors[ "IsException" ],
		//		//			ToponymName = ( string ) selectors[ "ToponymName" ],
		//		//			Region = ( string ) selectors[ "Region" ],
		//		//			Country = ( string ) selectors[ "Country" ],
		//		//			//Latitude = ( double ) ( ( decimal ) selectors[ "Latitude" ] ),
		//		//			//Longitude = ( double ) ( ( decimal ) selectors[ "Longitude" ] ),
		//		//			Latitude = double.Parse( selectors[ "Latitude" ].ToString() ),
		//		//			Longitude = double.Parse( selectors[ "Longitude" ].ToString() ),
		//		//			Url = ( string ) selectors[ "Url" ]
		//		//		};
		//		//		valid = js.GeoCoordinates_Add( region, context.Profile.RowId, AccountServices.GetUserFromSession().Id, ref status );
		//		//		return valid ? ConvertRegionToMicroProfile( js.GeoCoordiates_Get( region.Id ) ) : null;
		//		//	}
		//		case "IndustrySearch":
		//		case "OccupationSearch":
		//		case "CIPSearch":
		//			{
		//				var categoryID = 0;
		//				switch ( searchType )
		//				{
		//					case "IndustrySearch":
		//						categoryID = CodesManager.PROPERTY_CATEGORY_NAICS;
		//						break;
		//					case "OccupationSearch":
		//						categoryID = CodesManager.PROPERTY_CATEGORY_SOC;
		//						break;
		//					case "CIPSearch":
		//						categoryID = CodesManager.PROPERTY_CATEGORY_CIP;
		//						break;
		//					default:
		//						break;
		//				}
		//				var rawData = new ProfileServices().FrameworkItem_Add( context.Profile.RowId, 
		//					categoryID, 
		//					GetIntValue( selectors[ "CodeId" ] ), 
		//					AccountServices.GetUserFromSession(), 
		//					ref valid, 
		//					ref status );
		//				return ConvertEnumeratedItemToMicroProfile( rawData );
		//			}
				
		//		case "AssessmentSearch":
		//			{
		//				var target = GetProfileLinkFromSelectors( selectors );
		//				var rawData = new ProfileServices().Assessment_Add( context.Profile.RowId, target.Id, AccountServices.GetUserFromSession(), ref valid, ref status, allowMultipleSavedItems );
		//				if ( rawData == 0 )
		//				{
		//					valid = false;
		//					return null;
		//				}
		//				else
		//				{
		//					//if was added to a credential, then add to a condition profile
		//					//TODO - need to handle with process profiles
		//					if ( context.Profile.TypeName == "Credential" )
		//					{
		//						UpsertConditionProfileForAssessment( context.Profile.RowId, target.Id, user, ref status );

		//					}

		//					var results = AssessmentServices.Get( target.Id );
		//					return ConvertProfileToMicroProfile( results );
		//				}
		//			}
				
		//		case "LearningOpportunitySearch":
		//			{
		//				var target = GetProfileLinkFromSelectors( selectors );
		//				var newId = new ProfileServices().LearningOpportunity_Add( context.Profile.RowId, target.Id, user, ref valid, ref status, allowMultipleSavedItems );

		//				if ( newId == 0 )
		//				{
		//					valid = false;
		//					return null;
		//				}
		//				else
		//				{
		//					//if was added to a credential, then add to a condition profile
		//					if ( context.Profile.TypeName == "Credential")
		//					{
		//						UpsertConditionProfileForLearningOpp( context.Profile.RowId, target.Id, user, ref status );

		//					}
							

		//					var results = LearningOpportunityServices.GetForMicroProfile( target.Id );
		//					return ConvertProfileToMicroProfile( results );
		//				}
		//			}
		//		case "LearningOpportunityHasPartSearch":
		//			{
		//				//TODO - can we get rowId instead?
		//				Guid rowId = context.Parent.RowId;

		//				var target = GetProfileLinkFromSelectors( selectors );
		//				var rawData = new LearningOpportunityServices().AddLearningOpportunity_AsPart( context.Profile.RowId, target.Id, AccountServices.GetUserFromSession(), ref valid, ref status );
		//				if ( rawData == 0 )
		//				{
		//					valid = false;
		//					return null;
		//				}
		//				else
		//				{
		//					var results = LearningOpportunityServices.Get( target.Id );
		//					return ConvertProfileToMicroProfile( results );
		//				}
		//			}
		//		case "QACredentialSearch":
		//		case "CredentialSearch":
		//			{
		//				var target = GetProfileLinkFromSelectors( selectors );
		//				//use context.Profile.RowId for adding a credential to a condition profile or process profile
		//				var newId = new ProfileServices().EntityCredential_Save( context.Profile.RowId, target.Id, AccountServices.GetUserFromSession(), allowMultipleSavedItems, ref valid, ref status );
		//				if ( newId == 0 )
		//				{
		//					valid = false;
		//					return null;
		//				}
		//				else
		//				{
		//					//??
		//					var entity = ProfileServices.EntityCredential_Get( newId );
		//					//???
		//					return ConvertProfileToMicroProfile( entity.Credential );
		//				}
		//			}
		//		case "QAOrganizationSearch":
		//		case "OrganizationSearch":
		//			{
		//				return SaveMicroProfiles_ForOrgSearch( context, selectors, searchType, property, allowMultipleSavedItems, ref valid, ref status );

		//				//will need different actions dependent on profile type
		//				//var target = GetProfileLinkFromSelectors( selectors );
		//				//switch ( context.Profile.TypeName )
		//				//{
		//				//	case "Organization":
		//				//	{
		//				//		//need parent, and new child to connect, but need role, ie dept, subsidiary, or ????
		//				//		//NEW - need code to handle adding an org to an entity, like a credential, or role

		//				//		int roleId = Entity_AgentRelationshipManager.ROLE_TYPE_DEPARTMENT;
		//				//		if ( property == "OwningOrganization" )
		//				//		{
		//				//			//just return the org
		//				//			var entity = OrganizationServices.GetForSummary( target.Id );
		//				//			return ConvertProfileToMicroProfile( entity );
		//				//		}
		//				//		else if ( property == "Department" )
		//				//			roleId = Entity_AgentRelationshipManager.ROLE_TYPE_DEPARTMENT;
		//				//		else
		//				//			roleId = Entity_AgentRelationshipManager.ROLE_TYPE_SUBSIDIARY;

		//				//		var newId = new OrganizationServices().AddChildOrganization( context.Main.RowId, target.RowId, roleId, AccountServices.GetUserFromSession(), ref valid, ref status );

		//				//		if ( newId == 0 )
		//				//		{
		//				//			valid = false;
		//				//			return null;
		//				//		}
		//				//		else
		//				//		{
		//				//			//??
		//				//			var entity = OrganizationServices.GetForSummary( target.Id );
		//				//			return ConvertProfileToMicroProfile( entity );
		//				//		}
		//				//	}
							
		//				//	case "Credential":
		//				//		{
		//				//			//actually, if credential, only current action is for owning org - which is not a child relationship. Just return the org?
		//				//			//??
		//				//			var entity = OrganizationServices.GetForSummary( target.Id );
		//				//			return ConvertProfileToMicroProfile( entity );
		//				//		}
								
		//				//	case "ConditionProfile":
		//				//		{
		//				//			//conditon profile also has org as part of entity, no child. What to return to prevent error?
		//				//			var entity = OrganizationServices.GetForSummary( target.Id );
		//				//			return ConvertProfileToMicroProfile( entity );
		//				//		}
		//				//		//break;

		//				//	case "AgentRoleProfile_Recipient":
		//				//		{
		//				//			//??what else
		//				//			var entity = OrganizationServices.GetForSummary( target.Id );
		//				//			return ConvertProfileToMicroProfile( entity );
		//				//		}
		//				//		//break;
		//				//	default:
		//				//		break;
		//				//}
		//				//return null;
		//			}
		//			//no ajax save 
		//		//case "CredentialAssetSearch":
		//		//	{
						
		//		//	}
		//		case "CostProfileSearch":
		//			{
		//				var target = GetProfileLinkFromSelectors( selectors );
		//				//use 
		//				var rawData = new ProfileServices().CostProfile_Copy( target.RowId, context.Profile.RowId, AccountServices.GetUserFromSession(), ref valid, ref status );
		//				if ( rawData == 0 )
		//				{
		//					valid = false;
		//					return null;
		//				}
		//				else
		//				{
		//					var results = ProfileServices.CostProfile_Get( target.Id );
		//					return ConvertProfileToMicroProfile( results );
		//				}
		//			}
		//		case "ConditionManifestSearch":
		//			{
		//				var target = GetProfileLinkFromSelectors( selectors );
		//				//use 
		//				var rawData = new ConditionManifestServices().Entity_CommonCondition_Add( context.Profile.RowId, target.Id, AccountServices.GetUserFromSession(), ref valid, ref status );
		//				if ( rawData == 0 )
		//				{
		//					valid = false;
		//					return null;
		//				}
		//				else
		//				{
		//					var results = ConditionManifestServices.GetBasic( target.Id );
		//					return ConvertProfileToMicroProfile( results );
		//				}
		//			}
		//		case "CostManifestSearch":
		//			{
		//				var target = GetProfileLinkFromSelectors( selectors );
		//				//use 
		//				var rawData = new CostManifestServices().Entity_CommonCost_Add( context.Profile.RowId, target.Id, AccountServices.GetUserFromSession(), ref valid, ref status );
		//				if ( rawData == 0 )
		//				{
		//					valid = false;
		//					return null;
		//				}
		//				else
		//				{
		//					var results = CostManifestServices.GetBasic( target.Id );
		//					return ConvertProfileToMicroProfile( results );
		//				}
		//			}
		//		default:
		//			valid = false;
		//			status = "Unable to find Search Type";
		//			return null;
		//	}
		//}
		//private bool UpsertConditionProfileForAssessment( Guid credentialUid, int entityId, AppUser user, ref string status )
		//{
		//	bool addUpdateCondition = new ConditionProfileServices().UpsertConditionProfileForAssessment( credentialUid, entityId, user, ref status );

		//	if ( addUpdateCondition )
		//	{
		//		//activity tracking prob in latter call?
		//	}

		//	return addUpdateCondition;
		//}
		//private bool UpsertConditionProfileForLearningOpp( Guid credentialUid, int entityId, AppUser user, ref string status )
		//{
		//	bool addUpdateCondition = new ConditionProfileServices().UpsertConditionProfileForLearningOpp( credentialUid, entityId, user, ref status );

		//	if (addUpdateCondition)
		//	{
		//		//activity tracking prob in latter call?
		//	}

		//	return addUpdateCondition;
		//}
		/// <summary>
		/// Handle saves from an organization search
		/// </summary>
		/// <param name="context"></param>
		/// <param name="selectors"></param>
		/// <param name="searchType"></param>
		/// <param name="property"></param>
		/// <param name="allowMultipleSavedItems"></param>
		/// <param name="valid"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		//public static MicroProfile SaveMicroProfiles_ForOrgSearch( ProfileContext context, Dictionary<string, object> selectors, string searchType, string property, bool allowMultipleSavedItems, ref bool valid, ref string status )
		//{
		//	//will need different actions dependent on profile type
		//	ProfileLink target = GetProfileLinkFromSelectors( selectors );
		//	switch ( context.Profile.TypeName )
		//	{
		//		case "Organization":
		//		{
		//			//need parent, and new child to connect, but need role, ie dept, subsidiary, or ????
		//			//NEW - need code to handle adding an org to an entity, like a credential, or role

		//			int roleId = Entity_AgentRelationshipManager.ROLE_TYPE_DEPARTMENT;
		//			if ( property == "OwningOrganization" )
		//			{
		//				//just return the org
		//				var entity = OrganizationServices.GetForSummary( target.Id );
		//				return ConvertProfileToMicroProfile( entity );
		//			}
					
		//			else if( property == "Department" )
		//				roleId = Entity_AgentRelationshipManager.ROLE_TYPE_DEPARTMENT;
		//			else
		//				roleId = Entity_AgentRelationshipManager.ROLE_TYPE_SUBSIDIARY;

		//			var newId = new OrganizationServices().AddChildOrganization( context.Main.RowId, target.RowId, roleId, AccountServices.GetUserFromSession(), ref valid, ref status );

		//			if ( newId == 0 )
		//			{
		//				valid = false;
		//				return null;
		//			}
		//			else
		//			{
		//				//??
		//				var entity = OrganizationServices.GetForSummary( target.Id );
		//				return ConvertProfileToMicroProfile( entity );
		//			}
		//		}

		//		case "Credential":
		//			{
		//				if ( property == "OfferedByOrganization" )
		//				{

		//					if ( new OrganizationServices().EntityAgent_SaveRole( context.Main.RowId,
		//						target.RowId,
		//						Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY,
		//						AccountServices.GetUserFromSession(), ref status ) )
		//					{
		//						var entity = OrganizationServices.GetForSummary( target.Id );
		//						return ConvertProfileToMicroProfile( entity );
		//					}
		//					else
		//					{
		//						valid = false;
		//						return null;
		//					}
		//				}
		//				else
		//				{
		//					//actually, if credential, only current action is for owning org - which is not a child relationship. Just return the org?
		//					//??
		//					var entity = OrganizationServices.GetForSummary( target.Id );
		//					return ConvertProfileToMicroProfile( entity );
		//				}
		//		}

		//		case "ConditionProfile":
		//		{
		//			//conditon profile also has org as part of entity, no child. What to return to prevent error?
		//			var entity = OrganizationServices.GetForSummary( target.Id );
		//			return ConvertProfileToMicroProfile( entity );
		//		}
		//		//break;

		//		case "AgentRoleProfile_Recipient":
		//		{
		//			//??what else
		//			var entity = OrganizationServices.GetForSummary( target.Id );
		//			return ConvertProfileToMicroProfile( entity );
		//		}
		//		//break;
		//		default:
		//			break;
		//	}
		//	return null;
		//}

		//Get data for automated refresh of micro search results
		//public static List<MicroProfile> GetMicroProfiles( string searchType, ProfileContext context, string propertyName, ref bool valid, ref string status )
		//{
		//	//Get all items for property and context combination
		//	var items = new List<ProfileLink>();
		//	var profile = EditorServices.GetProfile( context, true, ref valid, ref status );
		//	foreach( var property in profile.GetType().GetProperties() )
		//	{
		//		if(	property.Name == propertyName )
		//		{
		//			try
		//			{
		//				items = ( List<ProfileLink> ) property.GetValue( profile );
		//			}
		//			catch { }
		//		}
		//	}

		//	//Get micro profiles
		//	return GetMicroProfiles( searchType, items, ref valid, ref status );
		//}
		//

		//Get data for initial display of micro search results
		//public static List<MicroProfile> GetMicroProfiles( string searchType, List<ProfileLink> items, ref bool valid, ref string status )
		//{
		//	var results = new List<MicroProfile>();
		//	switch ( searchType )
		//	{
		//		case "RegionSearch":
		//			{
		//				var data = new JurisdictionServices().GeoCoordinates_GetList( items.Select( m => m.Id ).ToList() );
		//				foreach ( var item in data )
		//				{
		//					results.Add( ConvertRegionToMicroProfile( item ) );
		//				}
		//				return results;
		//			}
		//		case "IndustrySearch":
		//		case "OccupationSearch":
		//		case "CIPSearch":
		//			{
		//				var data = ProfileServices.FrameworkItem_GetItems( items.Select( m => m.Id ).ToList() );
		//				foreach ( var item in data )
		//				{
		//					results.Add( ConvertEnumeratedItemToMicroProfile( item ) );
		//				}
		//				return results;
		//			}
		//		case "QACredentialSearch":
		//		case "CredentialSearch":
		//			{
		//				foreach ( var item in items )
		//				{

		//					if ( (item.RowId == null || item.RowId == Guid.Empty) && item.Id > 0 ) //No GUID, but ID is present
		//					{
		//						results.Add( ConvertProfileToMicroProfile( CredentialServices.GetBasicCredential( item.Id ) ) );
		//					}
		//					else 
		//					{
		//						results.Add( ConvertProfileToMicroProfile( CredentialServices.GetBasicCredentialAsLink( item.RowId ) ) );
		//					}
		//				}
		//				return results;
		//			}
		//		case "QAOrganizationSearch":
		//		case "OrganizationSearch":
		//			{
		//				foreach(var item in items)
		//				{
		//					results.Add( ConvertProfileToMicroProfile( OrganizationServices.GetLightOrgByRowId( item.RowId.ToString() ) ) );
		//				}
		//				return results;
		//			}
		//		case "AssessmentSearch":
		//			{
		//				foreach ( var item in items )
		//				{
		//					results.Add( ConvertProfileToMicroProfile( AssessmentServices.GetLightAssessmentByRowId( item.RowId.ToString() ) ) );
		//				}
		//				return results;
		//			}
		//		case "ConditionManifestSearch":
		//			{
		//				foreach ( var item in items )
		//				{
		//					results.Add( ConvertProfileToMicroProfile( ConditionManifestServices.GetBasic( item.Id ) ) );
		//				}
		//				return results;
		//			}
		//		case "CostManifestSearch":
		//			{
		//				foreach ( var item in items )
		//				{
		//					results.Add( ConvertProfileToMicroProfile( CostManifestServices.GetBasic( item.Id ) ) );
		//				}
		//				return results;
		//			}
		//		case "LearningOpportunitySearch":
		//		case "LearningOpportunityHasPartSearch":
		//			{
		//				foreach ( var item in items )
		//				{
		//					results.Add( ConvertProfileToMicroProfile( LearningOpportunityServices.GetLightLearningOpportunityByRowId( item.RowId.ToString() ) ) );
		//				}
		//				return results;
		//			}
		//		default:
		//			valid = false;
		//			status = "Unable to detect Microsearch type";
		//			return null;
		//	}
		//}
		//

	
		public static MicroProfile ConvertEnumeratedItemToMicroProfile( MC.EnumeratedItem item )
		{
			var guid = new Guid();
			Guid.TryParse( item.RowId, out guid );

			return new MicroProfile()
			{
				Id = item.Id,
				RowId = guid,
				Name = item.Name,
				Description = item.Description,
				Properties = new Dictionary<string, object>() 
				{ 
					{ "FrameworkCode", item.Value }, 
					{ "Url", item.URL } 
				},
				Selectors = new Dictionary<string, object>()
				{
					{ "CategoryId", item.CodeId },
					{ "CodeId", item.Value },
					{ "RecordId", item.Id }
				}
			};
		}
		//

		public static MicroProfile ConvertCodeItemToMicroProfile( Models.CodeItem item )
		{
			return new MicroProfile()
			{
				Id = item.Id,
				Name = item.Name,
				Description = item.Description,
				Properties = new Dictionary<string, object>()
				{
					{ "FrameworkCode", item.SchemaName },
					{ "Url", item.URL }
				},
				Selectors = new Dictionary<string, object>() {
					//{ "CategoryId", item.Code },
                    { "CategoryId", item.CategoryId },
                    { "Name", item.Name },
                    { "CodeId", item.Id }
				}
			};
		}
		//

		public static MicroProfile ConvertRegionToMicroProfile( MC.GeoCoordinates item )
		{
			return new MicroProfile()
			{
				Id = item.Id,
				RowId = item.RowId,
				Name = item.TitleFormatted,
				Description = item.LocationFormatted,
				Properties = new Dictionary<string, object>()
				{
					{ "Latitude", item.Latitude },
					{ "Longitude", item.Longitude },
					{ "Url", item.GeoURI },
					{ "GeoNamesId", item.GeoNamesId }
				},
				Selectors = new Dictionary<string, object>()
				{
					{ "RecordId", item.Id },
					{ "GeoNamesId", item.GeoNamesId },
					{ "Name", item.Name },
					{ "ToponymName", item.ToponymName },
					{ "Region", item.Region },
					{ "Country", item.Country },
					{ "Latitude", item.Latitude },
					{ "Longitude", item.Longitude },
					{ "Url", item.GeoURI }
				}
			};
		}
		//

		public static MicroProfile ConvertAddressToMicroProfile( MC.Address item )
		{
			return new MicroProfile()
			{
				Id = item.Id,
				RowId = item.RowId,
				Name = item.Name,
				Properties = new Dictionary<string, object>()
				{
					{ "Address1", item.Address1 },
					{ "Address2", item.Address2 },
					{ "City", item.City },
					{ "Region", item.AddressRegion },
					{ "PostalCode", item.PostalCode },
					{ "Country", item.Country }
					//{ "CountryId", item.CountryId }
				}
			};
		}
		//
		public static MicroProfile ConvertCostProfileToMicroProfile( MP.CostProfile item )
		{
			return new MicroProfile()
			{
				Id = item.Id,
				RowId = item.RowId,
				Name = item.ProfileName,
				Heading2 = item.ProfileSummary,
				Description = item.Description,
				Selectors = new Dictionary<string, object>()
				{
					{ "RowId", item.RowId },
					{ "Id", item.Id },
					{ "Name", item.ProfileName },
					{ "TypeName", item.GetType().Name }
				}
			};
		}
		public static MicroProfile ConvertEntityToMicroProfile( MC.Entity item )
		{
			return new MicroProfile()
			{
				Id = item.Id,
				RowId = item.EntityUid,
				Name = item.EntityBaseName,
				Heading2 = item.EntityType,
				Description = "",
				Selectors = new Dictionary<string, object>()
				{
					{ "RowId", item.EntityUid },
					{ "Id", item.Id },
					{ "Name", item.EntityBaseName },
					{ "TypeName", item.GetType().Name }
				}
			};
		}
		//
		public static MicroProfile ConvertConditionManifestToMicroProfile( MC.ConditionManifest item )
		{
			return new MicroProfile()
			{
				Id = item.Id,
				RowId = item.RowId,
				Name = item.ProfileName,
				Heading2 = "", // item.ConditionType,
				Description = item.Description,
				Selectors = new Dictionary<string, object>()
				{
					{ "RowId", item.RowId },
					{ "Id", item.Id },
					{ "Name", item.ProfileName },
					{ "TypeName", item.GetType().Name }
				}
			};
		}
		//
		public static MicroProfile ConvertCostManifestToMicroProfile( MC.CostManifest item )
		{
			return new MicroProfile()
			{
				Id = item.Id,
				RowId = item.RowId,
				Name = item.Name,
				Heading2 = "", 
				Description = item.Description,
				Selectors = new Dictionary<string, object>()
				{
					{ "RowId", item.RowId },
					{ "Id", item.Id },
					{ "Name", item.Name },
					{ "TypeName", item.GetType().Name }
				}
			};
		}
		//
		public static MicroProfile ConvertProfileToMicroProfile( object item )
		{
			
			try
			{
				if ( item == null )
					return new MicroProfile();
				var properties = item.GetType().GetProperties();
				return new MicroProfile()
				{
					Id = TryGetValue<int>( item, properties, "Id" ),
					Name = TryGetValue<string>( item, properties, "Name" ),
					Heading2 = TryGetValue<string>( item, properties, "OrganizationName" ),
					Description = TryGetValue<string>( item, properties, "Description" ),
					RowId = TryGetValue<Guid>( item, properties, "RowId" ),
					Selectors = new Dictionary<string, object>() //Create a faux ProfileLink
					{
						{ "RowId", TryGetValue<Guid>( item, properties, "RowId" ) },
						{ "Id", TryGetValue<int>( item, properties, "Id" ) },
						{ "Name", TryGetValue<string>( item, properties, "Name" ) },
						{ "TypeName", item.GetType().Name }
					}
				};
			}
			catch
			{
				return new MicroProfile()
				{
					Name = "Error retrieving this data",
					Description = "There was an error retrieving this item."
				};
			}
		}
		private static T TryGetValue<T>( object source, PropertyInfo[] properties, string name )
		{
			try
			{
				return ( T ) properties.FirstOrDefault( m => m.Name == name ).GetValue( source );
			}
			catch
			{
				return default( T );
			}
		}
		//

		private static int GetIntValue( object value )
		{
			try
			{
				return ( int ) value;
			}
			catch { }
			try
			{
				return int.Parse( ( string ) value );
			}
			catch { }

			return 0;
		}
		//

		private static ProfileLink GetProfileLinkFromSelectors( Dictionary<string, object> selectors )
		{
			try
			{
				return new ProfileLink()
				{
					Id = GetIntValue( selectors[ "Id" ] ),
					Name = ( string ) selectors[ "Name" ],
					RowId = Guid.Parse( ( string ) selectors[ "RowId" ] ),
					TypeName = ( string ) selectors[ "TypeName" ]
				};
			}
			catch
			{
				return new ProfileLink();
			}
		}
		//
	}
}
