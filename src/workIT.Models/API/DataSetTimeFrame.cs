using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.API
{
	public class DataSetTimeFrame
	{

		/// <summary>
		/// Data Source Coverage Type
		/// Type of geographic coverage of the subjects.
		/// <see cref="https://credreg.net/qdata/terms/dataSourceCoverageType#dataSourceCoverageType"/>
		/// skos:Concept
		/// <see cref="https://credreg.net/qdata/terms/DataSourceCoverage#DataSourceCoverage"/>
		/// sourceCoverage:Country
		///	sourceCoverage:Global
		///	sourceCoverage:Region
		///	sourceCoverage:StateOrProvince
		///	sourceCoverage:UrbanArea
		/// </summary>
		public List<LabelLink> DataSourceCoverageType { get; set; } 
		public string Description { get; set; }

		public string Name { get; set; }

		public string StartDate { get; set; }
		public string EndDate { get; set; }
		/// <summary>
		/// Attributes of the data set.
		/// qdata:DataProfile
		/// </summary>
		public List<DataProfile> DataAttributes { get; set; } = new List<DataProfile>();
	}
}
