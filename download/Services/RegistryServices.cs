using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Download.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Download.Services
{
	public class RegistryServices
	{
		public static string GetResourceType( string payload )
		{
			string ctdlType = "";
			RegistryObject ro = new RegistryObject( payload );
			ctdlType = ro.CtdlType;
			//ctdlType = ctdlType.Replace( "ceterms:", "" );
			return ctdlType;
		}
		/// <summary>
		/// Get the main resource object for a graph from a Decoded resource (from an envelope)
		/// Should to handle the decodedResource which contains th @graph, or just the contents of the a payload (i.e. stuff inside a graph)
		/// ==> unlikely to need in this project
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static GraphMainResource GetGraphMainResource( string json )
		{
			if ( string.IsNullOrWhiteSpace( json ) )
				return null; //??
			var graphMainResource = new GraphMainResource();
			Dictionary<string, object> dictionary = JsonToDictionary( json );
			object graph = dictionary[ "@graph" ];
			var glist = JsonConvert.SerializeObject( graph );
			//parse graph in to list of objects
			JArray graphList = JArray.Parse( glist );

			if ( graphList != null && graphList.Any() )
			{
				var main = graphList[ 0 ].ToString();
				graphMainResource = JsonConvert.DeserializeObject<GraphMainResource>( main );
			}
			return graphMainResource;
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
}
