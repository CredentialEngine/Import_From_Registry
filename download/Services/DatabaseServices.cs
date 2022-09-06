using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Configuration;
using System.Text;
using System.Threading.Tasks;


namespace Download.Services
{
	/// <summary>
	/// Coming SOON
	/// ******************* IN PROGRESS *****************************
	/// Initial implementation will be to a single table, using stored procedures for least impact for setup.
	/// This can be extended by adding separate tables for each entity type and then adding a stored procedure for each type.
	/// </summary>
	public class DatabaseServices
	{
		string thisClassName = "DatabaseServices";
		public int Add( CredentialRegistryResource entity, ref string statusMessage )
		{
			int newId = 0;
			try
			{
				string connectionString = MainConnection();
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
						command.Parameters.AddWithValue( "@PrimaryOrganizationCTID", entity.OwningOrganizationCTID );
						command.Parameters.AddWithValue( "@SubjectWebpage", entity.SubjectWebpage );
						command.Parameters.AddWithValue( "@Created", entity.Created );
						command.Parameters.AddWithValue( "@LastUpdated", entity.LastUpdated );
						command.Parameters.AddWithValue( "@DownloadDate", entity.DownloadDate );
						command.Parameters.AddWithValue( "@CredentialRegistryGraph", entity.CredentialRegistryGraph );
						//TBD - what was expected here?
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

				statusMessage = "successful";
				//for now just set to 1 to show successful
				newId = 1;

			}
			catch ( Exception ex )
			{
				//provide helpful info about failing entity
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add() for EntityType: {0} and CTID: {1}", entity.EntityType, entity.CTID ) );
				statusMessage = thisClassName + "- Unsuccessful: Create(): " + ex.Message.ToString();

				entity.IsValid = false;
			}

			return newId;
		}

		public static string MainConnection()
		{
			string conn = WebConfigurationManager.ConnectionStrings[ "MainConnection" ].ConnectionString;
			return conn;
		}
	}
	public class CredentialRegistryResource
	{
		public int Id { get; set; }
		public string EntityType { get; set; }
		public string CTID { get; set; }
		public DateTime DownloadDate { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string SubjectWebpage { get; set; }
		public string OwningOrganizationCTID { get; set; }
		public DateTime Created { get; set; }
		public DateTime LastUpdated { get; set; }
		public string CredentialRegistryGraph { get; set; }
		public string CredentialFinderObject { get; set; }
		public bool IsValid { get; set; }
	}
}
