using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using workIT.Utilities;

using EntityServices = workIT.Services.OrganizationServices;
using InputEntity = RA.Models.Json.Agent;

using InputEntityV3 = RA.Models.JsonV3.Agent;
using BNodeV3 = RA.Models.JsonV3.BlankNode;
using ThisEntity = workIT.Models.Common.Organization;
using workIT.Models.Common;
using workIT.Factories;
using workIT.Models;
using workIT.Models.ProfileModels;

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
		public bool RequestImportByEnvelopeId( string envelopeId, SaveStatus status )
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
					return CustomProcessEnvelope( envelope, status );
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
		public bool RequestImportByCtid( string ctid, SaveStatus status )
		{
			if ( string.IsNullOrWhiteSpace( ctid ) )
			{
				status.AddError( thisClassName + ".ImportByCtid - a valid ctid must be provided" );
				return false;
			}

			//this is currently specific, assumes envelop contains a credential
			//can use the hack for GetResourceType to determine the type, and then call the appropriate import method
			string statusMessage = "";
			string ctdlType = "";
			try
			{
				//probably always want to get by envelope
				ReadEnvelope envelope = RegistryServices.GetEnvelopeByCtid( ctid, ref statusMessage, ref ctdlType );
				if ( envelope != null && !string.IsNullOrWhiteSpace( envelope.EnvelopeIdentifier ) )
				{
					return CustomProcessEnvelope( envelope, status );
				}
				else
					return false;
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
			//EntityServices mgr = new EntityServices();
			string ctdlType = "";
			try
			{
				string payload = RegistryServices.GetResourceByUrl(resourceUrl, ref ctdlType, ref statusMessage );

				if ( !string.IsNullOrWhiteSpace( payload ) )
				{
                    if ( ImportServiceHelpers.IsAGraphResource( payload ) )
                    {
                        //if ( payload.IndexOf( "\"en\":" ) > 0 )
                            return ImportV3( payload, "", status );
                        //else
                        //    return ImportV2( payload, "", status );
                    }
                    else
                    {
                        input = JsonConvert.DeserializeObject<InputEntity>( payload.ToString() );
                        return Import( input, "", status );
                    }
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
			//EntityServices mgr = new EntityServices();
            if ( ImportServiceHelpers.IsAGraphResource( payload ) )
            {
				//if ( payload.IndexOf( "\"en\":" ) > 0 )
				return ImportV3( payload, "", status );
				//else
				//    return ImportV2( payload, "", status );
			}
			else
            {
                //do additional check in case of getting just the resource instead of /graph/
                input = JsonConvert.DeserializeObject<InputEntity>( payload );
                return Import( input, "", status );
            }
            
		}
		#endregion
		/// <summary>
		/// Custom version, typically called outside a scheduled import
		/// </summary>
		/// <param name="item"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool CustomProcessEnvelope( ReadEnvelope item, SaveStatus status )
		{
			EntityServices mgr = new EntityServices();
            bool importSuccessfull = ProcessEnvelope( item, status );
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
		public bool ProcessEnvelope( ReadEnvelope item, SaveStatus status )
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

            if ( ImportServiceHelpers.IsAGraphResource( payload ) )
            {
                //if ( payload.IndexOf( "\"en\":" ) > 0 )
                   return ImportV3( payload, envelopeIdentifier, status );
                //else
                //    return ImportV2( payload, envelopeIdentifier, status );
            }
            else
            {
                LoggingHelper.DoTrace( 5, "		envelopeUrl: " + envelopeUrl );
                LoggingHelper.WriteLogFile( 1, "org_" + item.EnvelopeIdentifier, payload, "", false );
                input = JsonConvert.DeserializeObject<InputEntity>( item.DecodedResource.ToString() );

                return Import( input, envelopeIdentifier, status );
            }
		}
	
		public bool Import( InputEntity input, string envelopeIdentifier, SaveStatus status )
		{
            List<string> messages = new List<string>();
            bool importSuccessfull = false;
            EntityServices mgr = new EntityServices();

            string ctid = input.Ctid;
            string referencedAtId = input.CtdlId;

            LoggingHelper.DoTrace( 5, "		name: " + input.Name );
            LoggingHelper.DoTrace( 6, "		url: " + input.SubjectWebpage);
			LoggingHelper.DoTrace( 5, "		ctid: " + input.Ctid );
            LoggingHelper.DoTrace( 5, "		@Id: " + input.CtdlId );
            status.Ctid = ctid;

            if ( status.DoingDownloadOnly )
                return true;

            if ( !DoesEntityExist( input.Ctid, ref output ) )
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
			output.AlternateNames = MappingHelper.MapToTextValueProfile( input.AlternateName );
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
            if ( !string.IsNullOrWhiteSpace( input.LEICode ) )
                output.IdentificationCodes.Add( new workIT.Models.ProfileModels.TextValueProfile { CodeSchema = "ceterms:leiCode", TextValue = input.LEICode } );
            //alternativeidentifier - should just be added to IdentificationCodes
            output.AlternativeIdentifier = MappingHelper.MapIdentifierValueListToString( input.AlternativeIdentifier );
			output.AlternativeIdentifierList = MappingHelper.MapIdentifierValueList( input.AlternativeIdentifier );

			//email

			output.Emails = MappingHelper.MapToTextValueProfile( input.Email );
            //contact point - now in address
            //output.ContactPoint = MappingHelper.FormatContactPoints( input.ContactPoint, ref status );
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
            output.Accredits = MappingHelper.MapEntityReferenceGuids( input.Accredits, 0, ref status );
            output.Approves = MappingHelper.MapEntityReferenceGuids( input.Approves, 0, ref status );
			output.Offers = MappingHelper.MapEntityReferenceGuids( input.Offers, 0, ref status );
			output.Owns = MappingHelper.MapEntityReferenceGuids( input.Owns, 0, ref status );
			output.Renews = MappingHelper.MapEntityReferenceGuids( input.Renews, 0, ref status );
			output.Revokes = MappingHelper.MapEntityReferenceGuids( input.Revokes, 0, ref status );
			output.Recognizes = MappingHelper.MapEntityReferenceGuids( input.Recognizes, 0, ref status );
            output.Regulates = MappingHelper.MapEntityReferenceGuids( input.Regulates, 0, ref status );

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

        public bool ImportV3( string payload, string envelopeIdentifier, SaveStatus status )
        {
            InputEntityV3 input = new InputEntityV3();
            var bnodes = new List<BNodeV3>();
            var mainEntity = new Dictionary<string, object>();

            Dictionary<string, object> dictionary = RegistryServices.JsonToDictionary( payload );
            object graph = dictionary[ "@graph" ];
            //serialize the graph object
            var glist = JsonConvert.SerializeObject( graph );

            //parse graph in to list of objects
            JArray graphList = JArray.Parse( glist );
            int cntr = 0;
            foreach ( var item in graphList )
            {
                cntr++;
                if ( cntr == 1 )
                {
                    var main = item.ToString();
                    //may not use this. Could add a trace method
                    mainEntity = RegistryServices.JsonToDictionary( main );
                    input = JsonConvert.DeserializeObject<InputEntityV3>( main );
                }
                else //is this too much of an assumption?
                {
                    var bn = item.ToString();
                    bnodes.Add( JsonConvert.DeserializeObject<BNodeV3>( bn ) );
                }

            }
  
            List<string> messages = new List<string>();
            bool importSuccessfull = false;
            EntityServices mgr = new EntityServices();
            MappingHelperV3 helper = new MappingHelperV3();
            helper.entityBlankNodes = bnodes;

            string ctid = input.Ctid;
            string referencedAtId = input.CtdlId;

            LoggingHelper.DoTrace( 5, "		name: " + input.Name.ToString() );
            LoggingHelper.DoTrace( 6, "		url: " + input.SubjectWebpage );
            LoggingHelper.DoTrace( 5, "		ctid: " + input.Ctid );
            LoggingHelper.DoTrace( 5, "		@Id: " + input.CtdlId );
            status.Ctid = ctid;

            if ( status.DoingDownloadOnly )
                return true;

            if ( !DoesEntityExist( input.Ctid, ref output ) )
            {
                //set the rowid now, so that can be referenced as needed
                output.RowId = Guid.NewGuid();
            }

            helper.currentBaseObject = output;

            output.AgentDomainType = input.Type;
            output.Name = helper.HandleLanguageMap( input.Name, output, "Name" );
            output.Description = helper.HandleLanguageMap( input.Description, output, "Description" );
            //map from idProperty to url
            output.SubjectWebpage = input.SubjectWebpage;
            output.CTID = input.Ctid;
            output.CredentialRegistryId = envelopeIdentifier;
            output.AlternateNames = helper.MapToTextValueProfile( input.AlternateName, output, "AlternateName" );
            output.ImageUrl = input.Image;

            output.AgentPurpose = input.AgentPurpose;
            output.AgentPurposeDescription = helper.HandleLanguageMap( input.AgentPurposeDescription, output, "AgentPurposeDescription" );

            output.FoundingDate = input.FoundingDate;
            output.AvailabilityListing = helper.MapListToString( input.AvailabilityListing );
            //future prep
            output.AvailabilityListings = input.AvailabilityListing;

            output.MissionAndGoalsStatement = input.MissionAndGoalsStatement;
            output.MissionAndGoalsStatementDescription = input.MissionAndGoalsStatementDescription;

            output.Addresses = helper.FormatAvailableAtAddresses( input.Address, ref status );
			if ( UtilityManager.GetAppKeyValue( "skipOppImportIfNoShortRegions", false ) )
			{
				if ( output.Addresses.Count == 0 )
				{
					//skip
					LoggingHelper.DoTrace( 2, string.Format( "		***Skipping org# {0}, {1} as it has no addresses and this is a special run.", output.Id, output.Name ) );
					return true;
				} else if (output.HasAnyShortRegions == false)
				{
					//skip
					LoggingHelper.DoTrace( 2, string.Format( "		***Skipping org# {0}, {1} as it has no addresses with short regions and this is a special run.", output.Id, output.Name ) );
					return true;
				}
			}

			//agent type, map to enumeration
			output.AgentType = helper.MapCAOListToEnumermation( input.AgentType );


            //Manifests
            output.ConditionManifestIds = helper.MapEntityReferences( input.HasConditionManifest, CodesManager.ENTITY_TYPE_CONDITION_MANIFEST, CodesManager.ENTITY_TYPE_ORGANIZATION, ref status );
            output.CostManifestIds = helper.MapEntityReferences( input.HasCostManifest, CodesManager.ENTITY_TYPE_COST_MANIFEST, CodesManager.ENTITY_TYPE_ORGANIZATION, ref status );

            //hasVerificationService
            output.VerificationServiceProfiles = helper.MapVerificationServiceProfiles( input.VerificationServiceProfiles, ref status );

            // output.targetc
            //other enumerations
            //	serviceType, AgentSectorType
            output.ServiceType = helper.MapCAOListToEnumermation( input.ServiceType );
            output.AgentSectorType = helper.MapCAOListToEnumermation( input.AgentSectorType );

            //Industries
            //output.Industry = helper.MapCAOListToEnumermation( input.IndustryType );
            output.Industries = helper.MapCAOListToCAOProfileList( input.IndustryType );
            //naics
            output.Naics = input.Naics;

            //keywords
            output.Keyword = helper.MapToTextValueProfile( input.Keyword, output, "Keyword" );

            //duns, Fein.  IpedsID, opeID
            if ( !string.IsNullOrWhiteSpace( input.DUNS ) )
                output.IdentificationCodes.Add( new workIT.Models.ProfileModels.TextValueProfile { CodeSchema = "ceterms:duns", TextValue = input.DUNS } );
            if ( !string.IsNullOrWhiteSpace( input.FEIN ) )
                output.IdentificationCodes.Add( new workIT.Models.ProfileModels.TextValueProfile { CodeSchema = "ceterms:fein", TextValue = input.FEIN } );

            if ( !string.IsNullOrWhiteSpace( input.IpedsID ) )
                output.IdentificationCodes.Add( new workIT.Models.ProfileModels.TextValueProfile { CodeSchema = "ceterms:ipedsID", TextValue = input.IpedsID } );

            if ( !string.IsNullOrWhiteSpace( input.OPEID ) )
                output.IdentificationCodes.Add( new workIT.Models.ProfileModels.TextValueProfile { CodeSchema = "ceterms:opeID", TextValue = input.OPEID } );
            if ( !string.IsNullOrWhiteSpace( input.LEICode ) )
                output.IdentificationCodes.Add( new workIT.Models.ProfileModels.TextValueProfile { CodeSchema = "ceterms:leiCode", TextValue = input.LEICode } );
            //alternativeidentifier - should just be added to IdentificationCodes
            output.AlternativeIdentifier = helper.MapIdentifierValueListToString( input.AlternativeIdentifier );
            output.AlternativeIdentifierList = helper.MapIdentifierValueList( input.AlternativeIdentifier );

            //email

            output.Emails = helper.MapToTextValueProfile( input.Email );
            //contact point
            //output.ContactPoint = helper.FormatContactPoints( input.ContactPoint, ref status );
            //Jurisdiction
            output.Jurisdiction = helper.MapToJurisdiction( input.Jurisdiction, ref status );

            //SameAs
            output.SameAs = helper.MapToTextValueProfile( input.SameAs );
            //Social media
            output.SocialMediaPages = helper.MapToTextValueProfile( input.SocialMedia );

            //departments
            //not sure - MP - want to change how depts, and subs are handled
            //output.ParentOrganization = helper.MapOrganizationReferenceGuids( input.ParentOrganization, ref status );
            output.Departments = helper.MapOrganizationReferenceGuids( input.Department, ref status );
            output.SubOrganizations = helper.MapOrganizationReferenceGuids( input.SubOrganization, ref status );

            //output.OrganizationRole_Subsidiary = helper.FormatOrganizationReferences( input.SubOrganization );

            //Process profiles
            output.AdministrationProcess = helper.FormatProcessProfile( input.AdministrationProcess, ref status );
            output.MaintenanceProcess = helper.FormatProcessProfile( input.MaintenanceProcess, ref status );
            output.ComplaintProcess = helper.FormatProcessProfile( input.ComplaintProcess, ref status );
            output.DevelopmentProcess = helper.FormatProcessProfile( input.DevelopmentProcess, ref status );
            output.RevocationProcess = helper.FormatProcessProfile( input.RevocationProcess, ref status );
            output.ReviewProcess = helper.FormatProcessProfile( input.ReviewProcess, ref status );
            output.AppealProcess = helper.FormatProcessProfile( input.AppealProcess, ref status );

            //BYs
            output.AccreditedBy = helper.MapOrganizationReferenceGuids( input.AccreditedBy, ref status );
            output.ApprovedBy = helper.MapOrganizationReferenceGuids( input.ApprovedBy, ref status );
            output.RecognizedBy = helper.MapOrganizationReferenceGuids( input.RecognizedBy, ref status );
            output.RegulatedBy = helper.MapOrganizationReferenceGuids( input.RegulatedBy, ref status );
            //INs
            output.AccreditedIn = helper.MapToJurisdiction( input.AccreditedIn, ref status );
            output.ApprovedIn = helper.MapToJurisdiction( input.ApprovedIn, ref status );
            output.RecognizedIn = helper.MapToJurisdiction( input.RecognizedIn, ref status );
            output.RegulatedIn = helper.MapToJurisdiction( input.RegulatedIn, ref status );

            //Asserts
            //the entity type is not known
            output.Accredits = helper.MapEntityReferenceGuids( input.Accredits, 0, ref status );
            output.Approves = helper.MapEntityReferenceGuids( input.Approves, 0, ref status );
            if ( output.Approves.Count > 0 )
            {

            }
            output.Offers = helper.MapEntityReferenceGuids( input.Offers, 0, ref status );
            output.Owns = helper.MapEntityReferenceGuids( input.Owns, 0, ref status );
            output.Renews = helper.MapEntityReferenceGuids( input.Renews, 0, ref status );
            output.Revokes = helper.MapEntityReferenceGuids( input.Revokes, 0, ref status );
            output.Recognizes = helper.MapEntityReferenceGuids( input.Recognizes, 0, ref status );
            output.Regulates = helper.MapEntityReferenceGuids( input.Regulates, 0, ref status );

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

        public bool DoesEntityExist( string ctid, ref ThisEntity entity )
        {
            bool exists = false;
            entity = EntityServices.GetByCtid( ctid );
            if ( entity != null && entity.Id > 0 )
                return true;

            return exists;
        }
    }
}
