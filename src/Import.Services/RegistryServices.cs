﻿using System;
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
        /// <summary>
        /// Retrieve an envelop from the registry and do import
        /// </summary>
        /// <param name="envelopeId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool ImportByEnvelopeId( string envelopeId, SaveStatus status )
        {
            //this is currently specific, assumes envelop contains a credential
            //can use the hack fo GetResourceType to determine the type, and then call the appropriate import method

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
                if ( envelope != null && !string.IsNullOrWhiteSpace( envelope.EnvelopeIdentifier ) )
                {
                    LoggingHelper.DoTrace( 4, string.Format( "RegistryServices.ImportByEnvelopeId ctdlType: {0}, EnvelopeId: {1} ", ctdlType, envelopeId ) );

                    switch ( ctdlType.ToLower() )
                    {
                        case "credentialorganization":
                            return new ImportOrganization().ProcessEnvelope( envelope, status );
                        //break;
                        case "qacredentialorganization":
                            //what distinctions do we need for QA orgs?
                            return new ImportOrganization().ProcessEnvelope( envelope, status );
                        //break;
                        case "organization":
                            return new ImportOrganization().ProcessEnvelope( envelope, status );
                        //break;CredentialOrganization
                        case "assessmentprofile":
                            return new ImportAssessment().ProcessEnvelope( envelope, status );
                        //break;
                        case "learningopportunityprofile":
                            return new ImportLearningOpportunties().ProcessEnvelope( envelope, status );
                        //break;
                        case "conditionmanifest":
                            return new ImportAssessment().ProcessEnvelope( envelope, status );
                        //break;
                        case "costmanifest":
                            return new ImportLearningOpportunties().ProcessEnvelope( envelope, status );
                        //break;
                        default:
                            //default to credential
                            return new ImportCredential().ProcessEnvelope( envelope, status );
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
                if ( !string.IsNullOrWhiteSpace( payload ) )
                {
                    LoggingHelper.WriteLogFile( 5, ctid + "_ImportByCtid.json", payload, "", false );
                    LoggingHelper.DoTrace( 4, string.Format( "RegistryServices.ImportByCtid ctdlType: {0}, ctid: {1} ", ctdlType, ctid ) );
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
        public static ReadEnvelope GetEnvelope( string envelopeId, ref string statusMessage, ref string ctdlType )
        {
            string document = "";

            string serviceUri = UtilityManager.GetAppKeyValue( "credentialRegistryGet" );

            serviceUri = string.Format( serviceUri, envelopeId );
            LoggingHelper.DoTrace( 5, string.Format( "RegistryServices.GetEnvelope envelopeId: {0}, serviceUri: {1} ", envelopeId, serviceUri ) );
            ReadEnvelope envelope = new ReadEnvelope();

            try
            {

                // Create a request for the URL.         
                WebRequest request = WebRequest.Create( serviceUri );

                // If required by the server, set the credentials.
                request.Credentials = CredentialCache.DefaultCredentials;

                //Get the response.
                HttpWebResponse response = ( HttpWebResponse ) request.GetResponse();

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
        public static ReadEnvelope GetEnvelopeByCtid( string ctid, ref string statusMessage, ref string ctdlType )
        {
            string document = "";

            string searchUrl = UtilityManager.GetAppKeyValue( "credentialRegistrySearch" );
            searchUrl = searchUrl + "ctid=" + ctid;
            LoggingHelper.DoTrace( 5, string.Format( "RegistryServices.ImportByCtid ctid: {0}, searchUrl: {1} ", ctid, searchUrl ) );
            ReadEnvelope envelope = new ReadEnvelope();
            List<ReadEnvelope> list = new List<ReadEnvelope>();
            try
            {

                // Create a request for the URL.         
                WebRequest request = WebRequest.Create( searchUrl );
                request.Credentials = CredentialCache.DefaultCredentials;
                HttpWebResponse response = ( HttpWebResponse ) request.GetResponse();
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
        public static string GetEnvelopeUrl( string envelopeId )
        {
            string registryEnvelopeUrl = string.Format( UtilityManager.GetAppKeyValue( "credentialRegistryGet", "http://lr-staging.learningtapestry.com/envelopes/{0}" ), envelopeId );
            return registryEnvelopeUrl;
        }

        /// <summary>
        /// Retrieve a resource from the registry by ctid
        /// </summary>
        /// <param name="ctid"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public static string GetResourceByCtid( string ctid, ref string ctdlType, ref string statusMessage )
        {
            string resourceId = UtilityManager.GetAppKeyValue( "credentialRegistryResource" );

            resourceId = string.Format( resourceId, ctid );
            return GetResourceByUrl( resourceId, ref ctdlType, ref statusMessage );
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

                //Get the response.
                HttpWebResponse response = ( HttpWebResponse ) request.GetResponse();

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
            string envelopeId = "";
            try
            {
                envelopeId = item.EnvelopeIdentifier;
                UnknownPayload input = JsonConvert.DeserializeObject<UnknownPayload>( item.DecodedResource.ToString() );
                ctid = input.Ctid;
            }
            catch ( Exception ex )
            {
                LoggingHelper.DoTrace( 2, "GetCtidFromUnknownEnvelope - unable to extract ctid from envelope" );
            }

            return ctid;
        }

		public static string GetResourceType( string payload )
		{
			string type = "";

			string template = "@type";
			string template2 = "ceterms:";
            string template3 = "ceasn:";

            //get first @type, then ceterms
            int startPos = payload.IndexOf( template );
			if ( startPos > 0 )
			{
				int begin = startPos + template.Length;
                if ( payload.IndexOf( template3, begin ) > -1 )
                {
                    int template2Position = payload.IndexOf( template3, begin );
                    if ( template2Position > begin )
                    {
                        //now get type
                        int endPos = payload.IndexOf( "\"", template2Position );
                        if ( endPos > template2Position )
                        {
                            type = payload.Substring( template2Position + template3.Length, endPos - ( template2Position + template3.Length ) );
                        }
                    }
                }
                else
                {
                    int template2Position = payload.IndexOf( template2, begin );
                    if ( template2Position > begin )
                    {
                        //now get type
                        int endPos = payload.IndexOf( "\"", template2Position );
                        if ( endPos > template2Position )
                        {
                            type = payload.Substring( template2Position + template2.Length, endPos - ( template2Position + template2.Length ) );
                        }
                    }
                }
			}

			return type;

		}

		public string ImportPending()
		{
            string status = "";
			new ImportCredential().ImportPendingRecords();

			new ImportOrganization().ImportPendingRecords();

			new ImportAssessment().ImportPendingRecords();

			new  ImportLearningOpportunties().ImportPendingRecords();

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
						return ( ( JValue ) token ).Value;
					}
			}
		}
		
	}
}
