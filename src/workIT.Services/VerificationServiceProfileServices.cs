using System.Collections.Generic;

using workIT.Factories;
using workIT.Models;
using workIT.Utilities;

using ResourceManager = workIT.Factories.VerificationServiceProfileManager;
using ThisResource = workIT.Models.ProfileModels.VerificationServiceProfile;

namespace workIT.Services
{
    public class VerificationServiceProfileServices
    {

        static string thisClassName = "VerificationServiceProfileServices";
        #region import
        public static ThisResource GetByCtid( string ctid )
        {
            ThisResource resource = new ThisResource();
            if ( string.IsNullOrWhiteSpace( ctid ) )
                return resource;

            return ResourceManager.GetByCtid( ctid );
        }

        public bool Import( ThisResource resource, ref SaveStatus status )
        {
            LoggingHelper.DoTrace( 7, thisClassName + ".Import - entered" );

            bool isValid = new ResourceManager().Save( resource, ref status );
            List<string> messages = new List<string>();
            if ( resource.Id > 0 )
            {
                if ( resource.OwningOrganizationId == 0 )
                {
                    resource = ResourceManager.GetBasic( resource.Id );
                }
                //currently no search, so skip
                //new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_VERIFICATION_PROFILE, resource.Id, 1, ref messages );
                new SearchPendingReindexManager().Add( CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, resource.OwningOrganizationId, 1, ref messages );
                if ( messages.Count > 0 )
                    status.AddWarningRange( messages );
            }

            return isValid;
        }
        #endregion
    }
}
