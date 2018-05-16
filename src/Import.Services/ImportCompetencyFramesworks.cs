using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using EntityServices = workIT.Services.CompetencyFrameworkServices;
using InputEntity = RA.Models.Json.CompetencyFrameworksGraph;
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
        InputEntity input = new InputEntity();
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
            input = JsonConvert.DeserializeObject<InputEntity>( item.DecodedResource.ToString() );
            RA.Models.Json.CompetencyFrameworkInput framework = GetFramework( input.Graph );
            LoggingHelper.DoTrace( 5, "		framework name: " + framework.name.English_US );

            //just store input for now
            return Import( mgr, input, envelopeIdentifier, status );

            //return true;
        } //
        private RA.Models.Json.CompetencyFrameworkInput GetFramework( object graph )
        {
            //string ctid = "";
            RA.Models.Json.CompetencyFrameworkInput entity = new RA.Models.Json.CompetencyFrameworkInput();
            Newtonsoft.Json.Linq.JArray jarray = ( Newtonsoft.Json.Linq.JArray ) graph;
            foreach ( var token in jarray )
            {
                if ( token.GetType() == typeof( Newtonsoft.Json.Linq.JObject ) )
                {
                    if ( token.ToString().IndexOf( "ceasn:CompetencyFramework" ) > -1 )
                    {
                        entity = ( ( Newtonsoft.Json.Linq.JObject ) token ).ToObject<RA.Models.Json.CompetencyFrameworkInput>();

                        //RA.Models.Json.CompetencyFrameworkInput cf = ( RA.Models.Json.CompetencyFrameworkInput ) JsonConvert.DeserializeObject( token.ToString() );
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
        public bool Import( EntityServices mgr, InputEntity input, string envelopeIdentifier, SaveStatus status )
        {
            List<string> messages = new List<string>();
            bool importSuccessfull = true;

            //try
            //{
            //input = JsonConvert.DeserializeObject<InputEntity>( item.DecodedResource.ToString() );
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
