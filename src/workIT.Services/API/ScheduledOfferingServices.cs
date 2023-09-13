using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using workIT.Factories;
using workIT.Models.Common;
using workIT.Models.Search;
using workIT.Utilities;


using ElasticHelper = workIT.Services.ElasticServices;
using ResourceManager = workIT.Factories.ScheduledOfferingManager;
using ThisResource = workIT.Models.Common.ScheduledOffering;
using OutputResource = workIT.Models.API.ScheduledOffering;
using ResourceHelper = workIT.Services.ScheduledOfferingServices;

using WMA = workIT.Models.API;
using WMP = workIT.Models.ProfileModels;
using WorkITSearchServices = workIT.Services.SearchServices;

namespace workIT.Services.API
{
    public class ScheduledOfferingServices
    {
        static string thisClassName = "API.ScheduledOfferingServices";
        public static string searchType = "ScheduledOffering";
        public static string EntityType = "ScheduledOffering";
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

            var offeredBy = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY );
             output.OfferedBy = ServiceHelper.MapOutlineToAJAX( offeredBy, "Offered by {0} Organization(s)" );
    
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


                output.EstimatedDuration = ServiceHelper.MapDurationProfiles( input.EstimatedDuration );
                output.DeliveryType = ServiceHelper.MapPropertyLabelLinks( input.DeliveryType, searchType );
                output.DeliveryTypeDescription = input.DeliveryTypeDescription;
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 1, Name: {0}, Id: {1}", input.Name, input.Id ) );

            }

            //=======================================
            try
            {
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
            output.OfferFrequencyType = ServiceHelper.MapPropertyLabelLinks( input.OfferFrequencyType, searchType );
            output.ScheduleFrequencyType = ServiceHelper.MapPropertyLabelLinks( input.ScheduleFrequencyType, searchType );
            output.ScheduleTimingType = ServiceHelper.MapPropertyLabelLinks( input.ScheduleTimingType, searchType );


            //====================================
            //
            if ( input.HasSupportService?.Count > 0 )
            {
                var work = new List<WMA.Outline>();
                foreach ( var target in input.HasSupportService )
                {
                    if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
                        work.Add( ServiceHelper.MapToOutline( target, EntityType ) );
                }
                output.HasSupportService = ServiceHelper.MapOutlineToAJAX( work, "Has {0} Support Services" );
            }
            //
            output.AggregateData = ServiceHelper.MapToAggregateDataProfile( input.AggregateData, searchType );
            if ( output.AggregateData != null )
            {
                //hmm check for dataSetProfile to add to RegistryDataList.
                //Might be better to do this in the managers
            }
            //TBD - need to exclude those that are already in the AggregateDataProfile ==> try to handle this in the managers!
            output.ExternalDataSetProfiles = ServiceHelper.MapToDatasetProfileList( input.ExternalDataSetProfiles, searchType );
            //could add these to RegistryDataList??
            if ( output.ExternalDataSetProfiles != null && output.ExternalDataSetProfiles.Any() )
            {
                foreach ( var item in output.ExternalDataSetProfiles )
                {
                    var regData = ServiceHelper.FillRegistryData( item.CTID, searchType );
                    output.RegistryDataList.Add( regData );
                }
            }

            //
            return output;
        }


    }
}
