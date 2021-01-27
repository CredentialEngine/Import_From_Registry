using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using workIT.Models.Common;

namespace workIT.Models.QData
{
	/// <summary>
	/// DataSet Time Frame
	/// Time frame including earnings and employment start and end dates of the data set.
	/// https://credreg.net/qdata/terms/DataSetTimeFrame
	/// </summary>
	public class DataSetTimeFrame
	{
		public string bnID { get; set; }
		public int Id { get; set; }
		public Guid RowId { get; set; }
		public int DataSetProfileId { get; set; }
		/// <summary>
		/// Attributes of the data set.
		/// qdata:DataProfile
		/// </summary>
		public List<DataProfile> DataAttributes { get; set; } = new List<DataProfile>();
		//import only
		public List<string> DataAttributesList { get; set; } = new List<string>();
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
		public Enumeration DataSourceCoverageType { get; set; } = new Enumeration();
		public List<string> DataSourceCoverageTypeList { get; set; } = new List<string>();
		public string Description { get; set; }

		public string Name { get; set; }

		public string StartDate { get; set; }
		public string EndDate { get; set; }

		public DateTime Created { get; set; }
		public DateTime LastUpdated { get; set; }
	}
}
