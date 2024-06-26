using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Utilities;
using workIT.Models;

using ThisResource = workIT.Models.RegistryImport;
using EntityMgr = workIT.Factories.ImportManager;

namespace Import.Services
{
    public class ImportServiceHelpers
    {
        public static bool IsAGraphResource(string payload)
        {
            if ( string.IsNullOrWhiteSpace( payload ) )
                return false;

            //may need additional checks?
            if ( payload.IndexOf( "@graph" ) > 0 )
                return true;
            //else if ( payload.IndexOf( "\"en\":" ) > 0 ) //actually this should be an error condition
            //    return true;
            else
                return false;
        }

        /// <summary>
        /// Add envelope and related data to the staging table
        /// </summary>
        /// <param name="item"></param>
        /// <param name="entityTypeId"></param>
        /// <param name="ctid"></param>
        /// <param name="importSuccessfull"></param>
        /// <param name="importErrorMsg"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public int Add( ReadEnvelope item, int entityTypeId, string ctid, bool importSuccessfull,
            string importErrorMsg, ref List<string> messages )
        {
            ThisResource entity = new ThisResource();
            entity.EntityTypedId = entityTypeId;
            entity.EnvelopeId = item.EnvelopeIdentifier;
            entity.Ctid = ctid;
            entity.ResourcePublicKey = item.ResourcePublicKey;
            DateTime updateDate = DateTime.Now;
            if ( DateTime.TryParse( item.NodeHeaders.UpdatedAt.Replace( "UTC", "" ).Trim(), out updateDate ) )
            {
                entity.DocumentUpdatedAt = updateDate;
            }

            entity.Message = importErrorMsg;
            entity.Payload = item.DecodedResource.ToString();

            return new EntityMgr().Add( entity, ref messages );
        }

        //
        public int AddMessages( int importId, SaveStatus status, ref List<string> messages )
        {

            return new EntityMgr().AddMessages( status, importId, ref messages );
        }
    }
}
