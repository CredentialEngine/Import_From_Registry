﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using EntityServices = workIT.Services.DataSetProfileServices;
using InputEntity = RA.Models.JsonV2.QData.DataSetProfile;
using ThisEntity = workIT.Models.QData.DataSetProfile;
using BNode = RA.Models.JsonV2.BlankNode;

using workIT.Utilities;
using workIT.Factories;
using workIT.Models;
using MC = workIT.Models.Common;
using Import.Services;
using workIT.Services;
using workIT.Data.Accounts;
using workIT.Models.Node;

namespace Import.Services
{
	public class ImportDataSetProfile
	{
		int entityTypeId = CodesManager.ENTITY_TYPE_DATASET_PROFILE;
		string thisClassName = "ImportDataSetProfile";
		ImportManager importManager = new ImportManager();
		ThisEntity output = new ThisEntity();
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
			//EntityServices mgr = new EntityServices();
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
			//EntityServices mgr = new EntityServices();
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
			int newImportId = importHelper.Add( item, entityTypeId, status.Ctid, importSuccessfull, importError, ref messages );
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
			//LoggingHelper.WriteLogFile( UtilityManager.GetAppKeyValue( "logFileTraceLevel", 5 ), item.EnvelopeCetermsCtid + "_TVP", payload, "", false );

			//just store input for now
			return Import( payload, envelopeIdentifier, status );

			//return true;
		} //

		public bool Import( string payload, string envelopeIdentifier, SaveStatus status )
		{

			List<string> messages = new List<string>();
			bool importSuccessfull = false;
			EntityServices mgr = new EntityServices();
			//
			InputEntity input = new InputEntity();
			var bnodes = new List<BNode>();
			var mainEntity = new Dictionary<string, object>();
			//
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
					input = JsonConvert.DeserializeObject<InputEntity>( main );
				}
				else
				{
					var bn = item.ToString();
					//20-07-02 need to handle the enhanced bnodes
					bnodes.Add( JsonConvert.DeserializeObject<BNode>( bn ) );
				}

			}

			///============= process =============================
			MappingHelperV3 helper = new MappingHelperV3( 3 );
			helper.entityBlankNodes = bnodes;
			helper.CurrentEntityCTID = input.CTID;
			helper.CurrentEntityName = input.Name.ToString();

			string ctid = input.CTID;
			status.Ctid = ctid;
			string referencedAtId = input.CtdlId;
			LoggingHelper.DoTrace( 5, "		ctid: " + ctid );
			LoggingHelper.DoTrace( 5, "		@Id: " + input.CtdlId );
			LoggingHelper.DoTrace( 5, "		name: " + input.Name.ToString() );

			if ( status.DoingDownloadOnly )
				return true;

			//add/updating DataSetProfile
			if ( !DoesEntityExist( input.CTID, ref output, ref status ) )
			{
				//set the rowid now, so that can be referenced as needed
				output.RowId = Guid.NewGuid();
			}
			helper.currentBaseObject = output;

			output.Name = helper.HandleLanguageMap( input.Name, output, "Name" );
			output.Description = helper.HandleLanguageMap( input.Description, output, "Description" );
			output.CTID = input.CTID;
			//TBD handling of referencing third party publisher
			if ( !string.IsNullOrWhiteSpace( status.DocumentPublishedBy ) )
			{
				//output.PublishedByOrganizationCTID = status.DocumentPublishedBy;
				var porg = OrganizationManager.GetSummaryByCtid( status.DocumentPublishedBy );
				if ( porg != null && porg.Id > 0 )
				{
					//TODO - store this in a json blob??????????
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
					//TBD
					//if ( output.DataProvider != null && output.DataProvider.Any() )
					//{
					//	var publishedByList = output.OrganizationRole.Where( s => s.RoleTypeId == 30 ).ToList();
					//	if ( publishedByList != null && publishedByList.Any() )
					//	{
					//		var pby = publishedByList[ 0 ].ActingAgentUid;
					//		output.PublishedBy = new List<Guid>() { publishedByList[ 0 ].ActingAgentUid };
					//	}
					//}
				}
			}
			output.Source = input.Source;
			//**** TBD DataProvider, or is the owner based on envelope?
			//output.OwnedBy = helper.MapOrganizationReferenceGuids( "DataSetProfile.OwnedBy", input.DataProvider, ref status );
			//if ( output.OwnedBy != null && output.OwnedBy.Count > 0 )
			//{
			//	output.OwningAgentUid = output.OwnedBy[ 0 ];
			//	helper.CurrentOwningAgentUid = output.OwnedBy[ 0 ];
			//}

			//



			importSuccessfull = new DataSetProfileServices().Import( output, ref status );
			//
			status.DocumentId = output.Id;
			status.DetailPageUrl = string.Format( "~/DataSetProfile/{0}", output.Id );
			status.DocumentRowId = output.RowId;

			//just in case
			if ( status.HasErrors )
				importSuccessfull = false;

			//if record was added to db, add to/or set EntityResolution as resolved
			int ierId = new ImportManager().Import_EntityResolutionAdd( referencedAtId,
						ctid, entityTypeId,
						output.RowId,
						output.Id,
						false,
						ref messages,
						output.Id > 0 );
			//

			return importSuccessfull;
		}

		//currently 
		public bool DoesEntityExist( string ctid, ref ThisEntity entity, ref SaveStatus status )
		{
			bool exists = false;
			entity = EntityServices.HandlingExistingEntity( ctid, ref status );
			if ( entity != null && entity.Id > 0 )
			{
				//we know for this type, there will entity.learningopp, entity.assessment and entity.credential relationships, and quick likely blank nodes.
				return true;
			}

			return exists;
		}

	}
}
