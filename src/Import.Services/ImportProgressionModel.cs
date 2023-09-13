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
using EntityServices = workIT.Services.ProgressionModelServices;


using InputEntity = RA.Models.JsonV2.ProgressionModel;
using InputPLevel = RA.Models.JsonV2.ProgressionLevel ;
using InputGraph = RA.Models.JsonV2.GraphContainer;
using MC = workIT.Models.Common;
using ThisResource		= workIT.Models.Common.ProgressionModel;

namespace Import.Services
{
	/// <summary>
	/// Import concept schemes and progression models
	/// These are different enough to have separate processes now!!!!!!!
	/// </summary>
	public class ImportProgressionModels
    {
        int DefaultEntityTypeId = CodesManager.ENTITY_TYPE_PROGRESSION_MODEL;
        string thisClassName = "ImportProgressionModels";
		string resourceType = "ProgressionModel";
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
			LoggingHelper.WriteLogFile( UtilityManager.GetAppKeyValue( "logFileTraceLevel", 5 ), item.EnvelopeCtid + "_ProgressionModel", payload, "", false );

            //just store input for now
            return Import( payload, status, ctdlType );

            //return true;
        } //

        public bool Import( string payload, SaveStatus status, string ctdlType )
        {
			LoggingHelper.DoTrace( 7, "ImportProgressionModels - entered." );
            List<string> messages = new List<string>();
			DateTime started = DateTime.Now;
			var saveDuration = new TimeSpan();
			//set default
			var entityTypeId = CodesManager.ENTITY_TYPE_PROGRESSION_MODEL;
			status.EntityTypeId = entityTypeId;
			MappingHelperV3 helper = new MappingHelperV3( entityTypeId );
			bool importSuccessfull = true;
			var input = new InputEntity();
			var mainEntity = new Dictionary<string, object>();
			List<InputPLevel> progressionLevels = new List<InputPLevel>();
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
				if ( mro.CtdlType == "asn:ProgressionModel" || mro.CtdlType == "ProgressionModel" )
				{
					input = JsonConvert.DeserializeObject<InputEntity>( main );
				}				
				else if ( mro.CtdlType == "asn:ProgressionLevel" || mro.CtdlType == "ProgressionLevel" )
				{
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

			string framework = input.Name.ToString();
			var org = new MC.Organization();
			string orgCTID = "";
			string orgName = "";
			try
			{

				if ( status.DoingDownloadOnly )
					return true;

				//add/updating ProgressionModel
				//var output = new ProgressionModel();
				if ( !DoesEntityExist( input.CTID, ref output ) )
				{
					//set the rowid now, so that can be referenced as needed
					output.RowId = Guid.NewGuid();
					//output.RowId = Guid.NewGuid();
				}
				helper.currentBaseObject = output;
				// 
				//TBD handling of referencing third party publisher
				if ( !string.IsNullOrWhiteSpace( status.DocumentPublishedBy ) )
				{
					//output.PublishedByOrganizationCTID = status.DocumentPublishedBy;
					var porg = OrganizationManager.GetSummaryByCtid( status.DocumentPublishedBy );
					if ( porg != null && porg.Id > 0 )
					{
						//TODO - store this in a json blob??????????
						output.PublishedByThirdPartyOrganizationId = porg.Id;
						//this will result in being added to Entity.AgentRelationship
						output.PublishedBy = new List<Guid>() { porg.RowId };
					}
					else
					{
						//if publisher not imported yet, all publishee stuff will be orphaned
						var entityUid = Guid.NewGuid();
						var resPos = status.ResourceURL.IndexOf( "/resources/" );
						var swp = status.ResourceURL.Substring( 0, ( resPos + "/resources/".Length ) ) + status.DocumentPublishedBy;
						int orgId = new OrganizationManager().AddPendingRecord( entityUid, status.DocumentPublishedBy, swp, ref status );
						output.PublishedByThirdPartyOrganizationId = porg.Id;
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
				output.Name = helper.HandleLanguageMap( input.Name, output, "Name" );
				output.Description = helper.HandleLanguageMap( input.Description, output, "description" );
				output.CTID = input.CTID;
				output.PrimaryOrganizationCTID = orgCTID;
				output.EntityTypeId = entityTypeId;
				//
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
				output.PublisherUid = helper.MapOrganizationReferencesToGuid( "ProgressionModel.Publisher", publisherList, ref status );

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
				
				output.Creator = helper.MapOrganizationReferenceGuids( "ProgressionModel.Creator", input.Creator, ref status );
				//20-06-11 - need to get creator, publisher, owner where possible
				//	include an org reference with name, swp, and??
				//should check creator first? Or will publisher be more likely to have an account Ctid?
				if ( output.Creator != null && output.Creator.Count() > 0 )
				{
					//get org or pending stub
					//look up org name
					org = OrganizationManager.Exists( output.Creator[ 0 ] );
					output.OwnedBy.Add( output.Creator[ 0 ] );
				}
				else
				{
					if ( output.PublisherUid != Guid.Empty )
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
				output.ChangeNote = helper.HandleLanguageMapList( input.ChangeNote, output, "ChangeNote" );
				output.ConceptKeyword = helper.HandleLanguageMapList( input.ConceptKeyword, output, "ConceptKeyword" );
				output.DateCopyrighted = input.DateCopyrighted;
				output.DateCreated = input.DateCreated;
				output.DateModified = input.DateModified;
				output.InLanguageCodeList = helper.MapInLanguageToTextValueProfile( input.InLanguage, $"{resourceType}.InLanguage. CTID: " + ctid );

				output.License = input.License;
				output.PublicationStatusType = input.PublicationStatusType;
				output.Rights = helper.HandleLanguageMap( input.Rights, output, "Rights" );

				output.RightsHolder = input.RightsHolder;
				if ( input.Source != null)
					output.Source = input.Source.ToString();


				//output.CredentialRegistryId = envelopeIdentifier;
				output.HasConcepts = new List<MC.Concept>();
				
				if ( progressionLevels != null && progressionLevels.Count > 0 )
				{
					output.TotalConcepts = progressionLevels.Count();
					foreach ( var item in progressionLevels )
					{
						var c = new MC.Concept()
						{
							PrefLabel = helper.HandleLanguageMap( item.PrefLabel, output, "PrefLabel" ),
							Definition = helper.HandleLanguageMap( item.Definition, output, "Definition" ),
							Notes = helper.HandleLanguageMapList( item.Note, output ),
							CTID = item.CTID,
						};
						if ( c.Notes != null && c.Notes.Any() )
							c.Note = c.Notes[0];
						output.HasConcepts.Add( c );
					}
				}
		
				//20-07-02 just storing the index ready concepts
				output.ConceptsStore = JsonConvert.SerializeObject( output.HasConcepts, MappingHelperV3.GetJsonSettings() );

				//adding common import pattern

				new ProgressionModelServices().Import( output, ref status );
				//
				status.DocumentId = output.Id;
				status.DetailPageUrl = string.Format( "~/ProgressionModel/{0}", output.Id );
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
				LoggingHelper.LogError( ex, thisClassName + ".Import", string.Format( "Exception encountered for CTID: {0}", ctid ), false, "Concept Scheme Import exception" );
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
			entity = EntityServices.GetByCtid( ctid );
			if ( entity != null && entity.Id > 0 )
				return true;

			return exists;
		}

    }
}
