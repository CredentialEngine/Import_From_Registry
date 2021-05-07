using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using CredentialFinderWebAPI.Models;
using CredentialFinderWebAPI.Services;

using workIT.Services;
using workIT.Utilities;

namespace CredentialFinderWebAPI.Controllers
{
    public class ToolsController : ApiController
    {
		[ApiExplorerSettings( IgnoreApi = true )]
		[HttpGet, Route( "tools/normalizeAddresses/{authorization}/{maxRecords}" )]
		public void normalizeAddresses( string authorization, int maxRecords = 250 )
		{
			var response = new ApiResponse();

			if ( maxRecords > 500 )
				maxRecords = 500;
			string message = "";
			new ProfileServices().NormalizeAddressesExternal( authorization, maxRecords, ref message );
			response.Messages.Add( message );
			response.Successful = true;
			//return response;

			new BaseController().SendResponse( message );
		}

		//[ApiExplorerSettings( IgnoreApi = true )]
		//[HttpGet, Route( "tools/normalizeAddresses2/{authorization}/{maxRecords}" )]
		//public string normalizeAddresses2( string authorization, int maxRecords = 250 )
		//{
		//	var response = new ApiResponse();

		//	if ( maxRecords > 500 )
		//		maxRecords = 500;
		//	string message = "";
		//	new ProfileServices().NormalizeAddressesExternal( authorization, maxRecords, ref message );
		//	response.Messages.Add( message );
		//	response.Successful = true;
		//	//return response;

		//	return message;
		//}
	}
}
