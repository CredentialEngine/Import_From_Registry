using System;
using System.Web;

using workIT.Factories;
using workIT.Utilities;

using OutputResource = workIT.Models.API.DataSetProfile;
using ResourceHelper = workIT.Services.DataSetProfileServices;
using ThisResource = workIT.Models.QData.DataSetProfile;
using ResourceManager = workIT.Factories.DataSetProfileManager;

namespace workIT.Services.API
{
    public class OutcomeDataServices
    {
        static string thisClassName = "API.OutcomeDataServices";
        public static string searchType = "DataSetProfile";
        public static string EntityType = "DataSetProfile";
        public static int EntityTypeId = CodesManager.ENTITY_TYPE_DATASET_PROFILE;
        
        public static OutputResource GetDetailForAPI( int id, bool skippingCache = false )
        {
            OutputResource outputEntity = new OutputResource();

            //only cache longer processes
            DateTime start = DateTime.Now;
            var entity = ResourceManager.Get( id, true, true );

            DateTime end = DateTime.Now;
            //for now don't include the mapping in the elapsed
            int elasped = ( DateTime.Now - start ).Seconds;
            outputEntity = MapToAPI( entity );

            return outputEntity;

        }
        public static OutputResource GetDetailByCtidForAPI( string ctid, bool skippingCache = false )
        {
            var credential = ResourceManager.GetByCtid( ctid, false );
            return GetDetailForAPI( credential.Id, skippingCache );

        }
        public static OutputResource GetDetailForElastic( int id, bool skippingCache )
        {
            var record = ResourceHelper.GetDetail( id );
            return MapToAPI( record );
        }
        private static OutputResource MapToAPI( ThisResource input )
        {

            //var output = new OutputResource()
            //{
            //    Meta_Id = input.Id,
            //    EntityTypeId = EntityTypeId,
            //    CTDLType = EntityType,
            //    CTID = input.CTID,
            //    Name = input.Name,
            //    Description = input.Description,
            //    EntityLastUpdated = input.EntityLastUpdated,
            //    SubjectWebpage = input.SubjectWebpage,
            //    Meta_StateId = input.EntityStateId,

            //};
            if ( input.EntityStateId == 0 )
            {
                return new OutputResource();
            }
            var output = ServiceHelper.MapToDatasetProfile( input, searchType );

            if (input.DataProviderId > 0)
            {
                output.OwnedByLabel = ServiceHelper.MapDetailLink( "Organization", input.DataProviderName, input.DataProviderId, input.PrimaryOrganization.FriendlyName );
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


            //something for primary org, but maybe not ownedBy
            //if ( input.DataProviderId > 0 )
            //{
            //    output.OwnedByLabel = ServiceHelper.MapDetailLink( "Organization", input.PrimaryOrganizationName, input.PrimaryOrganizationId, input.PrimaryOrganization.FriendlyName );
            //}


            //
            return output;
        }


    }
}
