using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;

using Newtonsoft.Json;

using workIT.Factories;
using workIT.Utilities;

namespace Import.Services
{
	public class ExternalServices
	{
		static string thisClassName = "ExternalServices"; 
		public bool PostResourceToExternalService( string myExternalAPIEndpoint, CredentialRegistryResource record,  ref List<string> messages )
		{ 
			//
			var apiKey = UtilityManager.GetAppKeyValue( "MyCredentialEngineAPIKey", "" );
			var response = new ExternalPostResponse();
			var responseContents = "";
			//ExternalPostResponse
			try
			{
				string postBody = JsonConvert.SerializeObject( record, JsonHelper.GetJsonSettings() );
				using ( var client = new HttpClient() )
				{
					client.DefaultRequestHeaders.
						Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );
					//could plug in api key just in case. Could be useful to reject requests sent to the publisher.
					client.DefaultRequestHeaders.Add( "Authorization", "ApiToken " + apiKey );

					//increase timeout - needed for larger documents
					client.Timeout = new TimeSpan( 0, 3, 0 );

					LoggingHelper.DoTrace( 6, "ExternalServices.PostResourceToExternalService: doing PostAsync to: " + myExternalAPIEndpoint );
					var task = client.PostAsync( myExternalAPIEndpoint,
						new StringContent( postBody, Encoding.UTF8, "application/json" ) );

					task.Wait();
					var result = task.Result;
					//LoggingHelper.DoTrace( 6, "Publisher.PostResourceToExternalService: reading task.Result.Content" );
					responseContents = task.Result.Content.ReadAsStringAsync().Result;

					if ( result.IsSuccessStatusCode == false )
					{
						LoggingHelper.DoTrace( 6, "Publisher.PostResourceToExternalService: result.IsSuccessStatusCode == false" );
						//response is TBD - probably can ditate what is to be returned.
						response = JsonConvert.DeserializeObject<ExternalPostResponse>( responseContents );
						//logging???
						string status = string.Join( ",", response.Messages.ToArray() );
						messages.AddRange( response.Messages );

						LoggingHelper.DoTrace( 4, thisClassName + string.Format( ".PostResourceToExternalService() {0}, CTID: '{1}', failed: {2}", record.EntityType, record.CTID, status ) );

						return false;
					}
					else
					{

						response = JsonConvert.DeserializeObject<ExternalPostResponse>( responseContents );
						//
						if ( response.Successful )
						{
							LoggingHelper.DoTrace( 5, thisClassName + " PostResourceToExternalService SUCCESSFUL" );
							//may have some warnings to display
							messages.AddRange( response.Messages );
						}
						else
						{
							string status = string.Join( ",", response.Messages.ToArray() );
							//this is display two other places, so skip here
							LoggingHelper.DoTrace( 7, thisClassName + " PostResourceToExternalService FAILED. result: " + status );
							messages.AddRange( response.Messages );
							return false;
						}
						

					}
					return result.IsSuccessStatusCode;
				}
			}
			catch ( Exception exc )
			{
				LoggingHelper.LogError( exc, string.Format( "PostResourceToExternalService. RequestType:{0}, Identifier: {1}. /n/r responseContents: {2}", record.EntityType, record.CTID, ( responseContents ?? "empty" ) ) );
				string message = LoggingHelper.FormatExceptions( exc );
				if ( message.IndexOf( "Time out" ) > -1 )
				{
					message = "The request took too long and has timed out waiting for a reply. Your request may still have been successful. Please contact System Administration. ";
				}
				messages.Add( message );
				return false;

			}
			finally
			{
				LoggingHelper.DoTrace( 6, string.Format( thisClassName + ".PostResourceToExternalService. Exiting." ) );
			}
		}  //

		/// <summary>
		/// Add record to CredentialRegistryDownload
		/// TODO - could make configurable to check if exists, and do update
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public int DownloadSave( CredentialRegistryResource entity, ref List<string> messages )
		{
			int newId = 0;
			try
			{
				string connectionString = DownloadConnection();
				using ( SqlConnection c = new SqlConnection( connectionString ) )
				{
					c.Open();

					using ( SqlCommand command = new SqlCommand( "[ResourceInsert]", c ) )
					{
						command.CommandType = CommandType.StoredProcedure;
						command.Parameters.AddWithValue( "@CTID", entity.CTID );
						command.Parameters.AddWithValue( "@CTDLType", entity.EntityType );
						command.Parameters.AddWithValue( "@Name", entity.Name );
						command.Parameters.AddWithValue( "@Description", entity.Description );
						command.Parameters.AddWithValue( "@SubjectWebpage", entity.SubjectWebpage );
						command.Parameters.AddWithValue( "@PrimaryOrganizationCTID", entity.OwningOrganizationCTID );
						command.Parameters.AddWithValue( "@DownloadDate", entity.DownloadDate );
						command.Parameters.AddWithValue( "@Created", entity.Created );
						command.Parameters.AddWithValue( "@LastUpdated", entity.LastUpdated );
						command.Parameters.AddWithValue( "@CredentialRegistryGraph", entity.CredentialRegistryGraph );
						command.Parameters.AddWithValue( "@CredentialFinderObject", entity.CredentialFinderObject );

						//SqlParameter outputId = new SqlParameter( "@NewId", newId );
						//outputId.Direction = ParameterDirection.Output;
						//command.Parameters.Add( outputId );
						try
						{
							command.ExecuteNonQuery();
							//newId = ( int )command.Parameters[ "@NewId" ].Value;
							//OR
							//string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
							//newId = Int32.Parse( rows );
						}
						catch ( Exception ex )
						{
							newId = 0;
							LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add() for EntityType: {0} and CTID: {1}", entity.EntityType, entity.CTID ) );
							return 0;
						}
					}
				}

				messages.Add( string.Format("Saved record: {0} to CredentialRegistryDownload", entity.CTID) );
				//for now just set to 1 to show successful
				newId = 1;

			}
			catch ( Exception ex )
			{
				//provide helpful info about failing entity
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add() for EntityType: {0} and CTID: {1}", entity.EntityType, entity.CTID ) );
				messages.Add( thisClassName + string.Format(".DownloadSave(). Unsuccessful: Save for {0}. : ", entity.CTID) + ex.Message.ToString());
			}
			return newId;
		}
		public static bool VerifyCEExternalExists( ref string status )
		{
			string queryString = "select TOP 2 * from [Codes.Countries] ;";
			string conn = "";

			try
            {
				//ensure a connection string exists
				conn = WebConfigurationManager.ConnectionStrings["ceExternalData"].ConnectionString;
			}

			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, "Error attempting to get the connection string for ceExternalData. Verify there is a correct entry in the config file." );
				status = "Error attempting to get the connection string for ceExternalData. Verify there is a correct entry in the config file.";
				return false;
			}

			try
				{
					using ( SqlConnection connection = new SqlConnection( conn ) )
					{
						SqlCommand command = new SqlCommand( queryString, connection );
						connection.Open();
						using ( SqlDataReader reader = command.ExecuteReader() )
						{
							while ( reader.Read() )
							{
								//Console.WriteLine( String.Format( "{0}, {1}", reader[ 0 ], reader[ 1 ] ) );
							}
						}
					}
					return true;
				}
				catch ( Exception ex )
				{
					status = ex.Message;
					return false;
				}
			
			
		}

		/// <summary>
		/// Verify can access the download database
		/// </summary>
		public static bool VerifyDownloadDatabaseExists( ref string status )
		{
			string queryString = "select TOP 2 * from Resource ;";
			try
			{
				using ( SqlConnection connection = new SqlConnection( DownloadConnection() ) )
				{
					SqlCommand command = new SqlCommand( queryString, connection );
					connection.Open();
					using ( SqlDataReader reader = command.ExecuteReader() )
					{
						while ( reader.Read() )
						{
							//Console.WriteLine( String.Format( "{0}, {1}", reader[ 0 ], reader[ 1 ] ) );
						}
					}
				}
				return true;
			} catch (Exception ex)
			{
				status = ex.Message;
				return false;
			}
		}

		public static string DownloadConnection()
		{
			string conn = WebConfigurationManager.ConnectionStrings[ "CredentialRegistryDownload" ].ConnectionString;
			return conn;
		}
	}
	public class CredentialRegistryResource
	{
		//public int Id { get; set; }
		public string EntityType { get; set; }
		public string CTID { get; set; }
		public DateTime DownloadDate { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string SubjectWebpage { get; set; }
		public string OwningOrganizationCTID { get; set; }
		public DateTime Created { get; set; }
		public DateTime LastUpdated { get; set; }

		public object CredentialFinderObject { get; set; }
		public string CredentialRegistryGraph { get; set; }
	}

	/// <summary>
	/// Registry Assistant Response
	/// </summary>
	public class ExternalPostResponse
	{
		public ExternalPostResponse()
		{
			Messages = new List<string>();
		}
		/// <summary>
		/// True if action was successfull, otherwise false
		/// </summary>
		public bool Successful { get; set; }
		/// <summary>
		/// List of error or warning messages
		/// </summary>
		public List<string> Messages { get; set; }


	}

}
