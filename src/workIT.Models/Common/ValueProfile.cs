using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
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
		public Enumeration CreditUnitType { get; set; } = new Enumeration();


		/// <summary>
		/// The level of credit associated with the credit awarded or required.
		/// Concept
		/// Scheme: AudienceLevel
		/// </summary>
		public Enumeration CreditLevelType { get; set; } = new Enumeration();


		/// <summary>
		/// Optional description of the value, using either a string value or as a language map
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// A percentage for this purpose. 
		/// Do not use if providing value
		/// qdata:percentage
		/// </summary>
		public decimal Percentage { get; set; }

		public Enumeration Subject { get; set; } = new Enumeration();

		/// <summary>
		/// A single value for this purpose. 
		/// </summary>
		public decimal Value { get; set; }
		public decimal MinValue { get; set; }
		public decimal MaxValue { get; set; }
		public bool HasData()
		{
			if ( Value > 0 || MinValue > 0 || Percentage > 0
				|| ( Description ?? "" ).Length > 2 )
			{
				return true;
			}

			return false;
		}
	}
}
