using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public string Name_Map { get; set; }
        public string Address1 { get; set; }
        public string Address1_Map { get; set; }
        //public string Address2 { get; set; }
		public string PostOfficeBoxNumber { get; set; }

		public string City { get; set; }
        public string City_Map { get; set; }
        public string AddressLocality { get { return City; } set { City = value; } } //Alias used for publishing
		public string AddressRegion { get; set; }
		public bool HasShortRegion { get; set; }
        public string AddressRegion_Map { get; set; }
		//
		//public string AddressRegionFull { get; set; }
		public string SubRegion { get; set; }
		public string Country { get; set; }
        public string Country_Map { get; set; }

        public string PostalCode { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public bool IsMainAddress { get; set; }
		public Guid ParentRowId { get; set; }
		public string DisplayAddress(string separator = ", ")
		{
			string address = "";
			if ( !string.IsNullOrWhiteSpace( Address1 ) )
				address = Address1;
			//if ( !string.IsNullOrWhiteSpace( Address2 ) )
			//	address += separator + Address2;
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
        public string LooseDisplayAddress( string separator = ", " ) //For easier geocoding
        {
            return
                ( string.IsNullOrWhiteSpace( City ) ? "" : City + separator ) +
                ( string.IsNullOrWhiteSpace( AddressRegion ) ? "" : AddressRegion + separator ) +
                ( string.IsNullOrWhiteSpace( PostalCode ) ? "" : PostalCode + " " ) +
                ( string.IsNullOrWhiteSpace( Country ) ? "" : Country );
        }
        public bool HasAddress()
		{
			bool hasAddress = true;

			if ( string.IsNullOrWhiteSpace( Address1 )
			//&& string.IsNullOrWhiteSpace( Address2 )
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
        public GeoCoordinates GeoCoordinates { get; set; }


		public List<ContactPoint> ContactPoint { get; set; }

	}
	//

}
