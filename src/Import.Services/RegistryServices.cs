using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using workIT.Models;
using workIT.Utilities;
using RA.Models.Json;

namespace Import.Services
{
    public class RegistryServices
    {
		public static string credentialEngineAPIKey = UtilityManager.GetAppKeyValue( "CredentialEngineAPIKey" );

		public RegistryServices()
		{
			//Community
		}
		public string Community { get; set; }

		#region Registry search
		public static List<ReadEnvelope> Search( string type, string startingDate, string endingDate, int pageNbr, int pageSize, ref int pTotalRows, ref string statusMessage, string community )
		{

			string document = "";
			string filter = "";
			//includes the question mark
			string serviceUri = GetRegistrySearchUrl( community );
			//from=2016-08-22T00:00:00&until=2016-08-31T23:59:59
			//resource_type=credential
			if ( !string.IsNullOrWhiteSpace( type ) )
				filter = string.Format( "resource_type={0}", type.ToLower() );

			SetPaging( pageNbr, pageSize, ref filter );
			SetDateFilters( startingDate, endingDate, ref filter );

			serviceUri += filter.Length > 0 ? filter : "";
			//future proof
			
			List<ReadEnvelope> list = new List<ReadEnvelope>();
			try
			{
				WebRequest request = WebRequest.Create( serviceUri );

				// If required by the server, set the credentials.
				request.Credentials = CredentialCache.DefaultCredentials;

				var hdr = new WebHeaderCollection
				{
					{ "Authorization", "Token  " + credentialEngineAPIKey }
				};
				request.Headers.Add( hdr );

				HttpWebResponse response = ( HttpWebResponse )request.GetResponse();
				Stream dataStream = response.GetResponseStream();

				// Open the stream using a StreamReader for easy access.
				StreamReader reader = new StreamReader( dataStream );
				// Read the content.
				document = reader.ReadToEnd();

				// Cleanup the streams and the response.
				reader.Close();
				dataStream.Close();
				response.Close();

				//Link contains links for paging
				var hdr2 = response.GetResponseHeader( "Link" );
				Int32.TryParse( response.GetResponseHeader( "Total" ), out pTotalRows );

				//map to the list
				list = JsonConvert.DeserializeObject<List<ReadEnvelope>>( document );

			}
			catch ( Exception exc )
			{
				LoggingHelper.LogError( exc, "RegistryServices.Search" );
			}
			return list;
		}
		public static string GetRegistrySearchUrl( string community = "" )
		{
			if ( string.IsNullOrWhiteSpace( community ) )
			{
				community = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			}
			string serviceUri = UtilityManager.GetAppKeyValue( "credentialRegistrySearch" );
			serviceUri = string.Format( serviceUri, community );
			return serviceUri;
		}
		public static string GetEnvelopeUrl(string envelopeId, string community = "")
		{
			if ( string.IsNullOrWhiteSpace( community ) )
			{
				community = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			}
			string serviceUri = UtilityManager.GetAppKeyValue( "cerGetEnvelope" );

			string registryEnvelopeUrl = string.Format( serviceUri, community, envelopeId );
			return registryEnvelopeUrl;
		}
		public static string GetResourceUrl(string ctid, string community = "")
		{
			if ( string.IsNullOrWhiteSpace( community ) )
			{
				community = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			}
			string serviceUri = UtilityManager.GetAppKeyValue( "credentialRegistryResource" );

			string registryUrl = string.Format( serviceUri, community, ctid );
			//not sure about this anymore
            //actually dependent on the purpose. If doing an import, then need graph
			if ( UtilityManager.GetAppKeyValue( "usingGraphDocuments", true ) )
			{
                registryUrl = registryUrl.Replace( "/resources/", "/graph/" );
			}
			return registryUrl;
		}
		//public static string GetRegistryEnvelopeUrl(string community)
		//{
		//	//the app key should be changed to be more meaningful!!
		//	string serviceUri = UtilityManager.GetAppKeyValue( "cerGetEnvelope" );
		//	serviceUri = string.Format( serviceUri, community );
		//	return serviceUri;
		//}
		public static string GetRegistryUrl(string appKey, string community)
		{
			//requires all urls to have a parameter?
			//or, check if the default community exists
			//also have to handle an absence community
			string serviceUri = UtilityManager.GetAppKeyValue( appKey );
			serviceUri = string.Format( serviceUri, community );
			return serviceUri;
		}

		private static void SetPaging( int pageNbr, int pageSize, ref string where )
		{
			string AND = "";
			if ( where.Length > 0 )
				AND = "&";

			if ( pageNbr > 0 )
			{
				where = where + AND + string.Format( "page={0}", pageNbr );
				AND = "&";
			}
			if ( pageSize > 0 )
			{
				where = where + AND + string.Format( "per_page={0}", pageSize );
				AND = "&";
			}
		}
		private static void SetSortOrder( ref string where )
		{
			string AND = "";
			if ( where.Length > 0 )
				AND = "&";
			where = where + AND + "sort_by=updated_at&sort_order=asc";
		}

		private static void SetDateFilters( string startingDate, string endingDate, ref string where )
		{
			string AND = "";
			if ( where.Length > 0 )
				AND = "&";

			string date = FormatDateFilter( startingDate );
			if ( !string.IsNullOrWhiteSpace( date ) )
			{
				where = where + AND + string.Format( "from={0}", startingDate );
				AND = "&";
			}

			date = FormatDateFilter( endingDate );
			if ( !string.IsNullOrWhiteSpace( date ) )
			{
				where = where + AND + string.Format( "until={0}", endingDate );
				AND = "&";
			}
			//if ( !string.IsNullOrWhiteSpace( endingDate ) && endingDate.Length == 10 )
			//{
			//	where = where + AND + string.Format( "until={0}T23:59:59", endingDate );
			//}
		}
		private static string FormatDateFilter( string date )
		{
			string formatedDate = "";
			if ( string.IsNullOrWhiteSpace( date ) )
				return "";

			//start by checking for just properly formatted date
			if ( !string.IsNullOrWhiteSpace( date ) && date.Length == 10 )
			{
				formatedDate = string.Format( "{0}T00:00:00", date );
			}
			else if ( !string.IsNullOrWhiteSpace( date ) )
			{
				//check if in proper format - perhaps with time provided
				if ( date.IndexOf( "T" ) > 8 )
				{
					formatedDate = string.Format( "{0}", date );
				}
				else
				{
					//not sure how to handle unexpected date except to ignore
					//might be better to send actual DateTime field
				}
			}

			return formatedDate;
		}
		#endregion

		#region Registry Gets


		/// <summary>
		/// Retrieve an envelop from the registry and do import
		/// </summary>
		/// <param name="envelopeId"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool ImportByEnvelopeId( string envelopeId, SaveStatus status )
        {
            //this is currently specific, assumes envelop contains a credential
            //can use the hack for GetResourceType to determine the type, and then call the appropriate import method

            if ( string.IsNullOrWhiteSpace( envelopeId ) )
            {
                status.AddError( "ImportByEnvelope - a valid envelope id must be provided" );
                return false;
            }

            string statusMessage = "";
            string ctdlType = "";
            try
            {
                ReadEnvelope envelope = RegistryServices.GetEnvelope( envelopeId, ref statusMessage, ref ctdlType );
				if ( envelope == null || string.IsNullOrWhiteSpace(envelope.EnvelopeType))
				{
					string defCommunity = UtilityManager.GetAppKeyValue( "defaultCommunity" );
					string community = UtilityManager.GetAppKeyValue( "additionalCommunity" );
					if (defCommunity != community)
						envelope = RegistryServices.GetEnvelope( envelopeId, ref statusMessage, ref ctdlType, community );
				}

				if ( envelope != null && !string.IsNullOrWhiteSpace( envelope.EnvelopeIdentifier ) )
                {
                    LoggingHelper.DoTrace( 4, string.Format( "RegistryServices.ImportByEnvelopeId ctdlType: {0}, EnvelopeId: {1} ", ctdlType, envelopeId ) );
                    ctdlType = ctdlType.Replace( "ceterms:", "" );

                    switch ( ctdlType.ToLower() )
                    {
                        case "credentialorganization":
                        case "qacredentialorganization": //what distinctions do we need for QA orgs?
                        case "organization":
                            return new ImportOrganization().CustomProcessEnvelope( envelope, status );
                        //break;CredentialOrganization
                        case "assessmentprofile":
                            return new ImportAssessment().CustomProcessEnvelope( envelope, status );
                        //break;
                        case "learningopportunityprofile":
                            return new ImportLearningOpportunties().CustomProcessEnvelope( envelope, status );
                        //break;
                        case "conditionmanifest":
                            return new ImportAssessment().CustomProcessEnvelope( envelope, status );
                        //break;
                        case "costmanifest":
                            return new ImportLearningOpportunties().CustomProcessEnvelope( envelope, status );
                        //break;
                        default:
                            //default to credential
                            return new ImportCredential().CustomProcessRequest( envelope, status );
                            //break;
                    }
                }
                else
                    return false;
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, string.Format( "RegistryServices`.ImportByEnvelopeId(). ctdlType: {0}", ctdlType ) );
                status.AddError( ex.Message );
                if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
                {
                    status.AddWarning( "The referenced registry document is using an old schema. Please republish it with the latest schema!" );
                }
                return false;
            }
        }

        public bool ImportByCtid( string ctid, SaveStatus status )
        {
            //this is currently specific, assumes envelop contains a credential
            //can use the hack fo GetResourceType to determine the type, and then call the appropriate import method

            if ( string.IsNullOrWhiteSpace( ctid ) )
            {
                status.AddError( "ImportByCtid - a valid ctid must be provided" );
                return false;
            }

            string statusMessage = "";
            string ctdlType = "";
            string payload = "";
            try
            {
                payload = GetResourceByCtid( ctid, ref ctdlType, ref statusMessage );
				if (string.IsNullOrWhiteSpace(payload))
				{
					string defCommunity = UtilityManager.GetAppKeyValue( "defaultCommunity" );
					string community = UtilityManager.GetAppKeyValue( "additionalCommunity" );
					if ( defCommunity != community )
						payload = GetResourceByCtid( ctid, ref ctdlType, ref statusMessage, community );
				}
				if ( !string.IsNullOrWhiteSpace( payload ) )
                {
                    LoggingHelper.WriteLogFile( 5, ctid + "_ImportByCtid.json", payload, "", false );
                    LoggingHelper.DoTrace( 4, string.Format( "RegistryServices.ImportByCtid ctdlType: {0}, ctid: {1} ", ctdlType, ctid ) );
                    ctdlType = ctdlType.Replace( "ceterms:", "" );
                    switch ( ctdlType.ToLower() )
                    {
                        case "credentialorganization":
                        case "qacredentialorganization":
                        case "organization":
                            return new ImportOrganization().ImportByPayload( payload, status );
                        //break;CredentialOrganization
                        case "assessmentprofile":
                            return new ImportAssessment().ImportByPayload( payload, status );
                        //break;
                        case "learningopportunityprofile":
                            return new ImportLearningOpportunties().ImportByPayload( payload, status );
                        //break;
                        case "conditionmanifest":
                            return new ImportConditionManifests().ImportByPayload( payload, status );
                        //break;
                        case "costmanifest":
                            return new ImportCostManifests().ImportByPayload( payload, status );
                        //break;
                        default:
                            //default to credential
                            return new ImportCredential().ImportByPayload( payload, status );
                            //break;
                    }
                }
                else
                    return false;
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, string.Format( "ImportCredential.ImportByEnvelopeId(). ctdlType: {0}", ctdlType ) );
                status.AddError( ex.Message );
                if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
                {
                    status.AddWarning( "The referenced registry document is using an old schema. Please republish it with the latest schema!" );
                }
                return false;
            }
        }


        /// <summary>
        /// Retrieve an envelope from the registry
        /// </summary>
        /// <param name="envelopeId"></param>
        /// <param name="statusMessage"></param>
        /// <param name="ctdlType"></param>
        /// <returns></returns>
        public static ReadEnvelope GetEnvelope( string envelopeId, ref string statusMessage, ref string ctdlType, string community = "" )
        {
            string document = "";
			//need to pass in an override community - eventually
			if (string.IsNullOrWhiteSpace( community ) )
				community = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			string serviceUri = GetEnvelopeUrl( envelopeId, community );

			serviceUri = string.Format( serviceUri, envelopeId );
            LoggingHelper.DoTrace( 5, string.Format( "RegistryServices.GetEnvelope envelopeId: {0}, serviceUri: {1} ", envelopeId, serviceUri ) );
            ReadEnvelope envelope = new ReadEnvelope();

            try
            {

                // Create a request for the URL.         
                WebRequest request = WebRequest.Create( serviceUri );
                // If required by the server, set the credentials.
                request.Credentials = CredentialCache.DefaultCredentials;
				var hdr = new WebHeaderCollection
				{
					{ "Authorization", "Token  " + credentialEngineAPIKey }
				};
				request.Headers.Add( hdr );

				//Get the response.
				HttpWebResponse response = ( HttpWebResponse )request.GetResponse();

                // Get the stream containing content returned by the server.
                Stream dataStream = response.GetResponseStream();

                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader( dataStream );
                // Read the content.
                document = reader.ReadToEnd();

                // Cleanup the streams and the response.

                reader.Close();
                dataStream.Close();
                response.Close();

                //map to the default envelope
                envelope = JsonConvert.DeserializeObject<ReadEnvelope>( document );

                if ( envelope != null && !string.IsNullOrWhiteSpace( envelope.EnvelopeIdentifier ) )
                {
                    string payload = envelope.DecodedResource.ToString();
                    ctdlType = RegistryServices.GetResourceType( payload );

                    //return ProcessProxy( mgr, item, status );
                }
            }
            catch ( Exception exc )
            {
                LoggingHelper.LogError( exc, "RegistryServices.GetEnvelope" );
                statusMessage = exc.Message;
            }
            return envelope;
        }

        /// <summary>
        /// Use search to get the envelope for a ctid
        /// </summary>
        /// <param name="ctid"></param>
        /// <param name="statusMessage"></param>
        /// <param name="ctdlType"></param>
        /// <returns></returns>
        public static ReadEnvelope GetEnvelopeByCtid( string ctid, ref string statusMessage, ref string ctdlType, string community = "")
        {
            string document = "";
			//perhaps this should be done in the caller. It could check the default or previous import source
			if ( string.IsNullOrWhiteSpace( community ) )
				community = UtilityManager.GetAppKeyValue( "defaultCommunity" );
			string additionalCommunity = UtilityManager.GetAppKeyValue( "additionalCommunity" );

			string credentialEngineAPIKey = UtilityManager.GetAppKeyValue( "CredentialEngineAPIKey" );
			//

			string searchUrl = GetRegistrySearchUrl( community );
			searchUrl = searchUrl + "ctid=" + ctid.ToLower();

            LoggingHelper.DoTrace( 5, string.Format( "RegistryServices.ImportByCtid ctid: {0}, searchUrl: {1} ", ctid, searchUrl ) );
            ReadEnvelope envelope = new ReadEnvelope();
            List<ReadEnvelope> list = new List<ReadEnvelope>();
            try
            {

                // Create a request for the URL.         
                WebRequest request = WebRequest.Create( searchUrl );
                request.Credentials = CredentialCache.DefaultCredentials;

				var hdr = new WebHeaderCollection
				{
					{ "Authorization", "Token  " + credentialEngineAPIKey }
				};
				request.Headers.Add( hdr );

				HttpWebResponse response = ( HttpWebResponse )request.GetResponse();
                Stream dataStream = response.GetResponseStream();

                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader( dataStream );
                // Read the content.
                document = reader.ReadToEnd();

                // Cleanup the streams and the response.

                reader.Close();
                dataStream.Close();
                response.Close();

                //map to list
                list = JsonConvert.DeserializeObject<List<ReadEnvelope>>( document );
                //only expecting one
                if ( list != null && list.Count > 0 )
                {
                    envelope = list[ 0 ];

                    if ( envelope != null && !string.IsNullOrWhiteSpace( envelope.EnvelopeIdentifier ) )
                    {
                        string payload = envelope.DecodedResource.ToString();
                        ctdlType = RegistryServices.GetResourceType( payload );
                    }
                }
            }
            catch ( Exception exc )
            {
                LoggingHelper.LogError( exc, "RegistryServices.GetEnvelopeByCtid" );
                statusMessage = exc.Message;
            }
            return envelope;
        }


        /// <summary>
        /// Retrieve a resource from the registry by ctid
        /// </summary>
        /// <param name="ctid"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public static string GetResourceByCtid( string ctid, ref string ctdlType, ref string statusMessage, string community = "")
        {
            string resourceIdUrl = GetResourceUrl( ctid, community );
            return GetResourceByUrl( resourceIdUrl, ref ctdlType, ref statusMessage );
        }

        /// <summary>
        /// Retrieve a resource from the registry by resourceId
        /// </summary>
        /// <param name="resourceId">Url to a resource in the registry</param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public static string GetResourceByUrl( string resourceUrl, ref string ctdlType, ref string statusMessage )
        {
            string payload = "";
            //NOTE - getting by ctid means no envelopeid
            try
            {
                // Create a request for the URL.         
                WebRequest request = WebRequest.Create( resourceUrl );

                // If required by the server, set the credentials.
                request.Credentials = CredentialCache.DefaultCredentials;
				var hdr = new WebHeaderCollection
				{
					{ "Authorization", "Token  " + credentialEngineAPIKey }
				};
				request.Headers.Add( hdr );

				//Get the response.
				HttpWebResponse response = ( HttpWebResponse )request.GetResponse();

                // Get the stream containing content returned by the server.
                Stream dataStream = response.GetResponseStream();

                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader( dataStream );
                // Read the content.
                payload = reader.ReadToEnd();

                // Cleanup the streams and the response.

                reader.Close();
                dataStream.Close();
                response.Close();

                ctdlType = RegistryServices.GetResourceType( payload );
            }
            catch ( Exception exc )
            {
                if ( exc.Message.IndexOf( "(404) Not Found" ) > 0 )
                {
                    //need to surface these better
                    statusMessage = "ERROR - resource was (still) not found in registry: " + resourceUrl;
                }
                else
                {
                    LoggingHelper.LogError( exc, "RegistryServices.GetResource" );
                    statusMessage = exc.Message;
                }
            }
            return payload;
        }

        public static string GetCtidFromUnknownEnvelope( ReadEnvelope item )
        {
            string ctid = "";
            //string envelopeId = "";
            try
            {
                RegistryObject ro = new RegistryObject( item.DecodedResource.ToString() );
                ctid = ro.Ctid;

                //TODO - this will have to change for type of @graph
                //envelopeId = item.EnvelopeIdentifier;
                //ctid = item.EnvelopeCetermsCtid ?? "";
                //if ( !string.IsNullOrWhiteSpace( ctid ) )
                //    return ctid;

                //string payload = item.DecodedResource.ToString();
                //if ( payload.IndexOf( "@graph" ) > -1 )
                //{
                //    UnknownPayload input = JsonConvert.DeserializeObject<UnknownPayload>( item.DecodedResource.ToString() );
                //    //extract from the @id
                //    if ( !string.IsNullOrWhiteSpace( input.Ctid ) )
                //    {
                //        ctid = input.Ctid;
                //    }
                //    else if ( !string.IsNullOrWhiteSpace( input.CtdlId ) )
                //    {
                //        int pos = input.CtdlId.LastIndexOf( "/" );
                //        ctid = input.CtdlId.Substring( pos );
                //    }
                //}
                //else
                //{
                //    UnknownPayload input = JsonConvert.DeserializeObject<UnknownPayload>( item.DecodedResource.ToString() );
                //    ctid = input.Ctid;
                //}
            }
            catch ( Exception ex )
            {
                LoggingHelper.DoTrace( 2, "GetCtidFromUnknownEnvelope - unable to extract ctid from envelope" );
            }

            return ctid;
        }

        public static string GetResourceType( string payload )
        {
            string ctdlType = "";
            RegistryObject ro = new RegistryObject( payload );
            ctdlType = ro.CtdlType;
            //ctdlType = ctdlType.Replace( "ceterms:", "" );
            return ctdlType;
        }

        public string ImportPending()
        {
            string status = "";
            new ImportCredential().ImportPendingRecords();

            new ImportOrganization().ImportPendingRecords();

            new ImportAssessment().ImportPendingRecords();

            new ImportLearningOpportunties().ImportPendingRecords();

            return status;
        }

        /// <summary>
        /// Generic handling of Json object - especially for unexpected types
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static Dictionary<string, object> JsonToDictionary( string json )
        {
            var result = new Dictionary<string, object>();
            var obj = JObject.Parse( json );
            foreach ( var property in obj )
            {
                result.Add( property.Key, JsonToObject( property.Value ) );
            }
            return result;
        }
        public static object JsonToObject( JToken token )
        {
            switch ( token.Type )
            {
                case JTokenType.Object:
                {
                    return token.Children<JProperty>().ToDictionary( property => property.Name, property => JsonToObject( property.Value ) );
                }
                case JTokenType.Array:
                {
                    var result = new List<object>();
                    foreach ( var obj in token )
                    {
                        result.Add( JsonToObject( obj ) );
                    }
                    return result;
                }
                default:
                {
                    return ( ( JValue )token ).Value;
                }
            }
        }
		#endregion
	}


	public class RegistryObject
    {
        public RegistryObject( string payload )
        {
            if ( !string.IsNullOrWhiteSpace( payload ) )
            {
                dictionary = RegistryServices.JsonToDictionary( payload );
                if ( payload.IndexOf( "@graph" ) > 0 && payload.IndexOf( "@graph\": null") == -1 )
                {
                    IsGraphObject = true;
                    //get the graph object
                    object graph = dictionary[ "@graph" ];
                    //serialize the graph object
                    var glist = JsonConvert.SerializeObject( graph );
                    //parse graph in to list of objects
                    JArray graphList = JArray.Parse( glist );

                    var main = graphList[ 0 ].ToString();
                    BaseObject = JsonConvert.DeserializeObject<RegistryBaseObject>( main );
                    CtdlType = BaseObject.CdtlType;
                    Ctid = BaseObject.Ctid;
					//not important to fully resolve yet
					if ( BaseObject.Name != null )
						Name = BaseObject.Name.ToString();
					else if ( CtdlType == "ceasn:CompetencyFramework" )
					{
						Name = ( BaseObject.CompetencyFrameworkName ?? "" ).ToString();
					}
					else
						Name = "?????";
				}
                else
                {
                    //check if old resource or standalone resource
                    BaseObject = JsonConvert.DeserializeObject<RegistryBaseObject>( payload );
                    CtdlType = BaseObject.CdtlType;
                    Ctid = BaseObject.Ctid;
                    Name = BaseObject.Name.ToString();
                }
                CtdlType = CtdlType.Replace( "ceterms:", "" );
				CtdlType = CtdlType.Replace( "ceasn:", "" );
			}
        }

        Dictionary<string, object> dictionary = new Dictionary<string, object>();

        public bool IsGraphObject { get; set; }
        public RegistryBaseObject BaseObject { get; set; } = new RegistryBaseObject();
        public string CtdlType { get; set; } = "";
        public string CtdlId { get; set; } = "";
        public string Ctid { get; set; } = "";
        public string Name { get; set;  }
    }

    public class RegistryBaseObject
    {
        [JsonProperty( "@id" )]
        public string CtdlId { get; set; }

        /// <summary>
        /// Type  of CTDL object
        /// </summary>
        [JsonProperty( "@type" )]
        public string CdtlType { get; set; }

        [JsonProperty( PropertyName = "ceterms:ctid" )]
        public string Ctid { get; set; }

        [JsonProperty( PropertyName = "ceterms:name" )]
        public object Name { get; set; }

		[JsonProperty( PropertyName = "ceterms:description" )]
		public object Description { get; set; }

		[JsonProperty( PropertyName = "ceasn:name" )]
		public object CompetencyFrameworkName { get; set; }

		[JsonProperty( PropertyName = "ceasn:description" )]
		public object FrameworkDescription { get; set; }


		[JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
        public string SubjectWebpage { get; set; }

    }
}
