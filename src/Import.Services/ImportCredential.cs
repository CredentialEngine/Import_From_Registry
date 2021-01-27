using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using workIT.Utilities;

using EntityServices = workIT.Services.CredentialServices;
using InputEntity = RA.Models.Json.Credential;

//input document from registry
using InputEntityV3 = RA.Models.JsonV2.Credential;
using JsonInput = RA.Models.JsonV2;
using BNode = RA.Models.JsonV2.BlankNode;
//target local storage class
using ThisEntity = workIT.Models.Common.Credential;
using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Services;

namespace Import.Services
{
	public class ImportCredential
	{
		ImportManager importManager = new ImportManager();
        ImportServiceHelpers importHelper = new ImportServiceHelpers();
        InputEntity input = new InputEntity();
		ThisEntity output = new ThisEntity();

		int thisEntityTypeId = CodesManager.ENTITY_TYPE_CREDENTIAL;
		string thisClassName = "ImportCredential";

		/// <summary>
		/// attempt to resolve pending (EntityStateId = 1) records
		/// </summary>
		public void ImportPendingRecords()
		{

			SaveStatus status = new SaveStatus();
			List<Credential> list = CredentialManager.GetPending();
			LoggingHelper.DoTrace( 1, string.Format( thisClassName + " - ImportPendingRecords(). Processing {0} records =================", list.Count()) );

			foreach ( Credential item in list )
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
		}   //

		/// <summary>
		/// Retrieve an envelop from the registry and do import
		/// </summary>
		/// <param name="envelopeId"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool ImportByEnvelopeId( string envelopeId, SaveStatus status )
		{
			//this is currently specific, assumes envelop contains a credential
			//can use the hack fo GetResourceType to determine the type, and then call the appropriate import method

			if ( string.IsNullOrWhiteSpace( envelopeId ) )
			{
				status.AddError( "ImportByEnvelope - a valid envelope id must be provided" );
				return false;
			}

			string statusMessage = "";
			//EntityServices mgr = new EntityServices();
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
			} catch (Exception ex)
			{
				LoggingHelper.LogError( ex, "ImportCredential.ImportByEnvelopeId()" );
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
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + string.Format(".ImportByCtid(). CTID: {0}", ctid) );
				status.AddError( ex.Message );
				if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
				{
					status.AddWarning( "The referenced registry document is using an old schema. Please republish it with the latest schema!" );
				}
				return false;
			}
		}

        /// <summary>
        /// NOTE: this will have to be updated to get by /graph/
        /// </summary>
        /// <param name="resourceUrl"></param>
        /// <param name="status"></param>
        /// <returns></returns>
		public bool ImportByResourceUrl( string resourceUrl, SaveStatus status )
		{
			//this is currently specific, assumes envelop contains a credential
			//can use the hack fo GetResourceType to determine the type, and then call the appropriate import method
			string statusMessage = "";
			EntityServices mgr = new EntityServices();
			string ctdlType = "";
			string payload = "";
			try
			{
				payload = RegistryServices.GetResourceByUrl( resourceUrl, ref ctdlType, ref statusMessage );

				if ( !string.IsNullOrWhiteSpace( payload ) )
				{
					return ImportV3( payload, "", status );
                }
				else
				{
					status.AddError( statusMessage );
					return false;
				}
			}
			catch ( Exception ex )
			{
				if ( ex.Message.IndexOf( "Unexpected character encountered while parsing" ) > -1 )
				{
					//usually indicates the schema is old
					//now what
					Dictionary<string, object> dictionary = RegistryServices.JsonToDictionary( payload );
				}
				else
				{
					LoggingHelper.LogError( ex, thisClassName + ".ImportByResourceUrl()" );
				}
				
				return false;
			}
		}

        public bool ImportByPayload( string payload, SaveStatus status )
		{
            //if ( ImportServiceHelpers.IsAGraphResource( payload ) )
            //{
                    return ImportV3( payload, "", status );
            //}
            //else
            //{
            //    input = JsonConvert.DeserializeObject<InputEntity>( payload );
            //    return Import( input, "", status );
            //}
        }
		/// <summary>
		/// Custom version, typically called outside a scheduled import
		/// </summary>
		/// <param name="item"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool CustomProcessRequest( ReadEnvelope item, SaveStatus status )
		{
			//EntityServices mgr = new EntityServices();
			bool importSuccessfull = ProcessEnvelope( item, status );
            List<string> messages = new List<string>();
            string importError = string.Join( "\r\n", status.GetAllMessages().ToArray() );
            //store envelope
            int newImportId = importHelper.Add( item, 1, status.Ctid, importSuccessfull, importError, ref messages );
            if ( newImportId > 0 && status.Messages != null && status.Messages.Count > 0 )
            {
                //add indicator of current recored
                string msg = string.Format( "========= Messages for Credential, EnvelopeIdentifier: {0}, ctid: {1}, Id: {2}, rowId: {3} =========", item.EnvelopeIdentifier, status.Ctid, status.DocumentId, status.DocumentRowId );
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

			DateTime createDate = new DateTime();
			DateTime envelopeUpdateDate = new DateTime();
			if ( DateTime.TryParse( item.NodeHeaders.CreatedAt.Replace( "UTC", "" ).Trim(), out createDate ) )
			{
				status.SetEnvelopeCreated( createDate );
			}
			if ( DateTime.TryParse( item.NodeHeaders.UpdatedAt.Replace( "UTC", "" ).Trim(), out envelopeUpdateDate ) )
			{
				status.SetEnvelopeUpdated( envelopeUpdateDate );
			}

			string payload = item.DecodedResource.ToString();
			string envelopeIdentifier = item.EnvelopeIdentifier;
			string ctdlType = RegistryServices.GetResourceType( payload );
			string envelopeUrl = RegistryServices.GetEnvelopeUrl( envelopeIdentifier );
			LoggingHelper.WriteLogFile( 1, item.EnvelopeCetermsCtid + "_credential", payload, "", false );

			//if ( ImportServiceHelpers.IsAGraphResource( payload ) )
			//{
			//if ( payload.IndexOf( "\"en\":" ) > 0 )
			return ImportV3( payload, envelopeIdentifier, status );
                //else
                //    return ImportV2( payload, envelopeIdentifier, status );
            //}
            //else
            //{
            //    LoggingHelper.DoTrace( 5, "		envelopeUrl: " + envelopeUrl );
            //    LoggingHelper.WriteLogFile( 1, "credential_" + item.EnvelopeIdentifier, payload, "", false );
            //    input = JsonConvert.DeserializeObject<InputEntity>( item.DecodedResource.ToString() );

               
            //    return Import( input, envelopeIdentifier, status, envelopeUpdateDate );
            //}
		}

        
        public bool ImportV3( string payload, string envelopeIdentifier, SaveStatus status )
        {
			LoggingHelper.DoTrace( 7, thisClassName + ".ImportV3 - entered." );
			DateTime started = DateTime.Now;
			var saveDuration = new TimeSpan();

			List<string> messages = new List<string>();
			bool importSuccessfull = false;
			EntityServices mgr = new EntityServices();
			//instantiate the input document (credential)
			InputEntityV3 input = new InputEntityV3();
            var bnodes = new List<BNode>();
			var holdersProfiles = new List<JsonInput.HoldersProfile>();
			var earningsProfiles = new List<JsonInput.EarningsProfile>();
			var employmentOutcomeProfiles = new List<JsonInput.EmploymentOutcomeProfile>();
			var dataSetProfiles = new List<JsonInput.QData.DataSetProfile>();
			var outcomesDTO = new OutcomesDTO();
			string ctid = "";
			var mainEntity = new Dictionary<string, object>();
			//map payload to a dictionary
            Dictionary<string, object> dictionary = RegistryServices.JsonToDictionary( payload );
			//get the @graph property
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
                    //mainEntity = RegistryServices.JsonToDictionary( main );
                    input = JsonConvert.DeserializeObject<InputEntityV3>( main );
                }
                else
                {
					//save blank nodes - typically reference organizations
					//20-10-15 now could be holdersProfile, EarningProfile, EmploymentOutlook, and more 
                    var bn = item.ToString();
					if ( bn.IndexOf( "ceterms:HoldersProfile" ) > -1 )
					{
						holdersProfiles.Add( JsonConvert.DeserializeObject<JsonInput.HoldersProfile>( bn ) );
					}
					else if ( bn.IndexOf( "ceterms:EarningsProfile" ) > -1 )
					{
						earningsProfiles.Add( JsonConvert.DeserializeObject<JsonInput.EarningsProfile>( bn ) );
					}
					else if ( bn.IndexOf( "ceterms:EmploymentOutcomeProfile" ) > -1 )
					{
						employmentOutcomeProfiles.Add( JsonConvert.DeserializeObject<JsonInput.EmploymentOutcomeProfile>( bn ) );
					}
					else if ( bn.IndexOf( "qdata:DataSetProfile" ) > -1 )
					{
						outcomesDTO.DataSetProfiles.Add( JsonConvert.DeserializeObject<JsonInput.QData.DataSetProfile>( bn ) );
					}
					else if ( bn.IndexOf( "qdata:DataSetTimeFrame" ) > -1 )
					{
						outcomesDTO.DataSetTimeFrames.Add( JsonConvert.DeserializeObject<JsonInput.QData.DataSetTimeFrame>( bn ) );
					}
					else if ( bn.IndexOf( "qdata:DataProfile" ) > -1 )
					{
						outcomesDTO.DataProfiles.Add( JsonConvert.DeserializeObject<JsonInput.QData.DataProfile>( bn ) );
					}
					else if ( bn.IndexOf( "_:" ) > -1 )
					{
						bnodes.Add( JsonConvert.DeserializeObject<BNode>( bn ) );
					}
				}
            }

            
            MappingHelperV3 helper = new MappingHelperV3(1);
            helper.entityBlankNodes = bnodes;
			helper.CurrentEntityCTID = input.Ctid;
			helper.CurrentEntityName = input.Name.ToString();

			status.EnvelopeId = envelopeIdentifier;
            try
            {
                //input = JsonConvert.DeserializeObject<InputEntity>( item.DecodedResource.ToString() );
                ctid = input.Ctid;
                string referencedAtId = input.CtdlId;

                LoggingHelper.DoTrace( 5, "		name: " + input.Name.ToString() );
                //LoggingHelper.DoTrace( 6, "		url: " + input.SubjectWebpage );
                //LoggingHelper.DoTrace( 5, "		ctid: " + input.Ctid );
                LoggingHelper.DoTrace( 6, "		@Id: " + input.CtdlId );
                status.Ctid = ctid;

                if ( status.DoingDownloadOnly )
                    return true;

                if ( !DoesEntityExist( input.Ctid, ref output ) )
                {
                    //set the rowid now, so that can be referenced as needed
                    output.RowId = Guid.NewGuid();
                }
                helper.currentBaseObject = output;

                //start with language and may use with language maps
                foreach ( var l in input.InLanguage )
                {
                    if ( !string.IsNullOrWhiteSpace( l ) )
                    {
                        var language = CodesManager.GetLanguage( l );
                        output.InLanguageCodeList.Add( new TextValueProfile()
                        {
                            CodeId = language.CodeId,
                            TextTitle = language.Name,
                            TextValue = language.Value
                        } );
                    }
                }

                if ( input.InLanguage.Count > 0)
                {
                    //could use to alter helper.DefaultLanguage
                }
                output.Name = helper.HandleLanguageMap( input.Name, output, "Name" );
                output.Description = helper.HandleLanguageMap( input.Description, output, "Description" );
                output.SubjectWebpage = input.SubjectWebpage;
                output.CTID = input.Ctid;

				//warning this gets set to blank if doing a manual import by ctid
				output.CredentialRegistryId = envelopeIdentifier;
                output.CredentialStatusType = helper.MapCAOToEnumermation( input.CredentialStatusType );

				//BYs - do owned and offered first
				output.OfferedBy = helper.MapOrganizationReferenceGuids( "Credential.OfferedBy", input.OfferedBy, ref status );
				//note need to set output.OwningAgentUid to the first entry
				output.OwnedBy = helper.MapOrganizationReferenceGuids( "Credential.OwnedBy", input.OwnedBy, ref status );
				if ( output.OwnedBy != null && output.OwnedBy.Count > 0 )
				{
					output.OwningAgentUid = output.OwnedBy[ 0 ];
					helper.CurrentOwningAgentUid = output.OwnedBy[ 0 ];
				}
				else
				{
					//add warning?
					if ( output.OfferedBy == null && output.OfferedBy.Count == 0 )
					{
						status.AddWarning( "document doesn't have an owning or offering organization." );
					}
				}
				output.AccreditedBy = helper.MapOrganizationReferenceGuids( "Credential.AccreditedBy", input.AccreditedBy, ref status );
				output.ApprovedBy = helper.MapOrganizationReferenceGuids( "Credential.ApprovedBy", input.ApprovedBy, ref status );
				output.RecognizedBy = helper.MapOrganizationReferenceGuids( "Credential.RecognizedBy", input.RecognizedBy, ref status );
				output.RegulatedBy = helper.MapOrganizationReferenceGuids( "Credential.RegulatedBy", input.RegulatedBy, ref status );
				output.RevokedBy = helper.MapOrganizationReferenceGuids( "Credential.RevokedBy", input.RevokedBy, ref status );
				output.RenewedBy = helper.MapOrganizationReferenceGuids( "Credential.RenewedBy", input.RenewedBy, ref status );
				//
				output.DateEffective = input.DateEffective;

                output.AlternateNames = helper.MapToTextValueProfile( input.AlternateName, output, "AlternateName" );
                output.ImageUrl = input.Image;
                output.CredentialTypeSchema = input.CredentialType;


                output.AvailabilityListing = helper.MapListToString( input.AvailabilityListing );
                output.AvailableOnlineAt = helper.MapListToString( input.AvailableOnlineAt );

                output.CredentialId = input.CredentialId;
                //TODO - develope entity for IdentitifierValue
                output.VersionIdentifier = helper.MapIdentifierValueListToString( input.VersionIdentifier );
                output.VersionIdentifierList = helper.MapIdentifierValueList( input.VersionIdentifier );

                output.CodedNotation = input.CodedNotation;
				output.Identifier = helper.MapIdentifierValueList( input.Identifier );
				if ( output.Identifier != null && output.Identifier.Count() > 0 )
				{
					output.IdentifierJson = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
				}
				output.ISICV4 = input.ISICV4;

				output.ProcessStandards = input.ProcessStandards;
                output.ProcessStandardsDescription = helper.HandleLanguageMap( input.ProcessStandardsDescription, output, "ProcessStandardsDescription" );
                output.LatestVersion = input.LatestVersion;
                output.PreviousVersion = input.PreviousVersion;
				output.NextVersion = input.NextVersion;
				output.SupersededBy = input.SupersededBy;
				output.Supersedes = input.Supersedes;

				output.Subject = helper.MapCAOListToTextValueProfile( input.Subject, CodesManager.PROPERTY_CATEGORY_SUBJECT );

                //occupations
                //output.Occupation = helper.MapCAOListToEnumermation( input.OccupationType );
				//actually used by import
                output.Occupations = helper.MapCAOListToCAOProfileList( input.OccupationType );
				//just append alternative items. Ensure empty lists are ignored
				//output.Occupations.AddRange(helper.AppendLanguageMapListToCAOProfileList( input.AlternativeOccupationType ));

				//skip if no occupations
				if ( output.Occupations.Count() == 0 
					&& UtilityManager.GetAppKeyValue( "skipCredImportIfNoOccupations", false ))
				{
					//LoggingHelper.DoTrace( 2, string.Format( "		***Skipping Credential# {0}, {1} as it has no occupations and this is a special run.", output.Id, output.Name ) );
					//return true;
				}
                //Industries
                output.Industries = helper.MapCAOListToCAOProfileList( input.IndustryType );
				//output.Industries.AddRange( helper.AppendLanguageMapListToCAOProfileList( input.AlternativeIndustryType ) );
				//naics
				output.Naics = input.Naics;

				output.InstructionalProgramTypes = helper.MapCAOListToCAOProfileList( input.InstructionalProgramType );
				//output.InstructionalProgramTypes.AddRange( helper.AppendLanguageMapListToCAOProfileList( input.AlternativeInstructionalProgramType ) );
				//
				//will want a custom method to lookup the rating
				NavyServices nsrvs = new NavyServices();
				output.NavyRating = NavyServices.MapRatingsListToEnumermation( input.HasRating );
				output.NavyRatingType.AddRange( nsrvs.MapCAOListToCAOProfileList( input.HasRating, ref messages ) );
				//

				output.Keyword = helper.MapToTextValueProfile( input.Keyword, output, "Keyword" );

                output.Jurisdiction = helper.MapToJurisdiction( input.Jurisdiction, ref status );
                //CopyrightHolder - expecting single; will need to expand
                output.CopyrightHolder = helper.MapOrganizationReferencesGuid( "Credential.CopyrightHolder", input.CopyrightHolder, ref status );
                //CAO
                output.AudienceLevelType = helper.MapCAOListToEnumermation( input.AudienceLevelType );
                //
                output.AudienceType = helper.MapCAOListToEnumermation( input.AudienceType );
                output.DegreeConcentration = helper.MapCAOListToTextValueProfile( input.DegreeConcentration, CodesManager.PROPERTY_CATEGORY_DEGREE_CONCENTRATION );
                output.DegreeMajor = helper.MapCAOListToTextValueProfile( input.DegreeMajor, CodesManager.PROPERTY_CATEGORY_DEGREE_MAJOR );
                output.DegreeMinor = helper.MapCAOListToTextValueProfile( input.DegreeMinor, CodesManager.PROPERTY_CATEGORY_DEGREE_MINOR );

				output.AssessmentDeliveryType = helper.MapCAOListToEnumermation( input.AssessmentDeliveryType );
				output.LearningDeliveryType = helper.MapCAOListToEnumermation( input.LearningDeliveryType );

				//EstimatedCost
				//will need to format, all populate Entity.RelatedCosts (for bubble up) - actually this would be for asmts, and lopps
				output.EstimatedCost = helper.FormatCosts( input.EstimatedCost, ref status );

                //EstimatedDuration
                output.EstimatedDuration = helper.FormatDuration( input.EstimatedDuration, ref status );
                output.RenewalFrequency = helper.FormatDurationItem( input.RenewalFrequency );
               
                //conditions
                output.Requires = helper.FormatConditionProfile( input.Requires, ref status );
                output.Recommends = helper.FormatConditionProfile( input.Recommends, ref status );
                output.Renewal = helper.FormatConditionProfile( input.Renewal, ref status );
                output.Corequisite = helper.FormatConditionProfile( input.Corequisite, ref status );
                output.Revocation = helper.FormatRevocationProfile( input.Revocation, ref status );

                //connections
                output.AdvancedStandingFrom = helper.FormatConditionProfile( input.AdvancedStandingFrom, ref status );
                output.AdvancedStandingFor = helper.FormatConditionProfile( input.IsAdvancedStandingFor, ref status );

                output.PreparationFrom = helper.FormatConditionProfile( input.PreparationFrom, ref status );
                output.IsPreparationFor = helper.FormatConditionProfile( input.IsPreparationFor, ref status );

                output.IsRequiredFor = helper.FormatConditionProfile( input.IsRequiredFor, ref status );
                output.IsRecommendedFor = helper.FormatConditionProfile( input.IsRecommendedFor, ref status );

                //common conditions
                output.ConditionManifestIds = helper.MapEntityReferences( input.CommonConditions, CodesManager.ENTITY_TYPE_CONDITION_MANIFEST, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status );
                //common costs
                output.CostManifestIds = helper.MapEntityReferences( input.CommonCosts, CodesManager.ENTITY_TYPE_COST_MANIFEST, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status );

                //HasPart/IsPart
                //WARNING - these methods assume all parts are the same type - the provided thisEntityTypeId. AT THIS TIME, THE PARTS SHOULD ALL BE CREDENTIALS
                output.HasPartIds = helper.MapEntityReferences( "Credential.HasPart", input.HasPart, thisEntityTypeId, ref status );
                output.IsPartOfIds = helper.MapEntityReferences( "Credential.IsPartOf", input.IsPartOf, thisEntityTypeId, ref status );
				//for a QA credential - HasETPLResource
				//this returns a list of guids
				//perhaps would be more efficient to return a list of TopLevelObjects? - we should already know the latter after resolution, so store now rather than having to look up later
				output.HasETPLResourceUids = helper.MapEntityReferenceGuids( "Credential.HasETPLResource", input.HasETPLResource, 0, ref status );
				if ( output.HasETPLResourceUids != null && output.HasETPLResourceUids.Count() > 0 )
				{
					var tloList = ProfileServices.ResolveToTopLevelObject( output.HasETPLResourceUids, "Credential.HasETPLResource", ref status );
					//could save the TLO, although we really only need the id later
					output.HasETPLCredentialsIds = ( List<int> )tloList.Where( s => s.EntityTypeId == 1 ).Select( g => g.Id);
					output.HasETPLAssessmentsIds = ( List<int> )tloList.Where( s => s.EntityTypeId == 3 ).Select( g => g.Id );
					output.HasETPLLoppsIds = ( List<int> )tloList.Where( s => s.EntityTypeId == 7 ).Select( g => g.Id );					

					//output.HasETPLAssessments = tloList.Where( s => s.EntityTypeId == 3 ).ToList();
					////
					//output.HasETPLCredentials = tloList.Where( s => s.EntityTypeId == 1 ).ToList();
					//var inputIds = output.HasETPLCredentials.Select( x => x.Id ).ToList();
					////
					//output.HasETPLLopps = tloList.Where( s => s.EntityTypeId == 7 ).ToList();
				}

				//Process profiles
				output.AdministrationProcess = helper.FormatProcessProfile( input.AdministrationProcess, ref status );
                output.DevelopmentProcess = helper.FormatProcessProfile( input.DevelopmentProcess, ref status );
                output.MaintenanceProcess = helper.FormatProcessProfile( input.MaintenanceProcess, ref status );

                output.AppealProcess = helper.FormatProcessProfile( input.AppealProcess, ref status );
                output.ComplaintProcess = helper.FormatProcessProfile( input.ComplaintProcess, ref status );
                output.ReviewProcess = helper.FormatProcessProfile( input.ReviewProcess, ref status );
                output.RevocationProcess = helper.FormatProcessProfile( input.RevocationProcess, ref status );

                //FinancialAssistance
                output.FinancialAssistance = helper.FormatFinancialAssistance( input.FinancialAssistance, ref status );
				if ( output.FinancialAssistance != null && output.FinancialAssistance.Any() )
					output.FinancialAssistanceJson = JsonConvert.SerializeObject( output.FinancialAssistance, MappingHelperV3.GetJsonSettings() );
				//TODO
				//these would be a list of URIs that should reference the earningsProfiles list
				var earningsProfile = input.Earnings;
				output.EarningsProfile = helper.FormatEarningsProfile( earningsProfiles, outcomesDTO, bnodes, ref status );
				//input.HoldersProfile is list of ctids that could be used to xref holdersProfiles
				var credentialHoldersProfiles = input.Holders;
				output.HoldersProfile = helper.FormatHoldersProfile( output.CTID, holdersProfiles, outcomesDTO, bnodes, ref status );
				//
				var employmentOutcomeProfile = input.EmploymentOutcome;
				output.EmploymentOutcomeProfile = helper.FormatEmploymentOutcomeProfile( employmentOutcomeProfiles, outcomesDTO, bnodes, ref status );
				//
				output.Addresses = helper.FormatAvailableAtAddresses( input.AvailableAt, ref status );
				if ( output.Addresses != null && output.Addresses.Any() )
				{
					output.CredentialExternalProperties.Addresses = output.Addresses;
					//this can be expanded to add more properties, so should do assignment later - in MapToDB?
					output.JsonProperties = JsonConvert.SerializeObject( output.CredentialExternalProperties, MappingHelperV3.GetJsonSettings() );
					//output.JsonProperties = JsonConvert.SerializeObject( output.Addresses, MappingHelperV3.GetJsonSettings() );
				}

				//INs
				output.AccreditedIn = helper.MapToJurisdiction( input.AccreditedIn, ref status );
                output.ApprovedIn = helper.MapToJurisdiction( input.ApprovedIn, ref status );
                output.ApprovedIn = helper.MapToJurisdiction( input.ApprovedIn, ref status );
                output.RecognizedIn = helper.MapToJurisdiction( input.RecognizedIn, ref status );
                output.RegulatedIn = helper.MapToJurisdiction( input.RegulatedIn, ref status );
                output.RevokedIn = helper.MapToJurisdiction( input.RevokedIn, ref status );
                output.RenewedIn = helper.MapToJurisdiction( input.RenewedIn, ref status );
				//mapping duration
				TimeSpan duration = DateTime.Now.Subtract( started );
				if ( duration.TotalSeconds > 10 )
					LoggingHelper.DoTrace( 5, string.Format( "         WARNING Mapping Duration: {0:N2} seconds ", duration.TotalSeconds ) );
				DateTime saveStarted = DateTime.Now;
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
				//save the data
                importSuccessfull = mgr.Import( output, ref status );
				//
				saveDuration = DateTime.Now.Subtract( saveStarted );
				if ( saveDuration.TotalSeconds > 5 )
					LoggingHelper.DoTrace( 6, string.Format( "         WARNING SAVE Duration: {0:N2} seconds ", saveDuration.TotalSeconds ) );
				status.DocumentId = output.Id;
                status.DetailPageUrl = string.Format( "~/credential/{0}", output.Id );
                status.DocumentRowId = output.RowId;

                //if record was added to db, add to/or set EntityResolution as resolved
                int ierId = new ImportManager().Import_EntityResolutionAdd( referencedAtId,
                        ctid,
                        CodesManager.ENTITY_TYPE_CREDENTIAL,
                        output.RowId,
                        output.Id,
                        ( output.Id > 0 ),
                        ref messages,
                        output.Id > 0 );
                //just in case
                if ( status.HasErrors )
                    importSuccessfull = false;
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, string.Format( "CredentialImport. Exception encountered for CTID: {0}", ctid ), false, "Credential Import exception" );
            }
			finally
			{
				var totalDuration = DateTime.Now.Subtract( started );
				if ( totalDuration.TotalSeconds > 9 && ( totalDuration.TotalSeconds - saveDuration.TotalSeconds > 3 ) )
					LoggingHelper.DoTrace( 5, string.Format( "         WARNING Total Duration: {0:N2} seconds ", totalDuration.TotalSeconds ) );

			}
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
		/*
//public bool Import( InputEntity input, string envelopeIdentifier, SaveStatus status )
//{
//	DateTime envelopeUpdateDate = System.DateTime.Now;
//	return Import( input, envelopeIdentifier, status, envelopeUpdateDate );
//}

[Obsolete]
private bool Import( InputEntity input, string envelopeIdentifier, SaveStatus status, DateTime envelopeUpdateDate )
{
	EntityServices mgr = new EntityServices();
	List<string> messages = new List<string>();
	bool importSuccessfull = false;
	status.EnvelopeId = envelopeIdentifier;
	try
	{
		//input = JsonConvert.DeserializeObject<InputEntity>( item.DecodedResource.ToString() );
		string ctid = input.Ctid;
		string referencedAtId = input.CtdlId;

		LoggingHelper.DoTrace( 6, "		name: " + input.Name );
		LoggingHelper.DoTrace( 6, "		url: " + input.SubjectWebpage );
		LoggingHelper.DoTrace( 6, "		ctid: " + input.Ctid );
		LoggingHelper.DoTrace( 6, "		@Id: " + input.CtdlId );
		status.Ctid = ctid;

		if ( status.DoingDownloadOnly )
			return true;

		if (!DoesEntityExist( input.Ctid, ref output ))
		{
			//set the rowid now, so that can be referenced as needed
			output.RowId = Guid.NewGuid();
		}
		//?? there is a difference with UTC, so not sure of the point of this check
		if ( envelopeUpdateDate  < DateTime.Now.AddMinutes(-1))
		{
			output.RegistryLastUpdated = envelopeUpdateDate;
		}

		//re:messages - currently passed to mapping but no errors are trapped??
		//				- should use SaveStatus and skip import if errors encountered (vs warnings)

		output.Name = input.Name;
		output.Description = input.Description;
		output.SubjectWebpage = input.SubjectWebpage;
		output.CTID = input.Ctid;
		output.CredentialRegistryId = envelopeIdentifier;
		output.CredentialStatusType = MappingHelper.MapCAOToEnumermation( input.CredentialStatusType );
		output.DateEffective = input.DateEffective;

		//handle both ways for now
		//output.AlternateName = input.AlternateName;
		output.AlternateNames = MappingHelper.MapToTextValueProfile( input.AlternateName );
		output.ImageUrl = input.Image;
		output.CredentialTypeSchema = input.CredentialType;


		output.AvailabilityListing = MappingHelper.MapListToString( input.AvailabilityListing );
		output.AvailableOnlineAt = MappingHelper.MapListToString( input.AvailableOnlineAt );

		output.CredentialId = input.CredentialId;
		//TODO - develope entity for IdentitifierValue
		output.VersionIdentifier = MappingHelper.MapIdentifierValueListToString( input.VersionIdentifier );
		output.VersionIdentifierList = MappingHelper.MapIdentifierValueList( input.VersionIdentifier );

		output.CodedNotation = input.CodedNotation;

		foreach ( var l in input.InLanguage )
		{
			if ( !string.IsNullOrWhiteSpace( l ) )
			{
				var language = CodesManager.GetLanguage( l );
				output.InLanguageCodeList.Add( new TextValueProfile()
				{
					CodeId = language.CodeId,
					TextTitle = language.Name,
					TextValue = language.Value
				} );
			}
		}

		output.ProcessStandards = input.ProcessStandards;
		output.ProcessStandardsDescription = input.ProcessStandardsDescription;
		output.LatestVersion = input.LatestVersion;
		output.PreviousVersion = input.PreviousVersion;

		output.Subject = MappingHelper.MapCAOListToTextValueProfile( input.Subject, CodesManager.PROPERTY_CATEGORY_SUBJECT );

		//occupations
		output.Occupation = MappingHelper.MapCAOListToEnumermation( input.OccupationType );
		output.Occupations = MappingHelper.MapCAOListToFramework( input.OccupationType );

		//Industries
		output.Industry = MappingHelper.MapCAOListToEnumermation( input.IndustryType );
		output.Industries = MappingHelper.MapCAOListToFramework( input.IndustryType );
		//naics
		output.Naics = input.Naics;

		output.Keyword = MappingHelper.MapToTextValueProfile( input.Keyword );

		output.Jurisdiction = MappingHelper.MapToJurisdiction( input.Jurisdiction, ref status );
		//CopyrightHolder - expecting single; will need to expand
		output.CopyrightHolder = MappingHelper.MapOrganizationReferencesGuid( input.CopyrightHolder, ref status );
		//CAO
		output.AudienceLevelType = MappingHelper.MapCAOListToEnumermation( input.AudienceLevel );
		//
		output.AudienceType = MappingHelper.MapCAOListToEnumermation( input.AudienceType );
		output.DegreeConcentration = MappingHelper.MapCAOListToTextValueProfile( input.DegreeConcentration, CodesManager.PROPERTY_CATEGORY_DEGREE_CONCENTRATION );
		output.DegreeMajor = MappingHelper.MapCAOListToTextValueProfile( input.DegreeMajor, CodesManager.PROPERTY_CATEGORY_DEGREE_MAJOR );
		output.DegreeMinor = MappingHelper.MapCAOListToTextValueProfile( input.DegreeMinor, CodesManager.PROPERTY_CATEGORY_DEGREE_MINOR );
		//EstimatedCost
		//will need to format, all populate Entity.RelatedCosts (for bubble up) - actually this would be for asmts, and lopps
		output.EstimatedCost = MappingHelper.FormatCosts( input.EstimatedCost, ref status );

		//EstimatedDuration
		output.EstimatedDuration = MappingHelper.FormatDuration( input.EstimatedDuration, ref status );
		output.RenewalFrequency = MappingHelper.FormatDurationItem( input.RenewalFrequency );

		//conditions
		output.Requires = MappingHelper.FormatConditionProfile( input.Requires, ref status );
		output.Recommends = MappingHelper.FormatConditionProfile( input.Recommends, ref status );
		output.Renewal = MappingHelper.FormatConditionProfile( input.Renewal, ref status );
		output.Corequisite = MappingHelper.FormatConditionProfile( input.Corequisite, ref status );
		output.Revocation = MappingHelper.FormatRevocationProfile( input.Revocation, ref status );

		//connections
		output.AdvancedStandingFrom = MappingHelper.FormatConditionProfile( input.AdvancedStandingFrom, ref status );
		output.AdvancedStandingFor = MappingHelper.FormatConditionProfile( input.IsAdvancedStandingFor, ref status );

		output.PreparationFrom = MappingHelper.FormatConditionProfile( input.PreparationFrom, ref status );
		output.IsPreparationFor = MappingHelper.FormatConditionProfile( input.IsPreparationFor, ref status );

		output.IsRequiredFor = MappingHelper.FormatConditionProfile( input.IsRequiredFor, ref status );
		output.IsRecommendedFor = MappingHelper.FormatConditionProfile( input.IsRecommendedFor, ref status );

		//common conditions
		output.ConditionManifestIds = MappingHelper.MapEntityReferences( input.CommonConditions, CodesManager.ENTITY_TYPE_CONDITION_MANIFEST, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status );
		//common costs
		output.CostManifestIds = MappingHelper.MapEntityReferences( input.CommonCosts, CodesManager.ENTITY_TYPE_COST_MANIFEST, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status );

		//HasPart/IsPart
		//WARNING - these methods assume all parts are the same type - the provided thisEntityTypeId. AT THIS TIME, THE PARTS SHOULD ALL BE CREDENTIALS
		output.HasPartIds = MappingHelper.MapEntityReferences( input.HasPart, thisEntityTypeId, ref status );
		output.IsPartOfIds = MappingHelper.MapEntityReferences( input.IsPartOf, thisEntityTypeId, ref status );

		//Process profiles
		output.AdministrationProcess = MappingHelper.FormatProcessProfile( input.AdministrationProcess, ref status );
		output.DevelopmentProcess = MappingHelper.FormatProcessProfile( input.DevelopmentProcess, ref status );
		output.MaintenanceProcess = MappingHelper.FormatProcessProfile( input.MaintenanceProcess, ref status );

		output.AppealProcess = MappingHelper.FormatProcessProfile( input.AppealProcess, ref status );
		output.ComplaintProcess = MappingHelper.FormatProcessProfile( input.ComplaintProcess, ref status );
		output.ReviewProcess = MappingHelper.FormatProcessProfile( input.ReviewProcess, ref status );
		output.RevocationProcess = MappingHelper.FormatProcessProfile( input.RevocationProcess, ref status );

		//FinancialAssistance
		output.FinancialAssistanceOLD = MappingHelper.FormatFinancialAssistance( input.FinancialAssistance, ref status );


		output.Addresses = MappingHelper.FormatAvailableAtAddresses( input.AvailableAt, ref status );

		//BYs
		output.AccreditedBy = MappingHelper.MapOrganizationReferenceGuids( input.AccreditedBy, ref status );
		output.ApprovedBy = MappingHelper.MapOrganizationReferenceGuids( input.ApprovedBy, ref status );
		output.OfferedBy = MappingHelper.MapOrganizationReferenceGuids( input.OfferedBy, ref status );
		//note need to set output.OwningAgentUid to the first entry
		output.OwnedBy = MappingHelper.MapOrganizationReferenceGuids( input.OwnedBy, ref status );
		if (output.OwnedBy != null && output.OwnedBy.Count > 0)
		{
			output.OwningAgentUid = output.OwnedBy[ 0 ];
		}
		else
		{
			//add warning?
			if (output.OfferedBy == null && output.OfferedBy.Count == 0)
			{
				status.AddWarning( "document doesn't have an owning or offering organization." );
			}
		}

	output.RecognizedBy = MappingHelper.MapOrganizationReferenceGuids( input.RecognizedBy, ref status );
	output.RegulatedBy = MappingHelper.MapOrganizationReferenceGuids( input.RegulatedBy, ref status );
	output.RevokedBy = MappingHelper.MapOrganizationReferenceGuids( input.RevokedBy, ref status );
	output.RenewedBy = MappingHelper.MapOrganizationReferenceGuids( input.RenewedBy, ref status );

	//INs
	output.AccreditedIn = MappingHelper.MapToJurisdiction( input.AccreditedIn, ref status );
	output.ApprovedIn = MappingHelper.MapToJurisdiction( input.ApprovedIn, ref status );
	output.ApprovedIn = MappingHelper.MapToJurisdiction( input.ApprovedIn, ref status );
	output.RecognizedIn = MappingHelper.MapToJurisdiction( input.RecognizedIn, ref status );
	output.RegulatedIn = MappingHelper.MapToJurisdiction( input.RegulatedIn, ref status );
	output.RevokedIn = MappingHelper.MapToJurisdiction( input.RevokedIn, ref status );
	output.RenewedIn = MappingHelper.MapToJurisdiction( input.RenewedIn, ref status );

	//=== if any messages were encountered treat as warnings for now
	if ( messages.Count > 0 )
		status.SetMessages( messages, true );
	//just in case check if entity added since start
	if (output.Id == 0)
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
	status.DetailPageUrl = string.Format( "~/credential/{0}", output.Id );
	status.DocumentRowId = output.RowId;

	//if record was added to db, add to/or set EntityResolution as resolved
	int ierId = new ImportManager().Import_EntityResolutionAdd( referencedAtId,
			ctid,
			CodesManager.ENTITY_TYPE_CREDENTIAL,
			output.RowId,
			output.Id,
			( output.Id > 0 ),
			ref messages,
			output.Id  > 0);
	//just in case
	if ( status.HasErrors )
		importSuccessfull = false;
	}
	catch ( Exception ex )
	{
		LoggingHelper.LogError( ex, string.Format( "Exception encountered in envelopeId: {0}", envelopeIdentifier ), false, "workIT Import exception" );
		//importError = ex.Message;

		////make continue on exceptions an option
		//exceptionCtr++;
		//if ( exceptionCtr > 10 )
		//{
		//	//arbitrarily stop if large number of exceptions
		//	LoggingHelper.LogError( "Many exceptions were encountered during import - abandoning.", true, "workIT Import - many exceptions" );
		//	isComplete = true;
		//	break;
		//}
	}

	return importSuccessfull;
}
*/
	}
}
