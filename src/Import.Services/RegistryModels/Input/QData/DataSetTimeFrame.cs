using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RA.Models.Input.profiles.QData
{
	/// <summary>
	/// DataSet Time Frame
	/// Time frame including earnings and employment start and end dates of the data set.
	/// Required:
	/// - StartDate (otherwise doesn't make sense)
	/// https://credreg.net/qdata/terms/DataSetTimeFrame
	/// </summary>
	public class DataSetTimeFrame
	{
		/// <summary>
		/// Attributes of the data set.
		/// qdata:DataProfile
		/// </summary>
		public List<DataProfile> DataAttributes { get; set; } = new List<DataProfile>();

		/// <summary>
		/// Data Source Coverage Type
		/// Type of geographic coverage of the subjects.
		/// <see cref="https://credreg.net/qdata/terms/dataSourceCoverageType"/>
		/// skos:Concept
		/// <see cref="https://credreg.net/qdata/terms/DataSourceCoverage"/>
		/// sourceCoverage:Country
		///	sourceCoverage:Global
		///	sourceCoverage:Region
		///	sourceCoverage:StateOrProvince
		///	sourceCoverage:UrbanArea
		/// </summary>
		public List<string> DataSourceCoverageType { get; set; } = new List<string>();

		//NOT required
		public string Description { get; set; }
		public LanguageMap Description_Map { get; set; } = new LanguageMap();

		public string Name { get; set; }
		public LanguageMap Name_Map { get; set; } = new LanguageMap();

		public string StartDate { get; set; }
		public string EndDate { get; set; }
	}
}
