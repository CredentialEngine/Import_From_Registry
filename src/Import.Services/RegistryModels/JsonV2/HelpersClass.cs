using System.Collections.Generic;
//using System.Text.Json;
//using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{

    /*
	public class GraphContainer2
	{
		//[JsonIgnore]
		//public static string ceasnContext = "https://credreg.net/ctdlasn/schema/context/json";

		public GraphContainer2()
		{
			Context = "https://credreg.net/ctdlasn/schema/context/json";
		}
		[JsonPropertyName( "@context" )]
		public string Context { get; set; }

		[JsonPropertyName( "@id" )]
		public string CtdlId { get; set; }

		/// <summary>
		/// Main graph object
		/// </summary>
		[JsonPropertyName( "@graph" )]
		public List<object> Graph { get; set; } = new List<object>();

		//[JsonIgnore]
		//[JsonPropertyName( "@type" )]
		//public string Type { get; set; }

		//[JsonIgnore]
		//[JsonPropertyName( "ceterms:ctid" )]
		//public string CTID { get; set; }

	}

    */
	public class IdProperty
	{
		[JsonProperty( "@id" )]
		public string Id { get; set; }
	}


	/// <summary>
	/// 20-08-23
	/// TBD - this may be obsolete now
	/// </summary>
	public class EntityReferenceHelper
    {
        public EntityReferenceHelper()
        {
           // IdPropertyList = new List<IdProperty>();
            ReturnedDataType = 0;
        }
        public List<OrganizationBase> OrgBaseList { get; set; } = new List<OrganizationBase>();

        public List<EntityBase> EntityBaseList { get; set; } = new List<EntityBase>();
       // public List<IdProperty> IdPropertyList { get; set; }

        /// <summary>
        /// indicate data returned
        /// 0 - none; 1 - Id list; 2 - org list
        /// </summary>
        public int ReturnedDataType { get; set; }

    }




}
