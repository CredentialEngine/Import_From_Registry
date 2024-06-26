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
using APIResourceServices = workIT.Services.API.PathwayServices;
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


				output.CTID = input.CTID;
				output.Name = helper.HandleLanguageMap( input.Name, output, "Name" );
                output.Description = helper.HandleLanguageMap( input.Description, output, "Description" );
                output.SubjectWebpage = input.SubjectWebpage;

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

                output.LifeCycleStatusType = helper.MapCAOToEnumermation( input.LifeCycleStatusType );
                output.LatestVersion = input.LatestVersion ?? "";
                output.PreviousVersion = input.PreviousVersion ?? "";
                output.NextVersion = input.NextVersion ?? "";
                output.VersionIdentifier = helper.MapIdentifierValueListInternal( input.VersionIdentifier );
                if ( output.VersionIdentifier != null && output.VersionIdentifier.Count() > 0 )
                {
                    output.VersionIdentifierJson = JsonConvert.SerializeObject( output.VersionIdentifier, MappingHelperV3.GetJsonSettings() );
                }


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
                    if ( item.PrecededBy != null )
                    {
                        foreach ( string id in item.PrecededBy )
                        {
                            HandleExternalComponent( id, item, input, output, messages );
                        }

                    }
                    if ( item.IsChildOf != null )
                    {
                        foreach ( string id in item.IsChildOf )
                        {
                            HandleExternalComponent( id, item, input, output, messages );
                        }
                    }
                    if ( item.HasCondition != null )
                    {
                        ProcessConditions( item.HasCondition, input, item, output, messages );

                    }
                    var c = ImportComponent( item, output, bnodes, status );
                    output.HasPart.Add( c );
                }
                //
                //check if in publisher
                ThisResource externalPathway = new ThisResource();
                if ( GetPathwayLayoutDataFromPublisher( output.CTID, output ) )
                {
                    //mark the pathway one way or the other to control use or not using with pathway display
                    if ( output.Properies == null )
                        output.Properies = new PathwayJSONProperties();

                    output.AllowUseOfPathwayDisplay = output.Properies.AllowUseOfPathwayDisplay = true;
                }

                var externalComponent = output.HasPart.Where( part => part.ExternalPathwayCTID != null ).ToList();
                foreach ( var comp in externalComponent )
                {
                    var component = PathwayComponentManager.GetByCtid( comp.ProxyFor );
                    foreach ( var item in output.HasPart )
                    {
                        if ( item.HasPrecededByList != null )
                        {
                            for ( int i = 0; i < item.HasPrecededByList.Count; i++ )
                            {
                                if ( item.HasPrecededByList[i] == component.RowId )
                                {
                                    item.HasPrecededByList[i] = comp.RowId;
                                }
                            }
                        }
                        if ( item.HasIsChildOfList != null )
                        {
                            for ( int i = 0; i < item.HasIsChildOfList.Count; i++ )
                            {
                                if ( item.HasIsChildOfList[i] == component.RowId )
                                {
                                    item.HasIsChildOfList[i] = comp.RowId;
                                }
                            }
                        }
                        if ( item.HasCondition != null && item.HasCondition.Count > 0 )
                        {
                            UpdateHasTargetComponent( item.HasCondition, component, comp );

                        }
                    }

                }


                //adding common import pattern
                importSuccessfull = mgr.Import( output, ref status );

                //TODO 
                if ( output.AllowUseOfPathwayDisplay == false )
                {
                    FormatPathwayForViewer( output, status ); 

				}
                //start storing the finder api ready version
                var resource = APIResourceServices.GetDetailForAPI( output.Id, true );
                if ( importSuccessfull )
                {
                    var resourceDetail = JsonConvert.SerializeObject( resource, JsonHelper.GetJsonSettings( false ) );

                    var statusMsg = "";
                    if ( new EntityManager().EntityCacheUpdateResourceDetail( output.CTID, resourceDetail, ref statusMsg ) == 0 )
                    {
                        status.AddError( statusMsg );
                    }
					//24-03-25 - start using the generic process
					new ProfileServices().IndexPrepForReferenceResource( helper.ResourcesToIndex, ref status );
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

            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError(ex, string.Format("Exception encountered in CTID: {0}", input.CTID));
            }

            return importSuccessfull;
		}

		//
		/// <summary>
		/// TODO - rough out formatting a pathway for display
		/// </summary>
		/// <param name="pathway"></param>
		/// <param name="status">TODO - do we need the pathway SaveStatus?</param>
		/// <returns></returns>
		public void FormatPathwayForViewer( ThisResource pathway,  SaveStatus status )
		{


            /*
             * get the progression model
             *     - if found get all levels and populated ???
             * get the destination component
             * get conditions
             *  - will display in the dest component section?
             *  - 
             */ 
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

                if ( input.PathwayComponentType=="ceterms:MultiComponent" &&  input.ProxyForList.Count>0 )
                {
                    var proxyForList = new List<TopLevelEntityReference>();
                    foreach(var proxyFor in input.ProxyForList )
                    {
                        if ( !string.IsNullOrWhiteSpace( proxyFor ) && proxyFor.IndexOf( "/resources/" ) > 0 )
                        {
                            //TODO - make more generic - use entity_cache and resourceSummary
                            var ctid = ResolutionServices.ExtractCtid( proxyFor );
                            if ( !string.IsNullOrWhiteSpace( ctid ) )
                            {
                                //store the ctid in ProxyFor
                                output.ProxyFor = ctid;
                                //could just use one call to entity cache?
                                //NO. need to be able to set up relationship to the related detail page.
                                //could store resourceSummary in the json?
                                var entity = EntityManager.EntityCacheGetByCTID( ctid );
                                if ( entity != null && entity.Id > 0 )
                                {
                                    //this approach 'buries' the cred from external references like credential in pathway
                                    //21-07-22 mparsons - OK, the database manager creates the Entity.Credential relationships as needed
                                    var entityReference = new TopLevelEntityReference()
                                    {
                                        Id = entity.BaseId,
                                        Name = entity.Name,
                                        Description = entity.Description,
                                        CTID = entity.CTID,
                                        SubjectWebpage = entity.SubjectWebpage,
                                        EntityType = entity.EntityType,
                                        EntityTypeId = entity.EntityTypeId,
                                        EntityStateId = entity.EntityStateId,

                                        DetailURL = credentialFinderMainSite + "resources/" + entity.CTID
                                    };
                                    proxyForList.Add( entityReference );
                                }
                                output.JsonProperties.ProxyForResourceList = proxyForList;
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
            //bool usingWrapper = true;
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
                        else //external Component CTID won't be there
                        {
                            var externalPC = importedPathway.HasPart.FirstOrDefault( x => x.Name == wrapperPC.Name && x.PathwayComponentType.Contains( wrapperPC.Type.Replace( "ceterms:", "" )) );
                            if ( externalPC != null )
                            {
                                externalPC.JsonProperties.ColumnNumber = externalPC.ColumnNumber = wrapperPC.ColumnNumber;
                                externalPC.JsonProperties.RowNumber = externalPC.RowNumber = wrapperPC.RowNumber;
                                ////todo handle conditions.may have to handle here
                                if ( externalPC.HasCondition != null && externalPC.HasCondition.Any() )
                                {
                                    externalPC.HasCondition = MapConditions( pathwayWrapper, wrapperPC.RowId, externalPC.HasCondition, ref statusMessage );
                                }

                                //worst case could be one component per level and all at 0,0 - unlikely
                                if ( externalPC.ColumnNumber > 0 || externalPC.RowNumber > 0 )
                                {
                                    hasPathwayBuilderLayout = true;
                                }
                                if(externalPC.CTID == null )
                                {
                                    externalPC.CTID = wrapperPC.CTID;// adding the publisher ctid and rowId for the externalcomponent
                                    if ( Guid.TryParse( wrapperPC.RowId, out Guid rowId ) )
                                    {
                                        externalPC.RowId = rowId;
                                        importedPathway.HasPartList.Add( rowId );

                                    }
                                }
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

        public static PathwayComponent HandleExternalComponent( string id, InputComponent pathwayComponent, InputResource input, Pathway output, List<string> messages )
        {
            var component = new PathwayComponent();
            string externalComponentId = "";
            var pos = id.ToLower().IndexOf( "/resources/ce-" );
            if ( pos > 1 )
            {
                externalComponentId = id.Substring( pos + 11 );
            }
            if ( !input.HasPart.Contains( id ) && !output.HasPart.Any( x => x.ProxyFor == externalComponentId ) )
            {
                component = PathwayComponentManager.GetByCtid( externalComponentId );
                if ( component.Id > 0 && component != null )
                {
                    component.PrecededBy = null;
                    component.Precedes = null;
                    component.HasChild = null;
                    component.IsChildOf = null;
                    component.ProxyFor = component.CTID;
                    component.Id = 0;
                    if ( pathwayComponent.HasProgressionLevel != null && pathwayComponent.HasProgressionLevel.Any() )
                    {
                        component.HasProgressionLevel = ResolutionServices.ExtractCtid( pathwayComponent.HasProgressionLevel[0] );
                    }
                    else
                    {
                        component.HasProgressionLevel = null;
                        component.HasProgressionLevelDisplay = null;
                        component.HasProgressionLevels = null;
                        component.ProgressionLevels = null;
                    }
                    component.CTID = null;// CTID is set to null here as this is the proxyforCTID 
                    component.JsonProperties.ExternalPathwayCTID = component.ExternalPathwayCTID = component.PathwayCTID;
                    component.HasCondition = null;
                    component.PathwayCTID = output.CTID;
                    output.HasPart.Add( component );
                }
                else
                {
                    messages.Add( string.Format( "The external component {0} was not found in the finder", externalComponentId ) );

                }

            }

            return component;
        }

        private void ProcessConditions( List<JInput.ComponentCondition> conditions, InputResource input, InputComponent inpComponent, Pathway output, List<string> messages )
        {
            foreach ( var cond in conditions )
            {
                if ( cond.TargetComponent != null )
                {
                    foreach ( var id in cond.TargetComponent )
                    {
                       HandleExternalComponent( id, inpComponent, input, output,messages );
                    }
                }
                if(cond.HasCondition != null )
                {
                    ProcessConditions( cond.HasCondition, input, inpComponent,output,messages );

                }
            }
        }
        private void UpdateHasTargetComponent( List<ComponentCondition> conditions, PathwayComponent externalParentcomponent, PathwayComponent externalcomp )
        {
            foreach ( var cond in conditions )
            {
                for ( int i = 0; i < cond.HasTargetComponentList.Count; i++ )
                {
                    if ( cond.HasTargetComponentList[i] == externalParentcomponent.RowId )
                    {
                        cond.HasTargetComponentList[i] = externalcomp.RowId;
                    }
                }

                if ( cond.HasCondition != null && cond.HasCondition.Count > 0 )
                {
                    UpdateHasTargetComponent( cond.HasCondition, externalParentcomponent, externalcomp );
                }
            }
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

    #region Classes used by the import from publisher process

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


    public class ConditionProperties
    {
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }
        public string HasProgressionLevel { get; set; }
    }

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
        public string Name { get; set; }

        public string OrganizationName { get; set; }

        public int EntityStateId { get; set; }

    }

    public class TargetLearningOpportunity
    {

        public string OrganizationName { get; set; }

        public string LastPublishDate { get; set; }

    }
    #endregion
}
