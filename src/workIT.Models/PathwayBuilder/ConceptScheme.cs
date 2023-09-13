using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace workIT.Models.PathwayBuilder
{
    public class ConceptScheme
    {
    }
	//

    public class Concept
    {
        public int Id { get; set; }
        public string CTID { get; set; }
        public string URI { get; set; }

        public string Name { get; set; }
        public string CodedNotation { get; set; }
        public string Description { get; set; }
		public string Icon { get; set; }
	}
	//

	public class URIMap
	{
		public URIMap()
		{
			From = new List<string>();
			To = new List<string>();
		}

		public URIMap( List<string> from, List<string> to )
		{
			From = from ?? new List<string>();
			To = to ?? new List<string>();
		}

		public List<string> From { get; set; }
		public List<string> To { get; set; }
	}
	//
}