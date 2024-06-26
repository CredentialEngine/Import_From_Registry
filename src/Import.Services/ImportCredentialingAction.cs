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
using ResourceServices = workIT.Services.CredentialingActionServices;
using APIResourceServices = workIT.Services.API.CredentialingActionServices;
using InputResource = RA.Models.JsonV2.CredentialingAction;
using JInput = RA.Models.JsonV2;
using ThisResource = workIT.Models.Common.CredentialingAction;

namespace Import.Services
{
	public class ImportCredentialingAction
	{
		readonly int EntityTypeId = CodesManager.ENTITY_TYPE_CREDENTIALING_ACTION;
		string thisClassName = "ImportCredentialingAction";
		readonly string ResourceType = "CredentialingAction";
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

			//
			return Import( payload, envelopeIdentifier, status );

			//return true;
		} //

		public bool Import( string payload, string envelopeIdentifier, SaveStatus status )
		{
			/* checklist
			 * 

			 * 
			 */
			LoggingHelper.DoTrace( 6, $"{thisClassName}.Import - entered." );
			List<string> messages = new List<string>();
			bool importSuccessfull = false;
			ResourceServices mgr = new ResourceServices();
			//
			InputResource input = new InputResource();
			var mainEntity = new Dictionary<string, object>();
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
				//name may be options
				LoggingHelper.DoTrace( 5, "		name: " + input.Name.ToString() );
				LoggingHelper.DoTrace( 5, "		ctid: " + input.CTID );
				LoggingHelper.DoTrace( 6, "		@Id: " + input.CtdlId );
				status.Ctid = ctid;
				var primaryOrg = new Organization();
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
				
				output.Type = input.Type;
				//23-11-10 - change to use Codes.CredentialingActionType
				output.ActionTypeId = CredentialingActionManager.GetActionTypeId( input.Type );

				//
				output.ActingAgentList = helper.MapOrganizationReferenceGuids( $"{ResourceType}.ActingAgent", input.ActingAgent, ref status );
				//should be required
				if ( output.ActingAgentList == null && output.ActingAgentList.Count == 0 )
				{
					//if no offered by, then use document owned by
					if ( BaseFactory.IsValidCtid( status.DocumentOwnedBy ) )
					{
						output.PrimaryOrganization = OrganizationServices.GetSummaryByCtid( status.DocumentOwnedBy );
						if ( output.PrimaryOrganization != null && output.PrimaryOrganization.Id > 0 )
							output.PrimaryAgentUID = output.PrimaryOrganization.RowId;
					}
				}
				else
				{
					output.PrimaryAgentUID = output.ActingAgentList[0];
					helper.CurrentOwningAgentUid = output.ActingAgentList[0];
					primaryOrg = OrganizationManager.GetForSummary( output.PrimaryAgentUID );
				}
				if ( string.IsNullOrWhiteSpace( output.Name ) )
				{
					output.Name = output.Type;
					if ( primaryOrg != null && primaryOrg.Id > 0 )
					{
						output.Name += " BY " + primaryOrg.Name;
					}
				}
				output.ActionStatusType = helper.MapCAOToEnumermation( input.ActionStatusType );
				if ( output.ActionStatusType != null && output.ActionStatusType.HasItems() )
				{
					output.ActionStatusTypeId = CodesManager.GetPropertyIdBySchema( CodesManager.PROPERTY_CATEGORY_ACTION_STATUS_TYPE, output.ActionStatusType.Items[0].SchemaName );
				}

				//TBD handling of referencing third party publisher
				helper.MapOrganizationPublishedBy( output, ref status );
				//warning this gets set to blank if doing a manual import by ctid
				output.CredentialRegistryId = envelopeIdentifier;
				output.StartDate = input.StartDate; 
				output.EndDate = input.EndDate;

				//it could be a bnode I suppose
				output.ObjectUid = helper.MapEntityReferenceGuid( "CredentialingAction.Object", input.Object, 0, ref status );

				output.EvidenceOfAction = input.EvidenceOfAction;
				//this should be single
				output.InstrumentIds = helper.MapEntityReferences( input.Instrument, CodesManager.ENTITY_TYPE_CREDENTIAL, CodesManager.ENTITY_TYPE_CREDENTIALING_ACTION, ref status );
				//participant
				output.ParticipantIds = helper.MapEntityReferences( input.Participant, CodesManager.ENTITY_TYPE_PLAIN_ORGANIZATION, CodesManager.ENTITY_TYPE_CREDENTIALING_ACTION, ref status );

				output.ParticipantList = helper.MapOrganizationReferenceGuids( $"{ResourceType}.Participant", input.Participant, ref status );
				//OR
				output.ParticipantIds = helper.MapOrganizationReferenceToInteger( $"{ResourceType}.Participant", input.Participant, ref status );
				//
				//OR
				//output.ParticipantIds = helper.MapOrganizationReferenceToInteger( $"{ResourceType}.Participant", input.Participant, ref status );
				//
				//resulting award - ceterms:CredentialAssertion not handled

				//jurisdiction for workForceDemand only
				output.Jurisdiction = helper.MapToJurisdiction( input.Jurisdiction, ref status );
				output.Image = input.Image;

				//=== if any messages were encountered treat as warnings for now
				if ( messages.Count > 0 )
					status.SetMessages( messages, true );
				//

				//adding common import pattern
				importSuccessfull = mgr.Import( output, ref status );
				//24-03-25 - use the generic process for blank nodes encountered during import
				new ProfileServices().IndexPrepForReferenceResource( helper.ResourcesToIndex, ref status );


				//
				status.DocumentId = output.Id;
				status.DetailPageUrl = string.Format( "~/credentialingAction/{0}", output.Id );
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
				LoggingHelper.LogError( ex, string.Format( "Exception encountered in CTID: {0}", output.CTID ) );
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
