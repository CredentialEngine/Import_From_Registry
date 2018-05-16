using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Elastic
{
	public class Address
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Address1 { get; set; }
		public string Address2 { get; set; }
		public string PostOfficeBoxNumber { get; set; }
		public string City { get; set; }

		public double Latitude { get; set; }
		public double Longitude { get; set; }

		public string AddressRegion { get; set; }
		public string PostalCode { get; set; }
		public string Country { get; set; }
		//      public string DisplayAddress( string separator = ", " )
		//      {
		//          string address = "";
		//          if ( !string.IsNullOrWhiteSpace( Address1 ) )
		//              address = Address1;
		//          if ( !string.IsNullOrWhiteSpace( Address2 ) )
		//              address += separator + Address2;
		//          if ( !string.IsNullOrWhiteSpace( City ) )
		//              address += separator + City;
		//          if ( !string.IsNullOrWhiteSpace( AddressRegion ) )
		//              address += separator + AddressRegion;
		//          if ( !string.IsNullOrWhiteSpace( PostalCode ) )
		//              address += " " + PostalCode;
		//          if ( !string.IsNullOrWhiteSpace( Country ) )
		//              address += separator + Country;
		//	return address;
		//}
	}
}
          
        
