using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;

using ThisEntity = workIT.Models.Common.CostManifest;
using EntityMgr = workIT.Factories.CostManifestManager;
using workIT.Utilities;
using workIT.Factories;

using workIT.Models.Search;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Web;

using System.Threading;
using workIT.Models.Helpers.CompetencyFrameworkHelpers;

namespace workIT.Services
{
    public class CompetencyFrameworkServices
    {
        string thisClassName = "CompetencyFrameworkServices";
        ActivityServices activityMgr = new ActivityServices();
        public List<string> messages = new List<string>();

		//Use the Credential Registry to search for competency frameworks
		public static List<CTDLAPICompetencyFrameworkResult> SearchViaRegistry( MainSearchInput data, ref int totalResults )
		{
			//Handle blind searches
			if ( string.IsNullOrWhiteSpace( data.Keywords ) )
			{
				data.Keywords = "search:anyValue";
			}

			var queryData = new JObject()
			{
				//Get competency frameworks...
				{ "@type", "ceasn:CompetencyFramework" },
				{ "search:termGroup", new JObject()
				{
					//Where name or description matches the keywords, or...
					{ "ceasn:name", data.Keywords },
					{ "ceasn:description", data.Keywords },
					//Where the framework contains a competency (via reverse connection) with competency text that contains the keywords
					{ "ceasn:isPartOf", new JObject() {
						{ "ceasn:competencyText", data.Keywords }
					} },
					{ "search:operator", "search:orTerms" }
				} }
			};

			var skip = data.PageSize * (data.StartPage - 1);
			var take = data.PageSize;

			var clientIP = "unknown";
			try
			{
				clientIP = HttpContext.Current.Request.UserHostAddress;
			}
			catch { }

			var resultData = DoQuery( queryData, skip, take, "https://credentialfinder.org/Finder/SearchViaRegistry/", clientIP );

			totalResults = resultData.extra.TotalResults;
			var resultItems = ParseResults<CTDLAPICompetencyFrameworkResult>( resultData.data );

			return resultItems;
		}
		//

		public static List<FrameworkSearchItem> ThreadedFrameworkSearch( List<FrameworkSearchItem> searchItems )
		{
			var itemSet = new FrameworkSearchItemSet() { Items = searchItems };
			//Trigger the threads
			foreach( var searchItem in itemSet.Items )
			{
				//Set this here to avoid any potential race conditions with the WaitUntiLAllAreFinished method
				searchItem.IsInProgress = true;
				WaitCallback searchMethod = StartFrameworkSearchThread;
				ThreadPool.QueueUserWorkItem( searchMethod, searchItem );
			}
			//Wait for them all to finish
			itemSet.WaitUntilAllAreFinished();

			//Return results
			return itemSet.Items;
		}
		private static void StartFrameworkSearchThread( object frameworkSearchItem )
		{
			//Cast the type and do the search
			var searchItem = ( FrameworkSearchItem ) frameworkSearchItem;
			try
			{
				var total = 0;
				searchItem.Results = searchItem.ProcessMethod.Invoke( searchItem.CompetencyCTIDs, searchItem.SkipResults, searchItem.TakeResults, ref total, searchItem.ClientIP );
				searchItem.TotalResults = total;
			}
			catch { }
			//When finished, set the variable that will be checked by the FrameworkSearchItemSet.WaitUntilAllAreFinished method
			searchItem.IsInProgress = false;
		}
		//

		public static List<JObject> GetCredentialsForCompetencies( List<string> competencyCTIDs, int skip, int take, ref int totalResults, string clientIP = null )
		{
			var competencies = new JArray( competencyCTIDs.ToArray() );
			var queryData = new JObject()
			{
				//TODO: may need to include the list of credential types here (as a parameter) - probably not necessary?
				//Find anything that requires...
				{ "ceterms:requires", new JObject()
				{
					//A target competency with a CTID that matches, or
					{ "ceterms:targetCompetency", new JObject()
					{
						{ "ceterms:targetNode", new JObject() {
							{ "ceterms:ctid", competencies }
						} }
					} },
					//A target assessment that assesses a competency with a CTID that matches, or
					{ "ceterms:targetAssessment", new JObject()
					{
						{ "ceterms:assesses", new JObject()
						{
							{ "ceterms:targetNode", new JObject()
							{
								{ "ceterms:ctid", competencies }
							} }
						} }
					} },
					//A target learning opportunity that teaches a competency with a CTID that matches
					{ "ceterms:targetLearningOpportunity", new JObject()
					{
						{ "ceterms:teaches", new JObject()
						{
							{ "ceterms:targetNode", new JObject()
							{
								{ "ceterms:ctid", competencies }
							} }
						} }
					} },
					{ "search:operator", "search:orTerms" }
				} }
			};

			return DoSimpleQuery( queryData, skip, take, ref totalResults, "https://credentialfinder.org/Finder/GetCredentialsForCompetencies/", clientIP );
		}
		//

		public static List<JObject> GetAssessmentsForCompetencies( List<string> competencyCTIDs, int skip, int take, ref int totalResults, string clientIP = null )
		{
			var competencies = new JArray( competencyCTIDs.ToArray() );
			var queryData = new JObject()
			{
				{ "ceterms:assesses", new JObject()
				{
					{ "ceterms:targetNode", new JObject()
					{
						{ "ceterms:ctid", competencies }
					} }
				} }
			};

			return DoSimpleQuery( queryData, skip, take, ref totalResults, "https://credentialfinder.org/Finder/GetAssessmentsForCompetencies/", clientIP );
		}
		//

		public static List<JObject> GetLearningOpportunitiesForCompetencies( List<string> competencyCTIDs, int skip, int take, ref int totalResults, string clientIP = null )
		{
			var competencies = new JArray( competencyCTIDs.ToArray() );
			var queryData = new JObject()
			{
				{ "ceterms:teaches", new JObject()
				{
					{ "ceterms:targetNode", new JObject()
					{
						{ "ceterms:ctid", competencies }
					} }
				} }
			};

			return DoSimpleQuery( queryData, skip, take, ref totalResults, "https://credentialfinder.org/Finder/GetLearningOpportunitiesForCompetencies/", clientIP );
		}
		//


		public void UpdateCompetencyFrameworkReportTotals()
		{
			var mgr = new CodesManager();

			try
			{
				mgr.UpdateEntityTypes( 10, GetCompetencyFrameworkTermTotal( null ) );
				mgr.UpdateEntityStatistic( 10, "frameworkReport:HasEducationLevels", GetCompetencyFrameworkTermTotal( "ceasn:educationLevelType" ) );
				mgr.UpdateEntityStatistic( 10, "frameworkReport:HasAlignFrom", GetCompetencyFrameworkTermTotal( "ceasn:alignFrom" ) );
				mgr.UpdateEntityStatistic( 10, "frameworkReport:HasAlignTo", GetCompetencyFrameworkTermTotal( "ceasn:alignTo" ) );
				mgr.UpdateEntityStatistic( 10, "frameworkReport:HasBroadAlignment", GetCompetencyFrameworkTermTotal( "ceasn:broadAlignment" ) );
				mgr.UpdateEntityStatistic( 10, "frameworkReport:HasExactAlignment", GetCompetencyFrameworkTermTotal( "ceasn:exactAlignment" ) );
				mgr.UpdateEntityStatistic( 10, "frameworkReport:HasMajorAlignment", GetCompetencyFrameworkTermTotal( "ceasn:majorAlignment" ) );
				mgr.UpdateEntityStatistic( 10, "frameworkReport:HasMinorAlignment", GetCompetencyFrameworkTermTotal( "ceasn:minorAlignment" ) );
				mgr.UpdateEntityStatistic( 10, "frameworkReport:HasNarrowAlignment", GetCompetencyFrameworkTermTotal( "ceasn:narrowAlignment" ) );
				mgr.UpdateEntityStatistic( 10, "frameworkReport:HasPrerequisiteAlignment", GetCompetencyFrameworkTermTotal( "ceasn:prerequisiteAlignment" ) );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "Services.UpdateCompetencyFrameworkReportTotals" );
			}
		}
		//

		public int GetCompetencyFrameworkTermTotal( string searchTerm )
		{
			var queryData = new JObject()
			{
				//Get competency frameworks...
                { "@type","ceasn:CompetencyFramework" },
			};
			if ( !string.IsNullOrWhiteSpace( searchTerm ) )
			{
				queryData.Add( searchTerm, "search:anyValue" );
			}

			var resultData = DoQuery( queryData, 0, 1, "https://credentialfinder.org/Finder/GetCompetencyFrameworkTermTotal/" );
			return resultData.extra.TotalResults;
		}
		//

		public class AsyncDataSet
		{
			public List<AsyncDataItem> Items { get; set; }
			public bool AllFinished { get { return Items.Where( m => m.InProgress ).Count() == 0; } }
		}
		//
		
		public class AsyncDataItem
		{
			public AsyncDataItem()
			{
				CompetencyCTIDs = new List<string>();
				ResultItems = new List<string>();
			}
			public string FrameworkCTID { get; set; }
			public List<string> CompetencyCTIDs { get; set; }
			public List<string> ResultItems { get; set; }
			public bool InProgress { get; set; }
		}
		//

		private static List<JObject> DoSimpleQuery( JObject queryData, int skip, int take, ref int totalResults, string referrer = null, string clientIP = null )
		{
			take = take == 0 ? 20 : take;
			var resultData = DoQuery( queryData, skip, take, "https://credentialfinder.org/Finder/GetLearningOpportunitiesForCompetencies/", clientIP );
			try
			{
				totalResults = resultData.extra.TotalResults;
				return resultData.data.ToObject<List<JObject>>();
			}
			catch
			{
				return new List<JObject>();
			}
		}
		//

		private static CTDLAPIJSONResponse DoQuery( JObject query, int skip, int take, string referrer = null, string clientIP = null )
		{
			var testGUID = Guid.NewGuid().ToString();
			var queryWrapper = new JObject()
			{
				{ "Query", query },
				{ "Skip", skip },
				{ "Take", take }
			};
			var queryJSON = JsonConvert.SerializeObject( queryWrapper );

			//Get API key and URL
			var apiKey = ConfigHelper.GetConfigValue( "CredentialEngineAPIKey", "" );
			var apiURL = ConfigHelper.GetConfigValue( "AssistantCTDLJSONSearchAPIUrl", "" );

			//Make it a little easier to track the source of the requests
			referrer = (string.IsNullOrWhiteSpace( referrer ) ? "https://credentialfinder.org/Finder/" : referrer);
			try
			{
				referrer = referrer + "?ClientIP=" + (string.IsNullOrWhiteSpace( clientIP ) ? HttpContext.Current.Request.UserHostAddress : clientIP);
			}
			catch
			{
				referrer = referrer + "?ClientIP=unknown"; //It seems HttpContext.Current.Request.UserHostAddress might only be available if passed in from the calling thread?
			}
			
			//Do the query
			var client = new HttpClient();
			client.DefaultRequestHeaders.TryAddWithoutValidation( "Authorization", "ApiToken " + apiKey );
			client.DefaultRequestHeaders.Referrer = new Uri( referrer );
			var result = client.PostAsync( apiURL, new StringContent( queryJSON, Encoding.UTF8, "application/json" ) ).Result;
			var rawResultData = result.Content.ReadAsStringAsync().Result ?? "{}";

			var resultData = JsonConvert.DeserializeObject<CTDLAPIJSONResponse>( rawResultData, new JsonSerializerSettings()
			{
				//Ignore errors
				Error = delegate ( object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e ) {
					e.ErrorContext.Handled = true;
				}
			} ) ?? new CTDLAPIJSONResponse();

			return resultData;
		}
		//


		private static List<T> ParseResults<T>( JArray items ) where T : new()
		{
			var properties = typeof( T ).GetProperties();
			var result = new List<T>();
			foreach( var item in items )
			{
				try
				{
					var converted = item.ToObject<T>();
					try
					{
						properties.FirstOrDefault( m => m.Name == "RawData" ).SetValue( converted, item.ToString( Formatting.None ) );
					}
					catch { }
					result.Add( converted );
				}
				catch { }
			}
			return result;
		}
		//

		private class CTDLAPIJSONResponse
		{
			public CTDLAPIJSONResponse()
			{
				data = new JArray();
				extra = new CTDLAPIJsonResponseExtra();
			}
			public JArray data { get; set; }
			public CTDLAPIJsonResponseExtra extra { get; set; }
		}
		private class CTDLAPIJsonResponseExtra
		{
			public int TotalResults { get; set; }
		}
		//

    }
}
