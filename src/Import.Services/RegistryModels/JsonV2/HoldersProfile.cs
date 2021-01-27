using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RA.Models.JsonV2.QData;
using Newtonsoft.Json;
namespace RA.Models.JsonV2
{
	/// <summary>
	/// Entity describing the count and related statistical information of holders of a given credential.
	/// </summary>
	public class HoldersProfile
	{

		[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceterms:HoldersProfile";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		/// <summary>
		/// Unique identifier
		/// </summary>
		[JsonProperty( "ceterms:ctid" )]
		public string Ctid { get; set; }

		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; }

		/// <summary>
		/// Description of the profile
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }

		/// <summary>
		/// Effective date of this profile
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:dateEffective" )]
		public string DateEffective { get; set; }


		/// <summary>
		/// DemographicInformation
		/// Aggregate data or summaries of statistical data relating to the population of credential holders including data about gender, geopolitical regions, age, education levels, and other categories of interest.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:demographicInformation" )]
		public LanguageMap DemographicInformation { get; set; }

		/// <summary>
		///  Upper interquartile earnings.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:numberAwarded" )]
		public int NumberAwarded { get; set; }

		/// <summary>
		/// Jurisdiction Profile
		/// Geo-political information about applicable geographic areas and their exceptions.
		/// <see cref="https://credreg.net/ctdl/terms/JurisdictionProfile"/>
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
		/// Relevant Data Set
		/// Data Set on which earnings or employment data is based.
		/// qdata:DataSetProfile
		/// </summary>
		[JsonProperty( PropertyName = "qdata:relevantDataSet" )]
		public List<string> RelevantDataSet { get; set; }
		//public List<DataSetProfile> RelevantDataSet { get; set; }

	}
}
