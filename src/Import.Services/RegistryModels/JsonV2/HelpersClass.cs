using System.Collections.Generic;

using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
    public class GraphContainer
    {
        [JsonIgnore]
        public static string classType = "ceterms:Credential";
        public GraphContainer()
        {
            Context = "http://credreg.net/ctdlasn/schema/context/json";
        }
        [JsonProperty( "@context" )]
        public string Context { get; set; }

        [JsonProperty( "@id" )]
        public string CtdlId { get; set; }

        /// <summary>
        /// Main graph object
        /// </summary>
        [JsonProperty( "@graph" )]
        public List<object> Graph { get; set; } = new List<object>();

        [JsonIgnore]
        [JsonProperty( "@type" )]
        public string Type { get; set; }

        [JsonIgnore]
        [JsonProperty( "ceterms:ctid" )]
        public string CTID { get; set; }

    }
    public class IdProperty
	{
		[JsonProperty( "@id" )]
		public string Id { get; set; }
	}
    public class BlankNode
    {

        /// <summary>
        /// An identifier for use with blank nodes, to minimize duplicates
        /// </summary>
        [JsonProperty( "@id" )]
        public string BNodeId { get; set; }

        /// <summary>
        /// the type of the entity must be provided. examples
        /// ceterms:AssessmentProfile
        /// ceterms:LearningOpportunityProfile
        /// ceterms:ConditionManifest
        /// ceterms:CostManifest
        /// or the many credential subclasses!!
        /// </summary>
        [JsonProperty( "@type" )]
        public string Type { get; set; }

        /// <summary>
        /// Name of the entity (required)
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:name" )]
        public LanguageMap Name { get; set; } = new LanguageMap();

        /// <summary>
        /// Description of the entity (optional)
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:description" )]
        public LanguageMap Description { get; set; } = new LanguageMap();

        /// <summary>
        /// Subject webpage of the entity
        /// </summary> (required)
        [JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
        public string SubjectWebpage { get; set; }

        [JsonProperty( PropertyName = "ceterms:socialMedia" )]
        public List<string> SocialMedia { get; set; } = null;
    }
    public class EntityReferenceHelper
    {
        public EntityReferenceHelper()
        {
            OrgBaseList = new List<OrganizationBase>();
            EntityBaseList = new List<EntityBase>();
           // IdPropertyList = new List<IdProperty>();
            ReturnedDataType = 0;
        }
        public List<OrganizationBase> OrgBaseList { get; set; }

        public List<EntityBase> EntityBaseList { get; set; }
       // public List<IdProperty> IdPropertyList { get; set; }

        /// <summary>
        /// indicate data returned
        /// 0 - none; 1 - Id list; 2 - org list
        /// </summary>
        public int ReturnedDataType { get; set; }

    }

    public class LanguageString
	{
		[JsonProperty( "en" )]
		public string Value { get; set; }

		[JsonProperty( "es" )]
		public string esValue { get; set; }

		[JsonProperty( "de" )]
		public string deValue { get; set; }
	}

	public class IdentifierValue
	{
		public IdentifierValue()
		{
			Type = "ceterms:IdentifierValue";
		}

		[JsonProperty( "@type" )]
		public string Type { get; set; }

		[JsonProperty( "ceterms:name" )]
		public string Name { get; set; }

		[JsonProperty( "ceterms:description" )]
		public LanguageMap Description { get; set; }

		[JsonProperty( "ceterms:identifierType" )]
		public string IdentifierType { get; set; }

		[JsonProperty( "ceterms:identifierValueCode" )]
		public string IdentifierValueCode { get; set; }
	}

}
