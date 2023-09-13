using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RA.Models.Input.profiles.QData
{
	/// <summary>
	/// DataSet Profile
	/// Particular characteristics or properties of a data set and its records.
	/// Required:
	/// - CTID
	/// - About
	/// qdata:DataSetProfile
	/// <see href="https://credreg.net/qdata/terms/DataSetProfile"/>
	/// </summary>
	public class DataSetProfile
	{
		/// <summary>
		/// CTID - Required 
		/// </summary>
		public string CTID { get; set; }

		/// CTID/URI
		/// <summary>
		/// Subject matter of the resource.
		/// Means to point to a credential where data is published by a third party.
		/// REQUIRED when dataSetProfile published separately.
		/// CTID/URI
		/// schema:about
		/// </summary>
		public List<EntityReference> About { get; set; } = new List<EntityReference>();

		///// <summary>
		///// Entity describing the process by which a credential, assessment, organization, or aspects of it, are administered.
		///// <see href="https://credreg.net/ctdl/terms/administrationProcess"/>
		///// </summary>
		//public List<ProcessProfile> AdministrationProcess { get; set; } = new List<ProcessProfile>();

		/// <summary>
		/// Description
		/// NOT Required 
		/// </summary>
		public string Description { get; set; }
		/// <summary>
		/// LanguageMap for Description
		/// </summary>
		public LanguageMap Description_Map { get; set; } = new LanguageMap();

		#region Instruction program and helpers
		/// <summary>
		/// Instructional Program Type
		/// Type of instructional program; select from an existing enumeration of such types.
		/// </summary>
		//public List<FrameworkItem> InstructionalProgramType { get; set; } = new List<FrameworkItem>();
		//public List<string> AlternativeInstructionalProgramType { get; set; } = new List<string>();

		/// <summary>
		/// Helper property - provided a list of CIP codes and API will validate and format 
		/// List of valid Classification of Instructional Program codes. See:
		/// https://nces.ed.gov/ipeds/cipcode/search.aspx?y=55
		/// </summary>
		public List<string> CIP_Codes { get; set; } = new List<string>();
		#endregion

		///// <summary>
		///// Jurisdiction Profile
		///// Geo-political information about applicable geographic areas and their exceptions.
		///// <see href="https://credreg.net/ctdl/terms/JurisdictionProfile"/>
		///// </summary>
		//public List<Jurisdiction> Jurisdiction { get; set; } = new List<Jurisdiction>();

		/// <summary>
		/// Name of dataset profile
		/// Not required
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		///  LanguageMap for Name
		/// </summary>
		public LanguageMap Name_Map { get; set; } = new LanguageMap();

		/// <summary>
		/// Relevant Data Set For
		/// Data set for the entity being referenced.
		/// REQUIRED when dataSetProfile published separately.
		/// Inverse property	- point back to the parent
		/// 21-02-19 mparsons	Removing these from range: HoldersProfile, EarningsProfile, EmploymentOutlook
		///						- adding credential, assessment, and lopp
		/// 21-05-10 mparsons	- effectively obsolete outside of HoldersProfile, EarningsProfile, EmploymentOutlook and the latter are moving to be obsolete
		/// </summary>
		[Obsolete]
		public List<string> RelevantDataSetFor { get; set; } = new List<string>();


		/// <summary>
		/// Authoritative source of an entity's information.
		/// URL 
		/// </summary>
		public string Source { get; set; }

		/// <summary>
		/// Credentialing organization or a third party providing the data.
		/// Required when publishing the DataSetProfile directly.
		/// </summary>
		public OrganizationReference  DataProvider { get; set; } = new OrganizationReference();

		/// <summary>
		/// Data Set Time Period
		/// Short- and long-term post-award reporting intervals including start and end dates.
		/// </summary>
		public List<DataSetTimeFrame> DataSetTimePeriod { get; set; } = new List<DataSetTimeFrame>();	

		/// <summary>
		/// Data Suppression Policy
		/// Description of a data suppression policy for earnings and employment data when cell size is below a certain threshold to ensure an individual's privacy and security.
		/// </summary>
		public string DataSuppressionPolicy { get; set; }
		public LanguageMap DataSuppressionPolicy_Map { get; set; } = new LanguageMap();

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
		public LanguageMap SubjectIdentification_Map { get; set; } = new LanguageMap();

	}
}
