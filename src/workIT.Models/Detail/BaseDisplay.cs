using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ME = workIT.Models.Elastic;

using MPM =workIT.Models.ProfileModels;

namespace workIT.Models.Detail
{
	[Serializable]
	public class BaseDisplay
	{
		public int? Id { get; set; }
		public string CTID { get; set; }
		/// <summary>
		/// name
		/// </summary>
		public string Name { get; set; }
		public string FriendlyName { get; set; }
		public string Description { get; set; }
		public string SubjectWebpage { get; set; }
		public int EntityStateId { get; set; }
		public int EntityTypeId { get; set; }
		public DateTime EntityLastUpdated { get; set; }
		public bool IsReferenceVersion { get; set; }

		public int? OrganizationId { get; set; }
		public string OrganizationName { get; set; }
		public string OrganizationSubjectWebpage { get; set; }

		/// <summary>
		/// The geo-political region in which the described resource is applicable.
		/// </summary>
		public List<ME.JurisdictionProfile> Jurisdiction { get; set; } = new List<ME.JurisdictionProfile>();

	}

	[Serializable]
	public class LabelLink
	{
		public string Label { get; set; }
		public string URL { get; set; }
		public int? Count { get; set; }
		public string Description { get; set; }
	}

	[Serializable]
	public class OrganizationRoleProfile
	{
		/// <summary>
		/// header
		/// </summary>
		public string Label { get; set; }
		/// <summary>
		/// If present, format the heading as a link
		/// </summary>
		public string URL { get; set; }
		public string Description { get; set; }
		/// <summary>
		/// The Role could be with or with a URL
		/// </summary>
		public List<LabelLink> Roles = new List<LabelLink>();
	}

	[Serializable]
	public class ProcessProfileGroup
	{
		public string Label { get; set; }
		public string Description { get; set; }
		public List<MPM.ProcessProfile> ProcessProfile { get; set; } = new List<MPM.ProcessProfile>();
		
	}

	//
	[Serializable]
	public class Address 	{
		public Address()
		{
		}
		public string Name { get; set; }
		public string Address1 { get; set; }
		public string PostOfficeBoxNumber { get; set; }
		public string City { get; set; }
		public string AddressRegion { get; set; }
		//
		public string SubRegion { get; set; }
		public string Country { get; set; }

		public string PostalCode { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public string DisplayAddress( string separator = ", " )
		{
			string address = "";
			if ( !string.IsNullOrWhiteSpace( Address1 ) )
				address = Address1;
			if ( !string.IsNullOrWhiteSpace( City ) )
				address += separator + City;
			if ( !string.IsNullOrWhiteSpace( AddressRegion ) )
				address += separator + AddressRegion;
			if ( !string.IsNullOrWhiteSpace( PostalCode ) )
				address += " " + PostalCode;
			if ( !string.IsNullOrWhiteSpace( Country ) )
				address += separator + Country;
			return address;
		}

		public bool HasAddress()
		{
			bool hasAddress = true;

			if ( string.IsNullOrWhiteSpace( Address1 )
			&& string.IsNullOrWhiteSpace( City )
			&& string.IsNullOrWhiteSpace( AddressRegion )
			&& string.IsNullOrWhiteSpace( PostalCode )
				)
				hasAddress = false;

			return hasAddress;
		}
		public bool HasContactPoints()
		{
			bool hasData = true;

			if ( ContactPoint == null || ContactPoint.Count == 0 )
				return false;

			return hasData;
		}
		/// <summary>
		/// Note: the GeoCoordinates use the rowId from the parent for the FK. If the parent of the address object can have other regions, then there will be a problem!
		/// This may lead to the addition of concrete rowIds as needed to a parent with an address.
		/// </summary>
		//public GeoCoordinates GeoCoordinates { get; set; }


		public List<ContactPoint> ContactPoint { get; set; }

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
			PhoneNumber = new List<string>();
			Email = new List<string>();
			SocialMediaPage = new List<string>();
		}

		public string Name { get; set; }
		/// <summary>
		/// Specification of the type of contact.
		/// </summary>
		public string ContactType { get; set; }

		#region Used by Import
		public List<string> PhoneNumber { get; set; }
		public List<string> Email { get; set; }
		/// <summary>
		/// A social media resource for the resource being described.
		/// </summary>
		public List<string> SocialMediaPage { get; set; }

		#endregion


	}
}
