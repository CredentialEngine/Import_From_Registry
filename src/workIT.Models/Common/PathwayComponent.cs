using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.ProfileModels;
namespace workIT.Models.Common
{
	/// <summary>
	/// Resource that serves as a defined point along the route of a Pathway which describes an objective and its completion requirements through reference to one or more instances of ComponentCondition.
	/// </summary>
	public class PathwayComponent : TopLevelObject
	{
		//use of constants?
		public static int PathwayComponentType_Assessment = 1;
		public static int PathwayComponentType_Basic = 2;
		public static int PathwayComponentType_Cocurricular = 3;
		public static int PathwayComponentType_Competency = 4;
		public static int PathwayComponentType_Course = 5;
		public static int PathwayComponentType_Credential = 6;
		public static int PathwayComponentType_Extracurricular = 7;
		public static int PathwayComponentType_Job = 8;
		public static int PathwayComponentType_Workexperience = 9;
		public static int PathwayComponentType_Selection = 10;

		public static string AssessmentComponent = "AssessmentComponent";
		public static string BasicComponent = "BasicComponent";
		public static string CocurricularComponent = "CocurricularComponent";
		public static string CompetencyComponent = "CompetencyComponent";
		public static string CourseComponent = "CourseComponent";
		public static string CredentialComponent = "CredentialComponent";
		public static string ExtracurricularComponent = "ExtracurricularComponent";
		public static string JobComponent = "JobComponent";
		public static string SelectionComponent = "SelectionComponent";
		public static string WorkExperienceComponent = "WorkExperienceComponent";
		public static string ComponentCondition = "ComponentCondition";


		//relationship is with Entity_HasPathwayComponent
		public static int PathwayComponentRelationship_HasDestinationComponent = 1;
		//probably not used for BU
		public static int PathwayComponentRelationship_IsChildOf = 2;
		public static int PathwayComponentRelationship_HasChild = 3;
		public static int PathwayComponentRelationship_Preceeds = 4;
		public static int PathwayComponentRelationship_Prerequiste = 5;
		public static int PathwayComponentRelationship_TargetComponent = 6;
		public static int PathwayComponentRelationship_HasPart = 7;

		public enum PathwayComponentTypes
		{
			UNKNOWN = 0,
			ASSESSMENT = 1,
			BASIC = 2,
			COCURRICULAR = 3,
			COMPETENCY = 4,
			COURSE = 5,
			CREDENTIAL = 6,
			EXTRACURRICULAR = 7,
			JOB = 8,
			WORKEXPERIENCE = 9,
			SELECTION = 10
		}

		public enum PathwayComponentRelationships
		{
			UNKNOWN = 0,
			HasDestinationComponent = 1,
			CHILDOF = 2,
			HASCHILD = 3,
			PRECEEDS = 4,
			PREREQUISITE = 5,
			TARGETCOMPONENT = 6
		}

		/// <summary>
		/// Type of PathwayComponent. 
		/// Valid values (with or without ceterms:) :
		/// ceterms:AssessmentComponent	
		/// ceterms:BasicComponent	
		/// ceterms:CocurricularComponent	
		/// ceterms:CompetencyComponent	
		/// ceterms:CourseComponent 	
		/// ceterms:CredentialComponent 	
		/// ceterms:ExtracurricularComponent 	
		/// ceterms:JobComponent 	
		/// ceterms:WorkExperienceComponent
		/// </summary>
		public string PathwayComponentType { get; set; }

		public int ComponentTypeId { get; set; }

		//this is the relationship to the parent
		public int ComponentRelationshipTypeId { get; set; }

		#region Common Properties
		//public string CTID { get; set; }

		/// <summary>
		/// Helper property enable easily retrieving all components for a pathway
		/// </summary>
		public string PathwayCTID { get; set; }

		/// <summary>
		/// may replace with IdentifierValue
		/// </summary>
		public string CodedNotation { get; set; } 
		//public List<IdentifierValue> IdentifierValue { get; set; } = new List<IdentifierValue>();

		/// <summary>
		/// Label identifying the category to further distinguish one component from another as designated by the promulgating body.
		/// Examples may include "Required", "Core", "General Education", "Elective", etc.
		/// </summary>
		public List<string> ComponentDesignationList { get; set; } = new List<string>();
		//public string ComponentDesignation { get; set; }


		public List<PathwayComponent> AllComponents { get; set; } = new List<PathwayComponent>();

		/// <summary>
		/// This property identifies a pathwayComponent(s) in the downward path.
		/// Provide the CTID or the full URI for the target environment. 
		/// ceterms:PathwayComponent
		/// </summary>
		public List<PathwayComponent> HasChild { get; set; } = new List<PathwayComponent>();

		/// <summary>
		/// Resource(s) that describes what must be done to complete a PathwayComponent, or part thereof, as determined by the issuer of the Pathway.
		/// Provide the CTID or the full URI for the target environment. 
		/// ceterms:ComponentCondition
		/// </summary>
		public List<PathwayComponentCondition> HasCondition { get; set; } = new List<PathwayComponentCondition>();


		/// <summary>
		/// Concept in a ProgressionModel concept scheme
		/// URI
		/// </summary>
		public List<string> HasProgressionLevels { get; set; } = new List<string>();
		public List<Concept> ProgressionLevels { get; set; } = new List<Concept>();
		public string HasProgressionLevelDisplay { get; set; }
		//public Concept ProgressionLevel { get; set; } = new Concept();
		/// <summary>
		/// The referenced resource is higher in some arbitrary hierarchy than this resource.
		/// Provide the CTID or the full URI for the target environment. 
		/// ceterms:PathwayComponent
		/// </summary>
		public List<PathwayComponent> IsChildOf { get; set; } = new List<PathwayComponent>();

		/// <summary>
		/// Identifier
		/// Definition:	Alphanumeric token that identifies this resource and information about the token's originating context or scheme.
		/// </summary>	
		public List<IdentifierValue> Identifier { get; set; } = new List<IdentifierValue>();
		//or could store this as json
		public string IdentifierJson { get; set; }

		/// <summary>
		/// Pathway for which this resource is the goal or destination.
		/// Like IsTopChildOf
		/// Provide the CTID or the full URI for the target environment. 
		/// ceterms:Pathway
		/// </summary>
		public List<Pathway> IsDestinationComponentOf { get; set; } = new List<Pathway>();
		public int IsDestinationComponentOfId { get; set; }
		public Pathway IsDestinationComponentOfPathway { get; set; } = new Pathway();
		/// <summary>
		/// This property identifies the Pathways of which it is a part. 
		/// 
		/// Provide the CTID or the full URI for the target environment. 
		/// Note use of helper property in the request class: 
		/// </summary>
		public List<Pathway> IsPartOf { get; set; } = new List<Pathway>();

		/// <summary>
		/// CreditValue
		/// A credit-related value.
		/// Used by: 
		/// ceterms:CourseComponent only 
		/// </summary>
		public List<QuantitativeValue> CreditValue { get; set; } = new List<QuantitativeValue>();
		/// <summary>
		/// Points associated with this resource, or points possible.
		/// Added Entity_QuantitativeValueManager for use
		/// </summary>
		public QuantitativeValue PointValue { get; set; } = new QuantitativeValue();
		public bool PointValueIsRange { get; set; }
		//public string PointValueJson { get; set; }

		/// <summary>
		/// Resource that logically comes after this resource.
		/// This property indicates a simple or suggested ordering of resources; if a required ordering is intended, use ceterms:prerequisite instead.
		/// Provide the CTID or the full URI for the target environment. 
		/// ceterms:ComponentCondition
		/// </summary>
		public List<PathwayComponent> Preceeds { get; set; } = new List<PathwayComponent>();

		/// <summary>
		/// Resource(s) required as a prior condition to this resource.
		/// Provide the CTID or the full URI for the target environment. 
		/// ceterms:ComponentCondition
		/// </summary>
		public List<PathwayComponent> Prerequisite { get; set; } = new List<PathwayComponent>();


		/// <summary>
		/// URL to structured data representing the resource.
		/// The preferred data serialization is JSON-LD or some other serialization of RDF.
		/// TBD - must this be a registry URI. Also if true, can just provide a CTID
		/// URL
		/// </summary>
		public string SourceData { get; set; }
		public TopLevelEntityReference SourceCredential { get; set; } = null;

		/// <summary>
		/// The webpage that describes this entity.
		/// URL
		/// </summary>
		//public string SubjectWebpage { get; set; }
		#endregion

		#region BasicComponent,	CocurricularComponent, ExtracurricularComponent 
		/// <summary>
		/// Component Category
		/// Identifies the type of PathwayComponent subclass not explicitly covered in the current array of PathwayComponent subclasses.
		/// Used by: 
		/// ceterms:BasicComponent,	ceterms:CocurricularComponent, ceterms:ExtracurricularComponent 
		/// </summary>
		public string ComponentCategory { get; set; }

		#endregion


		#region CourseComponent
		/// <summary>
		/// ProgramTerm
		/// Categorization of a term sequence based on the normative time between entry into a program of study and its completion such as "1st quarter", "2nd quarter"..."5th quarter".
		/// </summary>
		public string ProgramTerm { get; set; }

		#endregion

		#region CredentialComponent
		/// <summary>
		/// Type of credential such as badge, certification, bachelor degree.
		/// The credential type as defined in CTDL.
		/// Used by: 
		/// ceterms:CredentialComponent 
		/// </summary>
		public string CredentialType { get; set; }
		public int CredentialTypeId { get; set; }
		#endregion

		//prototype storing some properties as Json
		public string ComponentProperties{ get; set; }
		public PathwayComponentProperties JsonProperties { get; set; } = new PathwayComponentProperties();


		#region Import
		public List<Guid> HasChildList { get; set; } = new List<Guid>();
		public List<Guid> HasConditionList { get; set; } = new List<Guid>();
		public List<Guid> HasIsChildOfList { get; set; } = new List<Guid>();
		public List<Guid> HasPrerequisiteList { get; set; } = new List<Guid>();
		public List<Guid> HasPreceedsList { get; set; } = new List<Guid>();

		#endregion
	}

	/// <summary>
	/// Prototype storing some properties as Json
	/// </summary>
	public class PathwayComponentProperties
	{
		/// <summary>
		/// Label identifying the category to further distinguish one component from another as designated by the promulgating body.
		/// Examples may include "Required", "Core", "General Education", "Elective", etc.
		/// </summary>
		public List<string> ComponentDesignationList { get; set; } = new List<string>();

		/// <summary>
		/// CreditValue
		/// A credit-related value.
		/// Used by: 
		/// ceterms:CourseComponent only 
		/// </summary>
		public List<QuantitativeValue> CreditValue { get; set; } = new List<QuantitativeValue>();

		public List<IdentifierValue> Identifier { get; set; } = new List<IdentifierValue>();

		/// <summary>
		/// Points associated with this resource, or points possible.
		/// Added Entity_QuantitativeValueManager for use
		/// </summary>
		public QuantitativeValue PointValue { get; set; } = new QuantitativeValue();

		public TopLevelEntityReference SourceCredential { get; set; } = null;
	}

	public class Entity_HasPathwayComponent
	{
		public int Id { get; set; }
		public int EntityId { get; set; }
		public int PathwayComponentId { get; set; }
		public int ComponentRelationshipTypeId { get; set; }
		public string PathwayComponentName { get; set; }
		public System.DateTime Created { get; set; }
		public int CreatedById { get; set; }

		public virtual Entity Entity { get; set; }
		public virtual PathwayComponent PathwayComponent { get; set; }
	}
}
