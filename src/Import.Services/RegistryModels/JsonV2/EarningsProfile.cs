using System.Collections.Generic;

using Newtonsoft.Json;

using RA.Models.JsonV2.QData;

namespace RA.Models.JsonV2
{
	/// <summary>
	/// Earnings Profile
	/// Entity that describes earning and related statistical information for a given credential.
	/// </summary>
	public class EarningsProfile
	{
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceterms:EarningsProfile";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( "ceterms:ctid" )]
		public string CTID { get; set; }


		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }

		/// <summary>
		/// Effective date of this profile
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:dateEffective" )]
		public string DateEffective { get; set; }


		/// <summary>
		///  Upper interquartile earnings.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:highEarnings" )]
		public int HighEarnings { get; set; }

		/// <summary>
		/// Jurisdiction Profile
		/// Geo-political information about applicable geographic areas and their exceptions.
		/// <see cref="https://credreg.net/ctdl/terms/JurisdictionProfile"/>
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:jurisdiction" )]
		public List<JurisdictionProfile> Jurisdiction { get; set; }

		/// <summary>
		///  Lower interquartile earnings.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:lowEarnings" )]
		public int LowEarnings { get; set; }

		/// <summary>
		///  Median earnings.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:medianEarnings" )]
		public int MedianEarnings { get; set; }


		/// <summary>
		/// Number of months after earning a credential when employment and earnings data is collected.
		/// Number of months usually range between 3 months (one quarter) to ten years.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:postReceiptMonths" )]
		public int PostReceiptMonths { get; set; }
		//public JurisdictionProfile Region { get; set; }

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
		/// TODO - this may change to URIs
		/// </summary>
		[JsonProperty( PropertyName = "qdata:relevantDataSet" )]
		public List<string> RelevantDataSet { get; set; }
		[JsonProperty( PropertyName = "ceterms:alternateName" )]
		public LanguageMapList AlternateName { get; set; } 
		//[JsonProperty( PropertyName = "qdata:relevantDataSet" )]
		//public List<DataSetProfile> RelevantDataSets { get; set; }

	}
}
