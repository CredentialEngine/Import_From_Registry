using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;

namespace workIT.Models.PathwayBuilder
{
    public class PathwayWrapper
    {
        public Pathway Pathway { get; set; } = new Pathway();
        public List<PathwayComponent> PathwayComponents { get; set; } = new List<PathwayComponent>();
        public List<ProgressionModel> ProgressionModels { get; set; } = new List<ProgressionModel>();
        public List<ProgressionLevel> ProgressionLevels { get; set; } = new List<ProgressionLevel>();
        public List<ComponentCondition> ComponentConditions { get; set; } = new List<ComponentCondition>();
        public List<PathwayComponent> PendingComponents { get; set; } = new List<PathwayComponent>();
        public List<Constraint> Constraints { get; set; } = new List<Constraint>();
        /// <summary>
        /// UI will populate with components that were deleted. These will be removed from DB.
        /// Or should we use virtual deletes, just in case?
        /// 22-11-15 found that Protiviti is placing the full component in DeletedComponents
        /// </summary>
        public List<PathwayComponent> DeletedComponents { get; set; } = new List<PathwayComponent>();
        public List<Guid> DeletedComponentsOld { get; set; } = new List<Guid>();

        /// <summary>
        /// 22-11-15 found that Protiviti is placing the full component condition in DeletedComponentConditions
        /// </summary>
        public List<ComponentCondition> DeletedComponentConditions { get; set; } = new List<ComponentCondition>();
        public List<Guid> DeletedComponentConditionsOld { get; set; } = new List<Guid>();
    }
    //

    public class PathwayCore
    {
        public int Id { get; set; }
        public Guid RowId { get; set; }
        public string Name { get; set; }
        public string FriendlyName { get; set; }
        public string Description { get; set; }
        public string CTID { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastUpdated { get; set; }
        public string LastUpdatedBy { get; set; }
        public int LastUpdatedById { get; set; }
    }
	//

    public class Pathway : PathwayCore
    {
        public string SubjectWebpage { get; set; }
        //maybe
        public ResourceSummary Organization { get; set; } = new ResourceSummary();
        public List<string> Keyword { get; set; } = new List<string>();
        public List<string> Subject { get; set; } = new List<string>();
        public bool CanEditRecord { get; set; }
        /// <summary>
        /// CTID for a progression model
        /// May need more for this
        /// </summary>
        public List<string> HasProgressionModel { get; set; } = new List<string>();

        //do we need the full object or just the name and key? On click for details there will likely be a process to get the full component
        //public List<PathwayComponent> HasDestinationComponent { get; set; } = new List<PathwayComponent>();
        //22-08-19 Nate changed to a single
        //public List<Guid> HasDestinationComponent { get; set; } = new List<Guid>();
        public string HasDestinationComponent { get; set; } 
        /// <summary>
        /// Not clear how to use in UI at this time?
        /// </summary>
        public List<string> HasChild { get; set; } = new List<string>();

        public List<ResourceSummary> HasSupportService { get; set; } = new List<ResourceSummary>();

        public List<ResourceSummary> OccupationType { get; set; } = new List<ResourceSummary>();
        public List<ResourceSummary> IndustryType { get; set; } = new List<ResourceSummary>();
        public List<ResourceSummary> InstructionalProgramType { get; set; } = new List<ResourceSummary>();
    }
	//

  //  public class ResourceSummary
  //  {
  //      public int Id { get; set; }
  //      public Guid RowId { get; set; }
  //      public string CodedNotation { get; set; }
  //      public string Name { get; set; }
		//public string Description { get; set; }
  //      public string CTID { get; set; }
		//public string URI { get; set; }
		//public string Type { get; set; }
  //      public string CredentialId { get; set; }
  //  }
	//

    public class PathwayComponent : PathwayCore
    {
        public string PathwayCTID { get; set; }

        //URI
        public string Type { get; set; }
        public string TypeLabel { get; set; }
        public int PathwayComponentTypeId { get; set; }

        //[System.ComponentModel.DefaultValueAttribute(1)]
        public int RowNumber{ get; set; }
        public int ColumnNumber { get; set; }
        public bool IsPendingComponent { get; set; }
        /// <summary>
        /// this is the relationship to the parent
        /// Shouldn't this just be an enum instead?
        /// Id	Title						Description
        /// 1	Has Destination Component   Entity Has Destination Component
        /// 2	Is Child Of Entity          Is Child Of component
        /// 3	Has Child                   Entity has child component
        /// 4	Preceeds                    Resource that logically comes after this resource.
        /// 5	Preceded By                 Resource that logically comes before this resource.
        /// 6	Has Target Component        Entity Has Target Component
        /// </summary>
        public int ComponentRelationshipTypeId { get; set; }
        public string ComponentCategory { get; set; }
        public string CredentialType { get; set; }
        public List<ValueProfile> CreditValue { get; set; } = new List<ValueProfile>();

        /// <summary>
        /// Label identifying the category to further distinguish one component from another as designated by the promulgating body.
        /// Examples may include "Required", "Core", "General Education", "Elective", etc.
        /// </summary>
        public List<string> ComponentDesignation { get; set; } = new List<string>();

        /// <summary>
        /// When non null, this will be a CTID
        /// </summary>
        public string HasProgressionLevel { get; set; }
        public List<IdentifierValue> Identifier { get; set; } = new List<IdentifierValue>();
        public List<ResourceSummary> OccupationType { get; set; } = new List<ResourceSummary>();
        public List<ResourceSummary> IndustryType { get; set; } = new List<ResourceSummary>();
        public QuantitativeValue PointValue { get; set; } = new QuantitativeValue();

        /// <summary>
        /// Resource that logically comes after this resource.
        /// This property indicates a simple or suggested ordering of resources; if a required ordering is intended, use ceterms:prerequisite instead.
        /// Provide the CTID or the full URI for the target environment. 
        /// ceterms:PathwayComponent
        /// </summary>
        public List<string> Precedes { get; set; } = new List<string>();

        /// <summary>
        /// Resource that logically comes before this resource.
        /// Provide the CTID or the full URI for the target environment. 
        /// ceterms:PathwayComponent
        /// </summary>
        public List<string> PrecededBy { get; set; } = new List<string>();

        public string ProgramTerm { get; set; }

        //CTID
        public string ProxyFor { get; set; }
        public ResourceSummary FinderResource { get; set; }


        //have as a backup vs. doing a registry get 
        public string ProxyForLabel { get; set; }

        /// <summary>
        /// This property identifies a pathwayComponent(s) in the downward path.
        /// Provide the CTID  
        /// </summary>
        public List<string> HasChild { get; set; } = new List<string>();

        /// <summary>
        /// List of indentifiers (Guids) for ComponentConditions
        /// </summary>
        public List<Guid> HasCondition { get; set; } = new List<Guid>();

        /// <summary>
        /// Not used
        /// </summary>
        public List<string> IsChildOf { get; set; } = new List<string>();
		public string SubjectWebpage { get; set; }
    }
	//

    public class ComponentCondition 
    {
        /// <summary>
        /// Guid of parent.  
        /// In this context it is the GUID/RowId of a pathway component or component condition which is of course the Entity.EntityUid property
        /// </summary>
        public Guid ParentIdentifier { get; set; }
        public int ParentEntityId { get; set; }
        public Guid RowId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int RequiredNumber { get; set; }

        public DateTime Created { get; set; }
        public DateTime LastUpdated { get; set; }
        public string LastUpdatedBy { get; set; }
        public int LastUpdatedById { get; set; }

        /// <summary>
        /// Additional component conditions
        /// TODO - this will be external now
        /// ceterms:ComponentCondition
        /// </summary>
        public List<Guid> HasCondition { get; set; } = new List<Guid>();

        public List<string> TargetComponent { get; set; } = new List<string>();
        public string LogicalOperator { get; set; }

        /// <summary>
        /// Referenced resource defines a single constraint.
        /// TODO - this may be external now
        /// URI 
        /// ceterms:hasConstraint
        ///  Range: ceterms:Constraint
        /// </summary>
        public List<Guid> HasConstraint { get; set; } = new List<Guid>();
        //public List<Constraint> HasConstraint { get; set; } = new List<Constraint>();
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }       
        /// <summary>      
        /// When non null, this will be a CTID. not published, just for use by the UI
        /// TODO - need to enable means for a condition in a dest prog level - or don't allow
        /// </summary>
        public string HasProgressionLevel { get; set; }
        public string ExternalIdentifier { get; set; }
        public string PathwayCTID { get; set; }
    }
	//
  
    //ConceptScheme is too thick, need to have a custom class
    public class ProgressionModel : PathwayCore
    {
		public List<string> HasTopConcept { get; set; }
    }
	//

    public class ProgressionLevel : PathwayCore
	{
		/// <summary>
		/// List of CTIDs for child ProgressionLevels
		/// </summary>
		public List<string> Narrower { get; set; }

		public string InProgressionModel { get; set; }
    }
    //

}
