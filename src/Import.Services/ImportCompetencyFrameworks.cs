using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using workIT.Factories;
using workIT.Models;
using workIT.Services;
using workIT.Utilities;

using BNode = RA.Models.JsonV2.BlankNode;
using ResourceServices = workIT.Services.CompetencyFrameworkServices;
//WHY DO WE HAVE 2 classes for CompetencyFramework?
//this is meant for the graph search
using Framework = workIT.Models.ProfileModels.CompetencyFramework;
//CompetencyFramework used by the graph search - not Import
using ThisResource = workIT.Models.Common.CompetencyFramework;

using InputCompetency = RA.Models.JsonV2.Competency;
using InputResource = RA.Models.JsonV2.CompetencyFramework;
using InputGraph = RA.Models.JsonV2.CompetencyFrameworksGraph;

using ApiEntity = workIT.Models.API.CompetencyFramework;
using ApiCompetency = workIT.Models.API.Competency;
using MC = workIT.Models.Common;


namespace Import.Services
{
	public class ImportCompetencyFrameworks
    {
        int entityTypeId = CodesManager.ENTITY_TYPE_COMPETENCY_FRAMEWORK;
        string thisClassName = "ImportCompetencyFrameworks";
		string CurrentFramework = string.Empty;
		string ResourceType = "CompetencyFramework";
		ImportManager importManager = new ImportManager();
        InputGraph input = new InputGraph();
		ImportServiceHelpers importHelper = new ImportServiceHelpers();
		ThisResource output = new ThisResource();
		//??
		//ThisResource output = new ThisResource();

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

			string statusMessage = string.Empty;
			string ctdlType = string.Empty;
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
		public bool ImportByCtid( string ctid, SaveStatus status )
		{
			if ( string.IsNullOrWhiteSpace( ctid ) )
			{
				status.AddError( thisClassName + ".ImportByCtid - a valid ctid must be provided" );
				return false;
			}

			//this is currently specific, assumes envelop contains a credential
			//can use the hack for GetResourceType to determine the type, and then call the appropriate import method
			string statusMessage = string.Empty;
			ResourceServices mgr = new ResourceServices();
			string ctdlType = string.Empty;
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
			int newImportId = importHelper.Add( item, CodesManager.ENTITY_TYPE_COMPETENCY_FRAMEWORK, status.Ctid, importSuccessfull, importError, ref messages );
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
			if ( DateTime.TryParse( item.NodeHeaders.CreatedAt.Replace( "UTC", string.Empty ).Trim(), out createDate ) )
			{
				status.SetEnvelopeCreated( createDate );
			}
			if ( DateTime.TryParse( item.NodeHeaders.UpdatedAt.Replace( "UTC", string.Empty ).Trim(), out envelopeUpdateDate ) )
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
			string ct = RegistryServices.GetGraphPrimaryType( payload );

			string envelopeUrl = RegistryServices.GetEnvelopeUrl( envelopeIdentifier );
            LoggingHelper.DoTrace( 5, "		envelopeUrl: " + envelopeUrl );
			LoggingHelper.WriteLogFile( UtilityManager.GetAppKeyValue( "logFileTraceLevel", 5 ), item.EnvelopeCtid + "_competencyFrameswork", payload, string.Empty, false );
            //input = JsonConvert.DeserializeObject<InputGraph>( item.DecodedResource.ToString() );

            //InputResource framework = GetFramework( input.Graph );
            //LoggingHelper.DoTrace( 5, "		framework name: " + framework.name.ToString() );

            //just store input for now
            return Import( payload, status );

            //return true;
        } //

		/// <summary>
		/// Import a competency framework
		/// </summary>
		/// <param name="payload">Registry Envelope DecodedResource</param>
		/// <param name="envelopeIdentifier"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool Import( string payload, SaveStatus status )
        {
			LoggingHelper.DoTrace( 7, "ImportCompetencyFramesworks - entered." );
            List<string> messages = new List<string>();
			MappingHelperV3 helper = new MappingHelperV3(10);
			bool importSuccessfull = true;
			ResourceServices mgr = new ResourceServices();

			InputResource input = new InputResource();
			InputCompetency comp = new InputCompetency();
			var mainEntity = new Dictionary<string, object>();
			List<InputCompetency> competencies = new List<InputCompetency>();

			JArray graphList = RegistryServices.GetGraphList( payload );
			//??
			var glist = JsonConvert.SerializeObject( graphList );
			//Dictionary<string, object> dictionary = RegistryServices.JsonToDictionary( payload );
			//object graph = dictionary[ "@graph" ];
			////serialize the graph object
			//var glist = JsonConvert.SerializeObject( graph );
			////parse graph in to list of objects
			//JArray graphList = JArray.Parse( glist );
			var bnodes = new List<BNode>();
			int cntr = 0;
			foreach ( var item in graphList )
			{
				cntr++;
				//note older frameworks will not be in the priority order
				var resource = item.ToString();
                var resourceOutline = RegistryServices.GetGraphMainResource( resource );

                if ( resourceOutline.Type ==  "ceasn:CompetencyFramework")
				{
					//HACK
					
					if ( resource.IndexOf( "ceasn:CompetencyFramework" ) > -1 )
					{
						input = JsonConvert.DeserializeObject<InputResource>( resource );
					}					
				}
				else
				{
					//Error converting value "https://credentialengineregistry.org/resources/ce-949fcaba-45ed-44d9-88bf-43677277eb84" to type 'System.Collections.Generic.List`1[System.String]'. Path 'ceasn:isPartOf', line 11, position 108.
					//not set up to handle issues
					//comp = JsonConvert.DeserializeObject<InputCompetency>( item.ToString() );
					//competencies.Add( comp );

					//should just have competencies, but should check for bnodes
					if ( resourceOutline.CtdlId.IndexOf( "_:" ) > -1 )
					{
						bnodes.Add( JsonConvert.DeserializeObject<BNode>( resource ) );
						//ceasn:Competency
					}
					else if ( resourceOutline.Type == "ceasn:Competency" )
					{
						competencies.Add( JsonConvert.DeserializeObject<InputCompetency>( resource ) );
					}
					else
					{
						//unexpected
					}
				}
			}
			//====================================================================================================
			//NOTE: as the registry is used for searching and the detail page, a minimal import is being done
			//====================================================================================================
			string ctid = input.CTID;
            status.Ctid = ctid;
            status.ResourceURL = input.CtdlId;
            LoggingHelper.DoTrace( 5, "		ctid: " + ctid );
            LoggingHelper.DoTrace( 5, "		@Id: " + input.CtdlId );
			LoggingHelper.DoTrace( 5, "		name: " + input.name.ToString() );

			var org = new MC.Organization();
			string orgCTID = string.Empty;
			string orgName = string.Empty;
			List<string> publisher = input.publisher;
			try
			{

				//add/updating CompetencyFramework
				Framework output = new Framework();

				//20-06-11 - need to get creator, publisher, owner where possible
				//	include an org reference with name, swp, and??
				//should check creator first? Or will publisher be more likely to have an account Ctid?
				//24-04-17 mp - should consider DocumentOwnedBy. It will likely be the same as creator/publisher
				//			- at the very least create an entity.agentRelationship ownedBy

				if ( !string.IsNullOrWhiteSpace(status.DocumentOwnedBy) )
				{
					org = OrganizationManager.GetSummaryByCtid( status.DocumentOwnedBy );
					if ( org != null && org.Id > 0 )
					{
						orgCTID= org.CTID;
						//output.OwnedByIds.Add(org.RowId);
					}
				} else
				{
					if ( publisher != null && publisher.Count() > 0 )
					{
						orgCTID = ResolutionServices.ExtractCtid( publisher[0] );
						//NewLine is both \r\n so not appropriate
						//TODO - make a common method
						//orgCTID = orgCTID.Replace( Environment.NewLine, string.Empty );
						//orgCTID = orgCTID.Replace( "\n", String.Empty );
						//orgCTID = orgCTID.Replace( "\r", String.Empty );
						//orgCTID = orgCTID.Replace( "\t", String.Empty );
						//look up org name
						org = OrganizationManager.GetSummaryByCtid( orgCTID );
					}
					else
					{
						//try creator
						List<string> creator = input.creator;
						if ( creator != null && creator.Count() > 0 )
						{
							orgCTID = ResolutionServices.ExtractCtid( creator[0] );
							//look up org name
							org = OrganizationManager.GetSummaryByCtid( orgCTID );
						}
					}
				}

				if ( status.DoingDownloadOnly )
					return true;


				if ( !DoesEntityExist( input.CTID, ref output ) )
				{
					//set the rowid now, so that can be referenced as needed
					//output.RowId = Guid.NewGuid();
					output.RowId = Guid.NewGuid();
					LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".Import(). Record was NOT found using CTID: '{0}'", input.CTID ) );
				}
				else
				{
					LoggingHelper.DoTrace( 6, string.Format( thisClassName + ".Import(). Found record: '{0}' using CTID: '{1}'", input.name, input.CTID ) );
				}

				helper.currentBaseObject = output;
				output.ExistsInRegistry = true;
				if ( input.inLanguage.Count > 0 )
				{
					helper.DefaultLanguage = input.inLanguage[0];
				}
				//store graph
				output.CompetencyFrameworkGraph = glist;
				output.TotalCompetencies = competencies.Count();
				//clear all competency related store to handle obsolete stuff
				output.APIFramework = string.Empty;
				output.ElasticCompentenciesStore = string.Empty;
				CurrentFramework = output.Name = helper.HandleLanguageMap( input.name, output, "Name" );
				output.Description = helper.HandleLanguageMap( input.description, output, "description" );
				output.CTID = input.CTID;
				output.CreatedByIds = helper.MapOrganizationReferenceGuids( $"{ResourceType}.creator", input.creator, ref status );
				output.PublisherIds = helper.MapOrganizationReferenceGuids( $"{ResourceType}.publisher", input.publisher, ref status );
				output.AuthorId = helper.MapOrganizationReferenceGuids( $"{ResourceType}.author", input.author, ref status );


				//
				output.OrganizationCTID = orgCTID;
				if ( org != null && org.Id > 0 )
				{
					orgName = org.Name;
					output.OrganizationId = org.Id;
					helper.CurrentOwningAgentUid = org.RowId;
					output.OwnedByIds.Add( org.RowId );
				}
				output.PublicationStatusType = ( input.publicationStatusType ?? string.Empty ).Replace( "https://credreg.net/ctdlasn/vocabs/publicationStatus/", string.Empty );

				helper.MapInLanguageToTextValueProfile( input.inLanguage, "CompetencyFramework.InLanguage.CTID: " + ctid );
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

				//TBD handling of referencing third party publisher
				helper.MapOrganizationPublishedBy( output, ref status );
				
				//ef.CredentialRegistryId = envelopeIdentifier;
				//additions
				//ef.ind
				//can only handle one source
				int pcnt = 0;
				if ( input.source != null )
				{
					foreach ( var url in input.source )
					{
						pcnt++;
						output.Source = url;
						break;
					}
				}
				output.FrameworkUri = input.CtdlId;
				output.HasTopChild = input.hasTopChild;
				output.VersionIdentifier = helper.MapIdentifierValueList( input.VersionIdentifier );
                
                //would this have to have a CTID? that is can an external URL be provided
                output.LatestVersion = input.LatestVersion ?? string.Empty;
                output.PreviousVersion = input.PreviousVersion ?? string.Empty;
                output.NextVersion = input.NextVersion ?? string.Empty;
                //
                ApiEntity apiFramework = new ApiEntity()
				{
					Name = output.Name,
					CTID = output.CTID,
					Source = output.Source,
					HasTopChild = output.HasTopChild,
					EntityLastUpdated = status.LocalUpdatedDate
				};

				LoggingHelper.DoTrace( 5, string.Format( thisClassName + ".Import(). Number of competencies: '{0}'", competencies?.Count ) );
				
				//test 
				//ElasticManager.LoadCompetencies( ef.Name, ef.CompentenciesStore );

				//Format competencies and store them for future display
				//Could probably decide which of these methods to use via a web config option or something similar
				//Ditto for the inclusion of extended properties
				//22-05-11 - importing full competency again (well basic plus JSON)
				if ( UtilityManager.GetAppKeyValue( "importingFullCompetencies", true ))
				{
					ImportCompetencies( output, competencies, helper, ref status );
					//FormatCompetenciesHierarchy( apiFramework, competencies, helper, false );
					//apiFramework.HasTopChild = null;
				}
				else
				{
					//?store competencies in string?
					if ( competencies != null && competencies.Count > 0 )
					{
						output.TotalCompetencies = competencies.Count();
						//TODO - should we limit this if 1000+
						//do we use competencies in elastic? if not pause this usage
						cntr = 0;
						foreach ( var c in competencies )
						{
							cntr++;
							//var comments = helper.HandleLanguageMapList( c.comment, ef );
							output.Competencies.Add(
								new workIT.Models.Elastic.IndexCompetency()
								{
									Name = c.competencyText.ToString(),
									//CTID = c.CTID, 
									//Description = comments != null && comments.Count() > 0 ? comments[0].ToString()	 : string.Empty
								}
							);
							if ( cntr >= 1000 )
								break;
						}

						//20-07-02 just storing the index ready competencies
						//ef.CompentenciesJson = JsonConvert.SerializeObject( competencies, MappingHelperV3.GetJsonSettings() );
						output.ElasticCompentenciesStore = JsonConvert.SerializeObject( output.Competencies, MappingHelperV3.GetJsonSettings() );
					}

					//just a simple list of competencies that would be stored in the competencyFramework record. 
					FormatCompetenciesFlat( apiFramework, competencies, helper, true );

					//TODO store whole framework or just the competencies?
					output.APIFramework = JsonConvert.SerializeObject( apiFramework, MappingHelperV3.GetJsonSettings() );
				}


				//
				if ( input.hasTopChild != null )
				{
					if ( output.TotalCompetencies == input.hasTopChild.Count() )
					{
						//flat list - use for simple display
					}
					else
					{
						foreach ( var item in input.hasTopChild )
						{

						}
					}
				}
				//adding using common import pattern
				//status.Messages = new List<StatusMessage>();
				mgr.Import( output, ref status );

				////in order to export, do we need the resourceDetail? The data comes from the registry, so?
				//var resource = workIT.Services.API.CompetencyFrameworkServices.GetDetailForAPI( output.Id, true );
				////Remove nulls and empty properties
				//var resourceDetail = JsonConvert.SerializeObject( resource, JsonHelper.GetJsonSettings( false ) );
				//var eManager = new EntityManager();
				//var statusMsg = string.Empty;
				//if ( eManager.EntityCacheUpdateResourceDetail( output.CTID, resourceDetail, ref statusMsg ) == 0 )
				//{
				//	status.AddError( statusMsg );
				//}

				status.DocumentId = output.Id;
				status.DetailPageUrl = string.Format( "~/competencyframework/{0}", output.Id );
				status.DocumentRowId = output.RowId;

				//
				//just in case
				if ( status.HasErrors )
					importSuccessfull = false;

				//if record was added to db, add to/or set EntityResolution as resolved
				int ierId = new ImportManager().Import_EntityResolutionAdd( status.ResourceURL,
							ctid,
							CodesManager.ENTITY_TYPE_COMPETENCY_FRAMEWORK,
							output.RowId,
							output.Id,
							( output.Id > 0 ),
							ref messages,
							output.Id > 0 );
				//
				//framework checks
				if ( input.inLanguage == null || input.inLanguage.Count() == 0 )
				{
					//document for followup
					//LoggingHelper.DoTrace( 5, "		Framework missing inLanguage: " + input.name.ToString() );
				}
			
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError(ex, thisClassName + ".Import", string.Format("Exception encountered for CTID: {0}", ctid));
			}
			finally
			{

			}
			return importSuccessfull;
		}
		//Import full competency data
		public void ImportCompetencies( Framework framework, List<InputCompetency> input, MappingHelperV3 helper, ref SaveStatus status )
		{
			//Format the competency data for all of the competencies
			framework.ImportCompetencies = new List<workIT.Models.ProfileModels.Competency>();
			var competency = new workIT.Models.ProfileModels.Competency();
			var ResourceType = "Competency";
			try
			{
				foreach ( var item in input )
				{
					competency = new workIT.Models.ProfileModels.Competency();
					competency.CompetencyDetailJson = JsonConvert.SerializeObject( item, MappingHelperV3.GetJsonSettings() );
					competency.CtdlId = item.CtdlId;
					competency.CTID = item.CTID;
					competency.CompetencyText = helper.HandleLanguageMap( item.competencyText, framework, "competencyText" );
					competency.CompetencyLabel = helper.HandleLanguageMap( item.competencyLabel, framework, "CompetencyLabel" );
					competency.CompetencyCategory = helper.HandleLanguageMap( item.competencyCategory, framework, "CompetencyCategory" );
					if ( !string.IsNullOrWhiteSpace( item.dateCreated ) )
						competency.Created = DateTime.Parse( item.dateCreated );
					if (!string.IsNullOrEmpty(item.isTopChildOf))
                    {
						competency.IsTopChildOf = true;
					}
					//note: the db portion is using lastUpdated date to determine if something should be deleted (i.e. not part of the current download). 
					//		So we may want to have a separate dateModified in the db table?
					if (!string.IsNullOrWhiteSpace( item.dateModified ) )
						competency.DateModified = DateTime.Parse( item.dateModified );
					//competency.LastUpdated = helper.MapDate( item.dateModified, "dateModified", ref status );
					//
					competency.PhysicalCapabilityType = helper.MapEntityCTIDsToResourceSummary( item.PhysicalCapabilityType, CodesManager.ENTITY_TYPE_CONCEPT );
					competency.PerformanceLevelType = helper.MapEntityCTIDsToResourceSummary( item.PerformanceLevelType, CodesManager.ENTITY_TYPE_CONCEPT );
					competency.EnvironmentalHazardType = helper.MapEntityCTIDsToResourceSummary( item.EnvironmentalHazardType, CodesManager.ENTITY_TYPE_CONCEPT );
					competency.SensoryCapabilityType = helper.MapEntityCTIDsToResourceSummary( item.SensoryCapabilityType, CodesManager.ENTITY_TYPE_CONCEPT );
					//substanciating
					//
					competency.SubstantiatingCompetencyFrameworkIds = helper.MapEntityReferences( $"{ResourceType}.item.SubstantiatingCompetencyFramework", item.SubstantiatingCompetencyFramework, CodesManager.ENTITY_TYPE_COMPETENCY_FRAMEWORK, ref status );
					competency.SubstantiatingCredentialIds = helper.MapEntityReferences( $"{ResourceType}.item.SubstantiatingCredential", item.SubstantiatingCredential, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status );
					competency.SubstantiatingJobIds = helper.MapEntityReferences( $"{ResourceType}.item.SubstantiatingJob", item.SubstantiatingJob, CodesManager.ENTITY_TYPE_JOB_PROFILE, ref status );
					competency.SubstantiatingOccupationIds = helper.MapEntityReferences( $"{ResourceType}.item.SubstantiatingOccupation", item.SubstantiatingOccupation, CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE, ref status );
					competency.SubstantiatingOrganizationIds = helper.MapEntityReferences( $"{ResourceType}.item.SubstantiatingOrganization", item.SubstantiatingOrganization, CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, ref status );
					competency.SubstantiatingTaskIds = helper.MapEntityReferences( $"{ResourceType}.item.SubstantiatingTask", item.SubstantiatingTask, CodesManager.ENTITY_TYPE_TASK_PROFILE, ref status );
					competency.SubstantiatingWorkroleIds = helper.MapEntityReferences( $"{ResourceType}.item.SubstantiatingWorkrole", item.SubstantiatingWorkrole, CodesManager.ENTITY_TYPE_WORKROLE_PROFILE, ref status );

					//?will need a different method for this? 
					competency.SubstantiatingResourceIds = helper.MapEntityReferences( $"{ResourceType}.item.SubstantiatingResource", item.SubstantiatingResource, CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE, ref status );

					competency.VersionIdentifier = helper.MapIdentifierValueList( item.VersionIdentifier );


					//
					framework.ImportCompetencies.Add( competency );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName, "ImportCompetencies" );

			}
		}

		//Format competency data
		public void FormatCompetenciesFlat( ApiEntity entity, List<InputCompetency> input, MappingHelperV3 helper, bool includeExtendedProperties )
		{
			//Format the competency data for all of the competencies
			entity.Meta_HasPart = new List<ApiCompetency>();
			
			try
			{
				foreach ( var sourceCompetency in input )
				{
					var apiCompetency = TranslateInputCompetencyToApiCompetency( sourceCompetency, helper, includeExtendedProperties, true );
					entity.Meta_HasPart.Add( apiCompetency );
				}
			} catch (Exception ex)
			{
				LoggingHelper.LogError( ex, thisClassName, "FormatCompetenciesFlat" );

			}
		}
		/*
		public void FormatCompetenciesHierarchy( ApiEntity entity, List<InputCompetency> input, MappingHelperV3 helper, bool includeExtendedProperties )
		{
			//Start with hasTopChild
			//Loop through and fill out the hierarchy with embedded comps

			var output = new List<ApiCompetency>();
			var ac = new ApiCompetency();
			if ( entity.HasTopChild != null )
			{
				try
				{
					foreach ( var item in entity.HasTopChild )
					{
						//get the competency
						var c = input.Where( s => s.CtdlId == item ).FirstOrDefault();
						if ( c != null && !string.IsNullOrWhiteSpace( c.CTID ) )
						{
							ac = TranslateInputCompetencyToApiCompetency( c, helper, includeExtendedProperties, false );
							if ( c.hasChild != null && c.hasChild.Any() )
							{
								FormatCompetencyChildren( ac, c, input, helper, includeExtendedProperties );
							}

							output.Add( ac );
						}
						else
						{
							//log error
							LoggingHelper.DoTrace( 1, string.Format( "ImportCompetencyFramework. Framewwork: {0}, TopChild: {1} was not found in the list of competencies", entity.Name, item ) );
						}
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName, "FormatCompetenciesHierarchy", false );
				}
				entity.Meta_HasPart = output;
			}
		}


		public void FormatCompetencyChildren( ApiCompetency competency, InputCompetency input, List<InputCompetency> allCompetencies, MappingHelperV3 helper, bool includeExtendedProperties )
		{

			foreach ( var item in input.hasChild )
			{
				//get the competency
				var c = allCompetencies.Where( s => s.CtdlId == item ).FirstOrDefault();
				if ( c != null && !string.IsNullOrWhiteSpace( c.CTID ) )
				{
					var ac = TranslateInputCompetencyToApiCompetency( c, helper, includeExtendedProperties, false );
					if ( c.hasChild != null && c.hasChild.Any() )
					{
						FormatCompetencyChildren( ac, c, allCompetencies, helper, includeExtendedProperties );
					}

					competency.Meta_HasChild.Add( ac );
				}
				else
				{
					//log error
					LoggingHelper.DoTrace( 1, string.Format( "ImportCompetencyFramework.FormatCompetencyChildren() CompetencyCTID: {0}, child: {1} was not found in the list of competencies", competency.CTID, item ) );
				}
			}
		}
				*/
		private ApiCompetency TranslateInputCompetencyToApiCompetency( InputCompetency input, MappingHelperV3 helper, bool includeExtendedProperties, bool includeChildCompetencyURIs )
		{
			var result = new ApiCompetency();
			try
			{
				//Properties
				result.CTID = input.CTID;
				result.CompetencyLabel = helper.HandleLanguageMap( input.competencyLabel, "CompetencyLabel", false );
				result.CompetencyText = helper.HandleLanguageMap( input.competencyText, "CompetencyText", false );
				if ( includeExtendedProperties )
				{
					if ( input.comment != null)
						result.Comment = helper.HandleLanguageMapList( input.comment, null ).FirstOrDefault( m => !string.IsNullOrWhiteSpace( m ) );
					result.CompetencyCategory = helper.HandleLanguageMap( input.competencyCategory, "CompetencyCategory" );
					result.CodedNotation = input.codedNotation;
					result.ListID = input.ListID;
				}

				//Children
				result.HasChild = includeChildCompetencyURIs ? input.hasChild : null;
				


				//
				//result.PhysicalCapabilityType = helper.MapEntityCTIDsToResourceSummary( input.PhysicalCapabilityType, CodesManager.ENTITY_TYPE_CONCEPT );
				//output.PerformanceLevelType = helper.MapEntityCTIDsToResourceSummary( input.PerformanceLevelType, CodesManager.ENTITY_TYPE_CONCEPT );
				//output.EnvironmentalHazardType = helper.MapEntityCTIDsToResourceSummary( input.EnvironmentalHazardType, CodesManager.ENTITY_TYPE_CONCEPT );
				//output.SensoryCapabilityType = helper.MapEntityCTIDsToResourceSummary( input.SensoryCapabilityType, CodesManager.ENTITY_TYPE_CONCEPT );

			} catch (Exception ex)
			{
				//avoid duplicate messages
				//Value cannot be null. Parameter name: source. Add \r\n
				if ( ex.Message == "Value cannot be null.\r\nParameter name: source" )
				{
					var message = ImportManager.FormatExceptions( ex );
					LoggingHelper.DoTrace( 7, thisClassName + ".TranslateInputCompetencyToApiCompetency. \r\n" + message );
				} else 
					LoggingHelper.LogError( ex, thisClassName, CurrentFramework + ".TranslateInputCompetencyToApiCompetency" );
			}
			return result;
		}
		//

		public bool DoesEntityExist( string ctid, ref Framework entity )
		{
			bool exists = false;
			entity = ResourceServices.GetCompetencyFrameworkByCtid( ctid );
			if ( entity != null && entity.Id > 0 )
				return true;

			return exists;
		}
		private InputResource GetFramework( object graph )
        {
            //string ctid = string.Empty;
            InputResource entity = new InputResource();
            Newtonsoft.Json.Linq.JArray jarray = ( Newtonsoft.Json.Linq.JArray ) graph;
            foreach ( var token in jarray )
            {
                if ( token.GetType() == typeof( Newtonsoft.Json.Linq.JObject ) )
                {
                    if ( token.ToString().IndexOf( "ceasn:CompetencyFramework" ) > -1 )
                    {
                        entity = ( ( Newtonsoft.Json.Linq.JObject ) token ).ToObject<InputResource>();

                        //InputResource cf = ( InputResource ) JsonConvert.DeserializeObject( token.ToString() );
                        return entity;
                    }
                    else if ( token.ToString().IndexOf( "ceasn:Competency" ) > -1 )
                    {
                        //ignore
                        //var c1 = token.ToString().Replace( "exactMatch", "exactAlignment" );
                        //var c2 = ( ( Newtonsoft.Json.Linq.JObject ) c1 ).ToObject<RA.Models.Json.InputCompetency>();

                    }

					//var itemProperties = token.Children<JProperty>();
					////you could do a foreach or a linq here depending on what you need to do exactly with the value
					//var myElement = itemProperties.FirstOrDefault( x => x.Name == "url" );
					//var myElementValue = myElement.Value; ////This is a JValue type
				}
                else
                {
                    //error
                }
            }
            //no ctid found, so????
            return entity;
        }

    }
}
