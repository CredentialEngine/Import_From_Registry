using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using RA.Models.JsonV2;
namespace RA.Models.JsonV2.QData
{
	/// <summary>
	/// DataSet Time Frame
	/// Time frame including earnings and employment start and end dates of the data set.
	/// https://credreg.net/qdata/terms/DataSetTimeFrame
	/// </summary>
	public class DataSetTimeFrame
	{
		/// <summary>
		/// The type of the entity
		/// </summary>
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "qdata:DataSetTimeFrame";

		/// <summary>
		/// Id for this blank node
		/// </summary>
		[JsonProperty( "@id" )]
		public string CtdlId { get; set; }

		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }

		[JsonProperty( PropertyName = "ceterms:startDate" )]
		public string StartDate { get; set; }

		[JsonProperty( PropertyName = "ceterms:endDate" )]
		public string EndDate { get; set; }

		/// <summary>
		/// Attributes of the data set.
		/// URI to blank node
		/// qdata:DataProfile
		/// </summary>
		[JsonProperty( PropertyName = "qdata:dataAttributes" )]
		public List<string> DataAttributes { get; set; }

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
		[JsonProperty( PropertyName = "qdata:dataSourceCoverageType" )]
		public List<CredentialAlignmentObject> DataSourceCoverageType { get; set; }

	}
}
