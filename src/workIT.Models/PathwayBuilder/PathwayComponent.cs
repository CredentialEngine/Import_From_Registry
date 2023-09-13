using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using workIT.Models.Common;

namespace workIT.Models.API.Models
{
	public class PathwayComponent
	{
		public int Id { get; set; }
		public string CTID { get; set; }
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
		/// Helper property enable a unique external identifier by pathway since we don't have organization at this level
		/// ???why not ctid
		/// </summary>
		public Guid PathwayIdentifier { get; set; }
		//OR
		public string PathwayCTID { get; set; }

		/// <summary>
		/// may replace with IdentifierValue
		/// </summary>
		public List<IdentifierValue> Identifier { get; set; } = new List<IdentifierValue>();

		/// <summary>
		/// Label identifying the category to further distinguish one component from another as designated by the promulgating body.
		/// Examples may include "Required", "Core", "General Education", "Elective", etc.
		/// </summary>
		public List<string> ComponentDesignationList { get; set; } = new List<string>();
		//public string ComponentDesignation { get; set; }
		/// <summary>
		/// PathwayComponent Name 
		/// Required?
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// PathwayComponent Description 
		/// Required
		/// </summary>
		public string Description { get; set; }

		//public List<PathwayComponent> AllComponents { get; set; } = new List<PathwayComponent>();

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
		public List<ComponentCondition> HasCondition { get; set; } = new List<ComponentCondition>();


		/// <summary>
		/// Concept in a ProgressionModel concept scheme
		/// URI
		/// </summary>
		public string HasProgressionLevel { get; set; }
		public List<string> HasProgressionLevels { get; set; } = new List<string>();

		public string HasProgressionLevelDisplay { get; set; }
		//public Concept ProgressionLevel { get; set; } = new Concept();
		public List<Concept> ProgressionLevels { get; set; } = new List<Concept>();
		/// <summary>
		/// The referenced resource is higher in some arbitrary hierarchy than this resource.
		/// Provide the CTID or the full URI for the target environment. 
		/// ceterms:PathwayComponent
		/// </summary>
		public List<PathwayComponent> IsChildOf { get; set; } = new List<PathwayComponent>();


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
		public List<PathwayComponent> Precedes { get; set; } = new List<PathwayComponent>();

		/// <summary>
		/// Resource(s) required as a prior condition to this resource.
		/// Provide the CTID or the full URI for the target environment. 
		/// ceterms:ComponentCondition
		/// </summary>
		public List<PathwayComponent> Prerequisite { get; set; } = new List<PathwayComponent>();
		/// <summary>
		/// Indicates the resource for which a pathway component or similar proxy resource is a stand-in.
		/// URL
		/// </summary>
		public string ProxyFor { get; set; }

		/// <summary>
		/// An indicator whether or not the pathway component being described is required for successful completion of its parent component
		/// </summary>
		public bool RequiredForParentCompletion { get; set; }

		/// <summary>
		/// URL to structured data representing the resource.
		/// The preferred data serialization is JSON-LD or some other serialization of RDF.
		/// TBD - must this be a registry URI. Also if true, can just provide a CTID
		/// URL
		/// </summary>
		public string SourceData { get; set; }		


		/// <summary>
		/// The webpage that describes this entity.
		/// URL
		/// </summary>
		//public string SubjectWebpage { get; set; }
		#endregion

		#region BasicComponent,	CocurricularComponent, ExtracurricularComponent 
		///// <summary>
		///// Component Category
		///// Identifies the type of PathwayComponent subclass not explicitly covered in the current array of PathwayComponent subclasses.
		///// Used by: 
		///// ceterms:BasicComponent,	ceterms:CocurricularComponent, ceterms:ExtracurricularComponent 
		///// </summary>
		//public string ComponentCategory { get; set; }

		#endregion


		#region CourseComponent
		///// <summary>
		///// ProgramTerm
		///// Categorization of a term sequence based on the normative time between entry into a program of study and its completion such as "1st quarter", "2nd quarter"..."5th quarter".
		///// </summary>
		//public string ProgramTerm { get; set; }

		#endregion

		#region CredentialComponent
		///// <summary>
		///// Type of credential such as badge, certification, bachelor degree.
		///// The credential type as defined in CTDL.
		///// Used by: 
		///// ceterms:CredentialComponent 
		///// </summary>
		//public string CredentialType { get; set; }
		//public int CredentialTypeId { get; set; }
		#endregion

		public Enumeration IndustryType { get; set; } = new Enumeration();

		public List<string> AlternativeIndustries { get; set; } = new List<string>();
		public Enumeration OccupationType { get; set; } = new Enumeration();

		//now alternativeOccupation
		public List<string> AlternativeOccupations { get; set; } = new List<string>();

		//public Enumeration InstructionalProgramType { get; set; } = new Enumeration();
		//public CodeItemResult InstructionalProgramResults { get; set; } = new CodeItemResult();
		//public List<TextValueProfile> AlternativeInstructionalProgramType { get; set; } = new List<TextValueProfile>();

		//prototype storing some properties as Json
		public string ComponentProperties { get; set; }
	}
	public class AssessmentComponent : PathwayComponent
	{
		public TopLevelEntityReference SourceAssessment { get; set; } = null;
	}
	/// <summary>
	/// BasicPathwayComponent
	/// </summary>
	public class BasicComponent : PathwayComponent
	{
		/// <summary>
		/// Component Category
		/// Identifies the type of PathwayComponent subclass not explicitly covered in the current array of PathwayComponent subclasses.
		/// Used by: 
		/// ceterms:BasicComponent,	ceterms:CocurricularComponent, ceterms:ExtracurricularComponent 
		/// </summary>
		public string ComponentCategory { get; set; }
	}
	public class CocurricularComponent : BasicComponent
	{
	}
	public class CompetencyComponent : PathwayComponent
	{
		public TopLevelEntityReference SourceCompetency { get; set; } = null;
	}
	public class CourseComponent : PathwayComponent
	{
		/// <summary>
		/// ProgramTerm
		/// Categorization of a term sequence based on the normative time between entry into a program of study and its completion such as "1st quarter", "2nd quarter"..."5th quarter".
		/// </summary>
		public string ProgramTerm { get; set; }

		public TopLevelEntityReference SourceLearningOpportunity { get; set; } = null;

	}
	public class CredentialComponent : PathwayComponent
	{
		/// <summary>
		/// Type of credential such as badge, certification, bachelor degree.
		/// The credential type as defined in CTDL.
		/// Used by: 
		/// ceterms:CredentialComponent 
		/// </summary>
		public string CredentialType { get; set; }
		public int CredentialTypeId { get; set; }

		public TopLevelEntityReference SourceCredential { get; set; } = null;
	}
	//
	public class ExtracurricularComponent : BasicComponent
	{
	}
	public class JobComponent : PathwayComponent
	{
	}
	public class WorkExperienceComponent : PathwayComponent
	{
	}
}