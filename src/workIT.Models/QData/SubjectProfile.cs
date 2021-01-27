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
	/// Categories of subject in the data set.
	/// qdata:SubjectProfile
	/// </summary>
	public class SubjectProfile
	{		
		public string Description { get; set; }
		//public LanguageMap Description_Map { get; set; } = new LanguageMap();

		public string Name { get; set; }
		//public LanguageMap Name_Map { get; set; } = new LanguageMap();

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
		public Enumeration SubjectType { get; set; } = new Enumeration();

		/// <summary>
		/// Quantitative values and percentages for a subject category in the data set.
		/// qdata:subjectValue
		/// </summary>
		public List<QuantitativeValue> SubjectValue { get; set; } = new List<QuantitativeValue>();
	}
}
