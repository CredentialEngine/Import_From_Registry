using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;

using ThisEntity = workIT.Models.Common.PathwayComponent;
using PC = workIT.Models.Common.PathwayComponent;
using DBEntity = workIT.Data.Tables.PathwayComponent;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

namespace workIT.Factories
{
	public class PathwayComponentManager : BaseFactory
	{
		static string thisClassName = "PathwayComponentManager";
		public static int componentActionOfNone = 0;
		public static int componentActionOfSummary = 1;
		public static int componentActionOfDeep = 2;

		#region persistance ==================

		/// <summary>
		/// add a PathwayComponent
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Save( ThisEntity entity, ref SaveStatus status )
		{
			bool isValid = true;
			var efEntity = new DBEntity();
			using ( var context = new EntityContext() )
			{
				try
				{
					if ( ValidateProfile( entity, ref status ) == false )
					{
						return false;
					}

					if ( entity.Id == 0 )
					{
						MapToDB( entity, efEntity );

						if ( entity.RowId == null || entity.RowId == Guid.Empty )
							efEntity.RowId = entity.RowId = Guid.NewGuid();
						else
							efEntity.RowId = entity.RowId;

						efEntity.CTID = "ce-" + efEntity.RowId.ToString().ToLower();
						efEntity.Created = efEntity.LastUpdated = System.DateTime.Now;

						context.PathwayComponent.Add( efEntity );

						// submit the change to database
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							entity.Id = efEntity.Id;

							UpdateParts( entity, ref status );

							return true;
						}
						else
						{
							//?no info on error
							status.AddError( "Error - the profile was not saved. " );
							string message = string.Format( "PathwayComponentManager.Add Failed", "Attempted to add a PathwayComponent. The process appeared to not work, but was not an exception, so we have no message, or no clue.PathwayComponent. PathwayComponent: {0}, createdById: {1}", entity.Name, entity.CreatedById );
							EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
						}
					}
					else
					{
						efEntity = context.PathwayComponent
								.SingleOrDefault( s => s.Id == entity.Id );

						if ( efEntity != null && efEntity.Id > 0 )
						{
							//for updates, chances are some fields will not be in interface, don't map these (ie created stuff)
							MapToDB( entity, efEntity );
							efEntity.EntityStateId = 3;
							if ( HasStateChanged( context ) )
							{
								efEntity.LastUpdated = System.DateTime.Now;
								int count = context.SaveChanges();
								//can be zero if no data changed
								if ( count >= 0 )
								{
									isValid = true;
								}
								else
								{
									//?no info on error
									status.AddError( "Error - the update was not successful. " );
									string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a PathwayComponent. The process appeared to not work, but was not an exception, so we have no message, or no clue. PathwayComponentId: {0}, Id: {1}, updatedById: {2}", entity.Id, entity.Id, entity.LastUpdatedById );
									EmailManager.NotifyAdmin( thisClassName + ". PathwayComponent_Update Failed", message );
								}
							}
							//continue with parts regardless
							UpdateParts( entity, ref status );
						}
						else
						{
							status.AddError( "Error - update failed, as record was not found." );
						}
					}

				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					//LoggingHelper.LogError( dbex, thisClassName + string.Format( ".ContentAdd() DBEntityValidationException, Type:{0}", entity.TypeId ) );
					string message = thisClassName + string.Format( ".Save() DBEntityValidationException, PathwayComponentId: {0}", entity.Id );
					foreach ( var eve in dbex.EntityValidationErrors )
					{
						message += string.Format( "\rEntity of type \"{0}\" in state \"{1}\" has the following validation errors:",
							eve.Entry.Entity.GetType().Name, eve.Entry.State );
						foreach ( var ve in eve.ValidationErrors )
						{
							message += string.Format( "- Property: \"{0}\", Error: \"{1}\"",
								ve.PropertyName, ve.ErrorMessage );
						}

						LoggingHelper.LogError( message, true );
					}
				}
				catch ( Exception ex )
				{
					var message = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".PathwayComponent_Add(), PathwayComponentId: {0}", entity.ParentId ) );
					status.AddError( string.Format( "Error encountered saving component. Type: {0}, Name: {1}, Error: {2}. ", entity.PathwayComponentType, entity.Name, message ) );
					isValid = false;
				}
			}

			return isValid;
		}
		public bool UpdateParts( ThisEntity entity, ref SaveStatus status )
		{
			bool isValid = true;
			//21-01-07 mparsons - Identifier will now be saved in the Json properties, not in Entity_IdentifierValue
			//new Entity_IdentifierValueManager().SaveList( entity.Identifier, entity.RowId, Entity_IdentifierValueManager.CREDENTIAL_Identifier, ref status, false );

			return isValid;
		}
		public int AddPendingRecord( Guid entityUid, string ctid, string registryAtId, ref string status )
		{
			DBEntity efEntity = new DBEntity();
			try
			{
				//var pathwayCTIDTemp = "ce-abcb5fe0-8fde-4f06-9d70-860cd5bdc763";
				using ( var context = new EntityContext() )
				{
					if ( !IsValidGuid( entityUid ) )
					{
						status = thisClassName + " - A valid GUID must be provided to create a pending entity";
						return 0;
					}
					//quick check to ensure not existing
					ThisEntity entity = GetByCtid( ctid );
					if ( entity != null && entity.Id > 0 )
						return entity.Id;

					//only add DB required properties
					//NOTE - an entity will be created via trigger
					efEntity.Name = "Placeholder until full document is downloaded";
					efEntity.Description = "Placeholder until full document is downloaded";
					efEntity.PathwayCTID = "";
					//temp
					efEntity.ComponentTypeId = 1;
					//realitically the component should be added in the same workflow
					efEntity.EntityStateId = 1;
					efEntity.RowId = entityUid;
					//watch that Ctid can be  updated if not provided now!!
					efEntity.CTID = ctid;
					efEntity.SubjectWebpage = registryAtId;

					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;

					context.PathwayComponent.Add( efEntity );
					int count = context.SaveChanges();
					if ( count > 0 )
						return efEntity.Id;

					status = thisClassName + " Error - the save was not successful, but no message provided. ";
				}
			}

			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddPendingRecord. entityUid:  {0}, ctid: {1}", entityUid, ctid ) );
				status = thisClassName + " Error - the save was not successful. " + message;

			}
			return 0;
		}
		public bool Delete( int Id, ref string statusMessage )
		{
			bool isValid = false;
			if ( Id == 0 )
			{
				statusMessage = "Error - missing an identifier for the PathwayComponent";
				return false;
			}
			using ( var context = new EntityContext() )
			{
				DBEntity efEntity = context.PathwayComponent
							.SingleOrDefault( s => s.Id == Id );

				if ( efEntity != null && efEntity.Id > 0 )
				{
					context.PathwayComponent.Remove( efEntity );
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						isValid = true;
					}
				}
				else
				{
					statusMessage = "Error - delete failed, as record was not found.";
				}
			}

			return isValid;
		}

		private bool ValidateProfile( PathwayComponent profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;
			if ( string.IsNullOrWhiteSpace( profile.Name ) )
			{
				status.AddError( string.Format("Error: A PathwayComponent Name is required.  CTID: {0}, Component: {1}", profile.CTID ?? "none?", profile.ComponentTypeId) );
			}
			//if ( string.IsNullOrWhiteSpace( profile.Description ) )
			//{
			//	status.AddError( "Error: A PathwayComponent Description is required." );
			//}


			return status.WasSectionValid;
		}
		#endregion

		#region == Retrieval =======================

		public static ThisEntity Get( int id, int childComponentsAction = 1 )
		{
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.PathwayComponent
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, childComponentsAction );
				}
			}

			return entity;
		}
		/// <summary>
		/// Get component by Guid
		/// </summary>
		/// <param name="id"></param>
		/// <param name="childComponentsAction">1-default of summary</param>
		/// <returns></returns>
		public static ThisEntity Get( Guid id, int childComponentsAction = 1 )
		{
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.PathwayComponent
						.SingleOrDefault( s => s.RowId == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, childComponentsAction );
				}
			}

			return entity;
		}
		/// <summary>
		/// Get a basic PathwayComponent by CTID
		/// </summary>
		/// <param name="ctid"></param>
		/// <returns></returns>
		public static ThisEntity GetByCtid( string ctid, int childComponentsAction = 1 )
		{

			PathwayComponent entity = new PathwayComponent();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;

			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				EM.PathwayComponent item = context.PathwayComponent
							.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower()
								);

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, childComponentsAction );
				}
			}

			return entity;
		}

		//
		public static List<ThisEntity> GetAllForPathway( string pathwayCTID, int childComponentsAction = 2 )
		{
			var output = new List<ThisEntity>();
			var entity = new ThisEntity();

			using ( var context = new EntityContext() )
			{
				var list = context.PathwayComponent
							.Where( s => s.PathwayCTID == pathwayCTID )
							.ToList();
				foreach ( var item in list )
				{
					entity = new ThisEntity();
					//when called via a pathway getAll, the subcomponents will be useally lists, and the detailed component will be in the hasPart
					MapFromDB( item, entity, childComponentsAction );
					output.Add( entity );
				}
			}

			return output;
		}
		//


		/// <summary>
		/// 
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="childComponentsAction">0-none; 1-summary; 2-deep </param>
		public static void MapFromDB( DBEntity from, ThisEntity to, int childComponentsAction = 1 )
		{

			to.Id = from.Id;
			to.RowId = from.RowId;
			to.EntityStateId = from.EntityStateId < 1 ? 1 : from.EntityStateId;
			to.CTID = from.CTID;
			to.Name = from.Name;
			to.Description = from.Description;
			to.PathwayCTID = from.PathwayCTID;
			//if ( from.Entity_HasPathwayComponent != null && from.Entity_HasPathwayComponent.Count > 0 )
			//{
			//	//
			//}

			var relatedEntity = EntityManager.GetEntity( to.RowId, false );
			if ( relatedEntity != null && relatedEntity.Id > 0 )
				to.EntityLastUpdated = relatedEntity.LastUpdated;

			//need to get parent pathway?
			//to.IsDestinationComponentOf = Entity_PathwayComponentManager.GetPathwayForComponent( to.Id, PathwayComponent.PathwayComponentRelationship_HasDestinationComponent );

			//ispartof. Should be single, but using list for flexibility?
			//actually force one, as we are using pathway identifier an external id for a unique lookup
			//may not want to do this every time?
			//to.IsPartOf = Entity_PathwayComponentManager.GetPathwayForComponent( to.Id, PathwayComponent.PathwayComponentRelationship_HasPart );

			//may want to get all and split out
			if ( childComponentsAction == 2 )
			{
				to.AllComponents = Entity_PathwayComponentManager.GetAll( to.RowId, componentActionOfSummary );
				foreach ( var item in to.AllComponents )
				{
					if ( item.ComponentRelationshipTypeId == PC.PathwayComponentRelationship_HasChild )
						to.HasChild.Add( item );
					else if ( item.ComponentRelationshipTypeId == PC.PathwayComponentRelationship_IsChildOf )
						to.IsChildOf.Add( item );
					else if ( item.ComponentRelationshipTypeId == PC.PathwayComponentRelationship_Preceeds )
						to.Preceeds.Add( item );
					else if ( item.ComponentRelationshipTypeId == PC.PathwayComponentRelationship_Prerequiste )
						to.Prerequisite.Add( item );
				}
				//child components - details of condition, but summary of components
				to.HasCondition = PathwayComponentConditionManager.GetAll( to.Id, true );
			}

			//to.CodedNotation = from.CodedNotation;
			to.ComponentCategory = from.ComponentCategory;
			to.ComponentTypeId = from.ComponentTypeId;
			if ( from.Codes_PathwayComponentType != null && from.Codes_PathwayComponentType.Id > 0 )
			{
				to.PathwayComponentType = from.Codes_PathwayComponentType.Title;
			}
			else
			{
				to.PathwayComponentType = GetComponentType( to.ComponentTypeId );
			}
			//will be validated before getting here!
			to.CredentialType = from.CredentialType;
			if ( !string.IsNullOrWhiteSpace( to.CredentialType) && to.CredentialType.IndexOf("ctdl/terms") > 0)
			{
				int pos = to.CredentialType.IndexOf( "ctdl/terms" );
				to.CredentialType = to.CredentialType.Substring( pos + 11 );
			}

			//not sure if this will just be a URI, or point to a concept
			//if a concept, would probably need entity.hasConcept
			//to.HasProgressionLevel = from.HasProgressionLevel;
			//if ( !string.IsNullOrWhiteSpace( from.HasProgressionLevel ) )
			//{
			//	to.ProgressionLevel = ConceptSchemeManager.GetByConceptCtid( to.HasProgressionLevel );
			//	to.HasProgressionLevelDisplay = to.ProgressionLevel.PrefLabel;
			//}
			//20-10-28 now storing separated list
			if ( !string.IsNullOrWhiteSpace( from.HasProgressionLevel ) )
			{
				string[] array = from.HasProgressionLevel.Split( '|' );
				if ( array.Count() > 0 )
				{
					foreach ( var i in array )
					{
						if ( !string.IsNullOrWhiteSpace( i ) )
						{
							var pl = ConceptSchemeManager.GetByConceptCtid( i );
							to.ProgressionLevels.Add( pl );

							to.HasProgressionLevelDisplay += pl.PrefLabel + ", ";
						}
					}
					to.HasProgressionLevelDisplay.Trim().TrimEnd( ',' );
				}
			}

			to.ProgramTerm = from.ProgramTerm;
			to.SubjectWebpage = from.SubjectWebpage;
			to.SourceData = from.SourceData;

			//where to store ComponentDesignation - textvalue
			//Json
			if ( !string.IsNullOrEmpty( from.Properties ) )
			{
				PathwayComponentProperties pcp = JsonConvert.DeserializeObject<PathwayComponentProperties>( from.Properties );
				if ( pcp != null )
				{
					//unpack ComponentDesignation
					to.ComponentDesignationList = pcp.ComponentDesignationList;
					//credit value
					to.CreditValue = pcp.CreditValue;
					//this is now QuantitativeValue
					to.PointValue = pcp.PointValue;

					to.Identifier = new List<IdentifierValue>();
					if ( pcp.Identifier != null )
						to.Identifier = pcp.Identifier;
					if ( pcp.SourceCredential != null && pcp.SourceCredential.Id > 0 )
					{
						to.SourceCredential = pcp.SourceCredential;
						to.SourceData = "";
					}
					if ( pcp.SourceAssessment != null && pcp.SourceAssessment.Id > 0 )
					{
						to.SourceAssessment = pcp.SourceAssessment;
						to.SourceData = "";
					}
					if ( pcp.SourceLearningOpportunity != null && pcp.SourceLearningOpportunity.Id > 0 )
					{
						to.SourceLearningOpportunity = pcp.SourceLearningOpportunity;
						to.SourceData = "";
					}
				}
			}

			//
			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime )from.Created;
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime )from.LastUpdated;

		}
		public static void MapToDB( ThisEntity from, DBEntity to )
		{

			to.Id = from.Id;
			if ( to.Id < 1 )
			{

				//will need to be carefull here, will this exist in the input??
				//there could be a case where an external Id was added to bulk upload for an existing record
				to.PathwayCTID = from.PathwayCTID;
			}
			else
			{

				//don't map rowId, CTID, or dates as not on form
				//to.RowId = from.RowId;
				to.Name = from.Name;
				to.Description = from.Description;
				//to.CodedNotation = from.CodedNotation;
				to.EntityStateId = 3;
				to.ComponentCategory = from.ComponentCategory;
				to.ComponentTypeId = GetComponentTypeId( from.PathwayComponentType );
				if (string.IsNullOrWhiteSpace( to.PathwayCTID ) )
					to.PathwayCTID = from.PathwayCTID;
				//to.ComponentTypeId = from.ComponentTypeId;
				//will be validated before getting here!
				to.CredentialType = from.CredentialType;

				//to.ExternalIdentifier = from.ExternalIdentifier;
				//not sure if this will just be a URI, or point to a concept
				//if a concept, would probably need entity.hasConcept
				//to.HasProgressionLevel = from.HasProgressionLevels;
				if ( from.HasProgressionLevels.Any() )
				{
					to.HasProgressionLevel = string.Join( "|", from.HasProgressionLevels.ToArray() );
				}
				else
					to.HasProgressionLevel = null;

				//need to change ??
				//to.IsDestinationComponentOf = from.IsDestinationComponentOf;
				//this is now in JsonProperties
				//to.PointValue = from.PointValueOld;
				to.ProgramTerm = from.ProgramTerm;
				to.SubjectWebpage = from.SubjectWebpage;
				to.SourceData = from.SourceData;

				to.Properties = JsonConvert.SerializeObject( from.JsonProperties );
			}


		}

		public static int GetComponentTypeId( string componentType )
		{
			if ( string.IsNullOrWhiteSpace( componentType ) )
				return 1;

			int componentTypeId = 0;
			switch ( componentType.Replace( "ceterms:", "" ).ToLower() )
			{
				case "assessmentcomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Assessment;
					break;
				case "basiccomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Basic;
					break;
				case "cocurricularcomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Cocurricular;
					break;
				case "competencycomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Competency;
					break;
				case "coursecomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Course;
					break;
				case "credentialcomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Credential;
					break;
				case "extracurricularcomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Extracurricular;
					break;
				case "jobcomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Job;
					break;
				case "selectioncomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Selection;
					break;
				case "workexperiencecomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Workexperience;
					break;
				//
				default:
					componentTypeId = 0;
					break;
			}

			return componentTypeId;
		}

		public static string GetComponentType( int componentTypeId )
		{
			string componentType = "";
			switch ( componentTypeId )
			{
				case 1:
					componentType = PC.AssessmentComponent;
					break;
				case 2:
					componentType = PC.BasicComponent;
					break;
				case 3:
					componentType = PC.CocurricularComponent;
					break;
				case 4:
					componentType = PC.CompetencyComponent;
					break;
				case 5:
					componentType = PC.CourseComponent;
					break;
				case 6:
					componentType = PC.CredentialComponent;
					break;
				case 7:
					componentType = PC.ExtracurricularComponent;
					break;
				case 8:
					componentType = PC.JobComponent;
					break;
				case 9:
					componentType = PC.WorkExperienceComponent;
					break;
				case 10:
					componentType = PC.SelectionComponent;
					break;
				//
				default:
					componentType = "unexpected: " + componentTypeId.ToString();
					break;
			}

			return componentType;
		}
		#endregion

	}
}
