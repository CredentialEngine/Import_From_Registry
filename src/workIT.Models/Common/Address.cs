using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.ProfileModels;

namespace workIT.Models.Common
{
    [Serializable]
    public class Address : BaseObject
	{
		public Address()
		{
			GeoCoordinates = new GeoCoordinates();
			ContactPoint = new List<ContactPoint>();
		}
		public string Name { get; set; }
		[JsonIgnore]
		public string Name_Map { get; set; }
		public string Description { get; set; }
		public string StreetAddress { get; set; }
		[JsonIgnore]
		public string Address1_Map { get; set; }
        //public string Address2 { get; set; }
		public string PostOfficeBoxNumber { get; set; }

		public string AddressLocality { get; set; }
        public string City_Map { get; set; }
       
		public string AddressRegion { get; set; }
		public bool HasShortRegion { get; set; }
        public string AddressRegion_Map { get; set; }
		//
		//public string AddressRegionFull { get; set; }
		public string SubRegion { get; set; }
		public string AddressCountry { get; set; }
		[JsonIgnore]
		public string Country_Map { get; set; }

        public string PostalCode { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		/// <summary>
		/// Identifier
		/// Definition:	Means of identifying a resource, typically consisting of an alphanumeric token and a context or scheme from which that token originates.
		/// 21-11-03 MP - we should move to just using Identifier not Entity_IdentifierValue
		/// </summary>	
		public List<Entity_IdentifierValue> Identifier { get; set; } = new List<Entity_IdentifierValue>();
		public string IdentifierJson { get; set; }

		public bool IsMainAddress { get; set; }
		[JsonIgnore]
		public Guid ParentRowId { get; set; }
		public string DisplayAddress(string separator = ", ")
		{
			string address = "";
			if ( !string.IsNullOrWhiteSpace( StreetAddress ) )
				address = StreetAddress;
			//if ( !string.IsNullOrWhiteSpace( Address2 ) )
			//	address += separator + Address2;
			if ( !string.IsNullOrWhiteSpace( PostOfficeBoxNumber ) )
				address += separator + "P.O. " + PostOfficeBoxNumber;
			if ( !string.IsNullOrWhiteSpace( AddressLocality ) )
				address += separator + AddressLocality;
			if ( !string.IsNullOrWhiteSpace( SubRegion ) )
				address += separator + SubRegion;
			if ( !string.IsNullOrWhiteSpace( AddressRegion ) )
				address += separator + AddressRegion;
			if ( !string.IsNullOrWhiteSpace( PostalCode ) )
				address += " " + PostalCode;
			if ( !string.IsNullOrWhiteSpace( AddressCountry ) )
				address += separator + AddressCountry;
			return address;
		}
        public string LooseDisplayAddress( string separator = ", " ) //For easier geocoding
        {
            return
                ( string.IsNullOrWhiteSpace( AddressLocality ) ? "" : AddressLocality + separator ) +
                ( string.IsNullOrWhiteSpace( AddressRegion ) ? "" : AddressRegion + separator ) +
                ( string.IsNullOrWhiteSpace( PostalCode ) ? "" : PostalCode + " " ) +
                ( string.IsNullOrWhiteSpace( AddressCountry ) ? "" : AddressCountry );
        }
        public bool HasAddress()
		{
			bool hasAddress = true;

			if ( string.IsNullOrWhiteSpace( StreetAddress )
			//&& string.IsNullOrWhiteSpace( Address2 )
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

            if ( ContactPoint == null || ContactPoint.Count == 0 )
                return false;

            return hasData;
        }
        /// <summary>
        /// Note: the GeoCoordinates use the rowId from the parent for the FK. If the parent of the address object can have other regions, then there will be a problem!
        /// This may lead to the addition of concrete rowIds as needed to a parent with an address.
        /// </summary>
        public GeoCoordinates GeoCoordinates { get; set; }


		public List<ContactPoint> ContactPoint { get; set; }

	}
	//

}
