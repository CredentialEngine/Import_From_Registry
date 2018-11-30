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
        public SaveStatus ImportByEnvelopeId( string envelopeId )
        {
            LoggingHelper.DoTrace( 6, thisClassName + string.Format( "Request to import entity by envelopeId: {0}", envelopeId ) );
            Import.Services.RegistryServices mgr = new Import.Services.RegistryServices();
            if ( mgr.ImportByEnvelopeId( envelopeId, status ) )
            {
                new RegistryServices().ImportPending();
                //can't call Services from here
                //ElasticServices.UpdateElastic();
            }
            return status;
		}
		//
		public SaveStatus ImportByCtid( string ctid)
		{
            //, bool doPendingTask = false 
            LoggingHelper.DoTrace( 6, thisClassName + string.Format( "Request to import entity by ctid: {0}", ctid ) );
			Import.Services.RegistryServices mgr = new Import.Services.RegistryServices();
            if ( mgr.ImportByCtid( ctid, status ))
            {
                new RegistryServices().ImportPending();

                //ElasticServices.UpdateElastic();
            }
   
            return status;
		}
		//
		public SaveStatus ImportCredential(string envelopeId)
		{
			LoggingHelper.DoTrace( 6, thisClassName + string.Format( "Request to import credential by envelopeId: {0}", envelopeId ) );
			Import.Services.ImportCredential mgr = new Import.Services.ImportCredential();
			mgr.ImportByEnvelopeId( envelopeId, status );
			new RegistryServices().ImportPending();
			return status;
		}
		public SaveStatus ImportCredentialByCtid( string ctid )
		{
			LoggingHelper.DoTrace( 6, thisClassName + string.Format( "Request to import credential by ctid: {0}", ctid ) );
			Import.Services.ImportCredential mgr = new Import.Services.ImportCredential();
			mgr.ImportByCtid( ctid, status );
			new RegistryServices().ImportPending();
			return status;
		}
		public SaveStatus ImportOrganization( string envelopeId )
		{
			Import.Services.ImportOrganization mgr = new Import.Services.ImportOrganization();
			mgr.RequestImportByEnvelopeId( envelopeId, status );
			new RegistryServices().ImportPending();

			return status;
		}
		public SaveStatus ImportOrganizationByCtid( string ctid )
		{
			Import.Services.ImportOrganization mgr = new Import.Services.ImportOrganization();
			mgr.RequestImportByCtid( ctid, status );
			new RegistryServices().ImportPending();

			return status;
		}
		public SaveStatus ImportAssessment( string envelopeId )
		{
			Import.Services.ImportAssessment mgr = new Import.Services.ImportAssessment();
			mgr.ImportByEnvelopeId( envelopeId, status );
			new RegistryServices().ImportPending();

			return status;
		}
		public SaveStatus ImportAssessmentByCtid( string ctid )
		{
			Import.Services.ImportAssessment mgr = new Import.Services.ImportAssessment();
			mgr.ImportByCtid( ctid, status );
			new RegistryServices().ImportPending();
			return status;
		}
		public SaveStatus ImportLearningOpportunty( string envelopeId )
		{
			Import.Services.ImportLearningOpportunties mgr = new Import.Services.ImportLearningOpportunties();
			mgr.ImportByEnvelopeId( envelopeId, status );
			new RegistryServices().ImportPending();
			return status;
		}
		public SaveStatus ImportLearningOpportuntyByCtid( string ctid )
		{
			Import.Services.ImportLearningOpportunties mgr = new Import.Services.ImportLearningOpportunties();
			mgr.ImportByCtid( ctid, status );
			new RegistryServices().ImportPending();
			return status;
		}
	}
}
