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
using ResourceServices = workIT.Services.ConceptSchemeServices;


using InputConcept = RA.Models.JsonV2.Concept;
using InputResource = RA.Models.JsonV2.ConceptScheme;
using InputPModel = RA.Models.JsonV2.ProgressionModel;
using InputPLevel = RA.Models.JsonV2.ProgressionLevel ;
using InputGraph = RA.Models.JsonV2.GraphContainer;
using MC = workIT.Models.Common;
using ThisResource		= workIT.Models.Common.ConceptScheme;
using PModel			= workIT.Models.Common.ConceptScheme;
namespace Import.Services
{
	/// <summary>
	/// Import concept schemes and progression models
	/// These are different enough to have separate processes now!!!!!!!
	/// </summary>
	public class ImportConceptSchemes
    {
        int DefaultEntityTypeId = CodesManager.ENTITY_TYPE_CONCEPT_SCHEME;
        string thisClassName = "ImportConceptSchemes";
		string resourceType = "ConceptScheme";
		ImportManager importManager = new ImportManager();
        InputGraph input = new InputGraph();
        ThisResource output = new ThisResource();
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

			string statusMessage = string.Empty;
			ResourceServices mgr = new ResourceServices();
			string ctdlType = string.Empty;
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
			string statusMessage = string.Empty;
			ResourceServices mgr = new ResourceServices();
			string ctdlType = string.Empty;
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
			//22-05-11 - have to check envelope type for conceptscheme or progression model
			int newImportId = importHelper.Add( item, status.EntityTypeId, status.Ctid, importSuccessfull, importError, ref messages );
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
			if ( DateTime.TryParse( item.NodeHeaders.CreatedAt.Replace( "UTC", string.Empty ).Trim(), out createDate ) )
			{
				status.SetEnvelopeCreated( createDate );
			}
			if ( DateTime.TryParse( item.NodeHeaders.UpdatedAt.Replace( "UTC", string.Empty ).Trim(), out envelopeUpdateDate ) )
			{
				status.SetEnvelopeUpdated( envelopeUpdateDate );	
			}

			string payload = item.DecodedResource.ToString();
            string envelopeIdentifier = item.EnvelopeIdentifier;
            string ctdlType = RegistryServices.GetResourceType( payload );
            string envelopeUrl = RegistryServices.GetEnvelopeUrl( envelopeIdentifier );
            LoggingHelper.DoTrace( 5, "		envelopeUrl: " + envelopeUrl );
			LoggingHelper.WriteLogFile( UtilityManager.GetAppKeyValue( "logFileTraceLevel", 5 ), item.EnvelopeCtid + "_ConceptScheme", payload, string.Empty, false );

            //just store input for now
            return Import( payload, status, ctdlType );

            //return true;
        } //

        public bool Import( string payload, SaveStatus status, string ctdlType )
        {
			LoggingHelper.DoTrace( 7, "ImportConceptSchemes - entered." );
            List<string> messages = new List<string>();
			DateTime started = DateTime.Now;
			var saveDuration = new TimeSpan();
			//set default
			var entityTypeId = CodesManager.ENTITY_TYPE_CONCEPT_SCHEME;
			if (ctdlType == "ConceptScheme")
				entityTypeId = CodesManager.ENTITY_TYPE_CONCEPT_SCHEME;
			else if (ctdlType == "ProgressionModel")
			{
				entityTypeId = CodesManager.ENTITY_TYPE_PROGRESSION_MODEL;
				//in case get here, call PM import
				return new ImportProgressionModels().Import(payload, status, ctdlType );
			}
			else
			{

			}
			status.EntityTypeId = entityTypeId;
			MappingHelperV3 helper = new MappingHelperV3( entityTypeId );
			bool importSuccessfull = true;
			var input = new InputResource();
			var concept = new InputConcept();
			var mainEntity = new Dictionary<string, object>();
			List<InputConcept> concepts = new List<InputConcept>();
			List<InputPLevel> progressionLevels = new List<InputPLevel>();
			//Dictionary<string, object> dictionary = RegistryServices.JsonToDictionary( payload );
			//object graph = dictionary[ "@graph" ];
			////serialize the graph object
			//var glist = JsonConvert.SerializeObject( graph );
			////parse graph in to list of objects
			//JArray graphList = JArray.Parse( glist );
			JArray graphList = RegistryServices.GetGraphList( payload );
			var bnodes = new List<BNode>();
			int cntr = 0;
			foreach ( var item in graphList )
			{
				cntr++;
				//note older frameworks will not be in the priority order
				var main = item.ToString();
				RegistryObject mro = new RegistryObject( main );
				if ( mro.CtdlType == "skos:ConceptScheme" || mro.CtdlType == "ConceptScheme" )
				{
					input = JsonConvert.DeserializeObject<InputResource>( main );
				} 
				else if ( mro.CtdlType == "asn:ProgressionModel" || mro.CtdlType == "ProgressionModel" )
				{
					//????????????Should distinguish thios
					input = JsonConvert.DeserializeObject<InputResource>( main );
				}
				else if ( mro.CtdlType == "skos:Concept" || mro.CtdlType == "Concept" )
				{
					concepts.Add( JsonConvert.DeserializeObject<InputConcept>( main ) );
				}
				else if ( mro.CtdlType == "asn:ProgressionLevel" || mro.CtdlType == "ProgressionLevel" )
				{
					//need to start distinguishing
					progressionLevels.Add( JsonConvert.DeserializeObject<InputPLevel>( main ) );
				}
				else if( main.IndexOf( "_:" ) > -1 )
					{
					bnodes.Add( JsonConvert.DeserializeObject<BNode>( main ) );
				}
				else
                {
					//unexpected
				}
				//
				//var baseObject = JsonConvert.DeserializeObject<RegistryBaseObject>( main );

				//if ( cntr == 1 || main.IndexOf( ctdlType ) > -1 )
				//{
				//	//HACK
					
				//	if ( main.IndexOf( "ConceptScheme" ) > -1 )
				//	{
				//		input = JsonConvert.DeserializeObject<InputResource>( main );
				//	}		
				//	else if ( main.IndexOf( "ProgressionModel" ) > -1 )
				//	{
				//		input = JsonConvert.DeserializeObject<InputResource>( main );
				//	}
				//	else if ( main.IndexOf( "asn:ProgressionLevel" ) > -1 )
				//	{
				//		input = JsonConvert.DeserializeObject<InputResource>( main );
				//	}
				//}
				//else
				//{

				//	//should just have concepts, but should check for bnodes
				//	var child = item.ToString();
				//	if ( child.IndexOf( "_:" ) > -1 )
				//	{
				//		bnodes.Add( JsonConvert.DeserializeObject<BNode>( child ) );
				//	}
				//	else if ( child.IndexOf( "skos:Concept" ) > -1 )
				//	{
				//		concepts.Add( JsonConvert.DeserializeObject<InputConcept>( child ) );
				//	}
				//	else
				//	{
				//		//unexpected
				//	}
				//}
			}

			//try
			//{
			//input = JsonConvert.DeserializeObject<InputGraph>( item.DecodedResource.ToString() );
			string ctid = input.CTID;
            status.Ctid = ctid;
            status.ResourceURL = input.CtdlId;
            LoggingHelper.DoTrace( 5, "		ctid: " + ctid );
            LoggingHelper.DoTrace( 5, "		@Id: " + input.CtdlId );
			LoggingHelper.DoTrace( 5, "		name: " + input.Name.ToString() );

			//this will be the primary org based on creator (prime) or publisher
			var org = new MC.Organization();
			string orgCTID = string.Empty;
			string orgName = string.Empty;
			try
			{

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

				//TBD handling of referencing third party publisher
				helper.MapOrganizationPublishedBy( output, ref status );

				output.EntityTypeId = entityTypeId;
				//publisher should be a list, but need to handle older records as a string
				var publisherList = new List<string>();
				var publisher = input.Publisher;
				if ( publisher != null )
				{
					if ( publisher.GetType() == typeof( Newtonsoft.Json.Linq.JArray ) )
					{
						var stringArray = ( Newtonsoft.Json.Linq.JArray )publisher;
						var list = stringArray.ToObject<List<string>>();
						publisherList.AddRange(list );

					}
					else if ( publisher.GetType() == typeof( string ) )
					{
						//we could just force the object back to a string?
						var pub = publisher.ToString();
						publisherList = new List<string>() { pub };
					}
				}
				output.PublisherUid = helper.MapOrganizationReferencesToGuid( "ConceptScheme.Publisher", publisherList, ref status );

				//WHOA - THIS COULD BE A LanguageMapList?
				output.PublisherName = new List<string>();
				var publisherName = input.PublisherName;
				if ( publisherName != null )
				{
					if ( publisherName.GetType() == typeof( Newtonsoft.Json.Linq.JArray ) )
					{
						var stringArray = ( Newtonsoft.Json.Linq.JArray ) publisherName;
						var list = stringArray.ToObject<List<string>>();
						output.PublisherName.AddRange( list );

					}
					else if ( publisherName.GetType() == typeof( string ) )
					{
						//we could just force the object back to a string?
						var pub = publisherName.ToString();
						output.PublisherName = new List<string>() { pub };
					}
				}
				//
				output.Creator = helper.MapOrganizationReferenceGuids( "ConceptScheme.Creator", input.Creator, ref status );
				//20-06-11 - need to get creator, publisher, owner where possible
				//	include an org reference with name, swp, and??
				//should check creator first? Or will publisher be more likely to have an account Ctid?
				if ( output.Creator != null && output.Creator.Count() > 0 )
				{
					//get org or pending stub
					//look up org name
					org = OrganizationManager.Exists( output.Creator[ 0 ] );
					//output.Publisher.Add( output.Creator[ 0 ] );
					if (org != null )
					{
						orgCTID = org.CTID;
					}
				}
				//
				if ( output.PublisherUid != Guid.Empty )
				{
					output.Publisher.Add( output.PublisherUid );
					//if no creator, look up org name
					org = OrganizationManager.Exists( output.PublisherUid );					
					if ( org != null && string.IsNullOrWhiteSpace( orgCTID ))
					{
						orgCTID = org.CTID;
					}
				}
			
				//
				if ( org != null && org.Id > 0 )
				{
					orgName = org.Name;
					output.OrganizationId = org.Id;
					helper.CurrentOwningAgentUid = org.RowId;
				}
				output.PrimaryOrganizationCTID = orgCTID;

				//output.ChangeNote = helper.HandleLanguageMapList( input.ChangeNote, output, "ChangeNote" );
				output.ConceptKeyword = helper.HandleLanguageMapList( input.ConceptKeyword, output, "ConceptKeyword" );
				output.DateCopyrighted = input.DateCopyrighted;
				output.DateCreated = input.DateCreated;
				output.DateModified = input.DateModified;
				output.InLanguageCodeList = helper.MapInLanguageToTextValueProfile( input.InLanguage, $"{resourceType}.InLanguage. CTID: " + ctid );

				output.License = input.License;
				//remove "https://credreg.net/ctdlasn/vocabs/publicationStatus/"
				output.PublicationStatusType = (input.PublicationStatusType ?? string.Empty).Replace( "https://credreg.net/ctdlasn/vocabs/publicationStatus/", string.Empty) ;
				output.Rights = helper.HandleLanguageMap( input.Rights, output, "Rights" );

				if ( input.RightsHolder != null && input.RightsHolder.Any())
					output.RightsHolder = input.RightsHolder[0];
                //
                output.Source = new List<string>();
                var source = input.Source;
                if ( source != null )
                {
                    if ( source.GetType() == typeof( Newtonsoft.Json.Linq.JArray ) )
                    {
                        var stringArray = ( Newtonsoft.Json.Linq.JArray ) source;
                        var list = stringArray.ToObject<List<string>>();
                        output.Source.AddRange( list );

                    }
                    else if ( source.GetType() == typeof( string ) )
                    {
                        output.Source = new List<string>() { source.ToString() };
                    }
                }

                //output.CredentialRegistryId = envelopeIdentifier;
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
							CTID = item.CTID,
							SubjectWebpage = item.SubjectWebpage
						};
						if ( c.Notes != null && c.Notes.Any() )
							c.Note = c.Notes[0];
						output.HasConcepts.Add( c );
					}
				}
				
				//20-07-02 just storing the index ready concepts
				output.ConceptsStore = JsonConvert.SerializeObject( output.HasConcepts, MappingHelperV3.GetJsonSettings() );

				//
				new ConceptSchemeServices().Import( output, ref status );
                status.DetailPageUrl = string.Format( "~/conceptscheme/{0}", output.Id );

                status.DocumentId = output.Id;
				status.DocumentRowId = output.RowId;
				//just in case
				if ( status.HasErrors )
					importSuccessfull = false;

				//if record was added to db, add to/or set EntityResolution as resolved
				int ierId = new ImportManager().Import_EntityResolutionAdd( status.ResourceURL,
							ctid,
							entityTypeId,
							output.RowId,
							output.Id,
							( output.Id > 0 ),
							ref messages,
							output.Id > 0 );
			}
			catch ( Exception ex )
			{
				//activity type: cs, activity: import, event: exception, 
				LoggingHelper.LogError( ex, thisClassName + ".Import", string.Format( "Exception encountered for CTID: {0}", ctid ) );
			}
			finally
			{
				var totalDuration = DateTime.Now.Subtract( started );
				if ( totalDuration.TotalSeconds > 9 && ( totalDuration.TotalSeconds - saveDuration.TotalSeconds > 3 ) )
					LoggingHelper.DoTrace( 5, string.Format( "         WARNING Total Duration: {0:N2} seconds ", totalDuration.TotalSeconds ) );
			}
			return importSuccessfull;
        }

		//
		public bool DoesEntityExist( string ctid, ref ThisResource entity )
		{
			bool exists = false;
			entity = ResourceServices.GetByCtid( ctid );
			if ( entity != null && entity.Id > 0 )
				return true;

			return exists;
		}

    }
}
