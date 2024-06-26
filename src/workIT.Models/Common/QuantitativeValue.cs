﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
	public class QuantitativeValue
	{
		//public int Id { get; set; }
		//public int EntityId { get; set; }

		//Used for QV related to credits. Will not use for financial
		public Enumeration CreditUnitType { get; set; } = null;
		public int CreditTypeId { get; set; }

		public string UnitText { get; set; }
		public string Label { get; set; }
		public decimal? Value { get; set; }
		public decimal? MinValue { get; set; }
		public decimal? MaxValue { get; set; }
		public decimal? Percentage { get; set; }
		
		public string Description { get; set; }
		//helper
		public bool IsRange {
			get
			{
				//have to consider if minValue could be zero?
				if ( MinValue >= 0 && MaxValue > 0 )
					return true;
				else
					return false;
			}
		}

		public bool HasData()
		{
			if ( Value != 0 || MinValue != 0 || MaxValue != 0 || Percentage != 0
				|| ( Description ?? "" ).Length > 2 )
			{
				return true;
			}

			return false;
		}
		public string Summary()
		{
			if ( !HasData() )
				return "";

			var summary = "";
			if ( Percentage > 0 )
			{
				return string.Format( "{0}% {1}", Percentage, Description ?? "" );
			}
			else if ( Value > 0 )
			{
				//check if integer
				return string.Format( "{0} {1}", Value, Description ?? "" );
			}
			else if ( MinValue != 0 && MaxValue != 0 )
			{
				//check if integer
				return string.Format( "{0} to {1} {2}", MinValue, MaxValue, Description ?? "" );
			}
			else
				return Description;

		
		}
		/// <summary>
		/// To be valid:
		/// - either 
		///		- just description
		///		- description plus value(s)
		///		- or unit plus a value or a range
		/// Check inconsistancies
		/// </summary>
		/// <returns></returns>
		public bool IsValid( ref string message )
		{
			message = "";
			//if has unit text, must have a value
			if ( CreditTypeId > 0 || ( UnitText ?? "" ).Length > 0 )
			{
				if ( Value == 0 && MinValue == 0 && MaxValue == 0 )
					message = "Credit Unit Type/Text is entered but no values have been provided.";
				return false;
			}

			if ( !HasData() )
			{
				//if no data, should this return true or false?
				//incumbant upon user to first use HasData!
				message = "No data found.";
				return false;
			}

			//if not using format method, we may not have a consistant state.
			//if values entered without unit, reject
			if ( Value > 0 || MinValue > 0 || MaxValue > 0 || Percentage > 0 )
			{
				if ( CreditTypeId == 0 && string.IsNullOrWhiteSpace( UnitText ) && string.IsNullOrWhiteSpace( Description ) )
				{
					message = "A Credit Unit Type/Text or description must be entered if values are provided.";
					return false;
				}
			}

			return true;
		}
	}
}
