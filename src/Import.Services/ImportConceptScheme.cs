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
using EntityServices = workIT.Services.ConceptSchemeServices;
using ConceptScheme = workIT.Models.Common.ConceptScheme;
using InputConcept = RA.Models.JsonV2.Concept;
using InputEntity = RA.Models.JsonV2.ConceptScheme;
using InputGraph = RA.Models.JsonV2.GraphContainer;
using MC = workIT.Models.Common;
using ThisEntity = workIT.Models.Common.ConceptScheme;

namespace Import.Services
{
	public class ImportConceptSchemes
    {
        int entityTypeId = CodesManager.ENTITY_TYPE_CONCEPT_SCHEME;
        string thisClassName = "ImportConceptSchemes";
        ImportManager importManager = new ImportManager();
        InputGraph input = new InputGraph();
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
			EntityServices mgr = new EntityServices();
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
			EntityServices mgr = new EntityServices();
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
			int newImportId = importHelper.Add( item, CodesManager.ENTITY_TYPE_CONCEPT_SCHEME, status.Ctid, importSuccessfull, importError, ref messages );
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

			string payload = item.DecodedResource.ToString();
            string envelopeIdentifier = item.EnvelopeIdentifier;
            string ctdlType = RegistryServices.GetResourceType( payload );
            string envelopeUrl = RegistryServices.GetEnvelopeUrl( envelopeIdentifier );
            LoggingHelper.DoTrace( 5, "		envelopeUrl: " + envelopeUrl );
            LoggingHelper.WriteLogFile( 1, item.EnvelopeIdentifier + "_ConceptScheme", payload, "", false );

            //just store input for now
            return Import( payload, envelopeIdentifier, status );

            //return true;
        } //

        public bool Import( string payload, string envelopeIdentifier, SaveStatus status )
        {
			LoggingHelper.DoTrace( 7, "ImportConceptSchemes - entered." );
            List<string> messages = new List<string>();
			MappingHelperV3 helper = new MappingHelperV3(10);
			bool importSuccessfull = true;
			var input = new InputEntity();
			var concept = new InputConcept();
			var mainEntity = new Dictionary<string, object>();
			List<InputConcept> concepts = new List<InputConcept>();

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
				//note older frameworks will not be in the priority order
				var main = item.ToString();
				if ( cntr == 1 || main.IndexOf( "skos:ConceptScheme" ) > -1 )
				{
					//HACK
					
					if ( main.IndexOf( "skos:ConceptScheme" ) > -1 )
					{
						input = JsonConvert.DeserializeObject<InputEntity>( main );
					}					
				}
				else
				{

					//should just have concepts, but should check for bnodes
					var child = item.ToString();
					if ( child.IndexOf( "_:" ) > -1 )
					{
						bnodes.Add( JsonConvert.DeserializeObject<BNode>( child ) );
					}
					else if ( child.IndexOf( "skos:Concept" ) > -1 )
					{
						concepts.Add( JsonConvert.DeserializeObject<InputConcept>( child ) );
					}
					else
					{
						//unexpected
					}
				}
			}

			//try
			//{
			//input = JsonConvert.DeserializeObject<InputGraph>( item.DecodedResource.ToString() );
			string ctid = input.CTID;
            status.Ctid = ctid;
            string referencedAtId = input.CtdlId;
            LoggingHelper.DoTrace( 5, "		ctid: " + ctid );
            LoggingHelper.DoTrace( 5, "		@Id: " + input.CtdlId );
			LoggingHelper.DoTrace( 5, "		name: " + input.Name.ToString() );

			string framework = input.Name.ToString();
			var org = new MC.Organization();
			string orgCTID = "";
			string orgName = "";


			if ( status.DoingDownloadOnly )
            	return true;

			//add/updating ConceptScheme
			//var output = new ConceptScheme();
			if ( !DoesEntityExist( input.CTID, ref output ) )
			{
				//set the rowid now, so that can be referenced as needed
				output.RowId = Guid.NewGuid();
				//output.RowId = Guid.NewGuid();
			}
			helper.currentBaseObject = output;
			// 

			output.Name = helper.HandleLanguageMap( input.Name, output, "Name" );
			output.Description = helper.HandleLanguageMap( input.Description, output, "description" );
			output.CTID = input.CTID;
			output.PrimaryOrganizationCTID = orgCTID;
			//
			var publisher = input.Publisher;
			output.PublisherUid = helper.MapOrganizationReferenceGuid( "ConceptScheme.Publisher", input.Publisher, ref status );
			output.Creator = helper.MapOrganizationReferenceGuids( "ConceptScheme.Creator", input.Creator, ref status );
			//20-06-11 - need to get creator, publisher, owner where possible
			//	include an org reference with name, swp, and??
			//should check creator first? Or will publisher be more likely to have an account Ctid?
			if ( output.Creator != null && output.Creator.Count() > 0 )
			{
				//get org or pending stub
				//look up org name
				org = OrganizationManager.Exists( output.Creator[0] );
				output.OwnedBy.Add( output.Creator[ 0 ] );
			}
			else
			{
				if ( output.PublisherUid != Guid.Empty  )
				{
					//get org or pending stub
					//look up org name
					org = OrganizationManager.Exists( output.PublisherUid );
					output.OwnedBy.Add( output.PublisherUid );
				}
			}
			//
			if ( org != null && org.Id > 0 )
			{
				orgName = org.Name;
				output.OrganizationId = org.Id;
				helper.CurrentOwningAgentUid = org.RowId;
			}

			output.CredentialRegistryId = envelopeIdentifier;
			output.HasConcepts = new List<MC.Concept>();
			//?store concepts in string?
			if ( concepts != null && concepts.Count > 0 )
			{
				output.TotalConcepts = concepts.Count();
				foreach ( var item in concepts )
				{
					var c = new MC.Concept()
					{
						PrefLabel = helper.HandleLanguageMap( item.PrefLabel, output, "PrefLabel" ),
						Definition = helper.HandleLanguageMap( item.Definition, output, "Definition" ),
						Notes = helper.HandleLanguageMapList( item.Note, output ),
						CTID = item.CTID
					};
					if ( c.Notes != null && c.Notes.Any() )
						c.Note = c.Notes[ 0 ];
					output.HasConcepts.Add( c );
				}
			}
			//20-07-02 just storing the index ready concepts
			output.ConceptsStore = JsonConvert.SerializeObject( output.HasConcepts, MappingHelperV3.GetJsonSettings() );

			//adding common import pattern

			new ConceptSchemeServices().Import( output, ref status );

			//

			return importSuccessfull;
        }

		//currently use education framework
		public bool DoesEntityExist( string ctid, ref ConceptScheme entity )
		{
			bool exists = false;
			entity = EntityServices.GetByCtid( ctid );
			if ( entity != null && entity.Id > 0 )
				return true;

			return exists;
		}
		private InputEntity GetFramework( object graph )
        {
            //string ctid = "";
            InputEntity entity = new InputEntity();
            Newtonsoft.Json.Linq.JArray jarray = ( Newtonsoft.Json.Linq.JArray ) graph;
            foreach ( var token in jarray )
            {
                if ( token.GetType() == typeof( Newtonsoft.Json.Linq.JObject ) )
                {
                    if ( token.ToString().IndexOf( "ceasn:ConceptScheme" ) > -1 )
                    {
                        entity = ( ( Newtonsoft.Json.Linq.JObject ) token ).ToObject<InputEntity>();

                        //InputEntity cf = ( InputEntity ) JsonConvert.DeserializeObject( token.ToString() );
                        return entity;
                    }
                    else if ( token.ToString().IndexOf( "ceasn:Concept" ) > -1 )
                    {
                        //ignore
                        //var c1 = token.ToString().Replace( "exactMatch", "exactAlignment" );
                        //var c2 = ( ( Newtonsoft.Json.Linq.JObject ) c1 ).ToObject<RA.Models.Json.InputConcept>();

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
