using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMP = workIT.Models.ProfileModels;
namespace workIT.Models.Common
{
    //
    [Serializable]
    public class BaseEmploymentObject : TopLevelObject
    {
        /// <summary>
        /// URI
        /// </summary>
        public string CtdlId { get; set; }

		public List<WMP.OrganizationRoleProfile> OrganizationRole { get; set; }

		/// <summary>
		/// AbilityEmbodied
		/// Enduring attributes of the individual that influence performance are embodied either directly or indirectly in this resource.
		/// ceasn:abilityEmbodied
		/// </summary>
		public List<ResourceSummary> AbilityEmbodied { get; set; } = new List<ResourceSummary>();

        public List<string> AlternateName { get; set; }
        /// <summary>
        /// Category or classification of this resource.
        /// Where a more specific property exists, such as ceterms:naics, ceterms:isicV4, ceterms:credentialType, etc., use that property instead of this one.
        /// URI to a competency
        /// ceterms:classification
        /// </summary>
        public List<ResourceSummary> Classification { get; set; } = new List<ResourceSummary>();


        public string CodedNotation { get; set; }

        public string InCatalog { get; set; }
        /// <summary>
        /// Comment
        /// Definition:	en-US: Supplemental text provided by the promulgating body that clarifies the nature, scope or use of this competency.
        /// ceasn:comment
        /// </summary>
        public List<string> Comment { get; set; } = new List<string>();
        public string CommentJson { get; set; }

        /// <summary>
        /// Alphanumeric token that identifies this resource and information about the token's originating context or scheme.
        /// <see cref="http://purl.org/ctdl/terms/identifier"/>
        /// </summary>
        public List<IdentifierValue> Identifier { get; set; }
        public string IdentifierJson { get; set; }

        public List<ResourceSummary> HasSupportService { get; set; } = new List<ResourceSummary>();

        /// <summary>
        /// Body of information embodied either directly or indirectly in this resource.
        /// List of URIs for a competency
        /// ceasn:knowledgeEmbodied
        /// </summary>
		public List<ResourceSummary> KnowledgeEmbodied { get; set; } = new List<ResourceSummary>();

        /// <summary>
        ///Ability to apply knowledge and use know-how to complete tasks and solve problems including types or categories of developed proficiency or dexterity in mental operations and physical processes is embodied either directly or indirectly in this resource.
        /// </summary>
        public List<ResourceSummary> SkillEmbodied { get; set; } = new List<ResourceSummary>();

		/// <summary>
		/// Type of official status of the TransferProfile; select from an enumeration of such types.
		/// Provide the string value. API will format correctly. The name space of lifecycle doesn't have to be included
		/// lifecycle:Developing, lifecycle:Active", lifecycle:Suspended, lifecycle:Ceased
		/// </summary>
		public Enumeration LifeCycleStatusType { get; set; } = new Enumeration();
		public string LifeCycleStatus { get; set; }
		public int LifeCycleStatusTypeId { get; set; }

		/// <summary>
		/// Alphanumeric identifier of the version of the credential that is unique within the organizational context of its owner.
		/// ceterms:versionIdentifier
		/// </summary>
		public List<IdentifierValue> VersionIdentifier { get; set; }
        public string VersionIdentifierJson { get; set; }
        //Free floating Concepts 

        /// <summary>
        /// Environmental Hazard Type
        /// Type of condition in the physical work performance environment that entails risk exposures requiring mitigating processes; 
        /// select from an existing enumeration of such types.
        /// skos:Concept
        /// Blank nodes!
        /// </summary>
        public List<ResourceSummary> EnvironmentalHazardType { get; set; } = new List<ResourceSummary>();

        /// <summary>
        /// Type of required or expected human performance level; select from an existing enumeration of such types.
        /// skos:Concept
        /// Blank nodes!
        /// </summary>
        public List<ResourceSummary> PerformanceLevelType { get; set; } = new List<ResourceSummary>();

        /// <summary>
        /// Type of physical activity required or expected in performance; select from an existing enumeration of such types.
        /// skos:Concept
        /// Blank nodes!
        /// </summary>
        public List<ResourceSummary> PhysicalCapabilityType { get; set; } = new List<ResourceSummary>();

        /// <summary>
        /// Type of required or expected sensory capability; select from an existing enumeration of such types.
        /// skos:Concept
        /// Blank nodes!
        /// </summary>
        public List<ResourceSummary> SensoryCapabilityType { get; set; } = new List<ResourceSummary>();

        public List<CredentialAlignmentObjectProfile> TargetCompetency { get; set; } = new List<CredentialAlignmentObjectProfile>();

        public List<ResourceSummary> HasRubric { get; set; } = new List<ResourceSummary>();
        #region import
        public List<Guid> AbilityEmbodiedUIDs { get; set; } = new List<Guid>();
        public List<Guid> KnowledgeEmbodiedUIDs { get; set; } = new List<Guid>();
		public List<Guid> SkillEmbodiedUIDs { get; set; } = new List<Guid>();

		//NO - can't use integers where target entity type is not known
		//public List<int> AbilityEmbodiedList { get; set; }

		//      public List<int> KnowledgeEmbodiedList { get; set; }

		//      public List<int> SkillEmbodiedList { get; set; }
		//public List<string> ClassificationList { get; set; }
		public List<int> HasRubricIds { get; set; } = new List<int>();
		public List<int> HasSupportServiceIds { get; set; } = new List<int>();

        public List<CredentialAlignmentObjectFrameworkProfile> AbilityEmbodiedOutput { get; set; } = new List<CredentialAlignmentObjectFrameworkProfile>();
        public List<CredentialAlignmentObjectFrameworkProfile> KnowledgeEmbodiedOutput { get; set; } = new List<CredentialAlignmentObjectFrameworkProfile>();
        public List<CredentialAlignmentObjectFrameworkProfile> SkillEmbodiedOutput { get; set; } = new List<CredentialAlignmentObjectFrameworkProfile>();

        public List<ResourceSummary> RelatedJob { get; set; } = new List<ResourceSummary>();
        public List<ResourceSummary> RelatedOccupation { get; set; } = new List<ResourceSummary>();
        public List<ResourceSummary> RelatedWorkRole { get; set; } = new List<ResourceSummary>();
        public List<ResourceSummary> RelatedTask { get; set; } = new List<ResourceSummary>();
        public List<CollectionMember> RelatedCollection { get; set; } = new List<CollectionMember>();



        #endregion


    }
}
