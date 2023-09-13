using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RA.Models.Input
{
	/// <summary>
	/// Class for providing values for a property like FinancialAssistance.FinancialAssistanceValue or  LearningOpportunity.CreditValue.
	/// Recommended: Provide a single value OR a Min and Max value or a Percentage and provide a valid concept in UnitText
	/// If UnitText is provided, then a value is required.	
	/// Alternatively just a description can be provided if value is more complicated than can be expressed using either the Value or MinValue/MaxValue
	/// Edits:
	/// -	Where no values are entered for a QuantitativeValue, a description is required, the UnitText is not enough  
	/// -	Any of the values must be non-negative
	/// -	Currently can only have one numeric type: Value, or Min/MaxValue or Percentage
	/// -	Either UnitText or Description are required when values are present in the QuantitativeValue
	/// -	If UnitText is provided, then a value is required
	/// </summary>
	public class QuantitativeValue
	{
		/// <summary>
		/// Provide a valid concept from the CreditUnitType concept scheme, with or without the namespace. For example:
		/// creditUnit:DegreeCredit or ContinuingEducationUnit
		/// <see cref="https://credreg.net/ctdl/terms/creditUnitType"/> 
		/// If this object is a monetary purpose, the UnitText would typically be the related currency for the value (example: "USD")
		/// </summary>
		public string UnitText { get; set; }

		/// <summary>
		/// A single value for this purpose. 
		/// Do not use if providing a percentage, or minimum and maximum value.
		/// </summary>
		public decimal Value { get; set; }

		/// <summary>
		/// Minimum value for this purpose. If provided, a maximum value must also be provided
		/// </summary>
		public decimal MinValue { get; set; }

		/// <summary>
		/// Maximum value for this purpose.
		/// </summary>
		public decimal MaxValue { get; set; }

		/// <summary>
		/// A percentage for this purpose. 
		/// Best practice is to treat the value of this property as a verbatim percentage; for example, a value of 1.5 should be interpreted as 1.5%
		/// Do not use if providing any of: Value, Minimum or Maximum value.
		/// qdata:percentage
		/// </summary>
		public decimal? Percentage { get; set; }

		/// <summary>
		/// Optional description of the value, using either a string value or as a language map
		/// NOTE: the description is required if there are no quanitities (Value, Percentage, Minimum or Maximum value.), or there is a quantity without a unitText
		/// </summary>
		public string Description { get; set; }
		public LanguageMap Description_Map { get; set; } = new LanguageMap();
	}
}
