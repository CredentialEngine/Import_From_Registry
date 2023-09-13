using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;


namespace RA.Models.JsonV2
{
	public class AggregateDataProfile
	{
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "ceterms:AggregateDataProfile";

		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }

		[JsonProperty( PropertyName = "ceterms:alternateName" )]
		public LanguageMapList AlternateName { get; set; } 

		[JsonProperty( PropertyName = "ceterms:currency" )]
		public string Currency { get; set; }

		/// <summary>
		/// Effective date of this profile
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:dateEffective" )]
		public string DateEffective { get; set; }

		[JsonProperty( PropertyName = "ceterms:expirationDate" )]
		public string ExpirationDate { get; set; }
		/// <summary>
		/// DemographicInformation
		/// Aggregate data or summaries of statistical data relating to the population of credential holders including data about gender, geopolitical regions, age, education levels, and other categories of interest.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:demographicInformation" )]
		public LanguageMap DemographicInformation { get; set; }

		/// <summary>
		/// Faculty-to-Student Ratio
		/// Ratio of the number of teaching faculty to the number of students.
		/// The expression of the ratio should feature the number of faculty first, followed by the number of students, e.g., "1:10" to mean "one faculty per ten students".
		/// 
		/// </summary>
		[JsonProperty( PropertyName = "qdata:facultyToStudentRatio" )]
		public string FacultyToStudentRatio { get; set; }

		/// <summary>
		///  Upper interquartile earnings.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:highEarnings" )]
		public int HighEarnings { get; set; }

		/// <summary>
		///  Number of jobs obtained in the region during a given timeframe.
		///  ceterms:jobsObtained
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:jobsObtained" )]
		public List<QuantitativeValue> JobsObtained { get; set; }

		/// <summary>
		/// Jurisdiction Profile
		/// Geo-political information about applicable geographic areas and their exceptions.
		/// <see href="https://credreg.net/ctdl/terms/JurisdictionProfile"/>
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
		///  Number of credentials awarded.
		/// </summary>
		[JsonProperty( PropertyName = "ceterms:numberAwarded" )]
		public int NumberAwarded { get; set; }

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

		/// <summary>
		/// Check if actual outcome data was provided!
		/// </summary>
		/// <returns></returns>
		public bool HasOutcomeData()
		{
			if ( JobsObtained != null     //prime
			|| RelevantDataSet != null  //prime

			|| HighEarnings > 0
			|| LowEarnings > 0
			|| MedianEarnings > 0
			|| NumberAwarded > 0
			|| PostReceiptMonths > 0
			|| NumberAwarded > 0
			)
				return true;
			else
				return false;
		}
		public bool HasData()
        {
			if ( Name != null		//name is not important without other data
			//|| Description != null		//skip description here. Use with check for HasData. Actual Name as well
			|| DemographicInformation != null
			|| JobsObtained != null		//prime
			|| Jurisdiction != null
			|| RelevantDataSet != null	//prime

			|| !string.IsNullOrWhiteSpace( Currency )
			|| !string.IsNullOrWhiteSpace( DateEffective )
			|| !string.IsNullOrWhiteSpace( ExpirationDate )
			|| !string.IsNullOrWhiteSpace( FacultyToStudentRatio )
			|| !string.IsNullOrWhiteSpace( Source )
			|| HighEarnings> 0
			|| LowEarnings > 0
			|| MedianEarnings > 0
			|| NumberAwarded > 0
			|| PostReceiptMonths > 0
			|| NumberAwarded > 0
			)
				return true;
			else
				return false;
        }
	}
}
