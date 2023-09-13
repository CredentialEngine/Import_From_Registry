using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
	/// <summary>
	/// Describes what must be done to complete one PathwayComponent (or part thereof) as determined by the issuer of the Pathway
	/// </summary>
	public class ComponentCondition : BaseObject
	{
		public int EntityId { get; set; }

		//TBD?????????
		public Guid ParentIdentifier { get; set; }
        //this will replaced by EntityId
        //	- at this time need it to get the entity. Check where populated and get entity at that time.
        [Obsolete]
        public int ParentComponentId { get; set; }

		/// <summary>
		/// PathwayComponent Description 
		/// Required
		/// </summary>
		public string Description { get; set; }


		/// <summary>
		/// Name
		/// </summary>
		public string Name { get; set; }

		public int RequiredNumber { get; set; }

		public List<PathwayComponent> TargetComponent { get; set; } = new List<PathwayComponent>();

		public string LogicalOperator { get; set; }
		/// <summary>
		/// Resource(s) that describes what must be done to complete a PathwayComponent, or part thereof, as determined by the issuer of the Pathway.
		/// Provide the CTID or the full URI for the target environment. 
		/// ceterms:ComponentCondition
		/// </summary>
		public List<ComponentCondition> HasCondition { get; set; } = new List<ComponentCondition>();

		public List<Constraint> HasConstraint { get; set; }

		public string PathwayCTID { get; set; }

		public System.Guid PathwayIdentifier { get; set; }

		//****NOTE: will get stack overflow initializing here. 
		//current code will always check for nulls
		public Entity RelatedEntity { get; set; }

		#region Import
		public List<Guid> HasTargetComponentList { get; set; } = new List<Guid>();

        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }
        public string HasProgressionLevel { get; set; }
        #endregion
        /// <summary>
        /// TODO - may try to assign these on import. 
        /// </summary>
        public ConditionProperties ConditionProperties { get; set; } // = new ConditionProperties();
	}

	/// <summary>
	/// Resource that identifies the parameters defining a limitation or restriction applicable to candidate pathway components.
	/// </summary>
	public class ConditionProperties
	{
		public int RowNumber { get; set; }
		public int ColumnNumber { get; set; }
		//only to aid in display - TBD
		public string HasProgressionLevel { get; set; }
	}
	public class Constraint
	{
		public Constraint()
		{
			Type = "ceterms:Constraint";
		}

		public string Type { get; set; }

        public Guid RowId { get; set; }
        public Guid ParentIdentifier { get; set; }
        
        /// <summary>
        /// Constraint Name
        /// Optional
        /// </summary>
        public string Name { get; set; } = null;


		/// <summary>
		/// Constraint Description 
		/// Optional
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Type of symbol that denotes an operator in a constraint expression such as "gteq" (greater than or equal to), "eq" (equal to), "lt" (less than), "isAllOf" (is all of), "isAnyOf" (is any of); 
		/// URI to Concept
		/// ceterms:Concept 
		/// </summary>
		public string Comparator { get; set; }

		/// <summary>
		/// Left hand parameter of a constraint.
		/// Range: rdf:Property, skos:Concept 
		/// </summary>
		public List<string> LeftSource { get; set; }

		/// <summary>
		/// Action performed on the left constraint; select from an existing enumeration of such types.
		/// URI to Concept
		/// Range: ceterms:Concept 
		/// </summary>
		public string LeftAction { get; set; }

		/// <summary>
		/// Right hand parameter of a constraint.
		/// Range: rdf:Property, skos:Concept 
		/// </summary>
		public List<string> RightSource { get; set; }

		/// <summary>
		/// Action performed on the right constraint; select from an existing enumeration of such types.
		/// URI to Concept
		/// Range: ceterms:Concept
		/// </summary>
		public string RightAction { get; set; }
	}

}
