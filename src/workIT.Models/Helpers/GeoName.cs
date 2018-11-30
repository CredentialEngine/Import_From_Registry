using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Helpers
{
	public class GeoName
	{
		//public Timezone timezone { get; set; }
		public object timezone { get; set; }
		public Bbox bbox { get; set; }
		public string asciiName { get; set; }
		public int astergdem { get; set; }
		public string countryId { get; set; }
		public string fcl { get; set; }
		public int srtm3 { get; set; }
		public string countryCode { get; set; }
		public string adminId1 { get; set; }
		public string lat { get; set; }
		public string fcode { get; set; }
		public string continentCode { get; set; }
		public int elevation { get; set; }
		public string adminCode1 { get; set; }
		public string lng { get; set; }
		public int geonameId { get; set; }
		public string toponymName { get; set; }
		public string adminTypeName { get; set; }
		public int population { get; set; }
		public string wikipediaURL { get; set; }
		public string adminName5 { get; set; }
		public string adminName4 { get; set; }
		public string adminName3 { get; set; }
		public List<AlternateName> alternateNames { get; set; }
		public string adminName2 { get; set; }
		public string name { get; set; }
		public string fclName { get; set; }
		public string countryName { get; set; }
		public string fcodeName { get; set; }
		public string adminName1 { get; set; }
	}

	public class Timezone
	{
		public int gmtOffset { get; set; }
		public string timeZoneId { get; set; }
		public int dstOffset { get; set; }
	}

	public class Bbox
	{
		public decimal east { get; set; }
		public decimal south { get; set; }
		public decimal north { get; set; }
		public decimal west { get; set; }
		public int accuracyLevel { get; set; }
	}

	public class AlternateName
	{
		public string name { get; set; }
		public string lang { get; set; }
		public bool? isShortName { get; set; }
		public bool? isPreferredName { get; set; }
		public bool? isColloquial { get; set; }
	}

}
