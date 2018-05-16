﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
	public class IdentifierValue : BaseObject
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public string IdentifierType { get; set; }
		public string IdentifierValueCode { get; set; }
	}
}
