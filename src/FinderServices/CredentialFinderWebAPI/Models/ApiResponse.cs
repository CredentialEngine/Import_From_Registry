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
		public string Result { get; set; }

		public bool Successful { get; set; }

		public List<string> Messages { get; set; }

	}
}