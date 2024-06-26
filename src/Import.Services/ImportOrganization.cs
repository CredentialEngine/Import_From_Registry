using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Services;
using workIT.Utilities;
using BNodeV3 = RA.Models.JsonV2.BlankNode;
using ResourceServices = workIT.Services.OrganizationServices;
using FAPI = workIT.Services.API;
using InputEntityV3 = RA.Models.JsonV2.Agent;
using RMJ = RA.Models.JsonV2;
using ThisResource = workIT.Models.Common.Organization;
namespace Import.Services
{
	public class ImportOrganization
    {
        int entityTypeId = CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION;
		string thisClassName = "ImportOrganization";
		ImportManager importManager = new ImportManager();
        ImportServiceHelpers importHelper = new ImportServiceHelpers();
		ThisResource output = new ThisResource();

		#region custom imports
		public void ImportPendingRecords()
		{
            string where = " [EntityStateId] = 1 ";
            int pTotalRows = 0;

			SaveStatus status = new SaveStatus();
			List<OrganizationSummary> list = OrganizationManager.MainSearch( where, "", 1, 500, ref pTotalRows );
			LoggingHelper.DoTrace( 1, string.Format( thisClassName + " - ImportPendingRecords(). Processing {0} records =================", pTotalRows) );
			foreach ( OrganizationSummary item in list )
			{
				status = new SaveStatus();
				//SWP contains the resource url
				//pending records will have a  CTID, it should be used to get the envelope!
				//if ( !ImportByResourceUrl( item.SubjectWebpage, status ) )
				if ( !ImportByCtid( item.CTID, status ) )
				{
                    //check for 404
                    LoggingHelper.DoTrace(1, string.Format("     - (). Failed to import pending credential: {0}, message(s): {1}", item.Id, status.GetErrorsAsString()));
                }
                else
                    LoggingHelper.DoTrace(1, string.Format("     - (). Successfully imported pending credential: {0}", item.Id));
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
	
		/// <summary>
		/// Custom version, typically called outside a scheduled import
		/// </summary>
		/// <param name="item"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool CustomProcessRequest( ReadEnvelope item, SaveStatus status )
		{
			//ResourceServices mgr = new ResourceServices();
			bool importSuccessfull = ProcessEnvelope( item, status );
			List<string> messages = new List<string>();
			string importError = string.Join( "\r\n", status.GetAllMessages().ToArray() );
			//store envelope
			int newImportId = importHelper.Add( item, 1, status.Ctid, importSuccessfull, importError, ref messages );
			if ( newImportId > 0 && status.Messages != null && status.Messages.Count > 0 )
			{
				//add indicator of current recored
				string msg = string.Format( "========= Messages for Organization, EnvelopeIdentifier: {0}, ctid: {1}, Id: {2}, rowId: {3} =========", item.EnvelopeIdentifier, status.Ctid, status.DocumentId, status.DocumentRowId );
				importHelper.AddMessages( newImportId, status, ref messages );
			}
			return importSuccessfull;
		}
		#endregion
		/// <summary>
		/// Custom version, typically called outside a scheduled import
		/// </summary>
		/// <param name="item"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool CustomProcessEnvelope( ReadEnvelope item, SaveStatus status )
		{
			ResourceServices mgr = new ResourceServices();
            bool importSuccessfull = ProcessEnvelope( item, status );
            List<string> messages = new List<string>();
            string importError = string.Join( "\r\n", status.GetAllMessages().ToArray() );
            //store envelope
            int newImportId = importHelper.Add( item, 2, status.Ctid, importSuccessfull, importError, ref messages );
            if ( newImportId > 0 && status.Messages != null && status.Messages.Count > 0 )
            {
                //add indicator of current recored
                string msg = string.Format( "========= Messages for Organization, EnvelopeIdentifier: {0}, ctid: {1}, Id: {2}, rowId: {3} =========", item.EnvelopeIdentifier, status.Ctid, status.DocumentId, status.DocumentRowId );
                importHelper.AddMessages( newImportId, status, ref messages );
            }
            return importSuccessfull;
        }
		public bool ProcessEnvelope( ReadEnvelope item, SaveStatus status )
		{
			if ( item == null || string.IsNullOrWhiteSpace( item.EnvelopeIdentifier ) )
			{
				status.AddError( "A valid ReadEnvelope must be provided." );
				return false;
			}
            //
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
			if ( item.documentOwnedBy != null && item.documentPublishedBy != null && item.documentPublishedBy != item.documentOwnedBy )
				status.DocumentPublishedBy = item.documentPublishedBy;
			//
			string payload = item.DecodedResource.ToString();
            status.EnvelopeId = item.EnvelopeIdentifier;
            string ctdlType = RegistryServices.GetResourceType( payload );
			//string envelopeUrl = RegistryServices.GetEnvelopeUrl( status.EnvelopeId );
			//Already done in  RegistryImport
			//LoggingHelper.WriteLogFile( UtilityManager.GetAppKeyValue( "logFileTraceLevel", 5 ), item.EnvelopeCetermsCtid + "_organization", payload, "", false );


			if ( ImportServiceHelpers.IsAGraphResource( payload ) )
            {
                //if ( payload.IndexOf( "\"en\":" ) > 0 )
                   return ImportV3( payload, status );
                //else
                //    return ImportV2( payload, envelopeIdentifier, status );
            }
            else
            {
				status.AddError( "Importing of an organization resource payload is no longer supported. Please provide a /graph/ input." );
                //LoggingHelper.DoTrace( 5, "		envelopeUrl: " + envelopeUrl );
				//LoggingHelper.WriteLogFile( 1, "org_" + item.EnvelopeIdentifier, payload, "", false );
				return false;
            }
		}
	
	
        public bool ImportV3( string payload, SaveStatus status )
        {
			DateTime started = DateTime.Now;
			var saveDuration = new TimeSpan();
			InputEntityV3 input = new InputEntityV3();
            var bnodes = new List<BNodeV3>();
            var mainEntity = new Dictionary<string, object>();

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
                else //is this too much of an assumption?
                {
                    var bn = item.ToString();
                    bnodes.Add( JsonConvert.DeserializeObject<BNodeV3>( bn ) );
                }

            }
  
            List<string> messages = new List<string>();
            bool importSuccessfull = false;
            ResourceServices mgr = new ResourceServices();
			MappingHelperV3 helper = new MappingHelperV3( 2 ) { };
            helper.entityBlankNodes = bnodes;
            helper.CurrentEntityCTID = input.CTID;
            helper.CurrentEntityName = input.Name.ToString();

			try
			{
				string ctid = input.CTID;
				status.ResourceURL = input.CtdlId;

				LoggingHelper.DoTrace( 5, "		name: " + input.Name.ToString() );
				LoggingHelper.DoTrace( 6, "		url: " + input.SubjectWebpage );
				LoggingHelper.DoTrace( 5, "		ctid: " + input.CTID );
				LoggingHelper.DoTrace( 5, "		@Id: " + input.CtdlId );
				status.Ctid = ctid;

				if ( status.DoingDownloadOnly )
					return true;

				if ( !DoesEntityExist( input.CTID, ref output ) )
				{
					//set the rowid now, so that can be referenced as needed
					output.RowId = Guid.NewGuid();
						LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".ImportV3(). Record was NOT found using CTID: '{0}'", input.CTID ) );
				} 
				else
				{
					LoggingHelper.DoTrace( 6, string.Format( thisClassName + ".ImportV3(). Found record: '{0}' using CTID: '{1}'", input.Name.ToString(), input.CTID ));
				}

				helper.currentBaseObject = output;
				helper.CurrentOwningAgentUid = output.RowId;

				CommonMapping( input, helper, ref output, ref status );
				//21-12-21 - start storing JSON in one line. Also remove inheritance to exclude properties like Created/LastUpdates
				if ( output.Addresses != null && output.Addresses.Any() )
					output.AddressesJson = JsonConvert.SerializeObject( output.Addresses, MappingHelperV3.GetJsonSettings() );
				//
				if ( UtilityManager.GetAppKeyValue( "skipOrgImportIfNoAddress", false ) )
				{
					if ( output.Addresses.Count == 0 )
					{
						//skip
						LoggingHelper.DoTrace( 2, string.Format( "		***Skipping org# {0}, {1} as it has no addresses and this is a special run.", output.Id, output.Name ) );
						return true;
					}
				}
				else if ( UtilityManager.GetAppKeyValue( "skipOrgImportIfNoShortRegions", false ) )
				{
					if ( output.Addresses.Count == 0 )
					{
						//skip
						LoggingHelper.DoTrace( 2, string.Format( "		***Skipping org# {0}, {1} as it has no addresses and this is a special run.", output.Id, output.Name ) );
						return true;
					}
					else if ( output.HasAnyShortRegions == false )
					{
						//skip
						LoggingHelper.DoTrace( 2, string.Format( "		***Skipping org# {0}, {1} as it has no addresses with short regions and this is a special run.", output.Id, output.Name ) );
						return true;
					}
				}


				TimeSpan duration = DateTime.Now.Subtract( started );
				if ( duration.TotalSeconds > 5 )
					LoggingHelper.DoTrace( 6, string.Format( "         WARNING Mapping Duration: {0:N2} seconds ", duration.TotalSeconds ) );
				DateTime saveStarted = DateTime.Now;

				//=== if any messages were encountered treat as warnings for now
				if ( messages.Count > 0 )
					status.SetMessages( messages, true );
				//just in case check if entity added since start
				if ( output.Id == 0 )
				{
					ThisResource entity = ResourceServices.GetSummaryByCtid( ctid );
					if ( entity != null && entity.Id > 0 )
					{
						output.Id = entity.Id;
						output.RowId = entity.RowId;
					}
				}

				//================= save the data ========================================
				if ( UtilityManager.GetAppKeyValue( "writingToFinderDatabase", true ) )
				{
					importSuccessfull = mgr.Import( output, ref status );

					//24-03-25 - use the generic process for blank nodes encountered during import
					new ProfileServices().IndexPrepForReferenceResource( helper.ResourcesToIndex, ref status );
				}
                
				saveDuration = DateTime.Now.Subtract( saveStarted );
				if ( saveDuration.TotalSeconds > 5 )
					LoggingHelper.DoTrace( 6, string.Format( "         WARNING SAVE Duration: {0:N2} seconds ", saveDuration.TotalSeconds ) );
				//
				status.DocumentId = output.Id;
				status.DetailPageUrl = string.Format( "~/organization/{0}", output.Id );
				status.DocumentRowId = output.RowId;

				//just in case
				if ( status.HasErrors )
					importSuccessfull = false;
				//
				//========== if requested call method to send to external API
				if ( !string.IsNullOrWhiteSpace( UtilityManager.GetAppKeyValue( "myExternalAPIEndpoint" ) ) )
				{
					var request = new CredentialRegistryResource()
					{
						EntityType = "Organization",
						CTID = output.CTID,
						Name = output.Name,
						Description = output.Description,
						SubjectWebpage = output.SubjectWebpage,
						OwningOrganizationCTID = output.CTID,
						CredentialFinderObject = output,
						DownloadDate = DateTime.Now,
						Created = status.EnvelopeCreatedDate,
						LastUpdated = status.EnvelopeUpdatedDate
					};
					var url = UtilityManager.GetAppKeyValue( "myExternalAPIEndpoint" );
					new ExternalServices().PostResourceToExternalService( url, request, ref messages );
				}
				//========== check if to be written to the generic CredentialRegistryDownload.Resource table
				if ( UtilityManager.GetAppKeyValue( "writingToDownloadResourceDatabase", false ) )
				{
					var resource = "";
					resource = JsonConvert.SerializeObject( output, JsonHelper.GetJsonSettings() );
					//resource = output.ToString();
					var request = new CredentialRegistryResource()
					{
						EntityType = "Organization",
						CTID = output.CTID,
						Name = output.Name,
						Description = output.Description,
						SubjectWebpage = output.SubjectWebpage,
						OwningOrganizationCTID = output.CTID,
						CredentialFinderObject = resource,
						CredentialRegistryGraph = payload,
						DownloadDate = DateTime.Now,
						Created = status.EnvelopeCreatedDate,
						LastUpdated = status.EnvelopeUpdatedDate
					};
					new ExternalServices().DownloadSave( request, ref messages );
				}
				//
				//if record was added to db, add to/or set EntityResolution as resolved
				int ierId = new ImportManager().Import_EntityResolutionAdd( status.ResourceURL,
						ctid, CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION,
						output.RowId,
						output.Id,
						false,
						ref messages,
						output.Id > 0 );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, $"{thisClassName}.ImportV3 Exception encountered in CTID: {output.CTID}" );
			}
			finally
			{
				var totalDuration = DateTime.Now.Subtract( started );
				if ( totalDuration.TotalSeconds > 9 && (totalDuration.TotalSeconds - saveDuration.TotalSeconds > 3) )
					LoggingHelper.DoTrace( 5, string.Format( "         WARNING Total Duration: {0:N2} seconds ", totalDuration.TotalSeconds ) );

			}
			return importSuccessfull;
		}

		public bool HandleExternalRequest( InputEntityV3 input, ref ThisResource output, ref SaveStatus status )
		{
			ResourceServices mgr = new ResourceServices();
			MappingHelperV3 helper = new MappingHelperV3( 2 ) { };
			var importSuccessfull = true;
			//TODO on blank nodes
			//the github examples are currently resources, need to include checks for id type
			//TBD on just doing everything
			//so far just for use case where sample data from github may have been used

			output.RowId = Guid.NewGuid();
			try
			{
				CommonMapping( input, helper, ref output, ref status );

				if ( UtilityManager.GetAppKeyValue( "writingToFinderDatabase", true ) )
				{
					importSuccessfull = mgr.Import( output, ref status );
					//confirm the third parameter of getting all
					var resource = FAPI.OrganizationServices.GetDetailForAPI( output.Id, true, true );
					//var resourceDetail2 = JObject.FromObject( resource );
					//or 
					var resourceDetail = JsonConvert.SerializeObject( resource, JsonHelper.GetJsonSettings( false ) );

					var statusMsg = "";
					if ( new EntityManager().EntityCacheUpdateResourceDetail( output.CTID, resourceDetail, ref statusMsg ) == 0 )
					{
						status.AddError( statusMsg );
					}
				}
			} catch (Exception ex)
			{
				importSuccessfull = false;
				LoggingHelper.LogError(ex, string.Format(thisClassName + ".HandleExternalRequest. Exception encountered in CTID: {0}", input.CTID));

			}
			return importSuccessfull;
		}

		public void CommonMapping( InputEntityV3 input, MappingHelperV3 helper, ref ThisResource output, ref SaveStatus status )
		{

			output.AgentDomainType = input.Type;
			output.EntityTypeId = OrganizationManager.MapEntityTypeId( input.Type );
			output.CTID = input.CTID;

			output.Name = helper.HandleLanguageMap( input.Name, output, "Name" );
			output.Description = helper.HandleLanguageMap( input.Description, output, "Description" );
			//map from idProperty to url
			output.SubjectWebpage = input.SubjectWebpage;

			//TBD handling of referencing third party publisher
			helper.MapOrganizationPublishedBy( output, ref status );

			output.CredentialRegistryId = status.EnvelopeId; ;
			output.AlternateNames = helper.MapToTextValueProfile( input.AlternateName, output, "AlternateName" );
			output.Image = input.Image;
			output.LifeCycleStatusType = helper.MapCAOToEnumermation( input.LifeCycleStatusType );

			output.AgentPurpose = input.AgentPurpose;
			output.AgentPurposeDescription = helper.HandleLanguageMap( input.AgentPurposeDescription, output, "AgentPurposeDescription" );
			output.Emails = helper.MapToTextValueProfile( input.Email );

			output.FoundingDate = input.FoundingDate;
			//future prep
			output.AvailabilityListings = input.AvailabilityListing;
			//consider JSON
			//output.AvailabilityListing = JsonConvert.SerializeObject( input.AvailabilityListing, MappingHelperV3.GetJsonSettings() );
			output.AvailabilityListing = helper.MapListToString( input.AvailabilityListing );
			//
			output.MissionAndGoalsStatement = input.MissionAndGoalsStatement;
			output.MissionAndGoalsStatementDescription = helper.HandleLanguageMap( input.MissionAndGoalsStatementDescription, output, "MissionAndGoalsStatementDescription" );

			output.Addresses = helper.FormatAvailableAtAddresses( input.Address, ref status );
			//handle addresses with just contact points
			foreach (var item in output.Addresses )
			{
				if (item.HasAddress() == false && item.HasContactPoints() )
				{
					output.ContactPoint = item.ContactPoint;
				}
			}
			//now just get the addresses with an address 
			output.Addresses = output.Addresses.Where( x => x.HasAddress() ).ToList();	

			//agent type, map to enumeration
			output.AgentType = helper.MapCAOListToEnumermation( input.AgentType );
			output.SupersededBy = input.SupersededBy ?? "";
			output.Supersedes = input.Supersedes ?? "";
			if ( output.SupersededBy.ToLower().IndexOf( "/resources/ce-" ) > -1 )
			{
				//????
				output.SupersededBy = helper.FormatFinderResourcesURL( output.SupersededBy ); //ResolutionServices.ExtractCtid( output.SupersededBy.Trim() );
			}
			if ( output.Supersedes.ToLower().IndexOf( "/resources/ce-" ) > -1 )
			{
				output.Supersedes = helper.FormatFinderResourcesURL( output.Supersedes ); //ResolutionServices.ExtractCtid( output.Supersedes.Trim() );
			}
			output.SupportServiceStatement = input.SupportServiceStatement;
			output.SupportServiceStatementDescription = helper.HandleLanguageMap( input.SupportServiceStatementDescription, output, "SupportServiceStatementDescription" );

			output.TransferValueStatement = input.TransferValueStatement;
			output.TransferValueStatementDescription = helper.HandleLanguageMap( input.TransferValueStatementDescription, output, "TransferValueStatementDescription" );

			//hasVerificationService. just do both for now. will need to handle import of old data at some point
			//if ( UtilityManager.GetAppKeyValue( "usingNewHasVerificationService", false ) )
			if ( input.HasVerificationService != null )
			{
				var list = new List<string>();
				//NO will not be typeof( List<string> )
				//if ( input.HasVerificationService.GetType() == typeof( List<string> ) )
				//{
				//	list = input.HasVerificationService as List<string>;
				//	output.HasVerificationServiceIds = helper.MapVerificationServiceReferences( list, "HasVerificationService", output.CTID, ref status );

				//}
				//else 
				if ( input.HasVerificationService.GetType() == typeof( Newtonsoft.Json.Linq.JArray ) )
				{
					var jarray = ( Newtonsoft.Json.Linq.JArray ) input.HasVerificationService;
					if ( input.HasVerificationService.ToString().IndexOf( "ceterms:VerificationServiceProfile" ) > 0 )
					{
						//old style
						try
						{

							var vsp = JsonConvert.DeserializeObject<List<RMJ.VerificationServiceProfile>>( input.HasVerificationService.ToString() );
							//will basically ignore
							output.VerificationServiceProfiles = helper.MapVerificationServiceProfiles( vsp, ref status );

						}
						catch ( Exception ex )
						{

						}
					}
					else
					{
						list = jarray.ToObject<List<string>>();
						output.HasVerificationServiceIds = helper.MapVerificationServiceReferences( list, "HasVerificationService", output.CTID, ref status );
					}
				}
				else if ( input.HasVerificationService.GetType() == typeof( Newtonsoft.Json.Linq.JObject ) )
				{
					//only possibility should be the VSP, and only on import of older orgs
					try
					{
						var vsp = JsonConvert.DeserializeObject<List<RMJ.VerificationServiceProfile>>( input.HasVerificationService.ToString() );

						output.VerificationServiceProfiles = helper.MapVerificationServiceProfiles( vsp, ref status );

					}
					catch ( Exception ex )
					{

					}
				}
				//else 
				//output.VerificationServiceProfiles = helper.MapVerificationServiceProfiles( input.VerificationServiceProfiles, ref status );
			}

			// output.targetc
			//other enumerations
			//	serviceType, AgentSectorType
			output.ServiceType = helper.MapCAOListToEnumermation( input.ServiceType );
			output.AgentSectorType = helper.MapCAOListToEnumermation( input.AgentSectorType );

			//Industries
			//output.Industry = helper.MapCAOListToEnumermation( input.IndustryType );
			output.IndustryTypes = helper.MapCAOListToCAOProfileList( input.IndustryType );
			//naics
			//should check for duplicates in Naics, IndustryTypes, then remove from Naics
			//output.Naics = input.Naics;
			if ( input.Naics != null )
			{
				foreach ( var item in input.Naics )
				{
					var exists = output.IndustryTypes.FirstOrDefault( i => i.CodedNotation == item );
					if ( exists == null || string.IsNullOrWhiteSpace( exists.CodedNotation ) )
						output.Naics.Add( item );
				}
			}
			//keywords
			output.Keyword = helper.MapToTextValueProfile( input.Keyword, output, "Keyword" );

			//duns, Fein.  IpedsID, opeID
			if ( !string.IsNullOrWhiteSpace( input.DUNS ) )
				output.IdentificationCodes.Add( new workIT.Models.ProfileModels.TextValueProfile { CodeSchema = "ceterms:duns", TextValue = input.DUNS } );
			if ( !string.IsNullOrWhiteSpace( input.FEIN ) )
				output.IdentificationCodes.Add( new workIT.Models.ProfileModels.TextValueProfile { CodeSchema = "ceterms:fein", TextValue = input.FEIN } );

			if ( !string.IsNullOrWhiteSpace( input.IpedsID ) )
				output.IdentificationCodes.Add( new workIT.Models.ProfileModels.TextValueProfile { CodeSchema = "ceterms:ipedsID", TextValue = input.IpedsID } );

			if ( !string.IsNullOrWhiteSpace( input.OPEID ) )
				output.IdentificationCodes.Add( new workIT.Models.ProfileModels.TextValueProfile { CodeSchema = "ceterms:opeID", TextValue = input.OPEID } );
			if ( !string.IsNullOrWhiteSpace( input.LEICode ) )
				output.IdentificationCodes.Add( new workIT.Models.ProfileModels.TextValueProfile { CodeSchema = "ceterms:leiCode", TextValue = input.LEICode } );
			//
			if ( !string.IsNullOrWhiteSpace( input.NcesID ) )
				output.IdentificationCodes.Add( new workIT.Models.ProfileModels.TextValueProfile { CodeSchema = "ceterms:ncesID", TextValue = input.NcesID } );
			//
			if ( !string.IsNullOrWhiteSpace( input.ISICV4 ) )
				output.IdentificationCodes.Add( new workIT.Models.ProfileModels.TextValueProfile { CodeSchema = "ceterms:isicV4", TextValue = input.ISICV4 } );

			//alternativeidentifier - should just be added to IdentificationCodes
			//20-10-31 - replacing AlternativeIdentifier with Identifier
			output.Identifier = helper.MapIdentifierValueList( input.Identifier );
			if ( output.Identifier != null && output.Identifier.Count() > 0 )
			{
				output.IdentifierJson = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
			}
			//contact point
			//output.ContactPoint = helper.FormatContactPoints( input.ContactPoint, ref status );
			//Jurisdiction
			output.Jurisdiction = helper.MapToJurisdiction( input.Jurisdiction, ref status );

			//SameAs URI
			output.SameAs = helper.MapToTextValueProfile( input.SameAs );
			//Social media
			output.SocialMediaPages = helper.MapToTextValueProfile( input.SocialMedia );

			//departments
			//not sure - MP - want to change how depts, and subs are handled
			output.ParentOrganization = helper.MapOrganizationReferenceGuids( "Organization.ParentOrganization", input.ParentOrganization, ref status );
			output.Departments = helper.MapOrganizationReferenceGuids( "Organization.Department", input.Department, ref status );
			output.SubOrganizations = helper.MapOrganizationReferenceGuids( "Organization.SubOrganization", input.SubOrganization, ref status );

			//output.OrganizationRole_Subsidiary = helper.FormatOrganizationReferences( input.SubOrganization );

			//Process profiles
			output.AdministrationProcess = helper.FormatProcessProfile( input.AdministrationProcess, ref status );
			output.MaintenanceProcess = helper.FormatProcessProfile( input.MaintenanceProcess, ref status );
			output.ComplaintProcess = helper.FormatProcessProfile( input.ComplaintProcess, ref status );
			output.DevelopmentProcess = helper.FormatProcessProfile( input.DevelopmentProcess, ref status );
			output.RevocationProcess = helper.FormatProcessProfile( input.RevocationProcess, ref status );
			output.ReviewProcess = helper.FormatProcessProfile( input.ReviewProcess, ref status );
			output.AppealProcess = helper.FormatProcessProfile( input.AppealProcess, ref status );

			//BYs
			output.AccreditedBy = helper.MapOrganizationReferenceGuids( "Organization.AccreditedBy", input.AccreditedBy, ref status );
			output.ApprovedBy = helper.MapOrganizationReferenceGuids( "Organization.ApprovedBy", input.ApprovedBy, ref status );
			output.RecognizedBy = helper.MapOrganizationReferenceGuids( "Organization.RecognizedBy", input.RecognizedBy, ref status );
			output.RegulatedBy = helper.MapOrganizationReferenceGuids( "Organization.RegulatedBy", input.RegulatedBy, ref status );
			//INs
			output.AccreditedIn = helper.MapToJurisdiction( "Organization.AccreditedIn", input.AccreditedIn, ref status );
			output.ApprovedIn = helper.MapToJurisdiction( "Organization.ApprovedIn", input.ApprovedIn, ref status );
			output.RecognizedIn = helper.MapToJurisdiction( "Organization.RecognizedIn", input.RecognizedIn, ref status );
			output.RegulatedIn = helper.MapToJurisdiction( "Organization.RegulatedIn", input.RegulatedIn, ref status );

			//Asserts
			//the entity type is not known
			output.Accredits = helper.MapEntityReferenceGuids( "Organization.Accredits", input.Accredits, 0, ref status );
			output.Approves = helper.MapEntityReferenceGuids( "Organization.Approves", input.Approves, 0, ref status );
			if ( output.Approves.Count > 0 )
			{

			}
			output.Offers = helper.MapEntityReferenceGuids( "Organization.Offers", input.Offers, 0, ref status );
			output.Owns = helper.MapEntityReferenceGuids( "Organization.Owns", input.Owns, 0, ref status );
			output.Renews = helper.MapEntityReferenceGuids( "Organization.Renews", input.Renews, 0, ref status );
			output.Revokes = helper.MapEntityReferenceGuids( "Organization.Revokes", input.Revokes, 0, ref status );
			output.Recognizes = helper.MapEntityReferenceGuids( "Organization.Recognizes", input.Recognizes, 0, ref status );
			output.Regulates = helper.MapEntityReferenceGuids( "Organization.Regulates", input.Regulates, 0, ref status );

			//Manifests
			output.ConditionManifestIds = helper.MapEntityReferences( input.HasConditionManifest, CodesManager.ENTITY_TYPE_CONDITION_MANIFEST, CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, ref status );
			output.CostManifestIds = helper.MapEntityReferences( input.HasCostManifest, CodesManager.ENTITY_TYPE_COST_MANIFEST, CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION, ref status );



		}

		public bool DoesEntityExist( string ctid, ref ThisResource entity )
        {
            bool exists = false;
            entity = ResourceServices.GetMinimumSummaryByCtid( ctid );
            if ( entity != null && entity.Id > 0 )
                return true;

            return exists;
        }
    }
}
