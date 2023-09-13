using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using workIT.Factories;
using workIT.Utilities;

using OutputResource = workIT.Models.API.SupportService;
using ResourceHelper = workIT.Services.SupportServiceServices;
using ThisResource = workIT.Models.Common.SupportService;
using ResourceManager = workIT.Factories.SupportServiceManager;
using WMA = workIT.Models.API;

namespace workIT.Services.API
{
    public class SupportServiceServices
    {
        static string thisClassName = "API.SupportServiceServices";
        public static string searchType = "SupportService";
        public static OutputResource GetDetailForAPI( int id, bool skippingCache = false )
        {
            OutputResource outputEntity = new OutputResource();

            //only cache longer processes
            DateTime start = DateTime.Now;
            var entity = ResourceManager.GetForDetail( id, true );

            DateTime end = DateTime.Now;
            //for now don't include the mapping in the elapsed
            int elasped = ( DateTime.Now - start ).Seconds;
            outputEntity = MapToAPI( entity );

            return outputEntity;

        }
        public static OutputResource GetDetailByCtidForAPI( string ctid, bool skippingCache = false )
        {
            var credential = ResourceManager.GetMinimumByCtid( ctid );
            return GetDetailForAPI( credential.Id, skippingCache );

        }
        public static OutputResource GetDetailForElastic( int id, bool skippingCache )
        {
            var record = ResourceHelper.GetDetail( id, skippingCache );
            return MapToAPI( record );
        }
        private static OutputResource MapToAPI( ThisResource input )
        {

            var output = new OutputResource()
            {
                Meta_Id = input.Id,
                CTID = input.CTID,
                Name = input.Name,
                Description = input.Description,
                EntityLastUpdated = input.EntityLastUpdated,
                SubjectWebpage = input.SubjectWebpage,
                Meta_StateId = input.EntityStateId,

            };
            if ( input.EntityStateId == 0 )
            {
                return output;
            }

            if ( !string.IsNullOrWhiteSpace( input.CTID ) )
            {
                output.CredentialRegistryURL = RegistryServices.GetResourceUrl( input.CTID );

                output.RegistryData = ServiceHelper.FillRegistryData( input.CTID, searchType );
                //experimental - not used in UI yet
                output.RegistryDataList.Add( output.RegistryData );
            }
            //check for others

            //
            output.Meta_FriendlyName = HttpUtility.UrlPathEncode( input.Name );
            output.AlternateName = input.AlternateName;

            //Note: only offered by
            //need a label link for header
            if ( input.OwningOrganizationId > 0 )
            {
                output.OwnedByLabel = ServiceHelper.MapDetailLink( "Organization", input.OrganizationName, input.OwningOrganizationId, input.PrimaryOrganization.FriendlyName );
            }

            var ownedBy = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER );

            var offeredBy = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY );
            //output.OfferedBy = ServiceHelper.MapOutlineToAJAX( offeredBy, "Offered by {0} Organization(s)" );
            if ( ownedBy != null && offeredBy != null && ownedBy.Count == 1 && offeredBy.Count == 1
                    && ownedBy[0].Meta_Id == offeredBy[0].Meta_Id )
            {
                output.OwnedOfferedBy = ServiceHelper.MapOutlineToAJAX( ownedBy, "" );
            }
            else
            {
                output.OwnedBy = ServiceHelper.MapOutlineToAJAX( ownedBy, "" );
                output.OfferedBy = ServiceHelper.MapOutlineToAJAX( offeredBy, "Offered by {0} Organization(s)" );
            }

            //should only do if different from owner! Actually only populated if by a 3rd party
            output.Publisher = ServiceHelper.MapOutlineToAJAX( ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY ), "Published By" );

            try
            {

                //
                output.AvailabilityListing = input.AvailabilityListing;
                output.AvailableOnlineAt = input.AvailableOnlineAt;
                //addresses
                output.AvailableAt = ServiceHelper.MapAddress( input.AvailableAt );
                //

                output.AccommodationType = ServiceHelper.MapPropertyLabelLinks( input.AccommodationType, searchType );

                output.DeliveryType = ServiceHelper.MapPropertyLabelLinks( input.DeliveryType, searchType );
                output.DateEffective = input.DateEffective;
                output.ExpirationDate = input.ExpirationDate;
                output.FinancialAssistance = ServiceHelper.MapFinancialAssistanceProfiles( input.FinancialAssistance, searchType );
                output.Identifier = ServiceHelper.MapIdentifierValue( input.Identifier );
                output.Keyword = ServiceHelper.MapPropertyLabelLinks( input.Keyword, searchType );
                output.OccupationType = ServiceHelper.MapReferenceFramework( input.OccupationType, searchType, CodesManager.PROPERTY_CATEGORY_SOC );

                var links = new List<WMA.LabelLink>();
                output.Connections = null;

                var work = new List<WMA.Outline>();
                if ( input.SupportServiceReferencedBy?.Count > 0 )
                {
                    foreach ( var target in input.SupportServiceReferencedBy )
                    {
                        if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
                            work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
                    }
                    output.SupportServiceReferencedBy = ServiceHelper.MapOutlineToAJAX( work, "Referenced By Resources" );

                    ServiceHelper.MapSupportServiceSearchLink( input.Id, input.Name, input.SupportServiceReferencedBy.Count, "NO Referenced By {0} Resources", "supportservice", ref links );

                    output.Connections = links;
                }
                //
                if ( input.HasSpecificService?.Count > 0 )
                {
                    work = new List<WMA.Outline>();
                    foreach ( var target in input.HasSpecificService )
                    {
                        if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
                            work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
                    }
                    output.HasSpecificService = ServiceHelper.MapOutlineToAJAX( work, "Has {0} Support Services" );

                    ServiceHelper.MapSupportServiceSearchLink( input.Id, input.Name, input.HasSpecificService.Count, "Has {0} Specific Services", "supportservice", ref links );

                    output.Connections = links;
                }
                //
                if ( input.IsSpecificServiceOf?.Count > 0 )
                {
                    work = new List<WMA.Outline>();
                    foreach ( var target in input.IsSpecificServiceOf )
                    {
                        if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
                            work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
                    }
                    output.IsSpecificServiceOf = ServiceHelper.MapOutlineToAJAX( work, "Part of {0} Support Services" );

                    ServiceHelper.MapSupportServiceSearchLink( input.Id, input.Name, input.IsSpecificServiceOf.Count, "Part of {0} Support Services", "supportservice", ref links );

                    output.Connections = links;
                }

            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 1, Name: {0}, Id: {1}", input.Name, input.Id ) );

            }

            //=======================================
            try
            {
                if ( input.OfferedIn != null )
                {
                    var assertions = input.OfferedIn.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY ).ToList();
                    if ( assertions != null && assertions.Any() )
                        output.OfferedIn = ServiceHelper.MapJurisdiction( assertions, "OfferedIn" );
                }
                //
                output.SupportServiceConditon = ServiceHelper.MapToConditionProfiles( input.SupportServiceCondition, searchType );

                if ( input.CommonConditions != null && input.CommonConditions.Any() )
                {
                    output.CommonConditions = ServiceHelper.MapConditionManifests( input.CommonConditions, searchType );
                    if ( output.CommonConditions != null && output.CommonConditions.Any() )
                    {

                    }
                }

                if ( input.CommonCosts != null && input.CommonCosts.Any() )
                {
                    output.CommonCosts = ServiceHelper.MapCostManifests( input.CommonCosts, searchType );
                }

                if ( input.EstimatedCost != null && input.EstimatedCost.Any() )
                {
                    if ( output.EstimatedCost == null )
                        output.EstimatedCost = new List<Models.Elastic.CostProfile>();

                    var estimatedCost = ServiceHelper.MapCostProfiles( input.EstimatedCost, searchType );
                    if ( estimatedCost != null && estimatedCost.Any() )
                        output.EstimatedCost.AddRange( estimatedCost );
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 3, Name: {0}, Id: {1}", input.Name, input.Id ) );

            }
            output.SupportServiceType = ServiceHelper.MapPropertyLabelLinks( input.SupportServiceType, searchType );

            //
            return output;
        }


    }
}
