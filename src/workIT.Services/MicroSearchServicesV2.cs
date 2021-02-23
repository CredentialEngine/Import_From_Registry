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
		if ( query.Filters.Count() == 0 && query.SearchType != "organization")
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
				case "cf": mainSearchTypeCode = 10; break;
				case "widget": 
                    mainSearchTypeCode = 25;
                    break;
                default:
					mainSearchTypeCode = 1;
					break;
			}

			totalResults = 0;
			switch ( query.SearchType )
			{
                case "OrganizationSearch":
                case "organization":
                    {
                        var results = OrganizationServices.MicroSearch( query, query.PageNumber, query.PageSize, ref totalResults );
                        return results.ConvertAll( m => ConvertProfileToMicroProfile( m ) );
                    }
                case "RegionSearch":
					{
						var locationType = query.GetFilterValueString( "LocationType" ).Split( ',' ).ToList();
						var results = new ThirdPartyApiServices().GeoNamesSearch( query.GetFilterValueString( "Keywords" ), query.PageNumber, query.PageSize, locationType, ref totalResults, false );
						return results.ConvertAll( m => ConvertRegionToMicroProfile( m ) );
					}
				//case "IndustrySearch":
				//	{
				//		//TODO - getAll should be set to false if used by a search view (ie credential)
				//		bool getAll = query.IncludeAllCodes;
				//		var results = EnumerationServices.Industry_Search( mainSearchTypeCode,
				//			query.GetFilterValueInt( "HeaderId" ), 
				//			query.GetFilterValueString( "Keywords" ), 
				//			query.PageNumber, 
				//			query.PageSize, 
				//			ref totalResults, 
				//			getAll );
				//		return results.ConvertAll( m => ConvertCodeItemToMicroProfile( m ) );
				//	}
				//case "OccupationSearch":
				//	{
				//		//TODO - IncludeAllCodes should be set to false if used by a search view (ie credential)
				//		var results = EnumerationServices.Occupation_Search( query.GetFilterValueInt( "HeaderId" ), 
				//			query.GetFilterValueString( "Keywords" ), 
				//			query.PageNumber, 
				//			query.PageSize, ref totalResults, query.IncludeAllCodes );
				//		return results.ConvertAll( m => ConvertCodeItemToMicroProfile( m ) );
				//	}
				//case "CIPSearch":
					//{
					//	//TODO - need entity type

					//	var results = EnumerationServices.CIPS_Search( 
					//		mainSearchTypeCode, 
					//		query.GetFilterValueInt( "HeaderId" ),
					//		query.GetFilterValueString( "Keywords" ), 
					//		query.PageNumber, 
					//		query.PageSize, 
					//		ref totalResults, 
					//		query.IncludeAllCodes );
					//	return results.ConvertAll( m => ConvertCodeItemToMicroProfile( m ) );
					//}
				
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
