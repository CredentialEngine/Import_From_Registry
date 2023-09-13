using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using workIT.Models.Common;

namespace workIT.Models.PathwayBuilder
{
	public class ApiResponse
	{
		public ApiResponse()
		{
			Messages = new List<string>();
		}
		
		public ApiResponse( object data, bool valid, List<string> messages = null, object extra = null )
		{
			Data = data;
			Valid = valid;
			Messages = messages;
			Extra = extra;
		}
		public int PathwayId { get; set; }
		public bool Valid { get; set; }

		public List<string> Messages { get; set; }
		public List<StatusMessage> ObjectMessages { get; set; }
		public object Data { get; set; } = null;

		public object Extra { get; set; } = null;
	}
}