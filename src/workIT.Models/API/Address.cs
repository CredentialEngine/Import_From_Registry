using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MPM = workIT.Models.ProfileModels;

namespace workIT.Models.API
{
	[Serializable]
	public class Address
	{
		public Address()
		{
		}
		public string Name { get; set; }
		public string Description { get; set; }
		public string StreetAddress { get; set; }
		public string PostOfficeBoxNumber { get; set; }
		public string AddressLocality { get; set; }
		//
		public string AddressRegion { get; set; }
		//
		public string SubRegion { get; set; }
		public string AddressCountry { get; set; }

		public string PostalCode { get; set; }
		public List<IdentifierValue> Identifier { get; set; } = new List<IdentifierValue>();
		public double? Latitude { get; set; }
		public double? Longitude { get; set; }
		public string DisplayAddress( string separator = ", " )
		{
			string address = "";
			if ( !string.IsNullOrWhiteSpace( StreetAddress ) )
				address = StreetAddress;
			if ( !string.IsNullOrWhiteSpace( AddressLocality ) )
				address += separator + AddressLocality;
			if ( !string.IsNullOrWhiteSpace( AddressRegion ) )
				address += separator + AddressRegion;
			if ( !string.IsNullOrWhiteSpace( PostalCode ) )
				address += " " + PostalCode;
			if ( !string.IsNullOrWhiteSpace( AddressCountry ) )
				address += separator + AddressCountry;
			return address;
		}

		public bool HasAddress()
		{
			bool hasAddress = true;

			if ( string.IsNullOrWhiteSpace( StreetAddress )
			&& string.IsNullOrWhiteSpace( AddressLocality )
			&& string.IsNullOrWhiteSpace( AddressRegion )
			&& string.IsNullOrWhiteSpace( PostalCode )
				)
				hasAddress = false;

			return hasAddress;
		}
		public bool HasContactPoints()
		{
			bool hasData = true;

			if ( TargetContactPoint == null || TargetContactPoint.Count == 0 )
				return false;

			return hasData;
		}
		/// <summary>
		/// Note: the GeoCoordinates use the rowId from the parent for the FK. If the parent of the address object can have other regions, then there will be a problem!
		/// This may lead to the addition of concrete rowIds as needed to a parent with an address.
		/// </summary>
		//public GeoCoordinates GeoCoordinates { get; set; }


		public List<ContactPoint> TargetContactPoint { get; set; }

	}
	//
	/// <summary>
	/// Contact Point
	/// Likely will change for display
	/// </summary>
	[Serializable]
	public class ContactPoint
	{
		public ContactPoint()
		{
			Telephone = new List<string>();
			Email = new List<string>();
			SocialMedia = new List<string>();
		}

		public string Name { get; set; }
		/// <summary>
		/// Specification of the type of contact.
		/// </summary>
		public string ContactType { get; set; }

		public List<string> FaxNumber { get; set; } = new List<string>();
		public List<string> Email { get; set; }
		/// <summary>
		/// A social media resource for the resource being described.
		/// </summary>
		public List<string> SocialMedia { get; set; }
		public List<string> Telephone { get; set; }


	}
}
