using System.Collections.Generic;
using System.Linq;
using System.Web;
using CF = workIT.Factories;
using workIT.Models;
using workIT.Models.Search;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;

namespace workIT.Services
{
    public class SearchServices
    {
        //private static readonly object LoggingHelper;

        public MainSearchResults MainSearch( MainSearchInput data, ref bool valid, ref string status )
        {
            //Sanitize input
            data.Keywords = string.IsNullOrWhiteSpace( data.Keywords ) ? "" : data.Keywords;
            data.Keywords = ServiceHelper.CleanText( data.Keywords );
            data.Keywords = ServiceHelper.HandleApostrophes( data.Keywords );
            data.Keywords = data.Keywords.Trim();

            //Sanitize input
            var validSortOrders = new List<string>() { "newest", "oldest", "relevance", "alpha", "cost_lowest", "cost_highest", "duration_shortest", "duration_longest", "org_alpha" };
            if ( !validSortOrders.Contains( data.SortOrder ) )
            {
                data.SortOrder = validSortOrders.First();
            }

            //Determine search type
            var searchType = data.SearchType;
            if ( string.IsNullOrWhiteSpace( searchType ) )
            {
                valid = false;
                status = "Unable to determine search mode";
                return null;
            }

            //Do the search
            var totalResults = 0;
            switch ( searchType )
            {
                case "credential":
                    {
                        var qaSettings = data.GetFilterValues_Strings( "qualityAssurance" );

                        var results = CredentialServices.Search( data, ref totalResults );
                        return ConvertCredentialResults( results, totalResults, searchType );
                    }
                case "organization":
                    {
                        var qaSettings = data.GetFilterValues_Strings( "qualityAssurance" );

                        var results = OrganizationServices.Search( data, ref totalResults );
                        return ConvertOrganizationResults( results, totalResults, searchType );
                    }
                case "assessment":
                    {
                        var results = AssessmentServices.Search( data, ref totalResults );
                        return ConvertAssessmentResults( results, totalResults, searchType );
                    }
                case "learningopportunity":
                    {
                        var results = LearningOpportunityServices.Search( data, ref totalResults );
                        return ConvertLearningOpportunityResults( results, totalResults, searchType );
                    }
                default:
                    {
                        valid = false;
                        status = "Unknown search mode: " + searchType;
                        return null;
                    }
            }
        }

        //Do an autocomplete
        public static List<string> DoAutoComplete( string text, string context, string searchType )
        {
            var results = new List<string>();

            switch ( searchType.ToLower() )
            {
                case "credential":
                    {
                        switch ( context.ToLower() )
                        {
                            //case "mainsearch": return CredentialServices.Autocomplete( text, 10 ).Select( m => m.Name ).ToList();
                            case "mainsearch":
                                return CredentialServices.Autocomplete( text, 15 );
                            //case "competencies":
                            //	return CredentialServices.AutocompleteCompetencies( text, 10 );
                            case "subjects":
                                return Autocomplete_Subjects( CF.CodesManager.ENTITY_TYPE_CREDENTIAL, CF.CodesManager.PROPERTY_CATEGORY_SUBJECT, text, 10 );
                            case "occupations":
                                return Autocomplete_Occupations( CF.CodesManager.ENTITY_TYPE_CREDENTIAL, text, 10 );
                            case "industries":
                                return Autocomplete_Industries( CF.CodesManager.ENTITY_TYPE_CREDENTIAL, text, 10 );
                            default:
                                break;
                        }
                        break;
                    }
                case "organization":
                    {
                        switch ( context.ToLower() )
                        {
                            case "mainsearch":
                                return OrganizationServices.Autocomplete( text, 10 );
                            case "industries":
                                return Autocomplete_Industries( CF.CodesManager.ENTITY_TYPE_ORGANIZATION, text, 10 );
                            default:
                                break;
                        }
                        break;
                    }
                case "assessment":
                    {
                        switch ( context.ToLower() )
                        {
                            case "mainsearch":
                                return AssessmentServices.Autocomplete( text, 15 );
                            //case "competencies":
                            //	return AssessmentServices.Autocomplete( text, "competencies", 10 );
                            case "subjects":
                                return Autocomplete_Subjects( CF.CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, CF.CodesManager.PROPERTY_CATEGORY_SUBJECT, text, 10 );
                            case "instructionalprogramtype":
                                return Autocomplete_Cip( CF.CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, text, 10 );
                            default:
                                break;
                        }
                        break;
                    }
                case "learningopportunity":
                    {
                        switch ( context.ToLower() )
                        {
                            case "mainsearch":
                                return LearningOpportunityServices.Autocomplete( text, 15 );
                            //case "competencies":
                            //	return LearningOpportunityServices.Autocomplete( text, "competencies", 10 );
                            case "subjects":
                                return Autocomplete_Subjects( CF.CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, CF.CodesManager.PROPERTY_CATEGORY_SUBJECT, text, 10 );
                            case "instructionalprogramtype":
                                return Autocomplete_Cip( CF.CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, text, 10 );
                            default:
                                break;
                        }
                        break;
                    }
                default:
                    break;
            }

            return results;
        }
        //
        public static List<CredentialAlignmentObjectItem> EntityCompetenciesList( string searchType, int entityId, int maxRecords = 10 )
        {
            var results = new List<CredentialAlignmentObjectItem>();
            string filter = "";
            int pTotalRows = 0;
            switch ( searchType.ToLower() )
            {
                case "credential":
                    {
                        //not sure if will be necessary to include alignment type (ie teaches, and assesses, but not required)
                        filter = string.Format( "(CredentialId = {0})", entityId );
                        return CF.EducationFrameworkManager.Search( filter, "", 1, maxRecords, ref pTotalRows );
                    }
                case "assessment":
                    {
                        filter = string.Format( "(SourceEntityTypeId = 3 AND [SourceId] = {0})", entityId );
                        return CF.EducationFrameworkManager.Search( filter, "", 1, maxRecords, ref pTotalRows );
                    }
                case "learningopportunity":
                    {
                        filter = string.Format( "(SourceEntityTypeId = 7 AND [SourceId] = {0})", entityId );
                        return CF.EducationFrameworkManager.Search( filter, "", 1, maxRecords, ref pTotalRows );
                    }
                default:
                    break;
            }

            return results;
        }

        public static List<CostProfileItem> EntityCostsList( string searchType, int entityId, int maxRecords = 10 )
        {
            var results = new List<CostProfileItem>();
            string filter = "";
            int pTotalRows = 0;

            switch ( searchType.ToLower() )
            {
                case "credential":
                    {
                        filter = "";
                        return CF.CostProfileItemManager.Search( 1, entityId, filter, "", 1, maxRecords, ref pTotalRows );
                    }
                case "assessment":
                    {
                        filter = "";
                        return CF.CostProfileItemManager.Search( 3, entityId, filter, "", 1, maxRecords, ref pTotalRows );
                    }
                case "learningopportunity":
                    {
                        filter = "";
                        return CF.CostProfileItemManager.Search( 7, entityId, filter, "", 1, maxRecords, ref pTotalRows );
                    }
                default:
                    break;
            }

            return results;
        }
        public static List<CredentialAlignmentObjectItem> EntityQARolesList( string searchType, int entityId, int maxRecords = 10 )
        {
            var results = new List<CredentialAlignmentObjectItem>();
            string filter = "";
            int pTotalRows = 0;
            switch ( searchType.ToLower() )
            {
                case "credential":
                    {
                        //not sure if will be necessary to include alignment type (ie teaches, and assesses, but not required)
                        filter = string.Format( "(CredentialId = {0})", entityId );
                        return CF.EducationFrameworkManager.Search( filter, "", 1, maxRecords, ref pTotalRows );
                    }
                case "assessment":
                    {
                        filter = string.Format( "(SourceEntityTypeId = 3 AND [SourceId] = {0})", entityId );
                        return CF.EducationFrameworkManager.Search( filter, "", 1, maxRecords, ref pTotalRows );
                    }
                case "learningopportunity":
                    {
                        filter = string.Format( "(SourceEntityTypeId = 7 AND [SourceId] = {0})", entityId );
                        return CF.EducationFrameworkManager.Search( filter, "", 1, maxRecords, ref pTotalRows );
                    }
                default:
                    break;
            }

            return results;
        }

        //Convenience method to handle location data
        //For convenience, check boundaries.IsDefined to see if a boundary is defined
        public static BoundingBox GetBoundaries( MainSearchInput data, string name )
        {
            var boundaries = new BoundingBox();
            try
            {
                boundaries = data.FiltersV2.FirstOrDefault( m => m.Name == name ).AsBoundaries();
            }
            catch { }

            return boundaries;
        }
        //

        public enum TagTypes { CONNECTIONS, QUALITY, LEVEL, OCCUPATIONS, INDUSTRIES, SUBJECTS, COMPETENCIES, TIME, COST, ORGANIZATIONTYPE, ORGANIZATIONSECTORTYPE, ORG_SERVICE_TYPE, OWNED_BY, OFFERED_BY, ASMTS_OWNED_BY, LOPPS_OWNED_BY, DELIVER_METHODS, SCORING_METHODS, ASSESSMENT_USE_TYPES, ASSESSMENT_METHOD_TYPES, LEARNING_METHODS, INSTRUCTIONAL_PROGRAM }

        public MainSearchResults ConvertCredentialResults( List<CredentialSummary> results, int totalResults, string searchType )
        {
            var output = new MainSearchResults() { TotalResults = totalResults, SearchType = searchType };
            foreach ( var item in results )
            {
                var mergedCosts = item.NumberOfCostProfileItems; // CostProfileMerged.FlattenCosts( item.EstimatedCost );
                var subjects = Deduplicate( item.Subjects );
                var mergedQA = item.AgentAndRoles.Results.Concat( item.Org_QAAgentAndRoles.Results ).ToList();
                output.Results.Add( Result( item.Name, item.FriendlyName, item.Description, item.Id,
                    new Dictionary<string, object>()
                    {
                        { "Name", item.Name },
                        { "Description", item.Description },
                        { "Type", item.CredentialType },
                        { "Owner", item.OwnerOrganizationName },
                        { "OwnerId", item.OwnerOrganizationId },
						//{ "CanEditRecord", item.CanEditRecord },
						{ "TypeSchema", item.CredentialTypeSchema.ToLower()},
                        { "AvailableAt", new { Type = "locations", Data = ConvertAddresses( item.Addresses ) } },
                        { "Locations", ConvertAddresses( item.Addresses ) },
                        { "SearchType", searchType },
                        { "RecordId", item.Id },
                        { "ctid", item.CTID },
                        { "UrlTitle", item.FriendlyName },
                        { "HasBadge", item.HasVerificationType_Badge }, //Indicate existence of badge here
                        { "LastUpdated", item.LastUpdated.ToShortDateString() }

					},
                    new List<TagSet>(),
                    new List<Models.Helpers.SearchTag>()
                    {
						//Connections
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "Connections",
                            DisplayTemplate = "{#} Connection{s}",
                            Name = TagTypes.CONNECTIONS.ToString().ToLower(),
                            TotalItems = item.CredentialsList.Results.Count(),
                            SearchQueryType = "link",
							//Items = GetSearchTagItems_Filter( item.ConnectionsList.Results, "{Name} Credential(s)", item.ConnectionsList.CategoryId )
							//Something like this...
							/*	*/
							Items = item.CredentialsList.Results.Take(10).ToList().ConvertAll(m => new Models.Helpers.SearchTagItem()
                            {
                                Display = "<b>" + m.Connection + "</b>" + " " + m.Credential, //[Is Preparation For] [Some Credential Name] 
								QueryValues = new Dictionary<string, object>()
                                {
                                    { "TargetId", m.CredentialId }, //AgentId?
									{ "TargetType", "credential" }, //Probably okay to hard code this for now
									{ "ConnectionTypeId", m.ConnectionId }, //Connection type
								}
                            } ).ToList()
                        },
						//Credential Quality Assurance
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "Quality Assurance",
                            DisplayTemplate = "{#} Quality Assurance",
                            Name = "qualityassurance", //Using the "quality" enum breaks this filter since it tries to find the matching item in the checkbox list and it doesn't exist
							TotalItems = mergedQA.Count(),
                            SearchQueryType = "merged", //Change this to "custom", or back to detail
							//Something like this...
							//QAOrgRolesResults is a list of 1 role and 1 org (org repeating for each relevant role)
							//e.g. [Accredited By] [Organization 1], [Approved By] [Organization 1], [Accredited By] [Organization 2], etc.
							Items = mergedQA.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() {
                            Display = "<b>" + m.Relationship + "</b>" + " by " + m.Agent, //[Accredited By] [Organization 1]
							QueryValues = new Dictionary<string, object>() {
                                    { "Relationship", m.Relationship },
                                    { "TextValue", m.Agent },
                                    { "RelationshipId", m.RelationshipId },
                                     //  { "CodeId", m.AgentId },
                                    { "AgentId", m.AgentId }, //AgentId?
									{ "TargetType", m.EntityType }, //Probably okay to hard code this for now
                                    { "AgentUrl", m.AgentUrl},
                                    { "EntityStateId", m.EntityStateId }
									//{ "ConnectionTypeId", m.ConnectionId }, //Connection type
                                }
                            } ).ToList()
							
							//Items = GetSearchTagItems_Filter( item.QARolesResults.Results, "{Name} by Quality Assurance Organization(s)", item.QARolesResults.CategoryId )
						},
                       
						//Audience Level Type
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "Levels",
                            DisplayTemplate = "{#} Level{s}",
                            Name = TagTypes.LEVEL.ToString().ToLower(),
                            TotalItems = item.LevelsResults.Results.Count(),
                            SearchQueryType = "code",
                            Items = GetSearchTagItems_Filter( item.LevelsResults.Results, "{Name}", item.LevelsResults.CategoryId )
                        },
						//Occupations
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "Occupations",
                            DisplayTemplate = "{#} Occupation{s}",
                            Name = TagTypes.OCCUPATIONS.ToString().ToLower(),
                            TotalItems = item.OccupationResults.Results.Count(),
                            //SearchQueryType = "framework",
                            SearchQueryType = "text",
                            Items = item.OccupationResults.Results.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() { Display = m.CodeTitle, QueryValues = new Dictionary<string, object>() { { "TextValue", m.CodeTitle } } } )
                            //Items = GetSearchTagItems_Filter( item.OccupationResults.Results, "{Name}", item.OccupationResults.CategoryId )
                        },
						//Industries
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "Industries",
                            DisplayTemplate = "{#} Industr{ies}",
                            Name = TagTypes.INDUSTRIES.ToString().ToLower(),
                            TotalItems = item.NaicsResults.Results.Count(),
                            //SearchQueryType = "framework",
                            SearchQueryType = "text",
                            //Items = GetSearchTagItems_Filter( item.NaicsResults.Results, "{Name}", item.NaicsResults.CategoryId )
                            Items = item.NaicsResults.Results.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() { Display = m.CodeTitle, QueryValues = new Dictionary<string, object>() { { "TextValue", m.CodeTitle } } } )
                        },
						//Subjects
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "Subjects",
                            DisplayTemplate = "{#} Subject{s}",
                            Name = TagTypes.SUBJECTS.ToString().ToLower(),
                            TotalItems = subjects.Count(), //Returns a count of the de-duplicated items
							SearchQueryType = "text",
                            Items = subjects.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() { Display = m, QueryValues = new Dictionary<string, object>() { { "TextValue", m } } } )
                        },
						//Competencies
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "Competencies",
                            DisplayTemplate = "{#} Competenc{ies}",
                            Name = TagTypes.COMPETENCIES.ToString().ToLower(),
                            TotalItems = item.AssessmentsCompetenciesCount + item.LearningOppsCompetenciesCount,
                            SearchQueryType = "text",
                            IsAjaxQuery = true,
                            AjaxQueryName = "GetSearchResultCompetencies",
                            AjaxQueryValues = new Dictionary<string, object>()
                            {
                                { "SearchType", "credential" },
                                { "RecordId", item.Id },
                                { "TargetEntityType", "competencies" }
                            }
                        },
						//Durations
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "Time Estimates",
                            DisplayTemplate = " Time Estimate{s}",
                            Name = TagTypes.TIME.ToString().ToLower(),
                            TotalItems = item.EstimatedTimeToEarn.Count(), //# of duration profiles
							SearchQueryType = "detail", //Not sure how this could be any kind of search query
							Items = item.EstimatedTimeToEarn.Take(10).ToList().ConvertAll(m => new Models.Helpers.SearchTagItem()
                            {
                                Display = m.IsRange ? m.MinimumDuration.Print() + " - " + m.MaximumDuration.Print() : m.ExactDuration.Print(),
                                QueryValues = new Dictionary<string, object>()
                                {
                                    { "ExactDuration", m.ExactDuration },
                                    { "MinimumDuration", m.MinimumDuration },
                                    { "MaximumDuration", m.MaximumDuration }
                                }
                            } ).ToList()
                        },
						//Costs
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "Costs",
                            DisplayTemplate = " Cost{s}",
                            Name = TagTypes.COST.ToString().ToLower(),
                            TotalItems = item.NumberOfCostProfileItems, //# of cost profiles items
							SearchQueryType = "text",
                            IsAjaxQuery = true,
                            AjaxQueryName = "GetSearchResultCosts",
                            AjaxQueryValues = new Dictionary<string, object>()
                            {
                                { "SearchType", "credential" },
                                { "RecordId", item.Id },
                                { "TargetEntityType", "cost" }
                            }
                        },
						//Partial Loader
						new Models.Helpers.SearchTag()
						{
							CategoryName = "Test Partial Category", //Change this
							DisplayTemplate = "Test Partial Template", //Change this
							Name = "TestPartialName", //Change this (no spaces)
							TotalItems = 0, //Change this
							SearchQueryType = "partial", //Don't change this
							IsAjaxQuery = true, //Don't change this
							AjaxQueryName = "TestPanel", //This should be the name of the partial that lives in /Views/Search/ResultPanels, sans .cshtml
							AjaxQueryValues = new Dictionary<string, object>() //Make sure your partial uses an @model of Dictionary<string, object>
							{
								{ "ThisResultID", item.Id },
								{ "ThisResultCTID", item.CTID },
								{ "Parameter1ToSendToPartial", "valueToSend" },
								{ "Parameter2", 99 },
								{ "Parameter3", true },
								{ "Parameter4", "another value" }
							}
						}
                    }
                ) );
            }
            return output;
        }
        //

        public List<string> Deduplicate( List<string> items )
        {
            var added = new List<string>();
            var result = new List<string>();
            foreach ( var item in items )
            {
                var text = item.ToLower().Trim();
                if ( !added.Contains( text ) )
                {
                    added.Add( text );
                    result.Add( item.Trim() );
                }
            }
            return result;
        }
        //

        public List<Models.Helpers.SearchTagItem> GetSearchTagItems_Filter( List<CodeItem> items, string displayTemplate, int categoryID )
        {
            if ( "10 11 23".IndexOf( categoryID.ToString() ) > -1 )
            {
                return items.ConvertAll( m => new Models.Helpers.SearchTagItem()
                {
                    Display = Models.Helpers.SearchTagHelper.Count( displayTemplate.Replace( "{Name}", m.Name ), 1 ),
                    QueryValues = new Dictionary<string, object>()
                {
                    { "CategoryId", categoryID },
                    { "Code", m.Code },
                    { "SchemaName", m.SchemaName },
                    { "Name", m.Name },
                    { "CodeId", m.Id }
                }
                } );
            }
            else
            {
                return items.ConvertAll( m => new Models.Helpers.SearchTagItem()
                {
                    Display = Models.Helpers.SearchTagHelper.Count( displayTemplate.Replace( "{Name}", m.Name ), 1 ),
                    QueryValues = new Dictionary<string, object>()
                {
                    { "CategoryId", categoryID },
                    { "CodeId", m.Id },
                    { "SchemaName", m.SchemaName }
                }
                } );
            }

        }
        //

        public List<Models.Helpers.SearchTagItem> GetSearchTagItems_Filter( List<EnumeratedItem> items, string displayTemplate, int categoryID )
        {
            return items.ConvertAll( m => new Models.Helpers.SearchTagItem()
            {
                Display = Models.Helpers.SearchTagHelper.Count( displayTemplate.Replace( "{Name}", m.Name ), 1 ),
                QueryValues = new Dictionary<string, object>()
                {
                    { "CategoryId", categoryID },
                    { "CodeId", m.Id },
                    { "SchemaName", m.SchemaName }
                }
            } );
        }
        //

        public static TagSet GetTagSet( string searchType, TagTypes entityType, int recordID, int maxRecords = 10 )
        {
            var result = new TagSet();
            switch ( entityType ) //Match "Schema" in ConvertCredentialResults() method above
            {
                case TagTypes.COMPETENCIES:
                    {
                        var data = SearchServices.EntityCompetenciesList( searchType, recordID, maxRecords );
                        result = new TagSet()
                        {
                            Schema = TagTypes.COMPETENCIES.ToString().ToLower(),
                            Label = "Competencies",
                            Method = "direct",
                            Items = data.ConvertAll( m => new TagItem() { CodeId = m.Id, Label = m.TargetNodeName, Description = m.Description } )
                        };
                        break;
                    }

                case TagTypes.COST:
                    {
                        //future
                        var data = SearchServices.EntityCostsList( searchType, recordID, maxRecords );
                        result = new TagSet()
                        {
                            Schema = TagTypes.COST.ToString().ToLower(),
                            Label = "Costs",
                            Method = "direct",
                            CostItems = data.ConvertAll( c => new CostTagItem()
                            {
                                CodeId = c.CostProfileId,
                                Price = c.Price,
                                CostType = c.CostTypeName,
                                CurrencySymbol = c.CurrencySymbol,
                                SourceEntity = c.ParentEntityType
                            } ),
                            Items = data.ConvertAll( m => new TagItem() { CodeId = m.Id, Label = m.CostTypeName, Description = "" } )
                        };
                        break;
                    }
                default:
                    break;
            }
            return result;
        }
        //

        /*
		private List<TagItem> ConvertCodeItemsToTagItems( List<CodeItem> input )
		{
			var result = new List<TagItem>();
			foreach ( var item in input )
			{
				if ( result.FirstOrDefault( m => m.CodeId == item.Id ) == null ) //Prevent duplicates
				{
					result.Add( new TagItem() { CodeId = item.Id, Schema = item.SchemaName, Label = item.Name } );
				}
			}
			result = result.OrderBy( m => m.Label ).ToList();
			return result;
		}
		//
		*/
        public MainSearchResults ConvertOrganizationResults( List<OrganizationSummary> results, int totalResults, string searchType )
        {
            var output = new MainSearchResults() { TotalResults = totalResults, SearchType = searchType };
            foreach ( var item in results )
            {
                output.Results.Add( Result( item.Name, item.FriendlyName, item.Description, item.Id,
                    new Dictionary<string, object>()
                    {
                        { "Name", item.Name },
                        { "Description", item.Description },
                        { "OwnerId", 0 },
						//{ "CanEditRecord", item.CanEditRecord },
						{ "AvailableAt", new { Type = "locations", Data = ConvertAddresses( new List<Address>() { item.Address } ) } },
                        { "Locations", ConvertAddresses( item.Auto_Address ) },
                        { "SearchType", searchType },
                        { "RecordId", item.Id },
                        { "ctid", item.CTID },
                        { "UrlTitle", item.FriendlyName },
                        { "Logo", item.ImageUrl },
                        { "ResultImageUrl", item.ImageUrl ?? "" },
                        { "Location", item.Address.Country ?? "" + ( string.IsNullOrWhiteSpace( item.Address.Country ) ? "" : " - " ) + item.Address.City + ( string.IsNullOrWhiteSpace( item.Address.City ) ? "" : ", " ) + item.Address.AddressRegion },
                        { "LastUpdated", item.LastUpdated.ToShortDateString() },
                        { "Coordinates", new { Type = "coordinates", Data = new { Latitude = item.Address.Latitude, Longitude = item.Address.Longitude } } },
                        { "IsQA", item.ISQAOrganization ? "true" : "false" },
						
					},
                    null,
                    new List<Models.Helpers.SearchTag>()
                    {
						//Organization Type
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "OrganizationType",
                            DisplayTemplate = "{#} Organization Type{s}",
                            Name = TagTypes.ORGANIZATIONTYPE.ToString().ToLower(),
                            TotalItems = item.AgentType.Items.Count(),
                            SearchQueryType = "code",
                            Items = GetSearchTagItems_Filter( item.AgentType.Items.Take(10).ToList(), "{Name}", item.AgentType.Id )
                        },
						//Organization Sector Type
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "OrganizationSector",
                            DisplayTemplate = "{#} Sector{s}",
                            Name = TagTypes.ORGANIZATIONSECTORTYPE.ToString().ToLower(),
                            TotalItems = item.OrganizationSectorType.Items.Count(),
                            SearchQueryType = "code",
                            Items = GetSearchTagItems_Filter( item.OrganizationSectorType.Items.Take(10).ToList(), "{Name}", item.OrganizationSectorType.Id )
                        },
                        //Organization Service Type
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "OrganizationService",
                            DisplayTemplate = "{#} Service Type{s}",
                            Name = TagTypes.ORG_SERVICE_TYPE.ToString().ToLower(),
                            TotalItems = item.ServiceType.Items.Count(),
                            SearchQueryType = "code",
                            Items = GetSearchTagItems_Filter( item.ServiceType.Items, "{Name}", item.ServiceType.Id )
                        },
						//owns
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "OwnsCredentials",
                            DisplayTemplate = "Owns {#} Credential{s}",
                            Name = TagTypes.OWNED_BY.ToString().ToLower(),
                            TotalItems = item.OwnedByResults.Results.Count(),
                            SearchQueryType = "link",
                            Items = item.OwnedByResults.Results.Take(10).ToList().ConvertAll(m => new Models.Helpers.SearchTagItem()
                            {
                                Display = m.Title,
                                QueryValues = new Dictionary<string, object>()
                                {
                                    { "TargetId", m.Id }, //Credential ID
									{ "TargetType", "credential" },
                                }
                            } ).ToList()
                        },
						//offers
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "OffersCredentials",
                            DisplayTemplate = "Offers {#} Credential{s}",
                            Name = TagTypes.OFFERED_BY.ToString().ToLower(),
                            TotalItems = item.OfferedByResults.Results.Count(),
                            SearchQueryType = "link",
                            Items = item.OfferedByResults.Results.Take(10).ToList().ConvertAll(m => new Models.Helpers.SearchTagItem()
                            {
                                Display = m.Title,
                                QueryValues = new Dictionary<string, object>()
                                {
                                    { "TargetId", m.Id }, //Credential ID
									{ "TargetType", "credential" },
                                }
                            } ).ToList()
                        },
						//asmts owned by
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "OwnsAssessments",
                            DisplayTemplate = "Owns {#} Assessment{s}",
                            Name = TagTypes.ASMTS_OWNED_BY.ToString().ToLower(),
                            TotalItems = item.AsmtsOwnedByResults.Results.Count(),
                            SearchQueryType = "link",
                            Items = item.AsmtsOwnedByResults.Results.Take(10).ToList().ConvertAll(m => new Models.Helpers.SearchTagItem()
                            {
                                Display = m.Title,
                                QueryValues = new Dictionary<string, object>()
                                {
                                    { "TargetId", m.Id }, //asmt ID
									{ "TargetType", "assessment" },
                                }
                            } ).ToList()
                        },
						//lopps owned by
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "OwnsLearningOpportunity",
                            DisplayTemplate = "Owns {#} Learning Opportunit{ies}",
                            Name = TagTypes.LOPPS_OWNED_BY.ToString().ToLower(),
                            TotalItems = item.LoppsOwnedByResults.Results.Count(),
                            SearchQueryType = "link",
                            Items = item.LoppsOwnedByResults.Results.Take(10).ToList().ConvertAll(m => new Models.Helpers.SearchTagItem()
                            {
                                Display = m.Title,
                                QueryValues = new Dictionary<string, object>()
                                {
                                    { "TargetId", m.Id }, //lopp ID
									{ "TargetType", "learningopportunity" }, //??
								}
                            } ).ToList()
                        },

                        //Quality Assurance
                        new Models.Helpers.SearchTag()
                        {
                            CategoryName = "Quality Assurance",
                            DisplayTemplate = "{#} Quality Assurance",
                            Name = "qualityAssuranceBy", //Using the "quality" enum breaks this filter since it tries to find the matching item in the checkbox list and it doesn't exist
							TotalItems = item.QualityAssurance.Results.Count(),
                            SearchQueryType = "link", //Change this to "custom", or back to detail
							//Something like this...
							//QAOrgRolesResults is a list of 1 role and 1 org (org repeating for each relevant role)
							//e.g. [Accredited By] [Organization 1], [Approved By] [Organization 1], [Accredited By] [Organization 2], etc.
							Items = item.QualityAssurance.Results.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() {
                            Display = "<b>" + m.Relationship + "</b>" + "  " + m.Agent, //[Accredited By] [Organization 1]
							QueryValues = new Dictionary<string, object>() {
                                    { "TargetType", "organization" },
                                    { "TargetId", m.AgentId }
                                }
                            } ).ToList()

                        },
			            //Industries
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "Industries",
                            DisplayTemplate = "{#} Industr{ies}",
                            Name = TagTypes.INDUSTRIES.ToString().ToLower(),
                            TotalItems = item.NaicsResults.Results.Count(),
                            //SearchQueryType = "framework",
                            SearchQueryType = "text",
                            //Items = GetSearchTagItems_Filter( item.NaicsResults.Results, "{Name}", item.NaicsResults.CategoryId )
                            Items = item.NaicsResults.Results.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() { Display = m.CodeTitle, QueryValues = new Dictionary<string, object>() { { "TextValue", m.CodeTitle } } } )
                        },
                    }
                ) );
            }
            return output;
        }
        //
        public MainSearchResults ConvertOrganizationResultsOLD( List<Organization> results, int totalResults, string searchType )
        {
            var output = new MainSearchResults() { TotalResults = totalResults, SearchType = searchType };
            foreach ( var item in results )
            {
                output.Results.Add( Result( item.Name, item.FriendlyName, item.Description, item.Id,
                    new Dictionary<string, object>()
                    {
                        { "Name", item.Name },
                        { "Description", item.Description },
                        { "OwnerId", 0 },
						//{ "CanEditRecord", item.CanEditRecord },
						{ "AvailableAt", new { Type = "locations", Data = ConvertAddresses( new List<Address>() { item.Address } ) } },
                        { "Locations", ConvertAddresses( new List<Address>() { item.Address } ) },
                        { "SearchType", searchType },
                        { "RecordId", item.Id },
                        { "UrlTitle", item.FriendlyName },
                        { "Logo", item.ImageUrl },
                        { "ResultImageUrl", item.ImageUrl ?? "" },
                        { "Location", item.Address.Country + ( string.IsNullOrWhiteSpace( item.Address.Country ) ? "" : " - " ) + item.Address.City + ( string.IsNullOrWhiteSpace( item.Address.City ) ? "" : ", " ) + item.Address.AddressRegion },

                        { "Coordinates", new { Type = "coordinates", Data = new { Latitude = item.Address.Latitude, Longitude = item.Address.Longitude } } },
                        { "IsQA", item.ISQAOrganization ? "true" : "false" },
						//{ "AvailableAt", new { Type = "locations", Data = ConvertAddresses( item.Addresses ) } },

					},
                    null,
                    new List<Models.Helpers.SearchTag>()
                    {
						//Organization Type
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "OrganizationType",
                            DisplayTemplate = "{#} Organization Type{s}",
                            Name = TagTypes.ORGANIZATIONTYPE.ToString().ToLower(),
                            TotalItems = item.AgentType.Items.Count(),
                            SearchQueryType = "code",
                            Items = GetSearchTagItems_Filter( item.AgentType.Items, "{Name}", item.AgentType.Id )
                        },
						//Organization Sector Type
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "OrganizationSector",
                            DisplayTemplate = "{#} Economic Sector{s}",
                            Name = TagTypes.ORGANIZATIONSECTORTYPE.ToString().ToLower(),
                            TotalItems = item.OrganizationSectorType.Items.Count(),
                            SearchQueryType = "code",
                            Items = GetSearchTagItems_Filter( item.OrganizationSectorType.Items, "{Name}", item.OrganizationSectorType.Id )
                        },
                    }
                ) );
            }
            return output;
        }
        //

        public MainSearchResults ConvertAssessmentResults( List<AssessmentProfile> results, int totalResults, string searchType )
        {
            var output = new MainSearchResults() { TotalResults = totalResults, SearchType = searchType };
            foreach ( var item in results )
            {
                var subjects = Deduplicate( item.Subjects );
                var mergedQA = item.QualityAssurance.Results.Concat( item.Org_QAAgentAndRoles.Results ).ToList();

                output.Results.Add( Result( item.Name, item.Description, item.Id,
                    new Dictionary<string, object>()
                    {
                        { "Name", item.Name },
                        { "Description", item.Description },
                        { "Owner", string.IsNullOrWhiteSpace( item.OrganizationName ) ? "" : item.OrganizationName },
                        { "OwnerId", item.OwningOrganizationId },
                        { "ctid", item.CTID },
                        { "AvailableAt", new { Type = "locations", Data = ConvertAddresses( item.Addresses ) } },
                        { "Locations", ConvertAddresses( item.Addresses ) },
                        { "SearchType", searchType },
                        { "RecordId", item.Id },
                        { "UrlTitle", item.FriendlyName },
                        { "LastUpdated", item.LastUpdated.ToShortDateString() }
                    },
                    null,
                    new List<Models.Helpers.SearchTag>()
                    {
                        //Connections
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "Connections",
                            DisplayTemplate = "{#} Connection{s}",
                            Name = TagTypes.CONNECTIONS.ToString().ToLower(),
                            TotalItems = item.CredentialsList.Results.Count(),
                            SearchQueryType = "link",
							//Items = GetSearchTagItems_Filter( item.ConnectionsList.Results, "{Name} Credential(s)", item.ConnectionsList.CategoryId )
							//Something like this...
							/*	*/
							Items = item.CredentialsList.Results.Take(10).ToList().ConvertAll(m => new Models.Helpers.SearchTagItem()
                            {
                                Display = "<b>" + m.Connection + "</b>" + " " + m.Credential, //[Is Preparation For] [Some Credential Name] 
								QueryValues = new Dictionary<string, object>()
                                {
                                    { "TargetId", m.CredentialId }, //AgentId?
									{ "TargetType", "credential" }, //Probably okay to hard code this for now
									{ "ConnectionTypeId", m.ConnectionId }, //Connection type
								}
                            } ).ToList()

                        },
                        //Subjects
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "Subjects",
                            DisplayTemplate = "{#} Subject{s}",
                            Name = TagTypes.SUBJECTS.ToString().ToLower(),
                            TotalItems = subjects.Count(), //Returns a count of the de-duplicated items
							SearchQueryType = "text",
                            Items = subjects.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() { Display = m, QueryValues = new Dictionary<string, object>() { { "TextValue", m } } } )
                        },
                        
                        //Assessment Use Type
                        new Models.Helpers.SearchTag()
                        {
                            CategoryName = "AssessmentUseTypes",
                            DisplayTemplate = "{#} Assessment Use Type{s}",
                            Name = TagTypes.ASSESSMENT_USE_TYPES.ToString().ToLower(),
                            TotalItems = item.AssessmentUseTypes.Results.Count(),
                            SearchQueryType = "code",
                            Items = GetSearchTagItems_Filter(item.AssessmentUseTypes.Results, "{Name}", item.AssessmentUseTypes.CategoryId)
                        },
                        //Assessment Method Type
                        new Models.Helpers.SearchTag()
                        {
                            CategoryName = "AssessmentMethodTypes",
                            DisplayTemplate = "{#} Assessment Method Type{s}",
                            Name = TagTypes.ASSESSMENT_METHOD_TYPES.ToString().ToLower(),
                            TotalItems = item.AssessmentMethodTypes.Results.Count(),
                            SearchQueryType = "code",
                            Items = GetSearchTagItems_Filter(item.AssessmentMethodTypes.Results, "{Name}", item.AssessmentMethodTypes.CategoryId)
                        },
                        //Scoring Method Type
                        new Models.Helpers.SearchTag()
                        {
                            CategoryName = "ScoringMethodTypes",
                            DisplayTemplate = "{#} Scoring Method Type{s}",
                            Name = TagTypes.SCORING_METHODS.ToString().ToLower(),
                            TotalItems = item.ScoringMethodTypes.Results.Count(),
                            SearchQueryType = "code",
                            Items = GetSearchTagItems_Filter(item.ScoringMethodTypes.Results, "{Name}", item.ScoringMethodTypes.CategoryId)
                        },
                        //Delivery Method Type
                        new Models.Helpers.SearchTag()
                        {
                            CategoryName = "DeliveryMethodTypes",
                            DisplayTemplate = "{#} Delivery Method Type{s}",
                            Name = TagTypes.DELIVER_METHODS.ToString().ToLower(),
                            TotalItems = item.DeliveryMethodTypes.Results.Count(),
                            SearchQueryType = "code",
                            Items = GetSearchTagItems_Filter(item.DeliveryMethodTypes.Results, "{Name}", item.DeliveryMethodTypes.CategoryId)
                        },
                        //Instructional Program Classfication
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "InstructionalProgramType",
                            DisplayTemplate = "{#} Instructional Program{s}",
                            Name = "instructionalprogramtype",
                            TotalItems = item.InstructionalProgramClassification.Results.Count(),
                            SearchQueryType = "text",
                           //Items = GetSearchTagItems_Filter( item.InstructionalProgramClassification.Results, "{Name}", item.InstructionalProgramClassification.CategoryId )
                            Items = item.InstructionalProgramClassification.Results.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() { Display = m.CodeTitle, QueryValues = new Dictionary<string, object>() { { "TextValue", m.CodeTitle } } } )
                        },
                        
                        //Assessment Quality Assurance
                        new Models.Helpers.SearchTag()
                        {
                            CategoryName = "Quality Assurance",
                            DisplayTemplate = "{#} Quality Assurance",
                            Name = "qualityassurance", //Using the "quality" enum breaks this filter since it tries to find the matching item in the checkbox list and it doesn't exist
							TotalItems = mergedQA.Count(),
                            SearchQueryType = "merged", //Change this to "custom", or back to detail
					        Items = mergedQA.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() {
                            Display = "<b>" + m.Relationship + "</b>" + " " + m.Agent, //[Accredited By] [Organization 1]
							QueryValues = new Dictionary<string, object>() {
                                    { "Relationship", m.Relationship },
                                    { "TextValue", m.Agent },
                                    { "RelationshipId", m.RelationshipId },
                                    { "AgentId", m.AgentId }, //AgentId?
									{ "TargetType", m.EntityType },
                                    { "AgentUrl", m.AgentUrl},
                                    { "EntityStateId", m.EntityStateId }
                                }
                            } ).ToList()
                        },

						//Competencies
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "AssessesCompetencies",
                            DisplayTemplate = "Assesses {#} Competenc{ies}",
                            Name = TagTypes.COMPETENCIES.ToString().ToLower(),
                            TotalItems = item.CompetenciesCount,
                            SearchQueryType = "text",
                            IsAjaxQuery = true,
                            AjaxQueryName = "GetSearchResultCompetencies",
                            AjaxQueryValues = new Dictionary<string, object>()
                            {
                                { "SearchType", "assessment" },
                                { "RecordId", item.Id },
                                { "TargetEntityType", "competencies" }
                            }
                        },
						//Competencies direct - not used
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "AssessesCompetenciesDirect",
                            DisplayTemplate = "Assesses {#} Competenc{ies} Direct",
                            Name = TagTypes.COMPETENCIES.ToString().ToLower(),
                            TotalItems = item.AssessesCompetencies.Count(),
                            SearchQueryType = "detail",
                            Items = item.AssessesCompetencies.ConvertAll( m => new Models.Helpers.SearchTagItem()
                            {
                                Display = string.IsNullOrWhiteSpace(m.TargetNodeDescription) ?
                                m.TargetNodeName :
                                "<b>" + m.TargetNodeName + "</b>" + System.Environment.NewLine + m.TargetNodeDescription,
                                QueryValues = new Dictionary<string, object>()
                                {
                                    { "SchemaName", null },
                                    { "CodeId", m.Id },
                                    { "TextValue", m.TargetNodeName },
                                    { "TextDescription", m.TargetNodeDescription }
                                }
                            } )
                        },
                    }
                ) );
            }
            return output;
        }
        //

        public MainSearchResults ConvertLearningOpportunityResults( List<LearningOpportunityProfile> results, int totalResults, string searchType )
        {
            var output = new MainSearchResults() { TotalResults = totalResults, SearchType = searchType };
            foreach ( var item in results )
            {
                var subjects = Deduplicate( item.Subjects );
                var mergedQA = item.QualityAssurance.Results.Concat( item.Org_QAAgentAndRoles.Results ).ToList();
                output.Results.Add( Result( item.Name, item.Description, item.Id,
                    new Dictionary<string, object>()
                    {
                        { "Name", item.Name },
                        { "Description", item.Description },
                        { "Owner", string.IsNullOrWhiteSpace( item.OrganizationName ) ? "" : item.OrganizationName },
                        { "OwnerId", item.OwningOrganizationId },
						//{ "CanEditRecord", item.CanEditRecord },
						{ "AvailableAt", new { Type = "locations", Data = ConvertAddresses( item.Addresses ) } },
                        { "Locations", ConvertAddresses( item.Addresses ) },
                        { "SearchType", searchType },
                        { "RecordId", item.Id },
                        { "ctid", item.CTID },
                        { "UrlTitle", item.FriendlyName },
                        { "LastUpdated", item.LastUpdated.ToShortDateString() }
                    },
                    null,
                    new List<Models.Helpers.SearchTag>()
                    {
                        //Subjects
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "Subjects",
                            DisplayTemplate = "{#} Subject{s}",
                            Name = TagTypes.SUBJECTS.ToString().ToLower(),
                            TotalItems = subjects.Count(), //Returns a count of the de-duplicated items
							SearchQueryType = "text",
                            Items = subjects.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() { Display = m, QueryValues = new Dictionary<string, object>() { { "TextValue", m } } } )
                        },

                        //Lopp Quality Assurance
                        new Models.Helpers.SearchTag()
                        {
                            CategoryName = "Quality Assurance",
                            DisplayTemplate = "{#} Quality Assurance",
                            Name = "qualityassurance", //Using the "quality" enum breaks this filter since it tries to find the matching item in the checkbox list and it doesn't exist
							TotalItems = mergedQA.Count(),
                            SearchQueryType = "merged", //Change this to "custom", or back to detail
							Items = mergedQA.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() {
                            Display = "<b>" + m.Relationship + "</b>" + " by " + m.Agent, //[Accredited By] [Organization 1]
							QueryValues = new Dictionary<string, object>() {
                                    { "Relationship", m.Relationship },
                                    { "TextValue", m.Agent },
                                    { "RelationshipId", m.RelationshipId },
                                    { "AgentId", m.AgentId }, 
									{ "TargetType", m.EntityType },
                                    { "AgentUrl", m.AgentUrl},
                                    { "EntityStateId", m.EntityStateId }
                                }
                            } ).ToList()
						},

                        //Connections
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "Connections",
                            DisplayTemplate = "{#} Connection{s}",
                            Name = TagTypes.CONNECTIONS.ToString().ToLower(),
                            TotalItems = item.CredentialsList.Results.Count(),
                            SearchQueryType = "link",
							//Items = GetSearchTagItems_Filter( item.ConnectionsList.Results, "{Name} Credential(s)", item.ConnectionsList.CategoryId )
							//Something like this...
							/*	*/
							Items = item.CredentialsList.Results.Take(10).ToList().ConvertAll(m => new Models.Helpers.SearchTagItem()
                            {
                                Display = "<b>" + m.Connection + "</b>" + " " + m.Credential, //[Is Preparation For] [Some Credential Name] 
								QueryValues = new Dictionary<string, object>()
                                {
                                    { "TargetId", m.CredentialId }, //AgentId?
									{ "TargetType", "credential" }, //Probably okay to hard code this for now
									{ "ConnectionTypeId", m.ConnectionId }, //Connection type
								}
                            } ).ToList()

                        },

                        //Delivery Method Type
                        new Models.Helpers.SearchTag()
                        {
                            CategoryName = "DeliveryMethodTypes",
                            DisplayTemplate = "{#} Delivery Method Type{s}",
                            Name = TagTypes.DELIVER_METHODS.ToString().ToLower(),
                            TotalItems = item.DeliveryMethodTypes.Results.Count(),
                            SearchQueryType = "code",
                            Items = GetSearchTagItems_Filter(item.DeliveryMethodTypes.Results, "{Name}", item.DeliveryMethodTypes.CategoryId)
                        },
                         //Learning Method Type
                        new Models.Helpers.SearchTag()
                        {
                            CategoryName = "LearningMethodTypes",
                            DisplayTemplate = "{#} Learning Method Type{s}",
                            Name = TagTypes.LEARNING_METHODS.ToString().ToLower(),
                            TotalItems = item.LearningMethodTypes.Results.Count(),
                            SearchQueryType = "code",
                            Items = GetSearchTagItems_Filter(item.LearningMethodTypes.Results, "{Name}", item.LearningMethodTypes.CategoryId)
                        },
                        //Instructional Program Classfication
				        new Models.Helpers.SearchTag()
                        {
                            CategoryName = "InstructionalProgramType",
                            DisplayTemplate = "{#} Instructional Program{s}",
                            Name = "instructionalprogramtype",
                            TotalItems = item.InstructionalProgramClassification.Results.Count(),
                            SearchQueryType = "text",
                           //Items = GetSearchTagItems_Filter( item.InstructionalProgramClassification.Results, "{Name}", item.InstructionalProgramClassification.CategoryId )
                            Items = item.InstructionalProgramClassification.Results.Take(10).ToList().ConvertAll( m => new Models.Helpers.SearchTagItem() { Display = m.CodeTitle, QueryValues = new Dictionary<string, object>() { { "TextValue", m.CodeTitle } } } )
                        },
						//Competencies
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "TeachesCompetencies",
                            DisplayTemplate = "Teaches {#} Competenc{ies}",
                            Name = TagTypes.COMPETENCIES.ToString().ToLower(),
                            TotalItems = item.CompetenciesCount,
                            SearchQueryType = "text",
                            IsAjaxQuery = true,
                            AjaxQueryName = "GetSearchResultCompetencies",
                            AjaxQueryValues = new Dictionary<string, object>()
                            {
                                { "SearchType", "learningopportunity" },
                                { "RecordId", item.Id },
                                { "TargetEntityType", "competencies" }
                            }
                        },
						//Competencies direct
						new Models.Helpers.SearchTag()
                        {
                            CategoryName = "TeachesCompetenciesDirect",
                            DisplayTemplate = "Teaches {#} Competenc{ies} Direct",
                            Name = TagTypes.COMPETENCIES.ToString().ToLower(),
                            TotalItems = item.TeachesCompetencies.Count(),
                            SearchQueryType = "detail",
                            Items = item.TeachesCompetencies.ConvertAll( m => new Models.Helpers.SearchTagItem()
                            {
                                Display = string.IsNullOrWhiteSpace(m.TargetNodeDescription) ?
                                m.TargetNodeName :
                                "<b>" + m.TargetNodeName + "</b>" + System.Environment.NewLine + m.TargetNodeDescription,
                                QueryValues = new Dictionary<string, object>()
                                {
                                    { "SchemaName", null },
                                    { "CodeId", m.Id },
                                    { "TextValue", m.TargetNodeName },
                                    { "TextDescription", m.TargetNodeDescription }
                                }
                            } )
                        },

                    }
                ) );
            }
            return output;
        }
        //
        public Dictionary<string, string> ConvertCompetenciesToDictionary( List<CredentialAlignmentObjectProfile> input )
        {
            var result = new Dictionary<string, string>();
            if ( input != null )
            {
                foreach ( var item in input )
                {
                    try
                    {
                        result.Add( item.Id.ToString(), item.Description );
                    }
                    catch { }
                }
            }
            return result;
        }
        public MainSearchResult Result( string name, string description, int recordID, Dictionary<string, object> properties, List<TagSet> tags, List<Models.Helpers.SearchTag> tagsV2 = null )
        {
            return new MainSearchResult()
            {
                Name = string.IsNullOrWhiteSpace( name ) ? "No name" : name,
                Description = string.IsNullOrWhiteSpace( description ) ? "No description" : description,
                RecordId = recordID,
                Properties = properties == null ? new Dictionary<string, object>() : properties,
                Tags = tags == null ? new List<TagSet>() : tags,
                TagsV2 = tagsV2 ?? new List<Models.Helpers.SearchTag>()
            };
        }
        //
        public MainSearchResult Result( string name, string friendlyName, string description, int recordID, Dictionary<string, object> properties, List<TagSet> tags, List<Models.Helpers.SearchTag> tagsV2 = null )
        {
            return new MainSearchResult()
            {
                Name = string.IsNullOrWhiteSpace( name ) ? "No name" : name,
                FriendlyName = string.IsNullOrWhiteSpace( friendlyName ) ? "Record" : friendlyName,
                Description = string.IsNullOrWhiteSpace( description ) ? "No description" : description,
                RecordId = recordID,
                Properties = properties == null ? new Dictionary<string, object>() : properties,
                Tags = tags == null ? new List<TagSet>() : tags,
                TagsV2 = tagsV2 ?? new List<Models.Helpers.SearchTag>()
            };
        }
        //
        public Dictionary<string, string> ConvertCodeItemsToDictionary( List<CodeItem> input )
        {
            var result = new Dictionary<string, string>();
            foreach ( var item in input )
            {
                try
                {
                    result.Add( item.Code, item.Name );
                }
                catch { }
            }
            return result;
        }
        //

        public List<Dictionary<string, object>> ConvertAddresses( List<Address> input )
        {
            var result = new List<Dictionary<string, object>>();
            foreach ( var item in input )
            {
                try
                {
                    var data = new Dictionary<string, object>()
                    {
                        { "Latitude", item.Latitude },
                        { "Longitude", item.Longitude },
                        { "Address", item.DisplayAddress() }
                    };
                    result.Add( data );
                }
                catch { }
            }
            return result;
        }
        //

        public static List<string> Autocomplete_Subjects( int entityTypeId, int categoryId, string keyword, int maxTerms = 25 )
        {
            List<string> list = new List<string>();
            list = CF.Entity_ReferenceManager.QuickSearch_Subjects( entityTypeId, keyword, maxTerms );
            return list;
        }

        public static List<string> Autocomplete_Occupations( int entityTypeId, string keyword, int maxTerms = 25 )
        {
            return CF.Entity_ReferenceManager.QuickSearch_ReferenceFrameworks( entityTypeId, 11, "", keyword, maxTerms );
        }

        public static List<string> Autocomplete_Industries( int entityTypeId, string keyword, int maxTerms = 25 )
        {
            return CF.Entity_ReferenceManager.QuickSearch_ReferenceFrameworks( entityTypeId, 10, "", keyword, maxTerms );
        }
        public static List<string> Autocomplete_Cip( int entityTypeId, string keyword, int maxTerms = 25 )
        {
            return CF.Entity_ReferenceManager.QuickSearch_ReferenceFrameworks( entityTypeId, 23, "", keyword, maxTerms );
        }

        #region Common filters
        public static void HandleCustomFilters( MainSearchInput data, int searchCategory, ref string where )
        {
            string AND = "";
            //may want custom category for each one, to prevent requests that don't match the current search

            string sql = "";

            //Updated to use FilterV2
            if ( where.Length > 0 )
            {
                AND = " AND ";
            }
            foreach ( var filter in data.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ).ToList() )
            {
                var item = filter.AsCodeItem();
                if ( item.CategoryId != searchCategory )
                {
                    continue;
                }

                sql = GetPropertySql( item.Id );
                if ( string.IsNullOrWhiteSpace( sql ) == false )
                {
                    where = where + AND + sql;
                    AND = " AND ";
                }
            }
            if ( sql.Length > 0 )
            {
                LoggingHelper.DoTrace( 6, "SearchServices.HandleCustomFilters. result: \r\n" + where );
            }

            /* //Retained for reference
			foreach ( MainSearchFilter filter in data.Filters.Where( s => s.CategoryId == searchCategory ) )
			{
				//each item will be a custom sql 
				//the propertyId will differ in env, so can't use it for lookup in web.config. Could get from db, and cache
				if ( where.Length > 0 )
					AND = " AND ";
				int id = 0;
				foreach ( string item in filter.Items )
				{
					if (Int32.TryParse(item, out id)) 
					{
						sql = GetPropertySql( id );
						if ( string.IsNullOrWhiteSpace( sql ) == false )
						{
							where = where + AND + sql;
							AND = " AND ";
						}
					}
				}
				
			}
			if ( sql.Length > 0 )
			{
				LoggingHelper.DoTrace( 6, "SearchServices.HandleCustomFilters. result: \r\n" + where );
			}
			*/
        }
        public  static string GetPropertySql( int id )
        {
            string sql = "";
            string key = "propertySql_" + id.ToString();
            //check cache for vocabulary
            if ( HttpRuntime.Cache[key] != null )
            {
                sql = ( string )HttpRuntime.Cache[key];
                return sql;
            }

            CodeItem item = CF.CodesManager.Codes_PropertyValue_Get( id );
            if ( item != null && ( item.Description ?? "" ).Length > 5 )
            {
                sql = item.Description;
                HttpRuntime.Cache.Insert( key, sql );
            }

            return sql;
        }

        public static void SetSubjectsFilter( MainSearchInput data, int entityTypeId, ref string where )
        {
            string subjects = "  (base.RowId in (SELECT EntityUid FROM [Entity_Subjects] a where EntityTypeId = {0} AND {1} )) ";
            //RowId is same as EntityUid
            //if ( data.SearchType == "credential" )
            //    subjects = subjects.Replace( "base.RowId", "base.EntityUid" );

            string frameworkItems = " OR (RowId in (SELECT EntityUid FROM [dbo].[Entity_Reference_Summary] a where CategoryId= 23 AND {0} ) ) ";

            string phraseTemplate = " (a.Subject like '{0}') ";
            string titleTemplate = " (a.TextValue like '{0}') ";

            string AND = "";
            string OR = "";
            // string keyword = "";

            //Updated to use FilterV2
            string next = "";
            string fnext = "";
            if ( where.Length > 0 )
            {
                AND = " AND ";
            }

            foreach ( var filter in data.FiltersV2.Where( m => m.Name == "subjects" ) )
            {
                var text = ServiceHelper.HandleApostrophes( filter.AsText() );
                if ( string.IsNullOrWhiteSpace( text ) )
                {
                    continue;
                }

                next += OR + string.Format( phraseTemplate, SearchifyWord( text ) );
                fnext += OR + string.Format( titleTemplate, SearchifyWord( text ) );
                OR = " OR ";
            }
            string fsubject = "";
            if ( !string.IsNullOrWhiteSpace( fnext )
                && ( entityTypeId == 3 || entityTypeId == 7 ) )
            {
                fsubject = string.Format( frameworkItems, fnext );
            }
            if ( !string.IsNullOrWhiteSpace( next ) )
            {
                where = where + AND + " ( " + string.Format( subjects, entityTypeId, next ) + fsubject + ")";
            }

            /* //Retained for reference
			foreach ( MainSearchFilter filter in data.Filters.Where( s => s.Name == "subjects" ) )
			{
				string next = "";
				if ( where.Length > 0 )
					AND = " AND ";
				foreach ( string item in filter.Texts )
				{
					keyword = ServiceHelper.HandleApostrophes( item );
					if ( keyword.IndexOf( ";" ) > -1 )
					{
						var words = keyword.Split( ';' );
						foreach ( string word in words )
						{
							next += OR + string.Format( phraseTemplate, PrepWord( word) );
							OR = " OR ";
						}
					}
					else
					{
						next = string.Format( phraseTemplate, PrepWord( keyword ) );
					}
					//next += keyword;	//					+",";
					//just handle one for now
					break;
				}
				//next = next.Trim( ',' );
				if ( !string.IsNullOrWhiteSpace( next ) )
					where = where + AND + string.Format( subjects, entityTypeId, next );

				break;
			}
			*/
        }
        /// <summary>
        /// May want to make configurable, in case don't want to always perform check.
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public static string SearchifyWord( string word )
        {
            string keyword = word.Trim() + "^";


            if ( keyword.ToLower().LastIndexOf( "es^" ) > 4 )
            {
                keyword = keyword.Substring( 0, keyword.ToLower().LastIndexOf( "es" ) );
            }
            else if ( keyword.ToLower().LastIndexOf( "s^" ) > 4 )
            {
                keyword = keyword.Substring( 0, keyword.ToLower().LastIndexOf( "s" ) );
            }

            if ( keyword.ToLower().LastIndexOf( "ing^" ) > 3 )
            {
                keyword = keyword.Substring( 0, keyword.ToLower().LastIndexOf( "ing^" ) );
            }
            else if ( keyword.ToLower().LastIndexOf( "ed^" ) > 4 )
            {
                keyword = keyword.Substring( 0, keyword.ToLower().LastIndexOf( "ed^" ) );
            }
            else if ( keyword.ToLower().LastIndexOf( "ion^" ) > 3 )
            {
                keyword = keyword.Substring( 0, keyword.ToLower().LastIndexOf( "ion^" ) );
            }
            else if ( keyword.ToLower().LastIndexOf( "ive^" ) > 3 )
            {
                keyword = keyword.Substring( 0, keyword.ToLower().LastIndexOf( "ive^" ) );
            }

            if ( keyword.IndexOf( "%" ) == -1 )
            {
                keyword = "%" + keyword.Trim() + "%";
                keyword = keyword.Replace( "&", "%" ).Replace( " and ", "%" ).Replace( " in ", "%" ).Replace( " of ", "%" ).Replace( " for ", "%" ).Replace( " with ", "%" );
                keyword = keyword.Replace( " from ", "%" );
                keyword = keyword.Replace( " a ", "%" );
                keyword = keyword.Replace( " - ", "%" );
                keyword = keyword.Replace( " % ", "%" );

                //just replace all spaces with %?
                keyword = keyword.Replace( "  ", "%" );
                keyword = keyword.Replace( " ", "%" );
                keyword = keyword.Replace( "%%", "%" );
            }


            keyword = keyword.Replace( "^", "" );
            return keyword;
        }

        public static void SetPropertiesFilter( MainSearchInput data, int entityTypeId, string searchCategories, ref string where )
        {
            string AND = "";
            string template = " ( base.Id in ( SELECT  [EntityBaseId] FROM [dbo].[EntityProperty_Summary] where EntityTypeId= {0} AND [PropertyValueId] in ({1}))) ";
            int prevCategoryId = 0;

            //Updated to use FiltersV2
            string next = "";
            if (where.Length > 0)
                AND = " AND ";
            foreach (var filter in data.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ).ToList())
            {
                var item = filter.AsCodeItem();
                if (searchCategories.Contains( item.CategoryId.ToString() ))
                {
                    if (item.CategoryId != prevCategoryId)
                    {
                        if (prevCategoryId > 0)
                        {
                            next = next.Trim( ',' );
                            where = where + AND + string.Format( template, entityTypeId, next );
                            AND = " AND ";
                        }
                        prevCategoryId = item.CategoryId;
                        next = "";
                    }
                    next += item.Id + ",";
                }
            }
            next = next.Trim( ',' );
            if (!string.IsNullOrWhiteSpace( next ))
            {
                where = where + AND + string.Format( template, entityTypeId, next );
            }

        }
        public static void SetBoundariesFilter( MainSearchInput data, ref string where )
        {
            string AND = "";
            if ( where.Length > 0 )
                AND = " AND ";
            string template = " ( base.RowId in ( SELECT  b.EntityUid FROM [dbo].[Entity.Address] a inner join Entity b on a.EntityId = b.Id    where [Longitude] < {0} and [Longitude] > {1} and [Latitude] < {2} and [Latitude] > {3} ) ) ";
            if ( data.SearchType == "credential" )
                template = template.Replace( "base.RowId", "base.EntityUid" );

            var boundaries = SearchServices.GetBoundaries( data, "bounds" );
            if ( boundaries.IsDefined )
            {
                where = where + AND + string.Format( template, boundaries.East, boundaries.West, boundaries.North, boundaries.South );
            }
        }
        //
        public static void SetRolesFilter( MainSearchInput data, ref string where )
        {
            string AND = "";
            if ( where.Length > 0 )
                AND = " AND ";
            string template = " ( base.RowId in ( SELECT distinct b.EntityUid FROM [dbo].[Entity.AgentRelationship] a inner join Entity b on a.EntityId = b.Id   where [RelationshipTypeId] in ({0})   ) ) ";

            //if ( data.SearchType == "credential" )
            //    template = template.Replace( "base.RowId", "base.EntityUid" );

            //Updated to use FiltersV2
            string next = "";
            if ( where.Length > 0 )
                AND = " AND ";
            foreach ( var filter in data.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CODE ) )
            {
                var item = filter.AsCodeItem();
                if ( item.CategoryId == 13 )
                {
                    next += item.Id + ",";
                }
            }
            next = next.Trim( ',' );
            if ( !string.IsNullOrWhiteSpace( next ) )
            {
                where = where + AND + string.Format( template, next );
            }

            /* //Retained for reference
			foreach ( MainSearchFilter filter in data.Filters.Where( s => s.CategoryId == 13  ) )
			{
				string next = "";
				if ( where.Length > 0 )
					AND = " AND ";
				foreach ( string item in filter.Items )
				{
					next += item + ",";
				}
				next = next.Trim( ',' );
				where = where + AND + string.Format( template, next );
			}
			*/
        }


        public static void SetOrgRolesFilter( MainSearchInput data, ref string where )
        {
            string AND = "";
            if ( where.Length > 0 )
                AND = " AND ";
            string template = " ( base.RowId in ( SELECT distinct EntityUid FROM [dbo].[Entity.AgentRelationship] a inner join Entity b on a.EntityId = b.Id   where [RelationshipTypeId] in ({0})   ) ) ";

            foreach ( MainSearchFilter filter in data.Filters.Where( s => s.CategoryId == 13 ) )
            {
                string next = "";
                if ( where.Length > 0 )
                    AND = " AND ";
                foreach ( string item in filter.Items )
                {
                    next += item + ",";
                }
                next = next.Trim( ',' );
                where = where + AND + string.Format( template, next );
            }
        }

        private static void SetAuthorizationFilter( AppUser user, string summaryView, ref string where )
        {
            string AND = "";
            if ( where.Length > 0 )
                AND = " AND ";
            return;


            //if ( user == null || user.Id == 0 )
            //{
            //	//public only records
            //	where = where + AND + string.Format( " (base.StatusId = {0}) ", CF.CodesManager.ENTITY_STATUS_PUBLISHED );
            //	return;
            //}

            ////if ( AccountServices.IsUserSiteStaff( user )
            ////  || AccountServices.CanUserViewAllContent( user ) )
            ////{
            ////	//can view all, edit all
            ////	return;
            ////}

            ////can only view where status is published, or associated with 
            //where = where + AND + string.Format( "((base.StatusId = {0}) OR (base.Id in (SELECT cs.Id FROM [dbo].[Organization.Member] om inner join [{1}] cs on om.ParentOrgId = cs.ManagingOrgId where userId = {2}) ))", CF.CodesManager.ENTITY_STATUS_PUBLISHED, summaryView, user.Id );

        }
        #endregion
    }
}
