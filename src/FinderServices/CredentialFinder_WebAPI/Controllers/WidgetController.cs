using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

using CredentialFinderWebAPI.Models;
using workIT.Services;
using workIT.Models.Common;
using Newtonsoft.Json;

namespace CredentialFinderWebAPI.Controllers
{
    public class WidgetController : BaseController
    {
		//Get the configuration data for a widget based on its name or ID
		[HttpGet, Route( "Widget/GetConfig/{widgetNameOrID}" )]
		public void GetConfig( string widgetNameOrID )
        {
			var v1Data = new Widget();
			var v2Data = new WidgetV2();
			try
			{
				int validNbr = 0;

				if ( ServiceHelper.IsInteger( widgetNameOrID, ref validNbr )) 
				{
					v1Data = WidgetServices.Get( validNbr );
				} else
                    v1Data = WidgetServices.GetByAlias( widgetNameOrID );
            }
			catch
			{
				v1Data = WidgetServices.GetByAlias( widgetNameOrID );
			}

			if( v1Data != null && v1Data.Id > 0 )
			{
				try
				{
					//Extract the JSON data if it is V2 data
					v2Data = JsonConvert.DeserializeObject<WidgetV2>( v1Data.CustomStyles, new JsonSerializerSettings() { Error = IgnoreDeserializationErrors } );
                    //
                    v2Data.HasCredentialPotentialResults = v1Data.HasCredentialPotentialResults;
                    //this should not be necessary
                    v2Data.AllowsCSVExport = v1Data.AllowsCSVExport;
					if ( v2Data.Id == 0 )
					{
						throw new Exception();
					}
				}
				catch
				{
					v2Data.Description = "Error converting WidgetV1 Data";
				}
			}

			SendResponse( new ApiResponse( v2Data, v2Data.Id > 0 ) );
        }
		//

		//Ignore deserialization errors
		private void IgnoreDeserializationErrors( object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e )
		{
			e.ErrorContext.Handled = true;
		}
		//
	}
}