using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;

using BNode = RA.Models.JsonV2.BlankNode;
using EntityServices = workIT.Services.PathwayServices;
using InputComponent = RA.Models.JsonV2.PathwayComponent;
using InputEntity = RA.Models.JsonV2.Pathway;
using JInput = RA.Models.JsonV2;
using OutputComponent = workIT.Models.Common.PathwayComponent;
using ThisEntity = workIT.Models.Common.Pathway;

namespace Import.Services
{
	public class ImportPathways
	{
		int entityTypeId = CodesManager.ENTITY_TYPE_PATHWAY;
		string thisClassName = "ImportPathways";
		ImportManager importManager = new ImportManager();
		ThisEntity output = new ThisEntity();
		ImportServiceHelpers importHelper = new ImportServiceHelpers();

		int thisEntityTypeId = CodesManager.ENTITY_TYPE_PATHWAY;


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
			//EntityServices mgr = new EntityServices();
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
			//EntityServices mgr = new EntityServices();
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
			int newImportId = importHelper.Add( item, CodesManager.ENTITY_TYPE_PATHWAY, status.Ctid, importSuccessfull, importError, ref messages );
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
			LoggingHelper.WriteLogFile( UtilityManager.GetAppKeyValue( "logFileTraceLevel", 5 ), item.EnvelopeCetermsCtid + "_Pathway", payload, "", false );

			//just store input for now
			return Import( payload, envelopeIdentifier, status );

			//return true;
		} //

		public bool Import( string payload, string envelopeIdentifier, SaveStatus status )
		{
			LoggingHelper.DoTrace( 6, "ImportPathways - entered." );
			List<string> messages = new List<string>();
			bool importSuccessfull = false;
			EntityServices mgr = new EntityServices();
			//
			InputEntity input = new InputEntity();
			var inputComponents = new List<InputComponent>();
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
				if ( cntr == 1 )
				{
					var main = item.ToString();
					//may not use this. Could add a trace method
					mainEntity = RegistryServices.JsonToDictionary( main );
					input = JsonConvert.DeserializeObject<InputEntity>( main );
				}
				else
				{
					//may have blank nodes?
					var bn = item.ToString();
					bnodes.Add( JsonConvert.DeserializeObject<BNode>( bn ) );
					var child = item.ToString();
					if ( child.IndexOf( "_:" ) > -1 )
					{
						bnodes.Add( JsonConvert.DeserializeObject<BNode>( child ) );
						
					}
					else if ( child.IndexOf( "Component" ) > -1 )
					{
						inputComponents.Add( JsonConvert.DeserializeObject<InputComponent>( child ) );
					}
					else
					{
						//unexpected
						Dictionary<string, object> unexpected = RegistryServices.JsonToDictionary( child );
						object unexpectedType = unexpected[ "@type" ];
						status.AddError( "Unexpected document type" );
					}
				}
			}

			MappingHelperV3 helper = new MappingHelperV3( CodesManager.ENTITY_TYPE_PATHWAY );
			helper.entityBlankNodes = bnodes;
			helper.CurrentEntityCTID = input.CTID;
			helper.CurrentEntityName = input.Name.ToString();

			status.EnvelopeId = envelopeIdentifier;
			try
			{
				string ctid = input.CTID;
				string referencedAtId = input.CtdlId;

				LoggingHelper.DoTrace( 5, "		name: " + input.Name.ToString() );
				LoggingHelper.DoTrace( 6, "		url: " + input.SubjectWebpage );
				LoggingHelper.DoTrace( 5, "		ctid: " + input.CTID );
				LoggingHelper.DoTrace( 6, "		@Id: " + input.CtdlId );
				status.Ctid = ctid;

				if ( status.DoingDownloadOnly )
					return true;


				if ( !DoesEntityExist( input.CTID, ref output ) )
				{
					//TODO - perhaps create a pending pathway immediately 

					//set the rowid now, so that can be referenced as needed
					output.RowId = Guid.NewGuid();
					LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".ImportV3(). Record was NOT found using CTID: '{0}'", input.CTID ) );
				}
				else
				{
					LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".ImportV3(). Found record: '{0}' using CTID: '{1}'", input.Name, input.CTID ) );
				}
				helper.currentBaseObject = output;

				output.Name = helper.HandleLanguageMap( input.Name, output, "Name" );
				output.Description = helper.HandleLanguageMap( input.Description, output, "Description" );
				output.SubjectWebpage = input.SubjectWebpage;
				output.CTID = input.CTID;

				//warning this gets set to blank if doing a manual import by ctid
				output.CredentialRegistryId = envelopeIdentifier;

				//BYs - do owned and offered first
				output.OfferedBy = helper.MapOrganizationReferenceGuids( "Pathway.OfferedBy", input.OfferedBy, ref status );
				//note need to set output.OwningAgentUid to the first entry
				output.OwnedBy = helper.MapOrganizationReferenceGuids( "Pathway.OwnedBy", input.OwnedBy, ref status );
				if ( output.OwnedBy != null && output.OwnedBy.Count > 0 )
				{
					output.OwningAgentUid = output.OwnedBy[ 0 ];
					helper.CurrentOwningAgentUid = output.OwnedBy[ 0 ];
				}
				else
				{
					//add warning?
					if ( output.OfferedBy == null && output.OfferedBy.Count == 0 )
					{
						status.AddWarning( "document doesn't have an owning or offering organization." );
					}
				}
				//hasPart could contain all components. The API should have done a validation
				//not clear if necessary to do anything here
				//this would be a first step to create pending records?
				if ( input.HasPart != null && input.HasPart.Count() > 0 )
				{
					output.HasPartList = helper.MapEntityReferenceGuids( "Pathway.HasPart", input.HasPart, CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT, ref status );
				}
				//
				output.HasChildList = helper.MapEntityReferenceGuids( "Pathway.HasChild", input.HasChild, CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT, ref status );
				output.HasDestinationList = helper.MapEntityReferenceGuids( "Pathway.HasDestination", input.HasDestinationComponent, CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT, ref status );

				//has progression model
				//TODO - IMPORT CONCEPT SCHEMES
				// will need to check if related scheme has been imported. 
				output.ProgressionModelURI = ResolutionServices.ExtractCtid( input.HasProgressionModel);

				output.Keyword = helper.MapToTextValueProfile( input.Keyword, output, "Keyword" );
				output.Subject = helper.MapCAOListToTextValueProfile( input.Subject, CodesManager.PROPERTY_CATEGORY_SUBJECT );
				//Industries/occupations
				output.Industries = helper.MapCAOListToCAOProfileList( input.IndustryType );
				output.Occupations = helper.MapCAOListToCAOProfileList( input.OccupationType );

				//may need to save the pathway and then handle components
				//or do a create pending for hasDestination and any hasChild (actually already done by MapEntityReferenceGuids)

				//=== if any messages were encountered treat as warnings for now
				if ( messages.Count > 0 )
					status.SetMessages( messages, true );
				//components now or after save ?
				foreach ( var item in inputComponents )
				{
					var c = ImportComponent( item, output, bnodes, status );
					output.HasPart.Add( c );
				}//

				 //adding common import pattern
				importSuccessfull = mgr.Import( output, ref status );
				//
				status.DocumentId = output.Id;
				status.DetailPageUrl = string.Format( "~/pathway/{0}", output.Id );
				status.DocumentRowId = output.RowId;
				//if record was added to db, add to/or set EntityResolution as resolved
				int ierId = new ImportManager().Import_EntityResolutionAdd( referencedAtId,
						ctid,
						CodesManager.ENTITY_TYPE_PATHWAY,
						output.RowId,
						output.Id,
						( output.Id > 0 ),
						ref messages,
						output.Id > 0 );
				//just in case - not sure if applicable, as will want to do components if the pathway exists
				if ( status.HasErrors )
				{
					importSuccessfull = false;
					//email an error report, and/or add to activity log?
				}
				//
				//if ( output.Id > 0 )
				//{
				//	foreach ( var item in inputComponents )
				//	{
				//		var c=ImportComponent( item, output, bnodes, status );
				//		output.HasPart.Add( c );
				//	}//
				//	//call method to handle components
				//}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "Exception encountered in envelopeId: {0}", envelopeIdentifier ), false, "Pathway Import exception" );
			}

			return importSuccessfull;
		}

		//
		/// <summary>
		/// Handle component import
		/// TODO - should a save be done for each component or wait until the end
		/// </summary>
		/// <param name="input"></param>
		/// <param name="pathway"></param>
		/// <param name="bnodes"></param>
		/// <param name="status">TODO - do we want to continue using the pathway SaveStatus?</param>
		/// <returns></returns>
		public OutputComponent ImportComponent( InputComponent input, ThisEntity pathway, List<BNode> bnodes, SaveStatus status )
		{
			MappingHelperV3 helper = new MappingHelperV3( CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT );
			//do we need to reference blank nodes here? - if so pass to this method
			helper.entityBlankNodes = bnodes;
			helper.CurrentEntityCTID = input.CTID;
			helper.CurrentEntityName = input.Name.ToString();
			OutputComponent output = new OutputComponent();
			//
			LoggingHelper.DoTrace( 5, "======== Component ======== " );
			LoggingHelper.DoTrace( 5, "		type: " + input.PathwayComponentType.ToString() );
			LoggingHelper.DoTrace( 5, "		name: " + (input.Name ?? new JInput.LanguageMap( "componentNameMissing" ) ).ToString() );
			LoggingHelper.DoTrace( 5, "		ctid: " + input.CTID );
			LoggingHelper.DoTrace( 6, "		@Id: " + input.CtdlId );

			try
			{
				//add/updating Pathway
				if ( !DoesComponentExist( input.CTID, ref output ) )
				{
					//set the rowid now, so that can be referenced as needed
					//no, the guid comes from the resolving of entity references
					//actually OK, as earlier references would result in a pending record
					output.RowId = Guid.NewGuid();
				}
				helper.currentBaseObject = output;
				if (input.CTID == "ce-fa6c139f-0615-401f-9920-6ec8c445baca" )
				{

				}
				//initialize json properties
				output.JsonProperties = new PathwayComponentProperties();
				//
				output.PathwayComponentType = input.PathwayComponentType;
				output.Name = helper.HandleLanguageMap( input.Name, output, "Name" );
				output.Description = helper.HandleLanguageMap( input.Description, output, "Description" );
				output.SubjectWebpage = input.SubjectWebpage;
				output.SourceData = input.SourceData;

				if ( !string.IsNullOrWhiteSpace( output.SourceData ) && output.SourceData.IndexOf( "/resources/" ) > 0 )
				{
					var ctid = ResolutionServices.ExtractCtid( output.SourceData );
					if ( !string.IsNullOrWhiteSpace( ctid ) )
					{
						if ( output.PathwayComponentType.ToLower().IndexOf( "credential" ) > -1 )
						{
							var target = CredentialManager.GetByCtid( ctid );
							if ( target != null && target.Id > 0 )
							{
								//this approach 'buries' the cred from external references like credential in pathway
								output.SourceCredential = new TopLevelEntityReference()
								{
									Id = target.Id,
									Name = target.Name,
									Description = target.Description,
									CTID = target.CTID,
									SubjectWebpage = target.SubjectWebpage,
									//RowId = target.RowId
								};
								output.JsonProperties.SourceCredential = output.SourceCredential;
							}
						}
						else if ( output.PathwayComponentType.ToLower().IndexOf( "assessmentcomp" ) > -1 )
						{
							var target = AssessmentManager.GetByCtid( ctid );
							if ( target != null && target.Id > 0 )
							{
								//may not really need this, just the json
								output.SourceAssessment = new TopLevelEntityReference()
								{
									Id = target.Id,
									Name = target.Name,
									Description = target.Description,
									CTID = target.CTID,
									SubjectWebpage = target.SubjectWebpage,
									//RowId = target.RowId
								};
								output.JsonProperties.SourceAssessment = output.SourceAssessment;
							}
						}
						else if ( output.PathwayComponentType.ToLower().IndexOf( "coursecomp" ) > -1 )
						{
							var target = LearningOpportunityManager.GetByCtid( ctid );
							if ( target != null && target.Id > 0 )
							{
								//may not really need this, just the json
								output.SourceLearningOpportunity = new TopLevelEntityReference()
								{
									Id = target.Id,
									Name = target.Name,
									Description = target.Description,
									CTID = target.CTID,
									SubjectWebpage = target.SubjectWebpage,
									//RowId = target.RowId
								};
								output.JsonProperties.SourceLearningOpportunity = output.SourceLearningOpportunity;
							}
						}
					}
					

				}
				
				output.CTID = input.CTID;
				output.PathwayCTID = pathway.CTID;


				//output.CodedNotation = input.CodedNotation;
				output.Identifier = helper.MapIdentifierValueListInternal( input.Identifier );
				if ( output.Identifier != null && output.Identifier.Count() > 0 )
				{
					output.IdentifierJson = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
				}
				//
				output.ComponentDesignationList = helper.MapCAOListToList( input.ComponentDesignation );

				//
				output.CredentialType = input.CredentialType;
				output.CreditValue = helper.HandleValueProfileListToQVList( input.CreditValue, output.PathwayComponentType + ".CreditValue" );

				//TBD - how to handle. Will need to have imported the concept scheme/concept
				if ( input.HasProgressionLevel != null && input.HasProgressionLevel.Any())
				{
					foreach (var item in input.HasProgressionLevel )
					{
						output.HasProgressionLevels.Add( ResolutionServices.ExtractCtid( item ));
					}
				}
				
				output.PointValue = helper.HandleQuantitiveValue( input.PointValue, output.PathwayComponentType + ".PointValue" );

				//
				output.ProgramTerm = helper.HandleLanguageMap( input.ProgramTerm, output, "ProgramTerm" );
				//need to get relationshiptype to store-> this can be done by manager
				//3
				output.HasChildList = helper.MapEntityReferenceGuids( "PathwayComponent.HasChild", input.HasChild, CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT, ref status );
				//2
				output.HasIsChildOfList = helper.MapEntityReferenceGuids( "PathwayComponent.IsChildOf", input.IsChildOf, CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT, ref status );

				output.HasPrerequisiteList = helper.MapEntityReferenceGuids( "PathwayComponent.Prerequisite", input.Prerequisite, CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT, ref status );
				output.HasPreceedsList = helper.MapEntityReferenceGuids( "PathwayComponent.Preceeds", input.Preceeds, CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT, ref status );

				//populate JSON properties
				output.JsonProperties.ComponentDesignationList = output.ComponentDesignationList;
				output.JsonProperties.CreditValue = output.CreditValue;
				output.JsonProperties.Identifier = output.Identifier;
				output.JsonProperties.PointValue = output.PointValue;

				//
				if ( input.HasCondition != null && input.HasCondition.Count() > 0 )
				{
					output.HasCondition = new List<PathwayComponentCondition>();
					foreach ( var item in input.HasCondition )
					{
						//var jcc = JsonConvert.DeserializeObject<JInput.ComponentCondition>( item.ToString() );
						var cc = new PathwayComponentCondition();
						cc.Name = helper.HandleLanguageMap( item.Name, cc, "ComponentCondition.Name" );
						cc.Description = helper.HandleLanguageMap( item.Description, cc, "ComponentCondition.Description" );
						cc.RequiredNumber = item.RequiredNumber;
						cc.PathwayCTID = pathway.CTID;
						cc.HasTargetComponentList = helper.MapEntityReferenceGuids( "ComponentCondition.TargetComponent", item.TargetComponent, CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT, ref status );

						output.HasCondition.Add( cc );
					}
				}
			} catch (Exception ex)
			{
				LoggingHelper.LogError( ex, "ImportPathways.ImportComponent" );
				//status.AddError( string.Format( "ImportPathways.ImportComponent. ComponentType: {0}, Name: {1}, Message: {2}", output.ComponentTypeId, output.Name, ex.Message ) );
			}
			//then save
			return output;
		}

		//currently 
		public bool DoesEntityExist( string ctid, ref ThisEntity entity )
		{
			bool exists = false;
			entity = EntityServices.GetByCtid( ctid );
			if ( entity != null && entity.Id > 0 )
				return true;

			return exists;
		}
		public bool DoesComponentExist( string ctid, ref OutputComponent entity )
		{
			bool exists = false;
			entity = EntityServices.GetComponentByCtid( ctid );
			if ( entity != null && entity.Id > 0 )
				return true;

			return exists;
		}
	}
}
