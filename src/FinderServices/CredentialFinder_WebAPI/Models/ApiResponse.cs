using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CredentialFinderWebAPI.Models
{
	public class ApiResponse
	{
		public ApiResponse()
		{
			Messages = new List<string>();
		}
		public ApiResponse( object result, bool successful = true, List<string> messages = null )
		{
			Result = result;
			Successful = successful;
			Messages = messages;
		}

		public object Result { get; set; }

		public bool Successful { get; set; }

		public List<string> Messages { get; set; }

	}

    public class PathwayApiResponse
    {
        public PathwayApiResponse()
        {
            Messages = new List<string>();
        }
        public PathwayApiResponse( object result, bool successful = true, List<string> messages = null, object extra = null )
        {
            Data = result;
            Valid = successful;
            Messages = messages;
        }

        public object Data { get; set; }

        public bool Valid { get; set; }

        public List<string> Messages { get; set; }
        public object Extra { get; set; } = null;
    }
}