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
				//pending records will have a  CTID, it should be used to get the envelope!
				//if ( !ImportByResourceUrl( item.SubjectWebpage, status ) )
				if ( !ImportByCtid( item.CTID, status ) )
				{
                    //check for 404
                    LoggingHelper.DoTrace(1, string.Format("     - (). Failed to import pending credential: {0}, message(s): {1}", item.Id, status.GetErrorsAsString()));
                }
                else
                    LoggingHelper.DoTrace(1, string.Format("     - (). Successfully imported pending credential: {0}", item.Id));
            }
		}   //


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
			//
			status.DocumentOwnedBy = item.documentOwnedBy;
			if ( item.documentPublishedBy != null )
			{
				if ( item.documentOwnedBy == null || ( item.documentPublishedBy != item.documentOwnedBy ))
					status.DocumentPublishedBy = item.documentPublishedBy;
			} else
			{
				//will need to check elsewhere
				//OR as part of import check if existing one had 3rd party publisher
			}

			string payload = item.DecodedResource.ToString();
			status.EnvelopeId = item.EnvelopeIdentifier;
			//Already done in  RegistryImport
			//LoggingHelper.WriteLogFile( UtilityManager.GetAppKeyValue( "logFileTraceLevel",5), item.EnvelopeCetermsCtid + "_credential", payload, "", false );

			return ImportV3( payload, status );
		}

		/// <summary>
		/// Import a credential from an envelope DecodedResource
		/// </summary>
		/// <param name="payload">Registry Envelope DecodedResource</param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool ImportV3( string payload, SaveStatus status )
        {
			LoggingHelper.DoTrace( 7, thisClassName + ".ImportV3 - entered." );
			DateTime started = DateTime.Now;
			var saveDuration = new TimeSpan();

			List<string> messages = new List<string>();
			bool importSuccessfull = false;
			EntityServices mgr = new EntityServices();
			//instantiate the input document (credential)
			InputEntityV3 input = new InputEntityV3();
			var legacyInput = new JsonInput.CredentialProxy();

			var bnodes = new List<BNode>();
			var holdersProfiles = new List<JsonInput.HoldersProfile>();
			var earningsProfiles = new List<JsonInput.EarningsProfile>();
			var employmentOutcomeProfiles = new List<JsonInput.EmploymentOutcomeProfile>();
			//var dataSetProfiles = new List<JsonInput.QData.DataSetProfile>();
			var outcomesDTO = new OutcomesDTO();
			string ctid = "";
			var mainEntity = new Dictionary<string, object>();
			JArray graphList = RegistryServices.GetGraphList( payload );

			//map payload to a dictionary
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

					//22-08-30 need to check for CreditValue in the old Quantitative format
					if (main.IndexOf( "schema:QuantitativeValue" ) > 0)
                    {
						//what
						legacyInput = JsonConvert.DeserializeObject<JsonInput.CredentialProxy>( main );
					}
                }
                else
                {
					//save blank nodes - typically reference organizations
					//20-10-15 now could be holdersProfile, EarningProfile, EmploymentOutlook, and more 
					//21-02-22 mparsons - now the outcome profiles could be embedded in the credential
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
						//21-0=10-07 mp - the dataSetProfile will no longer be in the graph, but a separate graph/envelope
						//				- the DSP import would get it. The ADP processing will change now. 
						//outcomesDTO.DataSetProfiles.Add( JsonConvert.DeserializeObject<JsonInput.QData.DataSetProfile>( bn ) );
					}
					else if ( bn.IndexOf( "qdata:DataSetTimeFrame" ) > -1 )
					{
						//should no longer happen
						//outcomesDTO.DataSetTimeFrames.Add( JsonConvert.DeserializeObject<JsonInput.QData.DataSetTimeFrame>( bn ) );
					}
					else if ( bn.IndexOf( "qdata:DataProfile" ) > -1 )
					{
						//should no longer happen
						//outcomesDTO.DataProfiles.Add( JsonConvert.DeserializeObject<JsonInput.QData.DataProfile>( bn ) );
					}
					else if ( bn.IndexOf( "_:" ) > -1 )
					{
						bnodes.Add( JsonConvert.DeserializeObject<BNode>( bn ) );
					}
				}
            }

            
            MappingHelperV3 helper = new MappingHelperV3(1);
            helper.entityBlankNodes = bnodes;
			helper.CurrentEntityCTID = input.CTID;
			helper.CurrentEntityName = input.Name.ToString();
			var owningOrganizationCTID = "";
			//status.EnvelopeId = envelopeIdentifier;
			try
            {
                //input = JsonConvert.DeserializeObject<InputEntity>( item.DecodedResource.ToString() );
                ctid = input.CTID;
                status.ResourceURL = input.CtdlId;

                LoggingHelper.DoTrace( 5, "		name: " + input.Name.ToString() );
                //LoggingHelper.DoTrace( 6, "		url: " + input.SubjectWebpage );
                //LoggingHelper.DoTrace( 5, "		ctid: " + input.CTID );
                LoggingHelper.DoTrace( 6, "		@Id: " + input.CtdlId );
                status.Ctid = ctid;

                if ( status.DoingDownloadOnly )
                    return true;

				if ( !DoesEntityExist( input.CTID, ref output ) )
				{
					status.UpdateElasticIndex = true;
					//set the rowid now, so that can be referenced as needed
					output.RowId = Guid.NewGuid();
					LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".ImportV3(). Importing new record using CTID: '{0}'", input.CTID ) );
				}
				else
				{
					LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".ImportV3(). Found record: '{0}' using CTID: '{1}'", input.Name, input.CTID ) );
				}
				helper.currentBaseObject = output;

				//start with language and may use with language maps
				//21-05-27 mp - this was not being saved to InLanguageCodeList (just added) so how was the db being updated?
				output.InLanguageCodeList = helper.MapInLanguageToTextValueProfile( input.InLanguage, "Credential.InLanguage. CTID: " + ctid );

				//foreach ( var l in input.InLanguage )
				//{
				//	if ( !string.IsNullOrWhiteSpace( l ) )
				//	{
				//		var language = CodesManager.GetLanguage( l );
				//		output.InLanguageCodeList.Add( new TextValueProfile()
				//		{
				//			CodeId = language.CodeId,
				//			TextTitle = language.Name,
				//			TextValue = language.Value
				//		} );
				//	}
				//}

				if ( input.InLanguage.Count > 0 )
				{
					helper.DefaultLanguage = input.InLanguage[0];
				}
				else
				{
					//OR set based on the first language
					helper.SetDefaultLanguage( input.Name, "Name" );
				}


				output.Name = helper.HandleLanguageMap( input.Name, output, "Name" );
                output.Description = helper.HandleLanguageMap( input.Description, output, "Description" );
                output.SubjectWebpage = input.SubjectWebpage;
                output.CTID = input.CTID;
				//TBD handling of referencing third party publisher
				helper.MapOrganizationPublishedBy( output, ref status );
				

				//warning this gets set to blank if doing a manual import by ctid
				output.CredentialRegistryId = status.EnvelopeId;
				output.CredentialTypeSchema = input.CredentialType;
				//at this point could get the CredentialStatusTypeSchema, or just leave to the credentialManager phase
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
					} else
					{
						output.OwningAgentUid = output.OfferedBy[ 0 ];
						helper.CurrentOwningAgentUid = output.OfferedBy[ 0 ];
					}
				}
				//
				var org = OrganizationManager.GetBasics( output.OwningAgentUid );
				if ( org != null && org.Id > 0 )
					owningOrganizationCTID = org.CTID;
				//proposal, only in credentials for now
				status.CurrentDataProvider = helper.CurrentOwningAgentUid;

				output.AccreditedBy = helper.MapOrganizationReferenceGuids( "Credential.AccreditedBy", input.AccreditedBy, ref status );
				output.ApprovedBy = helper.MapOrganizationReferenceGuids( "Credential.ApprovedBy", input.ApprovedBy, ref status );
				output.RecognizedBy = helper.MapOrganizationReferenceGuids( "Credential.RecognizedBy", input.RecognizedBy, ref status );
				output.RegulatedBy = helper.MapOrganizationReferenceGuids( "Credential.RegulatedBy", input.RegulatedBy, ref status );
				output.RevokedBy = helper.MapOrganizationReferenceGuids( "Credential.RevokedBy", input.RevokedBy, ref status );
				output.RenewedBy = helper.MapOrganizationReferenceGuids( "Credential.RenewedBy", input.RenewedBy, ref status );
				//
				output.DateEffective = input.DateEffective;
				output.ExpirationDate = input.ExpirationDate;

                output.AlternateNames = helper.MapToTextValueProfile( input.AlternateName, output, "AlternateName" );
                output.Image = input.Image;

                output.AvailabilityListing = helper.MapListToString( input.AvailabilityListing );
                output.AvailableOnlineAt = helper.MapListToString( input.AvailableOnlineAt );

                output.CredentialId = input.CredentialId;
				//2021 - codedNotation is no longer part of credential
                output.CodedNotation = input.CodedNotation;

				//
				output.Identifier = helper.MapIdentifierValueList( input.Identifier );
				output.IdentifierNew = helper.MapIdentifierValueList2( input.Identifier );

				if ( output.IdentifierNew != null && output.IdentifierNew.Count() > 0 )
				{
					output.IdentifierJSON = JsonConvert.SerializeObject( output.IdentifierNew, MappingHelperV3.GetJsonSettings() );
				}
				//
				output.VersionIdentifierList = helper.MapIdentifierValueList( input.VersionIdentifier );
				output.VersionIdentifierNew = helper.MapIdentifierValueList2( input.VersionIdentifier );
				if ( output.VersionIdentifierNew != null && output.VersionIdentifierNew.Count() > 0 )
				{
					output.VersionIdentifierJSON = JsonConvert.SerializeObject( output.VersionIdentifierNew, MappingHelperV3.GetJsonSettings() );
				}
				//
				output.ISICV4 = input.ISICV4;

				output.ProcessStandards = input.ProcessStandards;
                output.ProcessStandardsDescription = helper.HandleLanguageMap( input.ProcessStandardsDescription, output, "ProcessStandardsDescription" );
				//TODO - need to change to resolve to credentials - if a resource URL
				//would this have to have a CTID? that is can an external URL be provided
				output.LatestVersion = input.LatestVersion ?? "";
				output.PreviousVersion = input.PreviousVersion ?? "";
				output.NextVersion = input.NextVersion ?? "";
				output.SupersededBy = input.SupersededBy ?? "";
				output.Supersedes = input.Supersedes ?? "";
				//if same as current version/ctid, hide
				if ( output.LatestVersion.ToLower().IndexOf(ctid.ToLower()) > -1)
				{
					output.LatestVersion = "";
				} else if ( output.LatestVersion.ToLower().IndexOf( "/resources/ce-" ) > -1 )
				{
					//should format as a finder/resources/ url, as should exist in the finder
					output.LatestVersion = helper.FormatFinderResourcesURL( output.LatestVersion );
					//ResolutionServices.ExtractCtid( output.LatestVersion.Trim() );
				}
				if ( output.PreviousVersion.ToLower().IndexOf( "/resources/ce-" ) > -1 )
				{
					//should format as a finder/resources/ url, as should exist in the finder
					output.PreviousVersion = helper.FormatFinderResourcesURL( output.PreviousVersion ); //ResolutionServices.ExtractCtid( output.PreviousVersion.Trim() );
				}
				if ( output.NextVersion.ToLower().IndexOf( "/resources/ce-" ) > -1 )
				{
					//????
					output.NextVersion = helper.FormatFinderResourcesURL( output.NextVersion ); //ResolutionServices.ExtractCtid( output.NextVersion.Trim() );
				}
				if ( output.SupersededBy.ToLower().IndexOf( "/resources/ce-" ) > -1 )
				{
					//????
					output.SupersededBy = helper.FormatFinderResourcesURL( output.SupersededBy ); //ResolutionServices.ExtractCtid( output.SupersededBy.Trim() );
				}
				if ( output.Supersedes.ToLower().IndexOf( "/resources/ce-" ) > -1 )
				{
					//????
					output.Supersedes = helper.FormatFinderResourcesURL( output.Supersedes ); //ResolutionServices.ExtractCtid( output.Supersedes.Trim() );
				}
				//output.LatestVersionId = helper.MapEntityReference( "Credential.LatestVersion", input.LatestVersion, thisEntityTypeId, ref status, false );
				//output.PreviousVersionId = helper.MapEntityReference( "Credential.PreviousVersion", input.PreviousVersion, thisEntityTypeId, ref status, false ); 
				//output.NextVersionId = helper.MapEntityReference( "Credential.NextVersion", input.NextVersion, thisEntityTypeId, ref status, false ); 
				//output.SupersededById = helper.MapEntityReference( "Credential.SupersededBy", input.SupersededBy, thisEntityTypeId, ref status, false ); 
				//output.SupersedesId = helper.MapEntityReference( "Credential.Supersedes", input.Supersedes, thisEntityTypeId, ref status, false ); 

				//
				output.Subject = helper.MapCAOListToTextValueProfile( input.Subject, CodesManager.PROPERTY_CATEGORY_SUBJECT );

                //occupations
                //output.Occupation = helper.MapCAOListToEnumermation( input.OccupationType );
				//actually used by import
                output.OccupationTypes = helper.MapCAOListToCAOProfileList( input.OccupationType );
				//just append alternative items. Ensure empty lists are ignored
				//output.Occupations.AddRange(helper.AppendLanguageMapListToCAOProfileList( input.AlternativeOccupationType ));

				//skip if no occupations
				if ( output.OccupationTypes.Count() == 0 
					&& UtilityManager.GetAppKeyValue( "skipCredImportIfNoOccupations", false ))
				{
					//LoggingHelper.DoTrace( 2, string.Format( "		***Skipping Credential# {0}, {1} as it has no occupations and this is a special run.", output.Id, output.Name ) );
					//return true;
				}
                //Industries
                output.IndustryTypes = helper.MapCAOListToCAOProfileList( input.IndustryType );
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
                output.CopyrightHolders = helper.MapOrganizationReferenceGuids( "Credential.CopyrightHolder", input.CopyrightHolder, ref status );
                //CAO
                output.AudienceLevelType = helper.MapCAOListToEnumermation( input.AudienceLevelType );
                //
                output.AudienceType = helper.MapCAOListToEnumermation( input.AudienceType );
                output.DegreeConcentration = helper.MapCAOListToTextValueProfile( input.DegreeConcentration, CodesManager.PROPERTY_CATEGORY_DEGREE_CONCENTRATION );
                output.DegreeMajor = helper.MapCAOListToTextValueProfile( input.DegreeMajor, CodesManager.PROPERTY_CATEGORY_DEGREE_MAJOR );
                output.DegreeMinor = helper.MapCAOListToTextValueProfile( input.DegreeMinor, CodesManager.PROPERTY_CATEGORY_DEGREE_MINOR );
				//only true should be published. Ensure the save only saves True
				output.IsNonCredit = input.IsNonCredit;

				output.AssessmentDeliveryType = helper.MapCAOListToEnumermation( input.AssessmentDeliveryType );
				output.LearningDeliveryType = helper.MapCAOListToEnumermation( input.LearningDeliveryType );

				//EstimatedCost
				//will need to format, all populate Entity.RelatedCosts (for bubble up) - actually this would be for asmts, and lopps
				output.EstimatedCost = helper.FormatCosts( input.EstimatedCost, ref status );

                //EstimatedDuration
                output.EstimatedDuration = helper.FormatDuration( input.EstimatedDuration, ref status );
                output.RenewalFrequency = helper.FormatDurationItem( input.RenewalFrequency );
               
                //conditions
				if ( legacyInput != null && legacyInput.Requires != null )
				{
					output.Requires = helper.FormatConditionProfile( legacyInput.Requires, ref status );
				} else
                {
					output.Requires = helper.FormatConditionProfile( input.Requires, ref status );
				}
				output.Recommends = helper.FormatConditionProfile( input.Recommends, ref status );
                output.Renewal = helper.FormatConditionProfile( input.Renewal, ref status );
                output.Corequisite = helper.FormatConditionProfile( input.Corequisite, ref status );
                output.Revocation = helper.FormatRevocationProfile( input.Revocation, ref status );

                //connections
                output.AdvancedStandingFrom = helper.FormatConditionProfile( input.AdvancedStandingFrom, ref status );
                output.IsAdvancedStandingFor = helper.FormatConditionProfile( input.IsAdvancedStandingFor, ref status );

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
				try
				{
					output.HasETPLResourceUids = helper.MapEntityReferenceGuids( "Credential.HasETPLResource", input.HasETPLResource, 0, ref status );
					if ( output.HasETPLResourceUids != null && output.HasETPLResourceUids.Count() > 0 )
					{
						var tloList = ProfileServices.ResolveToTopLevelObject( output.HasETPLResourceUids, "Credential.HasETPLResource", ref status );
						//could save the TLO, although we really only need the id later
						if ( tloList != null && tloList.Any() )
						{
							output.HasETPLCredentialsIds = ExtractEntityIds( tloList, 1 );
							output.HasETPLAssessmentsIds = ExtractEntityIds( tloList, 3 );
							output.HasETPLLoppsIds = ExtractEntityIds( tloList, 7 );
							//output.HasETPLCredentialsIds = ( List<int> )tloList.Where( s => s.EntityTypeId == 1 ).Select( g => g.Id );
							//output.HasETPLAssessmentsIds = ( List<int> )tloList.Where( s => s.EntityTypeId == 3 ).Select( g => g.Id );
							//output.HasETPLLoppsIds = ( List<int> )tloList.Where( s => s.EntityTypeId == 7 ).Select( g => g.Id );
						}

						//output.HasETPLAssessments = tloList.Where( s => s.EntityTypeId == 3 ).ToList();
						////
						//output.HasETPLCredentials = tloList.Where( s => s.EntityTypeId == 1 ).ToList();
						//var inputIds = output.HasETPLCredentials.Select( x => x.Id ).ToList();
						////
						//output.HasETPLLopps = tloList.Where( s => s.EntityTypeId == 7 ).ToList();
					}
				}catch(Exception ex)
				{
					LoggingHelper.LogError( ex, string.Format( "CredentialImport. Exception encountered for CTID: {0} during ETPL section", ctid ), false, "Credential Import ETPL exception" );
				}
				//Process profiles
				output.AdministrationProcess = helper.FormatProcessProfile( input.AdministrationProcess, ref status );
                output.DevelopmentProcess = helper.FormatProcessProfile( input.DevelopmentProcess, ref status );
                output.MaintenanceProcess = helper.FormatProcessProfile( input.MaintenanceProcess, ref status );

                output.AppealProcess = helper.FormatProcessProfile( input.AppealProcess, ref status );
                output.ComplaintProcess = helper.FormatProcessProfile( input.ComplaintProcess, ref status );
                output.ReviewProcess = helper.FormatProcessProfile( input.ReviewProcess, ref status );
                output.RevocationProcess = helper.FormatProcessProfile( input.RevocationProcess, ref status );
				//SameAs URI
				output.SameAs = helper.MapToTextValueProfile( input.SameAs );
				//FinancialAssistance
				output.FinancialAssistance = helper.FormatFinancialAssistance( input.FinancialAssistance, ref status );
				if ( output.FinancialAssistance != null && output.FinancialAssistance.Any() )
					output.FinancialAssistanceJson = JsonConvert.SerializeObject( output.FinancialAssistance, MappingHelperV3.GetJsonSettings() );
				//
				if ( input.AggregateData != null && input.AggregateData.Any() )
				{
					output.AggregateData = helper.FormatAggregateDataProfile( output.CTID, input.AggregateData, bnodes, ref status );
				}
				//TODO
				//these would be a list of URIs that should reference the earningsProfiles list
				//21-02-22 mparsons - these may start having data - but will be an embedded object
				if ( input.EarningsList != null && input.EarningsList.Any() )
				{
					output.Earnings = helper.FormatEarningsProfile( input.EarningsList, outcomesDTO, bnodes, ref status );
				}
				else
				{
					output.Earnings = helper.FormatEarningsProfile( earningsProfiles, outcomesDTO, bnodes, ref status );
				}
				//input.Holders is now (soon) an enbedded list of holdersProfiles
				//if the xxxList properties have data, then should be the new way - otherwise want to ignore Holders as will likely have type errors.
				if ( input.HoldersList != null && input.HoldersList.Any() )
				{
					output.Holders = helper.FormatHoldersProfile( output.CTID, input.HoldersList, outcomesDTO, bnodes, ref status );
				}
				else
				{
					output.Holders = helper.FormatHoldersProfile( output.CTID, holdersProfiles, outcomesDTO, bnodes, ref status );
				}
				//
				if ( input.EmploymentOutcomeList != null && input.EmploymentOutcomeList.Any() )
				{
					output.EmploymentOutcome = helper.FormatEmploymentOutcomeProfile( input.EmploymentOutcomeList, outcomesDTO, bnodes, ref status );
				}
				else
				{
					output.EmploymentOutcome = helper.FormatEmploymentOutcomeProfile( employmentOutcomeProfiles, outcomesDTO, bnodes, ref status );
				}
				//
				output.Addresses = helper.FormatAvailableAtAddresses( input.AvailableAt, ref status );
				if ( output.Addresses != null && output.Addresses.Any() )
				{
					//ISSUE - WE ARE SAVING HERE THE REGION GETS NORMALIZED!
					//NOTE: now done in FormatAvailableAtAddresses
					//foreach ( var item in output.Addresses)
     //               {
					//	//note: a risk as country may be empty. Acceptable for now
					//	if (item.AddressRegion?.Length == 2)
     //                   {
					//		var fullRegion = "";
					//		int boostLevel = 1;
					//		if ( CodesManager.Codes_IsState( item.AddressRegion, ref fullRegion, ref boostLevel ) )
					//		{
					//			item.AddressRegion = fullRegion;
					//		}
					//	}
     //               }
					output.CredentialExternalProperties.Addresses = output.Addresses;
					//this can be expanded to add more properties, so should do assignment later - in MapToDB?
					output.JsonProperties = JsonConvert.SerializeObject( output.CredentialExternalProperties, MappingHelperV3.GetJsonSettings() );
					//output.JsonProperties = JsonConvert.SerializeObject( output.Addresses, MappingHelperV3.GetJsonSettings() );
				}
				//
				//21-07-22 mparsons - TargetPathway should be an inverse relationship and not published with a credential
				//					- so OBSOLETE?
				//if ( input.TargetPathway != null && input.TargetPathway.Count > 0 )
				//	output.TargetPathwayIds = helper.MapEntityReferences( "Credential.TargetPathway", input.TargetPathway, CodesManager.ENTITY_TYPE_PATHWAY, ref status );
				//INs
				output.AccreditedIn = helper.MapToJurisdiction( input.AccreditedIn, ref status );
                output.ApprovedIn = helper.MapToJurisdiction( input.ApprovedIn, ref status );
                output.ApprovedIn = helper.MapToJurisdiction( input.ApprovedIn, ref status );
				output.OfferedIn = helper.MapToJurisdiction( input.OfferedIn, ref status );
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
                    ThisEntity entity = EntityServices.GetMinimumByCtid( ctid );
                    if ( entity != null && entity.Id > 0 )
                    {
						//in this case, should start over
                        output.Id = entity.Id;
                        output.RowId = entity.RowId;
                    }
                }
				//================= save the data ========================================
				if(UtilityManager.GetAppKeyValue( "writingToFinderDatabase", true))
					importSuccessfull = mgr.Import( output, ref status );

				
				saveDuration = DateTime.Now.Subtract( saveStarted );
				if ( saveDuration.TotalSeconds > 5 )
					LoggingHelper.DoTrace( 6, string.Format( "         WARNING SAVE Duration: {0:N2} seconds ", saveDuration.TotalSeconds ) );
				status.DocumentId = output.Id;
                status.DetailPageUrl = string.Format( "~/credential/{0}", output.Id );
                status.DocumentRowId = output.RowId;
				//
				//========== if requested call method to send to external API
				if ( !string.IsNullOrWhiteSpace( UtilityManager.GetAppKeyValue( "myExternalAPIEndpoint" ) ) )
				{
					//21-11-20 - concept: enable ability to call an external endpoint with import data
					//			HOWEVER. at this point there can be a lot of unresolved references.
					//			May need to consider how to call at end of import or on demand
					var request = new CredentialRegistryResource()
					{
						EntityType = "Credential",
						CTID = output.CTID,
						Name = output.Name,
						Description = output.Description,
						SubjectWebpage = output.SubjectWebpage,
						OwningOrganizationCTID = owningOrganizationCTID,
						CredentialFinderObject = output,
						DownloadDate = DateTime.Now,
						Created = status.EnvelopeCreatedDate,
						LastUpdated = status.EnvelopeUpdatedDate
					};
					var url = UtilityManager.GetAppKeyValue( "myExternalAPIEndpoint" );
					new ExternalServices().PostResourceToExternalService( url, request, ref messages );
				}
				//========== check if to be written to the generic CredentialRegistryDownload.Resource table
				if ( UtilityManager.GetAppKeyValue( "writingToDownloadResourceDatabase", false ) )
				{
					var resource = "";
					resource = JsonConvert.SerializeObject( output, JsonHelper.GetJsonSettings() );
					//resource = output.ToString();
					var request = new CredentialRegistryResource()
					{
						EntityType = "Credential",
						CTID = output.CTID,
						Name = output.Name,
						Description = output.Description,
						SubjectWebpage = output.SubjectWebpage,
						OwningOrganizationCTID = owningOrganizationCTID,
						CredentialFinderObject = resource,
						CredentialRegistryGraph = payload,
						DownloadDate = DateTime.Now,
						Created = status.EnvelopeCreatedDate,
						LastUpdated = status.EnvelopeUpdatedDate
					};
					new ExternalServices().DownloadSave( request, ref messages );
				}
				//
				//if record was added to db, add to/or set EntityResolution as resolved
				int ierId = new ImportManager().Import_EntityResolutionAdd( status.ResourceURL,
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
                LoggingHelper.LogError( ex, thisClassName + ".ImportV3", string.Format( "Exception encountered for CTID: {0}", ctid ), false, "Credential Import exception" );
            }
			finally
			{
				var totalDuration = DateTime.Now.Subtract( started );
				if ( totalDuration.TotalSeconds > 9 && ( totalDuration.TotalSeconds - saveDuration.TotalSeconds > 3 ) )
					LoggingHelper.DoTrace( 5, string.Format( "         WARNING Total Duration: {0:N2} seconds ", totalDuration.TotalSeconds ) );

			}
			return importSuccessfull;
		}


		public List<int> ExtractEntityIds( List<TopLevelObject> list, int entityTypeId)
		{
			var output = new List<int>();
			var results = list.Where( s => s.EntityTypeId == entityTypeId ).ToList();
			if (results != null && results.Any())
			{
				output = results.Select( x => x.Id ).ToList();
			}

			return output;
		}

        public bool DoesEntityExist( string ctid, ref ThisEntity entity )
        {
            bool exists = false;
            entity = EntityServices.GetMinimumByCtid( ctid );
            if ( entity != null && entity.Id > 0 )
                return true;

            return exists;
        }
		
	}
}
