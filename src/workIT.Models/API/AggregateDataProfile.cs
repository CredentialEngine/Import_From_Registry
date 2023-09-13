using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using MC = workIT.Models.Common;
using MD = workIT.Models.API;
using ME = workIT.Models.Elastic;
using MPM = workIT.Models.ProfileModels;
using MQD = workIT.Models.QData;
using WMA = workIT.Models.API;

namespace workIT.Models.API
{
	[JsonObject( ItemNullValueHandling = NullValueHandling.Ignore )]
	public class AggregateDataProfile 
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public string Currency { get; set; }
		public string CurrencySymbol { get; set; }
		/// <summary>
		/// Effective date of this profile
		/// </summary>
		public string DateEffective { get; set; }
		public string ExpirationDate { get; set; }
		/// <summary>
		/// DemographicInformation
		/// Aggregate data or summaries of statistical data relating to the population of credential holders including data about gender, geopolitical regions, age, education levels, and other categories of interest.
		/// </summary>
		public string DemographicInformation { get; set; }

		/// <summary>
		///  Lower interquartile earnings.
		/// </summary>
		public int? LowEarnings { get; set; }

		/// <summary>
		///  Median earnings.
		/// </summary>
		public int? MedianEarnings { get; set; }

		/// <summary>
		///  Upper interquartile earnings.
		/// </summary>
		public int? HighEarnings { get; set; }

		public List<WMA.QuantitativeValue> JobsObtainedList { get; set; } = new List<WMA.QuantitativeValue>();

		/// <summary>
		///  Number of credentials awarded.
		/// </summary>
		public int? NumberAwarded { get; set; }

		/// <summary>
		/// Number of months after earning a credential when employment and earnings data is collected.
		/// Number of months usually range between 3 months (one quarter) to ten years.
		/// </summary>
		public int? PostReceiptMonths { get; set; }
		/// <summary>
		/// Faculty-to-Student Ratio
		/// Ratio of the number of teaching faculty to the number of students.
		/// The expression of the ratio should feature the number of faculty first, followed by the number of students, e.g., "1:10" to mean "one faculty per ten students".
		/// qdata:facultyToStudentRatio
		/// </summary>
		public string FacultyToStudentRatio { get; set; }

		/// <summary>
		/// Authoritative source of an entity's information.
		/// URL
		/// </summary>
		public string Source { get; set; }

		/// <summary>
		/// Relevant Data Set
		/// Data Set on which earnings or employment data is based.
		/// qdata:DataSetProfile
		/// </summary>
		public List<DataSetProfile> RelevantDataSet { get; set; } 
		public List<ME.JurisdictionProfile> Jurisdiction { get; set; } 

	}
}
