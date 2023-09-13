using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ME = workIT.Models.Elastic;

using WMA = workIT.Models.API;
using WMS = workIT.Models.Search;


namespace workIT.Models.API
{
	public class DataSetProfile : BaseAPIType
	{
		public DataSetProfile()
		{
			EntityTypeId = 31;
			BroadType = "DataSetProfile";
			CTDLType = "qdata:DataSetProfile";
		}
		//public string CTID { get; set; }
		//public string Name { get; set; }
		//public string Description { get; set; }
		//public List<ME.JurisdictionProfile> Jurisdiction { get; set; }

		/// <summary>
		/// Entity describing the process by which a credential, assessment, organization, or aspects of it, are administered.
		/// <see cref="https://credreg.net/ctdl/terms/administrationProcess#administrationProcess"/>
		/// </summary>
		public WMS.AJAXSettings AdministrationProcess { get; set; } 
		public List<WMA.Outline> About { get; set; }

		/// <summary>
		/// Instructional Program Type
		/// Type of instructional program; select from an existing enumeration of such types.
		/// </summary>
		public List<ReferenceFramework> InstructionalProgramType { get; set; } = new List<ReferenceFramework>();

		/// <summary>
		/// Credentialing organization or a third party providing the data.
		/// </summary>
		public WMA.Outline DataProvider { get; set; }
		public WMS.AJAXSettings DataProviderMain { get; set; }

		/// <summary>
		/// Data Set Time Period
		/// Short- and long-term post-award reporting intervals including start and end dates.
		/// </summary>
		public List<DataSetTimeFrame> DataSetTimePeriod { get; set; }
		/// <summary>
		/// Data Suppression Policy
		/// Description of a data suppression policy for earnings and employment data when cell size is below a certain threshold to ensure an individual's privacy and security.
		/// </summary>
		public string DataSuppressionPolicy { get; set; }

		/// <summary>
		/// Distribution File
		/// Downloadable form of this dataset, at a specific location, in a specific format.
		/// URL
		/// </summary>
		public List<string> DistributionFile { get; set; } 

		/// <summary>
		/// Identification of data point(s) in the data set that describe personal subject attribute(s) used to uniquely identify a subject for the purpose of matching records and an indication of level of confidence in the accuracy of the match.
		/// </summary>
		public string SubjectIdentification { get; set; }
		public string Source { get; set; }
		
	}

}
