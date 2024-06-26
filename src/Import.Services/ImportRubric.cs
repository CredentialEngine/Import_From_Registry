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

using BNode = RA.Models.JsonV2.BlankNode;
using ResourceServices = workIT.Services.RubricServices;
using APIResourceServices = workIT.Services.API.RubricServices;
using InputResource = RA.Models.JsonV2.Rubric;
using InputRubricCriterion = RA.Models.JsonV2.RubricCriterion;
using InputRubricLevel = RA.Models.JsonV2.RubricLevel;
using InputCriterionLevel = RA.Models.JsonV2.CriterionLevel;

using JInput = RA.Models.JsonV2;
using ThisResource = workIT.Models.Common.Rubric;
using FAPI = workIT.Services.API;
namespace Import.Services
{
	public class ImportRubric
	{
		int entityTypeId = CodesManager.ENTITY_TYPE_RUBRIC;
		string thisClassName = "ImportRubrics";
		string ResourceType = "Rubric";
		ImportManager importManager = new ImportManager();
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
			//ResourceServices mgr = new ResourceServices();
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
			//ResourceServices mgr = new ResourceServices();
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
			int newImportId = importHelper.Add( item, entityTypeId, status.Ctid, importSuccessfull, importError, ref messages );
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
			string envelopeUrl = RegistryServices.GetEnvelopeUrl( envelopeIdentifier );
			LoggingHelper.DoTrace( 5, "		envelopeUrl: " + envelopeUrl );
			//Already done in  RegistryImport
			//LoggingHelper.WriteLogFile( UtilityManager.GetAppKeyValue( "logFileTraceLevel", 5 ), item.EnvelopeCetermsCtid + "_Pathway", payload, "", false );

			//just store input for now
			return Import( payload, envelopeIdentifier, status );

			//return true;
		} //

		public bool Import( string payload, string envelopeIdentifier, SaveStatus status )
		{
			/* checklist
			 * 
				Y		ceasn:abilityEmbodied
				Y		ceasn:comment				
				Y		ceasn:knowledgeEmbodied
				Y		ceasn:skillEmbodied
				Y		ceterms:classification
						ceasn:altCodedNotation
				Y		ceasn:codedNotation

				Y		ceterms:ctid
				Y		ceterms:description
				ceterms:HasRubric			
				Y		ceterms:identifier
				Y		ceterms:name
				Y		ceterms:versionIdentifier
			 * 
			 */
			LoggingHelper.DoTrace( 6, "ImportRubrics - entered." );
			List<string> messages = new List<string>();
			bool importSuccessfull = false;
			ResourceServices mgr = new ResourceServices();
			//
			InputResource input = new InputResource();
            var inputRubricCriterion = new List<InputRubricCriterion>();
            var inputRubricLevel = new List<InputRubricLevel>();
            var inputCriterionLevel = new List<InputCriterionLevel>();
            var mainEntity = new Dictionary<string, object>();
			//
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
                var main = item.ToString();
                RegistryObject mro = new RegistryObject( main );

                if ( mro.CtdlType == "ceasn:Rubric" || mro.CtdlType == "Rubric")
                {
					//may not use this. Could add a trace method
					mainEntity = RegistryServices.JsonToDictionary( main );
					input = JsonConvert.DeserializeObject<InputResource>( main );
				}
				else
				{
					if ( mro.CtdlId.IndexOf( "_:" ) > -1 )
					{
						switch ( mro.CtdlType )
						{
							case "RubricLevel":     //these are blank nodes
							{
								//
								inputRubricLevel.Add( JsonConvert.DeserializeObject<InputRubricLevel>( main ) );
								break;
							}
							case "CriterionLevel":
							{
								//
								inputCriterionLevel.Add( JsonConvert.DeserializeObject<InputCriterionLevel>( main ) );
								break;
							}

							default:
							{
								bnodes.Add( JsonConvert.DeserializeObject<BNode>( main ) );
								break;
							}
						}
					}
					else
					{


						//bnodes can be many things for this	
						switch ( mro.CtdlType )
						{
							case "RubricCriterion":
								{
                                    //
                                    inputRubricCriterion.Add( JsonConvert.DeserializeObject<InputRubricCriterion>( main ) );
                                    break;
								}
							case "RubricLevel":		//these are blank nodes
								{
                                    //
                                    inputRubricLevel.Add( JsonConvert.DeserializeObject<InputRubricLevel>( main ) );
                                    break;
								}
							case "CriterionLevel"://these are blank nodes
							{
                                    //
                                    inputCriterionLevel.Add( JsonConvert.DeserializeObject<InputCriterionLevel>( main ) );
                                    break;
								}

							default:
								{
									LoggingHelper.DoTrace( 1, thisClassName + string.Format( ".Import: '{0}' Unhandled node type: {1}.", input.CTID, mro.CtdlType ) );
									//could add to bnodes regardless?
									bnodes.Add( JsonConvert.DeserializeObject<BNode>( main ) );
									break;
								}
						}
					}
				}
			}

			MappingHelperV3 helper = new MappingHelperV3( entityTypeId );
			helper.entityBlankNodes = bnodes;
			helper.CurrentEntityCTID = input.CTID;
			helper.CurrentEntityName = input.Name.ToString();

			status.EnvelopeId = envelopeIdentifier;
			try
			{
				string ctid = input.CTID;
				status.ResourceURL = input.CtdlId;

				LoggingHelper.DoTrace( 5, "		name: " + input.Name.ToString() );
				LoggingHelper.DoTrace( 5, "		ctid: " + input.CTID );
				LoggingHelper.DoTrace( 6, "		@Id: " + input.CtdlId );
				status.Ctid = ctid;

				if ( status.DoingDownloadOnly )
					return true;


				if ( !DoesEntityExist( input.CTID, ref output ) )
				{

					//set the rowid now, so that can be referenced as needed
					output.RowId = Guid.NewGuid();
					LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".Import(). Record was NOT found using CTID: '{0}'", input.CTID ) );
				}
				else
				{
					LoggingHelper.DoTrace( 6, string.Format( thisClassName + ".Import(). Found record: '{0}' using CTID: '{1}'", input.Name, input.CTID ) );
				}
				helper.currentBaseObject = output;
				output.CTID = input.CTID;
				output.Name = helper.HandleLanguageMap( input.Name, output, "Name" );
				output.Description = helper.HandleLanguageMap( input.Description, output, "Description" );
				
				//NOTE: there is no asserting org for occupation, etc. It doesn't make sense. Add the data publisher as the primary
				if ( BaseFactory.IsValidCtid( status.DocumentOwnedBy ) )
				{
					output.PrimaryOrganization = OrganizationServices.GetSummaryByCtid( status.DocumentOwnedBy );
					if ( output.PrimaryOrganization != null && output.PrimaryOrganization.Id > 0 )
						output.PrimaryAgentUID = output.PrimaryOrganization.RowId;
				}
				else
				{
					//always should be here
				}			
				//TBD handling of referencing third party publisher
				helper.MapOrganizationPublishedBy( output, ref status );


				output.SubjectWebpage = input.SubjectWebpage;
				//warning this gets set to blank if doing a manual import by ctid
				output.CredentialRegistryId = envelopeIdentifier;

				//AbilityEmbodied - URI to existing:
				//		Competency Rubric Occupation Rubric Rubric
				//codeNotation
				output.CodedNotation = input.CodedNotation;
				output.AltCodedNotation = input.AltCodedNotation;
				//
				output.PublisherName = helper.HandleLanguageMapList( input.PublisherName, output, "PublisherName" );
				output.Publisher = helper.MapOrganizationReferenceGuids( "PathwaySet.OfferedBy", input.Publisher, ref status );
				//note need to set output.OwningAgentUid to the first entry
				output.Creator = helper.MapOrganizationReferenceGuids( "PathwaySet.OwnedBy", input.Creator, ref status );
				//output.Publisher = helper.MapEntityReferenceGuids( $"{ResourceType}.Publisher", input.Publisher, 0, ref status );
				//output.Creator = helper.MapEntityReferenceGuids( $"{ResourceType}.Creator", input.Creator, 0, ref status );

				output.InLanguageCodeList = helper.MapInLanguageToTextValueProfile( input.InLanguage, "LearningOpportunity.InLanguage. CTID: " + ctid );
				if ( input.InLanguage.Count > 0 )
				{
					helper.DefaultLanguage = input.InLanguage[0];
					output.InLanguage = input.InLanguage;
				}
				else
				{
					//OR set based on the first language
					helper.SetDefaultLanguage( input.Name, "Name" );
				}
				output.InCatalog = input.InCatalog;
				output.HasScope = helper.HandleLanguageMap( input.HasScope, output, "HasScope" );
				output.License = input.License;
				output.Rights = helper.HandleLanguageMap( input.Rights, output, "Rights" );

				output.DateCopyrighted = input.DateCopyrighted;
				output.DateCreated = input.DateCreated;
				output.DateModified = input.DateModified;
				output.DateValidFrom = input.DateValidFrom;
				output.DateValidUntil = input.DateValidUntil;

				output.DeliveryType = helper.MapCAOListToEnumermation( input.DeliveryType );
				output.AudienceType = helper.MapCAOListToEnumermation( input.AudienceType );
				output.AudienceLevelType = helper.MapCAOListToEnumermation( input.AudienceLevelType );
				output.EducationLevelType = helper.MapCAOListToEnumermation( input.EducationLevelType );
				output.EvaluatorType = helper.MapStringListToEnumeration( input.EvaluatorType);

				output.ConceptKeyword = helper.HandleLanguageMapList( input.ConceptKeyword, output, "ConceptKeyword" );
				output.Subject = helper.MapCAOListToTextValueProfile( input.Subject, CodesManager.PROPERTY_CATEGORY_SUBJECT );

				output.IndustryTypes = helper.MapCAOListToCAOProfileList( input.IndustryType );
				output.OccupationTypes = helper.MapCAOListToCAOProfileList( input.OccupationType );
				output.InstructionalProgramTypes = helper.MapCAOListToCAOProfileList( input.InstructionalProgramType );

				output.LifeCycleStatusType = helper.MapCAOToEnumermation( input.LifeCycleStatusType );
				output.LatestVersion = input.LatestVersion ?? "";
				output.PreviousVersion = input.PreviousVersion ?? "";
				output.NextVersion = input.NextVersion ?? "";
				output.VersionIdentifier = helper.MapIdentifierValueListInternal( input.VersionIdentifier );
				if ( output.VersionIdentifier != null && output.VersionIdentifier.Count() > 0 )
				{
					output.VersionIdentifierJson = JsonConvert.SerializeObject( output.VersionIdentifier, MappingHelperV3.GetJsonSettings() );
				}

				output.Classification = helper.MapEntityCTIDsToResourceSummary( input.Classification, CodesManager.ENTITY_TYPE_CONCEPT );

				output.TargetOccupationIds = helper.MapEntityReferences( $"{ResourceType}.TargetOccupation", input.TargetOccupation, CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE, ref status );

				output.HasCriterionCategorySetUid = helper.MapEntityReferenceGuid( "Rubric.HasCriterionCategorySet", input.HasCriterionCategorySet, 0, ref status );
				output.HasProgressionModelUid = helper.MapEntityReferenceGuid( "Rubric.HasProgressionModel", input.HasProgressionModel, 0, ref status );
				if ( !string.IsNullOrWhiteSpace(input.HasProgressionLevel))
				{
					output.HasProgressionLevelCTID = ResolutionServices.ExtractCtid( input.HasProgressionLevel );
				}


				//
				output.DerivedFromForImport = helper.MapEntityReferences( $"{ResourceType}..DerivedFrom", input.DerivedFrom, CodesManager.ENTITY_TYPE_RUBRIC, ref status );
				//HasRubric
				//if ( input.HasRubric != null && input.HasRubric.Count > 0 )
				//	output.HasRubric = helper.MapEntityReferences( "Job.HasRubric", input.HasRubric, CodesManager.ENTITY_TYPE_TASK_PROFILE, ref status );

				//
				output.Identifier = helper.MapIdentifierValueListInternal( input.Identifier );
				if ( output.Identifier != null && output.Identifier.Count() > 0 )
				{
					output.IdentifierJson = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
				}
				//

				//may need to save the job and then handle components
				//or do a create pending for hasDestination and any hasChild (actually already done by MapEntityReferenceGuids)

				//=== if any messages were encountered treat as warnings for now
				if ( messages.Count > 0 )
					status.SetMessages( messages, true );
                //parts
                if ( inputRubricCriterion?.Count > 0 )
                {
                    foreach ( var item in inputRubricCriterion )
                    {
                        var rc = new RubricCriterion()
                        {
                            Name = helper.HandleLanguageMap( item.Name, "RubricCriterion.Name", false ),
							Description= helper.HandleLanguageMap( item.Description, "RubricCriterion.Description", false ),
							CodedNotation = item.CodedNotation,
							CTID = item.CTID,
							ListID = item.ListID,
							Weight = item.Weight,
							TargetTaskIds= helper.MapEntityReferences( $"{ResourceType}.TargetTask", item.TargetTask, CodesManager.ENTITY_TYPE_TASK_PROFILE, ref status ),
							//HasProgressionLevel = helper.MapEntityReferenceGuids( $"{ResourceType}.HasProgressionLevel", item.HasProgressionLevel, 0, ref status ),
							TargetCompetency = helper.MapCAOListToCAOProfileList( item.TargetCompetency ),
							HasCriterionLevelUids = item.HasCriterionLevel,
							HasProgressionLevelCTID = ResolutionServices.ExtractCtid( input.HasProgressionLevel )
					};
                        //check if it exists?? Or will we do a delete all?
                       
                        output.RubricCriterion.Add( rc );
                    }
                }
				//
                if ( inputRubricLevel?.Count > 0 )
                {
                    foreach ( var item in inputRubricLevel )
                    {
						var rl = new RubricLevel()
						{
							Name = helper.HandleLanguageMap( item.Name, "RubricLevel.Name", false ),
							Description = helper.HandleLanguageMap( item.Description, "RubricLevel.Description", false ),
							CodedNotation = item.CodedNotation,
							ListID = item.ListID,
							HasProgressionLevelCTID = ResolutionServices.ExtractCtid( input.HasProgressionLevel ),
							HasCriterionLevelUids = item.HasCriterionLevel
						};
                        //check if it exists??

                        output.RubricLevel.Add( rl );
                    }
                }
                //
                if ( inputCriterionLevel?.Count > 0 )
                {
                    foreach ( var item in inputCriterionLevel )
                    {
						if ( Guid.TryParse( item.CtdlId.Replace( "_:", "" ), out Guid rowId ) )
						{
							var cl = new CriterionLevel()
							{
								RowId=rowId,
								BenchmarkLabel = helper.HandleLanguageMap( item.BenchmarkLabel, "CriterionLevel.BenchmarkLabel", false ),
								BenchmarkText = helper.HandleLanguageMap( item.BenchmarkText, "CriterionLevel.BenchmarkText", false ),
								CodedNotation = item.CodedNotation,
								ListID = item.ListID,
								Feedback = helper.HandleLanguageMap( item.Feedback, "CriterionLevel.Feedback", false ),
								Value = item.Value,
								MinValue = item.MinValue,
								MaxValue = item.MaxValue,
								Percentage = item.Percentage,
								MinPercentage = item.MinPercentage,
								MaxPercentage = item.MaxPercentage,

							};
							output.CriterionLevel.Add( cl );
						}
					}
                }
                //

                //adding common import pattern
                importSuccessfull = mgr.Import( output, ref status );
                

                //
                status.DocumentId = output.Id;
				status.DetailPageUrl = string.Format( "~/rubric/{0}", output.Id );
				status.DocumentRowId = output.RowId;
				//if record was added to db, add to/or set EntityResolution as resolved
				int ierId = new ImportManager().Import_EntityResolutionAdd( status.ResourceURL,
						ctid,
						entityTypeId,
						output.RowId,
						output.Id,
						( output.Id > 0 ),
						ref messages,
						output.Id > 0 );
				//just in case - not sure if applicable, as will want to do components if the occupation exists
				if ( status.HasErrors )
				{
					importSuccessfull = false;
					//email an error report, and/or add to activity log?
				}

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError(ex, $"{thisClassName}. Exception encountered in CTID: {output.CTID}");
			}

			return importSuccessfull;
		}


		//currently 
		public bool DoesEntityExist( string ctid, ref ThisResource entity )
		{
			bool exists = false;
			entity = ResourceServices.GetMinimumByCtid( ctid );
			if ( entity != null && entity.Id > 0 )
				return true;

			return exists;
		}

	}
}
