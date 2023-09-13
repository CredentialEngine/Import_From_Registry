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
		#region Imports
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
					//		return new ImportHelperServices().CustomProcessRequest( envelope, status );
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
				ReadEnvelope envelope = RegistryServices.GetEnvelopeByCtid( ctid.Trim(), ref statusMessage, ref ctdlType );
				if ( envelope == null || string.IsNullOrWhiteSpace( envelope.EnvelopeType ) )
				{
					LoggingHelper.DoTrace( 4, string.Format( "ImportHelperServices.ImportByCtid(). envelope not found CTID: {0}", ctid ) );
					//this is unlikely, so try again
					envelope = RegistryServices.GetEnvelopeByCtid( ctid, ref statusMessage, ref ctdlType );
					if ( envelope != null && !string.IsNullOrWhiteSpace( envelope.EnvelopeType ) )
					{
						LoggingHelper.DoTrace( 4, string.Format( "ImportHelperServices.ImportByCtid(). ****found envelope on immediate retry. CTID: {0} ******", ctid ) );
					}

					string defCommunity = UtilityManager.GetAppKeyValue( "defaultCommunity" );
					string community = UtilityManager.GetAppKeyValue( "additionalCommunity" );
					//21-05-07 mparsons - not a good idea. Need to provide context if using alternate community, otherwise could have unexpected results.
					//if ( defCommunity != community )
					//	envelope = RegistryServices.GetEnvelopeByCtid( ctid, ref statusMessage, ref ctdlType, community );
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

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "ImportHelperServices.ImportByCtid(). ctdlType: {0}", ctdlType ) );
				status.AddError( ex.Message );
				if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
				{
					status.AddWarning( "The referenced registry document is using an old schema. Please republish it with the latest schema!" );
				}
				return false;
			}
		}
		//
		public bool ImportByURL( string resourceUrl, SaveStatus status )
		{
			if ( string.IsNullOrWhiteSpace( resourceUrl ) )
			{
				status.AddError( "ImportByURL - a valid resourceUrl must be provided" );
				return false;
			}

			string statusMessage = "";
			string ctdlType = "";
			//string payload = "";
			try
			{
				ReadEnvelope envelope = RegistryServices.GetEnvelopeByURL( resourceUrl, ref statusMessage, ref ctdlType );
				if ( envelope != null && !string.IsNullOrWhiteSpace( envelope.EnvelopeIdentifier ) )
				{
					return ImportEnvelope( envelope, ctdlType, status );
				}
				else
				{
					status.AddError( string.Format( "ImportHelperServices.ImportByURL. Registry Envelope was not found for: {0}", resourceUrl ) );
					return false;
				}

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "ImportHelperServices.ImportByURL(). ctdlType: {0}", ctdlType ) );
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
					LoggingHelper.DoTrace( 4, string.Format( "RegistryServices.ImportByCtid ctdlType: {0}, CTID: {1} ", envelope.EnvelopeCtdlType, envelope.EnvelopeCtid ) );
					//TODO - can we just use string envelope.EnvelopeCtdlType consistently 
					ctdlType = ctdlType.Replace( "ceterms:", "" );
					//
					string payload = envelope.DecodedResource.ToString();
					LoggingHelper.WriteLogFile( UtilityManager.GetAppKeyValue( "logFileTraceLevel", 5 ), envelope.EnvelopeCtid + "_" + ctdlType, payload, "", false );


					switch ( ctdlType.ToLower() )
					{
						case "credential":
						case "apprenticeshipcertificate":
						case "associatedegree":
						case "bachelordegree":
						case "badge":
						case "certificate":
						case "certificateofcompletion":
						case "participationcertificate":
						case "certification":
						case "degree":
						case "diploma":
						case "digitalbadge":
						case "doctoraldegree":
						case "generaleducationdevelopment":
						case "journeymancertificate":
						case "license":
						case "mastercertificate":
						case "masterdegree":
						case "microcredential":
						case "openbadge":
						case "professionaldoctorate":
						case "qualityassurancecredential":
						case "researchdoctorate":
						case "secondaryschooldiploma":
						case "ceterms:apprenticeshipcertificate":
						case "ceterms:associatedegree":
						case "ceterms:bachelordegree":
						case "ceterms:badge":
						case "ceterms:certificate":
						case "ceterms:certificateofcompletion":
						case "ceterms:participationcertificate":
						case "ceterms:certification":
						case "ceterms:degree":
						case "ceterms:diploma":
						case "ceterms:digitalbadge":
						case "ceterms:doctoraldegree":
						case "ceterms:generaleducationdevelopment":
						case "ceterms:journeymancertificate":
						case "ceterms:license":
						case "ceterms:mastercertificate":
						case "ceterms:masterdegree":
						case "ceterms:microcredential":
						case "ceterms:openbadge":
						case "ceterms:professionaldoctorate":
						case "ceterms:qualityassurancecredential":
						case "ceterms:researchdoctorate":
						case "ceterms:secondaryschooldiploma":
							return new ImportCredential().CustomProcessRequest( envelope, status );
						case "credentialorganization":
						case "qacredentialorganization": //what distinctions do we need for QA orgs?
						case "organization":
							return new ImportOrganization().CustomProcessEnvelope( envelope, status );
						//break;CredentialOrganization
						case "assessmentprofile":
							return new ImportAssessment().CustomProcessEnvelope( envelope, status );
						//break;
						case "learningopportunityprofile":
						case "learningprogram":
						case "course":
							return new ImportLearningOpportunties().CustomProcessEnvelope( envelope, status );
						case "collection":
							return new ImportCollections().CustomProcessEnvelope( envelope, status );
						//break;
						case "conditionmanifest":
							return new ImportConditionManifests().CustomProcessEnvelope( envelope, status );
						//break;
						case "costmanifest":
							return new ImportCostManifests().CustomProcessEnvelope( envelope, status );
						case "competencyframework":
						case "ceasn:competencyframework":
							return new ImportCompetencyFramesworks().CustomProcessEnvelope( envelope, status );
						case "conceptscheme":
						case "skos:conceptscheme":
							return new ImportConceptSchemes().CustomProcessEnvelope( envelope, status );
						case "progressionmodel":
						case "asn:progressionmodel":
							return new ImportProgressionModels().CustomProcessEnvelope( envelope, status );
						case "datasetprofile":
						case "qdata:datasetprofile":
							return new ImportDataSetProfile().CustomProcessEnvelope( envelope, status );
						case "pathway":
							return new ImportPathways().CustomProcessEnvelope( envelope, status );
						case "pathwayset":
							return new ImportPathwaySets().CustomProcessEnvelope( envelope, status );
                        case "scheduledoffering":
                            return new ImportScheduledOfferings().CustomProcessEnvelope( envelope, status );
                        case "supportservice":
                            return new ImportSupportService().CustomProcessEnvelope( envelope, status );
                        case "transferintermediary":
                            return new ImportTransferIntermediary().CustomProcessEnvelope( envelope, status );
                        case "transfervalueprofile":
						case "transfervalue":
							return new ImportTransferValue().CustomProcessEnvelope( envelope, status );
                        case "verificationserviceprofile":
                            return new ImportVerificationService().CustomProcessEnvelope( envelope, status );
                        case "occupation":
							return new ImportOccupation().CustomProcessEnvelope( envelope, status );
						case "job":
							return new ImportJob().CustomProcessEnvelope( envelope, status );
                        case "task":
                            return new ImportTask().CustomProcessEnvelope( envelope, status );
                        case "workrole":
							return new ImportWorkRole().CustomProcessEnvelope( envelope, status );
						case "ccc":
						case "rating":
						case "rubric":
							{
								LoggingHelper.DoTrace( 1, string.Format( "ImportHelperServices.ImportEnvelope. {0} ({1}-{2}) is not handled at this time. ", ctdlType, envelope.EnvelopeCtdlType, envelope.EnvelopeCtid ) );
								return false;
							}
						//break;
						default:
							//
							LoggingHelper.DoTrace( 1, string.Format( "ImportHelperServices.ImportEnvelope. {0} ({1}-{2}) is not handled at this time. ", ctdlType, envelope.EnvelopeCtdlType, envelope.EnvelopeCtid ) );
							return false;
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
				LoggingHelper.LogError( ex, string.Format( "ImportHelperServices.ImportByCtid(). ctdlType: {0}", ctdlType ) );
				status.AddError( ex.Message );
				if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
				{
					status.AddWarning( "The referenced registry document is using an old schema. Please republish it with the latest schema!" );
				}
				return false;
			}
		}
		#endregion

		#region utilities

		//
		public bool PurgeByCtid( string ctid, SaveStatus status, bool deleteOnlyNoPurge = false )
		{
			//this is currently specific, assumes envelop contains a credential
			//can use the hack fo GetResourceType to determine the type, and then call the appropriate import method

			if ( string.IsNullOrWhiteSpace( ctid ) )
			{
				status.AddError( "PurgeByCtid - a valid ctid must be provided" );
				return false;
			}
			string statusMessage = "";

			string dataOwnerCtid = "";
			string entityType = "";
			var deleteService = new ImportUtilities();
			var isValid = false;
			try
			{
				if ( deleteOnlyNoPurge )
				{
					isValid = new RegistryServices().DeleteRequest( ctid, ref statusMessage, ref entityType );
				}
				else
				{
					isValid = new RegistryServices().PurgeRequest( ctid, ref dataOwnerCtid, ref entityType, ref statusMessage );
				}

				if ( isValid )
				{
                    //might be better to let the import take care of the delete or will see an error!
                    if ( UtilityManager.GetAppKeyValue( "onPurgeOrDeleteAlsoDeleteFromDB", false ) )
					{
						deleteService.HandleDeleteRequest( 1, ctid, entityType, ref statusMessage );
					}
				}
				else
				{
                    status.AddError( statusMessage );
				}
				
				return isValid;

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "ImportHelperServices.PurgeByCtid(). CTID: {0}, Type: {1}", ctid, entityType ) );
				status.AddError( ex.Message );
				if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
				{
					status.AddWarning( "The referenced registry document is using an old schema. Please republish it with the latest schema!" );
				}
				return false;
			}
		}
		//
		public bool SetOrganizationToCeased( string ctid, SaveStatus status)
		{
			//this is currently specific, assumes envelop contains a credential
			//can use the hack fo GetResourceType to determine the type, and then call the appropriate import method

			if ( string.IsNullOrWhiteSpace( ctid ) )
			{
				status.AddError( "SetOrganizationToCeased - a valid ctid must be provided" );
				return false;
			}
			string statusMessage = "";

			string dataOwnerCtid = "";
			string entityType = "";
			var deleteService = new ImportUtilities();
			var isValid = false;
			try
			{

				{
					isValid = new RegistryServices().SetOrganizationToCeased( ctid,  ref statusMessage );
				}
				if ( isValid )
				{
					deleteService.HandleDeleteRequest( 1, ctid, entityType, ref statusMessage );
				}
				else
				{
					status.Messages.Add( new StatusMessage()
					{
						Message = statusMessage
					} );
				}

				return isValid;

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "ImportHelperServices.PurgeByCtid(). CTID: {0}, Type: {1}", ctid, entityType ) );
				status.AddError( ex.Message );
				if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
				{
					status.AddWarning( "The referenced registry document is using an old schema. Please republish it with the latest schema!" );
				}
				return false;
			}
		}
		//
		#endregion
	}
}
