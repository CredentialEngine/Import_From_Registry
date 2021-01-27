using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;
using workIT.Models.ProfileModels;
namespace workIT.Models.QData
{
	/// <summary>
	/// DataSet Profile
	/// Particular characteristics or properties of a data set and its records.
	/// qdata:DataSetProfile
	/// <see cref="https://credreg.net/qdata/terms/DataSetProfile"/>
	/// </summary>
	public class DataSetProfile : OutcomesBaseObject
	{
		//public string Ctid { get; set; }
		/// <summary>
		/// Entity describing the process by which a credential, assessment, organization, or aspects of it, are administered.
		/// <see cref="https://credreg.net/ctdl/terms/administrationProcess#administrationProcess"/>
		/// </summary>
		public List<ProcessProfile> AdministrationProcess { get; set; } = new List<ProcessProfile>();

		//public string Description { get; set; }
		//public LanguageMap Description_Map { get; set; } = new LanguageMap();

		#region Instruction program and helpers
		/// <summary>
		/// Instructional Program Type
		/// Type of instructional program; select from an existing enumeration of such types.
		/// </summary>
		public List<CredentialAlignmentObjectProfile> InstructionalProgramTypes { get; set; } = new List<CredentialAlignmentObjectProfile>();
		public Enumeration InstructionalProgramType { get; set; } = new Enumeration();

		#endregion


		/// <summary>
		/// Credentialing organization or a third party providing the data.
		/// </summary>
		public Organization DataProvider { get; set; } = new Organization();
		public Guid DataProviderUID { get; set; }

		/// <summary>
		/// Data Set Time Period
		/// Short- and long-term post-award reporting intervals including start and end dates.
		/// </summary>
		public List<DataSetTimeFrame> DataSetTimePeriod { get; set; } = new List<DataSetTimeFrame>();
		public List<string> DataSetTimePeriodList { get; set; } = new List<string>();
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
		public List<string> DistributionFile { get; set; } = new List<string>();

		/// <summary>
		/// Identification of data point(s) in the data set that describe personal subject attribute(s) used to uniquely identify a subject for the purpose of matching records and an indication of level of confidence in the accuracy of the match.
		/// </summary>
		public string SubjectIdentification { get; set; }

	}

	public class Entity_DataSetProfile
	{
		public int Id { get; set; }
		public int EntityId { get; set; }
		public int DataSetProfileId { get; set; }
		public System.DateTime Created { get; set; }
	}
}
