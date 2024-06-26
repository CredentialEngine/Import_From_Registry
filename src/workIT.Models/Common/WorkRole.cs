using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using workIT.Models.ProfileModels;

namespace workIT.Models.Common
{
	/// <summary>
	/// Profession, trade, or career field that may involve training and/or a formal qualification.
	/// </summary>
	public class WorkRole : BaseEmploymentObject
	{
		/*
		ceasn:abilityEmbodied
		ceasn:comment
		ceasn:knowledgeEmbodied
		ceasn:skillEmbodied
		ceterms:alternateName
		ceterms:classification
		ceterms:codedNotation
		ceterms:ctid
		ceterms:description
		ceterms:environmentalHazardType		*
		ceterms:hasTask						*
		ceterms:identifier
		ceterms:isMemberOf					*
		ceterms:name
		ceterms:performanceLevelType		*
		ceterms:physicalCapabilityType		*
		ceterms:sensoryCapabilityType		*
		ceterms:versionIdentifier
		*/
		public WorkRole()
		{
			EntityTypeId = 34;
		}
		/// <summary>
		///  type
		/// </summary>
		public string Type { get; set; } = "ceterms:WorkRole";

		/// <summary>
		/// Type of condition in the physical work performance environment that entails risk exposures requiring mitigating processes; select from an existing enumeration of such types.
		/// </summary>
		//public Enumeration EnvironmentalHazardType { get; set; }

		/// <summary>
		/// Task related to this resource.
		/// <see cref="https://credreg.net/ctdl/terms/hasTask"/>
		/// </summary>
		public List<ResourceSummary> HasTask { get; set; }
		/// <summary>
		/// Occupation related to this resource.
		/// <see cref="https://credreg.net/ctdl/terms/hasOccupation"/>
		/// </summary>
		public List<ResourceSummary> HasOccupation { get; set; }

		/// <summary>
		/// Collection to which this resource belongs.
		/// </summary>
		public List<int> IsMemberOf { get; set; }

		///// <summary>
		///// Type of required or expected human performance level; select from an existing enumeration of such types.
		///// </summary>
		//public Enumeration PerformanceLevelType { get; set; }
		///// <summary>
		///// Type of physical activity required or expected in performance; select from an existing enumeration of such types.
		///// </summary>
		//public Enumeration PhysicalCapabilityType { get; set; }

		///// <summary>
		///// Type of required or expected sensory capability; select from an existing enumeration of such types.
		///// </summary>
		//public Enumeration SensoryCapabilityType { get; set; }



		#region Import
		public List<int> HasTaskIds { get; set; }
		public List<int> HasOccupationIds { get; set; }
		public List<Guid> AssertedByList { get; set; } = new List<Guid>();
		//public List<int> HasSupportServiceIds { get; set; } = new List<int>();

		#endregion
	}
}
