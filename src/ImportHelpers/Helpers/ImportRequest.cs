using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using Import.Services;
using workIT.Utilities;

namespace ImportHelpers
{
    /// <summary>
    /// Created to enable calling import methods from the web project
    /// </summary>
    public class ImportRequest
    {
		string thisClassName = "ImportHelpers.ImportRequest";
		SaveStatus status = new SaveStatus();
        public SaveStatus ImportByEnvelopeId( string envelopeId, bool handlingPendingRecords = false )
        {
            LoggingHelper.DoTrace( 6, thisClassName + string.Format( "Request to import entity by envelopeId: {0}", envelopeId ) );
            var mgr = new ImportHelperServices();
            if ( mgr.ImportByEnvelopeId( envelopeId, status ) )
            {
				if ( handlingPendingRecords )
					new RegistryServices().ImportPending();
                //can't call Services from here -> so will do in caller
                //ElasticServices.UpdateElastic();
            }
            return status;
		}
		//
		public SaveStatus ImportByCtid( string ctid, bool handlingPendingRecords = false )
		{
            //, bool doPendingTask = false 
            LoggingHelper.DoTrace( 6, thisClassName + string.Format( "Request to import entity by ctid: {0}", ctid ) );
			var mgr = new ImportHelperServices();
			//TODO - update to check for alternate community if not found with the default community
			if ( mgr.ImportByCtid( ctid, status ))
            {
				if ( handlingPendingRecords )
					new RegistryServices().ImportPending();

                //ElasticServices.UpdateElastic();
            }
   
            return status;
		}
		//
		public SaveStatus ImportByURL( string resourceUrl, bool handlingPendingRecords = false )
		{
			//, bool doPendingTask = false 
			LoggingHelper.DoTrace( 6, thisClassName + string.Format( "Request to import entity by resourceUrl: {0}", resourceUrl ) );
			var mgr = new ImportHelperServices();
			//TODO - update to check for alternate community if not found with the default community
			if ( mgr.ImportByURL( resourceUrl, status ) )
			{
				if ( handlingPendingRecords )
					new RegistryServices().ImportPending();

				//ElasticServices.UpdateElastic();
			}

			return status;
		}
		//
		public SaveStatus PurgeByCtid( string ctid, bool deleteOnlyNoPurge = false )
		{
			//, bool doPendingTask = false 
			LoggingHelper.DoTrace( 6, thisClassName + string.Format( "Request to purge entity by ctid: {0}", ctid ) );
			var mgr = new ImportHelperServices();
			SaveStatus status2 = new SaveStatus();
			//TODO - how to handle community
			if ( mgr.PurgeByCtid( ctid, status2, deleteOnlyNoPurge ) )
			{
				//allow the import to handle elastic- any issues with this?
				//ElasticServices.UpdateElastic();
			} else
			{
                //may want to delete record and from elastic regardless?
            }


            return status2;
		}
		//
		public SaveStatus SetOrganizationToCeased( string ctid )
		{
			//, bool doPendingTask = false 
			LoggingHelper.DoTrace( 6, thisClassName + string.Format( "Request to SetOrganizationToCeased: {0}", ctid ) );
			var mgr = new ImportHelperServices();
			SaveStatus status2 = new SaveStatus();
			//TODO - how to handle community
			if ( mgr.SetOrganizationToCeased( ctid, status2 ) )
			{
				//allow the import to handle elastic- any issues with this?
				//ElasticServices.UpdateElastic();
			}
			//may want to delete record and from elastic regardless

			return status2;
		}
		//
	}
}
