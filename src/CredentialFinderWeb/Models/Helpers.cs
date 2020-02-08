using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CredentialFinderWeb.Models
{
	//Bypass string size limitations on inbound requests
	public class Helpers
	{
		public static T BindJsonModel<T>( Stream requestStream, string inputPropertyName = "" )
		{
			requestStream.Seek( 0, SeekOrigin.Begin );
			var rawString = new StreamReader( requestStream ).ReadToEnd();
			var json = JObject.Parse( rawString );
			if ( !string.IsNullOrWhiteSpace( inputPropertyName ) )
			{
				return json[ inputPropertyName ].ToObject<T>( new JsonSerializer() { NullValueHandling = NullValueHandling.Ignore } );
			}
			else
			{
				return json.ToObject<T>( new JsonSerializer() { NullValueHandling = NullValueHandling.Ignore } );
			}
		}
	}

}