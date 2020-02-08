using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using workIT.Utilities;

using EntityServices = workIT.Services.CostManifestServices;
using InputEntity = RA.Models.Json.CostManifest;

using InputEntityV3 = RA.Models.JsonV2.CostManifest;
using BNodeV3 = RA.Models.JsonV2.BlankNode;
using ThisEntity = workIT.Models.Common.CostManifest;
using workIT.Factories;
using workIT.Models;
using workIT.Models.ProfileModels;

namespace Import.Services
{
    public class ImportCostManifests
    {
		int entityTypeId = CodesManager.ENTITY_TYPE_COST_MANIFEST;
		string thisClassName = "ImportCostManifests";
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
					return Import( input, "", status );
				}
				else
					return false;
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".ImportByResourceId()" );
				return false;
			}
		}

		public bool ImportByPayload( string payload, SaveStatus status )
		{
            if ( ImportServiceHelpers.IsAGraphResource( payload ) )
            {
				//if ( payload.IndexOf( "\"en\":" ) > 0 )
				return ImportV3( payload, "", status );
				//else
				//    return ImportV2( payload, "", status );

			}
			else
            {
                input = JsonConvert.DeserializeObject<InputEntity>( payload );
                return Import( input, "", status );
            }
        }
		#endregion

		public bool ProcessEnvelope( ReadEnvelope item, SaveStatus status )
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
            if ( ImportServiceHelpers.IsAGraphResource( payload ) )
            {
				//if ( payload.IndexOf( "\"en\":" ) > 0 )
				return ImportV3( payload, "", status );
				//else
				//    return ImportV2( payload, "", status );
			}
			else
            {
                LoggingHelper.DoTrace( 5, "		envelopeUrl: " + envelopeUrl );
                LoggingHelper.WriteLogFile( 1, "costManifest_" + item.EnvelopeIdentifier, payload, "", false );
                input = JsonConvert.DeserializeObject<InputEntity>( item.DecodedResource.ToString() );

                return Import( input, envelopeIdentifier, status );
            }
		}
		public bool Import( InputEntity input, string envelopeIdentifier, SaveStatus status )
		{
			List<string> messages = new List<string>();
			bool importSuccessfull = false;
            EntityServices mgr = new EntityServices();
            //try
            //{
            //input = JsonConvert.DeserializeObject<InputEntity>( item.DecodedResource.ToString() );
            string ctid = input.Ctid;
			string referencedAtId = input.CtdlId;
			LoggingHelper.DoTrace( 5, "		name: " + input.Name );
			LoggingHelper.DoTrace( 6, "		url: " + input.CostDetails );
			LoggingHelper.DoTrace( 5, "		ctid: " + input.Ctid );
			LoggingHelper.DoTrace( 5, "		@Id: " + input.CtdlId );
            status.Ctid = ctid;

            if ( status.DoingDownloadOnly )
                return true;

            if ( !DoesEntityExist( input.Ctid, ref output ) )
			{
				output.RowId = Guid.NewGuid();
			}

			//re:messages - currently passed to mapping but no errors are trapped??
			//				- should use SaveStatus and skip import if errors encountered (vs warnings)

			output.Name = input.Name;
			output.Description = input.Description;

			output.CTID = input.Ctid;
			output.CredentialRegistryId = envelopeIdentifier;
			output.CostDetails = input.CostDetails;

			output.OwningAgentUid = MappingHelper.MapOrganizationReferencesGuid( input.CostManifestOf, ref status );

			output.StartDate = MappingHelper.MapDate( input.StartDate, "StartDate", ref status );
			output.EndDate = MappingHelper.MapDate( input.EndDate, "StartDate", ref status );

			output.EstimatedCosts = MappingHelper.FormatCosts( input.EstimatedCost, ref status );

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
						CodesManager.ENTITY_TYPE_COST_MANIFEST,
						output.RowId,
						output.Id,
						false,
						ref messages,
						output.Id > 0 );
   //     }
			//catch ( Exception ex )
			//{

   //             LoggingHelper.LogError( ex, string.Format( "Exception encountered in envelopeId: {0}", envelopeIdentifier ), false, "Finder Import exception" );
			//}

			return importSuccessfull;
		}

        public bool ImportV3( string payload, string envelopeIdentifier, SaveStatus status )
        {
            InputEntityV3 input = new InputEntityV3();
            var bnodes = new List<BNodeV3>();
            var mainEntity = new Dictionary<string, object>();

            //status.AddWarning( "The resource uses @graph and is not handled yet" );

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
                    input = JsonConvert.DeserializeObject<InputEntityV3>( main );
                }
                else
                {
                    var bn = item.ToString();
                    bnodes.Add( JsonConvert.DeserializeObject<BNodeV3>( bn ) );
                }

            }

            List<string> messages = new List<string>();
            bool importSuccessfull = false;
            EntityServices mgr = new EntityServices();
            MappingHelperV3 helper = new MappingHelperV3();
            helper.entityBlankNodes = bnodes;
  
            string ctid = input.Ctid;
            string referencedAtId = input.CtdlId;
            LoggingHelper.DoTrace( 5, "		name: " + input.Name.ToString() );
            LoggingHelper.DoTrace( 6, "		url: " + input.CostDetails );
            LoggingHelper.DoTrace( 5, "		ctid: " + input.Ctid );
            LoggingHelper.DoTrace( 5, "		@Id: " + input.CtdlId );
            status.Ctid = ctid;

            if ( status.DoingDownloadOnly )
                return true;

            if ( !DoesEntityExist( input.Ctid, ref output ) )
            {
                output.RowId = Guid.NewGuid();
            }

            helper.currentBaseObject = output;

            output.Name = helper.HandleLanguageMap( input.Name, output, "Name" );
            output.Description = helper.HandleLanguageMap( input.Description, output, "Description" );

            output.CTID = input.Ctid;
            output.CredentialRegistryId = envelopeIdentifier;
            output.CostDetails = input.CostDetails;

            output.OwningAgentUid = helper.MapOrganizationReferencesGuid( "CostManifest.OwningAgentUid", input.CostManifestOf, ref status );

            output.StartDate = helper.MapDate( input.StartDate, "StartDate", ref status );
            output.EndDate = helper.MapDate( input.EndDate, "StartDate", ref status );

            output.EstimatedCosts = helper.FormatCosts( input.EstimatedCost, ref status );

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
                        CodesManager.ENTITY_TYPE_COST_MANIFEST,
                        output.RowId,
                        output.Id,
                        false,
                        ref messages,
                        output.Id > 0 );
 
            return importSuccessfull;
        }
        public bool DoesEntityExist( string ctid, ref ThisEntity entity )
        {
            bool exists = false;
            entity = EntityServices.GetByCtid( ctid );
            if ( entity != null && entity.Id > 0 )
                return true;

            return exists;
        }
    }

	
}
