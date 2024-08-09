using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Runtime.Caching;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using workIT.Services;
using workIT.Factories;
using API = workIT.Services.API;
using workIT.Models.Search;

namespace CredentialFinderWebAPI.Services
{
	public class ExportServices
	{
		public static Attempt<string> ExportCSV( MainSearchInput query, Guid transactionGUID, string appDomainURL, string registryDomainURL, JObject debug = null, bool allowCache = true )
		{
			//Hold the result
			var result = new Attempt<string>() { Valid = false, Debug = debug ?? new JObject() }; 
			var progressTracker = ProgressTrackingManager.UpdateProgressTracker( transactionGUID, true, 0, tracker => tracker.Messages.Add( "Starting Export..." ) );

			//Track timing
			var timer = new System.Diagnostics.Stopwatch();
			timer.Start();

			try
			{
				//Normalize the query
				query.PageSize = 500; //Probably should be a web.config value. Note 10,000 is the maximum size allowed by elastic by default
				query.StartPage = 1;
				query.IsExportMode = true;
				var cacheKey = "";

				//Return data from the cache, if applicable
				//For now, only cache blind searches/exports
				if ( string.IsNullOrWhiteSpace( query.Keywords ) && query.FiltersV2.Count() == 0 && allowCache )
				{
					cacheKey = "QueryExport_" + JObject.FromObject( query ).ToString( Formatting.None );
					var cachedResult = ( string ) MemoryCache.Default.Get( cacheKey );
					if ( !string.IsNullOrWhiteSpace( cachedResult ) )
					{
						result.Messages.Add( "Returning data from cache" );
						result.Data = cachedResult;
						result.Valid = true;
						timer.Stop();
						return result;
					}
				}

				//Setup the related stuff
				var searchService = new SearchServices();
				var loadedResults = new List<JObject>();
				var valid = true;
				var status = "";

				//Query the first page and log some things
				var firstPage = searchService.MainSearch( query, ref valid, ref status, result.Debug );
				loadedResults.AddRange( firstPage?.RelatedItems?.Where( m => m.Type == JTokenType.Object ).Select( m => ( JObject ) m ).ToList() ?? new List<JObject>() );
				result.Debug.Add( "Export Query", JObject.FromObject( query ) );

				//Handle initial errors
				if ( !valid )
				{
					return InvalidateAttempt( result, status );
				}

				//Handle empty result set
				if ( firstPage.TotalResults <= 0 || loadedResults.Count() == 0 )
				{
					return InvalidateAttempt( result, "No results found for the query." );
				}

				result.Messages.Add( "First page completed at " + timer.ElapsedMilliseconds + "ms" );
				//Get the rest of the pages, if there are any, with parallelization
				var totalPages = ( int ) Math.Ceiling( ( ( double ) firstPage.TotalResults ) / ( ( double ) query.PageSize ) );

				progressTracker.ThreadSafeUpdate( () =>
				{
					progressTracker.Messages.Add( "Exporting " + firstPage.TotalResults + " Results..." );
					progressTracker.TotalItems = firstPage.TotalResults;
					progressTracker.SuccessfulItems = firstPage.RelatedItems.Count();
					progressTracker.ErroneousItems = Math.Min( query.PageSize, firstPage.TotalResults ) - firstPage.RelatedItems.Count();
					progressTracker.ProcessedItems = Math.Min( query.PageSize, firstPage.TotalResults );
				} );

				result.Messages.Add( "Found " + firstPage.TotalResults + " Total Results (" + totalPages + " Pages)" );
				result.Messages.Add( "Page 1 returned " + firstPage.RelatedItems.Count() + " Items" );

				if ( totalPages > 1 )
				{
					//Setup a holder for each page of results
					//Doing this in advance lets us assemble the results in the correct sort order later on
					var pageTrackers = new List<QueryAndResultSet>();
					//StartPage is a 1-based index, and the first page was already retrieved, so start with page 2 and use <= instead of <
					for ( var i = 2; i <= totalPages; i++ )
					{
						var tracker = new QueryAndResultSet()
						{
							Query = JObject.FromObject( query ).ToObject<MainSearchInput>(),
							Result = new MainSearchResults(),
							Status = ""
						};

						tracker.Query.StartPage = i;

						pageTrackers.Add( tracker );
					}

					//Process each page of the query in parallel (in my testing, this is faster than doing serial queries with very large page sizes)
					ProgressTrackingManager.ProcessInParallelAndTrack( transactionGUID, pageTrackers, ( pageTracker, progress, index ) =>
					{
						//Get the data for this page of results
						var validItem = true;
						var statusItem = "";
						pageTracker.Result = searchService.MainSearch( pageTracker.Query, ref validItem, ref statusItem );
						pageTracker.Valid = validItem;
						pageTracker.Status = statusItem;

						//Figure out whether any data is missing
						var totalItemsForPage = pageTracker.Result.RelatedItems.Count();

						//The expected number of items for the page is smaller for the last page
						var expectedTotal = pageTracker.Query.StartPage == totalPages ? firstPage.TotalResults % query.PageSize : query.PageSize;
						expectedTotal = expectedTotal == 0 ? query.PageSize : expectedTotal;

						//Figure out whether any items are missing from this page (sometimes .RelatedItems is less than the number it should be)
						var missingItems = expectedTotal - totalItemsForPage;
						if ( missingItems > 0 )
						{
							progress.Errors.Add( "Skipping " + ( pageTracker.Query.PageSize - pageTracker.Result.RelatedItems.Count() ) + " empty results for page " + pageTracker.Query.StartPage + "." );
						}

						//Update the tracker's counts
						progress.SuccessfulItems += totalItemsForPage;
						progress.ErroneousItems += missingItems;
						progress.ProcessedItems += expectedTotal;

					}, ( pageTracker, progress, index, ex ) => {
						//Handle errors
						progress.Errors.Add( "Error processing page " + pageTracker.Query.StartPage + ": " + ex.Message.ToString() );
					}, ( progress ) =>
					{
						//Handle cancelation
						progress.Messages.Add( "Canceled processing." );
					}, false );

					if ( progressTracker.CancelProcessing )
					{
						return InvalidateAttempt( result, "User canceled processing." );
					}

					//Recombine the pages in the proper order
					foreach ( var pageTracker in pageTrackers )
					{
						if ( pageTracker.Valid )
						{
							result.Messages.Add( "Page " + pageTracker.Query.StartPage + " returned " + pageTracker.Result.RelatedItems.Count() + " Items (expected " + ( pageTracker.Query.StartPage == totalPages ? "up to " : "" ) + pageTracker.Query.PageSize + ")" );
							loadedResults.AddRange( pageTracker.Result.RelatedItems.Where( m => m.Type == JTokenType.Object ).Select( m => ( JObject ) m ).ToList() );
						}
						else
						{
							result.Messages.Add( "Error processing page " + pageTracker.Query.StartPage + " of results: " + pageTracker.Status );
						}
					}
				}

				/*
				//Get the rest of the pages without parallelization
				var hasMoreResults = translatedResults.TotalResults > query.PageSize * query.StartPage;
				while ( hasMoreResults )
				{
					query.StartPage++;
					resultsPage = searchService.MainSearch( query, ref valid, ref status ); //Probably don't need to pass the debug object here
					if ( valid )
					{
						loadedResults.AddRange( resultsPage.RelatedItems.Where( m => m.Type == JTokenType.Object ).Select( m => ( JObject ) m ).ToList() );
					}
					else
					{
						result.Messages.Add( "Error loading results for page " + query.StartPage + ": " + status ); //Consider skipping the rest of the pages(?)
					}

					hasMoreResults = translatedResults.TotalResults > query.PageSize * query.StartPage;
				}
				*/

				result.Messages.Add( "All results loaded at " + timer.ElapsedMilliseconds + "ms" );
				result.Messages.Add( "Loaded " + loadedResults.Count() + " Items (expected " + firstPage.TotalResults + ")" );
				progressTracker.ThreadSafeUpdate( () =>
				{
					progressTracker.ProcessedItems = firstPage.TotalResults;
					progressTracker.Messages.Add( "Results loaded. Formatting Results..." );
				} );

				//Prepare an object to hold the data for all of the rows in the export
				var dataForRows = new List<JObject>();

				//Enable tracking which columns actually have data at any point
				var activeColumns = new List<string>();

				//Enable adding intermediate rows
				var intermediateRows = new List<JObject>();

				//Enable adding columns dynamically (where the order of columns does not matter/cannot realistically be controlled for)
				var dynamicColumns = new List<string>();

				//For each search result...
				//This could probably be done in parallel, but is pretty fast as-is relative to the query step. Something to consider for later if it comes up (mind the concurrency issues with all of the List<>s above though!)
				foreach ( JObject item in loadedResults )
				{
					try
					{
						//Construct the object for this row
						//This list eventually dictates the overall order of columns in the final data regardless of what data is or isn't present or what type of data is being exported
						//Note that for complex data, the header string listed here must exactly match the header string provided in one of the methods further down!
						//If this is slow, consider refactoring into a Dictionary<string, Func<JObject, JToken>> outside of the foreach method
						var rowData = new JObject()
						{
							{ "Row Type", "Resource" },
							{ "CTID", item[ "CTID" ] },
							{ "CTDL Type", item["CTDLType"] },
							{ "CTDL Type Label", item["CTDLTypeLabel"] },
							{ "Name", item["Name"] },
							{ "Alternate Name(s)", NormalizeArrayToString( " | ", item["AlternateName"] ) },
							{ "Description", item["Description"] },
							{ "Credential Status Type", TokenOrNull( item["CredentialStatusType"] )?["Label"] },
							{ "Life Cycle Status Type", TokenOrNull( item["LifeCycleStatusType"])?["Label"] },
							{ "Address Identifier", null },
							{ "Address", null },
							{ "Finder ID", item["Meta_Id"] },
							{ "Identifier", NormalizeIdentifierValuesToString( item["Identifier"] ) },
							{ "In Language(s)", NormalizeArrayToString( " | ", item["InLanguage"] ) },
							{ "Last Updated", item["Meta_LastUpdated"] ?? item["EntityLastUpdated"] ?? "Unknown" },
							{ "Organization Type", NormalizeLabelsToString( item["AgentType"] ) },
							{ "Sector Type", NormalizeLabelsToString( item["AgentSectorType"] ) },
							{ "Owned By/Offered By", TokenOrNull( item["BroadType"] )?.ToString() == "Organization" ? null : NormalizeLabelsToString( item["OwnedByLabel"] ?? item["OfferedByLabel"] ) },
							{ "Time Estimate(s)", NormalizeInternalValuesToString( item["EstimatedDuration"], durationProfile => ( durationProfile["DurationSummary"] ?? durationProfile["Description"] ?? "" ).ToString() ) },
							{ "Industry Code(s)", NormalizeLabelsToString( item["IndustryType"] ) },
							{ "Occupation Code(s)", NormalizeLabelsToString( item["OccupationType"] ) },
							{ "Instructional Program Code(s)", NormalizeLabelsToString( item["InstructionalProgramType"] ) },
							{ "Delivery Type(s)", NormalizeLabelsToString( item["DeliveryType"] ) },
							{ "Learning Delivery Type(s)", NormalizeLabelsToString( item["LearningDeliveryType"] ) },
							{ "Subject Webpage", NormalizeArrayToString( " | ", item["SubjectWebpage"] ) },
							{ "Keywords", NormalizeLabelsToString( item["Keyword"] ) },
							{ "Start Date", item["StartDate"] },
							{ "End Date", item["EndDate"] },
							{ "Transfer Value Credit", null },
							{ "Transfer Value Credit Level(s)", null },
							{ "Transfer Value Credit Unit(s)", null },
							{ "Transfer Value Credit Subject(s)", null },
							{ "Transfer Value From - Finder URL", null },
							{ "Transfer Value From Resource - Type", null },
							{ "Transfer Value From Resource - Name", null },
							{ "Transfer Value From Resource - Description", null },
							{ "Transfer Value From Resource - Provider - Name", null },
							{ "Transfer Value From Resource - Provider - Finder URL", null },
							{ "Transfer Value For - Finder URL", null },
							{ "Transfer Value For Resource - Type", null },
							{ "Transfer Value For Resource - Name", null },
							{ "Transfer Value For Resource - Description", null },
							{ "Transfer Value For Resource - Provider - Name", null },
							{ "Transfer Value For Resource - Provider - Finder URL", null },
							{ "Receives Transfer Value From - Finder URL", null },
							{ "Receives Transfer Value From Resource - Type", null },
							{ "Receives Transfer Value From Resource - Name", null },
							{ "Receives Transfer Value From Resource - Description", null },
							{ "Receives Transfer Value From Resource - Provider - Name", null },
							{ "Receives Transfer Value From Resource - Provider - Finder URL", null },
							{ "Provides Transfer Value For - Finder URL", null },
							{ "Provides Transfer Value For Resource - Type", null },
							{ "Provides Transfer Value For Resource - Name", null },
							{ "Provides Transfer Value For Resource - Description", null },
							{ "Provides Transfer Value For Resource - Provider - Name", null },
							{ "Provides Transfer Value For Resource - Provider - Finder URL", null },
							{ "Related Transfer Value - Name", null },
							{ "Related Transfer Value - Description", null },
							{ "Related Transfer Value - Finder URL", null },
							{ "Related Transfer Value - Provider - Name", null },
							{ "Related Transfer Value - Provider - Finder URL", null },
							{ "Acting Agent - Name", null },
							{ "Acting Agent - URL", null },
							{ "Instrument - Name", null },
							{ "Instrument - URL", null },
							{ "Evidence of Action", NormalizeArrayToString( " | ", item["EvidenceOfAction"] ) },
							{ "Object (Action Recipient) - Name", null },
							{ "Object (Action Recipient) - URL", null },
							{ "Object (Action Recipient) - Description", null },
							{ "Object (Action Recipient) - Provider - Name", null },
							{ "Object (Action Recipient) - Provider - URL", null },
							{ "Financial Assistance", NormalizeInternalValuesToString( item["FinancialAssistance"], financialAssistanceProfile => ( financialAssistanceProfile["Name"] ?? financialAssistanceProfile["Description"] ?? "" ).ToString() ) },
							{ "Requirements Description(s)", NormalizeInternalValuesToString( item["Requires"], conditionProfile => TokenOrNull( conditionProfile["Description"] )?.ToString() ) },
							{ "Requirements Condition(s)", NormalizeInternalValuesToString( item["Requires"], conditionProfile => NormalizeArrayToString( " | ", conditionProfile["Condition"] ) ) },
							{ "Finder Detail Page", appDomainURL + TokenOrNull( item["BroadType"] )?.ToString() + "/" + item["Meta_Id"]?.ToString() },
							{ "Registry URI", TokenOrNull( item["CTID"] ) == null ? "" : registryDomainURL + "resources/" + item["CTID"] },
							{ "Required Credit", null },
							{ "Estimated Cost(s)", null },
							{ "Estimated Cost Details", null },
							{ "Outcomes Data Start Date", null },
							{ "Outcomes Data End Date", null }
						};

						//Track the row here temporarily
						intermediateRows.Add( rowData );

						//Now set the values for complex data that requires multiple headers and/or addendum rows

						//Required Credit (this could technically be done in the initialization of the rowData since it is only one column, but it is cleaner to do it here)
						Shunt( rowData, intermediateRows, NormalizeArray( item[ "Requires" ] )?.SelectMany( conditionProfile => conditionProfile[ "CreditValue" ] ).Select( valueProfile =>
						{
							return new JObject()
							{
								{ "Required Credit", NormalizeArrayToString( " | ", new JArray(){
									(valueProfile["Value"] ?? "Unknown") + " Credit(s)",
									NormalizeLabelsToString( valueProfile["CreditUnitType"], "; " ),
									NormalizeLabelsToString( valueProfile["CreditLevelType"], "; " ),
									NormalizeLabelsToString( valueProfile["Subject"], "; " )
								} ) }
							};
						} ).ToList() );

						//Addresses
						Shunt( rowData, intermediateRows, NormalizeArray( item[ "AvailableAt" ] ?? item[ "Address" ] ).Select( place =>
						{
							return new JObject()
							{
								{ "Address", NormalizeArrayToString( "; ", new JArray(){
									place["StreetAddress"], place["AddressLocality"], place["AddressRegion"], place["PostalCode"]
								} ) },
								{ "Address Identifier", NormalizeArrayToString( " | ", JArray.FromObject(
									TokenOrNull( place["Identifier"] )?.Select( identifier => (identifier["IdentifierTypeName"] ?? "Unknown Identifier") + ": " + (identifier["IdentifierValueCode"] ?? "Unknown Code") ).ToList()
								) ) }
							};
						} ).ToList() );

						//Costs
						Shunt( rowData, intermediateRows, NormalizeArray( item[ "EstimatedCost" ] ).Select( ( costProfile, index ) =>
						{
							var results = new List<JObject>();
							//Use the properties of the Cost Profile itself to construct the value for the first column
							var costText = NormalizeArrayToString( " | ", new JArray() {
								"Cost #" + ( index + 1 ),
								costProfile[ "Name" ],
								costProfile[ "Description" ],
								costProfile[ "CostDetails" ],
								string.IsNullOrWhiteSpace( TokenOrNull( costProfile[ "StartDate" ] )?.ToString() ) ? "" : "From " + costProfile[ "startDate" ],
								string.IsNullOrWhiteSpace( TokenOrNull( costProfile[ "EndDate" ] )?.ToString() ) ? "" : "Until " + costProfile[ "EndDate" ],
								costProfile[ "Currency" ]
							} );

							//For each Cost Item in the Cost Profile...
							foreach ( var costItem in NormalizeArray( costProfile[ "CostItems" ] ) )
							{
								//Construct the value for the second column, referencing the Cost # from the first column to make it easier to figure out which Item goes with which Cost
								var detailsText = NormalizeArrayToString( " | ", new JArray() {
									"Cost #" + ( index + 1 ),
									NormalizeLabelsToString( costItem[ "DirectCostType" ], "; " ),
									NormalizeLabelsToString( costItem[ "AudienceType" ], "; " ),
									TokenOrNull( costItem[ "Price" ] )?.ToString() == "0" ? "" : costItem[ "Price" ]
								} );

								//Add the value to the second column
								results.Add( new JObject()
								{
									{ "Estimated Cost(s)", costText },
									{ "Estimated Cost Details", detailsText }
								} );
							}

							//If by chance there were no Cost Items (ie the Cost Profile just had a description), then make sure the Cost Profile data still makes it into the export
							if ( results.Count() == 0 )
							{
								results.Add( new JObject()
								{
									{ "Estimated Cost(s)", costText },
									{ "Estimated Cost Details", "No Details Available" }
								} );
							}

							return results;
						} ).SelectMany( m => m ).ToList() );

						//Outcomes Data
						//Merge and deduplicate from both sources
						var outcomesData = new JArray();
						foreach ( var dsp in NormalizeArray( item[ "ExternalDataSetProfiles" ] ) )
						{
							outcomesData.Add( dsp );
						}
						foreach ( var adp in NormalizeArray( item[ "AggregateData" ] ) )
						{
							foreach ( var dsp in NormalizeArray( item[ "RelevantDataSet" ] ) )
							{
								outcomesData.Add( dsp );
							}
						}

						//Now finish handling Outcomes data
						Shunt( rowData, intermediateRows, NormalizeArray( outcomesData ).Select( dataSetProfile =>
						{
							var results = new List<JObject>();

							//For each Data Set Time Frame...
							foreach ( var dataSetTimeFrame in NormalizeArray( dataSetProfile[ "DataSetTimePeriod" ] ) )
							{
								//Construct an object that will hold the data for the Data Set Time Frame
								var resultItem = new JObject()
								{
									{ "Outcomes Data Start Date", dataSetTimeFrame[ "StartDate" ] },
									{ "Outcomes Data End Date", dataSetTimeFrame[ "EndDate" ] }
								};

								//For each Data Profile in the Data Set Time Frame...
								foreach ( var dataProfile in NormalizeArray( dataSetTimeFrame[ "DataAttributes" ] ).Where( m => m.Type == JTokenType.Object ).ToList() )
								{
									//Get the properties whose value is an array and flatten them into one big array
									var qValueList = ( ( JObject ) dataProfile ).Values().Where( value => value != null && value.Type == JTokenType.Array ).SelectMany( m => m ).ToList();

									//For each item in the merged array that is a Quantitative Value...
									foreach ( var qValue in qValueList.Where( value => value[ "Description" ] != null && ( value[ "Value" ] != null || value[ "MinValue" ] != null || value[ "MaxValue" ] != null ) ).ToList() )
									{
										//Get the description. This will become a dynamically-added Column Header in the final export.
										var label = TokenOrNull( qValue[ "Description" ] )?.ToString() ?? "Unknown Outcome";

										//Using that description as the key (which will become the Column Header), add a value constructed from the guts of the Quantitative Value
										resultItem.Add( label, NormalizeArrayToString( " ", new JArray()
										{
											GetLabeledValue( "", qValue["Value"] ),
											GetLabeledValue( "Minimum", qValue["Minimum"] ),
											GetLabeledValue( "Maximum", qValue["Maximum"] ),
											GetLabeledValue( "Percentage", qValue["Percentage"] ),
											GetLabeledValue( "Minimum Percent", qValue["MinPercent"] ),
											GetLabeledValue( "Maximum Percent", qValue["MaxPercent"] )
										} ) );

										//Add the description to the dynamic column list if it isn't already in there
										if ( !dynamicColumns.Contains( label ) )
										{
											dynamicColumns.Add( label );
										}
									}
								}

								results.Add( resultItem );
							}

							return results;
						} ).SelectMany( m => m ).ToList() );

						//Transfer Value Credit (for objects with a TransferValue property)
						Shunt( rowData, intermediateRows, NormalizeArray( item[ "TransferValue" ] ).Select( transferValueProfile =>
						{
							return new JObject()
							{
								{ "Transfer Value Credit", NormalizeArrayToString( " ", new JArray(){
									GetLabeledValue( "", transferValueProfile[ "Value" ] ),
									GetLabeledValue( "Minimum", transferValueProfile["Minimum"] ),
									GetLabeledValue( "Maximum", transferValueProfile["Maximum"] ),
									GetLabeledValue( "Percentage", transferValueProfile["Percentage"] ),
									GetLabeledValue( "Minimum Percent", transferValueProfile["MinPercent"] ),
									GetLabeledValue( "Maximum Percent", transferValueProfile["MaxPercent"] )
								} ) },
								{ "Transfer Value Credit Level(s)", NormalizeLabelsToString( transferValueProfile["CreditLevelType"] ) },
								{ "Transfer Value Credit Unit(s)", NormalizeLabelsToString( transferValueProfile["CreditUnitType"] ) },
								{ "Transfer Value Credit Subject(s)", NormalizeLabelsToString( transferValueProfile["Subject"] ) }
							};
						} ).ToList() );

						//Transfer Value From
						Shunt( rowData, intermediateRows, NormalizeArray( TokenOrNull( item[ "TransferValueFrom" ] )?[ "Values" ] ).Select( outline =>
						{
							return new JObject()
							{
								{ "Transfer Value From - Finder URL", outline["URL"] ?? "" },
								{ "Transfer Value From Resource - Type", outline["OutlineType"] ?? "Unknown Type" },
								{ "Transfer Value From Resource - Name", outline["Label"] ?? "Unknown Resource" },
								{ "Transfer Value From Resource - Description", outline["Description"] ?? "No Description" },
								{ "Transfer Value From Resource - Provider - Name", TokenOrNull( outline["Provider"] )?["Label"] ?? "Unknown Provider" },
								{ "Transfer Value From Resource - Provider - Finder URL", outline["URL"] ?? "" },
							};
						} ).ToList() );

						//Transfer Value For
						Shunt( rowData, intermediateRows, NormalizeArray( TokenOrNull( item[ "TransferValueFor" ] )?[ "Values" ] ).Select( outline =>
						{
							return new JObject()
							{
								{ "Transfer Value For - Finder URL", outline["URL"] ?? "" },
								{ "Transfer Value For Resource - Type", outline["OutlineType"] ?? "Unknown Type" },
								{ "Transfer Value For Resource - Name", outline["Label"] ?? "Unknown Resource" },
								{ "Transfer Value For Resource - Description", outline["Description"] ?? "No Description" },
								{ "Transfer Value For Resource - Provider - Name", TokenOrNull( outline["Provider"] )?["Label"] ?? "Unknown Provider" },
								{ "Transfer Value For Resource - Provider - Finder URL", outline["URL"] ?? "" },
							};
						} ).ToList() );

						//Receives Transfer Value From
						Shunt( rowData, intermediateRows, NormalizeArray( TokenOrNull( item[ "ReceivesTransferValueFrom" ] )?[ "Values" ] ).Select( outline =>
						{
							return new JObject()
							{
								{ "Receives Transfer Value From - Finder URL", outline["URL"] ?? "" },
								{ "Receives Transfer Value From Resource - Type", outline["OutlineType"] ?? "Unknown Type" },
								{ "Receives Transfer Value From Resource - Name", outline["Label"] ?? "Unknown Resource" },
								{ "Receives Transfer Value From Resource - Description", outline["Description"] ?? "No Description" },
								{ "Receives Transfer Value From Resource - Provider - Name", TokenOrNull( outline["Provider"] )?["Label"] ?? "Unknown Provider" },
								{ "Receives Transfer Value From Resource - Provider - Finder URL", outline["URL"] ?? "" },
							};
						} ).ToList() );

						//Provides Transfer Value For
						Shunt( rowData, intermediateRows, NormalizeArray( TokenOrNull( item[ "ProvidesTransferValueFor" ] )?[ "Values" ] ).Select( outline =>
						{
							return new JObject()
							{
								{ "Provides Transfer Value For - Finder URL", outline["URL"] ?? "" },
								{ "Provides Transfer Value For Resource - Type", outline["OutlineType"] ?? "Unknown Type" },
								{ "Provides Transfer Value For Resource - Name", outline["Label"] ?? "Unknown Resource" },
								{ "Provides Transfer Value For Resource - Description", outline["Description"] ?? "No Description" },
								{ "Provides Transfer Value For Resource - Provider - Name", TokenOrNull( outline["Provider"] )?["Label"] ?? "Unknown Provider" },
								{ "Provides Transfer Value For Resource - Provider - Finder URL", outline["URL"] ?? "" },
							};
						} ).ToList() );

						//Credentialing Actions - Acting Agent
						Shunt( rowData, intermediateRows, NormalizeArray( TokenOrNull( item[ "ActingAgent" ] )?[ "Values" ] ).Select( outline =>
						{
							return new JObject()
							{
								{ "Acting Agent - Name", outline[ "Label" ] ?? "" },
								{ "Acting Agent - URL", outline[ "URL" ] ?? "" }
							};
						} ).ToList() );

						//Credentialing Actions - Instrument
						Shunt( rowData, intermediateRows, NormalizeArray( TokenOrNull( item[ "Instrument" ] )?[ "Values" ] ).Select( outline =>
						{
							return new JObject()
							{
								{ "Instrument - Name", outline[ "Label" ] ?? "" },
								{ "Instrument - URL", outline[ "URL" ] ?? "" }
							};
						} ).ToList() );

						//Credentialing Actions - Object
						Shunt( rowData, intermediateRows, NormalizeArray( TokenOrNull( item[ "Object" ] )?[ "Values" ] ).Select( outline =>
						{
							return new JObject()
							{
								{ "Object (Action Recipient) - Name", outline[ "Label" ] ?? "" },
								{ "Object (Action Recipient) - URL", outline[ "URL" ] ?? "" },
								{ "Object (Action Recipient) - Description", outline[ "Description" ] ?? "" },
								{ "Object (Action Recipient) - Provider - Name", TokenOrNull( outline[ "Provider" ] )?[ "Label" ] ?? "" },
								{ "Object (Action Recipient) - Provider - URL", TokenOrNull( outline[ "Provider" ] )?[ "URL" ] ?? "" }
							};
						} ).ToList() );

						//Update the list of active columns with the columns from this row that have any data
						foreach ( var property in rowData.Properties() )
						{
							if ( !activeColumns.Contains( property.Name ) && !string.IsNullOrWhiteSpace( property.Value?.ToString() ) )
							{
								activeColumns.Add( property.Name );
							}
						}
					}
					catch ( Exception ex )
					{
						result.Messages.Add( "Error formatting resource " + ( TokenOrNull( item?[ "CTID" ] ) ?? "Unknown CTID" ) + ": " + ex.Message + "; " + ex.InnerException?.Message );
						progressTracker.ThreadSafeUpdate( () => progressTracker.Errors.Add( "Error formatting resource " + ( TokenOrNull( item?[ "CTID" ] ) ?? "Unknown CTID" ) + ": " + ex.Message + "; " + ex.InnerException?.Message ) );
					}

				}

				result.Messages.Add( "Results formatted at " + timer.ElapsedMilliseconds + "ms" );
				progressTracker.ThreadSafeUpdate( () => progressTracker.Messages.Add( "Results formatted. Generating CSV..." ) );

				//Return if no data
				if ( intermediateRows.Count() == 0 )
				{
					return InvalidateAttempt( result, "No rows with data were present in the downloaded data. There is no data to export." );
				}

				if ( progressTracker.CancelProcessing )
				{
					return InvalidateAttempt( result, "User canceled processing." );
				}

				//Use the first row's object to determine column order, and the active columns list to determine which columns to care about, and append the dynamically-added columns
				var headers = intermediateRows[ 0 ].Properties().Select( m => m.Name ).Where( m => activeColumns.Contains( m ) ).Concat( dynamicColumns ).ToList();

				//Convert results to CSV structured data
				var rawCSVData = new List<string>();
				rawCSVData.Add( CSVifyRowData( headers ) );
				foreach ( var rowData in intermediateRows )
				{
					rawCSVData.Add( CSVifyRowData( headers.Select( header => rowData[ header ]?.ToString() ?? "" ).ToList() ) );
				}

				//Flatten into actual CSV
				result.Data = string.Join( "\n", rawCSVData );
				result.Valid = true;

				result.Messages.Add( "CSV generated at " + timer.ElapsedMilliseconds + "ms" );
				progressTracker.ThreadSafeUpdate( () => progressTracker.Messages.Add( "CSV Generated." ) );

				//Cache the data if the cacheKey is valid
				if ( !string.IsNullOrWhiteSpace( cacheKey ) && allowCache )
				{
					result.Messages.Add( "Adding data to cache" );
					MemoryCache.Default.Remove( cacheKey );
					MemoryCache.Default.Add( cacheKey, result.Data, new DateTimeOffset( DateTime.Now.AddMinutes( 30 ) ) );
				}
			}
			catch ( Exception ex )
			{
				timer.Stop();
				progressTracker.ThreadSafeUpdate( () => progressTracker.Errors.Add( "Error exporting CSV Data: " + ex.Message + "; " + ex.InnerException?.Message ) );
				return InvalidateAttempt( result, "Error exporting CSV Data", ex );
			}

			timer.Stop();
			return result;
		}
		//

		private static string CSVifyRowData( List<string> rowData )
		{
			var wrapIfContains = new List<string>() { ",", "\r", "\n", "\"" };
			return string.Join( ",", rowData.Select( cellData => wrapIfContains.Any( m => cellData.Contains( m ) ) ? "\"" + cellData.Replace( "\"", "\"\"" ) + "\"" : cellData.Replace( "\"", "\"\"" ) ) );
		}
		//

		public static JToken TokenOrNull( JToken value )
		{
			return value == null || value.Type == JTokenType.Null ? null : value;
		}
		//

		public static string NormalizeIdentifierValuesToString( JToken identifierValues, string joiner = " | " )
		{
			return NormalizeInternalValuesToString( identifierValues, identifierValue => ( identifierValue[ "IdentifierTypeName" ] ?? "Unknown Identifier Type" ).ToString() + ": " + ( identifierValue[ "IdentifierValueCode" ] ?? "Unknown Code" ).ToString(), joiner );
		}
		//

		public static string NormalizeLabelsToString( JToken objectsWithLabelProperty, string joiner = " | " ) //Outline, etc
		{
			return NormalizeInternalValuesToString( objectsWithLabelProperty, item => TokenOrNull( item[ "Label" ] )?.ToString(), joiner );
		}
		//

		public static string NormalizeInternalValuesToString( JToken objects, Func<JToken, string> GetInternalValueMethod, string joiner = " | " )
		{
			return string.Join( joiner, NormalizeArray( objects ).Select( GetInternalValueMethod ).Where( m => !string.IsNullOrWhiteSpace( m ) ).ToList() );
		}
		//

		public static string GetLabeledValue( string label, JToken value )
		{
			return ( value == null || value.Type == JTokenType.Null || value.ToString() == "0" || value.ToString() == "0.0" || value.ToString().Length == 0 ) ? "" : ( !string.IsNullOrWhiteSpace( label ) ? label + ": " : "" ) + value.ToString();
		}
		//

		//Put the data in the current row for the first subItem, and into intermediate rows for subsequent subItems
		//The subItems are small JObjects containing the header name and the data for that header for however many rows are applicable
		public static void Shunt( JObject mainRowData, List<JObject> intermediateRows, List<JObject> subItems )
		{
			//Normalize the List<JObject> to remove null values
			subItems = ( subItems ?? new List<JObject>() ).Where( m => m != null ).ToList();

			//For each subItem...
			for ( var index = 0; index < subItems.Count(); index++ )
			{
				//If it is the first one, inject the data into the main Resource row
				if ( index == 0 )
				{
					foreach ( var property in subItems[ index ].Properties() )
					{
						mainRowData[ property.Name ] = property.Value;
					}
				}
				//Otherwise...
				else
				{
					//Create a new intermediate row with the appropriate Row Type and the main row's CTID
					var copy = new JObject()
					{
						{ "Row Type", "Addendum" },
						{ "CTID", mainRowData["CTID"] }
					};

					//Add the subItem's data to the intermediate row
					foreach ( var property in subItems[ index ].Properties() )
					{
						copy[ property.Name ] = property.Value;
					}

					//And put the intermediate row in the list
					intermediateRows.Add( copy );
				}
			}
		}
		//

		public static JArray NormalizeArray( JToken value )
		{
			if ( value == null || value.Type == JTokenType.Null )
			{
				return new JArray();
			}

			if ( value.Type == JTokenType.Array )
			{
				return JArray.FromObject( value
					.Where( m => m != null )
					.Where( m => m.Type == JTokenType.String ? !string.IsNullOrWhiteSpace( m.ToString() ) : true )
					.Where( m => m.Type == JTokenType.Integer ? ( ( int ) m ) > 0 : true )
					.Where( m => m.Type == JTokenType.Float ? ( ( float ) m ) > 0 : true )
					.ToList()
				);
			}

			if ( value.Type == JTokenType.Object )
			{
				return new JArray() { ( JObject ) value };
			}

			return new JArray() { value };
		}
		//

		public static string NormalizeArrayToString( string joiner, JToken value )
		{
			return string.Join( joiner, NormalizeArray( value ).Select( m => m.ToString() ).ToList() );
		}
		//

		public static Attempt<T> InvalidateAttempt<T>( Attempt<T> attempt, string message, Exception ex = null, Action<JObject> AppendDebugInfo = null )
		{
			return InvalidateAttempt( attempt, new List<string>() { message }, ex, AppendDebugInfo );
		}
		//

		public static Attempt<T> InvalidateAttempt<T>( Attempt<T> attempt, List<string> messages, Exception ex = null, Action<JObject> AppendDebugInfo = null )
		{
			attempt.Valid = false;
			attempt.Messages.AddRange(
				( messages ?? new List<string>() ).Concat( new List<string>()
				{
					ex?.Message,
					ex?.InnerException?.Message,
					ex?.InnerException?.InnerException?.Message
				} ).Where( m => !string.IsNullOrWhiteSpace( m ) ).ToList()
			);

			if ( AppendDebugInfo != null )
			{
				attempt.Debug = attempt.Debug ?? new JObject();
				AppendDebugInfo( attempt.Debug );
			}

			return attempt;
		}
		//

		private class QueryAndResultSet
		{
			public MainSearchInput Query { get; set; }
			public MainSearchResults Result { get; set; }
			public string Status { get; set; }
			public bool Valid { get; set; }
		}
		//

		public class Attempt<T>
		{
			public Attempt()
			{
				Messages = new List<string>();
			}

			public T Data { get; set; }
			public bool Valid { get; set; }
			public List<string> Messages { get; set; }
			public JObject Debug { get; set; }
		}
		//
	}
}