using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using workIT.Models.Common;
using workIT.Utilities;

namespace workIT.Services
{
	public class RegistryServices
	{
		public static string GetRegistryData( string ctid = "", string uri = "" )
		{
			string ctdlType = "";
			string statusMessage = "";
			try
			{
				if ( !string.IsNullOrWhiteSpace( uri ) )
				{
					return GetResourceByUrl( uri, ref ctdlType, ref statusMessage );
				}
				else
				{
					return GetResourceGraphByCtid( ctid, ref ctdlType, ref statusMessage );
				}
			}
			catch ( Exception ex )
			{
				return JsonConvert.SerializeObject( new { error = ex.Message } );
			}
		}
		//Used because HttpClient doesn't work in views for some reason
		public static string MakeHttpGet( string url )
		{
			return new HttpClient().GetAsync( url ).Result.Content.ReadAsStringAsync().Result;
		}


		/// <summary>
		/// Retrieve a resource from the registry by ctid
		/// </summary>
		/// <param name="ctid"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public static string GetResourceByCtid( string ctid, ref string ctdlType, ref string statusMessage, string community = "" )
		{
			string resourceIdUrl = GetResourceUrl( ctid, community );
			return GetResourceByUrl( resourceIdUrl, ref ctdlType, ref statusMessage );
		}

		public static string GetResourceGraphByCtid( string ctid, ref string ctdlType, ref string statusMessage, string community = "" )
		{
			string registryUrl = GetResourceUrl( ctid, community );
			//not sure about this anymore
			//actually dependent on the purpose. If doing an import, then need graph
			//here will always want graph
			registryUrl = registryUrl.Replace( "/resources/", "/graph/" );

			return GetResourceByUrl( registryUrl, ref ctdlType, ref statusMessage );
		}

		public static string GetResourceUrl( string ctid, string community = "" )
		{
			if ( string.IsNullOrWhiteSpace( community ) )
			{
				community = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			}
			string serviceUri = UtilityManager.GetAppKeyValue( "credentialRegistryResource" );

			string registryUrl = string.Format( serviceUri, community, ctid );
			return registryUrl;
		}

		public static string GetGraphUrl( string ctid, string community = "" )
		{
			if ( string.IsNullOrWhiteSpace( community ) )
			{
				community = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			}
			string serviceUri = UtilityManager.GetAppKeyValue( "credentialRegistryResource" );
			serviceUri = serviceUri.Replace( "/resources/", "/graph/" );

			string registryUrl = string.Format( serviceUri, community, ctid );
			return registryUrl;
		}

		/// <summary>
		/// Retrieve a resource from the registry by resourceId
		/// </summary>
		/// <param name="resourceId">Url to a resource in the registry</param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>	
		public static string GetResourceByUrl( string resourceUrl, ref string ctdlType, ref string statusMessage )
		{
			string payload = "";
			statusMessage = "";
			ctdlType = "";
			string ceApiKey = UtilityManager.GetAppKeyValue( "ceApiKey" );
			try
			{
				using ( var client = new HttpClient() )
				{
					client.DefaultRequestHeaders.
						Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );
					if ( !string.IsNullOrWhiteSpace( ceApiKey ) )
					{
						client.DefaultRequestHeaders.Add( "Authorization", "Token " + ceApiKey );
					}
					var task = client.GetAsync( resourceUrl );
					task.Wait();
					var response1 = task.Result;
					payload = task.Result.Content.ReadAsStringAsync().Result;

					//just in case, likely the caller knows the context
					if ( !string.IsNullOrWhiteSpace( payload )
							&& payload.Length > 100
							//&& payload.IndexOf("\"errors\":") == -1
							)
					{
						ctdlType = RegistryServices.GetResourceType( payload );
					}
					else
					{
						//nothing found, or error/not found
						LoggingHelper.DoTrace( 1, "RegistryServices.GetResourceByUrl. Did not find: " + resourceUrl );
						statusMessage = "The referenced resource was not found in the credential registry: " + resourceUrl;
						payload = "";
					}
					//

				}
			}
			catch ( Exception exc )
			{
				if ( exc.Message.IndexOf( "(404) Not Found" ) > 0 )
				{
					//need to surface these better
					statusMessage = "The referenced resource was not found in the credential registry: " + resourceUrl;
				}
				else
				{
					var msg = LoggingHelper.FormatExceptions( exc );
					if ( msg.IndexOf( "remote name could not be resolved: 'sandbox.credentialengineregistry.org'" ) > 0 )
					{
						//retry?
						statusMessage = "retry";
					}
					LoggingHelper.LogError( exc, "RegistryServices.GetResource" );
					statusMessage = exc.Message;
				}
			}
			return payload;
		}
		/// <summary>
		/// Handle where an input object could be a string or a list of strings, and output must be a list of strings
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static string AssignObjectToString( object input )
		{
			if ( input == null )
				return null;
			if ( input.GetType() == typeof( Newtonsoft.Json.Linq.JArray ) )
			{
				var list = input as Newtonsoft.Json.Linq.JArray;
				if ( list != null && list.Count() > 0 )
					return list[ 0 ].ToString();
			}
			else if ( input.GetType() == typeof( System.String ) )
			{
				return input.ToString();
			}
			else if ( input.GetType() == typeof( Newtonsoft.Json.Linq.JObject ) && input.ToString().IndexOf( "en-US" ) > -1 )
			{
				Dictionary<string, object> dictionary = RegistryServices.JsonToDictionary( input.ToString() );
				var output = dictionary[ "en-US" ].ToString();
				return output;
			}
			else
			{
				//unexpected/unhandled
			}
			//-		input.GetType()	{Name = "JObject" FullName = "Newtonsoft.Json.Linq.JObject"}	System.Type {System.RuntimeType}

			return null;
		}

		private static string GetResourceType( string payload )
		{
			string ctdlType = "";
			RegistryObject ro = new RegistryObject( payload );
			ctdlType = ro.CtdlType;
			//ctdlType = ctdlType.Replace( "ceterms:", "" );
			return ctdlType;

		}

		/// <summary>
		/// Generic handling of Json object - especially for unexpected types
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static Dictionary<string, object> JsonToDictionary( string json )
		{
			var result = new Dictionary<string, object>();
			var obj = JObject.Parse( json );
			foreach ( var property in obj )
			{
				result.Add( property.Key, JsonToObject( property.Value ) );
			}
			return result;
		}
		public static object JsonToObject( JToken token )
		{
			switch ( token.Type )
			{
				case JTokenType.Object:
				{
					return token.Children<JProperty>().ToDictionary( property => property.Name, property => JsonToObject( property.Value ) );
				}
				case JTokenType.Array:
				{
					var result = new List<object>();
					foreach ( var obj in token )
					{
						result.Add( JsonToObject( obj ) );
					}
					return result;
				}
				default:
				{
					return ( ( JValue )token ).Value;
				}
			}
		}
	}

	public class RegistryObject
	{
		public RegistryObject() { }

		public RegistryObject( string payload )
		{
			if ( !string.IsNullOrWhiteSpace( payload ) && ( payload.IndexOf( "errors" ) == -1 || payload.IndexOf( "errors" ) > 50 ) )
			{
				dictionary = RegistryServices.JsonToDictionary( payload );
				if ( payload.IndexOf( "@graph" ) > 0 && payload.IndexOf( "@graph\": null" ) == -1 )
				{
					IsGraphObject = true;
					//get the graph object
					object graph = dictionary[ "@graph" ];
					//serialize the graph object
					var glist = JsonConvert.SerializeObject( graph );
					//parse graph in to list of objects
					JArray graphList = JArray.Parse( glist );

					var main = graphList[ 0 ].ToString();
					BaseObject = JsonConvert.DeserializeObject<RegistryBaseObject>( main );
					CtdlType = BaseObject.CdtlType;
					Ctid = BaseObject.Ctid;

					var n = RegistryServices.AssignObjectToString( BaseObject.Name );
					//not important to fully resolve yet
					if ( BaseObject.Name != null )
						Name = BaseObject.Name.ToString();
					else if ( CtdlType == "ceasn:CompetencyFramework" )
					{
						//var n = JsonConvert.DeserializeObject<LanguageMap>( BaseObject.CompetencyFrameworkName.ToString() );
						Name = ( BaseObject.CompetencyFrameworkName ?? "" ).ToString();
					}
					else
						Name = "?????";
				}
				else
				{
					//check if old resource or standalone resource
					BaseObject = JsonConvert.DeserializeObject<RegistryBaseObject>( payload );
					CtdlType = BaseObject.CdtlType;
					Ctid = BaseObject.Ctid;
					if ( BaseObject.Name != null )
						Name = BaseObject.Name.ToString();
					else
					{
						Name = "no name property for this document";
					}
				}
				if ( ( Name ?? "" ).IndexOf( "{" ) == 0 )
				{
					var n = JsonConvert.DeserializeObject<LanguageMap>( Name );
					Name = n.ToString();
					//Name = n.;
				}
				CtdlType = CtdlType.Replace( "ceterms:", "" );
				CtdlType = CtdlType.Replace( "ceasn:", "" );
			}
		}

		Dictionary<string, object> dictionary = new Dictionary<string, object>();

		public bool IsGraphObject { get; set; }
		public RegistryBaseObject BaseObject { get; set; } = new RegistryBaseObject();
		public string CtdlType { get; set; } = "";
		public string CtdlId { get; set; } = "";
		public string Ctid { get; set; } = "";
		public string Name { get; set; }
	}
	public class RegistryBaseObject
	{
		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		/// <summary>
		/// Type  of CTDL object
		/// </summary>
		[JsonProperty( "@type" )]
		public string CdtlType { get; set; }

		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string Ctid { get; set; }

		[JsonProperty( PropertyName = "ceterms:name" )]
		public object Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
		public object Description { get; set; }

		[JsonProperty( PropertyName = "ceasn:name" )]
		public object CompetencyFrameworkName { get; set; }

		[JsonProperty( PropertyName = "ceasn:description" )]
		public object FrameworkDescription { get; set; }


		[JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
		public string SubjectWebpage { get; set; }

	}

}
