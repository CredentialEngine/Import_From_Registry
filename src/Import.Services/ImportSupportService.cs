using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using workIT.Factories;
using workIT.Models;
using workIT.Services;
using workIT.Utilities;
using APIResourceServices = workIT.Services.API.SupportServiceServices;
using BNode = RA.Models.JsonV2.BlankNode;
using InputResource = RA.Models.JsonV2.SupportService;
using ResourceServices = workIT.Services.SupportServiceServices;
using ThisResource = workIT.Models.Common.SupportService;

namespace Import.Services
{
	public class ImportSupportService
    {
        int EntityTypeId = CodesManager.ENTITY_TYPE_SUPPORT_SERVICE;
        string thisClassName = "ImportSupportService";
        string ResourceType = "SupportService";
        ImportManager importManager = new ImportManager();
        ImportServiceHelpers importHelper = new ImportServiceHelpers();
        ThisResource output = new ThisResource();

        #region custom imports


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
            ResourceServices mgr = new ResourceServices();
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
            ResourceServices mgr = new ResourceServices();
            //**process
            bool importSuccessfull = ProcessEnvelope( item, status );
            List<string> messages = new List<string>();
            string importError = string.Join( "\r\n", status.GetAllMessages().ToArray() );
            //store envelope
            int newImportId = importHelper.Add( item, EntityTypeId, status.Ctid, importSuccessfull, importError, ref messages );
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

            return Import( item.EnvelopeCtdlType, payload, status );
        }

        /// <summary>
        /// Import a SupportService
        /// </summary>
        /// <param name="resourceClass"></param>
        /// <param name="payload"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool Import( string resourceClass, string payload, SaveStatus status )
        {
            LoggingHelper.DoTrace( 7, thisClassName + ".Import - entered." );
            DateTime started = DateTime.Now;
            var saveDuration = new TimeSpan();
           

            resourceClass = resourceClass.Replace( "ceterms:", "" );


            InputResource input = new InputResource();
            var bnodes = new List<BNode>();
            var mainEntity = new Dictionary<string, object>();

            Dictionary<string, object> dictionary = RegistryServices.JsonToDictionary( payload );
            object graph = dictionary["@graph"];
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
                    input = JsonConvert.DeserializeObject<InputResource>( main );
                }
                else
                {
                    //unlikely?
                    var bn = item.ToString();
                    bnodes.Add( JsonConvert.DeserializeObject<BNode>( bn ) );
                }

            }

            List<string> messages = new List<string>();
            bool importSuccessfull = false;
            ResourceServices mgr = new ResourceServices();
            MappingHelperV3 helper = new MappingHelperV3( 7 );

            helper.entityBlankNodes = bnodes;
            helper.CurrentEntityCTID = input.CTID;

            helper.CurrentEntityName = input.Name.ToString();
            string ctid = input.CTID;
            status.ResourceURL = input.CtdlId;
            LoggingHelper.DoTrace( 5, "		name: " + input.Name.ToString() );
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

                    output.EntityTypeId = EntityTypeId;
                }
                else
                {
                    LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".ImportV3(). Found record: '{0}' using CTID: '{1}'", input.Name, input.CTID ) );
                }
                helper.currentBaseObject = output;
                //start with language and may use with language maps
                output.InLanguage = helper.MapInLanguageToTextValueProfile( input.InLanguage, $"{ResourceType}.InLanguage. CTID: " + ctid );
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
                
                output.CredentialRegistryId = status.EnvelopeId;

                helper.MapOrganizationPublishedBy( output, ref status );
                
                //BYs 
                output.OfferedByList = helper.MapOrganizationReferenceGuids( $"{ResourceType}.OfferedBy", input.OfferedBy, ref status );
                //note need to set output.OwningAgentUid to the first entry
                output.OwnedByList = helper.MapOrganizationReferenceGuids( $"{ResourceType}.OwnedBy", input.OwnedBy, ref status );
                if ( output.OwnedByList != null && output.OwnedByList.Count > 0 )
                {
                    output.PrimaryAgentUID = output.OwnedByList[0];
                    helper.CurrentOwningAgentUid = output.OwnedByList[0];
                }
                else
                {
                    //add warning?
                    if ( output.OfferedByList == null || output.OfferedByList.Count == 0 )
                    {
                        status.AddWarning( $"{ResourceType} {output.CTID} doesn't have an owning or offering organization." );
                    }
                    else
                    {
                        output.PrimaryAgentUID = output.OfferedByList[0];
                        helper.CurrentOwningAgentUid = output.OfferedByList[0];
                    }
                }
                //the primary org, so can be stored in activity 
                if ( BaseFactory.IsGuidValid( output.PrimaryAgentUID ) )
                {
                    var primaryOrg = OrganizationServices.GetBasic( output.PrimaryAgentUID );
                    if ( primaryOrg != null )
                    {
                        output.PrimaryOrganization = primaryOrg;
                    }
                }
                output.SubjectWebpage = input.SubjectWebpage;

                output.AlternateName = helper.HandleLanguageMapList( input.AlternateName, output, "AlternateName" );

                output.AvailabilityListing = input.AvailabilityListing;
                //ADDRESSES
                output.AvailableAt = helper.FormatAvailableAtAddresses( input.AvailableAt, ref status );

                output.AvailableOnlineAt = input.AvailableOnlineAt;

                output.AccommodationType = helper.MapCAOListToEnumermation( input.AccommodationType );
                output.DeliveryType = helper.MapCAOListToEnumermation( input.DeliveryType );
                output.DateEffective = input.DateEffective;
                output.ExpirationDate = input.ExpirationDate;
                output.Keyword = helper.HandleLanguageMapList( input.Keyword, output, "Keyword" );


                if ( input.HasSpecificService != null && input.HasSpecificService.Count > 0 )
                    output.HasSpecificServiceIds = helper.MapEntityReferences( $"{ResourceType}.HasSpecificService", input.HasSpecificService, CodesManager.ENTITY_TYPE_SUPPORT_SERVICE, ref status );

                if ( input.IsSpecificServiceOf != null && input.IsSpecificServiceOf.Count > 0 )
                    output.IsSpecificServiceOfIds = helper.MapEntityReferences( $"{ResourceType}.IsPartOfSupportService", input.IsSpecificServiceOf, CodesManager.ENTITY_TYPE_SUPPORT_SERVICE, ref status );

                output.LifeCycleStatusType = helper.MapCAOToEnumermation( input.LifeCycleStatusType );
                output.OfferedIn = helper.MapToJurisdiction( input.OfferedIn, ref status );

                output.SupportServiceCondition = helper.FormatConditionProfile( input.SupportServiceCondition, ref status );

                output.SupportServiceType = helper.MapCAOListToEnumermation( input.SupportServiceType );

                //

                //***EstimatedCost
                //will need to format, all populate Entity.RelatedCosts (for bubble up) - actually this would be for asmts, and lopps
                output.EstimatedCost = helper.FormatCosts( input.EstimatedCost, ref status );

                //common conditions
                output.ConditionManifestIds = helper.MapEntityReferences( input.CommonConditions, CodesManager.ENTITY_TYPE_CONDITION_MANIFEST, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, ref status );

                //common costs
                output.CostManifestIds = helper.MapEntityReferences( input.CommonCosts, CodesManager.ENTITY_TYPE_COST_MANIFEST, CodesManager.ENTITY_TYPE_SCHEDULED_OFFERING, ref status );
                //FinancialAssistance ============================
                //output.FinancialAssistanceOLD = helper.FormatFinancialAssistance( input.FinancialAssistance, ref status );
                output.FinancialAssistance = helper.FormatFinancialAssistance( input.FinancialAssistance, ref status );
                //
                var identifierImport = helper.MapIdentifierValueListInternal( input.Identifier );
                if ( identifierImport != null && identifierImport.Count() > 0 )
                {
                    output.IdentifierJSON = JsonConvert.SerializeObject( identifierImport, MappingHelperV3.GetJsonSettings() );
                }
                output.OccupationType = helper.MapCAOListToCAOProfileList( input.OccupationType );

                //
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
                    var entity = ResourceServices.GetMinimumByCtid( ctid );
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
                }
                //
                saveDuration = DateTime.Now.Subtract( saveStarted );
                if ( saveDuration.TotalSeconds > 5 )
                    LoggingHelper.DoTrace( 6, string.Format( "         WARNING SAVE Duration: {0:N2} seconds ", saveDuration.TotalSeconds ) );
                //
                status.DocumentId = output.Id;
                status.DetailPageUrl = string.Format( "~/SupportService/{0}", output.Id );
                status.DocumentRowId = output.RowId;

                //just in case
                if ( status.HasErrors )
                    importSuccessfull = false;

                //if record was added to db, add to/or set EntityResolution as resolved
                int ierId = new ImportManager().Import_EntityResolutionAdd( status.ResourceURL,
                            ctid, CodesManager.ENTITY_TYPE_SCHEDULED_OFFERING,
                            output.RowId,
                            output.Id,
                            ( output.Id > 0 ),
                            ref messages,
                            output.Id > 0 );
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, string.Format( thisClassName + ".ImportV3 . Exception encountered for CTID: {0}", ctid ) );
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
            entity = ResourceServices.GetMinimumByCtid( ctid );
            if ( entity != null && entity.Id > 0 )
                return true;

            return exists;
        }
    }
}
