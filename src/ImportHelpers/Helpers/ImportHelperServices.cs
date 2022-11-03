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
	public class ImportHelperServices
	{

		/// <summary>
		/// Retrieve an envelop from the registry and do import
		/// Custom version for Import from Finder.Import
		/// TODO - THIS IS SOMEWHAT HIDDEN HERE - easy to forget when adding new classes
		/// </summary>
		/// <param name="envelopeId"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool ImportByEnvelopeId( string envelopeId, SaveStatus status )
		{
			//this is currently specific, assumes envelop contains a credential
			//can use the hack for GetResourceType to determine the type, and then call the appropriate import method

			if ( string.IsNullOrWhiteSpace( envelopeId ) )
			{
				status.AddError( "ImportByEnvelope - a valid envelope id must be provided" );
				return false;
			}

			string statusMessage = "";
			string ctdlType = "";
			try
			{
				ReadEnvelope envelope = RegistryServices.GetEnvelope( envelopeId, ref statusMessage, ref ctdlType );
				if ( envelope == null || string.IsNullOrWhiteSpace( envelope.EnvelopeType ) )
				{
					string defCommunity = UtilityManager.GetAppKeyValue( "defaultCommunity" );
					string community = UtilityManager.GetAppKeyValue( "additionalCommunity" );
					if ( defCommunity != community )
						envelope = RegistryServices.GetEnvelope( envelopeId, ref statusMessage, ref ctdlType, community );
				}

				if ( envelope != null && !string.IsNullOrWhiteSpace( envelope.EnvelopeIdentifier ) )
				{
					return ImportEnvelope( envelope, ctdlType, status );
					//LoggingHelper.DoTrace( 4, string.Format( "RegistryServices.ImportByEnvelopeId ctdlType: {0}, EnvelopeId: {1} ", ctdlType, envelopeId ) );
					//ctdlType = ctdlType.Replace( "ceterms:", "" );

					//switch ( ctdlType.ToLower() )
					//{
					//	case "credentialorganization":
					//	case "qacredentialorganization": //what distinctions do we need for QA orgs?
					//	case "organization":
					//		return new ImportOrganization().CustomProcessEnvelope( envelope, status );
					//	//break;CredentialOrganization
					//	case "assessmentprofile":
					//		return new ImportAssessment().CustomProcessEnvelope( envelope, status );
					//	//break;
					//	case "learningopportunityprofile":
					//		return new ImportLearningOpportunties().CustomProcessEnvelope( envelope, status );
					//	//break;
					//	case "conditionmanifest":
					//		return new ImportAssessment().CustomProcessEnvelope( envelope, status );
					//	//break;
					//	case "costmanifest":
					//		return new ImportLearningOpportunties().CustomProcessEnvelope( envelope, status );
					//	case "competencyframework":
					//		return new ImportCompetencyFramesworks().CustomProcessEnvelope( envelope, status );
					//	case "conceptscheme":
					//		return new ImportConceptSchemes().CustomProcessEnvelope( envelope, status );
					//	case "pathway":
					//		return new ImportPathways().CustomProcessEnvelope( envelope, status );
					//	case "pathwaysset":
					//		return new ImportPathwaySets().CustomProcessEnvelope( envelope, status );
					//	case "transfervalueprofile":
					//		return new ImportTransferValue().CustomProcessEnvelope( envelope, status );

					//	case "job":
					//	case "occupation":
					//	case "rating":
					//	case "rubric":
					//	case "task":
					//	case "workrole":
					//	{
					//		LoggingHelper.DoTrace( 1, string.Format( "ImportByEnvelopeId. {0} ({1}-{2}) is not handled at this time. ", ctdlType, envelope.EnvelopeCtdlType, envelope.EnvelopeCetermsCtid ) );
					//		return false;
					//	}

					//	//break;
					//	default:
					//		//default to credential
					//		//actually should have an edit for this.
					//		return new ImportCredential().CustomProcessRequest( envelope, status );
					//		//break;
					//}
				}
				else
				{
					status.AddError( string.Format( "ImportHelperServices.ImportByEnvelopeId. Registry Envelope was not found for: {0}", envelopeId ) );
					return false;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "RegistryServices`.ImportByEnvelopeId(). ctdlType: {0}", ctdlType ) );
				status.AddError( ex.Message );
				if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
				{
					status.AddWarning( "The referenced registry document is using an old schema. Please republish it with the latest schema!" );
				}
				return false;
			}
		}
		//
		public bool ImportByCtid( string ctid, SaveStatus status )
		{
			//this is currently specific, assumes envelop contains a credential
			//can use the hack fo GetResourceType to determine the type, and then call the appropriate import method

			if ( string.IsNullOrWhiteSpace( ctid ) )
			{
				status.AddError( "ImportByCtid - a valid ctid must be provided" );
				return false;
			}

			string statusMessage = "";
			string ctdlType = "";
			//string payload = "";
			try
			{
				ReadEnvelope envelope = RegistryServices.GetEnvelopeByCtid( ctid, ref statusMessage, ref ctdlType );
				if ( envelope == null || string.IsNullOrWhiteSpace( envelope.EnvelopeType ) )
				{
					string defCommunity = UtilityManager.GetAppKeyValue( "defaultCommunity" );
					string community = UtilityManager.GetAppKeyValue( "additionalCommunity" );
					if ( defCommunity != community )
						envelope = RegistryServices.GetEnvelopeByCtid( ctid, ref statusMessage, ref ctdlType, community );
				}
				if ( envelope != null && !string.IsNullOrWhiteSpace( envelope.EnvelopeIdentifier ) )
				{
					return ImportEnvelope( envelope, ctdlType, status );
				}
				else
				{
					status.AddError( string.Format( "ImportHelperServices.ImportByCTID. Registry Envelope was not found for: {0}", ctid ) );
					return false;
				}

				/*======OLD ===================
				payload = GetResourceGraphByCtid( ctid, ref ctdlType, ref statusMessage );
				if (string.IsNullOrWhiteSpace(payload))
				{
					string defCommunity = UtilityManager.GetAppKeyValue( "defaultCommunity" );
					string community = UtilityManager.GetAppKeyValue( "additionalCommunity" );
					if ( defCommunity != community )
						payload = GetResourceGraphByCtid( ctid, ref ctdlType, ref statusMessage, community );
				}
				if ( !string.IsNullOrWhiteSpace( payload ) )
                {
                    LoggingHelper.WriteLogFile( 5, ctid + "_ImportByCtid.json", payload, "", false );
                    LoggingHelper.DoTrace( 4, string.Format( "RegistryServices.ImportByCtid ctdlType: {0}, ctid: {1} ", ctdlType, ctid ) );
                    ctdlType = ctdlType.Replace( "ceterms:", "" );
                    switch ( ctdlType.ToLower() )
                    {
                        case "credentialorganization":
                        case "qacredentialorganization":
                        case "organization":
                            return new ImportOrganization().ImportByPayload( payload, status );
                        //break;CredentialOrganization
                        case "assessmentprofile":
                            return new ImportAssessment().ImportByPayload( payload, status );
                        //break;
                        case "learningopportunityprofile":
                            return new ImportLearningOpportunties().ImportByPayload( payload, status );
                        //break;
                        case "conditionmanifest":
                            return new ImportConditionManifests().ImportByPayload( payload, status );
                        //break;
                        case "costmanifest":
                            return new ImportCostManifests().ImportByPayload( payload, status );
						case "competencyframework":
							return new ImportCompetencyFramesworks().Import( payload, "", status );
						case "conceptscheme":
						case "skos:conceptscheme":
							return new ImportConceptSchemes().Import( payload, "", status );
						case "pathway":
							return new ImportPathways().Import( payload, "", status );
						case "pathwayset":
							return new ImportPathwaySets().Import( payload, "", status );
						case "transfervalue":
						case "transfervalueprofile":
							return new ImportTransferValue().Import( payload, "", status );
						//break;
						default:
                            //default to credential
                            return new ImportCredential().ImportByPayload( payload, status );
                            //break;
                    }
                }
                else
                    return false;
				*/
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "ImportCredential.ImportByCtid(). ctdlType: {0}", ctdlType ) );
				status.AddError( ex.Message );
				if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
				{
					status.AddWarning( "The referenced registry document is using an old schema. Please republish it with the latest schema!" );
				}
				return false;
			}
		}

		private bool ImportEnvelope( ReadEnvelope envelope, string ctdlType, SaveStatus status )
		{
			try
			{
				//
				if ( envelope != null && !string.IsNullOrWhiteSpace( envelope.EnvelopeIdentifier ) )
				{
					//LoggingHelper.DoTrace( 4, string.Format( "RegistryServices.ImportByCtid ctdlType: {0}, CTID: {1} ", envelope.EnvelopeCtdlType, envelope.EnvelopeCetermsCtid ) );
					//TODO - can we just use string envelope.EnvelopeCtdlType consistently 
					ctdlType = ctdlType.Replace( "ceterms:", "" );

					switch ( ctdlType.ToLower() )
					{
						case "credentialorganization":
						case "qacredentialorganization": //what distinctions do we need for QA orgs?
						case "organization":
							return new ImportOrganization().CustomProcessEnvelope( envelope, status );
						//break;CredentialOrganization
						case "assessmentprofile":
							return new ImportAssessment().CustomProcessEnvelope( envelope, status );
						//break;
						case "learningopportunityprofile":
							return new ImportLearningOpportunties().CustomProcessEnvelope( envelope, status );
						//break;
						case "conditionmanifest":
							return new ImportConditionManifests().CustomProcessEnvelope( envelope, status );
						//break;
						case "costmanifest":
							return new ImportCostManifests().CustomProcessEnvelope( envelope, status );
						case "competencyframework":
							return new ImportCompetencyFramesworks().CustomProcessEnvelope( envelope, status );
						case "pathway":
							return new ImportPathways().CustomProcessEnvelope( envelope, status );
						case "pathwaysset":
							return new ImportPathwaySets().CustomProcessEnvelope( envelope, status );
						case "transfervalueprofile":
						case "transfervalue":
							return new ImportTransferValue().CustomProcessEnvelope( envelope, status );

						case "job":
						case "occupation":
						case "rating":
						case "rubric":
						case "task":
						case "workrole":
						{
							//LoggingHelper.DoTrace( 1, string.Format( "ImportHelperServices.ImportEnvelope. {0} ({1}-{2}) is not handled at this time. ", ctdlType, envelope.EnvelopeCtdlType, envelope.EnvelopeCetermsCtid ) );
							return false;
						}
						//break;
						default:
							//default to credential
							//should have an edit
							return new ImportCredential().CustomProcessRequest( envelope, status );
							//break;
					}
				}
				else
				{
					status.AddError( string.Format( "Registry Envelope was empty" ) );
					return false;
				}

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "ImportCredential.ImportByCtid(). ctdlType: {0}", ctdlType ) );
				status.AddError( ex.Message );
				if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
				{
					status.AddWarning( "The referenced registry document is using an old schema. Please republish it with the latest schema!" );
				}
				return false;
			}
		}

	}
}
