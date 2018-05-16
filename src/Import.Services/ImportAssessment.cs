using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using workIT.Utilities;

using EntityServices = workIT.Services.AssessmentServices;
using InputEntity = RA.Models.Json.AssessmentProfile;
using ThisEntity = workIT.Models.ProfileModels.AssessmentProfile;
using workIT.Factories;
using workIT.Models;

namespace Import.Services
{
	public class ImportAssessment
	{
		int entityTypeId = CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE;
		string thisClassName = "ImportAssessment";
		ImportManager importManager = new ImportManager();
		InputEntity input = new InputEntity();
		ThisEntity output = new ThisEntity();

		#region custom imports
		public void ImportPendingRecords()
		{
			string registryFilter = string.Format( UtilityManager.GetAppKeyValue( "credentialRegistryResource", "http://lr-staging.learningtapestry.com/resources/{0}" ), "ce-%" );
            //string where = string.Format( " [SubjectWebpage] like '{0}' ", registryFilter );
            string where = " [EntityStateId] = 1 ";
            //
            int pTotalRows = 0;

			SaveStatus status = new SaveStatus();
			List<ThisEntity> list = AssessmentManager.Search( where, "", 1, 500, ref pTotalRows );
			LoggingHelper.DoTrace( 1, string.Format( thisClassName + " - ImportPendingRecords(). Processing {0} records =================", pTotalRows ) );
			foreach ( ThisEntity item in list )
			{
				status = new SaveStatus();
                //SWP contains the resource url
                if (!ImportByResourceUrl(item.SubjectWebpage, status))
                {
                    //check for 404
                    LoggingHelper.DoTrace(1, string.Format("     - (). Failed to import pending record: {0}, message(s): {1}", item.Id, status.GetErrorsAsString()));
                }
                else
                    LoggingHelper.DoTrace(1, string.Format("     - (). Successfully imported pending record: {0}", item.Id));
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
			//this is currently specific, assumes envelop contains a credential
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
			return ProcessEnvelope( mgr, item, status );
		}
		public bool ProcessEnvelope( EntityServices mgr, ReadEnvelope item, SaveStatus status )
		{
			if ( item == null || string.IsNullOrWhiteSpace( item.EnvelopeIdentifier ) )
			{
				status.AddError( "A valid ReadEnvelope must be provided." );
				return false;
			}

			string payload = item.DecodedResource.ToString();
			string envelopeIdentifier = item.EnvelopeIdentifier;
			string ctdlType = RegistryServices.GetResourceType( payload );
			string envelopeUrl = RegistryServices.GetEnvelopeUrl( envelopeIdentifier );
			LoggingHelper.DoTrace( 5, "		envelopeUrl: " + envelopeUrl );
			LoggingHelper.WriteLogFile( 1, item.EnvelopeIdentifier + "_asmt", payload, "", false );
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
				output.RowId = Guid.NewGuid();
			}

			//re:messages - currently passed to mapping but no errors are trapped??
			//				- should use SaveStatus and skip import if errors encountered (vs warnings)

			output.Name = input.Name;
			output.Description = input.Description;
			output.CTID = input.Ctid;
			output.CredentialRegistryId = envelopeIdentifier;
			output.DateEffective = input.DateEffective;
			output.SubjectWebpage = input.SubjectWebpage;
			output.Subject = MappingHelper.MapCAOListToTextValueProfile( input.Subject, CodesManager.PROPERTY_CATEGORY_SUBJECT );
			output.Keyword = MappingHelper.MapToTextValueProfile( input.Keyword );
			output.AvailabilityListing = MappingHelper.MapListToString( input.AvailabilityListing);
			output.AvailableOnlineAt = MappingHelper.MapListToString( input.AvailableOnlineAt );
			output.AssessmentExample = input.AssessmentExample;
			output.ExternalResearch = MappingHelper.MapListToString( input.ExternalResearch );
			
			output.AssessmentExampleDescription = input.AssessmentExampleDescription;
			output.AssessmentMethodType = MappingHelper.MapCAOListToEnumermation( input.AssessmentMethodType );

			output.VersionIdentifier = MappingHelper.MapIdentifierValueListToString( input.VersionIdentifier );
			output.VersionIdentifierList = MappingHelper.MapIdentifierValueList( input.VersionIdentifier );

			//To be looked
			output.CodedNotation = input.CodedNotation;
			output.AssessmentOutput = input.AssessmentOutput;
			output.AssessmentUseType = MappingHelper.MapCAOListToEnumermation( input.AssessmentUseType );
			output.DeliveryType = MappingHelper.MapCAOListToEnumermation( input.DeliveryType );
			output.DeliveryTypeDescription = input.DeliveryTypeDescription;

			output.IsProctored = input.IsProctored;
			output.HasGroupEvaluation = input.HasGroupEvaluation;
			output.HasGroupParticipation = input.HasGroupParticipation;

			//TODO - change persistance to a list
			//output.InLanguageCodeList = MappingHelper.MapToTextValueProfile( input.InLanguage );
			output.InLanguage = MappingHelper.MapListToString( input.InLanguage );
			//output.InLanguage = input.InLanguage;

			output.ProcessStandards = input.ProcessStandards;
			output.ProcessStandardsDescription = input.ProcessStandardsDescription;
			output.ScoringMethodDescription = input.ScoringMethodDescription;
			output.ScoringMethodExample = input.ScoringMethodExample;
			output.ScoringMethodExampleDescription = input.ScoringMethodExampleDescription;
			output.ScoringMethodType = MappingHelper.MapCAOListToEnumermation( input.ScoringMethodType );

			//TBD - a custom version
			//output.InstructionalProgramType = MappingHelper.MapCAOListToEnumermation( input.InstructionalProgramType );
			output.InstructionalProgramTypes = MappingHelper.MapCAOListToFramework( input.InstructionalProgramType );

			output.CreditHourType = input.CreditHourType;
			output.CreditHourValue = input.CreditHourValue;
			output.CreditUnitType = MappingHelper.MapCAOToEnumermation( input.CreditUnitType );
			output.CreditUnitValue = input.CreditUnitValue;
			output.CreditUnitTypeDescription = input.CreditUnitTypeDescription;
			output.Jurisdiction = MappingHelper.MapToJurisdiction( input.Jurisdiction, ref status );

			//EstimatedCost
			//will need to format, all populate Entity.RelatedCosts (for bubble up) - actually this would be for asmts, and lopps
			output.EstimatedCost = MappingHelper.FormatCosts( input.EstimatedCost, ref status );

			//common conditions
			output.ConditionManifestIds = MappingHelper.MapEntityReferences( input.CommonConditions, CodesManager.ENTITY_TYPE_CONDITION_MANIFEST, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, ref status );
			//common costs
			output.CostManifestIds = MappingHelper.MapEntityReferences( input.CommonCosts, CodesManager.ENTITY_TYPE_COST_MANIFEST, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, ref status );

            //connections
            output.AdvancedStandingFrom = MappingHelper.FormatConditionProfile( input.AdvancedStandingFrom, ref status );
            output.AdvancedStandingFor = MappingHelper.FormatConditionProfile( input.IsAdvancedStandingFor, ref status );

            output.PreparationFrom = MappingHelper.FormatConditionProfile( input.PreparationFrom, ref status );
            output.IsPreparationFor = MappingHelper.FormatConditionProfile( input.IsPreparationFor, ref status );

            output.IsRequiredFor = MappingHelper.FormatConditionProfile( input.IsRequiredFor, ref status );
            output.IsRecommendedFor = MappingHelper.FormatConditionProfile( input.IsRecommendedFor, ref status );

            //EstimatedDuration ==============================
            output.EstimatedDuration = MappingHelper.FormatDuration( input.EstimatedDuration, ref status );

			//conditions ======================================
			output.Requires = MappingHelper.FormatConditionProfile( input.Requires, ref status );
			output.Recommends = MappingHelper.FormatConditionProfile( input.Recommends, ref status );
			output.EntryCondition = MappingHelper.FormatConditionProfile( input.EntryCondition, ref status );
			output.Corequisite = MappingHelper.FormatConditionProfile( input.Corequisite, ref status );

			//Process profiles ==============================
			output.AdministrationProcess = MappingHelper.FormatProcessProfile( input.AdministrationProcess, ref status );
			output.DevelopmentProcess = MappingHelper.FormatProcessProfile( input.DevelopmentProcess, ref status );
			output.MaintenanceProcess = MappingHelper.FormatProcessProfile( input.MaintenanceProcess, ref status );

			//

			output.Addresses = MappingHelper.FormatAvailableAtAddresses( input.AvailableAt, ref status );

			//BYs
			output.AccreditedBy = MappingHelper.MapOrganizationReferenceGuids( input.AccreditedBy, ref status );
			output.ApprovedBy = MappingHelper.MapOrganizationReferenceGuids( input.ApprovedBy, ref status );
			output.OfferedBy = MappingHelper.MapOrganizationReferenceGuids( input.OfferedBy, ref status );
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
			
			//INs
			output.AccreditedIn = MappingHelper.MapToJurisdiction( input.AccreditedIn, ref status );
			output.ApprovedIn = MappingHelper.MapToJurisdiction( input.ApprovedIn, ref status );
			output.OfferedIn = MappingHelper.MapToJurisdiction( input.OfferedIn, ref status );
			output.RecognizedIn = MappingHelper.MapToJurisdiction( input.RecognizedIn, ref status );
			output.RegulatedIn = MappingHelper.MapToJurisdiction( input.RegulatedIn, ref status );
		
			//FinancialAssistance ============================
			output.FinancialAssistance = MappingHelper.FormatFinancialAssistance( input.FinancialAssistance, ref status );

			//assesses compentencies
			output.AssessesCompetencies = MappingHelper.MapCAOListToCompetencies( input.Assesses );

			
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
			status.DetailPageUrl = string.Format( "~/assessment/{0}", output.Id );
			status.DocumentRowId = output.RowId;

			//just in case
			if ( status.HasErrors )
				importSuccessfull = false;

			//if record was added to db, add to/or set EntityResolution as resolved
			int ierId = new ImportManager().Import_EntityResolutionAdd( referencedAtId,
						ctid,
						CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE,
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
