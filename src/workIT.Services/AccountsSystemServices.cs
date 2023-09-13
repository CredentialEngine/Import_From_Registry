using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using workIT.Models.AccountsSystem;
using workIT.Utilities;

namespace workIT.Services
{
	public class AccountsSystemServices
	{
		public static QueryResult<AccountsSystemOrganization> OrganizationSearch( OrganizationQuery query )
		{
			var result = new QueryResult<AccountsSystemOrganization>();
			var user = AccountServices.GetCurrentUser();

			if ( query.ForCurrentUserOnly )
			{
				query.ForUserAccountEmails = new List<string>() { AccountServices.GetCurrentUser().Email };
			}
			/*
			else if( !AccountServices.IsUserAnAdmin() ) //Only allow admins to search for organizations they aren't a part of
			{
				//May want to log this?
				result.Valid = false;
				result.Status = "Error: You do not have permission to perform that query.";
				return result;
			}
			*/

			query.Password = UtilityManager.GetAppKeyValue( "CEAccountSystemStaticPassword", "" );
			var apiURL = UtilityManager.GetAppKeyValue( "CEAccountsSystemAPIURL" ) + "/organizationsearch";

			var requestContent = new StringContent( JsonConvert.SerializeObject( query ), System.Text.Encoding.UTF8, "application/json" );

			System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
			var response = new HttpClient().PostAsync( apiURL, requestContent ).Result;
			if ( !response.IsSuccessStatusCode )
			{
				result.Valid = false;
				result.Status = "Error performing search: " + response.ReasonPhrase + " (" + response.StatusCode.ToString() + ")";
				return result;
			}

			try
			{
				var rawContent = response.Content.ReadAsStringAsync().Result;
				var responseContent = JObject.Parse( rawContent );
				result.Valid = ( bool ) responseContent[ "valid" ];
				result.Status = ( string ) responseContent[ "status" ];
				result.TotalResults = ( int ) responseContent[ "data" ][ "TotalResults" ];
				result.Results = responseContent[ "data" ][ "Results" ].Select( m => m.ToObject<AccountsSystemOrganization>() ).ToList();
			}
			catch( Exception ex )
			{
				result.Valid = false;
				result.Status = "Error parsing response: " + ex.Message + ( ex.InnerException != null ? ", Inner Exception: " + ex.InnerException.Message : "" );
				return result;
			}

			return result;
		}
		//
	}
	//
}
