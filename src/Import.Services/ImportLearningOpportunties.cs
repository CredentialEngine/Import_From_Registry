using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using workIT.Utilities;

using EntityServices = workIT.Services.LearningOpportunityServices;
//using InputResource = RA.Models.Json.LearningOpportunityProfile;

using InputEntityV3 = RA.Models.JsonV2.LearningOpportunityProfile;
using JsonInput = RA.Models.JsonV2;
using BNode = RA.Models.JsonV2.BlankNode;
using ThisResource = workIT.Models.ProfileModels.LearningOpportunityProfile;
using workIT.Factories;
using workIT.Models;
using workIT.Models.ProfileModels;
using FAPI = workIT.Services.API;
using workIT.Models.Common;
using workIT.Services;

namespace Import.Services
{
    public class ImportLearningOpportunties
    {
        int thisEntityTypeId = CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE;
        string thisClassName = "ImportLearningOpportunties";
        string resourceType = "LearningOpportunity";
        ImportManager importManager = new ImportManager();
        ImportServiceHelpers importHelper = new ImportServiceHelpers();
        //InputResource input = new InputResource();
        ThisResource output = new ThisResource();

        #region custom imports
        public void ImportPendingRecords()
        {
            string where = " [EntityStateId] = 1 ";
            int pTotalRows = 0;

            SaveStatus status = new SaveStatus();
            List<ThisResource> list = LearningOpportunityManager.Search( where, "", 1, 500, ref pTotalRows );
            LoggingHelper.DoTrace( 1, string.Format( thisClassName + " - ImportPendingRecords(). Processing {0} records =================", pTotalRows ) );
            foreach ( ThisResource item in list )
            {
                status = new SaveStatus();
				//SWP contains the resource url
				//pending records will have a  CTID, it should be used to get the envelope!
				//if ( !ImportByResourceUrl( item.SubjectWebpage, status ) )
				if ( !ImportByCtid( item.CTID, status ) )
                {
                    //check for 404
                    LoggingHelper.DoTrace( 1, string.Format( "     - (). Failed to import pending record: {0}, message(s): {1}", item.Id, status.GetErrorsAsString() ) );
                }
                else
                    LoggingHelper.DoTrace( 1, string.Format( "     - (). Successfully imported pending record: {0}", item.Id ) );
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
			//**process
            bool importSuccessfull = ProcessEnvelope( item, status );
            List<string> messages = new List<string>();
            string importError = string.Join( "\r\n", status.GetAllMessages().ToArray() );
            //store envelope
            int newImportId = importHelper.Add( item, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, status.Ctid, importSuccessfull, importError, ref messages );
            if ( newImportId > 0 && status.Messages != null && status.Messages.Count > 0 )
            {
                //add indicator of current recored
                string msg = string.Format( "========= Messages for {4}, EnvelopeIdentifier: {0}, ctid: {1}, Id: {2}, rowId: {3} =========", item.EnvelopeIdentifier, status.Ctid, status.DocumentId, status.DocumentRowId, thisClassName );
                importHelper.AddMessages( newImportId, status, ref messages );
            }
            return importSuccessfull;
        }
		/// <summary>
		/// Process a learning opportunity or its subclasses of LearningProgram, and Course
		/// </summary>
		/// <param name="item"></param>
		/// <param name="status"></param>
		/// <returns></returns>
        public bool ProcessEnvelope( ReadEnvelope item, SaveStatus status )
        {
            if ( item == null || string.IsNullOrWhiteSpace( item.EnvelopeIdentifier ) )
            {
                status.AddError( "A valid ReadEnvelope must be provided." );
                return false;
            }
            //
            DateTime createDate = DateTime.Now;
            DateTime envelopeUpdateDate = DateTime.Now;
            if ( DateTime.TryParse( item.NodeHeaders.CreatedAt.Replace( "UTC", "" ).Trim(), out createDate ) )
            {
                status.SetEnvelopeCreated( createDate );
            }
            if ( DateTime.TryParse( item.NodeHeaders.UpdatedAt.Replace( "UTC", "" ).Trim(), out envelopeUpdateDate ) )
            {
                status.SetEnvelopeUpdated( envelopeUpdateDate );
            }
			status.DocumentOwnedBy = item.documentOwnedBy;
			if ( item.documentPublishedBy != null )
			{
				if ( item.documentOwnedBy == null || ( item.documentPublishedBy != item.documentOwnedBy ) )
					status.DocumentPublishedBy = item.documentPublishedBy;
			}
			else
			{
				//will need to check elsewhere
				//OR as part of import check if existing one had 3rd party publisher
			}
			//
			string payload = item.DecodedResource.ToString();
            status.EnvelopeId = item.EnvelopeIdentifier;

            return ImportV3( item.EnvelopeCtdlType, payload, status );
        }

		/// <summary>
		/// Import a learning opportunity class or subclass.
		/// </summary>
		/// <param name="learningOppClass"></param>
		/// <param name="payload"></param>
		/// <param name="status"></param>
		/// <returns></returns>
        public bool ImportV3( string learningOppClass, string payload, SaveStatus status )
        {
			LoggingHelper.DoTrace( 7, thisClassName + ".ImportV3 - entered." );
			DateTime started = DateTime.Now;
			var saveDuration = new TimeSpan();
			var dataSetProfiles = new List<JsonInput.QData.DataSetProfile>();
			var outcomesDTO = new OutcomesDTO();
			//HMMM - this will be in the payload that is deserialized to the JSON-LD input class
			if ( string.IsNullOrWhiteSpace( learningOppClass ) )
				learningOppClass = "LearningOpportunity";
			learningOppClass = learningOppClass.Replace( "ceterms:", "" );


			InputEntityV3 input = new InputEntityV3();
            var bnodes = new List<BNode>();
            var mainEntity = new Dictionary<string, object>();

            //status.AddWarning( "The resource uses @graph and is not handled yet" );

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
                else
                {
                    var bn = item.ToString();
                    bnodes.Add( JsonConvert.DeserializeObject<BNode>( bn ) );
                }

            }

            List<string> messages = new List<string>();
            bool importSuccessfull = false;
            EntityServices mgr = new EntityServices();
            MappingHelperV3 helper = new MappingHelperV3( thisEntityTypeId );
          
            helper.entityBlankNodes = bnodes;
            helper.CurrentEntityCTID = input.CTID;

            helper.CurrentEntityName = input.Name.ToString();
            string ctid = input.CTID;
            status.ResourceURL = input.CtdlId;
            LoggingHelper.DoTrace( 5, "		name: " + input.Name.ToString() );
            //LoggingHelper.DoTrace( 6, "		url: " + input.SubjectWebpage );
            //LoggingHelper.DoTrace( 5, "		ctid: " + input.Ctid );
            LoggingHelper.DoTrace( 5, "		@Id: " + input.CtdlId );
            status.Ctid = ctid;

            if ( status.DoingDownloadOnly )
                return true;

			try
			{
				if ( !DoesEntityExist( input.CTID, ref output ) )
				{
					//set the rowid now, so that can be referenced as needed
					output.RowId = Guid.NewGuid();
					LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".ImportV3(). Record was NOT found using CTID: '{0}'", input.CTID ) );
				}
				else
				{
					LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".ImportV3(). Found record: '{0}' using CTID: '{1}'", input.Name, input.CTID ) );
				}
				helper.currentBaseObject = output;

                //moved this code from just an add, to handle changes to the type
                switch ( learningOppClass.ToLower() )
                {
                    case "learningopportunity":
                    case "learningopportunityprofile":
                        output.LearningEntityType = "LearningOpportunity";
                        output.LearningEntityTypeId = 7;
                        break;
                    case "learningprogram":
                        output.LearningEntityType = "LearningProgram";
                        output.LearningEntityTypeId = CodesManager.ENTITY_TYPE_LEARNING_PROGRAM;
                        break;
                    case "course":
                        output.LearningEntityType = "Course";
                        output.LearningEntityTypeId = CodesManager.ENTITY_TYPE_COURSE;
                        break;
                    default:
                        messages.Add( string.Format( "CTID: {0}. Invalid value for Learning Type: '{1}. Valid values are: LearningOpportunity/LearningOpportunityProfile, LearningProgram or Course", ctid, learningOppClass ) );
                        output.LearningEntityType = "LearningOpportunity";
                        output.LearningEntityTypeId = 7;
                        break;
                }
                output.EntityTypeId = output.LearningEntityTypeId;


                //start with language and may use with language maps
                output.InLanguageCodeList = helper.MapInLanguageToTextValueProfile( input.InLanguage, "LearningOpportunity.InLanguage. CTID: " + ctid );

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
				output.CTID = input.CTID;
				output.Name = helper.HandleLanguageMap( input.Name, output, "Name" );
				output.Description = helper.HandleLanguageMap( input.Description, output, "Description" );
				output.Keyword = helper.MapToTextValueProfile( input.Keyword, output, "Keyword" );
				output.LearningEntityType = learningOppClass;


				output.CredentialRegistryId = status.EnvelopeId;
				//TBD handling of referencing third party publisher
				helper.MapOrganizationPublishedBy( output, ref status );
				//if ( !string.IsNullOrWhiteSpace( status.DocumentPublishedBy ) )
				//{
				//	//output.PublishedByOrganizationCTID = status.DocumentPublishedBy;
				//	var porg = OrganizationManager.GetSummaryByCtid( status.DocumentPublishedBy );
				//	if ( porg != null && porg.Id > 0 )
				//	{
				//		//TODO - store this in a json blob??????????
				//		output.PublishedByThirdPartyOrganizationId = porg.Id;
				//		//output.PublishedByOrganizationName = porg.Name;
				//		//this will result in being added to Entity.AgentRelationship
				//		output.PublishedBy = new List<Guid>() { porg.RowId };
				//	}
				//	else
				//	{
				//		//if publisher not imported yet, all publishee stuff will be orphaned
				//		var entityUid = Guid.NewGuid();
				//		var statusMsg = "";
				//		var resPos = status.ResourceURL.IndexOf( "/resources/" );
				//		var swp = status.ResourceURL.Substring( 0, ( resPos + "/resources/".Length ) ) + status.DocumentPublishedBy;
				//		int orgId = new OrganizationManager().AddPendingRecord( entityUid, status.DocumentPublishedBy, swp, ref status );
				//		output.PublishedBy = new List<Guid>() { entityUid };
				//	}
				//}
				//else
				//{
				//	//may need a check for existing published by to ensure not lost
				//	if ( output.Id > 0 )
				//	{
				//		if ( output.OrganizationRole != null && output.OrganizationRole.Any() )
				//		{
				//			var publishedByList = output.OrganizationRole.Where( s => s.RoleTypeId == 30 ).ToList();
				//			if ( publishedByList != null && publishedByList.Any() )
				//			{
				//				var pby = publishedByList[ 0 ].ActingAgentUid;
				//				output.PublishedBy = new List<Guid>() { publishedByList[ 0 ].ActingAgentUid };

				//			}
				//		}

				//	}
				//}
				output.LifeCycleStatusType = helper.MapCAOToEnumermation( input.LifeCycleStatusType );

				//BYs - do owned and offered first
				//NEW: handling examples with github URIs
				output.OwnedBy = helper.MapOrganizationReferenceGuids( "LearningOpp.OwnedBy", input.OwnedBy, ref status );
				output.OfferedBy = helper.MapOrganizationReferenceGuids( "LearningOpp.OfferedBy", input.OfferedBy, ref status );
				if ( output.OwnedBy != null && output.OwnedBy.Count > 0 )
				{
					output.PrimaryAgentUID = output.OwnedBy[ 0 ];
					helper.CurrentOwningAgentUid = output.OwnedBy[ 0 ];
				}
				else
				{
					if ( input.OwnedBy != null && input.OwnedBy.Count > 0 )
					{
						//do a get 
						var ctdlType = "";
						var statusMessage = "";
						var resource = RegistryServices.GetNonRegistryResourceByUrl( input.OwnedBy[0], ref ctdlType, ref statusMessage );

						if ( ctdlType  == "Organization" )
						{
							var inputOrg = JsonConvert.DeserializeObject<RA.Models.JsonV2.Agent>( resource );
							var outputOrg = new Organization();

							if (!string.IsNullOrWhiteSpace( inputOrg.CTID) )
							{
								var existingOrg = OrganizationServices.GetMinimumSummaryByCtid( input.CTID );
								if ( existingOrg != null &&  existingOrg.Id > 0 )
								{
									output.PrimaryAgentUID = existingOrg.RowId;
									helper.CurrentOwningAgentUid = existingOrg.RowId;
								} else
								{
									ImportOrganization orgImport = new ImportOrganization();
									//may want a separate status?
									if (orgImport.HandleExternalRequest( inputOrg, ref outputOrg, ref status ) || outputOrg.Id > 0)
									{
										output.PrimaryAgentUID = outputOrg.RowId;
										helper.CurrentOwningAgentUid = outputOrg.RowId;
									} else
									{

									}
									/*
									//either import now, or create a pending record.
									//orgImport.CommonMapping( inputOrg, helper, ref outputOrg, ref status );	
									
									if ( new OrganizationServices().Import( outputOrg, ref status ) || outputOrg .Id > 0)
									{
										output.PrimaryAgentUID = outputOrg.RowId;
										helper.CurrentOwningAgentUid = outputOrg.RowId;
									} else
									{

									}
									*/

								}
							}
							

						} else
						{
							//invalid type
							status.AddError( $"{thisClassName}.ImportV3 ({input.CTID}) Encountered a non registry Organization URL that does not reference an organization type: {input.OwnedBy[ 0 ]}" );
						}
					}
					//add warning?
					if ( output.OfferedBy == null || output.OfferedBy.Count == 0 )
					{
						status.AddWarning( "document doesn't have an owning or offering organization." );
					}
					else
					{
						output.PrimaryAgentUID = output.OfferedBy[ 0 ];
						helper.CurrentOwningAgentUid = output.OfferedBy[ 0 ];
					}
				}
				output.AccreditedBy = helper.MapOrganizationReferenceGuids( $"{resourceType}.AccreditedBy", input.AccreditedBy, ref status );
				output.ApprovedBy = helper.MapOrganizationReferenceGuids( $"{resourceType}.ApprovedBy", input.ApprovedBy, ref status );
				output.RecognizedBy = helper.MapOrganizationReferenceGuids( $"{resourceType}.RecognizedBy", input.RecognizedBy, ref status );
				output.RegulatedBy = helper.MapOrganizationReferenceGuids( $"{resourceType}.RegulatedBy", input.RegulatedBy, ref status );
                //RegisteredBy
                output.RegisteredBy = helper.MapOrganizationReferenceGuids( $"{resourceType}.RegisteredBy", input.RegisteredBy, ref status );
                output.DateEffective = input.DateEffective;
				output.ExpirationDate = input.ExpirationDate;

				output.SubjectWebpage = input.SubjectWebpage;



				output.AvailabilityListing = helper.MapListToString( input.AvailabilityListing );
				output.AvailableOnlineAt = helper.MapListToString( input.AvailableOnlineAt );
				output.DeliveryType = helper.MapCAOListToEnumermation( input.DeliveryType );
				output.DeliveryTypeDescription = helper.HandleLanguageMap( input.DeliveryTypeDescription, output, $"{resourceType}.DeliveryTypeDescription" );
				//AudienceType
				output.AudienceType = helper.MapCAOListToEnumermation( input.AudienceType );
				//CAO
				output.AudienceLevelType = helper.MapCAOListToEnumermation( input.AudienceLevelType );
				
				output.CodedNotation = input.CodedNotation;
				//=========================================
				output.Identifier = helper.MapIdentifierValueList( input.Identifier );
				output.IdentifierNew = helper.MapIdentifierValueListInternal( input.Identifier );

				if ( output.IdentifierNew != null && output.IdentifierNew.Count() > 0 )
				{
					//24-03-05 mp - this not being used yet, so why???
					output.IdentifierJSON = JsonConvert.SerializeObject( output.IdentifierNew, MappingHelperV3.GetJsonSettings() );
				}
				output.VersionIdentifierList = helper.MapIdentifierValueList( input.VersionIdentifier );
				output.VersionIdentifierNew = helper.MapIdentifierValueListInternal( input.VersionIdentifier );
				if ( output.VersionIdentifierNew != null && output.VersionIdentifierNew.Count() > 0 )
				{
					output.VersionIdentifierJSON = JsonConvert.SerializeObject( output.VersionIdentifierNew, MappingHelperV3.GetJsonSettings() );
				}
				//
				//handle QuantitativeValue
				//output.CreditValue = helper.HandleQuantitiveValue( input.CreditValue, "LearningOpportunity.CreditValue" );
				//output.QVCreditValueList = helper.HandleValueProfileListToQVList( input.CreditValue, "LearningOpportunity.CreditValue" );
				output.CreditValue = helper.HandleValueProfileList( input.CreditValue, $"{resourceType}.CreditValue" );
				output.CreditValueJson = JsonConvert.SerializeObject( output.CreditValue, MappingHelperV3.GetJsonSettings() );
				//output.CreditValueJson = JsonConvert.SerializeObject( output.CreditValueList, MappingHelperV3.GetJsonSettings() );
				//if ( output.CreditValueList != null && output.CreditValueList.Any() )
				//	output.CreditValue = output.CreditValueList[ 0 ];
				//
				//note can still have CreditUnitTypeDescription by itself. What to do if both?
				output.CreditUnitTypeDescription = helper.HandleLanguageMap( input.CreditUnitTypeDescription, output, $"{resourceType}.CreditUnitTypeDescription" );


				//only true should be published. Ensure the save only saves True
				output.IsNonCredit = input.IsNonCredit;

				//occupations
				//output.Occupation = helper.MapCAOListToEnumermation( input.OccupationType );
				//actually used by import
				output.OccupationTypes = helper.MapCAOListToCAOProfileList( input.OccupationType );
				//just append alternative items. Ensure empty lists are ignored
				//output.Occupations.AddRange( helper.AppendLanguageMapListToCAOProfileList( input.AlternativeOccupationType ) );

				//skip if no occupations
				if ( output.OccupationTypes.Count() == 0
					&& UtilityManager.GetAppKeyValue( "skipCredImportIfNoOccupations", false ) )
				{
					//LoggingHelper.DoTrace( 2, string.Format( "		***Skipping lopp# {0}, {1} as it has no occupations and this is a special run.", output.Id, output.Name ) );
					//return true;
				}
				//Industries
				output.IndustryTypes = helper.MapCAOListToCAOProfileList( input.IndustryType );
				//output.Industries.AddRange( helper.AppendLanguageMapListToCAOProfileList( input.AlternativeIndustryType ) );
				//naics
				//output.Naics = input.Naics;

				output.InstructionalProgramTypes = helper.MapCAOListToCAOProfileList( input.InstructionalProgramType );
				//output.InstructionalProgramTypes.AddRange( helper.AppendLanguageMapListToCAOProfileList( input.AlternativeInstructionalProgramType ) );
				if ( output.InstructionalProgramTypes.Count() == 0 && UtilityManager.GetAppKeyValue( "skipAsmtImportIfNoCIP", false ) )
				{
					//skip
					//LoggingHelper.DoTrace( 2, string.Format( "		***Skipping lopp# {0}, {1} as it has no InstructionalProgramTypes and this is a special run.", output.Id, output.Name ) );
					//return true;
				}

				output.LearningMethodType = helper.MapCAOListToEnumermation( input.LearningMethodType );
				output.Subject = helper.MapCAOListToTextValueProfile( input.Subject, CodesManager.PROPERTY_CATEGORY_SUBJECT );
				output.SupersededBy = input.SupersededBy ?? "";
				output.Supersedes = input.Supersedes ?? "";
				if ( output.SupersededBy.ToLower().IndexOf( "/resources/ce-" ) > -1 )
				{
					output.SupersededBy = helper.FormatFinderResourcesURL( output.SupersededBy ); 
				}
				if ( output.Supersedes.ToLower().IndexOf( "/resources/ce-" ) > -1 )
				{
					output.Supersedes = helper.FormatFinderResourcesURL( output.Supersedes ); 
				}
				//
				output.AssessmentMethodType = helper.MapCAOListToEnumermation( input.AssessmentMethodType );

				output.AssessmentMethodDescription = helper.HandleLanguageMap( input.AssessmentMethodDescription, output, "AssessmentMethodDescription" );
				//
				output.LearningMethodDescription = helper.HandleLanguageMap( input.LearningMethodDescription, output, "LearningMethodDescription" );

				output.ScheduleTimingType = helper.MapCAOListToEnumermation( input.ScheduleTimingType );
				output.ScheduleFrequencyType = helper.MapCAOListToEnumermation( input.ScheduleFrequencyType );
				output.OfferFrequencyType = helper.MapCAOListToEnumermation( input.OfferFrequencyType );

				// TransferValue Profile
				if ( input.ProvidesTransferValueFor != null && input.ProvidesTransferValueFor.Count > 0 )
					output.ProvidesTVForIds = helper.MapEntityReferences( $"{resourceType}.ProvidesTransferValueFor", input.ProvidesTransferValueFor, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, ref status );
				if ( input.ReceivesTransferValueFrom != null && input.ReceivesTransferValueFrom.Count > 0 )
					output.ReceivesTVFromIds = helper.MapEntityReferences( $"{resourceType}.ProvidesTransferValueFor", input.ReceivesTransferValueFrom, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, ref status );
				//
				//Target Action 
				if ( input.ObjectOfAction != null && input.ObjectOfAction.Count > 0 )
					output.ObjectOfActionIds = helper.MapEntityReferences( $"{resourceType}.ObjectOfAction", input.ObjectOfAction, CodesManager.ENTITY_TYPE_CREDENTIALING_ACTION, ref status );

				//output.VerificationMethodDescription = helper.HandleLanguageMap( input.VerificationMethodDescription, output, "VerificationMethodDescription" );
				//financial assitance
				output.FinancialAssistance = helper.FormatFinancialAssistance( input.FinancialAssistance, ref status );
				if ( output.FinancialAssistance != null && output.FinancialAssistance.Any() )
					output.FinancialAssistanceJson = JsonConvert.SerializeObject( output.FinancialAssistance, MappingHelperV3.GetJsonSettings() );
				//TODO - are we handling prior dsps not in this import?
				//23-02-12 currently
				//		- we are doing a check for dsp and as needed adding pending
				//		- but in the entity.adpManager, all eadps are deleted, and so would the datasetprofile which means the eadp.ReleventDatasetIds will be wrong (no longer exist)
				//		So:
				//		-
				//		- 
				bool hasDataSetProfiles = false;
                List<string> ctidList = new List<string>();
                output.AggregateData = helper.FormatAggregateDataProfile( output.CTID, input.AggregateData, ref status, ref ctidList );
				if ( ctidList != null && ctidList.Any() )
				{
					//especially for one-time adhoc imports, may want a reminder to import the dsp as well. Well would be good to have the actual dsp ctid to pass back
					hasDataSetProfiles = true;
					output.DataSetProfileCTIDList = ctidList;
					output.DataSetProfileCount= ctidList.Count;
					//also what to do with current process that does a virtual delete on delete of the e.adp?
					//No adp
					//- if there is no adp in this run, then the latter is good (will need a process for orphan dsps!)
					//Have adp
					//- if there is an adp and the related dsp didn't change, then it will not be imported
					//			==> plug this hole
					//- any included dsp links have to be valid. 
					//	- after completing lopp or just adp, reactivate all dsps in this list
                }
				//
				output.Jurisdiction = helper.MapToJurisdiction( input.Jurisdiction, ref status );

				//***EstimatedCost
				//will need to format, all populate Entity.RelatedCosts (for bubble up) - actually this would be for asmts, and lopps
				output.EstimatedCost = helper.FormatCosts( input.EstimatedCost, ref status );
				//connections
				output.AdvancedStandingFrom = helper.FormatConditionProfile( input.AdvancedStandingFrom, ref status );
				output.IsAdvancedStandingFor = helper.FormatConditionProfile( input.IsAdvancedStandingFor, ref status );

				output.PreparationFrom = helper.FormatConditionProfile( input.PreparationFrom, ref status );
				output.IsPreparationFor = helper.FormatConditionProfile( input.IsPreparationFor, ref status );

				output.IsRequiredFor = helper.FormatConditionProfile( input.IsRequiredFor, ref status );
				output.IsRecommendedFor = helper.FormatConditionProfile( input.IsRecommendedFor, ref status );
				//
				output.DegreeConcentration = helper.MapCAOListToTextValueProfile( input.DegreeConcentration, CodesManager.PROPERTY_CATEGORY_DEGREE_CONCENTRATION );
				//EstimatedDuration
				//TODO - need to convert to handle decimals. First step, truncate 
				output.EstimatedDuration = helper.FormatDuration( $"{resourceType}.EstimatedDuration", input.EstimatedDuration, ref status );

				//conditions ======================================
				output.Requires = helper.FormatConditionProfile( input.Requires, ref status );
				output.Recommends = helper.FormatConditionProfile( input.Recommends, ref status );
				output.EntryCondition = helper.FormatConditionProfile( input.EntryCondition, ref status );
				output.Corequisite = helper.FormatConditionProfile( input.Corequisite, ref status );
				output.CoPrerequisite = helper.FormatConditionProfile( input.CoPrerequisite, ref status );

				//
				//SameAs URI
				output.SameAs = helper.MapToTextValueProfile( input.SameAs );

				output.SCED = input.SCED;
				//
				if ( input.TargetAssessment != null && input.TargetAssessment.Count > 0 )
					output.TargetAssessmentIds = helper.MapEntityReferences( $"{resourceType}.TargetAssessment", input.TargetAssessment, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, ref status );
				//this is an inverse property and would not be published like this
				//if ( input.TargetLearningOpportunity != null && input.TargetLearningOpportunity.Count > 0 )
				//	output.TargetLearningOppIds = helper.MapEntityReferences( "LearningOpportunity.TargetLearningOpportunity", input.TargetLearningOpportunity, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status );
				//
				//21-04-13 mp - TargetLearningResource will be URLs not registry resources
				if ( input.TargetLearningResource != null && input.TargetLearningResource.Count > 0 )
				{
					output.TargetLearningResource = input.TargetLearningResource;
					
				}
				//not sure if this will be used, or purely an inverse relationship
				output.TargetLearningOpportunityIds = helper.MapEntityReferences( $"{resourceType}.TargetLearningOpportunity", input.TargetLearningResource, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status );

				//this is an inverse property and would not be published with this resource
				//if ( input.TargetPathway != null && input.TargetPathway.Count > 0 )
				//	output.TargetPathwayIds = helper.MapEntityReferences( "LearningOpportunity.TargetPathway", input.TargetPathway, CodesManager.ENTITY_TYPE_PATHWAY, ref status );

				//assesses compentencies
				output.AssessesCompetencies = helper.MapCAOListToCAOProfileList( input.Assesses, true );
				//if ( output.AssessesCompetencies.Count() == 0 && UtilityManager.GetAppKeyValue( "skipAsmtImportIfNoCompetencies", false ) )
				//{
				//	//skip
				//	LoggingHelper.DoTrace( 2, string.Format( "		***Skipping Lopp# {0}, {1} as it has no competencies and this is a special run.", output.Id, output.Name ) );
				//	return true;
				//}
				//teaches compentencies
				output.TeachesCompetencies = helper.MapCAOListToCAOProfileList( input.Teaches, true );
				if ( output.TeachesCompetencies.Count() == 0 && UtilityManager.GetAppKeyValue( "skipLoppImportIfNoCompetencies", false ) )
				{
					//skip
					LoggingHelper.DoTrace( 2, string.Format( "		***Skipping lopp# {0}, {1} as it has no competencies and this is a special run.", output.Id, output.Name ) );
					return true;
				}
                //
                if ( input.HasOffering != null && input.HasOffering.Count > 0 )
                    output.HasOfferingIds = helper.MapEntityReferences( $"{resourceType}.HasOffering", input.HasOffering, CodesManager.ENTITY_TYPE_SCHEDULED_OFFERING, ref status );
				if ( input.HasRubric != null && input.HasRubric.Count > 0 )
					output.HasRubricIds = helper.MapEntityReferences( $"{resourceType}.HasRubric", input.HasRubric, CodesManager.ENTITY_TYPE_RUBRIC, ref status );
				if ( input.HasSupportService != null && input.HasSupportService.Count > 0 )
                    output.HasSupportServiceIds = helper.MapEntityReferences( $"{resourceType}.HasSupportService", input.HasSupportService, CodesManager.ENTITY_TYPE_SUPPORT_SERVICE, ref status );

				output.InCatalog = input.InCatalog;

				output.AlternateNames = helper.MapToTextValueProfile( input.AlternateName, output, "AlternateName" );

				//common conditions
				output.ConditionManifestIds = helper.MapEntityReferences( input.CommonConditions, CodesManager.ENTITY_TYPE_CONDITION_MANIFEST, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status );
				//common costs
				output.CostManifestIds = helper.MapEntityReferences( input.CommonCosts, CodesManager.ENTITY_TYPE_COST_MANIFEST, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status );

				//ADDRESSES
				output.AvailableAt = helper.FormatAvailableAtAddresses( input.AvailableAt, ref status );

				//INs
				output.AccreditedIn = helper.MapToJurisdiction( input.AccreditedIn, ref status );
				output.ApprovedIn = helper.MapToJurisdiction( input.ApprovedIn, ref status );
				output.ApprovedIn = helper.MapToJurisdiction( input.ApprovedIn, ref status );
				output.RecognizedIn = helper.MapToJurisdiction( input.RecognizedIn, ref status );
				output.RegulatedIn = helper.MapToJurisdiction( input.RegulatedIn, ref status );
				//TODO - do we need to specify Course here?
				//output.LearningEntityTypeId, The prereq must be a course, so use LearningEntityTypeId
				output.PrerequisiteIds = helper.MapEntityReferences( input.Prerequisite, output.LearningEntityTypeId, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status );

				//we don't know the actual type of the part, so stay generic
				output.HasPartIds = helper.MapEntityReferences( input.HasPart, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status );

				//var isPartIds = input.IsPartOf.Select( x => x.CtdlId ).ToList();
				output.IsPartOfIds = helper.MapEntityReferences( input.IsPartOf, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status );
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
					ThisResource entity = EntityServices.GetByCtid( ctid );
					if ( entity != null && entity.Id > 0 )
					{
						output.Id = entity.Id;
						output.RowId = entity.RowId;
					}
				}
				
                //================= save the data ========================================
                if ( UtilityManager.GetAppKeyValue( "writingToFinderDatabase", true ) )
                {
                    importSuccessfull = mgr.Import( output, ref status );

					//24-03-25 - use the generic process for blank nodes encountered during import
					new ProfileServices().IndexPrepForReferenceResource( helper.ResourcesToIndex, ref status );
				}
                //
                saveDuration = DateTime.Now.Subtract( saveStarted );
				if ( saveDuration.TotalSeconds > 5 )
					LoggingHelper.DoTrace( 6, string.Format( "         WARNING SAVE Duration: {0:N2} seconds ", saveDuration.TotalSeconds ) );
				//
				status.DocumentId = output.Id;
				status.DetailPageUrl = string.Format( "~/learningOpportunity/{0}", output.Id );
				status.DocumentRowId = output.RowId;

				//just in case
				if ( status.HasErrors )
					importSuccessfull = false;

				//if record was added to db, add to/or set EntityResolution as resolved
				int ierId = new ImportManager().Import_EntityResolutionAdd( status.ResourceURL,
							ctid, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE,
							output.RowId,
							output.Id,
							( output.Id > 0 ),
							ref messages,
							output.Id > 0 );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError(ex, string.Format(thisClassName + ".ImportV3 . Exception encountered for CTID: {0}", ctid));
			}
			finally
			{
				var totalDuration = DateTime.Now.Subtract( started );
				if ( totalDuration.TotalSeconds > 9 && ( totalDuration.TotalSeconds - saveDuration.TotalSeconds > 3 ) )
					LoggingHelper.DoTrace( 5, string.Format( "         WARNING Total Duration: {0:N2} seconds ", totalDuration.TotalSeconds ) );

			}
			return importSuccessfull;
        }


        public bool DoesEntityExist( string ctid, ref ThisResource entity )
        {
            bool exists = false;
            entity = EntityServices.GetByCtid( ctid );
            if ( entity != null && entity.Id > 0 )
                return true;

            return exists;
        }
    }
}
