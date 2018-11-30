using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using EntityServices = workIT.Services.CompetencyFrameworkServices;
using InputGraph = RA.Models.JsonV3.CompetencyFrameworksGraph;
using InputEntity = RA.Models.JsonV3.CompetencyFrameworkInput;
using ThisEntity = workIT.Models.Common.CompetencyFramework;
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
            LoggingHelper.WriteLogFile( 1, item.EnvelopeIdentifier + "_competencyFrameswork", payload, "", false );
            input = JsonConvert.DeserializeObject<InputGraph>( item.DecodedResource.ToString() );
            InputEntity framework = GetFramework( input.Graph );
            LoggingHelper.DoTrace( 5, "		framework name: " + framework.name.ToString() );

            //just store input for now
            return Import( mgr, input, envelopeIdentifier, status );

            //return true;
        } //
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
                        //var c2 = ( ( Newtonsoft.Json.Linq.JObject ) c1 ).ToObject<RA.Models.Json.CompetencyInput>();

                    }
                }
                else
                {
                    //error
                }
            }
            //no ctid found, so????
            return entity;
        }
        public bool Import( EntityServices mgr, InputGraph input, string envelopeIdentifier, SaveStatus status )
        {
            List<string> messages = new List<string>();
            bool importSuccessfull = true;

            //try
            //{
            //input = JsonConvert.DeserializeObject<InputGraph>( item.DecodedResource.ToString() );
            string ctid = input.CTID;
            status.Ctid = ctid;
            string referencedAtId = input.CtdlId;
            LoggingHelper.DoTrace( 5, "		ctid: " + ctid );
            LoggingHelper.DoTrace( 5, "		@Id: " + input.CtdlId );

            if ( status.DoingDownloadOnly )
            	return true; 

            return importSuccessfull;
        }
    }
}
