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
	/// Categories of subject in the data set.
	/// qdata:SubjectProfile
	/// </summary>
	public class SubjectProfile
	{
		[JsonProperty( "@type" )]
		public string Type { get; set; } = "qdata:SubjectProfile";

		[JsonProperty( PropertyName = "ceterms:name" )]
		public LanguageMap Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
		public LanguageMap Description { get; set; }


		/// <summary>
		/// Type of subject included or excluded from the data set.
		/// qdata:subjectType	
		/// skos:Concept
		/// <see cref="https://credreg.net/qdata/terms/SubjectCategory#SubjectCategory"/>
		/// subjectCategory:AssessmentCompleter
		/// subjectCategory:CredentialHolder
		/// subjectCategory:CredentialSeeker
		/// subjectCategory:Enrollee
		/// subjectCategory:FinancialAidRecipient
		/// subjectCategory:Graduate
		/// subjectCategory:HigherLevelCredential
		/// subjectCategory:InFurtherEducation
		/// subjectCategory:InsufficientDataAvailable
		/// subjectCategory:PostCredentialEarnings
		/// subjectCategory:PreCredentialEarnings
		/// </summary>
		[JsonProperty( PropertyName = "qdata:subjectType" )]
		public List<CredentialAlignmentObject> SubjectType { get; set; }

		/// <summary>
		/// Quantitative values and percentages for a subject category in the data set.
		/// qdata:subjectValue
		/// </summary>
		[JsonProperty( PropertyName = "qdata:subjectValue" )]
		public List<QuantitativeValue> SubjectValue { get; set; } 
	}
}
