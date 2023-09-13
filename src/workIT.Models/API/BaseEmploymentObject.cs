using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.API
{
    public class BaseEmploymentObject : BaseAPIType
    {

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
        public List<string> ClassificationList { get; set; }

        public string CodedNotation { get; set; }


        /// <summary>
        /// Comment
        /// Definition:	en-US: Supplemental text provided by the promulgating body that clarifies the nature, scope or use of this competency.
        /// ceasn:comment
        /// </summary>
        public List<string> Comment { get; set; } = new List<string>();

        /// <summary>
        /// Alphanumeric token that identifies this resource and information about the token's originating context or scheme.
        /// <see cref="http://purl.org/ctdl/terms/identifier"/>
        /// </summary>
        public List<IdentifierValue> Identifier { get; set; }

        /// <summary>
        /// Body of information embodied either directly or indirectly in this resource.
        /// List of URIs for a competency
        /// ceasn:knowledgeEmbodied
        /// </summary>
        public List<string> KnowledgeEmbodied { get; set; }



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
