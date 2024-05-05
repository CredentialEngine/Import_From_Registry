using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
namespace RA.Models.JsonV2.QData
{
	/// <summary>
	/// Data Profile
	/// Entity describing the attributes of the data set, its subjects and their values.
	/// qdata:DataProfile
	/// </summary>
	public class DataProfile
	{
		/// <summary>
		/// The type of the entity
		/// </summary>
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "qdata:DataProfile";

		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }

		/// <summary>
		/// Describes whether and how the provided earnings have been adjusted for factors such as inflation, participant demographics and economic conditions.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:adjustment" )]
		public LanguageMap Adjustment { get; set; }

		/// <summary>
		/// Type of administrative record used; e.g. W2, 1099, and Unemployment Insurance Wage Record.
		/// skos:Concept
		/// <see cref="https://credreg.net/qdata/terms/administrativeRecordType"/>
		/// adminRecord:Tax1099
		/// adminRecord:TaxW2
		/// adminRecord:UnemploymentInsurance
		/// </summary>
		[JsonProperty( PropertyName = "qdata:administrativeRecordType" )]
		public CredentialAlignmentObject AdministrativeRecordType { get; set; }

		//public string AdministrativeRecordType { get; set; }

		/// <summary>
		/// Number of credential holders in the reporting group for which employment and earnings data is included in the data set.
		/// qdata:dataAvailable
		/// </summary>
		[JsonProperty( PropertyName = "qdata:dataAvailable" )]
		public List<QuantitativeValue> DataAvailable { get; set; } 

		/// <summary>
		/// Number of credential holders in the reporting group for which employment and earnings data has not been included in the data set.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:dataNotAvailable" )]
		public List<QuantitativeValue> DataNotAvailable { get; set; }


		/// <summary>
		/// Earnings rate for a demographic category.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:demographicEarningsRate" )]
		public List<QuantitativeValue> DemographicEarningsRate { get; set; } 

		/// <summary>
		/// Employment rate for a demographic category.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:demographicEmploymentRate" )]
		public List<QuantitativeValue> DemographicEmploymentRate { get; set; } 


		/// <summary>
		/// Reference to an entity describing aggregate earnings.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:earningsAmount" )]
		public List<MonetaryAmount> EarningsAmount { get; set; } 

		/// <summary>
		/// Definition of "earnings" used by the data source in the context of the reporting group.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:earningsDefinition" )]
		public LanguageMap EarningsDefinition { get; set; } 

		/// <summary>
		/// Reference to an entity describing median earnings as well as earnings at various percentiles.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:earningsDistribution" )]
		public List<MonetaryAmountDistribution> EarningsDistribution { get; set; } 

		/// <summary>
		/// Statement of any work time or earnings threshold used in determining whether a sufficient level of workforce attachment has been achieved to qualify as employed during the time period of the data set.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:earningsThreshold" )]
		public LanguageMap EarningsThreshold { get; set; } 

		/// <summary>
		/// Statement of criteria used to determine whether sufficient levels of work time and/or earnings have been met to be considered employed during the earning time period.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:employmentDefinition" )]
		public LanguageMap EmploymentDefinition { get; set; }

		/// <summary>
		/// Projected employment estimate.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:employmentOutlook" )]
		public List<QuantitativeValue> EmploymentOutlook { get; set; }

		/// <summary>
		/// Rate computed by dividing the number of holders or subjects meeting the data set's criteria of employment (meetEmploymentCriteria) by the number of holders or subjects for which data was available (dataAvailable).
		/// </summary>
		[JsonProperty( PropertyName = "qdata:employmentRate" )]
		public List<QuantitativeValue> EmploymentRate { get; set; }

		/// <summary>
		/// Faculty-to-Student Ratio
		/// Ratio of the number of teaching faculty to the number of students.
		/// The expression of the ratio should feature the number of faculty first, followed by the number of students, e.g., "1:10" to mean "one faculty per ten students".
		/// 
		/// </summary>
		[JsonProperty( PropertyName = "qdata:facultyToStudentRatio" )]
		public string FacultyToStudentRatio { get; set; }

		/// <summary>
		///  Number of credential holders in the final data collection and reporting.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:holdersInSet" )]
		public List<QuantitativeValue> HoldersInSet { get; set; }

		/// <summary>
		/// Mechanism by which income is determined; i.e., actual or annualized earnings.
		/// skos:Concept
		/// <see cref="https://credreg.net/qdata/terms/IncomeDeterminationMethod"/>
		/// incomeDetermination:ActualEarnings 
		/// incomeDetermination:AnnualizedEarnings
		/// </summary>
		[JsonProperty( PropertyName = "qdata:incomeDeterminationType" )]
		public CredentialAlignmentObject IncomeDeterminationType { get; set; }

		/// <summary>
		/// Employment rate for an industry category.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:industryRate" )]
		public List<QuantitativeValue> IndustryRate { get; set; } 

		/// <summary>
		/// Number of holders that do not meet the prescribed employment threshold in terms of earnings or time engaged in work as defined for the data set (employmentDefinition).
		/// </summary>
		[JsonProperty( PropertyName = "qdata:insufficientEmploymentCriteria" )]
		public List<QuantitativeValue> InsufficientEmploymentCriteria { get; set; }

		/// <summary>
		/// Number of holders that meet the prescribed employment threshold in terms of earnings or time engaged in work as defined for the data set (employmentDefinition).
		/// </summary>
		[JsonProperty( PropertyName = "qdata:meetEmploymentCriteria" )]
		public List<QuantitativeValue> MeetEmploymentCriteria { get; set; } 

		/// <summary>
		/// Non-holders who departed or are likely to depart higher education prematurely.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:nonCompleters" )]
		public List<QuantitativeValue> NonCompleters { get; set; }

		/// <summary>
		/// Non-holder subject actively pursuing the credential through a program or assessment.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:nonHoldersInSet" )]
		public List<QuantitativeValue> NonHoldersInSet { get; set; }

		/// <summary>
		/// Employment rate for an occupation category.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:occupationRate" )]
		public List<QuantitativeValue> OccupationRate { get; set; }

		/// <summary>
		/// Rate computed by dividing the number of subjects passing an assessment by the total number taking the assessment.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:passRate" )]
		public List<QuantitativeValue> PassRate { get; set; }

		/// <summary>
		///  Reference to an entity describing median earnings as well as earnings at various percentiles for holders or subjects in the region.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:regionalEarningsDistribution" )]
		public List<QuantitativeValue> RegionalEarningsDistribution { get; set; } 

		/// <summary>
		/// Rate computed by dividing the number of holders or subjects in the region meeting the data set's criteria of employment (meetEmploymentCriteria) by the number of holders or subjects in the region for which data was available (dataAvailable).
		/// qdata:regionalEmploymentRate
		/// </summary>
		[JsonProperty( PropertyName = "qdata:regionalEmploymentRate" )]
		public List<QuantitativeValue> RegionalEmploymentRate { get; set; } 

		/// <summary>
		/// Number of people employed in the area of work (e.g., industry, occupation) in which the credential provided preparation.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:relatedEmployment" )]
		public List<QuantitativeValue> RelatedEmployment { get; set; }

        /// <summary>
        /// Type of subject included or excluded from the data set.
        /// 23-??-?? No longer has a domain of dataProfile
        /// skos:Concept
        /// ConceptScheme: qdata:SubjectCategory
        /// <see href="https://credreg.net/qdata/terms/SubjectCategory"/>
        /// </summary>        
        [Obsolete]
        [JsonProperty( PropertyName = "qdata:subjectType" )]
        public CredentialAlignmentObject SubjectType { get; set; }	

        /// <summary>
        /// Quantitative values and percentages for a subject category (SubjectType) in the data set.
		/// 23-??-?? No longer has a domain of dataProfile
        /// </summary>        
		[Obsolete]
		[JsonProperty( PropertyName = "qdata:subjectValue" )]
        public List<QuantitativeValue> SubjectValue { get; set; }


        /// <summary>
        /// Category of subject excluded from the data.
        /// Feb,2021 change type to QuantitativeValue
        /// </summary>
        [JsonProperty( PropertyName = "qdata:subjectExcluded" )]
		public List<QuantitativeValue> SubjectExcluded { get; set; }

		//deprecated Feb/2021
		///// <summary>
		///// Category of subject included in the data.
		///// </summary>
		//[JsonProperty( PropertyName = "qdata:subjectIncluded" )]
		//public List<SubjectProfile> SubjectIncluded { get; set; } 

		/// <summary>
		/// Total credential holders and non-holders in the final data collection and reporting.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:subjectsInSet" )]
		public List<QuantitativeValue> SubjectsInSet { get; set; } 

		/// <summary>
		/// Number of holders that meet the prescribed employment threshold in terms of earnings or time engaged in work as defined for the data set (employmentDefinition).
		/// qdata:sufficientEmploymentCriteria
		/// </summary>
		[JsonProperty( PropertyName = "qdata:sufficientEmploymentCriteria" )]
		public List<QuantitativeValue> SufficientEmploymentCriteria { get; set; }

		/// <summary>
		/// Number of people employed outside the area of work (e.g., industry, occupation) in which the credential provided preparation.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:unrelatedEmployment" )]
		public List<QuantitativeValue> UnrelatedEmployment { get; set; } 

		/// <summary>
		/// Statement of earnings thresholds used in determining whether a sufficient level of workforce attachment has been achieved to qualify as employed during the chosen employment and earnings time period.
		/// </summary>
		[JsonProperty( PropertyName = "qdata:workTimeThreshold" )]
		public LanguageMap WorkTimeThreshold { get; set; }

		[JsonProperty( PropertyName = "qdata:totalWIOACompleters" )]
		public List<QuantitativeValue> TotalWIOACompleters { get; set; }

		[JsonProperty( PropertyName = "qdata:totalWIOAParticipants" )]
		public List<QuantitativeValue> TotalWIOAParticipants { get; set; }

		[JsonProperty( PropertyName = "qdata:totalWIOAExiters" )]
		public List<QuantitativeValue> TotalWIOAExiters { get; set; } 
	}
}
