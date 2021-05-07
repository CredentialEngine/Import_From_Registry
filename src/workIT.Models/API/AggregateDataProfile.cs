using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MC = workIT.Models.Common;
using MD = workIT.Models.API;
using ME = workIT.Models.Elastic;
using MPM = workIT.Models.ProfileModels;
using MQD = workIT.Models.QData;

namespace workIT.Models.API
{
	public class AggregateDataProfile : BaseDisplay
	{
		/// <summary>
		/// DemographicInformation
		/// Aggregate data or summaries of statistical data relating to the population of credential holders including data about gender, geopolitical regions, age, education levels, and other categories of interest.
		/// </summary>
		public string DemographicInformation { get; set; }

		/// <summary>
		///  Lower interquartile earnings.
		/// </summary>
		public int LowEarnings { get; set; }

		/// <summary>
		///  Median earnings.
		/// </summary>
		public int MedianEarnings { get; set; }

		/// <summary>
		///  Upper interquartile earnings.
		/// </summary>
		public int HighEarnings { get; set; }

		public List<MC.QuantitativeValue> JobsObtainedList { get; set; } = new List<MC.QuantitativeValue>();

		/// <summary>
		///  Number of credentials awarded.
		/// </summary>
		public int NumberAwarded { get; set; }

		/// <summary>
		/// Number of months after earning a credential when employment and earnings data is collected.
		/// Number of months usually range between 3 months (one quarter) to ten years.
		/// </summary>
		public int PostReceiptMonths { get; set; }


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
		public List<MQD.DataSetProfile> RelevantDataSet { get; set; } = new List<MQD.DataSetProfile>();
	}
}
