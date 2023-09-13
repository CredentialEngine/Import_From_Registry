using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace workIT.Models.Common
{
	/// <summary>
	/// Specific activity, typically related to performing a function or achieving a goal.
	/// </summary>
	public class Task : BaseEmploymentObject
	{
		/*
			ceasn:abilityEmbodied
			ceasn:comment
			ceasn:hasChild
			ceasn:isChildOf
			ceasn:knowledgeEmbodied
			ceasn:listID
			ceasn:skillEmbodied
			ceterms:alternateName
			ceterms:classification
			ceterms:codedNotation
			ceterms:ctid
			ceterms:description
			ceterms:environmentalHazardType
			ceterms:identifier
			ceterms:isMemberOf
			ceterms:name
			ceterms:performanceLevelType
			ceterms:physicalCapabilityType
			ceterms:sensoryCapabilityType
			ceterms:versionIdentifier
		*/
		public Task()
		{
			EntityTypeId = 33;
		}
		/// <summary>
		///  type
		/// </summary>
		public string Type { get; set; } = "ceterms:Task";


		#region import
		public List<int> HasChild { get; set; }
		public List<int> IsChildOf { get; set; }

		public List<int> ListId { get; set; }
		#endregion

	}
	public class BaseTask
	{


		/// <summary>
		/// URI
		/// </summary>
		public string CtdlId { get; set; }

		/// <summary>
		/// Globally unique Credential Transparency Identifier (CTID) by which the creator, owner or provider of a resource recognizes it in transactions with the external environment (e.g., in verifiable claims involving the resource).
		/// required
		/// <see cref="https://credreg.net/ctdl/terms/ctid"/>
		/// </summary>
		public string CTID { get; set; }

		/// <summary>
		/// AbilityEmbodied
		/// Enduring attributes of the individual that influence performance are embodied either directly or indirectly in this resource.
		/// ceasn:abilityEmbodied
		/// </summary>
		public List<string> AbilityEmbodied { get; set; }

		/// <summary>
		/// Category or classification of this resource.
		/// Where a more specific property exists, such as ceterms:naics, ceterms:isicV4, ceterms:credentialType, etc., use that property instead of this one.
		/// URI to a competency
		/// ceterms:classification
		/// </summary>
		public List<string> Classification { get; set; }
		//public Enumeration Classification { get; set; }

		/// <summary>
		/// The referenced resource is lower in some arbitrary hierarchy than this resource.
		/// CTID for an existing Task
		/// </summary>
		public List<string> HasChild { get; set; }

		/// <summary>
		/// The referenced resource is higher in some arbitrary hierarchy than this resource
		/// CTID for an existing Task
		/// </summary>
		public List<string> IsChildOf { get; set; }

		/// <summary>
		/// Alphanumeric token that identifies this resource and information about the token's originating context or scheme.
		/// <see cref="https://purl.org/ctdl/terms/identifier"/>
		/// </summary>
		public List<IdentifierValue> Identifier { get; set; }

		/// <summary>
		/// Body of information embodied either directly or indirectly in this resource.
		/// List of URIs for a competency
		/// ceasn:knowledgeEmbodied
		/// </summary>
		public List<string> KnowledgeEmbodied { get; set; }

		/// <summary>
		/// An alphanumeric string found in the source framework indicating the relative position of a competency in an ordered list of competencies such as "A", "B", or "a", "b", or "I", "II", or "1", "2".
		/// </summary>
		public string ListID { get; set; }

		/// <summary>
		/// Organization(s) that offer this resource
		/// </summary>
		public List<string> OfferedBy { get; set; }

		/// <summary>
		///Ability to apply knowledge and use know-how to complete tasks and solve problems including types or categories of developed proficiency or dexterity in mental operations and physical processes is embodied either directly or indirectly in this resource.
		/// </summary>
		public List<string> SkillEmbodied { get; set; }

		/// <summary>
		/// Alphanumeric identifier of the version of the credential that is unique within the organizational context of its owner.
		/// ceterms:versionIdentifier
		/// </summary>
		public List<IdentifierValue> VersionIdentifier { get; set; }

	}
}
