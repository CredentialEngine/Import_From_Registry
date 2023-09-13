using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RA.Models.JsonV2.QData;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	public class EmploymentOutcomeProfile
	{

		[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceterms:EmploymentOutcomeProfile";

		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( "ceterms:ctid" )]
		public string CTID { get; set; }


		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:alternateName" )]
		public LanguageMapList AlternateName { get; set; } 

		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }

		/// <summary>
		/// Effective date of this profile
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:dateEffective" )]
		public string DateEffective { get; set; }


		/// <summary>
		///  Number of jobs obtained in the region during a given timeframe.
		///  ceterms:jobsObtained
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:jobsObtained" )]
		public List<QuantitativeValue> JobsObtained { get; set; }
		//public int JobsObtained { get; set; }

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
		/// TODO - this may change to URIs
		/// </summary>
		//[JsonProperty( PropertyName = "qdata:relevantDataSet" )]
		//public List<DataSetProfile> RelevantDataSet { get; set; }

		[JsonProperty( PropertyName = "qdata:relevantDataSet" )]
		public List<string> RelevantDataSet { get; set; }
	}
}
