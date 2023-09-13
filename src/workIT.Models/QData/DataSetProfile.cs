using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;
using workIT.Models.ProfileModels;

using WMA = workIT.Models.API;


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
		public DataSetProfile()
		{
			RelevantDataSet = null;
		}
		//public string Ctid { get; set; }
		/// <summary>
		/// Entity describing the process by which a credential, assessment, organization, or aspects of it, are administered.
		/// <see cref="https://credreg.net/ctdl/terms/administrationProcess#administrationProcess"/>
		/// </summary>
		public List<ProcessProfile> AdministrationProcess { get; set; } = new List<ProcessProfile>();
		public List<TopLevelEntityReference> AboutInternal { get; set; }
		public List<WMA.Outline> About { get; set; }

		public List<Guid> AboutUids { get; set; }
		public List<Guid> RelevantDataSetForUids { get; set; }
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
		public Organization DataProviderOld { get; set; } = new Organization();
		public WMA.Outline DataProvider { get; set; }
		public Guid DataProviderUID { get; set; }
        //helper
        public Guid PrimaryAgentUID { get; set; }

		/// <summary>
		/// NOTE: cannot initialize here, as can lead to a stack overflow
		/// </summary>
		public Organization PrimaryOrganization { get; set; }

        /// <summary>
        /// Data Set Time Period
        /// Short- and long-term post-award reporting intervals including start and end dates.
        /// </summary>
        public List<DataSetTimeFrame> DataSetTimePeriod { get; set; } = new List<DataSetTimeFrame>();

		/// <summary>
		/// The JSON for DataSetTimePeriod
		/// </summary>
		public string DataSetTimePeriodJson { get; set; }
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

		#region Import
		public int DataProviderId { get; set; }
		public string DataProviderName { get; set; }
		public string DataProviderCTID { get; set; }
		public List<int> AssessmentIds { get; set; } = new List<int>();
		public List<int> CredentialIds { get; set; } = new List<int>();
		public List<int> LearningOpportunityIds { get; set; } = new List<int>();
		#endregion

	}
	public class DataSetProfileSummary : DataSetProfile
	{

	}

    public class Entity_DataSetProfile
	{
		public int Id { get; set; }
		public int EntityId { get; set; }
		public int DataSetProfileId { get; set; }
		public System.DateTime Created { get; set; }
	}
}
