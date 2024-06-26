using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using workIT.Utilities;
using workIT.Services.API;
using workIT.Models.API;
using workIT.Factories;

using PM = workIT.Models.ProfileModels;
using CM = workIT.Models.Common;
using EM = workIT.Models.Elastic;
using SM = workIT.Models.Search;

namespace workIT.Services.API
{
	public class CompetencyFrameworkServices
	{
		public static CompetencyFramework GetDetailForAPI( int id, bool skippingCache = false )
		{
			//Get the data
			var rawData = CompetencyFrameworkManager.Get( id );
			if( rawData == null || rawData.Id == 0 )
			{
				//Not clear whether this should return null or a new object
				return new CompetencyFramework()
				{
					Name = "Error: No matching competency framework for ID " + id
				};
			}

			//Return the converted data
			return ConvertProfileModelsCompetencyFrameworkToFinderAPICompetencyFramework( rawData );
		}

		//
		public static CompetencyFramework GetDetailByCtidForAPI( string ctid, bool skippingCache = false )
		{
			//Get the data
			var graphData = RegistryServicesV2.GetDataByCTID( ctid, true );
			if ( !graphData.Successful )
			{
				//Not clear whether this should return null or a new object
				return new CompetencyFramework()
				{
					Name = "Error Loading Framework Data",
					Description = graphData.DebugInfo.ToString()
				};
			}

			//Return the converted data
			return ConvertCTDLASNCompetencyFrameworkGraphToFinderAPICompetencyFramework( (JObject) graphData.RawData );
		}
		//
		public static Competency GetCompetencyDetailForAPI( int id, bool skippingCache = false )
		{
			//Get the data
			var rawData = CompetencyFrameworkCompetencyManager.Get( id );
			if ( rawData == null || rawData.Id == 0 )
			{
				//Not clear whether this should return null or a new object
				return new Competency()
				{
					CompetencyText = "Error: No matching competency for ID " + id
				};
			}

			//Return the converted data
			return ConvertProfileModelsCompetencyToFinderAPICompetency( rawData );
		}
		//
		public static Competency GetCompetencyDetailByCtidForAPI( string ctid, bool skippingCache = false )
		{
			//Get the data
			var graphData = RegistryServicesV2.GetDataByCTID( ctid, true );
			if ( !graphData.Successful )
			{
				//Not clear whether this should return null or a new object
				return new Competency()
				{
					CompetencyText = "Error Loading Competency Data",
				};
			}

			//Return the converted data
			return ConvertProfileModelsCompetencyToFinderAPICompetency( ( JObject ) graphData.RawData );
		}
		//

		public static CompetencyFramework ConvertProfileModelsCompetencyFrameworkToFinderAPICompetencyFramework( PM.CompetencyFramework source )
		{
			var result = new CompetencyFramework();
			var registryBaseURL = ConfigHelper.GetConfigValue( "credentialRegistryUrl", "" );
			//Meta properties
			result.Meta_Id = source.Id;
			result.Meta_Language = "en"; //Need a way to detect this
			result.EntityLastUpdated = source.LastUpdated;
			result.Meta_StateId = source.EntityStateId;
			//the FrameworkUri is probably OK. Actually use a common method so can handle the community hack 
			//result.CredentialRegistryURL = source.FrameworkUri;
			result.CredentialRegistryURL = RegistryServices.GetResourceUrl( source.CTID );
			//

			result.RegistryData = ServiceHelper.FillRegistryData( source.CTID );

			//Organization properties
			result.Creator = GetOrganizationAJAXSummaryFromDatabase( "Creator", source.Creator );
			result.Publisher = GetOrganizationAJAXSummaryFromDatabase( "Publisher", source.Publisher );
			result.RightsHolder = GetOrganizationAJAXSummaryFromDatabase( "Rights Holder", new List<string>() { source.RightsHolder }.Where( m => !string.IsNullOrWhiteSpace( m ) ).ToList() );

			//Core properties
			result.CTID = source.CTID;
			result.Name = source.Name;
			result.Description = source.Description;
			result.SubjectWebpage = source.SubjectWebpage; //Not a property of Competency Framework==> uses Source

			//Extra properties
			result.Source = source.Source;
			result.Meta_FriendlyName = source.FriendlyName;
			result.InLanguage = source.inLanguage;

			//Competencies
			result.HasTopChild = source.HasTopChild;
			result.Meta_HasPart = ConvertElasticModelsCompetencyListToFinderAPICompetencyList( registryBaseURL, source.Competencies );

			return result;
		}
		//
		public static Competency ConvertProfileModelsCompetencyToFinderAPICompetency( PM.Competency source )
		{
			var result = new Competency();
			var registryBaseURL = ConfigHelper.GetConfigValue( "credentialRegistryUrl", "" );
			//Meta properties
			//result.Meta_Id = source.Id;
			//result.Meta_Language = "en"; //Need a way to detect this
			//result.EntityLastUpdated = source.LastUpdated;
			//result.Meta_StateId = 3;
			//the FrameworkUri is probably OK. Actually use a common method so can handle the community hack 
			//result.CredentialRegistryURL = source.FrameworkUri;
			result.CredentialRegistryURL = RegistryServices.GetResourceUrl( source.CTID );
			//

			result.RegistryData = ServiceHelper.FillRegistryData( source.CTID );


			//Core properties
			result.CTID = source.CTID;
			result.CompetencyText = source.CompetencyText;
			//result.Description = source.Description;
			//result.SubjectWebpage = source.SubjectWebpage; 

			////Extra properties
			//result.Source = source.Source;
			//result.Meta_FriendlyName = source.FriendlyName;
			//result.InLanguage = source.inLanguage;

			//other
			
			return result;
		}
		//
		public static Competency ConvertProfileModelsCompetencyToFinderAPICompetency( JObject source, List<JObject> debug = null )
		{
			var result = new Competency();
			var registryBaseURL = ConfigHelper.GetConfigValue( "credentialRegistryUrl", "" );
			debug = debug ?? new List<JObject>();
			var debugStep = "Start";

			try
			{
				//Extract data
				debugStep = "Extract Data";
				var framework = ( JObject ) ( ( JArray ) source["@graph"] ).FirstOrDefault( m => ( ( JObject ) m )["@type"].ToString() == "ceasn:Competency" );
				//other? Child comps?
				var competencies = ( ( JArray ) source["@graph"] ).Where( m => ( ( JObject ) m )["@type"].ToString() == "ceasn:Competency" ).Select( m => ( JObject ) m ).ToList();

				var ftype = framework["@type"]?.ToString() ?? "Competency";
				//Map data
				var frameworkCTID = framework["ceterms:ctid"]?.ToString() ?? "unknown";
				var language = JArrayToStringList( ( JArray ) framework["ceterms:inLanguage"] )?.FirstOrDefault();
				//Meta properties
				debugStep = "Meta Properties";
				//result.Meta_Id = -99; //Should probably try to look this up from the database(?)
				//result.Meta_Language = !string.IsNullOrWhiteSpace( language ) ? language : "en"; //Need a way to detect this
				//result.Meta_StateId = 3; //Always published/full																								 
				result.CredentialRegistryURL = framework["@id"]?.ToString() ?? "Unknown URI";
				result.RegistryData = ServiceHelper.FillRegistryData( frameworkCTID );

				result.CTID = frameworkCTID;

				//
				//result.EntityLastUpdated = Attempt( () => DateTime.Parse( framework["ceasn:dateModified"].ToString() ), DateTime.MinValue );

				//Core properties
				debugStep = "Core Properties";
				result.CompetencyText = CompetencyFrameworkServicesV2.GetEnglishString( framework["ceasn:competencyText"], "Unknown Competencjy" );

		


				//Core properties
				//debugStep = "Core Properties";

				//Extra properties
				debugStep = "Extra Properties";

				//result.Meta_FriendlyName = Regex.Replace( result.Name, @"[^A-Za-z0-9]", "_" );
				//result.InLanguage = JArrayToStringList( ( JArray ) framework["ceasn:inLanguage"] );


				return result;
			}
			catch ( Exception ex )
			{
				debug.Add( new JObject()
				{
					{ "Exception", ex.Message },
					{ "Inner Exception", ex.InnerException?.Message },
					{ "Debug Step", debugStep }
				} );
			}

			return null;
		}
		//
		public static List<Competency> ConvertElasticModelsCompetencyListToFinderAPICompetencyList( string baseURL, List<EM.IndexCompetency> source )
		{
			source = source ?? new List<EM.IndexCompetency>();
			var result = new List<Competency>();

			foreach( var item in source )
			{
				var itemURI = string.IsNullOrWhiteSpace( item.CTID ) ? "_:" + Guid.NewGuid().ToString() : baseURL + item.CTID;
				result.Add( new Competency()
				{
					CredentialRegistryURL = itemURI,
					CTID = item.CTID,
					CompetencyLabel = item.Name,
					CompetencyText = item.Description
					//No source data for hasChild property, so we can't build a hierarchy from this
				} );
			}

			return result;
		}
		//

		public static CompetencyFramework ConvertCTDLASNCompetencyFrameworkGraphToFinderAPICompetencyFramework( JObject source, List<JObject> debug = null )
		{
			var result = new CompetencyFramework();
			var registryBaseURL = ConfigHelper.GetConfigValue( "credentialRegistryUrl", "" );
			debug = debug ?? new List<JObject>();
			var debugStep = "Start";

			try
			{
				//Extract data
				debugStep = "Extract Data";
				var framework = ( JObject ) ( ( JArray ) source["@graph"] ).FirstOrDefault( m => ( ( JObject ) m )["@type"].ToString() == "ceasn:CompetencyFramework" );
				if ( framework == null )
				{
					framework = ( JObject ) ( ( JArray ) source["@graph"] ).FirstOrDefault( m => ( ( JObject ) m )["@type"].ToString() == "ceterms:Collection" );
				}
				var competencies = ( ( JArray ) source["@graph"] ).Where( m => ( ( JObject ) m )["@type"].ToString() == "ceasn:Competency" ).Select( m => ( JObject ) m ).ToList();

				var ftype = framework["@type"]?.ToString() ?? "CompetencyFramework";
				//Map data
				var frameworkCTID = framework["ceterms:ctid"]?.ToString() ?? "unknown";
				var language = JArrayToStringList( ( JArray ) framework["ceterms:inLanguage"] )?.FirstOrDefault();
				//Meta properties
				debugStep = "Meta Properties";
				result.Meta_Id = -99; //Should probably try to look this up from the database(?)
				result.Meta_Language = !string.IsNullOrWhiteSpace( language ) ? language : "en"; //Need a way to detect this
				result.Meta_StateId = 3; //Always published/full																								 
				result.CredentialRegistryURL = framework["@id"]?.ToString() ?? "Unknown URI";
				result.RegistryData = ServiceHelper.FillRegistryData( frameworkCTID );

				result.CTID = frameworkCTID;

				if ( ftype == "ceterms:Collection" )
				{
					//Core properties
					debugStep = "Core Properties";
					result.Name = CompetencyFrameworkServicesV2.GetEnglishString( framework["ceterms:name"], "Unknown Name" );
					result.Description = CompetencyFrameworkServicesV2.GetEnglishString( framework["ceterms:description"], "Unknown Name" );
					result.SubjectWebpage = framework["ceterms:subjectWebpage"]?.ToString() ?? "";
					result.Source = result.SubjectWebpage;
				}
				else
                {
					//need alternative for a collection
					//var dateModified = framework["ceasn:dateModified"]??"";
					if ( framework["ceasn:dateModified"] != null )
					{
						result.EntityLastUpdated = Attempt( () => DateTime.Parse( framework["ceasn:dateModified"]?.ToString() ), DateTime.MinValue );
					}
					//Organization properties
					debugStep = "Organization Properties";
					result.Creator = GetOrganizationAJAXSummaryFromDatabase( "Creator", JArrayToStringList( ( JArray ) framework["ceasn:creator"] ) );
					result.Publisher = GetOrganizationAJAXSummaryFromDatabase( "Publisher", JArrayToStringList( ( JArray ) framework["ceasn:publisher"] ) );
					result.RightsHolder = GetOrganizationAJAXSummaryFromDatabase( "Rights Holder", new List<string>() { framework["ceasn:rightsHolder"]?.ToString() }.Where( m => !string.IsNullOrWhiteSpace( m ) ).ToList() );

					//Core properties
					debugStep = "Core Properties";
					result.Name = CompetencyFrameworkServicesV2.GetEnglishString( framework["ceasn:name"], "Unknown Name" );
					result.Description = CompetencyFrameworkServicesV2.GetEnglishString( framework["ceasn:description"], "Unknown Name" );
					result.SubjectWebpage = JArrayToStringList( ( JArray ) framework["ceasn:source"] )?.FirstOrDefault();

					result.Source = result.SubjectWebpage;
				}


				//Core properties
				//debugStep = "Core Properties";
			
				//result.Name = CompetencyFrameworkServicesV2.GetEnglishString( framework[ "ceasn:name" ], "Unknown Name" );
				//result.Description = CompetencyFrameworkServicesV2.GetEnglishString( framework[ "ceasn:description" ], "Unknown Name" );
				//result.SubjectWebpage = JArrayToStringList( ( JArray ) framework[ "ceasn:source" ] )?.FirstOrDefault();

				//Extra properties
				debugStep = "Extra Properties";

				result.Meta_FriendlyName = Regex.Replace( result.Name, @"[^A-Za-z0-9]", "_" );
				result.InLanguage = JArrayToStringList( ( JArray ) framework[ "ceasn:inLanguage" ] );

				//Competencies
				debugStep = "Competencies";
				result.HasTopChild =
					JArrayToStringList( ( JArray ) framework[ "ceasn:hasTopChild" ] ) ??
					Attempt( () => competencies.Select( m => m[ "@id" ].ToString() ).ToList(), null ) ??
					new List<string>();
				result.Meta_HasPart = ConvertCTDLASNCompetencyListToFinderAPICompetencyList( competencies );

				return result;
			}
			catch ( Exception ex )
			{
				debug.Add( new JObject()
				{
					{ "Exception", ex.Message },
					{ "Inner Exception", ex.InnerException?.Message },
					{ "Debug Step", debugStep }
				} );
			}

			return null;
		}
		//

		public static List<Competency> ConvertCTDLASNCompetencyListToFinderAPICompetencyList( List<JObject> source )
		{
			source = source ?? new List<JObject>();
			var result = new List<Competency>();

			foreach( var item in source )
			{
				result.Add( new Competency()
				{
					CredentialRegistryURL = item[ "@id" ].ToString(),
					CTID = item[ "ceterms:ctid" ]?.ToString(),
					CompetencyLabel = CompetencyFrameworkServicesV2.GetEnglishString( item[ "ceasn:competencyLabel" ], null ),
					CompetencyText = CompetencyFrameworkServicesV2.GetEnglishString( item[ "ceasn:competencyText" ], "Unknown Text" ),
					Comment = CompetencyFrameworkServicesV2.GetEnglishList( item[ "ceasn:comment" ], null )?.FirstOrDefault( m => !string.IsNullOrWhiteSpace( m ) ),
					CodedNotation = item[ "ceasn:codedNotation" ]?.ToString(),
					CompetencyCategory = CompetencyFrameworkServicesV2.GetEnglishString( item[ "ceasn:competencyCategory" ], null ),
					ListID = item[ "ceasn:listID" ]?.ToString(),
					HasChild = JArrayToStringList( ( JArray ) item[ "ceasn:hasChild" ] )
				} );
			}

			return result;
		}
		//
		/*
		public static CompetencyFramework ConvertCredentialAlignmentObjectFrameworkProfileToFinderAPICompetencyFramework( CM.CredentialAlignmentObjectFrameworkProfile source )
		{
			var result = new CompetencyFramework();
			var registryBaseURL = ConfigHelper.GetConfigValue( "credentialRegistryUrl", "" );

			//Meta properties
			result.Meta_Id = source.Id;
			result.Meta_Language = "en"; //Need a way to detect this
			result.EntityLastUpdated = source.LastUpdated;
			result.Meta_StateId = 3; //Assumed
			result.CredentialRegistryURL = source.FrameworkUri;
			result.RegistryData = ServiceHelper.FillRegistryData( source.FrameworkCtid );

			//No source data for Organization properties

			//Core properties
			result.CTID = source.FrameworkCtid;
			result.Name = source.FrameworkName;
			result.Description = source.Description;
			result.SubjectWebpage = source.Framework;
			result.CredentialRegistryURL = source.IsARegistryFrameworkUrl ? source.FrameworkUri : null;

			//Extra properties
			result.Source = source.Framework;
			result.Meta_FriendlyName = Regex.Replace( result.Name, @"[^A-Za-z0-9]", "_" );
			result.InLanguage = string.IsNullOrWhiteSpace( source.FirstLanguage ) ? null : new List<string>() { source.FirstLanguage };

			//No source data for Competencies
			result.HasTopChild = source.Items.Select( m => m.TargetNode ).ToList();
			result.Meta_HasPart = ConvertCredentialAlignmentObjectProfileListToFinderAPICompetencyList( source.Items );

			return result;
		}
		*/

		public static List<Competency> ConvertCredentialAlignmentObjectProfileListToFinderAPICompetencyList( List<CM.CredentialAlignmentObjectItem> source )
		{
			source = source ?? new List<CM.CredentialAlignmentObjectItem>();
			var result = new List<Competency>();

			foreach( var item in source )
			{
				//TODO - should we null non registry URIs?
				//23-06-06 mp - No. Need to show these
				var itemURI = "_:" + Guid.NewGuid().ToString();
				var targetNode = "";
				if ( !string.IsNullOrWhiteSpace( item.TargetNode ) )
				{
                    if ( URItoCTID(item.TargetNode) != null )
                        itemURI = item.TargetNode;
					else
					{
						targetNode = item.TargetNode;
					}
                }
                //var itemURI = string.IsNullOrWhiteSpace( item.TargetNode ) ? "_:" + Guid.NewGuid().ToString() : item.TargetNode;
				result.Add( new Competency()
				{
					//a url may not be a registry url, and can be a blank node id
					CredentialRegistryURL = itemURI,
					CTID = itemURI.IndexOf( "_:" ) == 0 ? null : URItoCTID( item.TargetNode ),
					TargetNode = targetNode,
					CompetencyLabel = string.IsNullOrWhiteSpace( item.TargetNodeDescription ) ? null : item.TargetNodeName, //If the description field was not populated, then the name is the competencyText
					CompetencyText = string.IsNullOrWhiteSpace( item.TargetNodeDescription ) ? item.TargetNodeName : item.TargetNodeDescription, //If the description field was not populated, use the name
					CodedNotation = item.CodedNotation
					//No source data for hasChild property, so we can't build a hierarchy from this
				} );
			}

			return result;
		}
		//

		public static CompetencyFramework ConvertCAOFrameworkProfileToFinderAPICompetencyFrameworkWithOnlyReferencedCompetenciesAndTheirParents( CM.CredentialAlignmentObjectFrameworkProfile source, List<JObject> debug = null )
		{
			//Setup debugging
			debug = debug ?? new List<JObject>();
			var debugItem = new JObject();
			debug.Add( debugItem );
			var innerConvertDebug = new List<JObject>();

			//Return null if no data
			if( source == null )
			{
				return null;
			}

			try
			{
				var frameworkName = "Miscellaneous Competencies";
				if (source != null && !string.IsNullOrWhiteSpace(source.FrameworkName ) )
					frameworkName= source.FrameworkName;

				//If the competencies aren't from a framework (ie CredentialAlignmentObject only), just convert them
				debugItem.Add( "Raw Payload", source.RegistryImport?.Payload );
				if ( string.IsNullOrWhiteSpace( source.RegistryImport?.Payload ) )
				{
					var convertedCompetencies = ConvertCredentialAlignmentObjectProfileListToFinderAPICompetencyList( source.Items );
					convertedCompetencies.ForEach( m => m.Meta_IsReferenced = true );
					//why is this always miscellaneous?
					//UI attempts to use registry to get comps, will fail if misc.
					//do we skip hasTopChild if not in registry
					var convertedFramework = new CompetencyFramework()
					{
						Name = frameworkName,
						HasTopChild = convertedCompetencies.Select( m => m.CredentialRegistryURL ).ToList(),
						Meta_HasPart = convertedCompetencies,
						CTID = !string.IsNullOrEmpty( source.FrameworkCtid ) ? source.FrameworkCtid : null,
						Description = !string.IsNullOrEmpty( source.Description ) ? source.Description : null
					};

					return convertedFramework;
				}
				//Otherwise, extract framework data from the source's import payload. This will allow us to get the hierarchy information, since the raw competency data does not contain it
				else
				{
					var graph = RegistryServicesV2.ParseJSONWithoutParsingDates<JObject>( source.RegistryImport.Payload );
					debugItem.Add( "Graph", graph );

					var convertedFramework = ConvertCTDLASNCompetencyFrameworkGraphToFinderAPICompetencyFramework( graph, innerConvertDebug );
					debugItem.Add( "Converted", JObject.FromObject( convertedFramework, new JsonSerializer() { NullValueHandling = NullValueHandling.Ignore } ) );

					//Strip unwanted items from the hierarchy
					var selectedItemURIs = source.Items.Select( m => m.TargetNode ).ToList();
					debugItem.Add( "Starting with URIs", JArray.FromObject( convertedFramework.Meta_HasPart.Select( m => m.CredentialRegistryURL ).ToList() ) );
					debugItem.Add( "Keeping URIs", JArray.FromObject( selectedItemURIs ) );
					convertedFramework.Meta_HasPart = KeepOnlyReferencedCompetenciesAndTheirParents( convertedFramework.Meta_HasPart, selectedItemURIs );

					//Strip unneeded URI references
					var remainingURIs = convertedFramework.Meta_HasPart.Select( m => m.CredentialRegistryURL ).ToList();
					convertedFramework.HasTopChild = convertedFramework.HasTopChild.Where( m => remainingURIs.Contains( m ) ).ToList();
					convertedFramework.Meta_HasPart.ForEach( m => m.HasChild = m.HasChild?.Where( n => remainingURIs.Contains( n ) ).ToList() );

					return convertedFramework;
				}

			}
			catch ( Exception ex )
			{
				debug.Add( new JObject()
				{
					{ "Exception", ex.Message },
					{ "Inner Exception", ex.InnerException?.Message },
					{ "Inner Convert Debug", JArray.FromObject( innerConvertDebug, new JsonSerializer() { NullValueHandling = NullValueHandling.Ignore } ) }
				} );
			}

			return null;
		}
		//

		public static List<Competency> KeepOnlyReferencedCompetenciesAndTheirParents( List<Competency> source, List<string> referencedCompetencyURIs )
		{
			var result = source.Where( m => referencedCompetencyURIs.Contains( m.CredentialRegistryURL ) ).ToList();
			result.ForEach( m => m.Meta_IsReferenced = true ); //Flag these competencies as having been directly referenced
			AddParentCompetencies( source, result );

			return result;
		}
		private static void AddParentCompetencies( List<Competency> source, List<Competency> result )
		{
			//Matches = everything in the source which A) is not already in the result and B) references a child competency that is in the result
			var matches = source.Where( m => !result.Contains( m ) ).Where( m => result.Where( n => m.HasChild != null && m.HasChild.Contains( n.CredentialRegistryURL ) ).Count() > 0 ).ToList();

			//If there are any matches, add them and process the lists again
			if( matches.Count() > 0 )
			{
				result.AddRange( matches );
				AddParentCompetencies( source, result);
			}
		}
		//

		public static SM.AJAXSettings WrapCompetencyFrameworkListInAJAXSettings( string labelTemplate, List<CompetencyFramework> source )
		{
			source = source ?? new List<CompetencyFramework>();
			var total = 0;
			foreach( var framework in source )
			{
				if ( framework != null )
				{
					//Only count the items that were referenced, but if that count is zero because something broke, return a count of all items in the framework
					var referenced = framework.Meta_HasPart.Where( m => m.Meta_IsReferenced == true ).Count();
					total += referenced > 0 ? referenced : framework.Meta_HasPart.Count();
				}
			}
			if ( total == 0 )
				return null;
			return new SM.AJAXSettings()
			{
				Label = total > 0 ? labelTemplate.Replace( "{#}", total.ToString() ).Replace( "{ies}", total == 1 ? "y" : "ies" ) : "",
				Total = total,
				Values = source.Select( m => ( object ) m ).ToList()
			};
		}
		//

		public static SM.AJAXSettings ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( string labelTemplate, List<CM.CredentialAlignmentObjectFrameworkProfile> source, List<JObject> debug = null )
		{
			if ( source == null || !source.Any() || source[0] == null)
				return null;
			debug = debug ?? new List<JObject>();
			source = source ?? new List<CM.CredentialAlignmentObjectFrameworkProfile>();

			try
			{
				var converted = source.Select( m => ConvertCAOFrameworkProfileToFinderAPICompetencyFrameworkWithOnlyReferencedCompetenciesAndTheirParents( m, debug ) ).ToList();
				if ( converted == null || !converted.Any() || converted[0] == null )
					return null;
				var wrapped = WrapCompetencyFrameworkListInAJAXSettings( labelTemplate, converted );
				return wrapped;
			}
			catch ( Exception ex )
			{
				debug.Add( new JObject() {
					{ "Error wrapping data in AJAXSettings", ex.Message },
					{ "Inner Exception", ex.InnerException?.Message },
					{ "Label Template", labelTemplate },
					{ "Frameworks Count", source?.Count() ?? 0 }
				} );
			}

			return new SM.AJAXSettings();
		}

		private static T Attempt<T>( Func<T> method, T defaultValue ) 
		{
			try
			{
				return method();
			}
			catch
			{
				return defaultValue;
			}
		}
		//

		private static List<string> JArrayToStringList( JArray source )
		{
			return source == null ? null : source.Select( m => m.ToString() ).ToList();
		}
		/// <summary>
		/// extract a CTID from a uri if present
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		private static string URItoCTID( string uri )
		{
			return string.IsNullOrWhiteSpace( uri ) ? null : uri.IndexOf("/ce-") == -1 ? null : "ce-" + uri.Split( new string[] { "/ce-" }, StringSplitOptions.RemoveEmptyEntries ).LastOrDefault();
		}
		//

		private static SM.AJAXSettings GetOrganizationAJAXSummaryFromDatabase( string label, List<string> uris )
		{
			uris = uris ?? new List<string>();
			var links = uris
				.Select( m => OrganizationManager.GetSummaryByCtid( URItoCTID( m ), false ) )
				.Select( m => new LabelLink() { Label = m?.Name ?? "Unknown", URL = "/organization/" + ( m?.Id ?? -1 ) } )
				.ToList();

			return new SM.AJAXSettings()
			{
				Total = uris.Count(),
				Label = label,
				Values = links.Select( m => (object) m ).ToList()
			};
		}
		//

	}
}
