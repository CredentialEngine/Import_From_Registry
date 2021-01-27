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
            Import.Services.RegistryServices mgr = new Import.Services.RegistryServices();
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
			Import.Services.RegistryServices mgr = new Import.Services.RegistryServices();
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
		public SaveStatus ImportCredential( string envelopeId, bool handlingPendingRecords = false)
		{
			LoggingHelper.DoTrace( 6, thisClassName + string.Format( "Request to import credential by envelopeId: {0}", envelopeId ) );
			Import.Services.ImportCredential mgr = new Import.Services.ImportCredential();
			mgr.ImportByEnvelopeId( envelopeId, status );
			//new RegistryServices().ImportPending();
			if ( handlingPendingRecords )
				new ImportCredential().ImportPendingRecords();

			return status;
		}
		public SaveStatus ImportCredentialByCtid( string ctid, bool handlingPendingRecords = false )
		{
			LoggingHelper.DoTrace( 6, thisClassName + string.Format( "Request to import credential by ctid: {0}", ctid ) );
			Import.Services.ImportCredential mgr = new Import.Services.ImportCredential();
			mgr.ImportByCtid( ctid, status );
			//new RegistryServices().ImportPending();
			if (handlingPendingRecords)
				new ImportCredential().ImportPendingRecords();
			return status;
		}
		public SaveStatus ImportOrganization( string envelopeId )
		{
			Import.Services.ImportOrganization mgr = new Import.Services.ImportOrganization();
			mgr.RequestImportByEnvelopeId( envelopeId, status );
			//new RegistryServices().ImportPending();

			return status;
		}
		public SaveStatus ImportOrganizationByCtid( string ctid )
		{
			Import.Services.ImportOrganization mgr = new Import.Services.ImportOrganization();
			mgr.RequestImportByCtid( ctid, status );
			//new RegistryServices().ImportPending();

			return status;
		}
		public SaveStatus ImportAssessment( string envelopeId )
		{
			Import.Services.ImportAssessment mgr = new Import.Services.ImportAssessment();
			mgr.ImportByEnvelopeId( envelopeId, status );
			//new RegistryServices().ImportPending();

			return status;
		}
		public SaveStatus ImportAssessmentByCtid( string ctid )
		{
			Import.Services.ImportAssessment mgr = new Import.Services.ImportAssessment();
			mgr.ImportByCtid( ctid, status );
			//new RegistryServices().ImportPending();
			return status;
		}
		public SaveStatus ImportLearningOpportunty( string envelopeId, bool handlingPendingRecords = false )
		{
			Import.Services.ImportLearningOpportunties mgr = new Import.Services.ImportLearningOpportunties();
			mgr.ImportByEnvelopeId( envelopeId, status );
			//new RegistryServices().ImportPending();
			if ( handlingPendingRecords )
				new ImportLearningOpportunties().ImportPendingRecords();
			return status;
		}
		public SaveStatus ImportLearningOpportuntyByCtid( string ctid, bool handlingPendingRecords = false )
		{
			Import.Services.ImportLearningOpportunties mgr = new Import.Services.ImportLearningOpportunties();
			mgr.ImportByCtid( ctid, status );
			//new RegistryServices().ImportPending();
			if ( handlingPendingRecords )
				new ImportLearningOpportunties().ImportPendingRecords();
			return status;
		}

		//
		public SaveStatus ImportCompetencyFramework( string ctid, string envelopeId, bool handlingPendingRecords = false )
		{
			if ( !string.IsNullOrWhiteSpace( ctid ) )
				new Import.Services.ImportCompetencyFramesworks().ImportByCtid( ctid, status );
			else if ( !string.IsNullOrWhiteSpace( envelopeId ) )
				new Import.Services.ImportCompetencyFramesworks().ImportByEnvelopeId( envelopeId, status );

			//if ( handlingPendingRecords )
			//	new ImportLearningOpportunties().ImportPendingRecords();
			return status;
		}
		//
		public SaveStatus ImportPathway( string ctid, string envelopeId, bool handlingPendingRecords = false )
		{
			if ( !string.IsNullOrWhiteSpace( ctid ) )
				new Import.Services.ImportPathways().ImportByCtid( ctid, status );
			else if ( !string.IsNullOrWhiteSpace( envelopeId ) )
				new Import.Services.ImportPathways().ImportByEnvelopeId( envelopeId, status );

			//if ( handlingPendingRecords )
			//	new ImportLearningOpportunties().ImportPendingRecords();
			return status;
		}
		//
		public SaveStatus ImportPathwaySet( string ctid, string envelopeId, bool handlingPendingRecords = false )
		{
			if ( !string.IsNullOrWhiteSpace( ctid ) )
				new Import.Services.ImportPathwaySets().ImportByCtid( ctid, status );
			else if ( !string.IsNullOrWhiteSpace( envelopeId ) )
				new Import.Services.ImportPathwaySets().ImportByEnvelopeId( envelopeId, status );

			//if ( handlingPendingRecords )
			//	new ImportLearningOpportunties().ImportPendingRecords();
			return status;
		}
		//
		public SaveStatus ImportTransferValue( string ctid, string envelopeId, bool handlingPendingRecords = false )
		{
			if ( !string.IsNullOrWhiteSpace( ctid ) )
				new Import.Services.ImportTransferValue().ImportByCtid( ctid, status );
			else if ( !string.IsNullOrWhiteSpace( envelopeId ) )
				new Import.Services.ImportTransferValue().ImportByEnvelopeId( envelopeId, status );

			//if ( handlingPendingRecords )
			//	new ImportLearningOpportunties().ImportPendingRecords();
			return status;
		}
	}
}
