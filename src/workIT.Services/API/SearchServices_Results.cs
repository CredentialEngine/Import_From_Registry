using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Caching;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using CF = workIT.Factories;
using workIT.Models.Common;
using workIT.Models.Search;
using workIT.Models.API;
using System.Text.RegularExpressions;
using workIT.Utilities;

namespace workIT.Services.API
{
	//Search Services dealing with results and result transformation
	public partial class SearchServices
	{
		public static string credentialFinderMainSite = UtilityManager.GetAppKeyValue( "credentialFinderMainSite" );

		public static MainQueryResponse TranslateMainSearchResultsToAPIResults( MainSearchResults mainResultsData, JObject debug = null )
		{
			//Translate the outer layer and debugging stuff
			debug = debug ?? new JObject();

			//Prevent exceptions
			mainResultsData = mainResultsData ?? new MainSearchResults();
			var translatedResultsData = new MainQueryResponse()
			{
				TotalResults = mainResultsData.TotalResults,
				RelatedItems = mainResultsData.RelatedItems?.Count() == 0 ? null : mainResultsData.RelatedItems
			};

			//Translate each result
			foreach ( var sourceResult in mainResultsData.Results )
			{
				try
				{
					var translatedResult = TranslateMainSearchResultToAPIResult_Generic2( mainResultsData, sourceResult );
					translatedResult.Add( "debug:originalData", JObject.FromObject( sourceResult ) );
					translatedResultsData.Results.Add( translatedResult );
				}
				catch ( Exception ex )
				{
					translatedResultsData.Results.Add( new JObject()
					{
						{ "Error translating this result", ex.Message },
						{ "Inner exception", ex.InnerException?.Message },
						{ "Raw Data", JObject.FromObject( sourceResult ) }
					} );
				}
			}

			//Return the data
			return translatedResultsData;
		}
		//

		private static JObject JObjectify( object input )
		{
			return JObject.FromObject( input, new JsonSerializer() { NullValueHandling = NullValueHandling.Ignore } );
		}
		//

		public static JObject TranslateMainSearchResultToAPIResult_Generic2( MainSearchResults mainResultsData, MainSearchResult sourceResult )
		{
			//Basic Information common to all top-level types
			var result = JObjectify( TranslateBasicData( mainResultsData, sourceResult ) );

			//Type-Specific Information
			switch ( mainResultsData.SearchType )
			{
				case "organization":
				TranslateLocations( sourceResult, result, "Address" );
				AddIfPresent( sourceResult.Properties, "ResultImageUrl", result, "Image" );
				break;

				case "credential":
				TranslateLocations( sourceResult, result, "AvailableAt" );
				AddIfPresent( sourceResult.Properties, "ResultImageUrl", result, "Image" );
				AddIfPresent( sourceResult.Properties, "ResultIconUrl", result, "Meta_Icon" );
				if( sourceResult.Properties.ContainsKey("HasBadge") && (bool) sourceResult.Properties["HasBadge"] == true )
				{
					result.Add( "Meta_HasVerificationBadge", true );
				}
				break;

				case "assessment":
				case "learningopportunity":
				TranslateLocations( sourceResult, result, "AvailableAt" );
				break;

				default: break;
			}

			//Gray Buttons
			TranslateGrayButtonsToResultPills( mainResultsData, sourceResult, result );

			return result;
		}
		//

		private static BaseAPIType TranslateBasicData( MainSearchResults mainResultsData, MainSearchResult sourceResult )
		{
			//Hold result
			var result = new BaseAPIType();

			//Meta info
			result.Meta_Language = "en"; //Not sure how to obtain this
			result.Meta_Id = sourceResult.RecordId;
			try { result.EntityLastUpdated = DateTime.Parse( GetValueIfPresent( sourceResult.Properties, "T_LastUpdated" ) ?? DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss" ) ); } catch { }
			try { result.Meta_StateId = int.Parse( GetValueIfPresent( sourceResult.Properties, "T_StateId" ) ?? "0" ); } catch { }

			//Common info
			result.BroadType = GetValueIfPresent( sourceResult.Properties, "T_BroadType" );
			result.CTDLType = GetValueIfPresent( sourceResult.Properties, "T_CTDLType" );
			result.CTDLTypeLabel = GetValueIfPresent( sourceResult.Properties, "T_CTDLTypeLabel" );
			//21-07-12 - CTID is ctid in the properties
			result.CTID = GetValueIfPresent( sourceResult.Properties, "ctid" ) ?? GetValueIfPresent( sourceResult.Properties, "CTID" ) ?? "unknown";
			result.Name = sourceResult.Name;
			result.Meta_FriendlyName = sourceResult.FriendlyName;
			result.Description = sourceResult.Description;

			//Owner
			//Populate both OwnedByLabel and OwnedBy since it's not clear which one the interface should/will need
			try
			{
				if ( sourceResult.Properties.ContainsKey( "Owner" ) )
				{
					if ( mainResultsData.SearchType == "competencyframework" )
					{
						var owner = ( JObject ) sourceResult.Properties[ "Owner" ];
						if ( owner != null )
						{
							var ownerName = CompetencyFrameworkServicesV2.GetEnglishString( owner[ "ceterms:name" ], "" );
							if ( ownerName != "" && ownerName != "Unknown Organization" )
							{
								var friendlyName = Factories.BaseFactory.FormatFriendlyTitle( ownerName );
								result.OwnedByLabel = new LabelLink()
								{
									Label = CompetencyFrameworkServicesV2.GetEnglishString( owner[ "ceterms:name" ], "" ),
									URL = "/resources/" + owner[ "ceterms:ctid" ].ToString() + (string.IsNullOrWhiteSpace(friendlyName) ? "" : "/" + friendlyName)
								};
								result.OwnedBy = new AJAXSettings()
								{
									Total = 1,
									Values = new List<object>()
								{
									new LabelLink()
									{
										Label = ownerName,
										URL = "/resources/" + owner[ "ceterms:ctid" ].ToString()+ (string.IsNullOrWhiteSpace(friendlyName) ? "" : "/" + friendlyName)
									}
								}
								};
							}
						}
					}
					else
					{
						var ownerName = GetValueIfPresent( sourceResult.Properties, "Owner" ) ?? "";
						var friendlyName = Factories.BaseFactory.FormatFriendlyTitle( ownerName );

						result.OwnedByLabel = new LabelLink()
						{
							Label = GetValueIfPresent( sourceResult.Properties, "Owner" ) ?? "",
							URL = "/organization/" + (GetValueIfPresent( sourceResult.Properties, "OwnerId" ) ?? "0") + ( string.IsNullOrWhiteSpace( friendlyName ) ? "" : "/" + friendlyName )
						};
						result.OwnedBy = new AJAXSettings()
						{
							Total = 1,
							Values = new List<object>()
								{
									new LabelLink()
									{
										Label = GetValueIfPresent( sourceResult.Properties, "Owner" ) ?? "",
										URL = "/organization/" + (GetValueIfPresent( sourceResult.Properties, "OwnerId" ) ?? "0") + ( string.IsNullOrWhiteSpace( friendlyName ) ? "" : "/" + friendlyName )
									}
								}
						};
					}
				}
			}
			catch( Exception ex ) 
			{ 
				
			}

			return result;
		}
		//

		private static void TranslateLocations( MainSearchResult sourceResult, JObject holder, string propertyName )
		{
			try
			{
				if ( sourceResult.Properties.ContainsKey( "T_Locations" ) )
				{
					var addressList = new List<JObject>();
					foreach ( var rawAddress in ( ( JArray ) sourceResult.Properties[ "T_Locations" ] ).Select( m => ( JObject ) m ).ToList() )
					{
						//Convert the address
						var address = new JObject();
						AddIfPresent( rawAddress, "Name", address, "Name" );
						AddIfPresent( rawAddress, "Description", address, "Description" );
						AddIfPresent( rawAddress, "PostOfficeBoxNumber", address, "PostOfficeBoxNumber" );
						AddIfPresent( rawAddress, "StreetAddress", address, "StreetAddress" );
						AddIfPresent( rawAddress, "AddressLocality", address, "AddressLocality" );
						AddIfPresent( rawAddress, "AddressRegion", address, "AddressRegion" );
						AddIfPresent( rawAddress, "PostalCode", address, "PostalCode" );
						AddIfPresent( rawAddress, "AddressCountry", address, "AddressCountry" );
						AddIfPresent( rawAddress, "Latitude", address, "Latitude", m => Double.Parse( m ?? "0.0" ) );
						AddIfPresent( rawAddress, "Longitude", address, "Longitude", m => Double.Parse( m ?? "0.0" ) );

						//Convert the identifier
						if ( rawAddress.ContainsKey( "Identifier" ) )
						{
							try
							{
								var rawIdentifierData = JArray.FromObject( rawAddress[ "Identifier" ] ).Select( m => ( JObject ) m ).ToList();
								var identifierData = new List<JObject>();
								foreach( var rawIdentifier in rawIdentifierData )
								{
									var identifierItem = new JObject();
									AddIfPresent( rawIdentifier, "IdentifierTypeName", identifierItem, "IdentifierTypeName" );
									AddIfPresent( rawIdentifier, "IdentifierValueCode", identifierItem, "IdentifierValueCode" );
									AddIfPresent( rawIdentifier, "IdentifierType", identifierItem, "IdentifierType" );
									if ( identifierItem.Properties().Count() > 0 )
									{
										identifierData.Add( identifierItem );
									}
								}

								//Store the identifier
								if ( identifierData.Count() > 0 )
								{
									address.Add( "Identifier", JArray.FromObject( identifierData ) );
								}
							}
							catch ( Exception ex )
							{
								holder[ "debug:TranslateLocationsIdentifierError" ] = holder[ "debug:TranslateLocationsIdentifierError" ] ?? new JArray();
								((JArray) holder[ "debug:TranslateLocationsIdentifierError" ] ).Add( ex.Message );
							}
						}

						//Store the address
						address = JObjectify( address ); //Ensure null values are stripped out
						addressList.Add( address );
					}

					//Store the address list
					if ( addressList.Count() > 0 )
					{
						holder[ propertyName ] = JArray.FromObject( addressList );
					}
				}
			}
			catch ( Exception ex )
			{
				holder.Add( "debug:TranslateLocationsError", ex.Message );
			}
		}
		//

		public static JObject TranslateMainSearchResultToAPIResult_Generic<T>( MainSearchResults mainResultsData, MainSearchResult sourceResult, Func<int, bool, T> getDetailForAPI, bool skippingCache = false )
		{
			var detailData = getDetailForAPI( sourceResult.RecordId, skippingCache );
			var result = JObjectify( detailData );
			TranslateGrayButtonsToResultPills( mainResultsData, sourceResult, result );
			return result;
		}
		//

		//Encapsulating these into their own methods because we may end up wanting to do something more complex for them later
		public static JObject TranslateMainSearchResultToAPIResult_CompetencyFramework( MainSearchResults mainResultsData, MainSearchResult sourceResult, JObject translatedResult )
		{
			var detailData = new JObject() { { "TBD", "TBD" } };
			return JObjectify( detailData );
		}
		//

		public static JObject TranslateMainSearchResultToAPIResult_ConceptScheme( MainSearchResults mainResultsData, MainSearchResult sourceResult, JObject translatedResult )
		{
			var detailData = new JObject() { { "TBD", "TBD" } };
			return JObjectify( detailData );
		}
		//

		public static JObject TranslateMainSearchResultToAPIResult_PathwaySet( MainSearchResults mainResultsData, MainSearchResult sourceResult, JObject translatedResult )
		{
			var detailData = new JObject() { { "TBD", "TBD" } };
			return JObjectify( detailData );
		}
		//

		public static JObject TranslateMainSearchResultToAPIResult_Pathway( MainSearchResults mainResultsData, MainSearchResult sourceResult, JObject translatedResult )
		{
			var detailData = new JObject() { { "TBD", "TBD" } };
			return JObjectify( detailData );
		}
		//

		public static void TranslateGrayButtonsToResultPills( MainSearchResults mainResultsData, MainSearchResult sourceResult, JObject translatedResult )
		{
			var pills = new List<Models.API.TagSet>();
			var grayButtonErrors = new List<JObject>();
			var finderBaseSiteURL = ConfigHelper.GetConfigValue( "credentialFinderMainSiteBaseURL", "/" );
			foreach( var button in sourceResult.Buttons ?? new List<Models.Helpers.SearchResultButton>() )
			{
				var set = new Models.API.TagSet();

				set.Label = button.CategoryLabel;
				set.Total = button.TotalItems;
				set.Values = new List<Models.API.TagItem>();
				set.Icon = TranslateGrayButtonIcon( button.CategoryType );

				try
				{
					switch ( button.HandlerType )
					{
						case "handler_RenderCheckboxFilter":
						case "handler_RenderExternalCodeFilter":
							set.TagItemType = "tagItemType:FilterItem";
							foreach ( var item in button.Items )
							{
								set.Values.Add( new Models.API.TagItem()
								{
									Label = GetValueIfPresent( item, "ItemCodeTitle" ) ?? GetValueIfPresent( item, "ItemLabel" ),
									FilterID = int.Parse( GetValueIfPresent( item, "CategoryId" ) ?? "0" ),
									FilterItemID = int.Parse( GetValueIfPresent( item, "ItemCodeId" ) ?? "0" ),
									FilterItemText = GetValueIfPresent( item, "ItemCodeTitle" )
								} );
							}
							break;

						case "handler_RenderDetailPageLink":
						case "handler_RenderConnection":
							set.TagItemType = "tagItemType:Link";
							foreach ( var item in button.Items )
							{
								set.Values.Add( new Models.API.TagItem()
								{
									Label = GetValueIfPresent( item, "TargetLabel" ),
									URL = finderBaseSiteURL + GetValueIfPresent( item, "TargetType" ) + "/" + ( GetValueIfPresent( item, "TargetId" ) == "0" ? GetValueIfPresent( item, "TargetCTID" ) : GetValueIfPresent( item, "TargetId" ) )
								} );
							}
							break;

						case "handler_RenderQualityAssurance":
							set.TagItemType = "tagItemType:Link";
							foreach ( var item in button.Items )
							{
								set.Values.Add( new Models.API.TagItem()
								{
									Label = GetValueIfPresent( item, "Relationship" ) + " " + GetValueIfPresent( item, "Agent" ),
									URL = finderBaseSiteURL + "organization/" + GetValueIfPresent( item, "AgentId" )
								} );
							}
							break;

						case "handler_GetRelatedItemsViaAJAX":
							set.TagItemType = "tagItemType:Link";
							set.URL = "/Search/GetTagSetItems";
							set.QueryData = new TagSetRequest()
							{
								TargetType = GetValueIfPresent( button.RenderData, "TargetEntityType" ),
								RecordId = sourceResult.RecordId,
								SearchType = mainResultsData.SearchType
							};
							break;

						default:
							grayButtonErrors.Add( new JObject()
							{
								{ "Error matching handler", button.HandlerType }
							} );
							break;
					}
				}
				catch ( Exception ex )
				{
					grayButtonErrors.Add( new JObject()
					{
						{ "Error Translating Tag Item", ex.Message },
						{ "Inner Exception", ex.InnerException?.Message ?? "" },
						{ "Raw Item", JObject.FromObject( button ) }
					} );
				}

				pills.Add( set );
			}

			translatedResult.Add( "SearchTags", JArray.FromObject( pills ) );

			if( grayButtonErrors.Count() > 0 )
			{
				translatedResult.Add( "debug:grayButtonErrors", JArray.FromObject( grayButtonErrors ) );
			}
		}
		//

		public static List<Models.API.TagItem> GetTagSetItems( TagSetRequest request, int maxResults = 10, JObject debug = null )
		{
			debug = debug ?? new JObject();
			debug[ "Farthest Point" ] = "Translating Request";
			var result = new List<Models.API.TagItem>();
			var enumType = TranslateNewTargetTypeToOldTargetType( request.TargetType );
			debug[ "Farthest Point" ] = "Getting Data";
			var rawData = workIT.Services.SearchServices.GetTagSet( request.SearchType, enumType, request.RecordId, maxResults );

			debug[ "Raw Data" ] = JObject.FromObject( rawData );
			debug[ "Farthest Point" ] = "Processing Data";
			switch ( rawData.Method?.ToLower() ?? "" )
			{
				case "link":
				foreach ( var item in rawData.EntityTagItems )
				{
					if( string.IsNullOrWhiteSpace(item.TargetFriendlyName))
						item.TargetFriendlyName = CF.BaseFactory.FormatFriendlyTitle( item.TargetEntityName );

					result.Add( new Models.API.TagItem()
					{
						Label = item.TargetEntityName,
						URL = credentialFinderMainSite + item.TargetEntityType.ToLower() + "/" + item.TargetEntityBaseId + "/" + item.TargetFriendlyName
					} );
				}
				break;

				case "direct":
				foreach( var item in rawData.Items )
				{
					var friendlyName = CF.BaseFactory.FormatFriendlyTitle( item.Label );
					result.Add( new Models.API.TagItem()
					{
						Label = item.Label,
						URL = credentialFinderMainSite + request.SearchType + "/" + request.RecordId + "/" + friendlyName
					} );
				}
				break;

				case "qaperformed":
				foreach( var item in rawData.QAItems )
				{
						//21-10-18 mp - this is wrong. If always from org search, then link is to the target
						var friendlyName = CF.BaseFactory.FormatFriendlyTitle( item.TargetEntityName );
						result.Add( new Models.API.TagItem()
					{
						//Label = item.AgentToTargetRelationship + " by " + item.TargetEntityName, //TODO: Verify that this is the right combination
						//Label = item.AgentToTargetRelationship + item.TargetEntityName, //TargetEntityType
						Label = item.AgentToTargetRelationship + " '" + item.TargetEntityName + "' ", //TargetEntityType
						URL = credentialFinderMainSite + item.TargetEntityType + "/" + item.TargetEntityBaseId + "/" + friendlyName
						} );
				}
				break;

				default: break;
			}

			return result;
		}
		//

		public static List<MapFilter> TranslateGeoCoordinatesListToMapFilterList( List<GeoCoordinates> source )
		{
			var result = new List<MapFilter>();

			foreach( var sourceItem in source ?? new List<GeoCoordinates>() )
			{
				result.Add( new MapFilter()
				{
					BBoxCenterLatitude = sourceItem.Latitude,
					BBoxCenterLongitude = sourceItem.Longitude,
					BBoxNorth = Decimal.ToDouble( sourceItem.Bounds?.North ?? 0m ),
					BBoxEast = Decimal.ToDouble( sourceItem.Bounds?.East ?? 0m ),
					BBoxSouth = Decimal.ToDouble( sourceItem.Bounds?.South ?? 0m ),
					BBoxWest = Decimal.ToDouble( sourceItem.Bounds?.West ?? 0m ),
					Label = sourceItem.ToponymName,
					Region = sourceItem.Region,
					Country = sourceItem.Country
				} );
			}

			return result;
		}
		//

		private static Services.SearchServices.TagTypes TranslateNewTargetTypeToOldTargetType( string targetType )
		{
			//Same transformation that happens in SearchV2.cshtml
			switch ( ( targetType ?? "" ).ToLower() )
			{
				case "assessment": return Services.SearchServices.TagTypes.ASSESSMENT;
				case "assessmentprofile": return Services.SearchServices.TagTypes.ASSESSMENT;
				case "competency": return Services.SearchServices.TagTypes.COMPETENCIES;
				case "estimatedcost": return Services.SearchServices.TagTypes.COST;
				case "financialassistance": return Services.SearchServices.TagTypes.FINANCIAL;
				case "hasassessmentprofile": return Services.SearchServices.TagTypes.HAS_ASSESSMENT;
				case "has_assessment": return Services.SearchServices.TagTypes.HAS_ASSESSMENT;
				case "hascredential": return Services.SearchServices.TagTypes.HAS_CREDENTIAL;
				case "has_credential": return Services.SearchServices.TagTypes.HAS_CREDENTIAL;
				case "haslearningopportunityprofile": return Services.SearchServices.TagTypes.HAS_LOPP;				
				case "has_lopp": return Services.SearchServices.TagTypes.HAS_LOPP;
				case "hastransfervalue": return Services.SearchServices.TagTypes.HASTRANSFERVALUE;

				case "tvphascredential": return Services.SearchServices.TagTypes.TVPHASCREDENTIAL;
				case "tvphasassessment": return Services.SearchServices.TagTypes.TVPHASASSESSMENT;
				case "tvphaslopp": return Services.SearchServices.TagTypes.TVPHASLOPP;
				case "tvphaslearningopportunity": return Services.SearchServices.TagTypes.TVPHASLOPP;
				case "tvphastransferintermediary": return Services.SearchServices.TagTypes.TVPHASTRANSFERINTERMEDIARY;
				case "learningopportunityprofile": return Services.SearchServices.TagTypes.LEARNINGOPPORTUNITY;

                case "partofcollection": return Services.SearchServices.TagTypes.PARTOFCOLLECTION;
                case "pathway": return Services.SearchServices.TagTypes.PATHWAY;
				case "pathwayset": return Services.SearchServices.TagTypes.PATHWAYSET;
				case "qualityassuranceperformed": return Services.SearchServices.TagTypes.QAPERFORMED;
				case "transfervalue": return Services.SearchServices.TagTypes.TRANSFERVALUE;
				case "datasetprofile_credential": return Services.SearchServices.TagTypes.DATASETPROFILE_CREDENTIAL;
				case "datasetprofile": return Services.SearchServices.TagTypes.DATASETPROFILE;
				default: return ( Services.SearchServices.TagTypes ) Enum.Parse( typeof( Services.SearchServices.TagTypes ), targetType, true );
			}
		}
		//

		private static string TranslateGrayButtonIcon( string categoryType )
		{
			//Same transformation that happens in SearchV2.cshtml
			switch ( ( categoryType ?? "").ToLower() )
			{
				case "alignedcompetency": return "fa-project-diagram";
				case "alignedframework": return "fa-project-diagram";
				case "assessment": return "fa-tasks";
				case "assessmentdeliverytype": return "fa-paper-plane";
				case "assessmentmethodtype": return "fa-desktop";
				case "assessmentusetype": return "fa-calculator";
				case "audienceleveltype": return "fa-layer-group";
				case "audiencetype": return "fa-users";
				case "competency": return "fa-cogs";
				case "competencyframeworkalignment": return "fa-project-diagram";
				case "competencyalignent": return "fa-project-diagram";
				case "concept": return "fa-stream";
				case "conceptscheme": return "fa-stream";
				case "connection": return "fa-share-alt";
				case "credential": return "fa-award";
				case "earningsprofile": return "fa-chart-line";
				case "employmentoutcomeprofile": return "fa-chart-line";
				case "estimatedcost": return "fa-money-bill-wave";
				case "estimatedduration": return "fa-clock";
				case "financialassistance": return "fa-money-bill-wave";
				case "holdersprofile": return "fa-chart-line";
				case "industrytype": return "fa-city";
				case "instructionalprogramtype": return "fa-school";
				case "learningdeliverytype": return "fa-paper-plane";
				case "learningmethodtype": return "fa-chalkboard-teacher";
				case "learningopportunity": return "fa-graduation-cap";
				case "occupationtype": return "fa-briefcase";
				case "organizationoffers": return "fa-hands";
				case "organizationowns": return "fa-key";
				case "organizationroles": return "fa-check-circle";
				case "organizationtype": return "fa-building";
				case "orgquality": return "fa-check-circle";
				case "outcomedata": return "fa-chart-line";
				case "ownedby": return "fa-key";
				case "pathway": return "fa-project-diagram";
				case "quality": return "fa-check-circle";
				case "relatedconcept": return "fa-stream";
				case "relatedconceptscheme": return "fa-stream";
				case "scoringmethodtype": return "fa-pencil-alt";
				case "sectortype": return "fa-warehouse";
				case "servicetype": return "fa-users-cog";
				case "subject": return "fa-clipboard-list";
				default: return "fa-share-alt";
			}
		}
		//

		public static string GetValueIfPresent( JObject container, string key )
		{
			try
			{
				if ( container[ key ] != null )
				{
					var value = container[ key ].ToString();
					return string.IsNullOrWhiteSpace( value ) ? null : value;
				}
			}
			catch { }
			return null;
		}
		//

		public static string GetValueIfPresent( Dictionary<string, object> container, string key )
		{
			return GetValueIfPresent( JObject.FromObject( container ), key );
		}
		//

		public static void AddIfPresent( JObject source, string sourceKey, JObject destination, string destinationKey, Func<string, JToken> conversionMethod = null )
		{
			var value = GetValueIfPresent( source, sourceKey );
			if( !string.IsNullOrWhiteSpace( value ) )
			{
				destination.Add( destinationKey, conversionMethod == null ? value : conversionMethod( value ) );
			}
		}
		//

		public static void AddIfPresent( Dictionary<string, object> source, string sourceKey, JObject destination, string destinationKey, Func<string, JToken> conversionMethod = null )
		{
			AddIfPresent( JObject.FromObject( source ), sourceKey, destination, destinationKey, conversionMethod );
		}
		//

	}
}
