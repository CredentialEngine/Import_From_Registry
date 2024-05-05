﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	/// <summary>
	/// Specific activity, typically related to performing a function or achieving a goal.
	/// </summary>
	public class Task : BaseTask
	{
		/// <summary>
		/// Name
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; }

		/// <summary>
		/// Description
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }

		[JsonProperty( PropertyName = "ceterms:alternateName" )]
		public LanguageMapList AlternateName { get; set; }

		/// <summary>
		/// Comment
		/// Definition:	en-US: Supplemental text provided by the promulgating body that clarifies the nature, scope or use of this competency.
		/// ceasn:comment
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:comment" )]
		public LanguageMapList Comment { get; set; }

		/// <summary>
		/// Organization(s) that own this resource
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:assertedBy" )]
		public List<string> AssertedBy { get; set; }
	}
	/// <summary>
	/// Specific activity, typically related to performing a function or achieving a goal.
	/// </summary>
	public class TaskPlain : BaseTask
	{

		/// <summary>
		/// Name
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:name" )]
		public string Name { get; set; }

		/// <summary>
		/// Description
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:description" )]
		public string Description { get; set; }


		/// <summary>
		/// Comment
		/// Definition:	en-US: Supplemental text provided by the promulgating body that clarifies the nature, scope or use of this competency.
		/// ceasn:comment
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:comment" )]
		public List<string> Comment { get; set; }


	}
	public class BaseTask : BaseEmploymentToWorkObject
	{
		/// <summary>
		///  type
		/// </summary>
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceterms:Task";

		/// <summary>
		/// URI
		/// </summary>
		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		/// <summary>
		/// Globally unique Credential Transparency Identifier (CTID) by which the creator, owner or provider of a resource recognizes it in transactions with the external environment (e.g., in verifiable claims involving the resource).
		/// required
		/// <see cref="https://credreg.net/ctdl/terms/ctid"/>
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:ctid" )]
		public string CTID { get; set; }

        /// <summary>
        /// AbilityEmbodied
        /// Enduring attributes of the individual that influence performance are embodied either directly or indirectly in this resource.
        /// CTID/URI to any of:
        /// ceasn:Competency ceterms:Job ceterms:Occupation ceterms:Task ceterms:WorkRole
        /// ceasn:abilityEmbodied
        /// </summary>
        [JsonProperty( PropertyName = "ceasn:abilityEmbodied" )]
		public List<string> AbilityEmbodied { get; set; }

		/// <summary>
		/// Category or classification of this resource.
		/// Where a more specific property exists, such as ceterms:naics, ceterms:isicV4, ceterms:credentialType, etc., use that property instead of this one.
		/// URI to a concept
		/// ceterms:classification
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:classification" )]
		public List<string> Classification { get; set; }

		/// <summary>
		/// Set of alpha-numeric symbols that uniquely identifies an item and supports its discovery and use.
		/// ceterms:codedNotation
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:codedNotation" )]
		public string CodedNotation { get; set; }


		[JsonProperty( PropertyName = "ceterms:environmentalHazardType" )]
		public List<string> EnvironmentalHazardType { get; set; }

		[JsonProperty( PropertyName = "ceterms:performanceLevelType" )]
		public List<string> PerformanceLevelType { get; set; }

		[JsonProperty( PropertyName = "ceterms:physicalCapabilityType" )]
		public List<string> PhysicalCapabilityType { get; set; }


		[JsonProperty( PropertyName = "ceterms:sensoryCapabilityType" )]
		public List<string> SensoryCapabilityType { get; set; }

		/// <summary>
		/// The referenced resource is lower in some arbitrary hierarchy than this resource.
		/// CTID for an existing Task
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:hasChild" )]
		public List<string> HasChild { get; set; }

		/// <summary>
		/// The referenced resource is higher in some arbitrary hierarchy than this resource
		/// CTID for an existing Task
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:isChildOf" )]
		public List<string> IsChildOf { get; set; }

		/// <summary>
		/// Rubric related to this resource.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:hasRubric" )]
		public List<string> HasRubric { get; set; }

		/// <summary>
		/// Alphanumeric token that identifies this resource and information about the token's originating context or scheme.
		/// <see cref="https://purl.org/ctdl/terms/identifier"/>
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:identifier" )]
		public List<IdentifierValue> Identifier { get; set; }

		/// <summary>
		/// Body of information embodied either directly or indirectly in this resource.
		/// List of URIs for a competency
		/// ceasn:knowledgeEmbodied
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:knowledgeEmbodied" )]
		public List<string> KnowledgeEmbodied { get; set; }

		/// <summary>
		/// An alphanumeric string found in the source framework indicating the relative position of a competency in an ordered list of competencies such as "A", "B", or "a", "b", or "I", "II", or "1", "2".
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:listID" )]
		public string ListID { get; set; }

		/// <summary>
		/// Organization(s) that offer this resource
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:offeredBy" )]
		public List<string> OfferedBy { get; set; }

		/// <summary>
		///Ability to apply knowledge and use know-how to complete tasks and solve problems including types or categories of developed proficiency or dexterity in mental operations and physical processes is embodied either directly or indirectly in this resource.
		/// </summary>
		[JsonProperty( PropertyName = "ceasn:skillEmbodied" )]
		public List<string> SkillEmbodied { get; set; }


		/// <summary>
		/// Occupation related to this resource.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:hasOccupation" )]
		public List<string> HasOccupation { get; set; }
		/// <summary>
		/// Work Role related to this resource.
		/// List of URIs for an existing WorkRole
		/// ceterms:hasWorkRole
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:hasWorkRole" )]
		public List<string> HasWorkRole { get; set; }

		/// <summary>
		/// Job related to this resource.
		/// CTID for an existing Job
		/// ceterms:hasJob
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:hasJob" )]
		public List<string> HasJob { get; set; }

		/// <summary>
		/// Alphanumeric identifier of the version of the credential that is unique within the organizational context of its owner.
		/// ceterms:versionIdentifier
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:versionIdentifier" )]
		public List<IdentifierValue> VersionIdentifier { get; set; }

	}
}
