using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Services;
using workIT.Utilities;

using BNode = RA.Models.JsonV2.BlankNode;
using ResourceServices = workIT.Services.JobServices;
using APIResourceServices = workIT.Services.API.JobServices;
using InputResource = RA.Models.JsonV2.Job;
using JInput = RA.Models.JsonV2;
using ThisResource = workIT.Models.Common.Job;

namespace Import.Services
{
	public class ImportJob
	{
        readonly int EntityTypeId = CodesManager.ENTITY_TYPE_JOB_PROFILE;
		string thisClassName = "ImportJobs";
        readonly string ResourceType = "Job";
        ImportManager importManager = new ImportManager();
		ThisResource output = new ThisResource();
		ImportServiceHelpers importHelper = new ImportServiceHelpers();

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
			//ResourceServices mgr = new ResourceServices();
			string ctdlType = "";
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
			int newImportId = importHelper.Add( item, EntityTypeId, status.Ctid, importSuccessfull, importError, ref messages );
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
			return Import( payload, envelopeIdentifier, status );

			//return true;
		} //

		public bool Import( string payload, string envelopeIdentifier, SaveStatus status )
		{
			/* checklist
			 * 
				Y		ceasn:abilityEmbodied
				Y		ceasn:comment				
				Y		ceasn:knowledgeEmbodied
				Y		ceasn:skillEmbodied
				Y		ceterms:classification
				Y		ceterms:ctid
				Y		ceterms:description
						codedNotation
						HasTask
				ceterms:hasJob			- to Job
				ceterms:hasSpecialization	- to narrower Job
				ceterms:hasWorkRole		- to WorkRole
				Y		ceterms:identifier
				Y		ceterms:industryType
				ceterms:isSpecializationOf	- to broader occupation
				Y		ceterms:keyword
				Y		ceterms:name
				Y		ceterms:occupationType
				Y		ceterms:sameAs
				Y		ceterms:subjectWebpage
				Y		ceterms:versionIdentifier
			 * 
			 */
			LoggingHelper.DoTrace( 6, "ImportJobs - entered." );
			List<string> messages = new List<string>();
			bool importSuccessfull = false;
			ResourceServices mgr = new ResourceServices();
			//
			InputResource input = new InputResource();
			var mainEntity = new Dictionary<string, object>();
			//
			Dictionary<string, object> dictionary = RegistryServices.JsonToDictionary( payload );
			object graph = dictionary[ "@graph" ];
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
					var bnode = JsonConvert.DeserializeObject<BNode>( bn );
					if ( bnode.BNodeId.IndexOf( "_:" ) > -1 )
					{
                        bnodes.Add( bnode );
					}
					else
					{
						//unexpected
						//Dictionary<string, object> unexpected = RegistryServices.JsonToDictionary( child );
						//object unexpectedType = unexpected[ "@type" ];
						status.AddError( "Unexpected document type: " + bnode.BNodeId );
					}
				}
			}

			MappingHelperV3 helper = new MappingHelperV3( EntityTypeId );
			helper.entityBlankNodes = bnodes;
			helper.CurrentEntityCTID = input.CTID;
			helper.CurrentEntityName = input.Name.ToString();

			status.EnvelopeId = envelopeIdentifier;
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

					//set the rowid now, so that can be referenced as needed
					output.RowId = Guid.NewGuid();
					LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".Import(). Record was NOT found using CTID: '{0}'", input.CTID ) );
				}
				else
				{
					LoggingHelper.DoTrace( 6, string.Format( thisClassName + ".Import(). Found record: '{0}' using CTID: '{1}'", input.Name, input.CTID ) );
				}
				helper.currentBaseObject = output;
				output.CTID = input.CTID;
				output.Name = helper.HandleLanguageMap( input.Name, output, "Name" );
				output.Description = helper.HandleLanguageMap( input.Description, output, "Description" );
				output.SubjectWebpage = input.SubjectWebpage;
				
                //
                output.OfferedByList = helper.MapOrganizationReferenceGuids( $"{ResourceType}.OfferedBy", input.OfferedBy, ref status );
                //add warning?
                if ( output.OfferedByList == null || output.OfferedByList.Count == 0 )
                {
                    //if no offered by, then use document owned by
                    if ( BaseFactory.IsValidCtid( status.DocumentOwnedBy ) )
                    {
                        output.PrimaryOrganization = OrganizationServices.GetSummaryByCtid( status.DocumentOwnedBy );
						if ( output.PrimaryOrganization != null && output.PrimaryOrganization.Id > 0 )
						{
							output.PrimaryAgentUID = output.PrimaryOrganization.RowId;
                            helper.CurrentOwningAgentUid = output.PrimaryAgentUID;
                        }
                    }
                }
                else
                {
                    output.PrimaryAgentUID = output.OfferedByList[0];
                    helper.CurrentOwningAgentUid = output.OfferedByList[0];
                }
				
                
                //TBD handling of referencing third party publisher
                helper.MapOrganizationPublishedBy( output, ref status );
				//warning this gets set to blank if doing a manual import by ctid
				output.CredentialRegistryId = envelopeIdentifier;

                output.AlternateName = helper.HandleLanguageMapList( input.AlternateName, output, "AlternateName" );
				output.InCatalog = input.InCatalog;

				#region KSA
				//		Competency, Job, Occupation, Task, WorkRole
				output.AbilityEmbodied = helper.MapEntityCTIDsToResourceSummary( input.AbilityEmbodied, CodesManager.ENTITY_TYPE_COMPETENCY );
				output.KnowledgeEmbodied = helper.MapEntityCTIDsToResourceSummary( input.KnowledgeEmbodied, CodesManager.ENTITY_TYPE_COMPETENCY );
				output.SkillEmbodied = helper.MapEntityCTIDsToResourceSummary( input.SkillEmbodied, CodesManager.ENTITY_TYPE_COMPETENCY );
				//
				output.PhysicalCapabilityType = helper.MapEntityCTIDsToResourceSummary( input.PhysicalCapabilityType, CodesManager.ENTITY_TYPE_CONCEPT );
				output.PerformanceLevelType = helper.MapEntityCTIDsToResourceSummary( input.PerformanceLevelType, CodesManager.ENTITY_TYPE_CONCEPT );
				output.EnvironmentalHazardType = helper.MapEntityCTIDsToResourceSummary( input.EnvironmentalHazardType, CodesManager.ENTITY_TYPE_CONCEPT );
				output.SensoryCapabilityType = helper.MapEntityCTIDsToResourceSummary( input.SensoryCapabilityType, CodesManager.ENTITY_TYPE_CONCEPT );
				output.Classification = helper.MapEntityCTIDsToResourceSummary( input.Classification, CodesManager.ENTITY_TYPE_CONCEPT );

				//var abilityEmbodiedUIDs = helper.MapEntityReferenceGuids( $"{ResourceType}.AbilityEmbodied", input.AbilityEmbodied, 0, ref status );
				//output.AbilityEmbodied = helper.MapEntityUIDsToResourceSummary( abilityEmbodiedUIDs );

				//output.KnowledgeEmbodied = helper.MapEntityUIDsToResourceSummary( helper.MapEntityReferenceGuids( $"{ResourceType}.KnowledgeEmbodied", input.KnowledgeEmbodied, 0, ref status ) );

				//output.SkillEmbodied = helper.MapEntityUIDsToResourceSummary( helper.MapEntityReferenceGuids( $"{ResourceType}.SkillEmbodied", input.SkillEmbodied, 0, ref status ) );

				#endregion

				//
				output.CodedNotation = input.CodedNotation;
				//
				output.Comment = helper.HandleLanguageMapList( input.Comment, output );
				if ( output.Comment != null && output.Comment.Count() > 0 )
				{
					output.CommentJson = JsonConvert.SerializeObject( output.Comment, MappingHelperV3.GetJsonSettings() );
				}
				//
				if ( input.HasRubric != null && input.HasRubric.Count > 0 )
					output.HasRubricIds = helper.MapEntityReferences( $"{ResourceType}.HasRubric", input.HasRubric, CodesManager.ENTITY_TYPE_RUBRIC, ref status );
				//
				if ( input.HasSupportService != null && input.HasSupportService.Count > 0 )
                    output.HasSupportServiceIds = helper.MapEntityReferences( $"{ResourceType}.HasSupportService", input.HasSupportService, CodesManager.ENTITY_TYPE_SUPPORT_SERVICE, ref status );

                //HasOccupation
                if ( input.HasOccupation != null && input.HasOccupation.Count > 0 )
					output.HasOccupationIds = helper.MapEntityReferences( $"{ResourceType}.HasOccupation", input.HasOccupation, CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE, ref status );

				//
				//HasTask
				if ( input.HasTask != null && input.HasTask.Count > 0 )
					output.HasTaskIds = helper.MapEntityReferences( $"{ResourceType}.HasTask", input.HasTask, CodesManager.ENTITY_TYPE_TASK_PROFILE, ref status );

				//HasWorkRole
				if ( input.HasWorkRole != null && input.HasWorkRole.Count > 0 )
					output.HasWorkRoleIds = helper.MapEntityReferences( $"{ResourceType}.HasWorkRole", input.HasWorkRole, CodesManager.ENTITY_TYPE_WORKROLE_PROFILE, ref status );

				//
				output.Identifier = helper.MapIdentifierValueListInternal( input.Identifier );
				if ( output.Identifier != null && output.Identifier.Count() > 0 )
				{
					output.IdentifierJson = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
				}
				//
				output.Industries = helper.MapCAOListToCAOProfileList( input.IndustryType );
				//
				output.Keyword = helper.HandleLanguageMapList( input.Keyword, output, "Keyword" );
				//
				output.LifeCycleStatusType = helper.MapCAOToEnumermation( input.LifeCycleStatusType );
				//
				output.TargetCompetency = helper.MapCAOListToCAOProfileList( input.TargetCompetency );
				// TransferValue Profile
				if ( input.ProvidesTransferValueFor != null && input.ProvidesTransferValueFor.Count > 0 )
					output.ProvidesTVForIds = helper.MapEntityReferences( $"{ResourceType}.ProvidesTransferValueFor", input.ProvidesTransferValueFor, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, ref status );
				if ( input.ReceivesTransferValueFrom != null && input.ReceivesTransferValueFrom.Count > 0 )
					output.ReceivesTVFromIds = helper.MapEntityReferences( $"{ResourceType}.ProvidesTransferValueFor", input.ReceivesTransferValueFrom, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, ref status );
				//
				output.Occupations = helper.MapCAOListToCAOProfileList( input.OccupationType );

				//
				output.Requires = helper.FormatConditionProfile( input.Requires, ref status );

				//SameAs URI - need to chg this to json - 
				output.SameAs = input.SameAs;
				//
				output.VersionIdentifier = helper.MapIdentifierValueListInternal( input.VersionIdentifier );
				if ( output.VersionIdentifier != null && output.VersionIdentifier.Count() > 0 )
				{
					output.VersionIdentifierJson = JsonConvert.SerializeObject( output.VersionIdentifier, MappingHelperV3.GetJsonSettings() );
				}

				//=== if any messages were encountered treat as warnings for now
				if ( messages.Count > 0 )
					status.SetMessages( messages, true );
				//

				//adding common import pattern
				importSuccessfull = mgr.Import( output, ref status );
                if ( importSuccessfull )
                {

				}
                //
                status.DocumentId = output.Id;
				status.DetailPageUrl = string.Format( "~/job/{0}", output.Id );
				status.DocumentRowId = output.RowId;
				//if record was added to db, add to/or set EntityResolution as resolved
				int ierId = new ImportManager().Import_EntityResolutionAdd( status.ResourceURL,
						ctid,
						EntityTypeId,
						output.RowId,
						output.Id,
						( output.Id > 0 ),
						ref messages,
						output.Id > 0 );
				//just in case - not sure if applicable, as will want to do components if the occupation exists
				if ( status.HasErrors )
				{
					importSuccessfull = false;
					//email an error report, and/or add to activity log?
				}

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError(ex, $"{thisClassName}. Exception encountered in CTID: {output.CTID}");
			}

			return importSuccessfull;
		}


		//currently 
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
