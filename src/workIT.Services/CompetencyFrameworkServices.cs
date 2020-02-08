using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;

using ThisEntity = workIT.Models.Common.CostManifest;
using EntityMgr = workIT.Factories.CostManifestManager;
using workIT.Utilities;
using workIT.Factories;

using workIT.Models.Search;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Web;
using System.Reflection;
using System.Runtime.Caching;

using System.Threading;
using workIT.Models.Helpers.CompetencyFrameworkHelpers;

namespace workIT.Services
{
	public class CompetencyFrameworkServices
	{
		string thisClassName = "CompetencyFrameworkServices";
		ActivityServices activityMgr = new ActivityServices();
		public List<string> messages = new List<string>();

		#region Methods using registry search index
		//Get a Competency Framework description set
		public static CTDLAPICompetencyFrameworkResultSet GetCompetencyFrameworkDescriptionSet(string ctid)
		{
			var queryData = new JObject()
			{
				{ "@type", "ceterms:CompetencyFramework" },
				{ "ceterms:ctid", ctid }
			};

			var clientIP = "unknown";
			try
			{
				clientIP = HttpContext.Current.Request.UserHostAddress;
			}
			catch { }

			var resultData = DoQuery(queryData, 0, 1, "", true, "https://credentialfinder.org/Finder/SearchViaRegistry/", clientIP, "CompetencyFramework");

			var resultSet = new CTDLAPICompetencyFrameworkResultSet()
			{
				Results = ParseResults<CTDLAPICompetencyFrameworkResult>(resultData.data),
				RelatedItems = resultData.extra.RelatedItems,
				TotalResults = resultData.extra.TotalResults
			};

			return resultSet;
		}
		//

		//Use the Credential Registry to search for competency frameworks
		public static CTDLAPICompetencyFrameworkResultSet SearchViaRegistry(MainSearchInput data, bool asDescriptionSet = false)
		{
			//Handle blind searches
			if (string.IsNullOrWhiteSpace(data.Keywords))
			{
				data.Keywords = "search:anyValue";
			}

			var queryData = new JObject()
			{
				//Get competency frameworks...
				{ "@type", "ceasn:CompetencyFramework" },
				{ "search:termGroup", new JObject()
				{
					//Where name or description or CTID matches the keywords, or...
					{ "ceasn:name", data.Keywords },
					{ "ceasn:description", data.Keywords },
					{ "ceterms:ctid", data.Keywords },
					//Where the framework contains a competency (via reverse connection) with competency text that contains the keywords
					{ "ceasn:isPartOf", new JObject() {
						{ "ceasn:competencyText", data.Keywords },
						{ "ceterms:ctid", data.Keywords },
						{ "search:operator", "search:orTerms" }
					} },
					//Or where the creator or publisher have a name or CTID that matches the keywords
					{ "ceasn:creator", new JObject() {
						{ "ceterms:name", data.Keywords },
						{ "ceterms:ctid", data.Keywords },
						{ "search:operator", "search:orTerms" }
					} },
					{ "ceasn:publisher", new JObject() {
						{ "ceterms:name", data.Keywords },
						{ "ceterms:ctid", data.Keywords },
						{ "search:operator", "search:orTerms" }
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

			var orderBy = "";
			var orderDescending = true;
			TranslateSortOrder(data.SortOrder, ref orderBy, ref orderDescending);

			var resultData = DoQuery(queryData, skip, take, orderBy, orderDescending, "https://credentialfinder.org/Finder/SearchViaRegistry/", clientIP, asDescriptionSet ? "CompetencyFramework" : "");

			if (resultData.valid)
			{
				var resultSet = new CTDLAPICompetencyFrameworkResultSet()
				{
					Results = ParseResults<CTDLAPICompetencyFrameworkResult>(resultData.data),
					RelatedItems = resultData.extra.RelatedItems,
					TotalResults = resultData.extra.TotalResults,
				};
				try
				{
					var cacheSuccess = false;
					var cacheident = "";
					resultSet.PerResultRelatedItems = GetRelatedItemsForResults(resultData.data, resultData.extra.RelatedItems, data.Keywords == "search:anyValue", ref cacheSuccess, ref cacheident);
					resultSet.Debug = new JObject()
					{
						{ "Keywords", data.Keywords },
						{ "Use Cache", data.Keywords == "search:anyValue" },
						{ "Cache Succss", cacheSuccess },
						{ "Cache ID", cacheident }
					};
				}
				catch (Exception ex)
				{
					resultSet.PerResultRelatedItems = new List<CTDLAPICompetencyFrameworkRelatedItemSetForSearchResult>()
					{
						new CTDLAPICompetencyFrameworkRelatedItemSetForSearchResult()
						{
							Competencies = new CTDLAPIRelatedItemForSearchResult()
							{
								Label = "Error loading competencies: " + ex.Message,
								Samples = new List<JObject>()
								{
									JObject.FromObject(ex)
								}
							}
						}
					};
				}

				return resultSet;
			}
			else
			{
				var list = new List<CTDLAPICompetencyFrameworkResult>();
				var result = new CTDLAPICompetencyFrameworkResult()
				{
					Name = new LanguageMap("Error encountered"),
					Description = string.IsNullOrWhiteSpace(resultData.status) ? new LanguageMap("Sorry no useable message was returned.") : new LanguageMap(resultData.status)
				};
				list.Add(result);
				return new CTDLAPICompetencyFrameworkResultSet()
				{
					Results = list,
					RelatedItems = null,
					TotalResults = 0
				};
			}
		}
		//

		public static List<FrameworkSearchItem> ThreadedFrameworkSearch(List<FrameworkSearchItem> searchItems)
		{
			var itemSet = new FrameworkSearchItemSet() { Items = searchItems };
			//Trigger the threads
			foreach (var searchItem in itemSet.Items)
			{
				//Set this here to avoid any potential race conditions with the WaitUntiLAllAreFinished method
				searchItem.IsInProgress = true;
				WaitCallback searchMethod = StartFrameworkSearchThread;
				ThreadPool.QueueUserWorkItem(searchMethod, searchItem);
			}
			//Wait for them all to finish
			itemSet.WaitUntilAllAreFinished();

			//Return results
			return itemSet.Items;
		}
		private static void StartFrameworkSearchThread(object frameworkSearchItem)
		{
			//Cast the type and do the search
			var searchItem = (FrameworkSearchItem)frameworkSearchItem;
			try
			{
				var total = 0;
				searchItem.Results = searchItem.ProcessMethod.Invoke(searchItem.CompetencyCTIDs, searchItem.SkipResults, searchItem.TakeResults, ref total, searchItem.ClientIP);
				searchItem.TotalResults = total;
			}
			catch { }
			//When finished, set the variable that will be checked by the FrameworkSearchItemSet.WaitUntilAllAreFinished method
			searchItem.IsInProgress = false;
		}
		//

		public static List<JObject> GetCredentialsForCompetencies(List<string> competencyCTIDs, int skip, int take, ref int totalResults, string clientIP = null)
		{
			var competencies = new JArray(competencyCTIDs.ToArray());
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

			return DoSimpleQuery(queryData, skip, take, "", true, ref totalResults, "https://credentialfinder.org/Finder/GetCredentialsForCompetencies/", clientIP);
		}
		//

		public static List<JObject> GetAssessmentsForCompetencies(List<string> competencyCTIDs, int skip, int take, ref int totalResults, string clientIP = null)
		{
			var competencies = new JArray(competencyCTIDs.ToArray());
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

			return DoSimpleQuery(queryData, skip, take, "", true, ref totalResults, "https://credentialfinder.org/Finder/GetAssessmentsForCompetencies/", clientIP);
		}
		//

		public static List<JObject> GetLearningOpportunitiesForCompetencies(List<string> competencyCTIDs, int skip, int take, ref int totalResults, string clientIP = null)
		{
			var competencies = new JArray(competencyCTIDs.ToArray());
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

			return DoSimpleQuery(queryData, skip, take, "", true, ref totalResults, "https://credentialfinder.org/Finder/GetLearningOpportunitiesForCompetencies/", clientIP);
		}
		//


		public void UpdateCompetencyFrameworkReportTotals()
		{
			var mgr = new CodesManager();
			bool usingCFTotals = true;
			try
			{
				mgr.UpdateEntityTypes(10, GetCompetencyFrameworkTermTotal(null));
				if (usingCFTotals)
				{
					mgr.UpdateEntityStatistic(10, "frameworkReport:HasEducationLevels", GetCompetencyFrameworkTermTotal("ceasn:educationLevelType"));

					mgr.UpdateEntityStatistic(10, "frameworkReport:HasAlignFrom", GetCompetencyFrameworksWithCompetencyTermTotal("ceasn:alignFrom"));
					mgr.UpdateEntityStatistic(10, "frameworkReport:HasAlignTo", GetCompetencyFrameworksWithCompetencyTermTotal("ceasn:alignTo"));

					mgr.UpdateEntityStatistic(10, "frameworkReport:HasBroadAlignment", GetCompetencyFrameworksWithCompetencyTermTotal("ceasn:broadAlignment"));
					mgr.UpdateEntityStatistic(10, "frameworkReport:HasExactAlignment", GetCompetencyFrameworksWithCompetencyTermTotal("ceasn:exactAlignment"));

					mgr.UpdateEntityStatistic(10, "frameworkReport:HasMajorAlignment", GetCompetencyFrameworksWithCompetencyTermTotal("ceasn:majorAlignment"));
					mgr.UpdateEntityStatistic(10, "frameworkReport:HasMinorAlignment", GetCompetencyFrameworksWithCompetencyTermTotal("ceasn:minorAlignment"));

					mgr.UpdateEntityStatistic(10, "frameworkReport:HasNarrowAlignment", GetCompetencyFrameworksWithCompetencyTermTotal("ceasn:narrowAlignment"));
					mgr.UpdateEntityStatistic(10, "frameworkReport:HasPrerequisiteAlignment", GetCompetencyFrameworksWithCompetencyTermTotal("ceasn:prerequisiteAlignment"));
				}
				else
				{
					mgr.UpdateEntityStatistic(10, "frameworkReport:HasEducationLevels", GetCompetencyTermTotal("ceasn:educationLevelType"));

					mgr.UpdateEntityStatistic(10, "frameworkReport:HasAlignFrom", GetCompetencyTermTotal("ceasn:alignFrom"));
					mgr.UpdateEntityStatistic(10, "frameworkReport:HasAlignTo", GetCompetencyTermTotal("ceasn:alignTo"));

					mgr.UpdateEntityStatistic(10, "frameworkReport:HasBroadAlignment", GetCompetencyTermTotal("ceasn:broadAlignment"));
					mgr.UpdateEntityStatistic(10, "frameworkReport:HasExactAlignment", GetCompetencyTermTotal("ceasn:exactAlignment"));

					mgr.UpdateEntityStatistic(10, "frameworkReport:HasMajorAlignment", GetCompetencyTermTotal("ceasn:majorAlignment"));
					mgr.UpdateEntityStatistic(10, "frameworkReport:HasMinorAlignment", GetCompetencyTermTotal("ceasn:minorAlignment"));

					mgr.UpdateEntityStatistic(10, "frameworkReport:HasNarrowAlignment", GetCompetencyTermTotal("ceasn:narrowAlignment"));
					mgr.UpdateEntityStatistic(10, "frameworkReport:HasPrerequisiteAlignment", GetCompetencyTermTotal("ceasn:prerequisiteAlignment"));
				}
			}
			catch (Exception ex)
			{
				LoggingHelper.LogError(ex, "Services.UpdateCompetencyFrameworkReportTotals");
			}
		}
		//
		public int GetCompetencyFrameworkTermTotal(string searchTerm)
		{
			var queryData = new JObject()
			{
				//Get competency frameworks...
                { "@type","ceasn:CompetencyFramework" },
			};
			if (!string.IsNullOrWhiteSpace(searchTerm))
			{
				queryData.Add(searchTerm, "search:anyValue");
			}

			var resultData = DoQuery(queryData, 0, 1, "", true, "https://credentialfinder.org/Finder/GetCompetencyTermTotal/");
			return resultData.extra.TotalResults;
		}
		//
		public int GetCompetencyTermTotal(string searchTerm)
		{
			var queryData = new JObject()
			{
				//Get competency frameworks...
                { "@type","ceasn:Competency" },
			};
			if (!string.IsNullOrWhiteSpace(searchTerm))
			{
				queryData.Add(searchTerm, "search:anyValue");
			}

			var resultData = DoQuery(queryData, 0, 1, "", true, "https://credentialfinder.org/Finder/GetCompetencyTermTotal/");
			return resultData.extra.TotalResults;
		}
		//
		public int GetCompetencyFrameworksWithCompetencyTermTotal(string searchTerm)
		{
			if (string.IsNullOrWhiteSpace(searchTerm))
				return 0;
			//var termData = new JObject()
			//{
			//	//TODO: may need to include the list of credential types here (as a parameter) - probably not necessary?
			//	//Find anything that requires...
			//	{ "ceasn:isPartOf", new JObject()
			//		{
			//			{ searchTerm, "search:anyValue" }
			//		}
			//	}
			//};
			var queryData = new JObject()
			{
				//Get competency frameworks...
                { "@type","ceasn:CompetencyFramework" },
			};
			queryData.Add("ceasn:isPartOf", new JObject()
					{
						{ searchTerm, "search:anyValue" }
					});


			//string query = "@type\":\"ceasn:CompetencyFramework\",\"ceasn:isPartOf\": {\"" + searchTerm + "\": \"search:anyValue\"}";

			var resultData = DoQuery(queryData, 0, 1, "", true, "https://credentialfinder.org/Finder/GetCompetencyTermTotal/");
			return resultData.extra.TotalResults;
		}
		//
		public class AsyncDataSet
		{
			public List<AsyncDataItem> Items { get; set; }
			public bool AllFinished { get { return Items.Where(m => m.InProgress).Count() == 0; } }
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

		private static List<JObject> DoSimpleQuery(JObject queryData, int skip, int take, string orderBy, bool orderDescending, ref int totalResults, string referrer = null, string clientIP = null)
		{
			take = take == 0 ? 20 : take;
			var resultData = DoQuery(queryData, skip, take, orderBy, orderDescending, "https://credentialfinder.org/Finder/GetLearningOpportunitiesForCompetencies/", clientIP);
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

		private static CTDLAPIJSONResponse DoQuery(JObject query, int skip, int take, string orderBy, bool orderDescending, string referrer = null, string clientIP = null, string descriptionSetType = null)
		{
			var testGUID = Guid.NewGuid().ToString();
			var queryWrapper = new JObject()
			{
				{ "Query", query },
				{ "Skip", skip },
				{ "Take", take },
				{ "OrderBy", orderBy },
				{ "OrderDescending", orderDescending }
			};
			if (!string.IsNullOrWhiteSpace(descriptionSetType))
			{
				queryWrapper["DescriptionSetType"] = descriptionSetType;
			}
			var queryJSON = JsonConvert.SerializeObject(queryWrapper);

			//Get API key and URL
			var apiKey = ConfigHelper.GetConfigValue("CredentialEngineAPIKey", "");
			var apiURL = ConfigHelper.GetConfigValue("AssistantCTDLJSONSearchAPIUrl", "");

			//Make it a little easier to track the source of the requests
			referrer = (string.IsNullOrWhiteSpace(referrer) ? "https://credentialfinder.org/Finder/" : referrer);
			try
			{
				referrer = referrer + "?ClientIP=" + (string.IsNullOrWhiteSpace(clientIP) ? HttpContext.Current.Request.UserHostAddress : clientIP);
			}
			catch
			{
				referrer = referrer + "?ClientIP=unknown"; //It seems HttpContext.Current.Request.UserHostAddress might only be available if passed in from the calling thread?
			}

			//Do the query
			var client = new HttpClient();
			client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "ApiToken " + apiKey);
			client.DefaultRequestHeaders.Referrer = new Uri(referrer);
			var result = client.PostAsync(apiURL, new StringContent(queryJSON, Encoding.UTF8, "application/json")).Result;
			var rawResultData = result.Content.ReadAsStringAsync().Result ?? "{}";

			var resultData = JsonConvert.DeserializeObject<CTDLAPIJSONResponse>(rawResultData, new JsonSerializerSettings()
			{
				//Ignore errors
				Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e) {
					e.ErrorContext.Handled = true;
				}
			}) ?? new CTDLAPIJSONResponse();

			return resultData;
		}
		//


		private static List<T> ParseResults<T>(JArray items) where T : new()
		{
			var properties = typeof(T).GetProperties();
			var result = new List<T>();
			if (items == null || items.Count == 0)
				return result;

			foreach (var item in items)
			{
				try
				{
					var converted = item.ToObject<T>();
					try
					{
						properties.FirstOrDefault(m => m.Name == "RawData").SetValue(converted, item.ToString(Formatting.None));
					}
					catch { }
					result.Add(converted);
				}
				catch { }
			}
			return result;
		}
		//

		private static void TranslateSortOrder(string searchSortOrder, ref string orderBy, ref bool orderDescending)
		{
			switch (searchSortOrder)
			{
				case "alpha":
					{
						orderBy = "name";
						orderDescending = false;
						break;
					}
				case "newest":
					{
						orderBy = "updated";
						orderDescending = true;
						break;
					}
				default:
					{
						orderBy = "";
						orderDescending = false;
						break;
					}
			}
		}

		private class CTDLAPIJSONResponse
		{
			public CTDLAPIJSONResponse()
			{
				data = new JArray();
				extra = new CTDLAPIJsonResponseExtra();
			}
			public JArray data { get; set; }
			public CTDLAPIJsonResponseExtra extra { get; set; }
			public bool valid { get; set; }
			public string status { get; set; }
		}
		private class CTDLAPIJsonResponseExtra
		{
			public CTDLAPIJsonResponseExtra()
			{
				RelatedItems = new JArray();
			}
			public int TotalResults { get; set; }
			public JArray RelatedItems { get; set; }
		}
		//
		#endregion
		public static EducationFramework GetEducationFrameworkByCtid(string ctid)
		{
			EducationFramework entity = new EducationFramework();
			entity = EducationFrameworkManager.GetByCtid(ctid);
			return entity;
		}

		public static EducationFramework Get(int id)
		{
			EducationFramework entity = new EducationFramework();
			entity = EducationFrameworkManager.Get(id);
			return entity;
		}

		public static List<CTDLAPICompetencyFrameworkRelatedItemSetForSearchResult> GetRelatedItemsForResults(JArray rawResults, JArray rawRelatedItems, bool useCache, ref bool cacheSuccess, ref string cacheident)
		{

			//Get List<JObject> for the two arrays
			var relatedItemSets = new List<CTDLAPICompetencyFrameworkRelatedItemSetForSearchResult>();
			var results = rawResults.ToList().ConvertAll(m => (JObject)m).ToList();

			//Skip the hard part if possible
			var cacheID = "CompetencyFrameworkCache_" + string.Join(",", results.Select(m => (string)m["ceterms:ctid"] ?? "").ToList());
			cacheident = cacheID;
			cacheSuccess = false;
			var cache = MemoryCache.Default;
			if (useCache)
			{
				var data = cache[cacheID];
				if (data != null)
				{
					cacheSuccess = true;
					return JsonConvert.DeserializeObject<List<CTDLAPICompetencyFrameworkRelatedItemSetForSearchResult>>((string)data);
				}
			}

			var relatedItems = rawRelatedItems.ToList().ConvertAll(m => (JObject)m).ToList();

			//Hold onto these for later
			var credentialTypes = new List<string>() { "ceterms:ApprenticeshipCertificate", "ceterms:AssociateDegree", "ceterms:BachelorDegree", "ceterms:Badge", "ceterms:Certificate", "ceterms:Certification", "ceterms:Degree", "ceterms:DigitalBadge", "ceterms:Diploma", "ceterms:DoctoralDegree", "ceterms:GeneralEducationDevelopment", "ceterms:JourneymanCertificate", "ceterms:License", "ceterms:MasterCertificate", "ceterms:MasterDegree", "ceterms:MicroCredential", "ceterms:OpenBadge", "ceterms:ProfessionalDoctorate", "ceterms:QualityAssuranceCredential", "ceterms:ResearchDoctorate", "ceterms:SecondarySchoolDiploma" }; //Should probably retrieve this dynamically
			var connectionProperties = new List<string>() { "ceasn:creator", "ceasn:publisher", "ceasn:isPartOf", "ceasn:abilityEmbodied", "ceasn:skillEmbodied", "knowledgeEmbodied", "skos:inScheme", "ceterms:targetNode", "ceterms:targetCredential", "ceterms:targetAssessment", "ceterms:targetLearningOpportunity" };
			var alignmentProperties = new List<string>() { "ceasn:alignFrom", "ceasn:alignTo", "ceasn:broadAlignment", "ceasn:exactAlignment", "ceasn:narrowAlignment", "ceasn:majorAlignment", "ceasn:minorAlignment", "ceasn:prerequisiteAlignment" };
			var conceptProperties = new List<string>() { "ceasn:conceptTerm", "ceasn:complexityLevel" };
			var associativeProperties = connectionProperties.Concat(alignmentProperties).Concat(conceptProperties).ToList();
			var tripleProperties = typeof(RDFTriple).GetProperties();
			var subjectURIProperty = tripleProperties.FirstOrDefault(m => m.Name == "SubjectURI");
			var objectURIProperty = tripleProperties.FirstOrDefault(m => m.Name == "ObjectURI");

			//Build triples
			var allItems = results.Concat(relatedItems).ToList();
			var triples = new List<RDFTriple>();
			foreach (var item in allItems)
			{
				var label = GetEnglish(item["ceterms:name"] ?? item["ceasn:name"] ?? item["ceasn:competencyLabel"] ?? item["ceasn:competencyText"] ?? item["skos:prefLabel"] ?? "Item");
				foreach (var property in item.Properties())
				{
					var path = new List<string>() { property.Name };
					RecursivelyExtractTriples(property.Name, property.Value, path, (string)item["@type"], (string)item["@id"], label, (string)item["ceterms:ctid"], triples, associativeProperties);
				}
			}

			//Handle results
			var allCredentialTriples = triples.Where(m => credentialTypes.Contains(m.SubjectType)).ToList();
			var allConceptSchemeTriples = triples.Where(m => m.SubjectType == "skos:ConceptScheme").ToList();
			foreach (var result in results)
			{
				var debug = "";

				//Triples for the Framework
				var resultURI = result["@id"].ToString();
				var outgoingTriples = triples.Where(m => m.SubjectURI == resultURI).ToList();
				var incomingTriples = triples.Where(m => m.ObjectURI == resultURI).ToList();
				debug += "Outgoing Triples: " + outgoingTriples.Count() + "\n";
				debug += "Incoming Triples: " + incomingTriples.Count() + "\n";

				//Competencies and triples for the Competencies
				var competencyURIs = incomingTriples.Where(m => m.Path.Contains("ceasn:isPartOf")).Select(m => m.SubjectURI).ToList();
				var competencies = relatedItems.Where(m => competencyURIs.Contains((string)m["@id"] ?? "")).ToList();
				var outgoingCompetencyTriples = triples.Where(m => competencyURIs.Contains(m.SubjectURI)).ToList();
				var incomingCompetencyTriples = triples.Where(m => competencyURIs.Contains(m.ObjectURI)).ToList();
				debug += "Outgoing Competency Triples: " + outgoingCompetencyTriples.Count() + "\n";
				debug += "Incoming Competency Triples: " + incomingCompetencyTriples.Count() + "\n";

				//Framework-level relationships
				var publishers = FindRelated(objectURIProperty, outgoingTriples, delegate (RDFTriple m) { return m.Path.Contains("ceasn:publisher"); }, relatedItems);
				var creators = FindRelated(objectURIProperty, outgoingTriples, delegate (RDFTriple m) { return m.Path.Contains("ceasn:creator"); }, relatedItems);
				var outAlignedFrameworks = FindRelated(objectURIProperty, outgoingTriples, delegate (RDFTriple m) { return m.Path.Intersect(alignmentProperties).Count() > 0; }, relatedItems);
				var inAlignedFrameworks = FindRelated(objectURIProperty, incomingTriples, delegate (RDFTriple m) { return m.SubjectType == "ceasn:CompetencyFramework"; }, relatedItems);

				//Competency-level relationships
				var assessments = FindRelated(subjectURIProperty, incomingCompetencyTriples, delegate (RDFTriple m) { return m.SubjectType == "ceterms:AssessmentProfile"; }, relatedItems);
				var assessmentURIs = assessments.Select(m => (string)m["@id"] ?? "").ToList();
				debug += "Assessments: " + assessments.Count() + "\n";
				debug += "Subject URI Property: " + subjectURIProperty.Name + "\n";
				debug += "Related Items: " + relatedItems.Count();
				debug += "Test: " + FindRelated(subjectURIProperty, incomingCompetencyTriples, delegate (RDFTriple m) { return true; }, relatedItems).Count();
				var learningOpportunities = FindRelated(subjectURIProperty, incomingCompetencyTriples, delegate (RDFTriple m) { return m.SubjectType == "ceterms:LearningOpportunityProfile"; }, relatedItems);
				var learningOpportunityURIs = learningOpportunities.Select(m => (string)m["@id"] ?? "").ToList();
				var credentials = FindRelated(subjectURIProperty, incomingCompetencyTriples, delegate (RDFTriple m) { return credentialTypes.Contains(m.SubjectType); }, relatedItems);
				var credentialsViaAssessments = FindRelated(subjectURIProperty, allCredentialTriples, delegate (RDFTriple m) { return assessmentURIs.Contains(m.ObjectURI); }, relatedItems);
				var credentialsViaLearningOpportunities = FindRelated(subjectURIProperty, allCredentialTriples, delegate (RDFTriple m) { return learningOpportunityURIs.Contains(m.ObjectURI); }, relatedItems);
				var concepts = FindRelated(objectURIProperty, outgoingCompetencyTriples, delegate (RDFTriple m) { return m.Path.Intersect(conceptProperties).Count() > 0; }, relatedItems);
				var conceptSchemeURIs = concepts.Select(m => (string)m["skos:inScheme"] ?? "").Distinct().ToList();
				var conceptSchemes = FindRelated(subjectURIProperty, allConceptSchemeTriples, delegate (RDFTriple m) { return conceptSchemeURIs.Contains(m.SubjectURI); }, relatedItems);
				var outAlignedCompetencies = FindRelated(objectURIProperty, outgoingCompetencyTriples, delegate (RDFTriple m) { return m.Path.Intersect(alignmentProperties).Count() > 0; }, relatedItems);
				var inAlignedCompetencies = FindRelated(subjectURIProperty, incomingCompetencyTriples, delegate (RDFTriple m) { return m.Path.Intersect(alignmentProperties).Count() > 0; }, relatedItems);

				//Store the data
				var dataSet = new CTDLAPICompetencyFrameworkRelatedItemSetForSearchResult()
				{
					RelatedItemsForCTID = (string)result["ceterms:ctid"],
					Publishers = new CTDLAPIRelatedItemForSearchResult("ceasn:publisher", "# Publishers", publishers),
					Creators = new CTDLAPIRelatedItemForSearchResult("ceasn:creator", "# Creators", creators),
					Owners = new CTDLAPIRelatedItemForSearchResult("meta:owner", "# Owners", GetDistinctObjects(publishers.Concat(creators).ToList())),
					Competencies = new CTDLAPIRelatedItemForSearchResult("ceasn:Competency", "# Competencies", competencies),
					Credentials = new CTDLAPIRelatedItemForSearchResult("ceterms:Credential", "# Credentials", GetDistinctObjects(credentials.Concat(credentialsViaAssessments).Concat(credentialsViaLearningOpportunities).ToList())),
					Assessments = new CTDLAPIRelatedItemForSearchResult("ceterms:Assessment", "# Assessments", assessments),
					LearningOpportunities = new CTDLAPIRelatedItemForSearchResult("ceterms:LearningOpportunity", "# Learning Opportunities", learningOpportunities),
					ConceptSchemes = new CTDLAPIRelatedItemForSearchResult("skos:ConceptScheme", "# Concept Schemes", conceptSchemes),
					Concepts = new CTDLAPIRelatedItemForSearchResult("skos:Concept", "# Concepts", concepts),
					AlignedFrameworks = new CTDLAPIRelatedItemForSearchResult("meta:AlignedFramework", "# Aligned Frameworks", GetDistinctObjects(outAlignedFrameworks.Concat(inAlignedFrameworks).ToList())),
					AlignedCompetencies = new CTDLAPIRelatedItemForSearchResult("meta:AlignedCompetency", "# Aligned Competencies", GetDistinctObjects(outAlignedCompetencies.Concat(inAlignedCompetencies).ToList()))
				};
				relatedItemSets.Add(dataSet);
			}

			//Skip the hard part next time, if applicable
			if (useCache)
			{
				cache.Remove(cacheID);
				cache.Add(cacheID, JsonConvert.SerializeObject(relatedItemSets), new CacheItemPolicy() { AbsoluteExpiration = DateTime.Now.AddMinutes(15) });
			}

			return relatedItemSets;
		}
		private static void RecursivelyExtractTriples(string name, JToken value, List<string> path, string _type, string _id, string label, string ctid, List<RDFTriple> triples, List<string> associativeProperties)
		{
			if (value.Type == JTokenType.Array)
			{
				foreach (var itemValue in ((JArray)value))
				{
					RecursivelyExtractTriples(name, itemValue, path, _type, _id, label, ctid, triples, associativeProperties);
				}
			}
			else if (value.Type == JTokenType.Object)
			{
				foreach (var property in ((JObject)value).Properties())
				{
					var newPath = path.Concat(new List<string>() { property.Name }).ToList(); //Don't overwrite the original path, since it needs to branch for different parts of the recursion
					RecursivelyExtractTriples(property.Name, property.Value, newPath, _type, _id, label, ctid, triples, associativeProperties);
				}
			}
			else
			{
				if (associativeProperties.Contains(name))
				{
					triples.Add(new RDFTriple()
					{
						SubjectType = _type,
						SubjectURI = _id,
						SubjectLabel = label,
						SubjectCTID = ctid,
						Path = path,
						ObjectURI = value.ToString()
					});
				}
			}
		}
		private static List<JObject> FindRelated(PropertyInfo desiredURIProperty, List<RDFTriple> lookIn, Func<RDFTriple, bool> matchFunction, List<JObject> relatedItems)
		{
			var matchingURIs = lookIn.Where(m => matchFunction(m)).Select(m => desiredURIProperty.GetValue(m).ToString()).ToList();
			return relatedItems.Where(m => matchingURIs.Contains((string)m["@id"] ?? "")).ToList();
		}
		private static List<JObject> GetDistinctObjects(List<JObject> items)
		{
			var results = new List<JObject>();
			var uniqueURIs = items.Select(m => (string)m["@id"] ?? "").Distinct().ToList();
			foreach (var uri in uniqueURIs)
			{
				results.Add(items.FirstOrDefault(m => ((string)m["@id"] ?? "") == uri));
			}
			return results;
		}
		public static string GetEnglish(JToken data)
		{
			if (data == null)
			{
				return "";
			}
			else if (data.Type == JTokenType.String)
			{
				return data.ToString();
			}
			else if (data.Type == JTokenType.Object)
			{
				return (string)(data["en"] ?? data["en-us"] ?? data["en-US"]) ?? "";
			}
			else
			{
				return "";
			}
		}
		public class RDFTriple
		{
			public string SubjectType { get; set; }
			public string SubjectURI { get; set; }
			public string SubjectLabel { get; set; }
			public string SubjectCTID { get; set; }
			public List<string> Path { get; set; }
			public string ObjectURI { get; set; }
		}
	}
}
