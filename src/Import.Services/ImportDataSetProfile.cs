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
using ResourceServices = workIT.Services.DataSetProfileServices;
using APIResourceServices = workIT.Services.API.OutcomeDataServices;
using InputResource = RA.Models.JsonV2.QData.DataSetProfile;
using ThisResource = workIT.Models.QData.DataSetProfile;

namespace Import.Services
{
	public class ImportDataSetProfile
	{
		int entityTypeId = CodesManager.ENTITY_TYPE_DATASET_PROFILE;
		string thisClassName = "ImportDataSetProfile";
		ImportManager importManager = new ImportManager();
		ThisResource output = new ThisResource();
		ImportServiceHelpers importHelper = new ImportServiceHelpers();


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
			//LoggingHelper.WriteLogFile( UtilityManager.GetAppKeyValue( "logFileTraceLevel", 5 ), item.EnvelopeCetermsCtid + "_TVP", payload, "", false );

			//just store input for now
			return Import( payload, status );

			//return true;
		} //

		public bool Import( string payload, SaveStatus status )
		{

			List<string> messages = new List<string>();
			bool importSuccessfull = false;
			ResourceServices mgr = new ResourceServices();
			//
			InputResource input = new InputResource();
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
					input = JsonConvert.DeserializeObject<InputResource>( main );
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
			helper.CurrentEntityName = input.Name!= null ? input.Name.ToString() : "DataSetProfile: " + input.CTID;

			string ctid = input.CTID;
			status.Ctid = ctid;
			status.ResourceURL = input.CtdlId;
			LoggingHelper.DoTrace( 5, "		ctid: " + ctid );
			LoggingHelper.DoTrace( 5, "		@Id: " + input.CtdlId );
			LoggingHelper.DoTrace( 5, "		name: " + helper.CurrentEntityName );

			if ( status.DoingDownloadOnly )
				return true;
			try
			{
				//add/updating DataSetProfile
				//hmm. A little different for direct import, maybe need a mapping version that has dsp as input
				ThisResource existing = new ThisResource();
				if ( !DoesEntityExist( input.CTID, ref existing, ref status ) )
				{
					//set the rowid now, so that can be referenced as needed
					existing.RowId = Guid.NewGuid();
				}
				output = helper.FormatDataSetProfile( input.CTID, input, ref status );
				//***
				//output.DataProviderUID = helper.MapOrganizationReferenceGuid( "DataSetProfile.DataProvider", input.DataProvider, ref status );
				//probably too late for use of this
				if ( output.DataProviderUID != null && ServiceHelper.IsValidGuid( output.DataProviderUID ) )
				{
					helper.CurrentOwningAgentUid = output.DataProviderUID;
					var org = OrganizationManager.GetBasics( output.DataProviderUID, false );
					if ( org != null && org.Id > 0 )
					{
						output.DataProviderId = org.Id;
					}
				}

				//TODO - check if any About have not been downloaded
				if ( output.AboutUids != null && output.AboutUids.Any() )
				{
					output.About = new List<workIT.Models.API.Outline>();
					foreach ( var item in output.AboutUids )
					{
						//we don't know the type, so look up the Entity, or Entity.Cache in order to get state
						var ec = EntityManager.EntityCacheGetByGuid( item );
						if(ec != null && ec.Id > 0 )
						{
							if (ec.EntityStateId == 1)
							{
								//don't have much info
								var entity = new Import_PendingRequest()
								{
									EntityCtid = ec.CTID,
									PublishingEntityType = ec.EntityType,
								};
								importManager.Import_PendingRequestAdd( entity, ref status );
							} else if ( ec.EntityStateId == 3 )
							{
								output.About.Add( new workIT.Models.API.Outline()
								{
									Meta_Id = ec.Id,
									Description = ec.Description,
									Label = ec.Name
								} );
							}
						}
					}
				}
				helper.currentBaseObject = output;
				output.Id = existing.Id;
				output.RowId = existing.RowId;

				//TBD handling of referencing third party publisher
				helper.MapOrganizationPublishedBy( output, ref status );
				/*
				if ( !string.IsNullOrWhiteSpace( status.DocumentPublishedBy ) )
				{
					//output.PublishedByOrganizationCTID = status.DocumentPublishedBy;
					var porg = OrganizationManager.GetSummaryByCtid( status.DocumentPublishedBy );
					if ( porg != null && porg.Id > 0 )
					{
						//TODO - store this in a json blob??????????
						output.PublishedByOrganizationId = porg.Id;
						//this will result in being added to Entity.AgentRelationship
						output.PublishedBy = new List<Guid>() { porg.RowId };
					}
					else
					{
						//if publisher not imported yet, all publishee stuff will be orphaned
						var entityUid = Guid.NewGuid();
						var statusMsg = "";
						var resPos = status.ResourceURL.IndexOf( "/resources/" );
						var swp = status.ResourceURL.Substring( 0, ( resPos + "/resources/".Length ) ) + status.DocumentPublishedBy;
						int orgId = new OrganizationManager().AddPendingRecord( entityUid, status.DocumentPublishedBy, swp, ref status );
						output.PublishedByOrganizationId = porg.Id;
						output.PublishedBy = new List<Guid>() { entityUid };
					}
				}
				else
				{
					//may need a check for existing published by to ensure not lost
					if ( output.Id > 0 )
					{
						//if ( ef.OrganizationRole != null && ef.OrganizationRole.Any() )
						//{
						//	var publishedByList = ef.OrganizationRole.Where( s => s.RoleTypeId == 30 ).ToList();
						//	if ( publishedByList != null && publishedByList.Any() )
						//	{
						//		var pby = publishedByList[ 0 ].ActingAgentUid;
						//		ef.PublishedBy = new List<Guid>() { publishedByList[ 0 ].ActingAgentUid };
						//	}
						//}
					}
				}

				*/

				//
				importSuccessfull = new DataSetProfileServices().Import( output, ref status );
					
				//24-03-25 - use the generic process for blank nodes encountered during import
				new ProfileServices().IndexPrepForReferenceResource( helper.ResourcesToIndex, ref status );
			
				//
				status.DocumentId = output.Id;
				status.DetailPageUrl = string.Format( "~/DataSetProfile/{0}", output.Id );
				status.DocumentRowId = output.RowId;

				//just in case
				if ( status.HasErrors )
					importSuccessfull = false;

				//if record was added to db, add to/or set EntityResolution as resolved
				int ierId = new ImportManager().Import_EntityResolutionAdd( status.ResourceURL,
							ctid, entityTypeId,
							output.RowId,
							output.Id,
							false,
							ref messages,
							output.Id > 0 );
				//
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Import", string.Format( "Exception encountered for CTID: {0}", ctid ) );
			}
			finally
			{

			}
			return importSuccessfull;
		}

		//currently 
		public bool DoesEntityExist( string ctid, ref ThisResource entity, ref SaveStatus status )
		{
			bool exists = false;
			//currently only looks for the full resource, and not EntityStateId >=1
			entity = ResourceServices.HandlingExistingEntity( ctid, ref status );
			if ( entity != null && entity.Id > 0 )
			{
				//we know for this type, there will entity.learningopp, entity.assessment and entity.credential relationships, and quick likely blank nodes.
				return true;
			}

			return exists;
		}

	}
}
