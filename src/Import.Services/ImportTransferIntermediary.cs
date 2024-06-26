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
using ResourceServices = workIT.Services.TransferIntermediaryServices;
using InputResource = RA.Models.JsonV2.TransferIntermediary;
using ThisResource = workIT.Models.Common.TransferIntermediary;

using InputTVP = RA.Models.JsonV2.TransferValueProfile;
using TVPEntity = workIT.Models.Common.TransferValueProfile;
namespace Import.Services
{
	public class ImportTransferIntermediary
	{
		int entityTypeId = CodesManager.ENTITY_TYPE_TRANSFER_INTERMEDIARY;
		string thisClassName = "ImportTransferIntermediary";
		ImportManager importManager = new ImportManager();
		ThisResource output = new ThisResource();
		ImportServiceHelpers importHelper = new ImportServiceHelpers();
		#region Common Helper Methods
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

		} //
		#endregion
		public bool Import( string payload, SaveStatus status )
		{
			List<string> messages = new List<string>();
			bool importSuccessfull = false;
			ResourceServices mgr = new ResourceServices();
			//
			InputResource input = new InputResource();
			var bnodes = new List<BNode>();
			var tvProfiles = new List<InputTVP>();
			var mainEntity = new Dictionary<string, object>();
			//
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
					var child = item.ToString();
					RegistryObject ro = new RegistryObject( child );
					if ( ro.CtdlId.IndexOf( "_:" ) > -1 )
					{
						//	The only possible bnode will be for the ownedBy
						//However, the ownedByCTID is required for publishing, and on the envelope
						//SO don't bother
						var bn = item.ToString();
						//20-07-02 need to handle the enhanced bnodes
						bnodes.Add( JsonConvert.DeserializeObject<BNode>( bn ) );
					}
					else
                    {
						//any blank nodes will likely be from any imbedded TVPs
						//22-02-12 mp - actually there should not be any TVPs in the graphs?
					}
				}

			}

			///============= process =============================
			MappingHelperV3 helper = new MappingHelperV3( 3 );
			helper.entityBlankNodes = bnodes;
			helper.CurrentEntityCTID = input.CTID;
			helper.CurrentEntityName = input.Name != null ? input.Name.ToString() : "DataSetProfile: " + input.CTID;

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
				//add/updating TransferIntermediary
				if ( !DoesEntityExist( input.CTID, ref output, ref status ) )
				{
					//set the rowid now, so that can be referenced as needed
					output.RowId = Guid.NewGuid();
				}
				helper.currentBaseObject = output;
				output.CTID = input.CTID;
				output.Name = helper.HandleLanguageMap( input.Name, output, "Name" );
				output.Description = helper.HandleLanguageMap( input.Description, output, "Description" );
				
				//TBD handling of referencing third party publisher
				helper.MapOrganizationPublishedBy( output, ref status );

				//output.CredentialRegistryId = envelopeIdentifier;
				output.SubjectWebpage = input.SubjectWebpage;
				//
				output.OwnedBy = helper.MapOrganizationReferenceGuids( "TransferIntermediary.OwnedBy", input.OwnedBy, ref status );
				if ( output.OwnedBy != null && output.OwnedBy.Count > 0 )
				{
					output.PrimaryAgentUID = output.OwnedBy[0];
					helper.CurrentOwningAgentUid = output.OwnedBy[0];
				}
				output.AlternateNames = helper.MapToTextValueProfile( input.AlternateName, output, "AlternateName" );

				output.CodedNotation = input.CodedNotation;
				//CreditValue
				output.CreditValue = helper.HandleValueProfileList( input.CreditValue, "TransferIntermediary.CreditValue" );
				if ( output.CreditValue?.Count() > 0 )
				{
					output.CreditValueJson = JsonConvert.SerializeObject( output.CreditValue, MappingHelperV3.GetJsonSettings() );
				}
				//IntermediaryFor
				output.IntermediaryForJson = "";
				output.IntermediaryFor = new List<workIT.Models.Common.TopLevelObject>();
				//this needs to provide the entitytypeid = 26
				output.IntermediaryForImport = helper.MapEntityReferenceGuids( "TransferIntermediary.IntermediaryFor", input.IntermediaryFor, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, ref status );

				if ( output.IntermediaryForImport?.Count() > 0 )
				{
					//TransferValueForImport is a list of guids which could reference a blank node
					foreach ( var item in output.IntermediaryForImport )
					{
						//If using the json, may not need the TransferIntermediary.TransferValue table?
						//may need in order to link to from TVP to check membership
						var tlo = ProfileServices.GetEntityAsTopLevelObject( item );
						if ( tlo != null && tlo.Id > 0 )
							output.IntermediaryFor.Add( tlo );
						else
						{
							//log error
						}
					}
					//store all object. This would be OK for numbers up to ?50
					output.IntermediaryForJson = JsonConvert.SerializeObject( output.IntermediaryFor, MappingHelperV3.GetJsonSettings() );
				}
				//Requires
				output.Requires = helper.FormatConditionProfile( input.Requires, ref status );

				//Subject - really need to get away from the textValueProfile
				//	just store the list on the base table
				output.SubjectTVP = helper.MapCAOListToTextValueProfile( input.Subject, CodesManager.PROPERTY_CATEGORY_SUBJECT );
                output.Subject = helper.MapCAOListToList( input.Subject );
                //
                //
                importSuccessfull = new TransferIntermediaryServices().Import( output, ref status );
				//
				status.DocumentId = output.Id;
				status.DetailPageUrl = string.Format( "~/TransferIntermediary/{0}", output.Id );
				status.DocumentRowId = output.RowId;

				//just in case
				if ( status.HasErrors )
					importSuccessfull = false;

				//if record was added to db, add to/or set EntityResolution as resolved
				int ierId = new ImportManager().Import_EntityResolutionAdd( status.ResourceURL,
							ctid, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE,
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
				//var totalDuration = DateTime.Now.Subtract( started );
				//if ( totalDuration.TotalSeconds > 9 && ( totalDuration.TotalSeconds - saveDuration.TotalSeconds > 3 ) )
				//	LoggingHelper.DoTrace( 5, string.Format( "         WARNING Total Duration: {0:N2} seconds ", totalDuration.TotalSeconds ) );
			}
            return importSuccessfull;
		}

		//currently 
		public bool DoesEntityExist( string ctid, ref ThisResource entity, ref SaveStatus status )
		{
			bool exists = false;
			entity = ResourceServices.HandlingExistingEntity( ctid, ref status );
			if ( entity != null && entity.Id > 0 )
			{
				return true;
			}

			return exists;
		}

	}
}
