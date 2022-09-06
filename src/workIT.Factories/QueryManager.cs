using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.API;
using workIT.Models.Services.Reports;

using EntityContext = workIT.Data.Tables.workITEntities;
using LinkCheckerServices;
using Views = workIT.Data.Views;
using ViewContext = workIT.Data.Views.workITViews;
using workIT.Utilities;

namespace workIT.Factories
{
    public class QueryManager : BaseFactory
    {
        private static string thisClassName = "QueryManager";
        #region Duplicates
        /// <summary>
        /// Report.Duplicates queries
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static List<QuerySummary> EntityDuplicates( Query request )
        {
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            try
            {
                using ( var context = new EntityContext() )
                {
                    //Start the query
                    var query = context.Reports_Duplicates.Where( s => s.Id > 0 );

					//Determine which type of report to run
					var organizationCTIDs = request.GetFilterValues( "filter:OrganizationCTID", new List<string>() );
                    if ( organizationCTIDs?.Count() > 0 )
                    {
                        if ( IsForPublishingRecipients( request ) )
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.PublisherCTID ) );
                        }
                        else
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.OrganizationCTID ) /*|| s.PublisherCTID == request.OrganizationCTID*/ ); //Same as OwnerCTID
                        }
                    }
					/*
                    if ( !string.IsNullOrWhiteSpace( request.OwnerCTID ) )
                    {
                        query = query.Where( s => s.OrganizationCTID == request.OwnerCTID );
                    }
					*/

                    //handle entity type
                    var EntityType = request.GetFilterValue( "filter:ResourceType" );
                    if( !string.IsNullOrWhiteSpace( EntityType ))
                    {
                        query = query.Where( s => s.EntityType == EntityType );
                    }

                    //Handle Keywords Name filter
                    var nameText = request.GetFilterValue( "filter:NameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( nameText ) )
                    {
                        query = query.Where( s => s.Name.ToLower().Contains( nameText ) );
                    }

                    //Get total
                    request.TotalRows = query.Count();

                    var results = query.OrderBy( s => s.Organization ).ThenBy( s => s.Name )
                        .Skip( request.Skip ).Take( request.Take ).ToList();
                        if ( results != null && results.Count > 0 )
                        {
                            foreach ( var item in results )
                            {
                                entity = new QuerySummary()
                                {
                                    PublisherCTID = item.PublisherCTID,
                                    Publisher = item.Publisher,
                                    Organization = item.Organization,
                                    DataOwnerCTID = item.OrganizationCTID,
                                    Name = item.Name,
                                    EntityCTID = item.CTID,
                                    EntityType = item.EntityType,
                                    EntitySubType = item.Category != "" ? item.Category : item.EntityType ,
                                    Id = item.Id,
                                    SubjectWebpage = item.SubjectWebpage,
                                    Description = item.Description,
                                    //FinderURL = item.FinderURL,
                                    LastUpdated = ( DateTime ) item.LastUpdated,
                                    IsInPublisher = item.ExistsInPublisher.HasValue ? item.ExistsInPublisher.Value : false
                                };
                                output.Add( entity );
                            }
                        }
                    }
            }
            catch ( Exception ex )
            {
                var msg = FormatExceptions( ex );
                LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DuplicatesQuery. " + msg ) );
                output.Add( new QuerySummary()
                {
                    Publisher = "Error encountered",
                    Organization = ex.Message,
                    Name = msg
                } );
            }
            return output;
        }

        public static List<QuerySummary> DuplicateSummary(Query request )
        {
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            try
            {
                using ( var context = new EntityContext() )
                {
                    //Start the query for the desired entitytype
                    var query = context.Reports_Duplicates.Where( s => s.Id > 0 );

					//Determine which type of report to run
					var organizationCTIDs = request.GetFilterValues( "filter:OrganizationCTID", new List<string>() );
					if ( organizationCTIDs?.Count() > 0 )
                    {
                        if ( IsForPublishingRecipients( request ) )
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.PublisherCTID ) );
                        }
                        else
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.OrganizationCTID ) /*|| s.PublisherCTID == request.OrganizationCTID*/ ); //Same as OwnerCTID
                        }
                    }
					/*
                    if ( !string.IsNullOrWhiteSpace( request.OwnerCTID ) )
                    {
                        query = query.Where( s => s.OrganizationCTID == request.OwnerCTID );
                    }
					*/

                    //handle entity type
                    var EntityType = request.GetFilterValue( "filter:ResourceType" );
                    if ( !string.IsNullOrWhiteSpace( EntityType ) )
                    {
                        query = query.Where( s => s.EntityType==EntityType);
                    }

                    //Handle Keywords Name filter
                    var OrgnameText = request.GetFilterValue( "filter:OrgNameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( OrgnameText ) )
                    {
                        query = query.Where( s => s.Publisher.ToLower().Contains( OrgnameText ) );
                    }

                    //Get total
                    request.TotalRows = query.Count();
                    var results = query.OrderBy( s => s.Organization ).ThenBy( s => s.Name ).ToList();
                    if( IsForPublishingRecipients(request) )
                    {
                        var result = results.GroupBy( s => s.Publisher ).Select( s => new { Count = s.Count(), OrgName = s.Key, CTID = s.First().PublisherCTID } ).OrderByDescending( s => s.Count ); // group by publisher name 
                        request.TotalRows = result.Count();
                        if ( result != null )
                        {
                            foreach ( var item in result )
                            {
                                entity = new QuerySummary()
                                {
                                    PublisherCTID = item.CTID,
                                    Publisher = item.OrgName,
                                    TotalItems = item.Count
                                };
                                output.Add( entity );
                            }
                        }
                    }
                    else
                    {
                        var result = results.GroupBy( s => s.Organization ).Select( s => new { Count = s.Count(), OrgName = s.Key, CTID = s.First().OrganizationCTID } ).OrderByDescending( s => s.Count ); // group by owner name 
                        request.TotalRows = result.Count();
                        if ( result != null )
                        {
                            foreach ( var item in result )
                            {
                                entity = new QuerySummary()
                                {
                                    PublisherCTID = item.CTID,
                                    Publisher = item.OrgName,
                                    TotalItems = item.Count
                                };
                                output.Add( entity );
                            }
                        }
                    }
                    output=output.OrderBy(s=>s.Publisher).Skip( request.Skip ).Take( request.Take ).ToList();


                }
            }
            catch ( Exception ex )
            {
                var msg = FormatExceptions( ex );
                LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DuplicatesQuery. " + msg ) );
                output.Add( new QuerySummary()
                {
                    Publisher = "Error encountered",
                    Organization = ex.Message,
                    Name = msg
                } );
            }
            return output;
        }

        /// <summary>
        /// Populate Reports.Duplicates
        /// </summary>
        public void PopulateReportsDuplicates()
        {
            string connectionString = MainConnection();
            try
            {
                using ( SqlConnection c = new SqlConnection( connectionString ) )
                {
                    c.Open();

                    using ( SqlCommand command = new SqlCommand( "[Reports.PopulateDuplicates]", c ) )
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 300;
                        command.ExecuteNonQuery();
                        command.Dispose();
                        c.Close();
                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, "PopulateReportsDuplicates", false );
            }
        }

        #region Old methods
        public static List<QuerySummary> DuplicateCredentialsNameDescSWP( Query request )
        {
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            try
            {
                using ( var context = new ViewContext() )
                {
                    //Start the query
                    var query = context.Reports_DuplicateCredentialsOrgCredentialDescSWP.Where( s => s.Id > 0 );

					//Determine which type of report to run
					var organizationCTIDs = request.GetFilterValues( "filter:OrganizationCTID", new List<string>() );
					if ( organizationCTIDs?.Count() > 0 )
                    {
                        if ( IsForPublishingRecipients( request ) )
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.PublisherCTID ) );
                        }
                        else
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.DataOwnerCTID ) || organizationCTIDs.Contains( s.PublisherCTID ) ); //Same as OwnerCTID
                        }
                    }
					/*
                    if ( !string.IsNullOrWhiteSpace( request.OwnerCTID ) )
                    {
                        query = query.Where( s => s.DataOwnerCTID == request.OwnerCTID );
                    }
					*/

                    /*
                    if ( !string.IsNullOrWhiteSpace( request.PublishingOrganizationCTID ) ) 
                    {
                        query = query.Where( s => s.PublisherCTID == request.PublishingOrganizationCTID );
                    }
                    //if ( !string.IsNullOrWhiteSpace( request.OrganizationCTID ) )
                    //{
                    //    query = query.Where( s => s.DataOwnerCTID == request.OrganizationCTID );
                    //}
                    if ( !string.IsNullOrWhiteSpace( request.OrganizationCTID ) )
                    {
                        query = query.Where( s => s.DataOwnerCTID == request.OrganizationCTID || s.PublisherCTID == request.OrganizationCTID );
                    }
                    */
                    //Handle Keywords Name filter
                    var nameText = request.GetFilterValue( "filter:NameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( nameText ) )
                    {
                        query = query.Where( s => s.Name.ToLower().Contains( nameText ) );
                    }
                    var OrgnameText = request.GetFilterValue( "filter:OrgNameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( OrgnameText ) )
                    {
                        query = query.Where( s => s.Publisher.ToLower().Contains( OrgnameText ) );
                    }

                    //Get total
                    request.TotalRows = query.Count();

                    var results = query.OrderBy( s => s.Organization ).ThenBy( s => s.Name ).ToList();
                    //Check if the data is for summary page
                    if ( request.IsSummary == true )
                    {
                        var result = results.GroupBy( s => s.Publisher ).Select( s => new { Count = s.Count(), OrgName = s.Key, CTID = s.First().PublisherCTID } ).OrderByDescending( s => s.Count ); // gropu by publisher name and get the count before skip and take
                        if ( result != null )
                        {
                            foreach ( var item in result )
                            {
                                entity = new QuerySummary()
                                {
                                    PublisherCTID = item.CTID,
                                    Publisher = item.OrgName,
                                    Id = item.Count //temporarily assigned summary count to this Id instead of creating a new prop
                                };
                                output.Add( entity );
                            }
                            request.TotalRows = result.Count();
                            output = output.Skip( request.Skip ).Take( request.Take ).ToList();
                        }
                    }
                    else //not summary
                    {
                         results = results.Skip( request.Skip ).Take( request.Take ).ToList();
                        if ( results != null && results.Count > 0 )
                        {
                            foreach ( var item in results )
                            {
                                entity = new QuerySummary()
                                {
                                    PublisherCTID = item.PublisherCTID,
                                    Publisher = item.Publisher,
                                    Organization = item.Organization,
                                    DataOwnerCTID = item.DataOwnerCTID,
                                    Name = item.Name,
                                    EntityType = item.CredentialType,
                                    EntityCTID = item.EntityCTID,
                                    EntitySubType = item.CredentialType,
                                    Id = item.Id,
                                    SubjectWebpage = item.SubjectWebpage,
                                    Description = item.Description,
                                    //FinderURL = item.Finder,
                                    LastUpdated = ( DateTime ) item.LastUpdated
                                };
                                output.Add( entity );
                            }
                        }
                    }
                }
            } catch (Exception ex)
            {
                var msg = FormatExceptions( ex );
                LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DuplicateCredentialsNameDescSWP. " + msg ) );
                output.Add( new QuerySummary()
                {
                    Publisher="Error encountered",
                    Organization = ex.Message,
                    Name = msg
                } );
            }
            return output;
        }
        public static List<QuerySummary> DuplicateCredentialsNameSWP( Query request )
        {
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            try
            {
                using ( var context = new ViewContext() )
                {
                    //Start the query
                    var query = context.Reports_DuplicateCredentialsOrgCredentialSWP.Where( s => s.Id > 0 );

					//Determine which type of report to run
					var organizationCTIDs = request.GetFilterValues( "filter:OrganizationCTID", new List<string>() );
					if ( organizationCTIDs?.Count() > 0 )
                    {
                        if ( IsForPublishingRecipients( request ) )
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.PublisherCTID ) );
                        }
                        else
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.DataOwnerCTID ) || organizationCTIDs.Contains( s.PublisherCTID ) );
                        }
                    }

                    /*
                    if ( !string.IsNullOrWhiteSpace( request.PublishingOrganizationCTID ) ) 
                    {
                        query = query.Where( s => s.PublisherCTID == request.PublishingOrganizationCTID );
                    }
                    //if ( !string.IsNullOrWhiteSpace( request.OrganizationCTID ) )
                    //{
                    //    query = query.Where( s => s.DataOwnerCTID == request.OrganizationCTID );
                    //}
                    if ( !string.IsNullOrWhiteSpace( request.OrganizationCTID ) )
                    {
                        query = query.Where( s => s.DataOwnerCTID == request.OrganizationCTID || s.PublisherCTID == request.OrganizationCTID );
                    }
                    */
                    //Handle Keywords Name filter
                    var nameText = request.GetFilterValue( "filter:NameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( nameText ) )
                    {
                        query = query.Where( s => s.Name.ToLower().Contains( nameText ) );
                    }
                    var OrgnameText = request.GetFilterValue( "filter:OrgNameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( OrgnameText ) )
                    {
                        query = query.Where( s => s.Publisher.ToLower().Contains( OrgnameText ) );
                    }
                    //Get total
                    request.TotalRows = query.Count();

                    var results = query.OrderBy( s => s.Organization ).ThenBy( s => s.Name )
                                  .Skip( request.Skip )
                                  .Take( request.Take )
                                  .ToList();
                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( var item in results )
                        {
                            entity = new QuerySummary()
                            {
                                PublisherCTID = item.PublisherCTID,
                                Publisher = item.Publisher,
                                Organization = item.Organization,
                                DataOwnerCTID = item.DataOwnerCTID,
                                Name = item.Name,
                                EntityType = item.CredentialType,
                                EntityCTID = item.EntityCTID,
                                EntitySubType = item.CredentialType,
                                Id = item.Id,
                                SubjectWebpage = item.SubjectWebpage,
                                Description = item.Description,
                                //FinderURL = item.Finder,
                                LastUpdated = ( DateTime ) item.LastUpdated
                            };
                            output.Add( entity );
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                var msg = FormatExceptions( ex );
                LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DuplicateCredentialsNameSWP. " + msg ) );
                output.Add( new QuerySummary()
                {
                    Publisher = "Error encountered",
                    Organization = ex.Message,
                    Name = msg
                } );
            }
            return output;
        }

        public static List<QuerySummary> DuplicateOrgsNameSWP( Query request )
        {
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            try
            {
                using ( var context = new ViewContext() )
                {
                    //Start query
                    var query = context.Reports_DuplicateOrgsNameSWP.Where( s => s.Id > 0 );

					//Determine what kind of query to run
					var organizationCTIDs = request.GetFilterValues( "filter:OrganizationCTID", new List<string>() );
					if ( organizationCTIDs?.Count() > 0 )
                    {
                        if ( IsForPublishingRecipients( request ) )
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.PublisherCTID ) );
                        }
                        else
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.DataOwnerCTID ) ); //Same as OwnerCTID
                        }
                    }
					/*
                    if ( !string.IsNullOrWhiteSpace( request.OwnerCTID ) )
                    {
                        query = query.Where( s => s.DataOwnerCTID == request.OwnerCTID );
                    }
					*/

                    /*
                    if ( !string.IsNullOrWhiteSpace( request.PublishingOrganizationCTID ) )
                    {
                        query = query.Where( s => s.PublisherCTID == request.PublishingOrganizationCTID );
                    }
                    if ( !string.IsNullOrWhiteSpace( request.OrganizationCTID ) )
                    {
                        query = query.Where( s => s.DataOwnerCTID == request.OrganizationCTID );
                    }
                    */
                    //Handle Keywords Name filter
                    var nameText = request.GetFilterValue( "filter:NameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( nameText ) )
                    {
                        query = query.Where( s => s.Organization.ToLower().Contains( nameText ) );
                    }
                    var OrgnameText = request.GetFilterValue( "filter:OrgNameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( OrgnameText ) )
                    {
                        query = query.Where( s => s.Publisher.ToLower().Contains( OrgnameText ) );
                    }
                    //Get total
                    request.TotalRows = query.Count();
                    var results = query.OrderBy( s => s.Publisher ).ThenBy( s => s.Organization ).ToList();
                    //checking for summary
                    if ( request.IsSummary == true )
                    {
                        var result = results.GroupBy( s => s.Publisher ).Select( s => new { Count = s.Count(), OrgName = s.Key, CTID = s.First().PublisherCTID } ).OrderByDescending( s => s.Count );//group by name and get required values before skip and take
                        if ( result != null )
                        {
                            foreach ( var item in result )
                            {
                                entity = new QuerySummary()
                                {
                                    PublisherCTID = item.CTID,
                                    Publisher = item.OrgName,
                                    Id = item.Count// temporarily assigned summary count to Id
                                };
                                output.Add( entity );
                            }
                            request.TotalRows = result.Count();
                            output = output.Skip( request.Skip ).Take( request.Take ).ToList();
                        }
                    }
                    else
                    {
                        results = results.Skip( request.Skip ).Take( request.Take ).ToList();
                        if ( results != null && results.Count > 0 )
                        {
                            foreach ( var item in results )
                            {
                                entity = new QuerySummary()
                                {
                                    PublisherCTID = item.PublisherCTID,
                                    Publisher = item.Publisher,
                                    Organization = item.Organization,
                                    DataOwnerCTID = item.DataOwnerCTID,
                                    EntityType = "ceterms:Organization",
                                    EntitySubType = "Organization",
                                    //include just in case
                                    EntityCTID = item.DataOwnerCTID,
                                    Id = item.Id,
                                    Name = item.Organization,
                                    SubjectWebpage = item.SubjectWebpage,
                                    Description = item.Description,
                                   // FinderURL = item.Finder,
                                    LastUpdated = ( DateTime ) item.LastUpdated
                                };
                                output.Add( entity );
                            }
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                var msg = FormatExceptions( ex );
                LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DuplicateOrgNameSWP. " + msg ) );
                output.Add( new QuerySummary()
                {
                    Publisher = "Error encountered",
                    Organization = ex.Message,
                    Name = msg
                } );
            }
            return output;
        }
        public static List<QuerySummary> DuplicateOrgsNameDescSWP( Query request )
        {
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            try
            {
                using ( var context = new ViewContext() )
                {
                    //Start query
                    var query = context.Reports_DuplicateOrgsName.Where( s => s.Id > 0 );

					//Determine what kind of query to run
					var organizationCTIDs = request.GetFilterValues( "filter:OrganizationCTID", new List<string>() );
					if ( organizationCTIDs?.Count() > 0 )
                    {
                        if ( IsForPublishingRecipients( request ) )
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.PublisherCTID ) );
                        }
                        else
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.DataOwnerCTID ) );
                        }
                    }

                    /*
                    if ( !string.IsNullOrWhiteSpace( request.PublishingOrganizationCTID ) )
                    {
                        query = query.Where( s => s.PublisherCTID == request.PublishingOrganizationCTID );
                    }
                    if ( !string.IsNullOrWhiteSpace( request.OrganizationCTID ) )
                    {
                        query = query.Where( s => s.DataOwnerCTID == request.OrganizationCTID );
                    }
                    */
                    //Handle Keywords Name filter
                    var nameText = request.GetFilterValue( "filter:NameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( nameText ) )
                    {
                        query = query.Where( s => s.Organization.ToLower().Contains( nameText ) );
                    }
                    var OrgnameText = request.GetFilterValue( "filter:OrgNameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( OrgnameText ) )
                    {
                        query = query.Where( s => s.Publisher.ToLower().Contains( OrgnameText ) );
                    }
                    //Get total
                    request.TotalRows = query.Count();
                    var results = query.OrderBy( s => s.Publisher ).ThenBy( s => s.Organization )
                                .Skip( request.Skip )
                                .Take( request.Take )
                                .ToList();

                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( var item in results )
                        {
                            entity = new QuerySummary()
                            {
                                PublisherCTID = item.PublisherCTID,
                                Publisher = item.Publisher,
                                Organization = item.Organization,
                                DataOwnerCTID = item.DataOwnerCTID,
                                EntityType = "ceterms:Organization",
                                EntitySubType = "Organization",
                                //include just in case
                                EntityCTID = item.DataOwnerCTID,
                                Id = item.Id,
                                Name = item.Organization,
                                SubjectWebpage = item.SubjectWebpage,
                                Description = item.Description,
                                //FinderURL = item.Finder,
                                LastUpdated = ( DateTime ) item.LastUpdated
                            };
                            output.Add( entity );
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                var msg = FormatExceptions( ex );
                LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DuplicateOrgNameDescSWP. " + msg ) );
                output.Add( new QuerySummary()
                {
                    Publisher = "Error encountered",
                    Organization = ex.Message,
                    Name = msg
                } );
            }

            return output;
        }

        public static List<QuerySummary> DuplicateLoppsNameDescSWP( Query request )
        {
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();

            try
            {
                using ( var context = new ViewContext() )
                {
                    //Start query
                    var query = context.Reports_DuplicateLoppsNameDescSWP.Where( s => s.Id > 0 );

					//Determine what kind of query to run
					var organizationCTIDs = request.GetFilterValues( "filter:OrganizationCTID", new List<string>() );
					if ( organizationCTIDs?.Count() > 0 )
                    {
                        if ( IsForPublishingRecipients( request ) )
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.PublisherCTID ) );
                        }
                        else
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.DataOwnerCTID ) || organizationCTIDs.Contains( s.PublisherCTID ) ); //Same as OwnerCTID
                        }
                    }
					/*
                    if ( !string.IsNullOrWhiteSpace( request.OwnerCTID ) )
                    {
                        query = query.Where( s => s.DataOwnerCTID == request.OwnerCTID );
                    }
					*/

                    /*
                    if ( !string.IsNullOrWhiteSpace( request.PublishingOrganizationCTID ) )
                    {
                        query = query.Where( s => s.PublisherCTID == request.PublishingOrganizationCTID );
                    }
                    if ( !string.IsNullOrWhiteSpace( request.OrganizationCTID ) )
                    {
                        query = query.Where( s => s.DataOwnerCTID == request.OrganizationCTID || s.PublisherCTID == request.OrganizationCTID );
                    }
                    */
                    //Handle Keywords Name filter
                    var nameText = request.GetFilterValue( "filter:NameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( nameText ) )
                    {
                        query = query.Where( s => s.Name.ToLower().Contains( nameText ) );
                    }
                    var OrgnameText = request.GetFilterValue( "filter:OrgNameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( OrgnameText ) )
                    {
                        query = query.Where( s => s.Publisher.ToLower().Contains( OrgnameText ) );
                    }
                    //Get total
                    request.TotalRows = query.Count();
                    var results = query.OrderBy( s => s.Organization ).ThenBy( s => s.Name ).ToList();
                    //Check if the data is for summary page
                    if ( request.IsSummary == true )
                    {
                        var result = results.GroupBy( s => s.Publisher ).Select( s => new { Count = s.Count(), OrgName = s.Key, CTID = s.First().PublisherCTID } ).OrderByDescending( s => s.Count ); // group by publisher name and get the count before skip and take
                        if ( result != null )
                        {
                            foreach ( var item in result )
                            {
                                entity = new QuerySummary()
                                {
                                    PublisherCTID = item.CTID,
                                    Publisher = item.OrgName,
                                    Id = item.Count //temporarily assigned summary count to this Id instead of creating a new prop
                                };
                                output.Add( entity );
                            }
                            request.TotalRows = result.Count();
                            output = output.Skip( request.Skip ).Take( request.Take ).ToList();
                        }
                    }
                    else //not summary
                    {
                        results = results.Skip( request.Skip ).Take( request.Take ).ToList();

                        if ( results != null && results.Count > 0 )
                        {
                            foreach ( var item in results )
                            {
                                entity = new QuerySummary()
                                {
                                    PublisherCTID = item.PublisherCTID,
                                    Publisher = item.Publisher,
                                    Organization = item.Organization,
                                    DataOwnerCTID = item.DataOwnerCTID,
                                    EntityType = "ceterms:LearningOppotunityProfile",
                                    EntitySubType = item.LearningType,
                                    Name = item.Name,
                                    EntityCTID = item.EntityCTID,
                                    Id = item.Id,
                                    SubjectWebpage = item.SubjectWebpage,
                                    Description = item.Description,
                                   // FinderURL = item.Finder,
                                    LastUpdated = ( DateTime ) item.LastUpdated
                                };
                                output.Add( entity );
                            }
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                var msg = FormatExceptions( ex );
                LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DuplicateLoppsNameDescSWP. " + msg ) );
                output.Add( new QuerySummary()
                {
                    Publisher = "Error encountered",
                    Organization = ex.Message,
                    Name = msg
                } );
            }

            return output;
        }
        public static List<QuerySummary> DuplicateLoppsNameSWP( Query request )
        {
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            try
            {
                using ( var context = new ViewContext() )
                {
                    //Start query
                    var query = context.Reports_DuplicateLoppsNameSWP.Where( s => s.Id > 0 );

					//Determine what kind of query to run
					var organizationCTIDs = request.GetFilterValues( "filter:OrganizationCTID", new List<string>() );
					if ( organizationCTIDs?.Count() > 0 )
                    {
                        if ( IsForPublishingRecipients( request ) )
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.PublisherCTID ) );
                        }
                        else
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.DataOwnerCTID ) || organizationCTIDs.Contains( s.PublisherCTID ) );
                        }
                    }

                    /*
                    if ( !string.IsNullOrWhiteSpace( request.PublishingOrganizationCTID ) )
                    {
                        query = query.Where( s => s.PublisherCTID == request.PublishingOrganizationCTID );
                    }
                    if ( !string.IsNullOrWhiteSpace( request.OrganizationCTID ) )
                    {
                        query = query.Where( s => s.DataOwnerCTID == request.OrganizationCTID || s.PublisherCTID == request.OrganizationCTID );
                    }
                    */
                    //Handle Keywords Name filter
                    var nameText = request.GetFilterValue( "filter:NameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( nameText ) )
                    {
                        query = query.Where( s => s.Name.ToLower().Contains( nameText ) );
                    }
                    var OrgnameText = request.GetFilterValue( "filter:OrgNameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( OrgnameText ) )
                    {
                        query = query.Where( s => s.Publisher.ToLower().Contains( OrgnameText ) );
                    }
                    //Get total
                    request.TotalRows = query.Count();
                    var results = query.OrderBy( s => s.Organization ).ThenBy( s => s.Name )
                                .Skip( request.Skip )
                                .Take( request.Take )
                                .ToList();

                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( var item in results )
                        {
                            entity = new QuerySummary()
                            {
                                PublisherCTID = item.PublisherCTID,
                                Publisher = item.Publisher,
                                Organization = item.Organization,
                                DataOwnerCTID = item.DataOwnerCTID,
                                EntityType = "ceterms:LearningOppotunityProfile",
                                EntitySubType = item.LearningType,
                                Name = item.Name,
                                EntityCTID = item.EntityCTID,
                                Id = item.Id,
                                SubjectWebpage = item.SubjectWebpage,
                                Description = item.Description,
                                //FinderURL = item.Finder,
                                LastUpdated = ( DateTime ) item.LastUpdated
                            };
                            output.Add( entity );
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                var msg = FormatExceptions( ex );
                LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DuplicateLoppsNameSWP. " + msg ) );
                output.Add( new QuerySummary()
                {
                    Publisher = "Error encountered",
                    Organization = ex.Message,
                    Name = msg
                } );
            }
            return output;
        }

        public static List<QuerySummary> DuplicateAsmtsNameDescSWP( Query request )
        {
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            try
            {
                using ( var context = new ViewContext() )
                {
                    //Start query
                    var query = context.Reports_DuplicateAsmtsNameDescSWP.Where( s => s.Id > 0 );

					//Determine what kind of query to run
					var organizationCTIDs = request.GetFilterValues( "filter:OrganizationCTID", new List<string>() );
					if ( organizationCTIDs?.Count() > 0 )
                    {
                        if ( IsForPublishingRecipients( request ) )
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.PublisherCTID ) );
                        }
                        else
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.DataOwnerCTID ) || organizationCTIDs.Contains( s.PublisherCTID ) ); //Same as OwnerCTID
                        }
                    }
					/*
                    if ( !string.IsNullOrWhiteSpace( request.OwnerCTID ) )
                    {
                        query = query.Where( s => s.DataOwnerCTID == request.OwnerCTID );
                    }
					*/

                    /*
                    if ( !string.IsNullOrWhiteSpace( request.PublishingOrganizationCTID ) )
                    {
                        query = query.Where( s => s.PublisherCTID == request.PublishingOrganizationCTID );
                    }
                    if ( !string.IsNullOrWhiteSpace( request.OrganizationCTID ) )
                    {
                        query = query.Where( s => s.DataOwnerCTID == request.OrganizationCTID || s.PublisherCTID == request.OrganizationCTID );
                    }
                    */
                    //Handle Keywords Name filter
                    var nameText = request.GetFilterValue( "filter:NameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( nameText ) )
                    {
                        query = query.Where( s => s.Name.ToLower().Contains( nameText ) );
                    }
                    var OrgnameText = request.GetFilterValue( "filter:OrgNameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( OrgnameText ) )
                    {
                        query = query.Where( s => s.Publisher.ToLower().Contains( OrgnameText ) );
                    }
                    //Get total
                    request.TotalRows = query.Count();
                    var results = query.OrderBy( s => s.Organization ).ThenBy( s => s.Name ).ToList();
                    //Check if the data is for summary page
                    if ( request.IsSummary == true )
                    {
                        var result = results.GroupBy( s => s.Publisher ).Select( s => new { Count = s.Count(), OrgName = s.Key, CTID = s.First().PublisherCTID } ).OrderByDescending( s => s.Count ); // group by publisher name and get the count before skip and take
                        if ( result != null )
                        {
                            foreach ( var item in result )
                            {
                                entity = new QuerySummary()
                                {
                                    PublisherCTID = item.CTID,
                                    Publisher = item.OrgName,
                                    Id = item.Count //temporarily assigned summary count to this Id instead of creating a new prop
                                };
                                output.Add( entity );
                            }
                            request.TotalRows = result.Count();
                            output = output.Skip( request.Skip ).Take( request.Take ).ToList();
                        }
                    }
                    else //not summary
                    {
                        results = results.Skip( request.Skip ).Take( request.Take ).ToList();

                        if ( results != null && results.Count > 0 )
                        {
                            foreach ( var item in results )
                            {
                                entity = new QuerySummary()
                                {
                                    PublisherCTID = item.PublisherCTID,
                                    Publisher = item.Publisher,
                                    Organization = item.Organization,
                                    DataOwnerCTID = item.DataOwnerCTID,
                                    EntityType = "ceterms:AssessmentProfile",
                                    EntitySubType = "Assessment",
                                    Name = item.Name,
                                    EntityCTID = item.EntityCTID,
                                    Id = item.Id,
                                    SubjectWebpage = item.SubjectWebpage,
                                    Description = item.Description,
                                    //FinderURL = item.Finder,
                                    LastUpdated = ( DateTime ) item.LastUpdated
                                };
                                output.Add( entity );
                            }
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                var msg = FormatExceptions( ex );
                LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DuplicateAsmtsNameDescSWP. " + msg ) );
                output.Add( new QuerySummary()
                {
                    Publisher = "Error encountered",
                    Organization = ex.Message,
                    Name = msg
                } );
            }

            return output;
        }
        public static List<QuerySummary> DuplicateAsmtsNameSWP( Query request )
        {
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            try
            {
                using ( var context = new ViewContext() )
                {
                    //Start query
                    var query = context.Reports_DuplicateAsmtsNameSWP.Where( s => s.Id > 0 );

					//Determine what kind of query to run
					var organizationCTIDs = request.GetFilterValues( "filter:OrganizationCTID", new List<string>() );
					if ( organizationCTIDs?.Count() > 0 )
                    {
                        if ( IsForPublishingRecipients( request ) )
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.PublisherCTID ) );
                        }
                        else
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.DataOwnerCTID ) || organizationCTIDs.Contains( s.PublisherCTID ) );
                        }
                    }

                    /*
                    if ( !string.IsNullOrWhiteSpace( request.PublishingOrganizationCTID ) )
                    {
                        query = query.Where( s => s.PublisherCTID == request.PublishingOrganizationCTID );
                    }
                    if ( !string.IsNullOrWhiteSpace( request.OrganizationCTID ) )
                    {
                        query = query.Where( s => s.DataOwnerCTID == request.OrganizationCTID || s.PublisherCTID == request.OrganizationCTID );
                    }
                    */
                    //Handle Keywords Name filter
                    var nameText = request.GetFilterValue( "filter:NameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( nameText ) )
                    {
                        query = query.Where( s => s.Name.ToLower().Contains( nameText ) );
                    }
                    var OrgnameText = request.GetFilterValue( "filter:OrgNameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( OrgnameText ) )
                    {
                        query = query.Where( s => s.Publisher.ToLower().Contains( OrgnameText ) );
                    }
                    //Get total
                    request.TotalRows = query.Count();
                    var results = query.OrderBy( s => s.Organization ).ThenBy( s => s.Name )
                                .Skip( request.Skip )
                                .Take( request.Take )
                                .ToList();

                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( var item in results )
                        {
                            entity = new QuerySummary()
                            {
                                PublisherCTID = item.PublisherCTID,
                                Publisher = item.Publisher,
                                Organization = item.Organization,
                                DataOwnerCTID = item.DataOwnerCTID,
                                EntityType = "ceterms:AssessmentProfile",
                                EntitySubType = "Assessment",
                                Name = item.Name,
                                EntityCTID = item.EntityCTID,
                                Id = item.Id,
                                SubjectWebpage = item.SubjectWebpage,
                                Description = item.Description,
                               // FinderURL = item.Finder,
                                LastUpdated = ( DateTime ) item.LastUpdated
                            };
                            output.Add( entity );
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                var msg = FormatExceptions( ex );
                LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DuplicateAsmtsNameSWP. " + msg ) );
                output.Add( new QuerySummary()
                {
                    Publisher = "Error encountered",
                    Organization = ex.Message,
                    Name = msg
                } );
            }
            return output;
        }
        #endregion
        #endregion

        #region Currency
        /// <summary>
        /// Currency queries
        /// NOTES:
        /// - need to consider credential and life cycle status
        /// </summary>
        /// <param name="request"></param>
        /// <param name="publisherCTID"></param>
        /// <param name="dataOwnerCTID"></param>
        /// <returns></returns>
        public static List<QuerySummary> CurrencyQuery( Query request )
        {
            //get age range. Use months
            var range = UtilityManager.GetAppKeyValue( "currencyMonthsRange", 18);
            var checkDate = DateTime.Now.AddMonths( range * -1 );
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            try
            {
                using ( var context = new EntityContext() )
                {
                    var query = ( from record in context.Entity_Cache
                                  join org in context.Organization on record.OwningOrgId equals org.Id
                                  where record.parentEntityId == 0
                                        && org.EntityStateId > 1 && record.EntityStateId == 3
                                        && record.LastUpdated < checkDate
                                  select new
                                  {
                                      record.CTID,
                                      record.BaseId,
                                      record.EntityType,
                                      record.EntityTypeId,
                                      record.EntityUid,
                                      record.Name,
                                      //TBD
                                      Organization = org.Name,
                                      OrganizationCTID = org.CTID,
                                      record.OwningOrgId,
                                      record.LastUpdated,
                                  } );

					//var query = context.Entity_Cache.Where( s => s.Id > 0 );
					//if ( !string.IsNullOrWhiteSpace( publisherCTID ) )
					//{
					//    query = query.Where( s => s.PublisherCTID == publisherCTID );
					//}
					var organizationCTIDs = request.GetFilterValues( "filter:OrganizationCTID", new List<string>() );
					if ( organizationCTIDs?.Count() > 0 )
                    {
                        query = query.Where( s => organizationCTIDs.Contains( s.OrganizationCTID ) );
                    }
                    //if ( !string.IsNullOrWhiteSpace( request.EntityType ) )
                    //{
                    //    query = query.Where( s => s.EntityType == request.EntityType );
                    //}
                    //
                    if ( request.Filters?.Count > 0 )
                    {
                        foreach ( var item in request.Filters )
                        {
                            if ( item.FilterURI == "filter:ResourceType" )
                            {
                                if ( item.Values[0] == "" )
                                    continue;
                                if ( IsInteger( item.Values[0] ) )
                                {
                                    query = query.Where( s => s.EntityTypeId == int.Parse( item.Values[0] ) );
                                }
                                else
                                {
                                    query = query.Where( s => s.EntityType == item.Values[0] );
                                }

                            }
                            else if ( item.FilterURI == "filter:NameText" )
                            {
                                //just handle one for now
                                var keyword = item.Values[0].ToLower();
                                if ( !string.IsNullOrWhiteSpace( keyword ) )
                                    query = query.Where( s => s.Name.ToLower().Contains( keyword ) );
                            }
                        }
                    }

                    //need to get the total to return

                    request.TotalRows = query.Count();
                    //var results = query.OrderBy( s => s.Publisher ).ThenBy( s => s.Organization ).ThenBy( s => s.Name )
                    //    .ToList();
                    var results = query
                                    .OrderBy( s => s.Organization ).ThenBy( s => s.EntityType ).ThenBy( s => s.Name )
                                    .Skip( request.Skip )
                                    .Take( request.Take )
                                    .ToList();

                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( var item in results )
                        {
                            entity = new QuerySummary()
                            {
                                PublisherCTID = item.OrganizationCTID,
                                Publisher = item.Organization,

                                Organization = item.Organization,
                                DataOwnerCTID = item.OrganizationCTID,
                                Name = item.Name,
                                EntityCTID = item.CTID,
                                EntityType = item.EntityType,
                                Id = item.BaseId,
                                LastUpdated = ( DateTime ) item.LastUpdated
                            };
                            output.Add( entity );
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                var msg = FormatExceptions( ex );
                LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".CurrencyQuery. " + msg ) );
                output.Add( new QuerySummary()
                {
                    Publisher = "Error encountered",
                    Organization = ex.Message,
                    Name = msg
                } );
            }
            return output;
        }

        public static List<QuerySummary> CurrencyQueryView( Query request )
        {
            //get age range. Use months
            var range = UtilityManager.GetAppKeyValue( "currencyMonthsRange", 18 );
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            using ( var context = new ViewContext() )
            {
				//Start the query
                var query = context.ResourceCurrency_Summary.Where( s => s.BaseId > 0 );

				//Handle date range
				var lastUpdatedAfterMonths = request.GetFilterIntValue( "filter:LastUpdatedAfterMonths", range );
				var checkDate = DateTime.Now.AddMonths( lastUpdatedAfterMonths * -1 );
				query = query.Where( s => s.LastUpdated < checkDate );

				//Determine which type of report to run
				var organizationCTIDs = request.GetFilterValues( "filter:OrganizationCTID", new List<string>() );
				if ( organizationCTIDs?.Count() > 0 )
				{
					if ( IsForPublishingRecipients( request ) )
					{
						query = query.Where( s => organizationCTIDs.Contains( s.PublisherCTID ) );
					}
					else
					{
						query = query.Where( s => organizationCTIDs.Contains( s.OrganizationCTID ) /*|| s.PublisherCTID == request.OrganizationCTID */); //Same as OwnerCTID
					}
				}
				/*
                if ( !string.IsNullOrWhiteSpace( request.OwnerCTID ) )
                {
                   query = query.Where( s => s.OrganizationCTID == request.OwnerCTID );
                }
				*/

                //Handle Resource Type filter
                var entityTypeID = request.GetFilterIntValue( "filter:ResourceType" );
				var entityTypeText = request.GetFilterValue( "filter:ResourceType" );
				if ( entityTypeID > 0 )
				{
					query = query.Where( s => s.EntityTypeId == entityTypeID );
				}
				else if(!string.IsNullOrWhiteSpace(entityTypeText) )
				{
					query = query.Where( s => s.EntityType == entityTypeText );
				}
                //Handle Keywords filter
                var nameText = request.GetFilterValue( "filter:NameText" )?.ToLower();
				if ( !string.IsNullOrWhiteSpace( nameText ) )
				{
					query = query.Where( s => s.Name.ToLower().Contains( nameText ) );
				}
                var orgNameText = request.GetFilterValue( "filter:OrgNameText" )?.ToLower();
                if ( !string.IsNullOrWhiteSpace( orgNameText ) )
                {
                    query = query.Where( s => s.Publisher.ToLower().Contains( orgNameText ) );
                }
              
                /*
				if ( !string.IsNullOrWhiteSpace( request.PublishingOrganizationCTID ) )
                {
                    query = query.Where( s => s.PublisherCTID == request.PublishingOrganizationCTID );
                }
                if ( !string.IsNullOrWhiteSpace( request.OrganizationCTID ) )
                {
                    query = query.Where( s => s.OrganizationCTID == request.OrganizationCTID || s.PublisherCTID == request.OrganizationCTID );
                }
                //if ( !string.IsNullOrWhiteSpace( request.EntityType ) )
                //{
                //    query = query.Where( s => s.EntityType == request.EntityType );
                //}
                //
                if ( request.Filters?.Count > 0 )
                {
                    foreach ( var item in request.Filters )
                    {
                        if ( item.FilterURI == "filter:ResourceType" )
                        {
                            if ( item.Values[0] == "" )
                                continue;
                            if ( IsInteger( item.Values[0] ) )
                            {
                                var entityTypeId = int.Parse( item.Values[0] );
                                query = query.Where( s => s.EntityTypeId == entityTypeId );
                            }
                            else
                            {
                                var entityType = item.Values[0];
                                query = query.Where( s => s.EntityType == entityType );
                            }

                        }
                        else if ( item.FilterURI == "filter:NameText" )
                        {
                            //just handle one for now
                            var keyword = item.Values[0].ToLower();
                            if ( !string.IsNullOrWhiteSpace( keyword ) )
                                query = query.Where( s => s.Name.ToLower().Contains( keyword ) );
                        }
                    }
                }
				*/

                try
                {
                    //need to get the total to return

                    request.TotalRows = query.Count();
                    var results = query.OrderBy( s => s.Publisher ).ThenBy( s => s.Organization ).ThenBy( s => s.Name ).Skip( request.Skip ).Take( request.Take ).ToList();
                        if ( results != null && results.Count > 0 )
                        {
                            foreach ( var item in results )
                            {
                                entity = new QuerySummary()
                                {
                                    PublisherCTID = item.PublisherCTID,
                                    Publisher = item.Publisher,

                                    Organization = item.Organization,
                                    DataOwnerCTID = item.OrganizationCTID,
                                    Name = item.Name,
                                    EntityCTID = item.CTID,
                                    EntityType = item.EntityType,
                                    Id = item.BaseId,
                                    LastUpdated = ( DateTime ) item.LastUpdated,
                                    IsInPublisher=Convert.ToBoolean(item.IsInPublisher)
                                };
                                output.Add( entity );
                            }
                        }
                }catch (Exception ex)
                {
                    var msg = FormatExceptions( ex );
                    LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".CurrencyQueryView. " + msg ) );
                    output.Add( new QuerySummary()
                    {
                        Publisher = "Error encountered",
                        Organization = ex.Message,
                        Name = msg
                    } );
                }
            }

            return output;
        }

        public static List<QuerySummary> CurrencySummary( Query request )
        {
            //get age range. Use months
            var range = UtilityManager.GetAppKeyValue( "currencyMonthsRange", 18 );
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            using ( var context = new ViewContext() )
            {
                //Start the query
                var query = context.ResourceCurrency_Summary.Where( s => s.BaseId > 0 );

                //Handle date range
                var lastUpdatedAfterMonths = request.GetFilterIntValue( "filter:LastUpdatedAfterMonths", range );
                var checkDate = DateTime.Now.AddMonths( lastUpdatedAfterMonths * -1 );
                query = query.Where( s => s.LastUpdated < checkDate );

				//Determine which type of report to run
				var organizationCTIDs = request.GetFilterValues( "filter:OrganizationCTID", new List<string>() );
				if ( organizationCTIDs?.Count() > 0 )
                {
                    if ( IsForPublishingRecipients( request ) )
                    {
                        query = query.Where( s => organizationCTIDs.Contains( s.PublisherCTID ) );
                    }
                    else
                    {
                        query = query.Where( s => organizationCTIDs.Contains( s.OrganizationCTID ) /*|| s.PublisherCTID == request.OrganizationCTID*/ ); //Same as OwnerCTID
                    }
                }
				/*
                if ( !string.IsNullOrWhiteSpace( request.OwnerCTID ) )
                {
                    query = query.Where( s => s.OrganizationCTID == request.OwnerCTID );
                }
				*/
                
                //Handle Keywords filter
               
                var orgNameText = request.GetFilterValue( "filter:OrgNameText" )?.ToLower();
                if ( !string.IsNullOrWhiteSpace( orgNameText ) )
                {
                    query = query.Where( s => s.Publisher.ToLower().Contains( orgNameText ) );
                }


                try
                {
                    var results = query.OrderBy( s => s.Publisher ).ThenBy( s => s.Organization ).ThenBy( s => s.Name ).ToList();

                    if ( IsForPublishingRecipients( request ) )
                    {
                        //groupby publisher name and get the count
                        var result = results.GroupBy( s => s.Publisher ).Select( s => new { Count = s.Count(), OrgName = s.Key, CTID = s.First().PublisherCTID } ).OrderByDescending( s => s.Count );//group by publisher
                        request.TotalRows = result.Count();
                        if ( result != null )
                        {
                            foreach ( var item in result )
                            {
                                entity = new QuerySummary()
                                {
                                    PublisherCTID = item.CTID,
                                    Publisher = item.OrgName,
                                    TotalItems = item.Count
                                };
                                output.Add( entity );
                            }
                        }
                    }
                    else
                    {
                        //groupby owner name and get the count
                        var result = results.GroupBy( s => s.Organization ).Select( s => new { Count = s.Count(), OrgName = s.Key, CTID = s.First().OrganizationCTID } ).OrderByDescending( s => s.Count );
                        request.TotalRows = result.Count();
                        if ( result != null )
                        {
                            foreach ( var item in result )
                            {
                                entity = new QuerySummary()
                                {
                                    PublisherCTID = item.CTID,
                                    Publisher = item.OrgName,
                                    TotalItems = item.Count
                                };
                                output.Add( entity );
                            }
                        }
                    }
                    output = output.OrderBy( s => s.Publisher ).Skip( request.Skip ).Take( request.Take ).ToList();



                }
                catch ( Exception ex )
                {
                    var msg = FormatExceptions( ex );
                    LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".CurrencyQueryView. " + msg ) );
                    output.Add( new QuerySummary()
                    {
                        Publisher = "Error encountered",
                        Organization = ex.Message,
                        Name = msg
                    } );
                }
            }

            return output;
        }


        public static List<QuerySummary> CurrencyCandidatesQuery( Query request )
        {
            //get age range. Use months
            var range = UtilityManager.GetAppKeyValue( "currencyMonthsRange", 18 );
            var checkDate = DateTime.Now.AddMonths( range * -1 );
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            //may allow other filters to get a single summary
            
            using ( var context = new ViewContext() )
            {
                var sql = string.Format("SELECT [Publisher],[PublisherCTID]	,[Organization]	,[OrganizationCTID]	,count(*) as Total FROM [dbo].[ResourceCurrency_Summary]  Where LastUpdated < '{0}' group by   [Publisher]	,[PublisherCTID]	,[Organization]	,[OrganizationCTID]", checkDate);

                try
                {

                    var query = context.ResourceCurrency_Summary.SqlQuery( sql ).ToList();
                    //need to get the total to return

                    request.TotalRows = query.Count();
                    if ( query != null && query.Count > 0 )
                    {
                        foreach ( var item in query )
                        {
                            entity = new QuerySummary()
                            {
                                PublisherCTID = item.PublisherCTID,
                                Publisher = item.Publisher,

                                Organization = item.Organization,
                                DataOwnerCTID = item.OrganizationCTID,
                                
                                //should return the count, would need to be added to the view
                            };
                            output.Add( entity );
                        }
                    }
                }
                catch ( Exception ex )
                {
                    var msg = FormatExceptions( ex );
                    LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".CurrencyQueryView. " + msg ) );
                    output.Add( new QuerySummary()
                    {
                        Name = ex.Message
                    } );
                }
            }

            return output;
        }

        #endregion

        #region LinkChecker
        public static List<QuerySummary> LinkcheckerSearch(Query request )
        {
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            var filterType = "";
            int pTotalRow = 0;
             request.OrderBy= "PublisherName,OrganizationName,EntityName";

			//Determine which type of report to run
			var organizationCTIDs = request.GetFilterValues( "filter:OrganizationCTID", new List<string>() );
			if ( organizationCTIDs?.Count() > 0 )
            {
                if ( IsForPublishingRecipients( request ) )
                {
                    //filterType = "(  PublisherCTID='" + organizationCTID + "' )";
					filterType = "( PublisherCTID IN (" + string.Join( ",", organizationCTIDs.Select( m => "'" + m + "'" ).ToList() ) + ") )";
                }
                else
                {
                    //filterType = "(  OrganizationCTID='" + organizationCTID + "' )";
					filterType = "( OrganizationCTID IN (" + string.Join( ",", organizationCTIDs.Select( m => "'" + m + "'" ).ToList() ) + ") )";
				}
            }

            //Handle the skip consistent with the linkchecker
            request.Skip =( request.Skip ) / 100 +1;

            //Handle EntityType
            var entityTypeId = request.GetFilterValue( "filter:ResourceType" )?.ToLower();
            if ( !string.IsNullOrWhiteSpace( entityTypeId ) )
            {
                entityTypeId = entityTypeId == "credential" ? "1" : entityTypeId;
                filterType = filterType == "" ? "( EntityTypeId = " + Convert.ToInt32( entityTypeId ) + " )" : filterType + "AND ( EntityTypeId = " + Convert.ToInt32( entityTypeId ) + ")";
            }
            //Handle LinkType
            var linkType= request.GetFilterValue( "filter:LinkType" )?.ToLower();
            if ( !string.IsNullOrWhiteSpace( linkType ) && linkType != "any" )
            {
                int type = linkType == "registry" ? 1 : 0;
                filterType = filterType == "" ? "( IsRegistryUrl =" + type + ")" : filterType+"AND ( IsRegistryUrl =" + type + ")";
            }

            //Handle Name Filter
            var nameText = request.GetFilterValue( "filter:NameText" );
            if ( !string.IsNullOrWhiteSpace( nameText ) )
            {
                filterType = filterType == "" ? "( EntityName LIKE '%" + nameText + "%' )" : filterType + "AND ( EntityName LIKE '%" + nameText + "%' )";
            }

            //Handle Status Filter
            var statusType = request.GetFilterValue( "filter:StatusType" );
            if ( !string.IsNullOrWhiteSpace( statusType ) && statusType != "All" )
            {
                filterType = filterType == "" ? "( statusSummary LIKE '%" + statusType + "%' )" : filterType + "AND ( statusSummary LIKE '%" + statusType + "%' )";
            }
            var results = LinkCheckerServices.LinkCheckerServices.Search( filterType, request.OrderBy, request.Skip, request.Take, ref pTotalRow );
          
            request.TotalRows = pTotalRow;
            if ( results != null && results.Count > 0 )
            {
                foreach ( var item in results )
                {
                    entity = new QuerySummary()
                    {
                        Publisher = item.PublisherName,
                        PublisherCTID = item.PublisherCTID,
                        Organization = item.OrganizationName,
                        DataOwnerCTID = item.OrganizationCTID,
                        Name = item.EntityName,
                        EntityCTID = item.EntityCTID,
                        EntityType = item.EntityType,
                        EntitySubType = char.ToUpper(item.EntityType[0] )+item.EntityType.Substring( 1 ),
                        Id = item.Id,
                        FinderURL = item.FinderUrl,
                        LastUpdated = ( DateTime ) item.EntityLastUpdated,
                        BadURL=item.URL,
                        Status=item.Status+"("+item.StatusCode+")",
                        LinkType= item.IsRegistryUrl==true? "Registry": "Content",
                        Property=item.Property,
                        IsInPublisher= item.IsInPublisher
                    };
                    output.Add( entity );
                }
            }
            return output;
        }

        public static List<QuerySummary> LinkCheckerSummary(Query request )
        {
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            var filterType = "";
            int pTotalRow = 0;
            var results= new List<LinkCheckerServices.Models.OrganizationTotalsSummary>();
            var  OrgNameText = request.GetFilterValue( "filter:OrgNameText" );

            // Handling Orderby consistent with the linkchecker
            if ( IsForPublishingRecipients( request ) )
            {
                request.OrderBy = "PublisherName";
                if ( !string.IsNullOrWhiteSpace( OrgNameText ) )
                {
                    filterType = "( PublisherName LIKE '%" + OrgNameText + "%' )";
                }
            }
            else
            {
                request.OrderBy = "OrganizationName";
                // Handle the Organization Name filter
                if ( !string.IsNullOrWhiteSpace( OrgNameText ) )
                {
                    filterType = "( OrganizationName LIKE '%" + OrgNameText + "%' )";
                }
            }
            //Handle the skip consistent with the linkchecker
            request.Skip = ( request.Skip ) / 100 + 1;

			//Determine which type of report to run
			var organizationCTIDs = request.GetFilterValues( "filter:OrganizationCTID", new List<string>() );
			if ( organizationCTIDs?.Count() > 0 )
             {
				if ( IsForPublishingRecipients( request ) )
				{
					var joinString = "( PublisherCTID IN (" + string.Join( ",", organizationCTIDs.Select( m => "'" + m + "'" ).ToList() ) + ") )";
					filterType = filterType == "" ? joinString : filterType + "AND " + joinString;
					//filterType = filterType == "" ? "(  PublisherCTID='" + organizationCTID + "' )" : filterType + "AND  ( PublisherCTID = '" + organizationCTID + "' )";
				}
				else
				{
					var joinString = "( OrganizationCTID IN (" + string.Join( ",", organizationCTIDs.Select( m => "'" + m + "'" ).ToList() ) + ") )";
					filterType = filterType == "" ? joinString : filterType + "AND " + joinString;
					//filterType = filterType == "" ? "(  OrganizationCTID='" + organizationCTID + "' )" : filterType + "AND  ( OrganizationCTID = '" + organizationCTID + "' )";
				}
			}

            if ( IsForPublishingRecipients( request ) )
            {
                //Getting the publisher data from Linkchecker Services
                results = LinkCheckerServices.LinkCheckerServices.PublishingOrganizationsWithBadLinks( filterType, request.OrderBy, request.Skip, request.Take, ref pTotalRow );
            }
            else
            {
                //Getting the Org data from Linkchecker Services
                results = LinkCheckerServices.LinkCheckerServices.OrganizationsWithBadLinks( filterType, request.OrderBy, request.Skip, request.Take, ref pTotalRow );
            }
            
            

            request.TotalRows = pTotalRow;
            if ( results != null && results.Count > 0 )
            {
                foreach ( var item in results )
                {
                    entity = new QuerySummary()
                    {
                        Organization = item.OrganizationName,
                        DataOwnerCTID = item.OrganizationCTID,
                        URIIssues=item.URIIssuesTotal,
                        URLIssues=item.URLIssuesTotal,
                        TotalItems=item.URIIssuesTotal+item.URLIssuesTotal
                    };
                    output.Add( entity );
                }
            }
            return output;
        }
        #endregion

        #region CredentialType
        public static List<QuerySummary> CredentialType( Query request )
        {
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            try
            {
                using ( var context = new ViewContext() )
                {
                    //Start the query
                    var query = context.Reports_CredentialType.Where(s=>s.CredentialTypeId>0);

					//Determine which type of report to run
					var organizationCTIDs = request.GetFilterValues( "filter:OrganizationCTID", new List<string>() );
					if ( organizationCTIDs?.Count() > 0 )
                    {
                        if ( IsForPublishingRecipients( request ) )
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.PublisherCTID ) );
                        }
                        else
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.DataOwnerCTID ) /*|| organizationCTIDs.Contains( s.PublisherCTID ) */); //Same as OwnerCTID
                        }
                    }
					/*
                    if ( !string.IsNullOrWhiteSpace( request.OwnerCTID ) )
                    {
                        query = query.Where( s => s.DataOwnerCTID == request.OwnerCTID );
                    }
					*/

                    //handle entity type
                    var credentialTypeId = request.GetFilterIntValue( "filter:ResourceType" );
                    if ( credentialTypeId > 0)
                    {
                        query = query.Where( s => s.CredentialTypeId == credentialTypeId );
                    }

                    //Handle Keywords Name filter
                    var nameText = request.GetFilterValue( "filter:NameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( nameText ) )
                    {
                        query = query.Where( s => s.Name.ToLower().Contains( nameText ) );
                    }

                    //Get total
                    request.TotalRows = query.Count();

                    var results = query.OrderBy( s => s.Publisher ).ThenBy( s => s.DataOwner ).ThenBy( s => s.Name ).Skip( request.Skip ).Take( request.Take ).ToList();
                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( var item in results )
                        {
                            entity = new QuerySummary()
                            {
                                PublisherCTID = item.PublisherCTID,
                                Publisher = item.Publisher != null ? item.Publisher:"",
                                Organization = item.DataOwner,
                                DataOwnerCTID = item.DataOwnerCTID,
                                Name = item.Name,
                                EntityCTID = item.CTID,
                                EntityType = item.CredentialType,
                                Description = item.Description,
                                SubjectWebpage=item.SubjectWebpage,
                               // FinderURL = item.FinderURL,
                                LastUpdated = ( DateTime ) item.LastUpdated
                            };
                            output.Add( entity );
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                var msg = FormatExceptions( ex );
                LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".CredentialTypeQuery. " + msg ) );
                output.Add( new QuerySummary()
                {
                    Publisher = "Error encountered",
                    Organization = ex.Message,
                    Name = msg
                } );
            }
            return output;
        }

        #endregion

        #region DataQuality
        public static List<QuerySummary> OrgsWithoutOwnsOffers( Query request )
        {
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            try
            {
                using ( var context = new ViewContext() )
                {
                    //Start the query
                    var query = context.Reports_CredentialOrgsWithoutOwnsOrOffers.Where( s => s.Id > 0 );

                    //Determine which type of report to run
                    var organizationCTIDs = request.GetFilterValues( "filter:OrganizationCTID", new List<string>() );
                    if ( organizationCTIDs?.Count() > 0 )
                    {
                        if ( IsForPublishingRecipients( request ) )
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.PublisherCTID ) );
                        }
                        else
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.CTID ) || organizationCTIDs.Contains( s.PublisherCTID ) ); //Same as OwnerCTID
                        }
                    }
                    /*
                    if ( !string.IsNullOrWhiteSpace( request.OwnerCTID ) )
                    {
                        query = query.Where( s => s.DataOwnerCTID == request.OwnerCTID );
                    }
					*/

                    //Handle Keywords Name filter
                    var nameText = request.GetFilterValue( "filter:NameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( nameText ) )
                    {
                        query = query.Where( s => s.Name.ToLower().Contains( nameText ) );
                    }

                    //Get total
                    request.TotalRows = query.Count();

                    var results = query.OrderBy( s => s.Publisher ).ThenBy( s => s.Name ).Skip( request.Skip ).Take( request.Take ).ToList();
                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( var item in results )
                        {
                            entity = new QuerySummary()
                            {
                                PublisherCTID = item.PublisherCTID,
                                Publisher = item.Publisher != null ? item.Publisher : "",
                                Organization = item.Name,
                                DataOwnerCTID = item.CTID,
                                Name = item.Name,
                                EntityCTID = item.CTID,
                                Description = item.Description,
                                SubjectWebpage = item.SubjectWebpage,
                                // FinderURL = item.FinderURL,
                                LastUpdated = ( DateTime ) item.LastUpdated,
                                IsInPublisher= Convert.ToBoolean( item.IsInPublisher )
                            };
                            output.Add( entity );
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                var msg = FormatExceptions( ex );
                LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".CredentialTypeQuery. " + msg ) );
                output.Add( new QuerySummary()
                {
                    Publisher = "Error encountered",
                    Organization = ex.Message,
                    Name = msg
                } );
            }
            return output;
        }

        #endregion

        #region ResourceType
        public static List<QuerySummary> ResourceType( Query request )
        {
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            try
            {
                using ( var context = new ViewContext() )
                {
                    //Start the query
                    var query = context.Reports_ResourceType.Where( s => s.EntityTypeId > 0 );

                    //Determine which type of report to run
                    var organizationCTIDs = request.GetFilterValues( "filter:OrganizationCTID", new List<string>() );
                    if ( organizationCTIDs?.Count() > 0 )
                    {
                        if ( IsForPublishingRecipients( request ) )
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.PublisherCTID ) );
                        }
                        else
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.OrganizationCTID ) /*|| organizationCTIDs.Contains( s.PublisherCTID ) */); //Same as OwnerCTID
                        }
                    }
                    /*
                    if ( !string.IsNullOrWhiteSpace( request.OwnerCTID ) )
                    {
                        query = query.Where( s => s.DataOwnerCTID == request.OwnerCTID );
                    }
					*/

                    //handle entity type
                    var entityTypeID = request.GetFilterIntValue( "filter:ResourceType" );
                    var entityTypeText = request.GetFilterValue( "filter:ResourceType" );
                    if ( entityTypeID > 0 )
                    {
                        query = query.Where( s => s.EntityTypeId == entityTypeID );
                    }
                    else if ( !string.IsNullOrWhiteSpace( entityTypeText ) )
                    {
                        query = query.Where( s => s.EntityType == entityTypeText );
                    }

                    //Handle Keywords Name filter
                    var nameText = request.GetFilterValue( "filter:NameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( nameText ) )
                    {
                        query = query.Where( s => s.Name.ToLower().Contains( nameText ) );
                    }

                    //Get total
                    request.TotalRows = query.Count();

                    var results = query.OrderBy( s => s.Publisher ).ThenBy( s => s.Organization ).ThenBy( s => s.Name ).Skip( request.Skip ).Take( request.Take ).ToList();
                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( var item in results )
                        {
                            entity = new QuerySummary()
                            {
                                PublisherCTID = item.PublisherCTID,
                                Publisher = item.Publisher != null ? item.Publisher : "",
                                Organization = item.Organization,
                                DataOwnerCTID = item.OrganizationCTID,
                                Name = item.Name,
                                EntityCTID = item.CTID,
                                EntityType = item.EntityType,
                                Description = item.Description,
                                SubjectWebpage = item.SubjectWebpage,
                                // FinderURL = item.FinderURL,
                                LastUpdated = ( DateTime ) item.LastUpdated
                            };
                            output.Add( entity );
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                var msg = FormatExceptions( ex );
                LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".ResourceTypeQuery. " + msg ) );
                output.Add( new QuerySummary()
                {
                    Publisher = "Error encountered",
                    Organization = ex.Message,
                    Name = msg
                } );
            }
            return output;
        }
        public static List<QuerySummary> ResourceSummary( Query request )
        {
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            try
            {
                using ( var context = new ViewContext() )
                {
                    //Start the query for the desired entitytype
                    var query = context.Reports_ResourceType.Where( s => s.EntityTypeId > 0 );

                    //Determine which type of report to run
                    var organizationCTIDs = request.GetFilterValues( "filter:OrganizationCTID", new List<string>() );
                    if ( organizationCTIDs?.Count() > 0 )
                    {
                        if ( IsForPublishingRecipients( request ) )
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.PublisherCTID ) );
                        }
                        else
                        {
                            query = query.Where( s => organizationCTIDs.Contains( s.OrganizationCTID ) /*|| s.PublisherCTID == request.OrganizationCTID*/ ); //Same as OwnerCTID
                        }
                    }
                    /*
                    if ( !string.IsNullOrWhiteSpace( request.OwnerCTID ) )
                    {
                        query = query.Where( s => s.OrganizationCTID == request.OwnerCTID );
                    }
					*/

                    //handle entity type
                    var EntityType = request.GetFilterIntValue( "filter:ResourceType" );
                    if ( EntityType>0 )
                    {
                        query = query.Where( s => s.EntityTypeId == EntityType );
                    }

                    //Handle Keywords Name filter
                    var OrgnameText = request.GetFilterValue( "filter:OrgNameText" )?.ToLower();
                    if ( !string.IsNullOrWhiteSpace( OrgnameText ) )
                    {
                        query = query.Where( s => s.Publisher.ToLower().Contains( OrgnameText ) );
                    }

                    //Get total
                    request.TotalRows = query.Count();
                    if ( IsForPublishingRecipients( request ) )
                    {
                        var result = query.GroupBy( x => x.Publisher ).ToList();
                        request.TotalRows = result.Count();
                        if ( result != null )
                        {
                            foreach ( var item in result )
                            {
                                entity = new QuerySummary()
                                {
                                    PublisherCTID = item.First().PublisherCTID,
                                    Publisher = item.Key,
                                    TotalItems = item.Count(),
                                };
                                output.Add( entity );
                            }
                        }
                    }
                    else
                    {
                        var result = query.GroupBy( x => x.Organization ).ToList(); ; // group by owner name 
                        request.TotalRows = result.Count();
                        if ( result != null )
                        {
                            foreach ( var item in result )
                            {
                                entity = new QuerySummary()
                                {
                                    PublisherCTID = item.First().PublisherCTID,
                                    Publisher = item.Key,
                                    TotalItems = item.Count()
                                };
                                output.Add( entity );
                            }
                        }
                    }
                    output = output.OrderBy( s => s.Publisher ).Skip( request.Skip ).Take( request.Take ).ToList();


                }
            }
            catch ( Exception ex )
            {
                var msg = FormatExceptions( ex );
                LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DuplicatesQuery. " + msg ) );
                output.Add( new QuerySummary()
                {
                    Publisher = "Error encountered",
                    Organization = ex.Message,
                    Name = msg
                } );
            }
            return output;
        }
       
        #endregion



        #region Helpers
        //Determine which type of report to run (this value comes from the _ReportsCoreV1.cshtml file at the moment, but that could be loaded from elsewhere)
        public static bool IsForPublishingRecipients( Query request )
		{
			var reportType = request.GetFilterValue( "filter:ReportType", "ForMyOrganization" );
			return reportType == "ForMyPublishingRecipients";
		}
      
        #endregion

        #region  Work.Query - mostly for adhoc queries
        /// <summary>
        /// Get purge candidates from Work.Query table
        /// </summary>
        /// <param name="pTotalRows"></param>
        /// <returns></returns>
        public static List<QuerySummary> WorkQuery_Purge( string purgeType, ref int pTotalRows)
        {
            var output = new List<QuerySummary>();
            var entity = new QuerySummary();
            try
            {
                using ( var context = new EntityContext() )
                {
                    var results = context.Work_Query
                            .Where( s => s.ReportType == "propath purge" || s.ReportType == "purge" )
                            .OrderBy( s => s.Name )
                             .ToList();

                    if ( results != null && results.Count > 0 )
                    {

                        foreach ( var item in results )
                        {
                            entity = new QuerySummary()
                            {
                                //PublisherCTID = item.OrganizationCTID,
                                //Publisher = item.Organization,

                                Organization = item.Organization,
                                DataOwnerCTID = item.OrganizationCTID,
                                Name = item.Name,
                                EntityCTID = item.CTID,
                                EntityType = string.IsNullOrWhiteSpace(item.EntityType) ? item.Category : item.EntityType
                            
                                //LastUpdated = ( DateTime ) item.LastUpdated
                            };
                            output.Add( entity );
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                var msg = FormatExceptions( ex );
                LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".WorkQuery_Purge. " + msg ) );
                output.Add( new QuerySummary()
                {
                    Publisher = "Error encountered",
                    Organization = ex.Message,
                    Name = msg
                } );
            }
            return output;
        }



        #endregion
    }
}
