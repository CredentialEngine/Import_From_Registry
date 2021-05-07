using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using workIT.Utilities;

using EntityServices = workIT.Services.LearningOpportunityServices;
using InputEntity = RA.Models.Json.LearningOpportunityProfile;

using InputEntityV3 = RA.Models.JsonV2.LearningOpportunityProfile;
using BNode = RA.Models.JsonV2.BlankNode;
using ThisEntity = workIT.Models.ProfileModels.LearningOpportunityProfile;
using workIT.Factories;
using workIT.Models;
using workIT.Models.ProfileModels;

namespace Import.Services
{
    public class ImportLearningOpportunties
    {
        int entityTypeId = CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE;
        string thisClassName = "ImportLearningOpportunties";
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
            List<ThisEntity> list = LearningOpportunityManager.Search( where, "", 1, 500, ref pTotalRows );
            LoggingHelper.DoTrace( 1, string.Format( thisClassName + " - ImportPendingRecords(). Processing {0} records =================", pTotalRows ) );
            foreach ( ThisEntity item in list )
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
        /// Retrieve an envelop from the registry and do import
        /// </summary>
        /// <param name="envelopeId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        //public bool ImportByEnvelopeId( string envelopeId, SaveStatus status )
        //{
        //    //this is currently specific, assumes envelop contains a credential
        //    //can use the hack fo GetResourceType to determine the type, and then call the appropriate import method

        //    if ( string.IsNullOrWhiteSpace( envelopeId ) )
        //    {
        //        status.AddError( thisClassName + ".ImportByEnvelope - a valid envelope id must be provided" );
        //        return false;
        //    }

        //    string statusMessage = "";
        //    EntityServices mgr = new EntityServices();
        //    string ctdlType = "";
        //    try
        //    {
        //        ReadEnvelope envelope = RegistryServices.GetEnvelope( envelopeId, ref statusMessage, ref ctdlType );
        //        if ( envelope != null && !string.IsNullOrWhiteSpace( envelope.EnvelopeIdentifier ) )
        //        {
        //            return CustomProcessEnvelope( envelope, status );
        //        }
        //        else
        //            return false;
        //    }
        //    catch ( Exception ex )
        //    {
        //        LoggingHelper.LogError( ex, thisClassName + ".ImportByEnvelopeId()" );
        //        status.AddError( ex.Message );
        //        if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
        //        {
        //            status.AddWarning( "The referenced registry document is using an old schema. Please republish it with the latest schema!" );
        //        }
        //        return false;
        //    }
        //}

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

    //    public bool ImportByResourceUrl( string resourceUrl, SaveStatus status )
    //    {
    //        if ( string.IsNullOrWhiteSpace( resourceUrl ) )
    //        {
    //            status.AddError( thisClassName + ".ImportByResourceUrl - a valid resourceUrl must be provided" );
    //            return false;
    //        }
    //        //this is currently specific, assumes envelop contains an organization
    //        //can use the hack for GetResourceType to determine the type, and then call the appropriate import method
    //        string statusMessage = "";
    //        EntityServices mgr = new EntityServices();
    //        string ctdlType = "";
    //        try
    //        {
    //            string payload = RegistryServices.GetResourceByUrl( resourceUrl, ref ctdlType, ref statusMessage );

    //            if ( !string.IsNullOrWhiteSpace( payload ) )
    //            {
				//	return ImportV3( payload, status );
				//}
    //            else
    //                return false;
    //        }
    //        catch ( Exception ex )
    //        {
    //            LoggingHelper.LogError( ex, thisClassName + ".ImportByResourceUrl()" );
    //            status.AddError( ex.Message );
    //            if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
    //            {
    //                status.AddWarning( "The referenced registry document is using an old schema. Please republish it with the latest schema!" );
    //            }
    //            return false;
    //        }
    //    }
    //    public bool ImportByPayload( string payload, SaveStatus status )
    //    {
    //        //if ( ImportServiceHelpers.IsAGraphResource( payload ) )
    //        //{
				////if ( payload.IndexOf( "\"en\":" ) > 0 )
				//return ImportV3( payload, "status );

    //    }
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
        public bool ProcessEnvelope( ReadEnvelope item, SaveStatus status )
        {
            if ( item == null || string.IsNullOrWhiteSpace( item.EnvelopeIdentifier ) )
            {
                status.AddError( "A valid ReadEnvelope must be provided." );
                return false;
            }
            //
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
            string ctdlType = RegistryServices.GetResourceType( payload );
            //string envelopeUrl = RegistryServices.GetEnvelopeUrl( envelopeIdentifier );
			//Already done in  RegistryImport
			//LoggingHelper.WriteLogFile( UtilityManager.GetAppKeyValue( "logFileTraceLevel", 5 ), item.EnvelopeCetermsCtid + "_lopp", payload, "", false );

			//if ( ImportServiceHelpers.IsAGraphResource( payload ) )
   //         {     }
                //if ( payload.IndexOf( "\"en\":" ) > 0 )
                    return ImportV3( payload, status );
                //else
                //    return ImportV2( payload, envelopeIdentifier, status );
       
            //else
            //{
            //    LoggingHelper.DoTrace( 5, "		envelopeUrl: " + envelopeUrl );
            //    //LoggingHelper.WriteLogFile( 1, "lopp_" + item.EnvelopeIdentifier, payload, "", false );
            //    input = JsonConvert.DeserializeObject<InputEntity>( item.DecodedResource.ToString() );

            //    return Import( input, envelopeIdentifier, status );
            //}
        }

        public bool ImportV3( string payload, SaveStatus status )
        {
			LoggingHelper.DoTrace( 7, thisClassName + ".ImportV3 - entered." );
			DateTime started = DateTime.Now;
			var saveDuration = new TimeSpan();

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
            MappingHelperV3 helper = new MappingHelperV3(7);
            helper.entityBlankNodes = bnodes;
            helper.entityBlankNodes = bnodes;
            helper.CurrentEntityCTID = input.CTID;

            helper.CurrentEntityName = input.Name.ToString();
            string ctid = input.CTID;
            string referencedAtId = input.CtdlId;
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
					LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".ImportV3(). Record was NOT found using CTID: '{0}'", input.CTID ) );
				}
				else
				{
					LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".ImportV3(). Found record: '{0}' using CTID: '{1}'", input.Name, input.CTID ) );
				}
				helper.currentBaseObject = output;

				//start with language and may use with language maps
				helper.MapInLanguageToTextValueProfile( input.InLanguage, "LearningOpportunity.InLanguage.CTID: " + ctid );
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
					//could use to alter helper.DefaultLanguage
				}
				output.Name = helper.HandleLanguageMap( input.Name, output, "Name" );
				output.Description = helper.HandleLanguageMap( input.Description, output, "Description" );
				output.Keyword = helper.MapToTextValueProfile( input.Keyword, output, "Keyword" );

				output.CTID = input.CTID;
				output.CredentialRegistryId = status.EnvelopeId;
				//TBD handling of referencing third party publisher
				if ( !string.IsNullOrWhiteSpace( status.DocumentPublishedBy ) )
				{
					//output.PublishedByOrganizationCTID = status.DocumentPublishedBy;
					var porg = OrganizationManager.GetSummaryByCtid( status.DocumentPublishedBy );
					if ( porg != null && porg.Id > 0 )
					{
						//TODO - store this in a json blob??????????
						//output.PublishedByOrganizationId = porg.Id;
						//output.PublishedByOrganizationName = porg.Name;
						//this will result in being added to Entity.AgentRelationship
						output.PublishedBy = new List<Guid>() { porg.RowId };
					}
					else
					{
						//if publisher not imported yet, all publishee stuff will be orphaned
						var entityUid = Guid.NewGuid();
						var statusMsg = "";
						var resPos = referencedAtId.IndexOf( "/resources/" );
						var swp = referencedAtId.Substring( 0, ( resPos + "/resources/".Length ) ) + status.DocumentPublishedBy;
						int orgId = new OrganizationManager().AddPendingRecord( entityUid, status.DocumentPublishedBy, swp, ref statusMsg );

					}
				}
				else
				{
					//may need a check for existing published by to ensure not lost
					if ( output.Id > 0 )
					{
						if ( output.OrganizationRole != null && output.OrganizationRole.Any() )
						{
							var publishedByList = output.OrganizationRole.Where( s => s.RoleTypeId == 30 ).ToList();
							if ( publishedByList != null && publishedByList.Any() )
							{
								var pby = publishedByList[ 0 ].ActingAgentUid;
								output.PublishedBy = new List<Guid>() { publishedByList[ 0 ].ActingAgentUid };

							}
						}

					}
				}
				//BYs - do owned and offered first
				output.OwnedBy = helper.MapOrganizationReferenceGuids( "LearningOpp.OwnedBy", input.OwnedBy, ref status );
				output.OfferedBy = helper.MapOrganizationReferenceGuids( "LearningOpp.OfferedBy", input.OfferedBy, ref status );
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
				output.AccreditedBy = helper.MapOrganizationReferenceGuids( "LearningOpp.AccreditedBy", input.AccreditedBy, ref status );
				output.ApprovedBy = helper.MapOrganizationReferenceGuids( "LearningOpp.ApprovedBy", input.ApprovedBy, ref status );
				output.RecognizedBy = helper.MapOrganizationReferenceGuids( "LearningOpp.RecognizedBy", input.RecognizedBy, ref status );
				output.RegulatedBy = helper.MapOrganizationReferenceGuids( "LearningOpp.RegulatedBy", input.RegulatedBy, ref status );

				output.DateEffective = input.DateEffective;
				output.ExpirationDate = input.ExpirationDate;

				output.SubjectWebpage = input.SubjectWebpage;

				output.AvailabilityListing = helper.MapListToString( input.AvailabilityListing );
				output.AvailableOnlineAt = helper.MapListToString( input.AvailableOnlineAt );
				output.DeliveryType = helper.MapCAOListToEnumermation( input.DeliveryType );
				output.DeliveryTypeDescription = helper.HandleLanguageMap( input.DeliveryTypeDescription, output, "DeliveryTypeDescription" );
				//AudienceType
				output.AudienceType = helper.MapCAOListToEnumermation( input.AudienceType );
				//CAO
				output.AudienceLevelType = helper.MapCAOListToEnumermation( input.AudienceLevelType );
				output.VersionIdentifier = helper.MapIdentifierValueListToString( input.VersionIdentifier );
				output.VersionIdentifierList = helper.MapIdentifierValueList( input.VersionIdentifier );
				output.CodedNotation = input.CodedNotation;
				output.Identifier = helper.MapIdentifierValueList( input.Identifier );
				if ( output.Identifier != null && output.Identifier.Count() > 0 )
				{
					output.IdentifierJson = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
				}
				//handle QuantitativeValue
				//output.CreditValue = helper.HandleQuantitiveValue( input.CreditValue, "LearningOpportunity.CreditValue" );
				//output.QVCreditValueList = helper.HandleValueProfileListToQVList( input.CreditValue, "LearningOpportunity.CreditValue" );
				output.CreditValue = helper.HandleValueProfileList( input.CreditValue, "LearningOpportunity.CreditValue" );
				output.CreditValueJson = JsonConvert.SerializeObject( output.CreditValue, MappingHelperV3.GetJsonSettings() );
				//output.CreditValueJson = JsonConvert.SerializeObject( output.CreditValueList, MappingHelperV3.GetJsonSettings() );
				//if ( output.CreditValueList != null && output.CreditValueList.Any() )
				//	output.CreditValue = output.CreditValueList[ 0 ];
				//
				//note can still have CreditUnitTypeDescription by itself. What to do if both?
				output.CreditUnitTypeDescription = helper.HandleLanguageMap( input.CreditUnitTypeDescription, output, "LearningOpportunity.CreditUnitTypeDescription" );


				//occupations
				//output.Occupation = helper.MapCAOListToEnumermation( input.OccupationType );
				//actually used by import
				output.Occupations = helper.MapCAOListToCAOProfileList( input.OccupationType );
				//just append alternative items. Ensure empty lists are ignored
				//output.Occupations.AddRange( helper.AppendLanguageMapListToCAOProfileList( input.AlternativeOccupationType ) );

				//skip if no occupations
				if ( output.Occupations.Count() == 0
					&& UtilityManager.GetAppKeyValue( "skipCredImportIfNoOccupations", false ) )
				{
					//LoggingHelper.DoTrace( 2, string.Format( "		***Skipping lopp# {0}, {1} as it has no occupations and this is a special run.", output.Id, output.Name ) );
					//return true;
				}
				//Industries
				output.Industries = helper.MapCAOListToCAOProfileList( input.IndustryType );
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
				//
				output.AssessmentMethodType = helper.MapCAOListToEnumermation( input.AssessmentMethodType );

				output.AssessmentMethodDescription = helper.HandleLanguageMap( input.AssessmentMethodDescription, output, "AssessmentMethodDescription" );
				//
				output.LearningMethodDescription = helper.HandleLanguageMap( input.LearningMethodDescription, output, "LearningMethodDescription" );

				//output.VerificationMethodDescription = helper.HandleLanguageMap( input.VerificationMethodDescription, output, "VerificationMethodDescription" );
				//financial assitance
				output.FinancialAssistance = helper.FormatFinancialAssistance( input.FinancialAssistance, ref status );
				if ( output.FinancialAssistance != null && output.FinancialAssistance.Any() )
					output.FinancialAssistanceJson = JsonConvert.SerializeObject( output.FinancialAssistance, MappingHelperV3.GetJsonSettings() );
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

				//EstimatedDuration
				output.EstimatedDuration = helper.FormatDuration( input.EstimatedDuration, ref status );

				//conditions ======================================
				output.Requires = helper.FormatConditionProfile( input.Requires, ref status );
				output.Recommends = helper.FormatConditionProfile( input.Recommends, ref status );
				output.EntryCondition = helper.FormatConditionProfile( input.EntryCondition, ref status );
				output.Corequisite = helper.FormatConditionProfile( input.Corequisite, ref status );
				//
				//SameAs URI
				output.SameAs = helper.MapToTextValueProfile( input.SameAs );

				//TODO - develope entity for IdentitifierValue
				output.VersionIdentifier = helper.MapIdentifierValueListToString( input.VersionIdentifier );
				output.VersionIdentifierList = helper.MapIdentifierValueList( input.VersionIdentifier );
				//
				//21-04-13 mp - TargetLearningResource will be URLs not registry resources
				if ( input.TargetLearningResource != null && input.TargetLearningResource.Count > 0 )
				{
					output.TargetLearningResource = input.TargetLearningResource;
					//output.TargetLearningOpportunityIds = helper.MapEntityReferences( "Assessment.TargetLearningOpportunity", input.TargetLearningResource, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status );
				}
				//
				if ( input.TargetPathway != null && input.TargetPathway.Count > 0 )
					output.TargetPathwayIds = helper.MapEntityReferences( "LearningOpportunity.TargetPathway", input.TargetPathway, CodesManager.ENTITY_TYPE_PATHWAY, ref status );
				//teaches compentencies
				output.TeachesCompetencies = helper.MapCAOListToCAOProfileList( input.Teaches );
				if ( output.TeachesCompetencies.Count() == 0 && UtilityManager.GetAppKeyValue( "skipLoppImportIfNoCompetencies", false ) )
				{
					//skip
					LoggingHelper.DoTrace( 2, string.Format( "		***Skipping lopp# {0}, {1} as it has no competencies and this is a special run.", output.Id, output.Name ) );
					return true;
				}

				//common conditions
				output.ConditionManifestIds = helper.MapEntityReferences( input.CommonConditions, CodesManager.ENTITY_TYPE_CONDITION_MANIFEST, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status );
				//common costs
				output.CostManifestIds = helper.MapEntityReferences( input.CommonCosts, CodesManager.ENTITY_TYPE_COST_MANIFEST, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status );

				//ADDRESSES
				output.Addresses = helper.FormatAvailableAtAddresses( input.AvailableAt, ref status );

				//INs
				output.AccreditedIn = helper.MapToJurisdiction( input.AccreditedIn, ref status );
				output.ApprovedIn = helper.MapToJurisdiction( input.ApprovedIn, ref status );
				output.ApprovedIn = helper.MapToJurisdiction( input.ApprovedIn, ref status );
				output.RecognizedIn = helper.MapToJurisdiction( input.RecognizedIn, ref status );
				output.RegulatedIn = helper.MapToJurisdiction( input.RegulatedIn, ref status );


				//var hasPartIds = input.HasPart.Select( x => x.CtdlId ).ToList();
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
					ThisEntity entity = EntityServices.GetByCtid( ctid );
					if ( entity != null && entity.Id > 0 )
					{
						output.Id = entity.Id;
						output.RowId = entity.RowId;
					}
				}
				importSuccessfull = mgr.Import( output, ref status );
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
				int ierId = new ImportManager().Import_EntityResolutionAdd( referencedAtId,
							ctid, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE,
							output.RowId,
							output.Id,
							( output.Id > 0 ),
							ref messages,
							output.Id > 0 );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( thisClassName + ".ImportV3 . Exception encountered for CTID: {0}", ctid ), false, "LearningOpportunity Import exception" );
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
    }
}
