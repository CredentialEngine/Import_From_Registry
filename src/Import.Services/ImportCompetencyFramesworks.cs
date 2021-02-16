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
using EntityServices = workIT.Services.CompetencyFrameworkServices;
using Framework = workIT.Models.ProfileModels.CompetencyFramework;
using InputCompetency = RA.Models.JsonV2.Competency;
using InputEntity = RA.Models.JsonV2.CompetencyFramework;
using InputGraph = RA.Models.JsonV2.CompetencyFrameworksGraph;
using MC = workIT.Models.Common;
using ThisEntity = workIT.Models.Common.CompetencyFramework;

namespace Import.Services
{
	public class ImportCompetencyFramesworks
    {
        int entityTypeId = CodesManager.ENTITY_TYPE_CASS_COMPETENCY_FRAMEWORK;
        string thisClassName = "ImportCompetencyFramesworks";
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
			LoggingHelper.WriteLogFile( UtilityManager.GetAppKeyValue( "logFileTraceLevel", 5 ), item.EnvelopeCetermsCtid + "_competencyFrameswork", payload, "", false );
            //input = JsonConvert.DeserializeObject<InputGraph>( item.DecodedResource.ToString() );

            //InputEntity framework = GetFramework( input.Graph );
            //LoggingHelper.DoTrace( 5, "		framework name: " + framework.name.ToString() );

            //just store input for now
            return Import( payload, envelopeIdentifier, status );

            //return true;
        } //

        public bool Import( string payload, string envelopeIdentifier, SaveStatus status )
        {
			LoggingHelper.DoTrace( 7, "ImportCompetencyFramesworks - entered." );
            List<string> messages = new List<string>();
			MappingHelperV3 helper = new MappingHelperV3(10);
			bool importSuccessfull = true;
			InputEntity input = new InputEntity();
			InputCompetency comp = new InputCompetency();
			var mainEntity = new Dictionary<string, object>();
			List<InputCompetency> competencies = new List<InputCompetency>();

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
				if ( cntr == 1 || main.IndexOf( "ceasn:CompetencyFramework" ) > -1 )
				{
					//HACK
					
					if ( main.IndexOf( "ceasn:CompetencyFramework" ) > -1 )
					{
						input = JsonConvert.DeserializeObject<InputEntity>( main );
					}					
				}
				else
				{
					//Error converting value "https://credentialengineregistry.org/resources/ce-949fcaba-45ed-44d9-88bf-43677277eb84" to type 'System.Collections.Generic.List`1[System.String]'. Path 'ceasn:isPartOf', line 11, position 108.
					//not set up to handle issues
					//comp = JsonConvert.DeserializeObject<InputCompetency>( item.ToString() );
					//competencies.Add( comp );

					//should just have competencies, but should check for bnodes
					var child = item.ToString();
					if ( child.IndexOf( "_:" ) > -1 )
					{
						bnodes.Add( JsonConvert.DeserializeObject<BNode>( child ) );
						//ceasn:Competency
					}
					else if ( child.IndexOf( "ceasn:Competency" ) > -1 )
					{
						competencies.Add( JsonConvert.DeserializeObject<InputCompetency>( child ) );
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
			LoggingHelper.DoTrace( 5, "		name: " + input.name.ToString() );

			string framework = input.name.ToString();
			var org = new MC.Organization();
			string orgCTID = "";
			string orgName = "";
			List<string> publisher = input.publisher;
			//20-06-11 - need to get creator, publisher, owner where possible
			//	include an org reference with name, swp, and??
			//should check creator first? Or will publisher be more likely to have an account Ctid?
			if ( publisher != null && publisher.Count() > 0 )
			{
				orgCTID = ResolutionServices.ExtractCtid( publisher[ 0 ] );
				//look up org name
				org = OrganizationManager.GetByCtid( orgCTID );
			}
			else
			{
				//try creator
				List<string> creator = input.creator;
				if ( creator != null && creator.Count() > 0 )
				{
					orgCTID = ResolutionServices.ExtractCtid( creator[ 0 ] );
					//look up org name
					org = OrganizationManager.GetByCtid( orgCTID );
				}
			}


			if ( status.DoingDownloadOnly )
            	return true;

			//add/updating CompetencyFramework
			Framework ef = new Framework();
			if ( !DoesEntityExist( input.CTID, ref ef ) )
			{
				//set the rowid now, so that can be referenced as needed
				output.RowId = Guid.NewGuid();
				ef.RowId = Guid.NewGuid();
				LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".Import(). Record was NOT found using CTID: '{0}'", input.CTID ) );
			}
			else
			{
				LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".Import(). Found record: '{0}' using CTID: '{1}'", input.name, input.CTID ) );
			}

			helper.currentBaseObject = ef;
			ef.ExistsInRegistry = true;
			//?store competencies in string?
			if (competencies != null && competencies.Count > 0)
			{
				ef.TotalCompetencies = competencies.Count();
				foreach(var c in competencies )
				{
					ef.Competencies.Add( new workIT.Models.Elastic.IndexCompetency()
					{
						Name = c.competencyText.ToString()
						//Description = c.comment != null && c.comment.Count() > 0 ? c.comment[0].ToString()	
					} );
				}
			}
			//20-07-02 just storing the index ready competencies
			//ef.CompentenciesJson = JsonConvert.SerializeObject( competencies, MappingHelperV3.GetJsonSettings() );
			ef.CompentenciesStore = JsonConvert.SerializeObject( ef.Competencies, MappingHelperV3.GetJsonSettings() );

			//test 
			//ElasticManager.LoadCompetencies( ef.Name, ef.CompentenciesStore );
			ef.CompetencyFrameworkGraph = glist;
			ef.TotalCompetencies = competencies.Count();

			ef.Name = helper.HandleLanguageMap( input.name, ef, "Name" );
			ef.Description = helper.HandleLanguageMap( input.description, ef, "description" );
			ef.CTID = input.CTID;
			ef.OrganizationCTID = orgCTID;
			if ( org != null && org.Id > 0 ) 
			{
				orgName = org.Name;
				ef.OrganizationId = org.Id;
				helper.CurrentOwningAgentUid = org.RowId;
			}
			
			ef.CredentialRegistryId = envelopeIdentifier;
			//additions
			//ef.ind
			//can only handle one source
			int pcnt = 0;
			if ( input.source != null )
			{
				foreach ( var url in input.source )
				{
					pcnt++;
					ef.SourceUrl = url;
					break;
				}
			}
			ef.FrameworkUri = input.CtdlId;
			//adding common import pattern

			new CompetencyFrameworkServices().Import( ef, ref status );

			//

			//
			//framework checks
			if ( input.inLanguage == null || input.inLanguage.Count() == 0)
			{
				//document for followup
				//LoggingHelper.DoTrace( 5, "		Framework missing inLanguage: " + input.name.ToString() );
			}
			//output.Name = helper.HandleLanguageMap( input.name, output, "Name" );
			//output.description = helper.HandleLanguageMap( input.description, output, "Description" );
			output.CTID = input.CTID;

			return importSuccessfull;
        }

		//currently use education framework
		public bool DoesEntityExist( string ctid, ref Framework entity )
		{
			bool exists = false;
			entity = EntityServices.GetCompetencyFrameworkByCtid( ctid );
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
                    if ( token.ToString().IndexOf( "ceasn:CompetencyFramework" ) > -1 )
                    {
                        entity = ( ( Newtonsoft.Json.Linq.JObject ) token ).ToObject<InputEntity>();

                        //InputEntity cf = ( InputEntity ) JsonConvert.DeserializeObject( token.ToString() );
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
