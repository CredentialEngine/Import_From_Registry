using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using workIT.Utilities;

using EntityServices = workIT.Services.ConditionManifestServices;
using InputEntity = RA.Models.Json.ConditionManifest;
using ThisEntity = workIT.Models.Common.ConditionManifest;
using workIT.Factories;
using workIT.Models;

namespace Import.Services
{
    public class ImportConditionManifests
    {
		int entityTypeId = CodesManager.ENTITY_TYPE_CONDITION_MANIFEST;
		string thisClassName = "ImportConditionManifests";
		ImportManager importManager = new ImportManager();
		InputEntity input = new InputEntity();
		ThisEntity output = new ThisEntity();

		#region custom imports
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
		public bool ImportByResourceId( string ctid, SaveStatus status )
		{
			//this is currently specific, assumes envelop contains a credential
			//can use the hack fo GetResourceType to determine the type, and then call the appropriate import method
			string statusMessage = "";
			EntityServices mgr = new EntityServices();
			string ctdlType = "";
			try
			{
				string payload = RegistryServices.GetResourceByCtid( ctid, ref ctdlType, ref statusMessage );

				if ( !string.IsNullOrWhiteSpace( payload ) )
				{
					input = JsonConvert.DeserializeObject<InputEntity>( payload.ToString() );
					//ctdlType = RegistryServices.GetResourceType( payload );
					return Import( mgr, input, "", status );
				}
				else
					return false;
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".ImportByResourceId()" );
				status.AddError( ex.Message );
				if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
				{
					status.AddWarning( "The referenced registry document is using an old schema. Please republish it with the latest schema!" );
				}
				return false;
			}
		}
		public bool ImportByPayload( string payload, SaveStatus status )
		{
			EntityServices mgr = new EntityServices();
			input = JsonConvert.DeserializeObject<InputEntity>( payload );

			return Import( mgr, input, "", status );
		}
		#endregion

		public bool ProcessEnvelope( ReadEnvelope item, SaveStatus status )
		{
			EntityServices mgr = new EntityServices();
			return ProcessEnvelope( mgr, item, status );
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
			LoggingHelper.WriteLogFile( 1, item.EnvelopeIdentifier + "_conditionManifest", payload, "", false );
			input = JsonConvert.DeserializeObject<InputEntity>( item.DecodedResource.ToString() );

			return Import( mgr, input, envelopeIdentifier, status );
		}
		public bool Import( EntityServices mgr, InputEntity input, string envelopeIdentifier, SaveStatus status )
		{
			List<string> messages = new List<string>();
			bool importSuccessfull = false;

			//try
			//{
			//input = JsonConvert.DeserializeObject<InputEntity>( item.DecodedResource.ToString() );
			string ctid = input.Ctid;
			string referencedAtId = input.CtdlId;
			LoggingHelper.DoTrace( 5, "		name: " + input.Name );
			LoggingHelper.DoTrace( 6, "		url: " + input.SubjectWebpage );
			LoggingHelper.DoTrace( 5, "		ctid: " + input.Ctid );
			LoggingHelper.DoTrace( 5, "		@Id: " + input.CtdlId );
            status.Ctid = ctid;

            if ( status.DoingDownloadOnly )
                return true;

            if ( !DoesEntityExist( input, ref output ) )
			{
				output.RowId = Guid.NewGuid();
			}

			//re:messages - currently passed to mapping but no errors are trapped??
			//				- should use SaveStatus and skip import if errors encountered (vs warnings)

			output.Name = input.Name;
			output.Description = input.Description;
			output.CTID = input.Ctid;
			output.CredentialRegistryId = envelopeIdentifier;
			output.SubjectWebpage = input.SubjectWebpage;

			output.OwningAgentUid = MappingHelper.MapOrganizationReferencesGuid( input.ConditionManifestOf, ref status );

			output.Requires = MappingHelper.FormatConditionProfile( input.Requires, ref status );
			output.Recommends = MappingHelper.FormatConditionProfile( input.Recommends, ref status );
			output.EntryCondition = MappingHelper.FormatConditionProfile( input.EntryConditions, ref status );
			output.Corequisite = MappingHelper.FormatConditionProfile( input.Corequisite, ref status );
			output.Renewal = MappingHelper.FormatConditionProfile( input.Renewal, ref status );

			status.DocumentId = output.Id;
			status.DocumentRowId = output.RowId;

			//=== if any messages were encountered treat as warnings for now
			if ( messages.Count > 0 )
				status.SetMessages( messages, true );

			importSuccessfull = mgr.Import( output, ref status );
			//just in case
			if ( status.HasErrors )
				importSuccessfull = false;

			//if record was added to db, add to/or set EntityResolution as resolved
			int ierId = new ImportManager().Import_EntityResolutionAdd( referencedAtId,
						ctid,
						CodesManager.ENTITY_TYPE_CONDITION_MANIFEST,
						output.RowId,
						output.Id,
						false,
						ref messages,
						output.Id > 0 );

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
