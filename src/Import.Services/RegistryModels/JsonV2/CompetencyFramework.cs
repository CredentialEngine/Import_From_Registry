﻿using System.Collections.Generic;
//using System.Text.Json;
//using System.Text.Json.Serialization;

using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class CompetencyFrameworksGraph
	{
		
		public CompetencyFrameworksGraph()
		{
			//Type = classType;
			//Context = "https://credreg.net/ctdlasn/schema/context/json";
		}
		[JsonProperty( "@context" )]
		public string Context { get; set; } = "https://credreg.net/ctdlasn/schema/context/json";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; } //		/graph/

		/// <summary>
		/// Main graph object
		/// </summary>
		[JsonProperty( "@graph" )]
		public object Graph { get; set; }

	}
	public class CompetencyFramework //: JsonLDDocument
	{
		//[JsonIgnore]
		//public static string classType = "ceasn:CompetencyFramework";
		//[JsonIgnore]
		//public static string thisContext = "https://credreg.net/ctdlasn/schema/context/json";
		public CompetencyFramework()
		{
			//Type = classType;
			//Context = thisContext;	//
		}

		[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceasn:CompetencyFramework";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }	//		/resource

		[JsonProperty( "ceterms:ctid" )]
		public string CTID { get; set; }

		[JsonProperty( "ceasn:alignFrom" )]
		public List<string> alignFrom { get; set; } 

		[JsonProperty( "ceasn:alignTo" )]
		public List<string> alignTo { get; set; } 

		//[JsonProperty( "ceasn:altIdentifier" )]
		//public List<string> altIdentifier { get; set; }

		[JsonProperty( "ceasn:author" )]
		public string author { get; set; }
		//public List<string> author { get; set; }


		[JsonProperty( "ceasn:conceptKeyword" )]
		public LanguageMapList conceptKeyword { get; set; }

		[JsonProperty( "ceasn:conceptTerm" )]
		public List<string> conceptTerm { get; set; } 

		[JsonProperty( "ceasn:creator" )]
		public List<string> creator { get; set; } 

		[JsonProperty( "ceasn:dateCopyrighted" )]
		public string dateCopyrighted { get; set; }

		/// <summary>
		/// Only allow date (yyyy-mm-dd), no time
		/// xsd:date
		/// </summary>
		[JsonProperty( "ceasn:dateCreated" )]
		public string dateCreated { get; set; }

		/// <summary>
		/// Originally only allowing date (yyyy-mm-dd), no time. 
		/// However, this is defined as: xsd:dateTime. So consumers like the credential registry search, expect a datetime format.
		/// </summary>

		[JsonProperty( "ceasn:dateModified" )]
		public string dateModified { get; set; }

		/// <summary>
		/// xsd:dateTime
		/// </summary>
		[JsonProperty( "ceasn:dateValidFrom" )]
		public string dateValidFrom { get; set; }

		/// <summary>
		/// xsd:dateTime
		/// </summary>
		[JsonProperty( "ceasn:dateValidUntil" )]
		public string dateValidUntil { get; set; }

		//single per https://github.com/CredentialEngine/CompetencyFrameworks/issues/66
		//23-03-22 back to a list
		//but store as object due to old resources as string
		[JsonProperty( "ceasn:derivedFrom" )]
        public object derivedFrom { get; set; }
        //public List<string> derivedFrom { get; set; }

        //???language map??
        [JsonProperty( "ceasn:description" )]
		public LanguageMap description { get; set; }

		[JsonProperty( "ceasn:educationLevelType" )]
		public List<string> educationLevelType { get; set; }

		/// <summary>
		/// Top-level child competency of a competency framework.
		/// </summary>
		[JsonProperty( "ceasn:hasTopChild" )]
		public List<string> hasTopChild { get; set; } 

		[JsonProperty( "ceasn:identifier" )]
		public List<string> identifier { get; set; } 

		[JsonProperty( "ceasn:inLanguage" )]
		public List<string> inLanguage { get; set; } 

		[JsonProperty( "ceasn:license" )]
		public string license { get; set; }

		[JsonProperty( "ceasn:localSubject" )]
		public LanguageMapList localSubject { get; set; } 
		
		[JsonProperty( "ceasn:name" )]
		public LanguageMap name { get; set; } = new LanguageMap();

		[JsonProperty("ceasn:publicationStatusType" )]
		public string publicationStatusType { get; set; }
		//
		[JsonProperty( "ceasn:publisher" )]
		public List<string> publisher { get; set; } 

		[JsonProperty( "ceasn:publisherName" )]
		public LanguageMapList publisherName { get; set; } 
		//

		[JsonProperty( "ceasn:repositoryDate" )]
		public string repositoryDate { get; set; }

		/// <summary>
		/// 19-01-18 Changed to a language string
		/// Hide until changed in CaSS
		/// </summary>
		[JsonProperty( "ceasn:rights" )]
		public LanguageMap rights { get; set; } 
		//public object rights { get; set; }
		//public List<string> rights { get; set; } 

		[JsonProperty( "ceasn:rightsHolder" )]
		public object rightsHolder { get; set; }

		[JsonProperty( "ceasn:source" )]
		public List<string> source { get; set; } 

		//
		[JsonProperty( "ceasn:tableOfContents" )]
		public LanguageMap tableOfContents { get; set; } 

		[JsonProperty( "ceterms:occupationType" )]
		public List<CredentialAlignmentObject> OccupationType { get; set; } 

		[JsonProperty( "ceterms:industryType" )]
		public List<CredentialAlignmentObject> IndustryType { get; set; }
		[JsonProperty( PropertyName = "ceterms:instructionalProgramType" )]
		public List<CredentialAlignmentObject> InstructionalProgramType { get; set; }


		//*** Helper properties where publishing input is a graph. These will not be published
		/// <summary>
		/// CIP List is a helper when publishing from a graph. It will not be published
		/// </summary>
		[JsonProperty( "cipList" )]
		public List<string> CIPList { get; set; } = null;
		/// <summary>
		/// SOC List is a helper when publishing from a graph. It will not be published
		/// </summary>
		[JsonProperty( "socList" )]
		public List<string> SOCList { get; set; } = null;

        /// NAICS List is a helper when publishing from a graph. It will not be published
        [JsonProperty( "naicsList" )]
		public List<string> NaicsList { get; set; } = null;

		/// <summary>
		/// VersionIdentifier
		/// Alphanumeric identifier of the version of the credential that is unique within the organizational context of its owner.
		/// The credential version captured here is any local identifier used by the credential owner to identify the version of the credential in the its local system.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:versionIdentifier" )]
		public List<IdentifierValue> VersionIdentifier { get; set; }

		/// <summary>
		/// Latest version of the credential.
		/// full URL OR CTID (recommended)
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:latestVersion" )]
		public string LatestVersion { get; set; } //URL

		/// <summary>
		/// Version of the resource that immediately precedes this version.
		/// full URL OR CTID (recommended)
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:previousVersion" )]
		public string PreviousVersion { get; set; } //URL

		/// <summary>
		/// Version of the resource that immediately follows this version.
		/// full URL OR CTID (recommended)
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:nextVersion" )]
		public string NextVersion { get; set; } //URL

	}

	public class CompetencyFrameworkPlain
	{
		//[JsonIgnore]
		//public static string classType = "ceasn:CompetencyFramework";
		//[JsonIgnore]
		//public static string thisContext = "https://credreg.net/ctdlasn/schema/context/json";
		public CompetencyFrameworkPlain()
		{
			//Type = classType;
			//Context = thisContext;	//
		}

		[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceasn:CompetencyFramework";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( "ceterms:ctid" )]
		public string CTID { get; set; }

		[JsonProperty( "ceasn:alignFrom" )]
		public List<string> alignFrom { get; set; } = new List<string>();

		[JsonProperty( "ceasn:alignTo" )]
		public List<string> alignTo { get; set; } = new List<string>();

		[JsonProperty( "ceasn:altIdentifier" )]
		public List<string> altIdentifier { get; set; }

        //[JsonProperty( "ceasn:author" )]
        //public List<string> author { get; set; }


        [JsonProperty( "ceasn:conceptKeyword" )]
        public List<string> conceptKeyword { get; set; } = new List<string>();

        [JsonProperty( "ceasn:conceptTerm" )]
		public List<string> conceptTerm { get; set; } = new List<string>();

		[JsonProperty( "ceasn:creator" )]
		public List<string> creator { get; set; } = new List<string>();

		[JsonProperty( "ceasn:dateCopyrighted" )]
		public string dateCopyrighted { get; set; }

		[JsonProperty( "ceasn:dateCreated" )]
		public string dateCreated { get; set; }

		[JsonProperty( "ceasn:dateModified" )]
		public string dateModified { get; set; }

		[JsonProperty( "ceasn:dateValidFrom" )]
		public string dateValidFrom { get; set; }

		[JsonProperty( "ceasn:dateValidUntil" )]
		public string dateValidUntil { get; set; }

        //single per https://github.com/CredentialEngine/CompetencyFrameworks/issues/66
        //23-03-22 back to a list
        [JsonProperty( "ceasn:derivedFrom" )]
        public List<string> derivedFrom { get; set; }

        //???language map??
        [JsonProperty( "ceasn:description" )]
		public string description { get; set; }

		[JsonProperty( "ceasn:educationLevelType" )]
		public List<string> educationLevelType { get; set; } = new List<string>();

		/// <summary>
		/// Top-level child competency of a competency framework.
		/// </summary>
		[JsonProperty( "ceasn:hasTopChild" )]
		public List<string> hasTopChild { get; set; } = new List<string>();

		[JsonProperty( "ceasn:identifier" )]
		public List<string> identifier { get; set; } = new List<string>();

		[JsonProperty( "ceasn:inLanguage" )]
		public List<string> inLanguage { get; set; } = new List<string>();

		[JsonProperty( "ceasn:license" )]
		public string license { get; set; }

		[JsonProperty( "ceasn:localSubject" )]
		public List<string> localSubject { get; set; } = new List<string>();


		[JsonProperty( "ceasn:name" )]
		public string name { get; set; }

		[JsonProperty( "ceasn:publicationStatusType" )]
		public string publicationStatusType { get; set; }// = new List<IdProperty>();

		[JsonProperty( "ceasn:publisher" )]
		public List<string> publisher { get; set; } = new List<string>();

		[JsonProperty( "ceasn:publisherName" )]
		public List<string> publisherName { get; set; } = new List<string>();
		//

		[JsonProperty( "ceasn:repositoryDate" )]
		public string repositoryDate { get; set; }

		/// <summary>
		/// 19-01-18 Changed to a language string
		/// Hide until changed in CaSS
		/// </summary>
		[JsonProperty( "ceasn:rights" )]
		public LanguageMap rights { get; set; } = new LanguageMap();
		//public object rights { get; set; }
		//public List<string> rights { get; set; } = new List<string>();

		[JsonProperty( "ceasn:rightsHolder" )]
		public List<string> rightsHolder { get; set; }

		[JsonProperty( "ceasn:source" )]
		public List<string> source { get; set; } = new List<string>();

		//
		[JsonProperty( "ceasn:tableOfContents" )]
		public string tableOfContents { get; set; }

		[JsonProperty( "ceterms:occupationType" )]
		public List<CredentialAlignmentObject> OccupationType { get; set; } = new List<CredentialAlignmentObject>();

		[JsonProperty( "ceterms:industryType" )]
		public List<CredentialAlignmentObject> IndustryType { get; set; } = new List<CredentialAlignmentObject>();


	}
	
}
