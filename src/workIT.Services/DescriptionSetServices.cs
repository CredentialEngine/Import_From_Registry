using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Web;
using System.Net.Http;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using workIT.Models.DescriptionSet;
using workIT.Utilities;

namespace workIT.Services
{
	public class DescriptionSetServices
	{
		public static DescriptionSetResult GetDescriptionSetsByCTIDs( List<string> ctids, bool includeRelatedData = true, int relatedURIsLimit = 10, int relatedItemsLimit = 10 )
		{
			var result = new DescriptionSetResult();

			try
			{
				//Setup the request
				result.DebugInfo[ "Request CTIDs" ] = JArray.FromObject( ctids );
				var request = new JObject()
				{
					{ "DescriptionSetCTIDs", JArray.FromObject( ctids ) },
					{ "DescriptionSetRelatedURIsLimit", relatedURIsLimit },
					{ "DescriptionSetRelatedItemsLimit", relatedItemsLimit },
					{ "DescriptionSetIncludeData", includeRelatedData }
				};
				var clientIP = "unknown";
				try
				{
					clientIP = HttpContext.Current.Request.UserHostAddress;
				}
				catch { }
				var requestData = new StringContent( request.ToString( Formatting.None ), Encoding.UTF8, "application/json" );

				//Get API key and URL
				var apiKey = ConfigHelper.GetConfigValue( "MyCredentialEngineAPIKey", "" );
				var apiURL = ConfigHelper.GetConfigValue( "GetDescriptionSetsByCTIDsEndpoint", "" );
				var registryURL = ConfigHelper.GetConfigValue( "credentialRegistryUrl", "" );

				//Setup the client
				var client = new HttpClient();
				client.DefaultRequestHeaders.TryAddWithoutValidation( "Authorization", "ApiToken " + apiKey );
				client.DefaultRequestHeaders.Referrer = new Uri( "https://credentialfinder.org/Finder/GetCompetencyFrameworkDescriptionSets/" );
				client.Timeout = new TimeSpan( 0, 10, 0 );

				//Get the data
				var rawResult = client.PostAsync( apiURL, requestData ).Result;
				var rawResultText = rawResult.Content.ReadAsStringAsync().Result;
				result.DebugInfo[ "Raw Publisher API Response" ] = rawResultText;

				//Parse the result or handle errors
				if ( rawResult.IsSuccessStatusCode )
				{
					try
					{
						//Parse the result
						var parsedResult = JObject.Parse( rawResultText );
						var parsedResultData = parsedResult[ "data" ] ?? parsedResult;
						result.DebugInfo[ "Inner Debug" ] = parsedResultData[ "Debug" ];
						result.RelatedItems = parsedResultData[ "RelatedItems" ].Select( m => ( JObject ) m ).ToList();
						result.RelatedItemsMap = parsedResultData[ "RelatedItemsMap" ].Select( m => m.ToObject<RelatedItemsMapItem>() ).ToList();
					}
					catch( Exception ex )
					{
						result.DebugInfo[ "Error Parsing Response" ] = ex.Message;
					}
				}
				else
				{
					result.DebugInfo[ "HTTP Error Retrieving Description Sets" ] = new JObject()
					{
						{ "Status Code", rawResult.StatusCode.ToString() },
						{ "Reason Phrase", rawResult.ReasonPhrase }
					};
				}
			}
			catch ( Exception ex )
			{
				result.DebugInfo[ "Error Getting Description Sets" ] = ex.Message;
			}

			return result;
		}
		//


	}
}
