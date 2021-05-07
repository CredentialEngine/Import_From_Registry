using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;

using WMA = workIT.Models.API;

namespace workIT.Models.Elastic
{
	public class JurisdictionProfile
	{
		public JurisdictionProfile()
		{
		}

		public bool GlobalJurisdiction { get; set; }

		public string Description { get; set; }
		public Address MainJurisdiction { get; set; } //= new Address();

		public List<Address> JurisdictionException { get; set; }// = new List<Address>();
		//this will probably not be necessary, as will have named properties.
		public string AssertedInType { get; set; }
		public WMA.Outline AssertedBy { get; set; } 
	}

	//public class JurisdictionAssertionProfile : JurisdictionProfile
	//{
	//	public JurisdictionAssertionProfile()
	//	{
	//	}
	//	//this will probably not be necessary, as will have named properties.
	//	public string AssertedInType { get; set; }
	//	public TopLevelEntityReference AssertedBy { get; set; } = new TopLevelEntityReference();
	//}
}
