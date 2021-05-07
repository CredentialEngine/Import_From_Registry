using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.QData;

namespace workIT.Models.Common
{
	public class AggregateDataProfile
	{
		public AggregateDataProfile()
		{
			RowId = new Guid(); 
			Created = new DateTime();
			LastUpdated = new DateTime();
		}
		public string Type { get; set; } = "ceterms:AggregateDataProfile";
		public int Id { get; set; }
		public int EntityId { get; set; }
		public Guid RowId { get; set; }

		public DateTime Created { get; set; }
		public DateTime LastUpdated { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }

		public string Currency { get; set; }
		public string CurrencySymbol { get; set; }
		/// <summary>
		/// Effective date of this profile
		/// </summary>
		public string DateEffective { get; set; }

		/// <summary>
		/// DemographicInformation
		/// Aggregate data or summaries of statistical data relating to the population of credential holders including data about gender, geopolitical regions, age, education levels, and other categories of interest.
		/// </summary>
		public string DemographicInformation { get; set; }

		/// <summary>
		///  Upper interquartile earnings.
		/// </summary>
		public int HighEarnings { get; set; }

		/// <summary>
		///  Number of jobs obtained in the region during a given timeframe.
		///  ceterms:jobsObtained
		/// </summary>
		public List<QuantitativeValue> JobsObtained { get; set; } = new List<QuantitativeValue>();

		/// <summary>
		/// Jurisdiction Profile
		/// Geo-political information about applicable geographic areas and their exceptions.
		/// <see cref="https://credreg.net/ctdl/terms/JurisdictionProfile"/>
		/// </summary>
		public List<JurisdictionProfile> Jurisdiction { get; set; } = new List<JurisdictionProfile>();

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
		public int NumberAwarded { get; set; }

		/// <summary>
		/// Number of months after earning a credential when employment and earnings data is collected.
		/// Number of months usually range between 3 months (one quarter) to ten years.
		/// </summary>
		public int PostReceiptMonths { get; set; }
		//public JurisdictionProfile Region { get; set; }

		/// <summary>
		/// Authoritative source of an entity's information.
		/// URL
		/// </summary>
		public string Source { get; set; }

		/// <summary>
		/// Relevant Data Set
		/// Data Set on which earnings or employment data is based.
		/// qdata:DataSetProfile
		/// TODO - this may change to URIs
		/// </summary>
		/// <summary>
		/// Relevant Data Set
		/// Data Set on which earnings or employment data is based.
		/// qdata:DataSetProfile
		/// </summary>
		public List<DataSetProfile> RelevantDataSet { get; set; } = new List<DataSetProfile>();

		//import only-maybe
		public List<string> RelevantDataSetList { get; set; } = new List<string>();

	}
}
