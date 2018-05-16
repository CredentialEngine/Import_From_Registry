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
using ThisEntity = workIT.Models.Common.Credential;
using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;

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

		public void ImportPendingRecords()
		{
			//string registryFilter = string.Format( UtilityManager.GetAppKeyValue( "credentialRegistryResource", "http://lr-staging.learningtapestry.com/resources/{0}" ), "ce-%" );
			//string where = string.Format( " [SubjectWebpage] like '{0}' ", registryFilter );
			//int pTotalRows = 0;

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
				LoggingHelper.LogError( ex, thisClassName + string.Format(".ImportByCtid(). CTID: {0}", ctid) );
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
					input = JsonConvert.DeserializeObject<InputEntity>( payload.ToString() );
					return Import( mgr, input, "", status );
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
		//public Dictionary<string, object> JsonToDictionary( string json )
		//{
		//	var result = new Dictionary<string, object>();
		//	var obj = JObject.Parse( json );
		//	foreach ( var property in obj )
		//	{
		//		result.Add( property.Key, JsonToObject( property.Value ) );
		//	}
		//	return result;
		//}
		//public object JsonToObject( JToken token )
		//{
		//	switch ( token.Type )
		//	{
		//		case JTokenType.Object:
		//			{
		//				return token.Children<JProperty>().ToDictionary( property => property.Name, property => JsonToObject( property.Value ) );
		//			}
		//		case JTokenType.Array:
		//			{
		//				var result = new List<object>();
		//				foreach ( var obj in token )
		//				{
		//					result.Add( JsonToObject( obj ) );
		//				}
		//				return result;
		//			}
		//		default:
		//			{
		//				return ( ( JValue ) token ).Value;
		//			}
		//	}
		//}
		public bool ImportByPayload( string payload, SaveStatus status )
		{
			EntityServices mgr = new EntityServices();
			input = JsonConvert.DeserializeObject<InputEntity>( payload );

			return Import( mgr, input, "", status );
		}
		public bool ProcessEnvelope( ReadEnvelope item, SaveStatus status )
		{
			EntityServices mgr = new EntityServices();
			bool importSuccessfull = ProcessEnvelope( mgr, item, status );
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
			LoggingHelper.WriteLogFile( 1, item.EnvelopeIdentifier + "_cred", payload, "", false );
			input = JsonConvert.DeserializeObject<InputEntity>( item.DecodedResource.ToString() );

			DateTime createDate = new DateTime();
			DateTime envelopeUpdateDate = new DateTime();
			if ( DateTime.TryParse( item.NodeHeaders.CreatedAt.Replace( "UTC", "" ).Trim(), out createDate ) )
			{
				//entity.DocumentUpdatedAt = updateDate;
			}
			if ( DateTime.TryParse( item.NodeHeaders.UpdatedAt.Replace( "UTC", "" ).Trim(), out envelopeUpdateDate ) )
			{
				//entity.DocumentUpdatedAt = envelopeUpdateDate;
			}
			return Import( mgr, input, envelopeIdentifier,status, envelopeUpdateDate );
		}

		public bool Import( EntityServices mgr, InputEntity input, string envelopeIdentifier, SaveStatus status )
		{
			DateTime envelopeUpdateDate = System.DateTime.Now;
			return Import( mgr, input, envelopeIdentifier, status, envelopeUpdateDate );
		}
		
		public bool Import( EntityServices mgr, InputEntity input, string envelopeIdentifier, SaveStatus status, DateTime envelopeUpdateDate )
		{
	
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

                if (!DoesEntityExist( input, ref output ))
                {
                    //set the rowid now, so that can be referenced as needed
                    output.RowId = Guid.NewGuid();
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

                //output.CodedNotation = MappingHelper.MapListToString( input.CodedNotation );
                output.CodedNotation = input.CodedNotation;

                //TODO - change persistance to a list
                //output.InLanguageCodeList = MappingHelper.MapToTextValueProfile( input.InLanguage );
                output.InLanguage = MappingHelper.MapListToString( input.InLanguage );
                //output.InLanguage = input.InLanguage;

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
                output.FinancialAssistance = MappingHelper.FormatFinancialAssistance( input.FinancialAssistance, ref status );


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
