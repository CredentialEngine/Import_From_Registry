using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using RA.Models.JsonV2;

namespace RA.Models.JsonV2.QData
{
	/// <summary>
	/// DataSet Profile
	/// Particular characteristics or properties of a data set and its records.
	/// qdata:DataSetProfile
	/// <see href="https://credreg.net/qdata/terms/DataSetProfile"/>
	/// </summary>
	public class DataSetProfile : BaseResourceDocument
	{
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "qdata:DataSetProfile";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( "ceterms:ctid" )]
		public string CTID { get; set; }


		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }

		/// <summary>
		/// Entity describing the process by which a credential, assessment, organization, or aspects of it, are administered.
		/// <see href="https://credreg.net/ctdl/terms/administrationProcess"/>
		/// </summary>
		[ JsonProperty( PropertyName = "ceterms:administrationProcess" )]
		public List<ProcessProfile> AdministrationProcess { get; set; }

		[JsonProperty( PropertyName = "ceterms:alternateName" )]
		public LanguageMapList AlternateName { get; set; } 

		/// <summary>
		/// Instructional Program Type
		/// Type of instructional program; select from an existing enumeration of such types.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:instructionalProgramType" )]
		public List<CredentialAlignmentObject> InstructionalProgramType { get; set; } 

		/// <summary>
		/// Jurisdiction Profile
		/// Geo-political information about applicable geographic areas and their exceptions.
		/// <see href="https://credreg.net/ctdl/terms/JurisdictionProfile"/>
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:jurisdiction" )]
		public List<JurisdictionProfile> Jurisdiction { get; set; }


		/// <summary>
		/// Authoritative source of an entity's information.
		/// URL
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:source" )]
		public string Source { get; set; }

		/// <summary>
		/// Credentialing organization or a third party providing the data.
		/// URI
		/// </summary>
		[JsonProperty( PropertyName = "qdata:dataProvider" )]
		public string DataProvider { get; set; }

		/// <summary>
		/// Data Set Time Period
		/// Short- and long-term post-award reporting intervals including start and end dates.
		/// URI of a blank node
		/// TODO - will need to define this as an object during conversion
		/// </summary>
		[JsonProperty( PropertyName = "qdata:dataSetTimePeriod" )]
		public List<DataSetTimeFrame> DataSetTimePeriod { get; set; }


		//[JsonProperty( PropertyName = "qdata:dataSetTimePeriodBNList" )]
		//public List<string> DataSetTimePeriodBNList { get; set; }

		/// <summary>
		/// Data Suppression Policy
		/// Description of a data suppression policy for earnings and employment data when cell size is below a certain threshold to ensure an individual's privacy and security.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:dataSuppressionPolicy" )]
		public LanguageMap DataSuppressionPolicy {get; set; }

		/// <summary>
		/// Distribution File
		/// Downloadable form of this dataset, at a specific location, in a specific format.
		/// URL
		/// </summary>
		[JsonProperty( PropertyName = "qdata:distributionFile" )]
		public List<string> DistributionFile { get; set; }

        /// <summary>
        /// Relevant Data Set For
        /// Data set for the entity being referenced.
        /// REQUIRED when dataSetProfile published separately.
        /// Inverse property	- point back to the parent
        /// 21-02-19 mparsons	Removing these from range: HoldersProfile, EarningsProfile, EmploymentOutlook
        ///						- adding credential, assessment, and lopp
        /// 21-05-10 mparsons	- effectively obsolete outside of HoldersProfile, EarningsProfile, EmploymentOutlook and the latter are moving to be obsolete
        /// 23-02-14 mparsons - however, for old data, it may still be present
        /// </summary>
        [Obsolete]
        [JsonProperty( PropertyName = "qdata:relevantDataSetFor" )]
        public List<string> RelevantDataSetFor { get; set; }

        /// <summary>
        /// Subject matter of the resource.
        /// Means to point to a credential (etc.) where data is published by a third party.
        /// CTID/URI
        /// </summary>
        [JsonProperty( PropertyName = "schema:about" )]
		public List<string> About { get; set; } 

		/// <summary>
		/// Identification of data point(s) in the data set that describe personal subject attribute(s) used to uniquely identify a subject for the purpose of matching records and an indication of level of confidence in the accuracy of the match.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:subjectIdentification" )]
		public LanguageMap SubjectIdentification { get; set; } 

	}
}
