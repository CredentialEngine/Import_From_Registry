using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.API
{
	public class ValueProfile
	{
		public ValueProfile()
		{
		}


		/// <summary>
		/// Provide a valid concept from the CreditUnitType concept scheme, with or without the namespace. For example:
		/// creditUnit:DegreeCredit or ContinuingEducationUnit
		/// <see cref="https://credreg.net/ctdl/terms/creditUnitType"/> 
		/// If this object is a monetary purpose, the UnitText would typically be the related currency for the value (example: "USD")
		/// </summary>
		public List<LabelLink> CreditUnitType { get; set; } 


		/// <summary>
		/// The level of credit associated with the credit awarded or required.
		/// Concept
		/// Scheme: AudienceLevel
		/// </summary>
		public List<LabelLink> CreditLevelType { get; set; } 


		/// <summary>
		/// Optional description of the value, using either a string value or as a language map
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// A percentage for this purpose. 
		/// Do not use if providing value
		/// qdata:percentage
		/// </summary>
		public decimal? Percentage { get; set; }

		public List<LabelLink> Subject { get; set; } 

		/// <summary>
		/// A single value for this purpose. 
		/// </summary>
		public decimal? Value { get; set; }
		public decimal? MinValue { get; set; }
		public decimal? MaxValue { get; set; }
	}
}
