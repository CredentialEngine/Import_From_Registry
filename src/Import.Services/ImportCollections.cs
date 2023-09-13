using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using workIT.Factories;
using workIT.Models;
using workIT.Services;
using workIT.Utilities;

using RJ = RA.Models.JsonV2;
using BNode = RA.Models.JsonV2.BlankNode;
using InputGraph = RA.Models.JsonV2.GraphContainer;
using InputCompetency = RA.Models.JsonV2.Competency;
using InputResource = RA.Models.JsonV2.Collection;
using InputCollectionMember = RA.Models.JsonV2.CollectionMember;

using ResourceServices = workIT.Services.CollectionServices;
using ThisResource = workIT.Models.Common.Collection;

using OutputCollectionMember = workIT.Models.Common.CollectionMember;
using ImportCompetency = workIT.Models.ProfileModels.Competency;

using ApiEntity = workIT.Models.API.Collection;
using MC = workIT.Models.Common;


namespace Import.Services
{
	public class ImportCollections
	{
		int thisEntityTypeId = CodesManager.ENTITY_TYPE_COLLECTION;
		string thisClassName = "ImportCollections";
        string resourceType = "Collection";
        string CurrentCollection = "";
		ImportManager importManager = new ImportManager();
		InputGraph input = new InputGraph();
        ThisResource output = new ThisResource();
		ImportServiceHelpers importHelper = new ImportServiceHelpers();
		#region Common Helper Methods
		/// <summary>
		/// Retrieve an envelop from the registry and do import
		/// </summary>
		/// <param Name="envelopeId"></param>
		/// <param Name="status"></param>
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
            ResourceServices mgr = new ResourceServices();
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
		/// <param Name="ctid"></param>
		/// <param Name="status"></param>
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
            ResourceServices mgr = new ResourceServices();
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
			status.DocumentOwnedBy = item.documentOwnedBy;

			if ( item.documentPublishedBy != null )
			{
				if ( item.documentOwnedBy == null || ( item.documentPublishedBy != item.documentOwnedBy ) )
					status.DocumentPublishedBy = item.documentPublishedBy;
			}
			else
			{
				//will need to check elsewhere
				//OR as part of import check if existing one had 3rd party publisher
			}
			string payload = item.DecodedResource.ToString();
			string envelopeIdentifier = item.EnvelopeIdentifier;
			string ctdlType = RegistryServices.GetResourceType( payload );
			string ct = RegistryServices.GetGraphPrimaryType( payload );

			string envelopeUrl = RegistryServices.GetEnvelopeUrl( envelopeIdentifier );
			LoggingHelper.DoTrace( 5, "		envelopeUrl: " + envelopeUrl );
			LoggingHelper.WriteLogFile( UtilityManager.GetAppKeyValue( "logFileTraceLevel", 5 ), item.EnvelopeCtid + "_Collection", payload, "", false );
			//input = JsonConvert.DeserializeObject<InputGraph>( item.DecodedResource.ToString() );

			//InputEntity framework = GetFramework( input.Graph );
			//LoggingHelper.DoTrace( 5, "		framework Name: " + framework.Name.ToString() );
			status.EnvelopeId = envelopeIdentifier;
			//just store input for now
			return Import( payload, status );

			//return true;
		} //
        #endregion

        /// <summary>
        /// Import a Collection
        /// </summary>
        /// <param Name="payload">Registry Envelope DecodedResource</param>
        /// <param Name="status"></param>
        /// <returns></returns>

        public bool Import( string payload, SaveStatus status )
		{
			LoggingHelper.DoTrace( 7, "ImportCollections - entered." );
			List<string> messages = new List<string>();
			MappingHelperV3 helper = new MappingHelperV3( thisEntityTypeId );
			bool importSuccessfull = true;
			DateTime started = DateTime.Now;
			InputResource input = new InputResource();
            ResourceServices mgr = new ResourceServices();
			InputCompetency comp = new InputCompetency();
			var mainEntity = new Dictionary<string, object>();
			//
			var competencies = new List<InputCompetency>();
			var collectionMembers = new List<InputCollectionMember>();
			//not sure if these will actually be possible, may just use hasMember for others
			var jobMembers = new List<RJ.Job>();
			var loppMembers = new List<RJ.LearningOpportunityProfile>();
			//used for resources to exclude from hasMember MapEntityReferenceGuids method 
			//CTIDs or URIs??
			var hasMemberExcludeList = new List<string>();

			JArray graphList = RegistryServices.GetGraphList( payload );
			//??
			var glist = JsonConvert.SerializeObject( graphList );
			var bnodes = new List<BNode>();
			int cntr = 0;
			//probably should consider possibility of mixed node types
			int nodeCount = 0;
			foreach ( var item in graphList )
			{
				cntr++;
				//
				var main = item.ToString();
				RegistryObject mro = new RegistryObject( main );
				if ( mro.CtdlType == "ceterms:Collection" || mro.CtdlType == "Collection" )
				{
					input = JsonConvert.DeserializeObject<InputResource>( main );
				}
				else
				{
					nodeCount++;
					//collection member will be a bnode
					var child = item.ToString();
					RegistryObject ro = new RegistryObject( child );
					if ( ro == null )
					{
						//??
						continue;
					}
					if (ro.CtdlId.IndexOf( "_:" ) > -1 )
                    {

                    }
					
					//bnodes can be many things for this	
					switch (ro.CtdlType)
                    {
						case "CollectionMember":
						case "ceterms:CollectionMember":
							{
								collectionMembers.Add( JsonConvert.DeserializeObject<InputCollectionMember>( child ) );
								break;
                        }
						case "Competency":
						case "ceasn:Competency":
							{
								//will need to distinguish between the full cmps and via blank nodes. The absense of a CTID should be enough
								competencies.Add( JsonConvert.DeserializeObject<InputCompetency>( child ) );
								break;
							}
						case "Course":
						case "LearningProgram":
						case "LearningOpportunity":
						case "ceterms:Course":
						case "ceterms:LearningProgram":
						case "ceterms:LearningOpportunity":
							{
								loppMembers.Add( JsonConvert.DeserializeObject<RJ.LearningOpportunityProfile>( child ) );
								break;
							}
						case "ceterms:Job":
						case "Job":
							{
								jobMembers.Add( JsonConvert.DeserializeObject<RJ.Job>( child ) );
								break;
							}
						case "ceterms:Occupation":
						case "Occupation":
							{
								break;
							}
						case "ceterms:Task":
						case "Task":
							{
								break;
							}
						case "ceterms:WorkRole":
						case "WorkRole":
							{
								break;
							}
						default: 
						{
							LoggingHelper.DoTrace( 1, thisClassName + string.Format(".Import. Collection: '{0}' Unhandled node type: {1}.", input.CTID, ro.CtdlType ));
							//could add to bnodes regardless?
							bnodes.Add( JsonConvert.DeserializeObject<BNode>( child ) );
							break;
						}
					}
				}
			}

			//try
			//{
			//input = JsonConvert.DeserializeObject<InputGraph>( item.DecodedResource.ToString() );
			helper.entityBlankNodes = bnodes;
			helper.CurrentEntityCTID = input.CTID;
			helper.CurrentEntityName = input.Name.ToString();
			string ctid = input.CTID;
			try
			{
				status.Ctid = ctid;
				status.ResourceURL = input.CtdlId;
				LoggingHelper.DoTrace( 5, "		ctid: " + ctid );
				LoggingHelper.DoTrace( 5, "		@Id: " + input.CtdlId );
				LoggingHelper.DoTrace( 5, "		Name: " + input.Name.ToString() );

				var org = new MC.Organization();
				string orgCTID = "";
				string orgName = "";
				List<string> publisher = input.OwnedBy;
				//20-06-11 - need to get publisher, owner where possible
				//	include an org reference with Name, swp, and??
				//should check creator first? Or will publisher be more likely to have an account Ctid?
				//22-09-16 mp - this is a copy from frameworks and should be N/A here as an owner is requrired
				if ( publisher != null && publisher.Count() > 0 )
				{
					orgCTID = ResolutionServices.ExtractCtid( publisher[0] );
					//look up org Name
					org = OrganizationManager.GetSummaryByCtid( orgCTID );
				}
				if ( status.DoingDownloadOnly )
					return true;

				//add/updating Collection
				//21-02-22 HUH - WHY ARE WE USING ef here instead of output


				if ( !DoesEntityExist( input.CTID, ref output ) )
				{
					//set the rowid now, so that can be referenced as needed
					//output.RowId = Guid.NewGuid();
					output.RowId = Guid.NewGuid();
					LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".Import(). Record was NOT found using CTID: '{0}'", input.CTID ) );
				}
				else
				{
					LoggingHelper.DoTrace( 6, string.Format( thisClassName + ".Import(). Found record: '{0}' using CTID: '{1}'", input.Name, input.CTID ) );
				}

				helper.currentBaseObject = output;

				//store graph???
				output.CollectionGraph = glist;

				CurrentCollection = output.Name = helper.HandleLanguageMap( input.Name, output, "Name" );
				output.Description = helper.HandleLanguageMap( input.Description, output, "Description" );
				output.SubjectWebpage = input.SubjectWebpage;
				output.CTID = input.CTID;
				if ( org != null && org.Id > 0 )
				{
					orgName = org.Name;
					output.OrganizationId = org.Id;
					helper.CurrentOwningAgentUid = org.RowId;
				}

				//helper.MapInLanguageToTextValueProfile( input.InLanguage, "Collection.InLanguage.CTID: " + ctid );

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
						var statusMsg = "";
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
				//additions

				//=== if any messages were encountered treat as warnings for now
				if ( messages.Count > 0 )
					status.SetMessages( messages, true );
				//just in case check if entity added since start

				//mapping properties
				output.InLanguageCodeList = helper.MapInLanguageToTextValueProfile( input.InLanguage, "Collection.InLanguage. CTID: " + ctid );
				output.CredentialRegistryId = status.EnvelopeId;
				//note need to set output.OwningAgentUid to the first entry
				output.OwnedBy = helper.MapOrganizationReferenceGuids( "Collection.OwnedBy", input.OwnedBy, ref status );
				if ( output.OwnedBy != null && output.OwnedBy.Count > 0 )
				{
					output.PrimaryAgentUID = output.OwnedBy[0];
					helper.CurrentOwningAgentUid = output.OwnedBy[0];
				}
				else
				{
					//add warning?
				}
				status.CurrentDataProvider = helper.CurrentOwningAgentUid;
				//list of concepts, that may or may not be from registry
				output.Classification = input.Classification;
				//resolve now or in the manager?
				if ( output.Classification!= null && output.Classification.Count > 0)
                {

                }
				output.CodedNotation = input.CodedNotation;
				output.CollectionType = helper.MapCAOListToEnumermation( input.CollectionType );

				output.DateEffective = input.DateEffective;
				output.ExpirationDate = input.ExpirationDate;
				//TBD - pending fix for issue from CaSS
				output.Keyword = helper.MapToTextValueProfile( input.Keyword, output, "Keyword" );
				output.License = input.License;
				output.LifeCycleStatusType = helper.MapCAOToEnumermation( input.LifeCycleStatusType );
				output.MembershipCondition = helper.FormatConditionProfile( input.MembershipCondition, ref status );
				//
				//Occupation, Industries, Instructional Program
				output.IndustryType = helper.MapCAOListToCAOProfileList( input.IndustryType );
				//InstructionalProgramTypes
				output.InstructionalProgramType = helper.MapCAOListToCAOProfileList( input.InstructionalProgramType );				
				output.OccupationType = helper.MapCAOListToCAOProfileList( input.OccupationType );
				//Note if competencies are present, then we may not need this?
				//maybe want a variation to check against a list of CTIDs or URIs?
				//same for collectionMembers
				if (nodeCount != input.HasMember?.Count)
                {
					//action:
					//if there are no collection members, then need to process HasMember as collection members 
					//or where a registry URL, and not bnode URI
                }

				if ( competencies.Count == input.HasMember?.Count )
				{
					//just competencies, can skip
				}
				else if ( collectionMembers.Count == input.HasMember?.Count )
				{
					//just collectionMembers, can skip hasMember
					//dangerous? 
				}
				else
				{
					//could be thousands of members of an ETPL
					//may want the CTIDs here and store in collection member
					//should not be blank nodes, but could have a mix of hasMember and collection member. What about mixed collections?
					//23-03-14 MP - note that com???
					output.HasMemberImport = helper.MapEntityReferenceGuids( "Collection.HasMember", input.HasMember, 0, ref status );
					if ( output.HasMemberImport?.Count > 0 )
					{
						foreach ( var item in output.HasMemberImport )
						{
							//look up entityType
							var cacheItem = EntityManager.EntityCacheGetByGuid( item );
							if ( cacheItem != null && cacheItem.Id > 0 )
							{
								output.EntityTypeId = cacheItem.EntityTypeId;
								var cmbr = new OutputCollectionMember()
								{
									Name = cacheItem.Name,
									ProxyFor = cacheItem.CTID,
									EntityTypeId = cacheItem.EntityTypeId,
								};
								output.CollectionMember.Add( cmbr );
							} else
                            {
								//error
                            }							
						}
					}
				}

				//
				output.Subject = helper.MapCAOListToTextValueProfile( input.Subject, CodesManager.PROPERTY_CATEGORY_SUBJECT );
				//collectionMember
				if (collectionMembers?.Count > 0)
                {
					foreach (var item in collectionMembers)
                    {
						var cmbr = new OutputCollectionMember()
						{
							Name = helper.HandleLanguageMap( item.Name, "Collection.Name", false ),
							Description = helper.HandleLanguageMap( item.Description, "Collection.Name", false ),
							StartDate = item.StartDate,
							EndDate = item.EndDate,
							ProxyFor = ResolutionServices.ExtractCtid( item.ProxyFor ) //might be good to get the type!
						};
						//check if it exists
						if (output.Id > 0)
                        {
							var cmbrExists = CollectionMemberManager.Get( output.Id, cmbr.ProxyFor );
							if ( cmbrExists != null && cmbrExists.Id > 0)
								cmbr.Id = cmbrExists.Id;
                        }
						output.CollectionMember.Add( cmbr );
                    }
                }
				//competencies
				if ( UtilityManager.GetAppKeyValue( "importingFullCompetencies", true ) )
				{
					ImportCompetencies( output, competencies, helper, ref status );
				}
				//mapping duration
				TimeSpan duration = DateTime.Now.Subtract( started );
				if ( duration.TotalSeconds > 10 )
					LoggingHelper.DoTrace( 5, string.Format( "         WARNING Mapping Duration: {0:N2} seconds ", duration.TotalSeconds ) );
				DateTime saveStarted = DateTime.Now;
				//just in case check if entity added since start
				bool doingUpdate = true;
				if ( output.Id == 0 )
				{
                    ThisResource entity = new ThisResource();
					if ( ctid == null )
					{
						//NOT POSSIBLE
						status.AddError( string.Format( "Encountered a Collection ('{0}') that does not have a CTID", output.Name ) );
						doingUpdate = false;
						//entity = EntityServices.GetByNameandRegId( output.Name, output.CredentialRegistryId );
					}
					else
					{
						entity = ResourceServices.GetByCtid( ctid );
						if ( entity != null && entity.Id > 0 )
						{
							output.Id = entity.Id;
							output.RowId = entity.RowId;
						}
					}
				}
				//SAVE
				if ( doingUpdate )
				{
					importSuccessfull = mgr.Import( output, ref status );

					status.DocumentId = output.Id;
					status.DetailPageUrl = string.Format( "~/collection/{0}", output.Id );
					status.DocumentRowId = output.RowId;
				}
				//
				//just in case
				if ( status.HasErrors )
					importSuccessfull = false;

				//if record was added to db, add to/or set EntityResolution as resolved
				int ierId = new ImportManager().Import_EntityResolutionAdd( status.ResourceURL,
							ctid,
							CodesManager.ENTITY_TYPE_COLLECTION,
							output.RowId,
							output.Id,
							( output.Id > 0 ),
							ref messages,
							output.Id > 0 );
				//
				//language checks
				if ( input.InLanguage == null || input.InLanguage.Count() == 0 )
				{
					//document for followup
					//LoggingHelper.DoTrace( 5, "		Framework missing inLanguage: " + input.Name.ToString() );
				}


			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Import", string.Format( "Exception encountered for CTID: {0}", ctid ), false, "Framework Import exception" );
			}
			finally
			{

			}
			return importSuccessfull;
		}
		//Import full competency data
		public void ImportCompetencies(ThisResource resource, List<InputCompetency> input, MappingHelperV3 helper, ref SaveStatus status )
		{
			//Format the competency data for all of the competencies
			resource.ImportCompetencies = new List<ImportCompetency>();
			var competency = new ImportCompetency();
			try
			{
				foreach ( var item in input )
				{
					competency = new ImportCompetency();
					competency.CompetencyDetailJson = JsonConvert.SerializeObject( item, MappingHelperV3.GetJsonSettings() );
					competency.CtdlId = item.CtdlId;
					competency.CTID = item.CTID;
					competency.CompetencyText = helper.HandleLanguageMap( item.competencyText, resource, "competencyText" );
					competency.CompetencyLabel = helper.HandleLanguageMap( item.competencyLabel, resource, "CompetencyLabel" );
					competency.CompetencyCategory = helper.HandleLanguageMap( item.competencyCategory, resource, "CompetencyCategory" );
					if ( !string.IsNullOrWhiteSpace( item.dateCreated ) )
						competency.Created = DateTime.Parse( item.dateCreated );
					if ( !string.IsNullOrEmpty( item.isTopChildOf ) )
					{
						competency.IsTopChildOf = true;
					}
					//note: the db portion is using lastUpdated date to determine if something should be deleted (i.e. not part of the current download). 
					//		So we may want to have a separate dateModified in the db table?
					if ( !string.IsNullOrWhiteSpace( item.dateModified ) )
						competency.DateModified = DateTime.Parse( item.dateModified );
					//competency.LastUpdated = helper.MapDate( item.dateModified, "dateModified", ref status );

					resource.ImportCompetencies.Add( competency );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName, "ImportCompetencies", false );

			}
		}


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
