using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using EntityServices = workIT.Services.CompetencyFrameworkServices;
using InputGraph = RA.Models.JsonV2.CompetencyFrameworksGraph;
using InputEntity = RA.Models.JsonV2.CompetencyFramework;
using InputCompetency = RA.Models.JsonV2.Competency;
using ThisEntity = workIT.Models.Common.CompetencyFramework;
using Framework = workIT.Models.ProfileModels.EducationFramework;
using BNode = RA.Models.JsonV2.BlankNode;

using workIT.Utilities;
using workIT.Factories;
using workIT.Models;
using Import.Services;

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

		public bool ProcessEnvelope( EntityServices mgr, ReadEnvelope item, SaveStatus status )
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
				//entity.DocumentUpdatedAt = updateDate;
			}
			if ( DateTime.TryParse( item.NodeHeaders.UpdatedAt.Replace( "UTC", "" ).Trim(), out envelopeUpdateDate ) )
			{
				//entity.DocumentUpdatedAt = envelopeUpdateDate;
			}

			string payload = item.DecodedResource.ToString();
            string envelopeIdentifier = item.EnvelopeIdentifier;
            string ctdlType = RegistryServices.GetResourceType( payload );
            string envelopeUrl = RegistryServices.GetEnvelopeUrl( envelopeIdentifier );
            LoggingHelper.DoTrace( 5, "		envelopeUrl: " + envelopeUrl );
            LoggingHelper.WriteLogFile( 1, item.EnvelopeIdentifier + "_competencyFrameswork", payload, "", false );
            //input = JsonConvert.DeserializeObject<InputGraph>( item.DecodedResource.ToString() );

            //InputEntity framework = GetFramework( input.Graph );
            //LoggingHelper.DoTrace( 5, "		framework name: " + framework.name.ToString() );

            //just store input for now
            return Import( mgr, payload, envelopeIdentifier, status );

            //return true;
        } //

        public bool Import( EntityServices mgr, string payload, string envelopeIdentifier, SaveStatus status )
        {
            List<string> messages = new List<string>();
			MappingHelperV3 helper = new MappingHelperV3();
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
			string orgCTID = "";
			string orgName = "";
			List<string> publisher = input.publisher;
			//should check creator first? Or will publisher be more likely to have an account Ctid?
			if ( publisher != null && publisher.Count() > 0 )
			{
				orgCTID = ResolutionServices.ExtractCtid( publisher[ 0 ] );
				//look up org name
				orgName = OrganizationManager.GetByCtid( orgCTID ).Name ?? "missing";
			}
			else
			{
				//try creator
				List<string> creator = input.creator;
				if ( creator != null && creator.Count() > 0 )
				{
					orgCTID = ResolutionServices.ExtractCtid( creator[ 0 ] );
					//look up org name
					orgName = OrganizationManager.GetByCtid( orgCTID ).Name ?? "missing";
				}
			}

			if ( status.DoingDownloadOnly )
            	return true;

			//add updating educationFramework
			Framework ef = new Framework();
			if ( !DoesEntityExist( input.CTID, ref ef ) )
			{
				//set the rowid now, so that can be referenced as needed
				output.RowId = Guid.NewGuid();
				ef.RowId = Guid.NewGuid();
			}
			helper.currentBaseObject = ef;
			ef.ExistsInRegistry = true;

			ef.FrameworkName = helper.HandleLanguageMap( input.name, ef, "Name" );
			ef.CTID = input.CTID;
			ef.OrganizationCTID = orgCTID;
			ef.CredentialRegistryId = envelopeIdentifier;
			//can only handle one source
			int pcnt = 0;
			foreach ( var url in input.source )
			{
				pcnt++;
				ef.SourceUrl = url;
				break;
			}
			ef.FrameworkUri = input.CtdlId;
			new EducationFrameworkManager().Save( ef, ref status, true );
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
			output.Ctid = input.CTID;

			return importSuccessfull;
        }

		//currently use education framework
		public bool DoesEntityExist( string ctid, ref Framework entity )
		{
			bool exists = false;
			entity = EntityServices.GetEducationFrameworkByCtid( ctid );
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
