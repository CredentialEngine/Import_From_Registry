using System.Collections.Generic;

using Newtonsoft.Json;

namespace RA.Models.Json
{
    public class IdProperty
    {
        [JsonProperty( "@id" )]
        public string Id { get; set; }
    }

    public class EntityReferenceHelper
    {
        public EntityReferenceHelper()
        {
            OrgBaseList = new List<OrganizationBase>();
            EntityBaseList = new List<EntityBase>();
            IdPropertyList = new List<IdProperty>();
            ReturnedDataType = 0;
        }
        public List<OrganizationBase> OrgBaseList { get; set; }

        public List<EntityBase> EntityBaseList { get; set; }
        public List<IdProperty> IdPropertyList { get; set; }

        /// <summary>
        /// indicate data returned
        /// 0 - none; 1 - Id list; 2 - org lsit
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
        public string Description { get; set; }

        [JsonProperty( "ceterms:identifierType" )]
        public string IdentifierType { get; set; }

        [JsonProperty( "ceterms:identifierValueCode" )]
        public string IdentifierValueCode { get; set; }
    }

    public class UnknownPayload : JsonLDDocument
    {
        /// <summary>
		/// object type
		/// </summary>
		[JsonProperty( "@type" )]
        public string Type
        {
            get; set;
        }

        [JsonProperty( "@id" )]
        public string CtdlId { get; set; }
        [JsonProperty( PropertyName = "ceterms:ctid" )]
        public string Ctid { get; set; }
    }
}