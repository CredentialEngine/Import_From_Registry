using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using workIT.Utilities;

using EntityServices = workIT.Services.OrganizationServices;
using InputEntity = RA.Models.Json.Agent;
using ThisEntity = workIT.Models.Common.Organization;
using workIT.Models.Common;
using workIT.Factories;
using workIT.Models;

namespace Import.Services
{
    public class ImportOrganization
    {
        int entityTypeId = CodesManager.ENTITY_TYPE_ORGANIZATION;
		string thisClassName = "ImportOrganization";
		ImportManager importManager = new ImportManager();
        ImportServiceHelpers importHelper = new ImportServiceHelpers();
        InputEntity input = new InputEntity();
		ThisEntity output = new ThisEntity();

		#region custom imports
		public void ImportPendingRecords()
		{
			string registryFilter = string.Format( UtilityManager.GetAppKeyValue( "credentialRegistryResource", "http://lr-staging.learningtapestry.com/resources/{0}" ), "ce-%" );
            //string where = string.Format( " [SubjectWebpage] like '{0}' ", registryFilter );
            string where = " [EntityStateId] = 1 ";
            int pTotalRows = 0;

			SaveStatus status = new SaveStatus();
			List<OrganizationSummary> list = OrganizationManager.MainSearch( where, "", 1, 500, ref pTotalRows );
			LoggingHelper.DoTrace( 1, string.Format( thisClassName + " - ImportPendingRecords(). Processing {0} records =================", pTotalRows) );
			foreach ( OrganizationSummary item in list )
			{
				status = new SaveStatus();
                //SWP contains the resource url

                if (!ImportByResourceUrl( item.SubjectWebpage, status ))
                {
                    //check for 404
                    LoggingHelper.DoTrace(1, string.Format("     - (). Failed to import pending credential: {0}, message(s): {1}", item.Id, status.GetErrorsAsString()));
                }
                else
                    LoggingHelper.DoTrace(1, string.Format("     - (). Successfully imported pending credential: {0}", item.Id));
            }
		}
		/// <summary>
		/// Retrieve an envelop from the registry and do import
		/// </summary>
		/// <param name="envelopeId"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool ImportByEnvelopeId( string envelopeId, SaveStatus status )
		{
			//this is currently specific, assumes envelop contains an organization
			//can use the hack fo GetResourceType to determine the type, and then call the appropriate import method

			if ( string.IsNullOrWhiteSpace( envelopeId ) )
			{
				status.AddError( thisClassName + ".ImportByEnvelope - a valid envelope id must be provided" );
				return false;
			}

			string statusMessage = "";
			EntityServices mgr = new EntityServices();
			string ctdlType = "";
			try
			{
				ReadEnvelope envelope = RegistryServices.GetEnvelope( envelopeId, ref statusMessage, ref ctdlType );
				if ( envelope != null && !string.IsNullOrWhiteSpace( envelope.EnvelopeIdentifier ) )
				{
					return ProcessEnvelope( envelope, status );
				}
				else
					return false;
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".ImportByEnvelopeId()" );
				status.AddError( ex.Message );
				if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
				{
					status.AddWarning( "The referenced registry document is using an old schema. Please republish it with the latest schema!" );
				}
				return false;
			}
		}

		/// <summary>
		/// Retrieve an resource from the registry by ctid and do import
		/// </summary>
		/// <param name="ctid"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool ImportByCtid( string ctid, SaveStatus status )
		{
			if ( string.IsNullOrWhiteSpace( ctid ) )
			{
				status.AddError( thisClassName + ".ImportByCtid - a valid ctid must be provided" );
				return false;
			}

			//this is currently specific, assumes envelop contains a credential
			//can use the hack for GetResourceType to determine the type, and then call the appropriate import method
			string statusMessage = "";
			EntityServices mgr = new EntityServices();
			string ctdlType = "";
			try
			{
				//probably always want to get by envelope
				ReadEnvelope envelope = RegistryServices.GetEnvelopeByCtid( ctid, ref statusMessage, ref ctdlType );
				if ( envelope != null && !string.IsNullOrWhiteSpace( envelope.EnvelopeIdentifier ) )
				{
					return ProcessEnvelope( envelope, status );
				}
				else
					return false;
				//string payload = RegistryServices.GetResourceByCtid( ctid, ref ctdlType, ref statusMessage );

				//if ( !string.IsNullOrWhiteSpace( payload ) )
				//{
				//	input = JsonConvert.DeserializeObject<InputEntity>( payload.ToString() );
				//	//ctdlType = RegistryServices.GetResourceType( payload );
				//	return Import( mgr, input, "", status );
				//}
				//else
				//	return false;
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".ImportByCtid(). CTID: {0}", ctid ) );
				status.AddError( ex.Message );
				if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
				{
					status.AddWarning( "The referenced registry document is using an old schema. Please republish it with the latest schema!" );
				}
				return false;
			}
		}
		public bool ImportByResourceUrl( string resourceUrl, SaveStatus status )
		{
			if ( string.IsNullOrWhiteSpace(resourceUrl) )
			{
				status.AddError( thisClassName + ".ImportByResourceUrl - a valid resourceUrl must be provided");
				return false;
			}
			//this is currently specific, assumes envelop contains an organization
			//can use the hack for GetResourceType to determine the type, and then call the appropriate import method
			string statusMessage = "";
			EntityServices mgr = new EntityServices();
			string ctdlType = "";
			try
			{
				string payload = RegistryServices.GetResourceByUrl(resourceUrl, ref ctdlType, ref statusMessage );

				if ( !string.IsNullOrWhiteSpace( payload ) )
				{
					input = JsonConvert.DeserializeObject<InputEntity>( payload.ToString() );
					return Import( mgr, input, "", status );
				}
				else
					return false;
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".ImportByResourceUrl()" );
				status.AddError( ex.Message );
				if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
				{
					status.AddWarning( "The referenced registry document is using an old schema. Please republish it with the latest schema!" );
				}
				return false;
			}
		}
		public bool ImportByPayload( string payload, SaveStatus status )
		{
			EntityServices mgr = new EntityServices();
			input = JsonConvert.DeserializeObject<InputEntity>( payload );

			return Import( mgr, input, "", status );
		}
		#endregion

		public bool ProcessEnvelope( ReadEnvelope item, SaveStatus status )
		{
			EntityServices mgr = new EntityServices();
            bool importSuccessfull = ProcessEnvelope( mgr, item, status );
            List<string> messages = new List<string>();
            string importError = string.Join( "\r\n", status.GetAllMessages().ToArray() );
            //store envelope
            int newImportId = importHelper.Add( item, 2, status.Ctid, importSuccessfull, importError, ref messages );
            if ( newImportId > 0 && status.Messages != null && status.Messages.Count > 0 )
            {
                //add indicator of current recored
                string msg = string.Format( "========= Messages for Organization, EnvelopeIdentifier: {0}, ctid: {1}, Id: {2}, rowId: {3} =========", item.EnvelopeIdentifier, status.Ctid, status.DocumentId, status.DocumentRowId );
                importHelper.AddMessages( newImportId, status, ref messages );
            }
            return importSuccessfull;
        }
		public bool ProcessEnvelope( EntityServices mgr, ReadEnvelope item, SaveStatus status )
		{
			if ( item == null || string.IsNullOrWhiteSpace( item.EnvelopeIdentifier ) )
			{
				status.AddError( "A valid ReadEnvelope must be provided." );
				return false;
			}

			//EntityServices mgr = new EntityServices();
			string payload = item.DecodedResource.ToString();
			string envelopeIdentifier = item.EnvelopeIdentifier;
			string ctdlType = RegistryServices.GetResourceType( payload );
			string envelopeUrl = RegistryServices.GetEnvelopeUrl( envelopeIdentifier );
			LoggingHelper.DoTrace( 5, "		envelopeUrl: " + envelopeUrl );
			LoggingHelper.WriteLogFile( 1, item.EnvelopeIdentifier + "_org", payload, "", false );
			input = JsonConvert.DeserializeObject<InputEntity>( item.DecodedResource.ToString() );

			return Import( mgr, input, envelopeIdentifier, status );
		}
	
		public bool Import( EntityServices mgr, InputEntity input, string envelopeIdentifier, SaveStatus status )
		{
            List<string> messages = new List<string>();
            bool importSuccessfull = false;

            //try
            //{
            //input = JsonConvert.DeserializeObject<InputEntity>( item.DecodedResource.ToString() );
            string ctid = input.Ctid;
            string referencedAtId = input.CtdlId;

            LoggingHelper.DoTrace( 5, "		name: " + input.Name );
            LoggingHelper.DoTrace( 6, "		url: " + input.SubjectWebpage);
			LoggingHelper.DoTrace( 5, "		ctid: " + input.Ctid );
            LoggingHelper.DoTrace( 5, "		@Id: " + input.CtdlId );
            status.Ctid = ctid;

            if ( status.DoingDownloadOnly )
                return true;

            if ( !DoesEntityExist( input, ref output ) )
            {
                //set the rowid now, so that can be referenced as needed
                output.RowId = Guid.NewGuid();
            }


            //re:messages - currently passed to mapping but no errors are trapped??
            //				- should use SaveStatus and skip import if errors encountered (vs warnings)

            output.AgentDomainType = input.Type;
            output.Name = input.Name;
            output.Description = input.Description;
            //map from idProperty to url
            output.SubjectWebpage = input.SubjectWebpage;
            output.CTID = input.Ctid;
            output.CredentialRegistryId = envelopeIdentifier;
			//output.AlternateNames = input.AlternateName;
			output.AlternateName = MappingHelper.MapToTextValueProfile( input.AlternateName );
			output.ImageUrl = input.Image;

            output.AgentPurpose = input.AgentPurpose;
            output.AgentPurposeDescription = input.AgentPurposeDescription;

            output.FoundingDate = input.FoundingDate;
            output.AvailabilityListing = MappingHelper.MapListToString( input.AvailabilityListing );
            //future prep
            output.AvailabilityListings = input.AvailabilityListing;

            output.MissionAndGoalsStatement = input.MissionAndGoalsStatement;
            output.MissionAndGoalsStatementDescription = input.MissionAndGoalsStatementDescription;

            output.Addresses = MappingHelper.FormatAddresses( input.Address, ref status );

            //agent type, map to enumeration
            output.AgentType = MappingHelper.MapCAOListToEnumermation( input.AgentType );
          

            //Manifests
            output.ConditionManifestIds = MappingHelper.MapEntityReferences( input.HasConditionManifest, CodesManager.ENTITY_TYPE_CONDITION_MANIFEST, CodesManager.ENTITY_TYPE_ORGANIZATION,  ref status );
            output.CostManifestIds = MappingHelper.MapEntityReferences( input.HasCostManifest,  CodesManager.ENTITY_TYPE_COST_MANIFEST, CodesManager.ENTITY_TYPE_ORGANIZATION, ref status );

            //hasVerificationService
            output.VerificationServiceProfiles = MappingHelper.MapVerificationServiceProfiles( input.VerificationServiceProfiles, ref status);

            // output.targetc
            //other enumerations
            //	serviceType, AgentSectorType
            output.ServiceType = MappingHelper.MapCAOListToEnumermation( input.ServiceType );
            output.AgentSectorType = MappingHelper.MapCAOListToEnumermation( input.AgentSectorType );

            //Industries
            output.Industry = MappingHelper.MapCAOListToEnumermation( input.IndustryType );
			output.Industries = MappingHelper.MapCAOListToFramework( input.IndustryType );
			//naics
			output.Naics = input.Naics;

			//keywords
			output.Keyword = MappingHelper.MapToTextValueProfile( input.Keyword );

            //duns, Fein.  IpedsID, opeID
            if ( !string.IsNullOrWhiteSpace( input.DUNS ) )
                output.IdentificationCodes.Add( new workIT.Models.ProfileModels.TextValueProfile { CodeSchema = "ceterms:duns", TextValue = input.DUNS } );
            if ( !string.IsNullOrWhiteSpace( input.FEIN ) )
                output.IdentificationCodes.Add( new workIT.Models.ProfileModels.TextValueProfile { CodeSchema = "ceterms:fein", TextValue = input.FEIN } );

            if ( !string.IsNullOrWhiteSpace( input.IpedsID ) )
                output.IdentificationCodes.Add( new workIT.Models.ProfileModels.TextValueProfile { CodeSchema = "ceterms:ipedsID", TextValue = input.IpedsID } );

            if ( !string.IsNullOrWhiteSpace( input.OPEID ) )
                output.IdentificationCodes.Add( new workIT.Models.ProfileModels.TextValueProfile { CodeSchema = "ceterms:opeID", TextValue = input.OPEID } );
			//alternativeidentifier - should just be added to IdentificationCodes
			output.AlternativeIdentifier = MappingHelper.MapIdentifierValueListToString( input.AlternativeIdentifier );
			output.AlternativeIdentifierList = MappingHelper.MapIdentifierValueList( input.AlternativeIdentifier );

			//email

			output.Emails = MappingHelper.MapToTextValueProfile( input.Email );
            //contact point
            output.ContactPoint = MappingHelper.FormatContactPoints( input.ContactPoint, ref status );
            //Jurisdiction
            output.Jurisdiction = MappingHelper.MapToJurisdiction( input.Jurisdiction, ref status );

			//SameAs
			output.SameAs = MappingHelper.MapToTextValueProfile( input.SameAs );
            //Social media
            output.SocialMediaPages = MappingHelper.MapToTextValueProfile( input.SocialMedia );

			//departments
			//not sure - MP - want to change how depts, and subs are handled
			//output.ParentOrganization = MappingHelper.MapOrganizationReferenceGuids( input.ParentOrganization, ref status );
			output.Departments = MappingHelper.MapOrganizationReferenceGuids( input.Department, ref status );
			output.SubOrganizations = MappingHelper.MapOrganizationReferenceGuids( input.SubOrganization, ref status );

			//output.OrganizationRole_Subsidiary = MappingHelper.FormatOrganizationReferences( input.SubOrganization );

			//Process profiles
			output.AdministrationProcess = MappingHelper.FormatProcessProfile( input.AdministrationProcess, ref status );
            output.MaintenanceProcess = MappingHelper.FormatProcessProfile( input.MaintenanceProcess, ref status );
            output.ComplaintProcess = MappingHelper.FormatProcessProfile( input.ComplaintProcess, ref status );
            output.DevelopmentProcess = MappingHelper.FormatProcessProfile( input.DevelopmentProcess, ref status );
            output.RevocationProcess = MappingHelper.FormatProcessProfile( input.RevocationProcess, ref status );
            output.ReviewProcess = MappingHelper.FormatProcessProfile( input.ReviewProcess, ref status );
            output.AppealProcess = MappingHelper.FormatProcessProfile( input.AppealProcess, ref status );

			//BYs
			output.AccreditedBy = MappingHelper.MapOrganizationReferenceGuids( input.AccreditedBy, ref status );
			output.ApprovedBy = MappingHelper.MapOrganizationReferenceGuids( input.ApprovedBy, ref status );
			output.RecognizedBy = MappingHelper.MapOrganizationReferenceGuids( input.RecognizedBy, ref status );
			output.RegulatedBy = MappingHelper.MapOrganizationReferenceGuids( input.RegulatedBy, ref status );
			//INs
			output.AccreditedIn = MappingHelper.MapToJurisdiction( input.AccreditedIn, ref status );
			output.ApprovedIn = MappingHelper.MapToJurisdiction( input.ApprovedIn, ref status );
			output.RecognizedIn = MappingHelper.MapToJurisdiction( input.RecognizedIn, ref status );
			output.RegulatedIn = MappingHelper.MapToJurisdiction( input.RegulatedIn, ref status );

			//Asserts
			//the entity type is not known
			output.Approves = MappingHelper.MapEntityReferenceGuids( input.Approves, 0, ref status );
			output.Offers = MappingHelper.MapEntityReferenceGuids( input.Offers, 0, ref status );
			output.Owns = MappingHelper.MapEntityReferenceGuids( input.Owns, 0, ref status );
			output.Renews = MappingHelper.MapEntityReferenceGuids( input.Renews, 0, ref status );
			output.Revokes = MappingHelper.MapEntityReferenceGuids( input.Revokes, 0, ref status );
			output.Recognizes = MappingHelper.MapEntityReferenceGuids( input.Recognizes, 0, ref status );

			//Ins - defer to later    

			

			//=== if any messages were encountered treat as warnings for now
			if ( messages.Count > 0 )
                status.SetMessages( messages, true );
			//just in case check if entity added since start
			if ( output.Id == 0 )
			{
				ThisEntity entity = EntityServices.GetByCtid( ctid );
				if ( entity != null && entity.Id > 0 )
				{
					output.Id = entity.Id;
					output.RowId = entity.RowId;
				}
			}
			importSuccessfull = mgr.Import( output, ref status );

			status.DocumentId = output.Id;
			status.DetailPageUrl = string.Format( "~/organization/{0}", output.Id );
			status.DocumentRowId = output.RowId;

			//just in case
			if ( status.HasErrors )
                importSuccessfull = false;

            //if record was added to db, add to/or set EntityResolution as resolved
            int ierId = new ImportManager().Import_EntityResolutionAdd( referencedAtId,
                        ctid, CodesManager.ENTITY_TYPE_ORGANIZATION,
                        output.RowId,
                        output.Id,
                        false,
                        ref messages,
                        output.Id > 0 );

            return importSuccessfull;
        }


        public bool DoesEntityExist( InputEntity jsonEntity, ref ThisEntity entity )
        {
            bool exists = false;
            entity = EntityServices.GetByCtid( jsonEntity.Ctid );
            if ( entity != null && entity.Id > 0 )
                return true;

            return exists;
        }
    }
    }
