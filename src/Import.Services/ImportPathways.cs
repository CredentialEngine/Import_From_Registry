using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;

using BNode = RA.Models.JsonV2.BlankNode;
using ResourceServices = workIT.Services.PathwayServices;
using InputComponent = RA.Models.JsonV2.PathwayComponent;
using InputResource = RA.Models.JsonV2.Pathway;
using JInput = RA.Models.JsonV2;
using OutputComponent = workIT.Models.Common.PathwayComponent;
using ThisResource = workIT.Models.Common.Pathway;
using PB=workIT.Models.PathwayBuilder;
using workIT.Services;
using FAPI = workIT.Services.API;
//using System.Data.Common;

namespace Import.Services
{
    public class ImportPathways
    {
        int entityTypeId = CodesManager.ENTITY_TYPE_PATHWAY;
        public static string thisClassName = "ImportPathways";
        string resourceType = "Pathway";
        ImportManager importManager = new ImportManager();
        ThisResource output = new ThisResource();
        ImportServiceHelpers importHelper = new ImportServiceHelpers();
        string credentialFinderMainSite = UtilityManager.GetAppKeyValue( "credentialFinderMainSite" );
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
            //ResourceServices mgr = new ResourceServices();
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
        public bool CustomProcessEnvelope( ReadEnvelope item, SaveStatus status )
        {
            //handle
            bool importSuccessfull = ProcessEnvelope( item, status );
            List<string> messages = new List<string>();
            string importError = string.Join( "\r\n", status.GetAllMessages().ToArray() );
            //store envelope
            int newImportId = importHelper.Add( item, CodesManager.ENTITY_TYPE_PATHWAY, status.Ctid, importSuccessfull, importError, ref messages );
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
                status.AddError( thisClassName + " A valid ReadEnvelope must be provided." );
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
            string payload = item.DecodedResource.ToString();
            string envelopeIdentifier = item.EnvelopeIdentifier;
            string ctdlType = RegistryServices.GetResourceType( payload );
            string envelopeUrl = RegistryServices.GetEnvelopeUrl( envelopeIdentifier );
            LoggingHelper.DoTrace( 5, "		envelopeUrl: " + envelopeUrl );
            //Already done in  RegistryImport
            //LoggingHelper.WriteLogFile( UtilityManager.GetAppKeyValue( "logFileTraceLevel", 5 ), item.EnvelopeCetermsCtid + "_Pathway", payload, "", false );

            //just store input for now
            return Import( payload, status );

            //return true;
        } //

        public bool Import( string payload, SaveStatus status )
        {
            LoggingHelper.DoTrace( 6, "ImportPathways - entered." );
            List<string> messages = new List<string>();
            bool importSuccessfull = false;
            ResourceServices mgr = new ResourceServices();
            //
            InputResource input = new InputResource();
            var inputComponents = new List<InputComponent>();
            var mainEntity = new Dictionary<string, object>();

            #region consider use of JObject
            var graph2 = JObject.Parse( payload );
            var pathwayNode = ( JObject ) graph2["@graph"].FirstOrDefault( m => m["@type"].ToString() == "ceterms:Pathway" );
            var pathwayComponentNodes = graph2["@graph"].Where( m => m["ceasn:isPartOf"] != null ).Select( m => ( JObject ) m ).ToList();
            var progressionModelURIs = pathwayNode["asn:hasProgressionModel"] == null ? new List<string>() : new List<string>() { pathwayNode["asn:hasProgressionModel"].ToString() };


            #endregion
            //
            Dictionary<string, object> dictionary = RegistryServices.JsonToDictionary( payload );
            object graph = dictionary["@graph"];
            //serialize the graph object
            var glist = JsonConvert.SerializeObject( graph );

            //parse graph in to list of objects
            JArray graphList = JArray.Parse( glist );
            var bnodes = new List<BNode>();
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
                    //may have blank nodes?
                    var bn = item.ToString();
                    bnodes.Add( JsonConvert.DeserializeObject<BNode>( bn ) );
                    var child = item.ToString();
                    if ( child.IndexOf( "_:" ) > -1 )
                    {
                        bnodes.Add( JsonConvert.DeserializeObject<BNode>( child ) );

                    }
                    else if ( child.IndexOf( "Component" ) > -1 )
                    {
                        inputComponents.Add( JsonConvert.DeserializeObject<InputComponent>( child ) );
                    }
                    else
                    {
                        //unexpected
                        Dictionary<string, object> unexpected = RegistryServices.JsonToDictionary( child );
                        object unexpectedType = unexpected["@type"];
                        status.AddError( "Unexpected document type" );
                    }
                }
            }

            MappingHelperV3 helper = new MappingHelperV3( CodesManager.ENTITY_TYPE_PATHWAY );
            helper.entityBlankNodes = bnodes;
            helper.CurrentEntityCTID = input.CTID;
            helper.CurrentEntityName = input.Name.ToString();

            //status.EnvelopeId = envelopeIdentifier;
            try
            {
                string ctid = input.CTID;
                status.ResourceURL = input.CtdlId;

                LoggingHelper.DoTrace( 5, "		name: " + input.Name.ToString() );
                LoggingHelper.DoTrace( 6, "		url: " + input.SubjectWebpage );
                LoggingHelper.DoTrace( 5, "		ctid: " + input.CTID );
                LoggingHelper.DoTrace( 6, "		@Id: " + input.CtdlId );
                status.Ctid = ctid;

                if ( status.DoingDownloadOnly )
                    return true;


                if ( !DoesEntityExist( input.CTID, ref output ) )
                {
                    //TODO - perhaps create a pending pathway immediately 

                    //set the rowid now, so that can be referenced as needed
                    output.RowId = Guid.NewGuid();
                    LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".ImportV3(). Record was NOT found using CTID: '{0}'", input.CTID ) );
                }
                else
                {
                    LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".ImportV3(). Found record: '{0}' using CTID: '{1}'", input.Name, input.CTID ) );
                }
                helper.currentBaseObject = output;
                //TODO - may want to get from publisher if available to get the row and column info. 


                //
                output.Name = helper.HandleLanguageMap( input.Name, output, "Name" );
                output.Description = helper.HandleLanguageMap( input.Description, output, "Description" );
                output.SubjectWebpage = input.SubjectWebpage;
                output.CTID = input.CTID;
                //TBD handling of referencing third party publisher
                helper.MapOrganizationPublishedBy( output, ref status );

                //warning this gets set to blank if doing a manual import by ctid
                //output.CredentialRegistryId = envelopeIdentifier;

                //BYs - do owned and offered first
                output.OfferedBy = helper.MapOrganizationReferenceGuids( "Pathway.OfferedBy", input.OfferedBy, ref status );
                //note need to set output.OwningAgentUid to the first entry
                output.OwnedBy = helper.MapOrganizationReferenceGuids( "Pathway.OwnedBy", input.OwnedBy, ref status );
                if ( output.OwnedBy != null && output.OwnedBy.Count > 0 )
                {
                    output.PrimaryAgentUID = output.OwnedBy[0];
                    helper.CurrentOwningAgentUid = output.OwnedBy[0];
                }
                else
                {
                    //add warning?
                    if ( output.OfferedBy == null && output.OfferedBy.Count == 0 )
                    {
                        status.AddWarning( "document doesn't have an owning or offering organization." );
                    }
                    else
                    {
                        output.PrimaryAgentUID = output.OfferedBy[0];
                        helper.CurrentOwningAgentUid = output.OfferedBy[0];
                    }
                }
                //hasPart could contain all components. The API should have done a validation
                //not clear if necessary to do anything here
                //this would be a first step to create pending records?
                if ( input.HasPart != null && input.HasPart.Count() > 0 )
                {
                    //2023 - not doing anything with this yet
                    //all components are added to HasPart later anyway
                    output.HasPartList = helper.MapEntityReferenceGuids( "Pathway.HasPart", input.HasPart, CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT, ref status, output.CTID );
                }
                //rare
                output.HasChildList = helper.MapEntityReferenceGuids( "Pathway.HasChild", input.HasChild, CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT, ref status, output.CTID );

                output.HasDestinationList = helper.MapEntityReferenceGuids( "Pathway.HasDestination", input.HasDestinationComponent, CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT, ref status, output.CTID );

                //has progression model
                //TODO - IMPORT CONCEPT SCHEMES
                // will need to check if related scheme has been imported. 
                output.ProgressionModelURI = ResolutionServices.ExtractCtid( input.HasProgressionModel );

                if ( input.HasSupportService != null && input.HasSupportService.Count > 0 )
                    output.HasSupportServiceIds = helper.MapEntityReferences( $"{resourceType}.HasSupportService", input.HasSupportService, CodesManager.ENTITY_TYPE_SUPPORT_SERVICE, ref status );

                output.AlternateNames = helper.MapToTextValueProfile( input.AlternateName, output, "AlternateName" );

                output.Keyword = helper.MapToTextValueProfile( input.Keyword, output, "Keyword" );
                output.Subject = helper.MapCAOListToTextValueProfile( input.Subject, CodesManager.PROPERTY_CATEGORY_SUBJECT );
                //Industries/occupations
                output.IndustryTypes = helper.MapCAOListToCAOProfileList( input.IndustryType );
                output.OccupationTypes = helper.MapCAOListToCAOProfileList( input.OccupationType );
                output.InstructionalProgramTypes = helper.MapCAOListToCAOProfileList( input.InstructionalProgramType );

                //may need to save the pathway and then handle components
                //or do a create pending for hasDestination and any hasChild (actually already done by MapEntityReferenceGuids)

                //TODO - arbitrarily, or based on some attributes, make a call to publisher to get the pathway in order to assist with layout
                //		


                //=== if any messages were encountered treat as warnings for now
                if ( messages.Count > 0 )
                    status.SetMessages( messages, true );
                //components now or after save ?
                foreach ( var item in inputComponents )
                {
                    var c = ImportComponent( item, output, bnodes, status );
                    output.HasPart.Add( c );
                }
                //
                //check if in publisher
                ThisResource externalPathway = new ThisResource();
                if ( GetPathwayLayoutDataFromPublisher( output.CTID, output ) )
                {
                    //mark the pathway one way or the other to control use or not using with pathway display
                    if ( output.Properies == null)
                        output.Properies= new PathwayJSONProperties();

                    output.AllowUseOfPathwayDisplay = output.Properies.AllowUseOfPathwayDisplay = true;
                }


                //adding common import pattern
                importSuccessfull = mgr.Import( output, ref status );
                //start storing the finder api ready version
                var resource = FAPI.PathwayServices.GetDetailForAPI( output.Id, true );
                var resourceDetail = JsonConvert.SerializeObject( resource, JsonHelper.GetJsonSettings( false ) );

                var statusMsg = "";
                if ( new EntityManager().EntityCacheUpdateResourceDetail( output.CTID, resourceDetail, ref statusMsg ) == 0 )
                {
                    status.AddError( statusMsg );
                }
                //
                status.DocumentId = output.Id;
                status.DetailPageUrl = string.Format( "~/pathway/{0}", output.Id );
                status.DocumentRowId = output.RowId;
                //if record was added to db, add to/or set EntityResolution as resolved
                int ierId = new ImportManager().Import_EntityResolutionAdd( status.ResourceURL,
                        ctid,
                        CodesManager.ENTITY_TYPE_PATHWAY,
                        output.RowId,
                        output.Id,
                        ( output.Id > 0 ),
                        ref messages,
                        output.Id > 0 );
                //just in case - not sure if applicable, as will want to do components if the pathway exists
                if ( status.HasErrors )
                {
                    importSuccessfull = false;
                    //email an error report, and/or add to activity log?
                }
                //
                //if ( output.Id > 0 )
                //{
                //	foreach ( var item in inputComponents )
                //	{
                //		var c=ImportComponent( item, output, bnodes, status );
                //		output.HasPart.Add( c );
                //	}//
                //	//call method to handle components
                //}
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, string.Format( "Exception encountered in CTID: {0}", input.CTID ), false, "Pathway Import exception" );
            }

            return importSuccessfull;
        }

        //
        /// <summary>
        /// Handle component import
        /// TODO - should a save be done for each component or wait until the end
        /// </summary>
        /// <param name="input"></param>
        /// <param name="pathway"></param>
        /// <param name="bnodes"></param>
        /// <param name="status">TODO - do we want to continue using the pathway SaveStatus?</param>
        /// <returns></returns>
        public OutputComponent ImportComponent( InputComponent input, ThisResource pathway, List<BNode> bnodes, SaveStatus status )
        {
            MappingHelperV3 helper = new MappingHelperV3( CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT );
            //do we need to reference blank nodes here? - if so pass to this method
            helper.entityBlankNodes = bnodes;
            helper.CurrentEntityCTID = input.CTID;
            helper.CurrentEntityName = input.Name.ToString();
            OutputComponent output = new OutputComponent();
            //
            LoggingHelper.DoTrace( 5, "======== Component ======== " );
            LoggingHelper.DoTrace( 5, "		type: " + input.PathwayComponentType.ToString() );
            LoggingHelper.DoTrace( 5, "		name: " + ( input.Name ?? new JInput.LanguageMap( "componentNameMissing" ) ).ToString() );
            LoggingHelper.DoTrace( 5, "		ctid: " + input.CTID );
            LoggingHelper.DoTrace( 6, "		@Id: " + input.CtdlId );

            try
            {
                //add/updating Pathway
                if ( !DoesComponentExist( input.CTID, ref output ) )
                {
                    //set the rowid now, so that can be referenced as needed
                    //no, the guid comes from the resolving of entity references
                    //actually OK, as earlier references would result in a pending record
                    output.RowId = Guid.NewGuid();
                }
                helper.currentBaseObject = output;
                if ( input.CTID == "ce-fa6c139f-0615-401f-9920-6ec8c445baca" )
                {

                }
                //initialize json properties
                output.JsonProperties = new PathwayComponentProperties();
                //
                output.PathwayComponentType = input.PathwayComponentType;
                output.Name = helper.HandleLanguageMap( input.Name, output, "Name" );
                output.Description = helper.HandleLanguageMap( input.Description, output, "Description" );
                output.SubjectWebpage = input.SubjectWebpage;
                //Industries/occupations
                output.IndustryTypes = helper.MapCAOListToCAOProfileList( input.IndustryType );
                output.OccupationTypes = helper.MapCAOListToCAOProfileList( input.OccupationType );


                output.ProxyFor = input.ProxyFor;
                //mostly replaced by ProxyFor
                output.SourceData = input.SourceData;
                if ( string.IsNullOrWhiteSpace( output.ProxyFor ) && !string.IsNullOrWhiteSpace( output.SourceData ) )
                    output.ProxyFor = output.SourceData;

                //we are assuming ProxyFor is single, can it be a list?
                if ( !string.IsNullOrWhiteSpace( output.ProxyFor ) && output.ProxyFor.IndexOf( "/resources/" ) > 0 )
                {
                    //TODO - make more generic - use entity_cache and resourceSummary
                    var ctid = ResolutionServices.ExtractCtid( output.ProxyFor );
                    if ( !string.IsNullOrWhiteSpace( ctid ) )
                    {
                        //store the ctid in ProxyFor
                        output.ProxyFor = ctid;
                        //could just use one call to entity cache?
                        //NO. need to be able to set up relationship to the related detail page.
                        //could store resourceSummary in the json?
                        var entity = EntityManager.EntityCacheGetByCTID( ctid );
                        if (entity != null && entity.Id > 0)
                        {
                            //this approach 'buries' the cred from external references like credential in pathway
                            //21-07-22 mparsons - OK, the database manager creates the Entity.Credential relationships as needed
                            output.ProxyForResource = new TopLevelEntityReference()
                            {
                                Id = entity.BaseId,
                                Name = entity.Name,
                                Description = entity.Description,
                                CTID = entity.CTID,
                                SubjectWebpage = entity.SubjectWebpage,
                                 EntityType = entity.EntityType,
                                 EntityTypeId = entity.EntityTypeId,
                                 EntityStateId = entity.EntityStateId,
                                 
                                 DetailURL= credentialFinderMainSite + "resources/" + entity.CTID
                            };
                            output.JsonProperties.ProxyForResource = output.ProxyForResource;
                        }

                        if ( output.PathwayComponentType.ToLower().IndexOf( "credentialcomp" ) > -1 )
                        {
                            var target = CredentialManager.GetMinimumByCtid( ctid );
                            if ( target != null && target.Id > 0 )
                            {
                                //this approach 'buries' the cred from external references like credential in pathway
                                //21-07-22 mparsons - OK, the database manager creates the Entity.Credential relationships as needed
                                output.SourceCredential = new TopLevelEntityReference()
                                {
                                    Id = target.Id,
                                    Name = target.Name,
                                    Description = target.Description,
                                    CTID = target.CTID,
                                    SubjectWebpage = target.SubjectWebpage,
                                    DetailURL = credentialFinderMainSite + "resources/" + entity.CTID
                                    //RowId = target.RowId
                                };
                                output.JsonProperties.SourceCredential = output.SourceCredential;
                            }
                        }
                        else if ( output.PathwayComponentType.ToLower().IndexOf( "assessmentcomp" ) > -1 )
                        {
                            var target = AssessmentManager.GetSummaryByCtid( ctid );
                            if ( target != null && target.Id > 0 )
                            {
                                //may not really need this, just the json
                                //OK, the database manager creates the Entity.Assessment relationships as needed
                                output.SourceAssessment = new TopLevelEntityReference()
                                {
                                    Id = target.Id,
                                    Name = target.Name,
                                    Description = target.Description,
                                    CTID = target.CTID,
                                    SubjectWebpage = target.SubjectWebpage,
                                    DetailURL = credentialFinderMainSite + "resources/" + entity.CTID
                                    //RowId = target.RowId
                                };
                                output.JsonProperties.SourceAssessment = output.SourceAssessment;
                            }
                        }
                        else if ( output.PathwayComponentType.ToLower().IndexOf( "coursecomp" ) > -1 )
                        {
                            var target = LearningOpportunityManager.GetByCtid( ctid );
                            if ( target != null && target.Id > 0 )
                            {
                                //21-07-22 mparsons - OK, the database manager creates the Entity.LearningOpp relationships as needed
                                output.SourceLearningOpportunity = new TopLevelEntityReference()
                                {
                                    Id = target.Id,
                                    Name = target.Name,
                                    Description = target.Description,
                                    CTID = target.CTID,
                                    SubjectWebpage = target.SubjectWebpage,
                                    DetailURL = credentialFinderMainSite + "resources/" + entity.CTID
                                    //RowId = target.RowId
                                };
                                output.JsonProperties.SourceLearningOpportunity = output.SourceLearningOpportunity;
                            }
                        }
                        else if ( output.PathwayComponentType.ToLower().IndexOf( "competencycomponent" ) > -1 )
                        {
                            //TODO
                            var target = CompetencyFrameworkCompetencyManager.GetByCtid( ctid );
                            if ( target != null && target.Id > 0 )
                            {
                                //21-07-22 mparsons - add handling for a competency
                                output.SourceCompetency = new TopLevelEntityReference()
                                {
                                    Id = target.Id,
                                    Name = target.CompetencyText,
                                    Description = "",
                                    CTID = target.CTID,
                                    SubjectWebpage = "",
                                    //RowId = target.RowId
                                };
                                output.JsonProperties.SourceCompetency = output.SourceCompetency;
                            }
                        }
                    }


                }

                output.CTID = input.CTID;
                output.PathwayCTID = pathway.CTID;


                //output.CodedNotation = input.CodedNotation;
                output.Identifier = helper.MapIdentifierValueListInternal( input.Identifier );
                if ( output.Identifier != null && output.Identifier.Count() > 0 )
                {
                    output.IdentifierJson = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
                }
                //
                output.ComponentDesignationList = helper.MapCAOListToList( input.ComponentDesignation );

                //
                output.CredentialType = input.CredentialType;
                output.CreditValue = helper.HandleValueProfileList( input.CreditValue, output.PathwayComponentType + ".CreditValue" );

                //TBD - how to handle. Will need to have imported the concept scheme/concept
                if ( input.HasProgressionLevel != null && input.HasProgressionLevel.Any() )
                {
                    int cntr = 0;
                    foreach ( var item in input.HasProgressionLevel )
                    {
                        cntr++;
                        //storing list here. A delimited list is saved to the db.
                        output.HasProgressionLevels.Add( ResolutionServices.ExtractCtid( item ) );
                        if ( cntr == 1 )
                        {
                            output.HasProgressionLevel = output.HasProgressionLevels[0];
                        }
                    }
                }

                output.PointValue = helper.HandleQuantitiveValue( input.PointValue, output.PathwayComponentType + ".PointValue" );

                //
                output.ProgramTerm = helper.HandleLanguageMap( input.ProgramTerm, output, "ProgramTerm" );
                //need to get relationshiptype to store-> this can be done by manager
                //TODO - need to ensure existing hasChild relationships are removed if not present in import
                //3
                output.HasChildList = helper.MapEntityReferenceGuids( "PathwayComponent.HasChild", input.HasChild, CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT, ref status, output.PathwayCTID );
                //2
                output.HasIsChildOfList = helper.MapEntityReferenceGuids( "PathwayComponent.IsChildOf", input.IsChildOf, CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT, ref status, output.PathwayCTID );


                //Prerequisite is obsolete
                //output.HasPrerequisiteList = helper.MapEntityReferenceGuids( "PathwayComponent.Prerequisite", input.Prerequisite, CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT, ref status, output.PathwayCTID );
                output.HasPrecededByList = helper.MapEntityReferenceGuids( "PathwayComponent.PrecededBy", input.PrecededBy, CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT, ref status, output.PathwayCTID );
                output.HasPrecedesList = helper.MapEntityReferenceGuids( "PathwayComponent.Precedes", input.Precedes, CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT, ref status, output.PathwayCTID );

                //populate JSON properties
                output.JsonProperties.ComponentDesignationList = output.ComponentDesignationList;
                output.JsonProperties.CreditValue = output.CreditValue;
                output.JsonProperties.Identifier = output.Identifier;
                output.JsonProperties.PointValue = output.PointValue;

                //
                if ( input.HasCondition != null && input.HasCondition.Count() > 0 )
                {
                    output.HasCondition = new List<ComponentCondition>();
                    foreach ( var item in input.HasCondition )
                    {
                        ComponentCondition componentCondition = new ComponentCondition();
                        if ( ImportComponentCondition( item, pathway.CTID, helper, status, ref componentCondition ) )
                        { 
                            if ( componentCondition.ConditionProperties == null )
                                componentCondition.ConditionProperties = new workIT.Models.Common.ConditionProperties();

                            componentCondition.ConditionProperties.HasProgressionLevel = output.HasProgressionLevel;
                            output.HasCondition.Add( componentCondition );
                        }

                    }
                }
            } catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, "ImportPathways.ImportComponent" );
                //status.AddError( string.Format( "ImportPathways.ImportComponent. ComponentType: {0}, Name: {1}, Message: {2}", output.ComponentTypeId, output.Name, ex.Message ) );
            }
            //then save
            return output;
        }

        // 
        public bool ImportComponentCondition( JInput.ComponentCondition input, string pathwayCTID, MappingHelperV3 helper, SaveStatus status, ref ComponentCondition output )
        {
            var isValid = true;
            //output = new PathwayComponentCondition();
            output.Name = helper.HandleLanguageMap( input.Name, output, "ComponentCondition.Name" );
            output.Description = helper.HandleLanguageMap( input.Description, output, "ComponentCondition.Description" );
            output.RequiredNumber = input.RequiredNumber;
            output.PathwayCTID = pathwayCTID;
            output.HasTargetComponentList = helper.MapEntityReferenceGuids( "ComponentCondition.TargetComponent", input.TargetComponent, CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT, ref status, pathwayCTID );

            //new 
            output.LogicalOperator = input.LogicalOperator;
            if ( input.HasConstraint != null && input.HasConstraint.Count() > 0 )
            {
                output.HasConstraint = new List<Constraint>();
                foreach ( var item in input.HasConstraint )
                {
                    var constraint = new Constraint()
                    {
                        RowId = Guid.NewGuid(),
                        Name = helper.HandleLanguageMap( input.Name, output, "Constraint.Name" ),
                        Description = helper.HandleLanguageMap( input.Description, output, "Constraint.Description" ),
                        LeftAction = item.LeftAction,
                        LeftSource = item.LeftSource,
                        Comparator = item.Comparator,
                        RightAction = item.RightAction,
                        RightSource = item.RightSource,
                    };
                    output.HasConstraint.Add( constraint );
                }
            }
            if ( input.HasCondition != null && input.HasCondition.Count() > 0 )
            {
                output.HasCondition = new List<ComponentCondition>();
                foreach ( var item in input.HasCondition )
                {
                    ComponentCondition componentCondition = new ComponentCondition();
                    if ( ImportComponentCondition( item, pathwayCTID, helper, status, ref componentCondition ) )
                    {
                        output.HasCondition.Add( componentCondition );
                    }
                }
            }
            return isValid;
        }

        //currently 
        public bool DoesEntityExist( string ctid, ref ThisResource entity )
        {
            bool exists = false;
            entity = ResourceServices.GetByCtid( ctid );
            if ( entity != null && entity.Id > 0 )
                return true;

            return exists;
        }
        public bool DoesComponentExist( string ctid, ref OutputComponent entity )
        {
            bool exists = false;
            entity = ResourceServices.GetComponentByCtid( ctid );
            if ( entity != null && entity.Id > 0 )
                return true;

            return exists;
        }
        public bool GetPathwayLayoutDataFromPublisher( string ctid, ThisResource importedPathway )
        {
            var pbURL = UtilityManager.GetAppKeyValue( "pbuilderAPILoadPathwayURL" ) + ctid; //pbuilderAPILoadPathwayURL
            if ( string.IsNullOrWhiteSpace( pbURL ) )
            {
				//just for testing
                pbURL = "https://sandbox.credentialengine.org/publisher/PathwayBuilderApi/Load/Pathway/" + ctid;
                //pbURL = "https://apps.credentialengine.org/publisher/PathwayBuilderApi/Load/Pathway/" + ctid;
            }
            var hasPathwayBuilderLayout = false;
            List<string> messages = new List<string>();
            //try wrapper 
            bool usingWrapper = true;
            //pbURL = "https://localhost:44330/PathwayBuilderApi/Load/Pathway/" + ctid;
            //resourceUrl = "https://localhost:44330/PathwayBuilderApi/Load/Pathway/ce-c5632451-cdbd-4f6b-bca1-3668dece6aa5?userCreds=finderImport";
            //what to do about a login? 
            //use of apiKey? or userCreds - but more secure?
            //however we don't want the pathwayWrapper, just the pathway, may want a different method. Also may need a PathwayWrapper for the later display;
            string statusMessage = "";
            var pathwayJson = GetPublisherResource( pbURL, ref statusMessage );
            if ( string.IsNullOrEmpty( pathwayJson ) || !string.IsNullOrWhiteSpace( statusMessage ))
                return false;
            //

            //now what to deserialize to ? Need to compare the two pathway classes
            try
            {
                //deserialize to 
                var publisherPathway = JsonConvert.DeserializeObject<PathwayApiResponse>( pathwayJson );

                //if ( usingWrapper )
                //{
                var pathwayWrapper = JsonConvert.DeserializeObject<PathwayWrapperImport>( publisherPathway.Data.ToString() );
                if ( pathwayWrapper != null )
                {
                    foreach ( var wrapperPC in pathwayWrapper.PathwayComponents )
                    {
                        var importedPC = importedPathway.HasPart.FirstOrDefault( x => x.CTID == wrapperPC.CTID );
                        if ( importedPC != null )
                        {
                            importedPC.JsonProperties.ColumnNumber = importedPC.ColumnNumber = wrapperPC.ColumnNumber;
                            importedPC.JsonProperties.RowNumber = importedPC.RowNumber = wrapperPC.RowNumber;
                            //todo handle conditions.may have to handle here
                            if (importedPC.HasCondition != null && importedPC.HasCondition.Any())
                            {
                                importedPC.HasCondition= MapConditions( pathwayWrapper, wrapperPC.RowId, importedPC.HasCondition, ref statusMessage );
                            }

                            //worst case could be one component per level and all at 0,0 - unlikely
                            if (importedPC.ColumnNumber > 0 || importedPC.RowNumber> 0 )
                            {
                                hasPathwayBuilderLayout = true;
                            }
                        }
                    }
                }


            } catch ( Exception ex )
            {

                return false;
            }
            if ( hasPathwayBuilderLayout )
                return true;
            else 
                return false;
        }


        /*
         * 
         * 
         * 
         */ 
        public static List<ComponentCondition> MapConditions( PathwayWrapperImport pathwayWrapper, string conditionParentUID, List<ComponentCondition> input, ref string statusMessage )
        {
            List <ComponentCondition> output = new List <ComponentCondition>(); 

            if (input == null || input.Count == 0) 
                return output;  

            foreach ( var importedCondition in input )
            {
                //get the condition from the wrapper. 
                var wrapperCondition = pathwayWrapper.ComponentConditions.FirstOrDefault( c => c.ParentIdentifier == conditionParentUID && c.Name.Trim() == importedCondition.Name?.Trim() );
                if ( wrapperCondition != null && !string.IsNullOrWhiteSpace( wrapperCondition.ParentIdentifier ) )
                {
                    if ( importedCondition.ConditionProperties == null )
                        importedCondition.ConditionProperties = new workIT.Models.Common.ConditionProperties();

                    importedCondition.ConditionProperties.ColumnNumber = importedCondition.ColumnNumber = wrapperCondition.ColumnNumber;
                    importedCondition.ConditionProperties.RowNumber = importedCondition.RowNumber = wrapperCondition.RowNumber;

                    //a Guid is used rather than a CTID where there is no progression model
                    //if ( ServiceHelper.IsValidCtid( condition.HasProgressionLevel, ref messages )) 
                    //{
                    importedCondition.ConditionProperties.HasProgressionLevel = importedCondition.HasProgressionLevel = wrapperCondition.HasProgressionLevel;
                    //}
                } else
				{
					//should not happen report, and continue
					LoggingHelper.LogError( $"{thisClassName}.MapConditions. Pathway: '{pathwayWrapper.Pathway.Name}' ({pathwayWrapper.Pathway.CTID}) Wrapper condition was not found! conditionParentUID:{conditionParentUID}" );
					continue;
				}
                //check sub conditions
                if ( importedCondition.HasCondition != null && importedCondition.HasCondition.Any() )
                {
                    importedCondition.HasCondition= MapConditions( pathwayWrapper, wrapperCondition.RowId, importedCondition.HasCondition, ref statusMessage );
                    //foreach ( var item2 in importedCondition.HasCondition )
                    //{
                    //    var subCondition = pathwayWrapper.ComponentConditions.FirstOrDefault( c => c.ParentIdentifier == wrapperCondition.RowId );
                    //    if ( subCondition != null && !string.IsNullOrWhiteSpace( subCondition.ParentIdentifier ) )
                    //    {
                    //        if ( item2.ConditionProperties == null )
                    //            item2.ConditionProperties = new workIT.Models.Common.ConditionProperties();

                    //        item2.ConditionProperties.ColumnNumber = item2.ColumnNumber = subCondition.ColumnNumber;
                    //        item2.ConditionProperties.RowNumber = item2.RowNumber = subCondition.RowNumber;
                    //        //NOTE: that where a pathway has no progression model, arbitr
                    //        //if ( ServiceHelper.IsValidCtid( condition.HasProgressionLevel, ref messages ) )
                    //        //{
                    //        item2.ConditionProperties.HasProgressionLevel = item2.HasProgressionLevel = subCondition.HasProgressionLevel;
                    //        //}                                                    
                    //    }
                    //}

                   
                }
                output.Add( importedCondition );
            }
            return output;


        }
        public static string GetPublisherResource( string resourceUrl, ref string statusMessage )
        {
            string payload = "";
            statusMessage = "";
            string credentialEngineAPIKey = UtilityManager.GetAppKeyValue( "MyCredentialEngineAPIKey" );
            //temp workaround
            resourceUrl += "?userCreds=finderImport";

            try
            {
                using ( var client = new HttpClient() )
                {
                    client.DefaultRequestHeaders.
                        Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );
                    client.DefaultRequestHeaders.Add( "Authorization", "Bearer " + credentialEngineAPIKey );
                    var task = client.GetAsync( resourceUrl );
                    task.Wait();
                    var response1 = task.Result;
                    payload = task.Result.Content.ReadAsStringAsync().Result;

                    //just in case, likely the caller knows the context
                    if ( !string.IsNullOrWhiteSpace( payload ) && payload.IndexOf( "ERROR - Invalid request" ) == -1 && payload.Length > 100 )
                    {
                        //should be OK
                    }
                    else
                    {
                        //nothing found, or error/not found
                        if ( payload.IndexOf( "401 Unauthorized" ) > -1 || payload.IndexOf( "ERROR - Invalid request" ) > -1)
                        {
                            LoggingHelper.DoTrace( 1, "RegistryServices.GetResourceByUrl. Not authorized to view: " + resourceUrl );
                            statusMessage = "This organization is not authorized to view: " + resourceUrl + ". " + payload;
                        }
                        else
                        {
                            LoggingHelper.DoTrace( 1, "RegistryServices.GetResourceByUrl. Did not find: " + resourceUrl );
                            statusMessage = "The referenced resource was not found in the credential registry: " + resourceUrl;
                        }
                        payload = "";
                    }
                    //
                }
            }
            catch ( Exception exc )
            {
                if ( exc.Message.IndexOf( "(404) Not Found" ) > 0 )
                {
                    //need to surface these better
                    statusMessage = "The referenced resource was not found in the credential registry: " + resourceUrl;
                }
                else
                {
                    var msg = LoggingHelper.FormatExceptions( exc );
                    if ( msg.IndexOf( "remote name could not be resolved: 'sandbox.credentialengineregistry.org'" ) > 0 )
                    {
                        //retry?
                        statusMessage = "retry";
                    }
                    else if ( msg.IndexOf( "The underlying connection was closed" ) > 0 )
                    {
                        //retry?
                        statusMessage = "The underlying connection was closed: An unexpected error occurred on a send.";
                    }
                    else
                    {
                        LoggingHelper.LogError( exc, "RegistryServices.GetResourceByUrl: " + resourceUrl );
                        statusMessage = exc.Message;
                    }
                }
            }
            return payload;
        }

    }
    public class PathwayApiResponse
    {
        public PathwayApiResponse()
        {
            Messages = new List<string>();
        }
        public PathwayApiResponse( object result, bool successful = true, List<string> messages = null, object extra = null )
        {
            Data = result;
            Valid = successful;
            Messages = messages;
        }

        public object Data { get; set; }

        public bool Valid { get; set; }

        public List<string> Messages { get; set; }
        public object Extra { get; set; } = null;
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);

    public class PathwayWrapperImport
    {
        public PWPathway Pathway { get; set; } = new PWPathway();
        public List<PWPathwayComponent> PathwayComponents { get; set; } = new List<PWPathwayComponent>();
        public List<PWProgressionModel> ProgressionModels { get; set; } = new List<PWProgressionModel>();
        public List<PWProgressionLevel> ProgressionLevels { get; set; } = new List<PWProgressionLevel>();
        public List<PWComponentCondition> ComponentConditions { get; set; } = new List<PWComponentCondition>();
    }
    public class PWPathway
    {
        public int Id { get; set; }
        public string RowId { get; set; }
        public string CTID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
    public class PWPathwayComponent
    {
        public string Type { get; set; }
        public string RowId { get; set; }
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }
        public string CTID { get; set; }
        public string Name { get; set; }
        public string HasProgressionLevel { get; set; }
        public List<string> HasCondition { get; set; }
    }
    public class PWProgressionModel
    {
        public string CTID { get; set; }
        public string Name { get; set; }
    }
    public class PWProgressionLevel
    {
        public string CTID { get; set; }
        public string Name { get; set; }
    }
    public class PWComponentCondition
    {
        public string ParentIdentifier { get; set; }
        public string RowId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> TargetComponent { get; set; }
        public string HasProgressionLevel { get; set; }
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }
    }
    public class PathwayFromPublisher
    {
        public int Id { get; set; }
        public string RowId { get; set; }
        public string CTID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        //public Properties Properties { get; set; }
        public List<HasPart> HasPart { get; set; }
        public List<object> HasChild { get; set; }
        public List<HasDestinationComponent> HasDestinationComponent { get; set; }
        public HasProgressionModel HasProgressionModel { get; set; }
        public string HasProgressionModelURI { get; set; }
        public OwnerRoles OwnerRoles { get; set; }
        public List<object> OwnedByOrganization { get; set; }
        public List<object> OfferedByOrganization { get; set; }
        //public IndustryType IndustryType { get; set; }
        //public OccupationType OccupationType { get; set; }
        //public InstructionalProgramType InstructionalProgramType { get; set; }
        //public List<object> InstructionalProgramTypes { get; set; }
        //public List<Keyword> Keyword { get; set; }
        //public List<object> Subject { get; set; }
        //public List<object> Keywords { get; set; }

        public string SubjectWebpage { get; set; }
        //public string OwningAgentUid { get; set; }
        //public OwningOrganization OwningOrganization { get; set; }
        public string OrganizationName { get; set; }
        //public int OwningOrganizationId { get; set; }
        //public List<OrganizationRole> OrganizationRole { get; set; }
        public int EntityStateId { get; set; }
        //public string CredentialRegistryId { get; set; }
        public DateTime LastApproved { get; set; }
        public bool IsApproved { get; set; }
        public int LastApprovedById { get; set; }
        public DateTime LastPublished { get; set; }
        public int LastPublishedById { get; set; }
        public string LastPublishDate { get; set; }
        public bool IsPublished { get; set; }

        public string LastUpdatedDisplay { get; set; }
        public string LastUpdatedBy { get; set; }


        public DateTime Created { get; set; }
        public DateTime LastUpdated { get; set; }
    }
    public class ActingAgent
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string CTID { get; set; }
        public string SubjectWebpage { get; set; }

        public int Id { get; set; }
        public string RowId { get; set; }
        //public string OrganizationName { get; set; }

        //public LifeCycleStatusType LifeCycleStatusType { get; set; }
        //public string AgentType { get; set; }
        //public int AgentTypeId { get; set; }
        //public OrganizationSubclass OrganizationSubclass { get; set; }
        //public string Auto_OrgURI { get; set; }
        //public List<object> Addresses { get; set; }
        //public List<object> Auto_Address { get; set; }
        //public FirstAddress FirstAddress { get; set; }
        //public List<object> Auto_AvailabilityListing { get; set; }
        //public List<object> SocialMediaPages { get; set; }
        //public List<object> Auto_SocialMedia { get; set; }
        //public List<object> PhoneNumbers { get; set; }
        //public List<AutoTargetContactPointForDetail> Auto_TargetContactPointForDetail { get; set; }
        //public List<object> Emails { get; set; }
        //public List<object> Keyword { get; set; }
        //public List<object> AlternateName { get; set; }
        //public List<object> ContactPoint { get; set; }
        //public List<object> IdentificationCodes { get; set; }
        //public List<object> AlternativeIdentifiers { get; set; }
        //public List<object> ID_AlternativeIdentifier { get; set; }
        //public string FoundingDate { get; set; }
        //public string FoundingYear { get; set; }
        //public string FoundingMonth { get; set; }
        //public string FoundingDay { get; set; }
        //public string Founded { get; set; }
        //public OrganizationType OrganizationType { get; set; }
        //public OrganizationSectorType OrganizationSectorType { get; set; }
        //public AgentSectorType AgentSectorType { get; set; }
        //public ServiceType ServiceType { get; set; }
        //public List<object> HasConditionManifest { get; set; }
        //public List<object> HasCostManifest { get; set; }
        //public List<object> ParentOrganizations { get; set; }
        //public List<object> OrganizationRole_Dept { get; set; }
        //public List<object> OrganizationRole_Subsidiary { get; set; }
        //public List<object> OrganizationRole_QAPerformed { get; set; }
        //public List<object> OrganizationRole_Actor { get; set; }
        //public List<object> OrganizationThirdPartyAssertions { get; set; }
        //public List<object> OrganizationFirstPartyAssertions { get; set; }
        //public List<object> CredentialAssertions { get; set; }
        //public List<object> OrganizationAssertions { get; set; }
        //public List<object> AssessmentAssertions { get; set; }
        //public List<object> LoppAssertions { get; set; }
        //public List<object> OrganizationRole_Recipient { get; set; }
        //public List<object> OrganizationRole { get; set; }
        //public Identifiers Identifiers { get; set; }
        //public List<object> Identifier { get; set; }
        //public List<object> VerificationServiceProfiles { get; set; }
        //public List<object> HasVerificationService { get; set; }
        //public List<object> CreatedCredentials { get; set; }
        //public List<object> Owns_Auto_Organization_OwnsCredentials { get; set; }
        //public List<object> OfferedCredentials { get; set; }
        //public List<object> OwnedAssessments { get; set; }
        //public List<object> OwnedLearningOpportunities { get; set; }
        //public List<object> QACredentials { get; set; }
        //public List<object> JurisdictionAssertions { get; set; }
        //public List<object> AgentProcess { get; set; }
        //public Industry Industry { get; set; }
        //public IndustryType IndustryType { get; set; }
        //public List<object> AlternativeIndustries { get; set; }
        //public List<object> InLanguageIds { get; set; }
        //public List<object> ProcessProfiles { get; set; }
        //public List<object> AppealProcess { get; set; }
        //public List<object> ComplaintProcess { get; set; }
        //public List<object> ReviewProcess { get; set; }
        //public List<object> RevocationProcess { get; set; }
        //public List<object> AdministrationProcess { get; set; }
        //public List<object> DevelopmentProcess { get; set; }
        //public List<object> MaintenanceProcess { get; set; }
        //public List<object> VerificationStatus { get; set; }

        //public DateTime LastApproved { get; set; }
        //public bool IsApproved { get; set; }
        //public DateTime LastPublished { get; set; }
        //public int LastPublishedById { get; set; }
        //public string LastPublishDate { get; set; }
        //public bool IsPublished { get; set; }
        //public List<object> Jurisdiction { get; set; }
        //public bool IsEditorUpdate { get; set; }
        //public string StatusMessage { get; set; }
        //public string LastUpdatedDisplay { get; set; }
        //public string EntityType { get; set; }
        //public ExtraData ExtraData { get; set; }

        //public string RowIdString { get; set; }
    }

    public class AgentAndRoles
    {
        public bool HasAnIdentifer { get; set; }
        public List<object> Results { get; set; }
    }

    public class AgentRole
    {
        public string Name { get; set; }
        public string SchemaName { get; set; }
        public string Description { get; set; }
        public int ParentId { get; set; }
        public List<Item> Items { get; set; }
        public int Id { get; set; }
        //public string OtherValue { get; set; }
        //public List<ItemsAsAlignmentObject> ItemsAsAlignmentObjects { get; set; }
        //public List<ItemsAsAlignmentObjectsWithCode> ItemsAsAlignmentObjectsWithCodes { get; set; }
        //public List<string> ItemsAsString { get; set; }
    }

    public class AgentSectorType
    {
        public string Name { get; set; }
        public string SchemaName { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public List<Item> Items { get; set; }
        public int Id { get; set; }
        public string OtherValue { get; set; }
        public List<ItemsAsAlignmentObject> ItemsAsAlignmentObjects { get; set; }
        public List<ItemsAsAlignmentObjectsWithCode> ItemsAsAlignmentObjectsWithCodes { get; set; }
        public List<string> ItemsAsString { get; set; }
    }

    public class AllComponent
    {
        public string PathwayComponentType { get; set; }
        public string TypeLabel { get; set; }
        public int PathwayComponentTypeId { get; set; }
        public int ComponentRelationshipTypeId { get; set; }
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }
        public string PathwayCTID { get; set; }
        public List<object> AllComponents { get; set; }
        public List<object> HasChild { get; set; }
        public List<object> HasCondition { get; set; }
        public List<object> Identifier { get; set; }
        public List<object> IsChildOf { get; set; }
        public List<object> HasProgressionLevels { get; set; }
        public string HasProgressionLevelDisplay { get; set; }
        public List<object> ProgressionLevels { get; set; }
        public List<object> PrecededBy { get; set; }
        public List<object> Precedes { get; set; }
        public string ProxyFor { get; set; }
        public string ComponentCategory { get; set; }
        public string ProgramTerm { get; set; }
        public string CredentialType { get; set; }
        public IndustryType IndustryType { get; set; }
        public OccupationType OccupationType { get; set; }
        public JsonProperties JsonProperties { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CTID { get; set; }
        public string SubjectWebpage { get; set; }
        public string OrganizationName { get; set; }
        public List<object> OrganizationRole { get; set; }
        public string LastPublishDate { get; set; }
        //public List<object> Jurisdiction { get; set; }
        //public bool IsEditorUpdate { get; set; }
        //public string StatusMessage { get; set; }
        //public string LastUpdatedDisplay { get; set; }
        //public RelatedEntity RelatedEntity { get; set; }
        //public DateTime EntityLastUpdated { get; set; }
        //public ExtraData ExtraData { get; set; }
        public int Id { get; set; }
        public string RowId { get; set; }
        public string RowIdString { get; set; }
        public DateTime Created { get; set; }
        public int CreatedById { get; set; }
        public DateTime LastUpdated { get; set; }
        public int LastUpdatedById { get; set; }
    }

    //public class AssessmentConnectionsList
    //{
    //    public List<object> Results { get; set; }
    //}

    //public class AssessmentDeliveryType
    //{
    //    public List<object> Items { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<object> ItemsAsAlignmentObjects { get; set; }
    //    public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<object> ItemsAsString { get; set; }
    //}

    //public class AssessmentMethodType
    //{
    //    public List<object> Items { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<object> ItemsAsAlignmentObjects { get; set; }
    //    public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<object> ItemsAsString { get; set; }
    //}

    //public class AssessmentMethodTypes
    //{
    //    public bool HasAnIdentifer { get; set; }
    //    public List<object> Results { get; set; }
    //}

    //public class AssessmentUseType
    //{
    //    public List<object> Items { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<object> ItemsAsAlignmentObjects { get; set; }
    //    public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<object> ItemsAsString { get; set; }
    //}

    //public class AssessmentUseTypes
    //{
    //    public bool HasAnIdentifer { get; set; }
    //    public List<object> Results { get; set; }
    //}

    //public class AudienceLevelType
    //{
    //    public List<object> Items { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<object> ItemsAsAlignmentObjects { get; set; }
    //    public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<object> ItemsAsString { get; set; }
    //}

    //public class AudienceType
    //{
    //    public List<object> Items { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<object> ItemsAsAlignmentObjects { get; set; }
    //    public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<object> ItemsAsString { get; set; }
    //}

    //public class AutoTargetContactPointForDetail
    //{
    //    public string ProfileName { get; set; }
    //    public string Name { get; set; }
    //    public List<object> PhoneNumbers { get; set; }
    //    public List<object> Emails { get; set; }
    //    public List<object> SocialMediaPages { get; set; }
    //    public List<object> Auto_Telephone { get; set; }
    //    public List<object> Auto_FaxNumber { get; set; }
    //    public List<object> Auto_Email { get; set; }
    //    public List<object> Auto_SocialMedia { get; set; }
    //    public List<object> Auto_ContactOption { get; set; }
    //    public bool IsEditorUpdate { get; set; }
    //    public string StatusMessage { get; set; }
    //    public string LastUpdatedDisplay { get; set; }
    //    public ExtraData ExtraData { get; set; }
    //    public string RowIdString { get; set; }
    //}

    //public class Bounds
    //{
    //}

    public class ConditionProperties
    {
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }
        public string HasProgressionLevel { get; set; }
    }

    //public class CredentialStatusType
    //{
    //    public List<object> Items { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<object> ItemsAsAlignmentObjects { get; set; }
    //    public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<object> ItemsAsString { get; set; }
    //}

    //public class CredentialType
    //{
    //    public List<object> Items { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<object> ItemsAsAlignmentObjects { get; set; }
    //    public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<object> ItemsAsString { get; set; }
    //}

    public class CreditLevelTypeEnum
    {
        public List<object> Items { get; set; }
        public string OtherValue { get; set; }
        public List<object> ItemsAsAlignmentObjects { get; set; }
        public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
        public List<object> ItemsAsString { get; set; }
    }

    public class CreditUnitType
    {
        public List<object> Items { get; set; }
        public string OtherValue { get; set; }
        public List<object> ItemsAsAlignmentObjects { get; set; }
        public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
        public List<object> ItemsAsString { get; set; }
    }

    //public class CreditValue
    //{
    //    public CreditUnitType CreditUnitType { get; set; }
    //    public CreditLevelTypeEnum CreditLevelTypeEnum { get; set; }
    //    public List<object> CreditLevelType { get; set; }
    //    public List<object> CreditUnitTypes { get; set; }
    //    public List<object> Subject { get; set; }
    //}

    //public class DeliveryMethodTypes
    //{
    //    public bool HasAnIdentifer { get; set; }
    //    public List<object> Results { get; set; }
    //}

    //public class DeliveryType
    //{
    //    public List<object> Items { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<object> ItemsAsAlignmentObjects { get; set; }
    //    public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<object> ItemsAsString { get; set; }
    //}

    //public class ExtraData
    //{
    //}

    //public class FirstAddress
    //{
    //    public string StreetAddress { get; set; }
    //    public List<object> Identifier { get; set; }
    //    public GeoCoordinates GeoCoordinates { get; set; }
    //    public List<object> ContactPoint { get; set; }
    //    public List<object> Auto_TargetContactPoint { get; set; }
    //    public bool IsEditorUpdate { get; set; }
    //    public string StatusMessage { get; set; }
    //    public string LastUpdatedDisplay { get; set; }
    //    public ExtraData ExtraData { get; set; }
    //    public string RowIdString { get; set; }
    //}

    //public class GeoCoordinates
    //{
    //    public string Name { get; set; }
    //    public List<object> Auto_Address { get; set; }
    //    public string ToponymName { get; set; }
    //    public string Region { get; set; }
    //    public string Country { get; set; }
    //    public string TitleFormatted { get; set; }
    //    public string LocationFormatted { get; set; }
    //    public Bounds Bounds { get; set; }
    //    public bool IsEditorUpdate { get; set; }
    //    public string StatusMessage { get; set; }
    //    public string LastUpdatedDisplay { get; set; }
    //    public ExtraData ExtraData { get; set; }
    //    public string RowIdString { get; set; }
    //}

    public class HasComponentCondition
    {
        public int EntityId { get; set; }
        public string ParentIdentifier { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
        public int RequiredNumber { get; set; }
        public List<object> HasCondition { get; set; }
        public List<TargetComponent> TargetComponent { get; set; }
        public string LogicalOperator { get; set; }
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }
        public string HasProgressionLevel { get; set; }
        public List<object> HasConstraint { get; set; }
        public string PathwayCTID { get; set; }
        //public RelatedEntity RelatedEntity { get; set; }
        //public DateTime EntityLastUpdated { get; set; }
       // public string LastUpdatedDisplay { get; set; }
        public ConditionProperties ConditionProperties { get; set; }
        public int Id { get; set; }
        public string RowId { get; set; }

   
        public DateTime LastUpdated { get; set; }
        public int LastUpdatedById { get; set; }
    }

    public class HasDestinationComponent
    {
        public string PathwayComponentType { get; set; }
        public string TypeLabel { get; set; }
        public int PathwayComponentTypeId { get; set; }
        public int ComponentRelationshipTypeId { get; set; }
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }
        public string PathwayCTID { get; set; }
        public List<object> AllComponents { get; set; }
        public List<object> HasChild { get; set; }
        public List<HasComponentCondition> HasCondition { get; set; }
        public List<object> Identifier { get; set; }
        public List<object> IsChildOf { get; set; }
        public List<object> HasProgressionLevels { get; set; }
        public string HasProgressionLevelDisplay { get; set; }
        public List<object> ProgressionLevels { get; set; }
        public List<object> PrecededBy { get; set; }
        public List<object> Precedes { get; set; }
        public string ProxyFor { get; set; }
        public string ComponentCategory { get; set; }
        public string ProgramTerm { get; set; }
        public string CredentialType { get; set; }
        public IndustryType IndustryType { get; set; }
        public OccupationType OccupationType { get; set; }
        public JsonProperties JsonProperties { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CTID { get; set; }
        public string SubjectWebpage { get; set; }
        public string OrganizationName { get; set; }
        public List<object> OrganizationRole { get; set; }
        public string LastPublishDate { get; set; }
        //public List<object> Jurisdiction { get; set; }
        //public bool IsEditorUpdate { get; set; }
        //public string StatusMessage { get; set; }
        //public string LastUpdatedDisplay { get; set; }
        //public RelatedEntity RelatedEntity { get; set; }
        //public DateTime EntityLastUpdated { get; set; }
        //public ExtraData ExtraData { get; set; }
        public int Id { get; set; }
        public string RowId { get; set; }
        public string RowIdString { get; set; }
        public DateTime Created { get; set; }
        public int CreatedById { get; set; }
        public DateTime LastUpdated { get; set; }
        public int LastUpdatedById { get; set; }
    }

    public class HasPart
    {
        public string PathwayComponentType { get; set; }
        public string TypeLabel { get; set; }
        public int PathwayComponentTypeId { get; set; }
        public int ComponentRelationshipTypeId { get; set; }
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }
        public string PathwayCTID { get; set; }
        public List<AllComponent> AllComponents { get; set; }
        public List<object> HasChild { get; set; }
        public List<HasComponentCondition> HasCondition { get; set; }
        public List<object> Identifier { get; set; }
        public List<object> IsChildOf { get; set; }
        public List<object> HasProgressionLevels { get; set; }
        public string HasProgressionLevelDisplay { get; set; }
        public List<object> ProgressionLevels { get; set; }
        public List<object> PrecededBy { get; set; }
      //  public List<Precede> Precedes { get; set; }
        public string ProxyFor { get; set; }
        public string ComponentCategory { get; set; }
        public string ProgramTerm { get; set; }
        public string CredentialType { get; set; }
        public IndustryType IndustryType { get; set; }
        public OccupationType OccupationType { get; set; }
        public JsonProperties JsonProperties { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CTID { get; set; }
        public string SubjectWebpage { get; set; }
        //public string OrganizationName { get; set; }
        //public List<object> OrganizationRole { get; set; }
        //public string LastPublishDate { get; set; }
        //public List<object> Jurisdiction { get; set; }
        //public bool IsEditorUpdate { get; set; }
        //public string StatusMessage { get; set; }
        //public string LastUpdatedDisplay { get; set; }
        //public RelatedEntity RelatedEntity { get; set; }
        //public DateTime EntityLastUpdated { get; set; }
        //public ExtraData ExtraData { get; set; }
        public int Id { get; set; }
        public string RowId { get; set; }
        public string RowIdString { get; set; }
        public DateTime Created { get; set; }
        public int CreatedById { get; set; }
        public DateTime LastUpdated { get; set; }
        public int LastUpdatedById { get; set; }
    }

    public class HasProgressionModel
    {
        public List<object> HasProgressionLevel { get; set; }
        public List<object> HasConcepts { get; set; }
        public string PublicationStatusType { get; set; }
        public OwnerRoles OwnerRoles { get; set; }
        public List<object> HasTopConcept { get; set; }
        public List<object> HasPart { get; set; }
        public List<object> InLanguage { get; set; }
        public List<object> HasProgressionLevelsOld { get; set; }
        public List<object> HasProgressionLevels { get; set; }
        public List<object> Pathways { get; set; }
        public List<object> HasPathway { get; set; }
    
    }

    //public class Identifiers
    //{
    //    public List<object> Items { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<object> ItemsAsAlignmentObjects { get; set; }
    //    public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<object> ItemsAsString { get; set; }
    //}

    //public class Industry
    //{
    //    public List<object> Items { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<object> ItemsAsAlignmentObjects { get; set; }
    //    public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<object> ItemsAsString { get; set; }
    //}

    //public class IndustryOtherResults
    //{
    //    public bool HasAnIdentifer { get; set; }
    //    public List<object> Results { get; set; }
    //}

    //public class IndustryResults
    //{
    //    public bool HasAnIdentifer { get; set; }
    //    public List<object> Results { get; set; }
    //}

    public class IndustryType
    {
        public string Name { get; set; }
        public string SchemaName { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public List<object> Items { get; set; }
        public int Id { get; set; }
        public string OtherValue { get; set; }
        public List<object> ItemsAsAlignmentObjects { get; set; }
        public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
        public List<object> ItemsAsString { get; set; }
    }

    //public class InLanguageCodeList
    //{
    //    public int LanguageCodeId { get; set; }
    //    public string LanguageName { get; set; }
    //    public string LanguageCode { get; set; }
    //    public string ParentSummary { get; set; }
    //    public string ViewHeading { get; set; }
    //    public List<object> Jurisdiction { get; set; }
    //    public List<object> ReferenceUrl { get; set; }
    //    public bool IsEditorUpdate { get; set; }
    //    public string StatusMessage { get; set; }
    //    public string LastUpdatedDisplay { get; set; }
    //    public ExtraData ExtraData { get; set; }
    //    public string RowIdString { get; set; }
    //}

    //public class InstructionalProgramResults
    //{
    //    public bool HasAnIdentifer { get; set; }
    //    public List<object> Results { get; set; }
    //}

    //public class InstructionalProgramType
    //{
    //    public string Name { get; set; }
    //    public string SchemaName { get; set; }
    //    public string Description { get; set; }
    //    public List<object> Items { get; set; }
    //    public int Id { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<object> ItemsAsAlignmentObjects { get; set; }
    //    public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<object> ItemsAsString { get; set; }
    //}

    public class Item
    {
        public int Id { get; set; }
        public int CodeId { get; set; }
        public int RecordId { get; set; }
        public string Value { get; set; }
        public bool Selected { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SchemaName { get; set; }
        public string RowId { get; set; }
        public string ReverseTitle { get; set; }
        public string ReverseSchemaName { get; set; }
    }

    public class ItemsAsAlignmentObject
    {
        public string AlignmentType { get; set; }
        public string FrameworkName { get; set; }
        public string TargetNode { get; set; }
        public string Framework { get; set; }
        public string TargetNodeName { get; set; }
        public string TargetNodeDescription { get; set; }
    }

    public class ItemsAsAlignmentObjectsWithCode
    {
        public string AlignmentType { get; set; }
        public string CodedNotation { get; set; }
        public string FrameworkName { get; set; }
        public string TargetNode { get; set; }
        public string TargetNodeName { get; set; }
        public string TargetNodeDescription { get; set; }
    }

    public class JsonProperties
    {
        public List<object> ComponentDesignationList { get; set; }
    }

    //public class Keyword
    //{
    //    public int CategoryId { get; set; }
    //    public int EntityId { get; set; }
    //    public string TextValue { get; set; }
    //    public string ProfileSummary { get; set; }
    //    public string ParentSummary { get; set; }
    //    public string ViewHeading { get; set; }
    //    public List<object> Jurisdiction { get; set; }
    //    public List<object> ReferenceUrl { get; set; }
    //    public bool IsEditorUpdate { get; set; }
    //    public int ParentId { get; set; }
    //    public string StatusMessage { get; set; }
    //    public string LastUpdatedDisplay { get; set; }
    //    public ExtraData ExtraData { get; set; }
    //    public int Id { get; set; }
    //    public string RowIdString { get; set; }
    //    public DateTime Created { get; set; }
    //    public int CreatedById { get; set; }
    //}

    //public class LearningDeliveryType
    //{
    //    public List<object> Items { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<object> ItemsAsAlignmentObjects { get; set; }
    //    public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<object> ItemsAsString { get; set; }
    //}

    //public class LearningMethodType
    //{
    //    public List<object> Items { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<object> ItemsAsAlignmentObjects { get; set; }
    //    public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<object> ItemsAsString { get; set; }
    //}

    //public class LearningOppConnectionsList
    //{
    //    public List<object> Results { get; set; }
    //}

    //public class LearningType
    //{
    //    public List<object> Items { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<object> ItemsAsAlignmentObjects { get; set; }
    //    public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<object> ItemsAsString { get; set; }
    //}

    //public class LifeCycleStatusType
    //{
    //    public List<object> Items { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<object> ItemsAsAlignmentObjects { get; set; }
    //    public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<object> ItemsAsString { get; set; }
    //}

    //public class NavyRating
    //{
    //    public List<object> Items { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<object> ItemsAsAlignmentObjects { get; set; }
    //    public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<object> ItemsAsString { get; set; }
    //}

    //public class Occupation
    //{
    //    public List<object> Items { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<object> ItemsAsAlignmentObjects { get; set; }
    //    public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<object> ItemsAsString { get; set; }
    //}

    //public class OccupationOtherResults
    //{
    //    public bool HasAnIdentifer { get; set; }
    //    public List<object> Results { get; set; }
    //}

    //public class OccupationResults
    //{
    //    public bool HasAnIdentifer { get; set; }
    //    public List<object> Results { get; set; }
    //}

    public class OccupationType
    {
        public string Name { get; set; }
        public string SchemaName { get; set; }
        public string Description { get; set; }
        public List<object> Items { get; set; }
        public int Id { get; set; }
        public string OtherValue { get; set; }
        public List<object> ItemsAsAlignmentObjects { get; set; }
        public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
        public List<object> ItemsAsString { get; set; }
    }

    //public class OrganizationRole
    //{
    //    public int ParentTypeId { get; set; }
    //    public ActingAgent ActingAgent { get; set; }
    //    public int ActingAgentId { get; set; }
    //    public string ActingAgentUid { get; set; }
    //    public AgentRole AgentRole { get; set; }
    //    public TargetCredential TargetCredential { get; set; }
    //    public TargetOrganization TargetOrganization { get; set; }
    //    public TargetAssessment TargetAssessment { get; set; }
    //    public TargetLearningOpportunity TargetLearningOpportunity { get; set; }
    //    public string ProfileSummary { get; set; }
    //    public string ParentSummary { get; set; }
    //    public string ViewHeading { get; set; }
    //    public List<object> Jurisdiction { get; set; }
    //    public List<object> ReferenceUrl { get; set; }
    //    public bool IsEditorUpdate { get; set; }
    //    public int ParentId { get; set; }
    //    public string StatusMessage { get; set; }
    //    public string LastUpdatedDisplay { get; set; }
    //    public ExtraData ExtraData { get; set; }
    //    public string RowIdString { get; set; }
    //}

    //public class OrganizationSectorType
    //{
    //    public string Name { get; set; }
    //    public string SchemaName { get; set; }
    //    public string Description { get; set; }
    //    public string Url { get; set; }
    //    public List<Item> Items { get; set; }
    //    public int Id { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<ItemsAsAlignmentObject> ItemsAsAlignmentObjects { get; set; }
    //    public List<ItemsAsAlignmentObjectsWithCode> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<string> ItemsAsString { get; set; }
    //}

    //public class OrganizationSubclass
    //{
    //    public string Name { get; set; }
    //    public string SchemaName { get; set; }
    //    public string Description { get; set; }
    //    public string Url { get; set; }
    //    public List<Item> Items { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<ItemsAsAlignmentObject> ItemsAsAlignmentObjects { get; set; }
    //    public List<ItemsAsAlignmentObjectsWithCode> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<string> ItemsAsString { get; set; }
    //}

    //public class OrganizationType
    //{
    //    public string Name { get; set; }
    //    public string SchemaName { get; set; }
    //    public string Description { get; set; }
    //    public string Url { get; set; }
    //    public List<Item> Items { get; set; }
    //    public int Id { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<ItemsAsAlignmentObject> ItemsAsAlignmentObjects { get; set; }
    //    public List<ItemsAsAlignmentObjectsWithCode> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<string> ItemsAsString { get; set; }
    //}

    //public class OtherInstructionalProgramResults
    //{
    //    public bool HasAnIdentifer { get; set; }
    //    public List<object> Results { get; set; }
    //}

    public class OwnerRoles
    {
        public List<object> Items { get; set; }
        public string OtherValue { get; set; }
        public List<object> ItemsAsAlignmentObjects { get; set; }
        public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
        public List<object> ItemsAsString { get; set; }
        public string Name { get; set; }
        public string SchemaName { get; set; }
        public string Description { get; set; }
        public int ParentId { get; set; }
        public int Id { get; set; }
    }

    public class OwningOrganization
    {
        //public LifeCycleStatusType LifeCycleStatusType { get; set; }
        //public string AgentType { get; set; }
        //public int AgentTypeId { get; set; }
        //public OrganizationSubclass OrganizationSubclass { get; set; }
        //public int OrganizationSubclassTypeId { get; set; }

   
        public string Name { get; set; }
        public string Description { get; set; }
        public int EntityTypeId { get; set; }
        public string CTID { get; set; }
        public string SubjectWebpage { get; set; }

        public int EntityStateId { get; set; }
   
        public DateTime LastUpdated { get; set; }
   
    }

    //public class Precede
    //{
    //    public string PathwayComponentType { get; set; }
    //    public string TypeLabel { get; set; }
    //    public int PathwayComponentTypeId { get; set; }
    //    public int ComponentRelationshipTypeId { get; set; }
    //    public int RowNumber { get; set; }
    //    public int ColumnNumber { get; set; }
    //    public string PathwayCTID { get; set; }
    //    public List<object> AllComponents { get; set; }
    //    public List<object> HasChild { get; set; }
    //    public List<object> HasCondition { get; set; }
    //    public List<object> Identifier { get; set; }
    //    public List<object> IsChildOf { get; set; }
    //    public List<object> HasProgressionLevels { get; set; }
    //    public string HasProgressionLevelDisplay { get; set; }
    //    public List<object> ProgressionLevels { get; set; }
    //    public List<object> PrecededBy { get; set; }
    //    public List<object> Precedes { get; set; }
    //    public string ProxyFor { get; set; }
    //    public string ComponentCategory { get; set; }
    //    public string ProgramTerm { get; set; }
    //    public string CredentialType { get; set; }
    //    public IndustryType IndustryType { get; set; }
    //    public OccupationType OccupationType { get; set; }
    //    public JsonProperties JsonProperties { get; set; }
    //    public string Name { get; set; }
    //    public string Description { get; set; }
    //    public string CTID { get; set; }
    //    public string SubjectWebpage { get; set; }
    //    public string OrganizationName { get; set; }
    //    public List<object> OrganizationRole { get; set; }
    //    public string LastPublishDate { get; set; }
    //    public List<object> Jurisdiction { get; set; }
    //    public bool IsEditorUpdate { get; set; }
    //    public string StatusMessage { get; set; }
    //    public string LastUpdatedDisplay { get; set; }
    //    public RelatedEntity RelatedEntity { get; set; }
    //    public DateTime EntityLastUpdated { get; set; }
    //    public ExtraData ExtraData { get; set; }
    //    public int Id { get; set; }
    //    public string RowId { get; set; }
    //    public string RowIdString { get; set; }
    //    public DateTime Created { get; set; }
    //    public int CreatedById { get; set; }
    //    public DateTime LastUpdated { get; set; }
    //    public int LastUpdatedById { get; set; }
    //}

    //public class Properties
    //{
    //}

    //public class QualityAssurance
    //{
    //    public bool HasAnIdentifer { get; set; }
    //    public List<object> Results { get; set; }
    //}

    //public class RelatedEntity
    //{
    //    public string EntityUid { get; set; }
    //    public int EntityTypeId { get; set; }
    //    public int EntityBaseId { get; set; }
    //    public string EntityBaseName { get; set; }
    //    public bool IsEditorUpdate { get; set; }
    //    public string StatusMessage { get; set; }
    //    public string LastUpdatedDisplay { get; set; }
    //    public string EntityType { get; set; }
    //    public ExtraData ExtraData { get; set; }
    //    public int Id { get; set; }
    //    public string RowIdString { get; set; }
    //    public DateTime Created { get; set; }
    //    public DateTime LastUpdated { get; set; }
    //    public bool IsTopLevelEntity { get; set; }
    //}

    //public class RenewalFrequencyPublish
    //{
    //}


    //public class ScoringMethodType
    //{
    //    public List<object> Items { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<object> ItemsAsAlignmentObjects { get; set; }
    //    public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<object> ItemsAsString { get; set; }
    //}

    //public class ScoringMethodTypes
    //{
    //    public bool HasAnIdentifer { get; set; }
    //    public List<object> Results { get; set; }
    //}

    //public class ServiceType
    //{
    //    public List<object> Items { get; set; }
    //    public string OtherValue { get; set; }
    //    public List<object> ItemsAsAlignmentObjects { get; set; }
    //    public List<object> ItemsAsAlignmentObjectsWithCodes { get; set; }
    //    public List<object> ItemsAsString { get; set; }
    //}

    //public class TargetAssessment
    //{
    //    public LifeCycleStatusType LifeCycleStatusType { get; set; }
    //    public OwnerRoles OwnerRoles { get; set; }
    //    public List<object> OfferedByOrganization { get; set; }
    //    public List<object> InLanguageIds { get; set; }
    //    public List<object> InLanguageCodeList { get; set; }
    //    public List<object> Auto_InLanguageCode { get; set; }
    //    public List<object> AlternateName { get; set; }
    //    public List<object> AlternateNameTVP { get; set; }
    //    public AssessmentUseType AssessmentUseType { get; set; }
    //    public AudienceLevelType AudienceLevelType { get; set; }
    //    public AudienceType AudienceType { get; set; }
    //    public CreditValue CreditValue { get; set; }
    //    public DeliveryType DeliveryType { get; set; }
    //    public List<object> ProcessProfiles { get; set; }
    //    public List<object> AdministrationProcess { get; set; }
    //    public List<object> DevelopmentProcess { get; set; }
    //    public List<object> MaintenanceProcess { get; set; }
    //    public List<object> EstimatedCost { get; set; }
    //    public List<object> EstimatedCost_Merged { get; set; }
    //    public List<object> FinancialAssistance { get; set; }
    //    public List<object> EstimatedDuration { get; set; }
    //    public List<object> Subject { get; set; }
    //    public List<object> Subjects { get; set; }
    //    public List<object> Auto_Subject { get; set; }
    //    public List<object> Keyword { get; set; }
    //    public List<object> AssessesCompetencies { get; set; }
    //    public List<object> AssessesCompetenciesFrameworks { get; set; }
    //    public List<object> RequiresCompetenciesFrameworks { get; set; }
    //    public List<object> TargetCompetency { get; set; }
    //    public List<object> Addresses { get; set; }
    //    public List<object> Identifier { get; set; }
    //    public List<object> IdentifierTVP { get; set; }
    //    public List<object> WhereReferenced { get; set; }
    //    public List<object> IsPartOfConditionProfile { get; set; }
    //    public List<object> IsPartOfCredential { get; set; }
    //    public List<object> IsPartOfLearningOpp { get; set; }
    //    public List<object> IsResourceOnETPL { get; set; }
    //    public List<object> CommonCosts { get; set; }
    //    public List<object> CommonConditions { get; set; }
    //    public List<object> AllConditions { get; set; }
    //    public List<object> Requires { get; set; }
    //    public List<object> Recommends { get; set; }
    //    public List<object> AssessmentConnections { get; set; }
    //    public AssessmentConnectionsList AssessmentConnectionsList { get; set; }
    //    public List<object> PreparationFrom { get; set; }
    //    public List<object> AdvancedStandingFrom { get; set; }
    //    public List<object> IsRequiredFor { get; set; }
    //    public List<object> IsRecommendedFor { get; set; }
    //    public List<object> IsAdvancedStandingFor { get; set; }
    //    public List<object> IsPreparationFor { get; set; }
    //    public List<object> Corequisite { get; set; }
    //    public List<object> EntryCondition { get; set; }
    //    public AgentAndRoles AgentAndRoles { get; set; }
    //    public Occupation Occupation { get; set; }
    //    public OccupationResults OccupationResults { get; set; }
    //    public OccupationOtherResults OccupationOtherResults { get; set; }
    //    public Industry Industry { get; set; }
    //    public IndustryResults IndustryResults { get; set; }
    //    public IndustryOtherResults IndustryOtherResults { get; set; }
    //    public InstructionalProgramType InstructionalProgramType { get; set; }
    //    public InstructionalProgramResults InstructionalProgramResults { get; set; }
    //    public OtherInstructionalProgramResults OtherInstructionalProgramResults { get; set; }
    //    public List<object> AlternativeInstructionalProgramType { get; set; }
    //    public AssessmentMethodType AssessmentMethodType { get; set; }
    //    public AssessmentMethodTypes AssessmentMethodTypes { get; set; }
    //    public List<object> SameAs { get; set; }
    //    public ScoringMethodType ScoringMethodType { get; set; }
    //    public List<object> TargetLearningResource { get; set; }
    //    public List<object> Region { get; set; }
    //    public List<object> JurisdictionAssertions { get; set; }
    //    public AssessmentUseTypes AssessmentUseTypes { get; set; }
    //    public ScoringMethodTypes ScoringMethodTypes { get; set; }
    //    public DeliveryMethodTypes DeliveryMethodTypes { get; set; }
    //    public QualityAssurance QualityAssurance { get; set; }
    //    public List<object> CommonCostsList { get; set; }
    //    public List<object> CommonConditionsList { get; set; }
    //    public List<object> FrameworksList { get; set; }
    //    public OwningOrganization OwningOrganization { get; set; }
    //    public string OrganizationName { get; set; }
    //    public List<object> OrganizationRole { get; set; }
    //    public string LastPublishDate { get; set; }
    //    public List<object> Jurisdiction { get; set; }
    //    public bool IsEditorUpdate { get; set; }
    //    public string StatusMessage { get; set; }
    //    public string LastUpdatedDisplay { get; set; }
    //    public string EntityType { get; set; }
    //    public ExtraData ExtraData { get; set; }
    //    public string RowIdString { get; set; }
    //}

    public class TargetComponent
    {
        public string PathwayComponentType { get; set; }
        public string TypeLabel { get; set; }
        public int PathwayComponentTypeId { get; set; }
        public int ComponentRelationshipTypeId { get; set; }
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }
        public string PathwayCTID { get; set; }
        public List<object> AllComponents { get; set; }
        public List<object> HasChild { get; set; }
        public List<object> HasCondition { get; set; }
        public List<object> Identifier { get; set; }
        public List<object> IsChildOf { get; set; }
        public List<object> HasProgressionLevels { get; set; }
        public string HasProgressionLevelDisplay { get; set; }
        public List<object> ProgressionLevels { get; set; }
        public List<object> PrecededBy { get; set; }
        public List<object> Precedes { get; set; }
        public string ProxyFor { get; set; }
        public string ComponentCategory { get; set; }
        public string ProgramTerm { get; set; }
        public string CredentialType { get; set; }
        public IndustryType IndustryType { get; set; }
        public OccupationType OccupationType { get; set; }
        public JsonProperties JsonProperties { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CTID { get; set; }
        public string SubjectWebpage { get; set; }
        //public string OrganizationName { get; set; }
        //public List<object> OrganizationRole { get; set; }
        //public string LastPublishDate { get; set; }
        //public List<object> Jurisdiction { get; set; }
        //public bool IsEditorUpdate { get; set; }
        //public string StatusMessage { get; set; }
        //public string LastUpdatedDisplay { get; set; }
        //public RelatedEntity RelatedEntity { get; set; }
        //public DateTime EntityLastUpdated { get; set; }
        //public ExtraData ExtraData { get; set; }
        public int Id { get; set; }
        public string RowId { get; set; }
        public string RowIdString { get; set; }
        public DateTime Created { get; set; }
        public int CreatedById { get; set; }
        public DateTime LastUpdated { get; set; }
        public int LastUpdatedById { get; set; }
    }

    public class TargetCredential
    {
        //public string OwnerNameAndCredentialName { get; set; }
        //public OwnerRoles OwnerRoles { get; set; }
        //public List<object> OwnerOrganizationRoles { get; set; }
        //public List<object> InLanguageIds { get; set; }
        //public List<object> InLanguageCodeList { get; set; }
        //public List<object> Auto_InLanguageCode { get; set; }
        //public List<object> Auto_InLanguageCode2 { get; set; }
        //public bool IsDescriptionRequired { get; set; }
        //public List<object> AlternateNames { get; set; }
        //public List<object> Identifier { get; set; }
        //public List<object> Auto_LatestVersion { get; set; }
        //public List<object> Auto_PreviousVersion { get; set; }
        //public List<object> Auto_AvailableOnlineAt { get; set; }
        //public List<object> Auto_AvailabilityListing { get; set; }
        //public List<object> Auto_ImageUrl { get; set; }
        //public List<object> Addresses { get; set; }
        //public List<object> Locations { get; set; }
        //public List<object> JurisdictionAssertions { get; set; }
        //public List<object> EstimatedDuration { get; set; }
        //public List<object> RenewalFrequency { get; set; }
        //public RenewalFrequencyPublish RenewalFrequency_Publish { get; set; }
        //public CredentialType CredentialType { get; set; }
        //public string ProfileType { get; set; }
        //public AudienceLevelType AudienceLevelType { get; set; }
        //public AudienceType AudienceType { get; set; }
        //public CredentialStatusType CredentialStatusType { get; set; }
        //public List<object> EmbeddedCredentials { get; set; }
        //public List<object> IsPartOf { get; set; }
        //public List<object> HasPartIds { get; set; }
        //public List<object> IsPartOfIds { get; set; }
        //public List<object> ETPLCredentials { get; set; }
        //public List<object> ETPLAssessments { get; set; }
        //public List<object> ETPLLearningOpportunities { get; set; }
        //public List<object> HasETPLMembers { get; set; }
        //public List<object> IsResourceOnETPL { get; set; }
        //public List<object> HasTransferValueProfile { get; set; }
        //public List<object> CredentialProcess { get; set; }
        //public List<object> AdministrationProcess { get; set; }
        //public List<object> DevelopmentProcess { get; set; }
        //public List<object> MaintenanceProcess { get; set; }
        //public List<object> AppealProcess { get; set; }
        //public List<object> ComplaintProcess { get; set; }
        //public List<object> RevocationProcess { get; set; }
        //public List<object> ReviewProcess { get; set; }
        //public Industry Industry { get; set; }
        //public List<object> Naics { get; set; }
        //public List<object> AlternativeIndustries { get; set; }
        //public Occupation Occupation { get; set; }
        //public List<object> AlternativeOccupations { get; set; }
        //public InstructionalProgramResults InstructionalProgramResults { get; set; }
        //public NavyRating NavyRating { get; set; }
        //public AssessmentDeliveryType AssessmentDeliveryType { get; set; }
        //public LearningDeliveryType LearningDeliveryType { get; set; }
        //public List<object> OfferedByOrganizationRole { get; set; }
        //public List<object> OfferedByOrganization { get; set; }
        //public List<object> QualityAssuranceAction { get; set; }
        //public List<object> AllConditions { get; set; }
        //public List<object> CredentialConnections { get; set; }
        //public List<object> CommonCosts { get; set; }
        //public List<object> CommonConditions { get; set; }
        //public List<object> Requires { get; set; }
        //public List<object> Recommends { get; set; }
        //public List<object> PreparationFrom { get; set; }
        //public List<object> AdvancedStandingFrom { get; set; }
        //public List<object> IsRequiredFor { get; set; }
        //public List<object> IsRecommendedFor { get; set; }
        //public List<object> IsAdvancedStandingFor { get; set; }
        //public List<object> IsPreparationFor { get; set; }
        //public List<object> Renewal { get; set; }
        //public List<object> Corequisite { get; set; }
        //public List<object> Revocation { get; set; }
        //public List<object> Keyword { get; set; }
        //public List<object> Subject { get; set; }
        //public List<object> Auto_Subject { get; set; }
        //public List<object> DegreeConcentration { get; set; }
        //public List<object> DegreeMajor { get; set; }
        //public List<object> DegreeMinor { get; set; }
        //public List<object> Auto_DegreeConcentration { get; set; }
        //public List<object> Auto_DegreeMajor { get; set; }
        //public List<object> Auto_DegreeMinor { get; set; }
        //public List<object> EstimatedCosts { get; set; }
        //public List<object> AssessmentEstimatedCosts { get; set; }
        //public List<object> LearningOpportunityEstimatedCosts { get; set; }
        //public List<object> EstimatedCost { get; set; }
        //public List<object> FinancialAssistance { get; set; }
        //public List<object> RequiresCompetenciesFrameworks { get; set; }
        //public List<object> TargetAssessment { get; set; }
        //public List<object> TargetLearningOpportunity { get; set; }
        //public List<object> SameAs { get; set; }
        //public List<object> UsesVerificationService { get; set; }
        public string Name { get; set; }
        //public OwningOrganization OwningOrganization { get; set; }
        public string OrganizationName { get; set; }
        //public List<object> OrganizationRole { get; set; }
        public int EntityStateId { get; set; }
        //public string LastPublishDate { get; set; }
        //public List<object> Jurisdiction { get; set; }
        //public bool IsEditorUpdate { get; set; }
        //public string StatusMessage { get; set; }
        //public string LastUpdatedDisplay { get; set; }
        //public string EntityType { get; set; }
        //public ExtraData ExtraData { get; set; }
        //public string RowIdString { get; set; }
    }

    public class TargetLearningOpportunity
    {
        //public LearningType LearningType { get; set; }
        //public int LearningEntityTypeId { get; set; }
        //public LifeCycleStatusType LifeCycleStatusType { get; set; }
        //public List<object> Auto_AvailableOnlineAt { get; set; }
        //public List<object> AvailableAt { get; set; }
        //public OwnerRoles OwnerRoles { get; set; }
        //public List<object> OfferedByOrganization { get; set; }
        //public List<object> InLanguageIds { get; set; }
        //public List<object> InLanguageCodeList { get; set; }
        //public List<object> Auto_InLanguageCode { get; set; }
        //public AudienceLevelType AudienceLevelType { get; set; }
        //public AudienceType AudienceType { get; set; }
        //public CreditValue CreditValue { get; set; }
        //public DeliveryType DeliveryType { get; set; }
        //public List<object> EstimatedDuration { get; set; }
        //public Occupation Occupation { get; set; }
        //public Industry Industry { get; set; }
        //public InstructionalProgramType InstructionalProgramType { get; set; }
        //public InstructionalProgramResults InstructionalProgramResults { get; set; }
        //public List<object> HasPart { get; set; }
        //public List<object> IsPartOf { get; set; }
        //public List<object> Prerequisite { get; set; }
        //public List<object> IsResourceOnETPL { get; set; }
        //public List<object> Keyword { get; set; }
        //public List<object> Subject { get; set; }
        //public List<object> Subjects { get; set; }
        //public List<object> Auto_Subject { get; set; }
        //public List<object> WhereReferenced { get; set; }
        //public List<object> Addresses { get; set; }
        //public List<object> AlternateName { get; set; }
        //public List<object> AlternateNameTVP { get; set; }
        //public List<object> Auto_AvailabilityListing { get; set; }
        //public List<object> SameAs { get; set; }
        //public List<object> IsPartOfConditionProfile { get; set; }
        //public List<object> IsPartOfCredential { get; set; }
        //public List<object> TeachesCompetencies { get; set; }
        //public List<object> TargetCompetency { get; set; }
        //public List<object> TeachesCompetenciesFrameworks { get; set; }
        //public List<object> RequiresCompetenciesFrameworks { get; set; }
        //public List<object> TargetCompetencies { get; set; }
        //public AssessmentMethodType AssessmentMethodType { get; set; }
        //public AssessmentMethodTypes AssessmentMethodTypes { get; set; }
        //public List<object> Region { get; set; }
        //public List<object> JurisdictionAssertions { get; set; }
        //public LearningMethodType LearningMethodType { get; set; }
        //public List<object> EstimatedCost { get; set; }
        //public List<object> EstimatedCost_Merged { get; set; }
        //public List<object> FinancialAssistance { get; set; }
        //public List<object> CommonCosts { get; set; }
        //public List<object> CommonConditions { get; set; }
        //public List<object> Identifier { get; set; }
        //public List<object> AllConditions { get; set; }
        //public List<object> Requires { get; set; }
        //public List<object> Recommends { get; set; }
        //public List<object> LearningOppConnections { get; set; }
        //public LearningOppConnectionsList LearningOppConnectionsList { get; set; }
        //public List<object> PreparationFrom { get; set; }
        //public List<object> AdvancedStandingFrom { get; set; }
        //public List<object> IsRequiredFor { get; set; }
        //public List<object> IsRecommendedFor { get; set; }
        //public List<object> IsAdvancedStandingFor { get; set; }
        //public List<object> IsPreparationFor { get; set; }
        //public List<object> Corequisite { get; set; }
        //public List<object> EntryCondition { get; set; }
        //public QualityAssurance QualityAssurance { get; set; }
        //public List<object> CommonCostsList { get; set; }
        //public List<object> CommonConditionsList { get; set; }
        //public List<object> FrameworksList { get; set; }
        //public OwningOrganization OwningOrganization { get; set; }
        public string OrganizationName { get; set; }
       // public List<object> OrganizationRole { get; set; }
        public string LastPublishDate { get; set; }
        //public List<object> Jurisdiction { get; set; }
        //public bool IsEditorUpdate { get; set; }
        //public string StatusMessage { get; set; }
        //public string LastUpdatedDisplay { get; set; }
        //public ExtraData ExtraData { get; set; }
        //public string RowIdString { get; set; }
    }

    //public class TargetOrganization
    //{
    //    public LifeCycleStatusType LifeCycleStatusType { get; set; }
    //    public string AgentType { get; set; }
    //    public int AgentTypeId { get; set; }
    //    public OrganizationSubclass OrganizationSubclass { get; set; }
    //    public string Auto_OrgURI { get; set; }
    //    public List<object> Addresses { get; set; }
    //    public List<object> Auto_Address { get; set; }
    //    public FirstAddress FirstAddress { get; set; }
    //    public List<object> Auto_AvailabilityListing { get; set; }
    //    public List<object> SocialMediaPages { get; set; }
    //    public List<object> Auto_SocialMedia { get; set; }
    //    public List<object> PhoneNumbers { get; set; }
    //    public List<AutoTargetContactPointForDetail> Auto_TargetContactPointForDetail { get; set; }
    //    public List<object> Emails { get; set; }
    //    public List<object> Keyword { get; set; }
    //    public List<object> AlternateName { get; set; }
    //    public List<object> ContactPoint { get; set; }
    //    public List<object> IdentificationCodes { get; set; }
    //    public List<object> AlternativeIdentifiers { get; set; }
    //    public List<object> ID_AlternativeIdentifier { get; set; }
    //    public string FoundingDate { get; set; }
    //    public string FoundingYear { get; set; }
    //    public string FoundingMonth { get; set; }
    //    public string FoundingDay { get; set; }
    //    public string Founded { get; set; }
    //    public OrganizationType OrganizationType { get; set; }
    //    public OrganizationSectorType OrganizationSectorType { get; set; }
    //    public AgentSectorType AgentSectorType { get; set; }
    //    public ServiceType ServiceType { get; set; }
    //    public List<object> HasConditionManifest { get; set; }
    //    public List<object> HasCostManifest { get; set; }
    //    public List<object> ParentOrganizations { get; set; }
    //    public List<object> OrganizationRole_Dept { get; set; }
    //    public List<object> OrganizationRole_Subsidiary { get; set; }
    //    public List<object> OrganizationRole_QAPerformed { get; set; }
    //    public List<object> OrganizationRole_Actor { get; set; }
    //    public List<object> OrganizationThirdPartyAssertions { get; set; }
    //    public List<object> OrganizationFirstPartyAssertions { get; set; }
    //    public List<object> CredentialAssertions { get; set; }
    //    public List<object> OrganizationAssertions { get; set; }
    //    public List<object> AssessmentAssertions { get; set; }
    //    public List<object> LoppAssertions { get; set; }
    //    public List<object> OrganizationRole_Recipient { get; set; }
    //    public List<object> OrganizationRole { get; set; }
    //    public Identifiers Identifiers { get; set; }
    //    public List<object> Identifier { get; set; }
    //    public List<object> VerificationServiceProfiles { get; set; }
    //    public List<object> HasVerificationService { get; set; }
    //    public List<object> CreatedCredentials { get; set; }
    //    public List<object> Owns_Auto_Organization_OwnsCredentials { get; set; }
    //    public List<object> OfferedCredentials { get; set; }
    //    public List<object> OwnedAssessments { get; set; }
    //    public List<object> OwnedLearningOpportunities { get; set; }
    //    public List<object> QACredentials { get; set; }
    //    public List<object> JurisdictionAssertions { get; set; }
    //    public List<object> AgentProcess { get; set; }
    //    public Industry Industry { get; set; }
    //    public IndustryType IndustryType { get; set; }
    //    public List<object> AlternativeIndustries { get; set; }
    //    public List<object> InLanguageIds { get; set; }
    //    public List<object> ProcessProfiles { get; set; }
    //    public List<object> AppealProcess { get; set; }
    //    public List<object> ComplaintProcess { get; set; }
    //    public List<object> ReviewProcess { get; set; }
    //    public List<object> RevocationProcess { get; set; }
    //    public List<object> AdministrationProcess { get; set; }
    //    public List<object> DevelopmentProcess { get; set; }
    //    public List<object> MaintenanceProcess { get; set; }
    //    public List<object> VerificationStatus { get; set; }
    //    public string OrganizationName { get; set; }
    //    public string LastPublishDate { get; set; }
    //    public List<object> Jurisdiction { get; set; }
    //    public bool IsEditorUpdate { get; set; }
    //    public string StatusMessage { get; set; }
    //    public string LastUpdatedDisplay { get; set; }
    //    public string EntityType { get; set; }
    //    public ExtraData ExtraData { get; set; }
    //    public string RowIdString { get; set; }
    //}


}
