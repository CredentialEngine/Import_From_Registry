using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Newtonsoft.Json;

using Import.Services;
//using CTI.Import.classes;
using workIT.Utilities;
using EntityServices = workIT.Services.OrganizationServices;
//using JsonEntity = RA.Models.Json.Agent;
//using ThisEntity = workIT.Models.Common.Organization;
using workIT.Models;
using workIT.Factories;

namespace CTI.Import
{
	public class OrganizationsImport : RegistryImport
	{
        int entityTypeId = CodesManager.ENTITY_TYPE_ORGANIZATION;
        ImportOrganization entityImportMgr = new ImportOrganization();
        ImportServiceHelpers importMgr = new ImportServiceHelpers();
		static string thisClassName = "OrganizationsImport";
		/// <summary>
		/// Import organizations
		/// </summary>
		/// <param name="startingDate"></param>
		/// <param name="endingDate"></param>
		public string Import( string startingDate, string endingDate, int maxRecords, bool downloadOnly = false )
		{
			LoggingHelper.DoTrace( 1, string.Format( "===  *****************  {0}  ***************** ", thisClassName ) );
			ReadEnvelope envelope = new ReadEnvelope();
			List<ReadEnvelope> list = new List<ReadEnvelope>();
			EntityServices mgr = new EntityServices();
			string type = "organization";
			int entityTypeId = CodesManager.ENTITY_TYPE_ORGANIZATION;
			int pageNbr = 1;
			int pageSize = 25;
			string importError = "";
			string importResults  = "";
			string importNote = "";
			//ThisEntity output = new ThisEntity();
			List<string> messages = new List<string>();

			//the nbr of records needs to be monitored, to determine the optimum
			//NOTE: consider the IOER approach that all candidate records are first downloaded, and then a separate process does the import
			
			int cntr = 0;
			int pTotalRows = 0;

			int exceptionCtr = 0;
			string statusMessage = "";
		
			bool isComplete = false;
			bool importSuccessfull = true;
			int newImportId = 0;
			SaveStatus status = new SaveStatus();

			//do credential org first, and then qacredentialOrg
			while ( pageNbr > 0 && !isComplete )
			{
				list = GetLatest( type, startingDate, endingDate, pageNbr, pageSize, ref pTotalRows, ref statusMessage );

				if ( list == null || list.Count == 0 )
				{
					isComplete = true;
					if ( pageNbr == 1 )
					{
						importNote = " Organizations: No records where found for date range ";

					   Console.WriteLine( thisClassName + importNote );
						LoggingHelper.DoTrace( 4, thisClassName + importNote );
					}
					break;
				}

				foreach ( ReadEnvelope item in list )
				{
					cntr++;
					string payload = item.DecodedResource.ToString();
					newImportId = 0;
					LoggingHelper.DoTrace( 2, string.Format( "{0}. EnvelopeIdentifier: {1} ", cntr, item.EnvelopeIdentifier ) );
			
					messages = new List<string>();
					status = new SaveStatus();
                    status.DoingDownloadOnly = downloadOnly;
					status.ValidationGroup = "Organization Import";
					importError = "";
					
					importSuccessfull = false;

					try
					{
						Console.WriteLine( string.Format( "{0}. Organization EnvelopeIdentifier {1} ", cntr, item.EnvelopeIdentifier ) );

						importSuccessfull = entityImportMgr.ProcessEnvelope( item, status );

                    }
                    catch ( Exception ex )
                    {
						if ( ex.Message.IndexOf( "Path '@context', line 1" ) > 0 )
						{
							importError = "The referenced registry document is using an old schema. Please republish it with the latest schema!";
							status.AddError( importError );
						}
						else
						{
							LoggingHelper.LogError( ex, string.Format( "Exception encountered in envelopeId: {0}", item.EnvelopeIdentifier ), false, "CredentialFinder Import exception" );
							status.AddError( ex.Message );
							importError = ex.Message;
						}

						//make continue on exceptions an option
						exceptionCtr++;
						if ( maxExceptions > 0 && exceptionCtr > maxExceptions )
						{
							//arbitrarily stop if large number of exceptions
							importNote = string.Format( thisClassName +  " - Many exceptions ({0}) were encountered during import - abandoning.", exceptionCtr );
							Console.WriteLine( importNote );
							LoggingHelper.DoTrace( 1, importNote );
							LoggingHelper.LogError( importNote, true, thisClassName + "- many exceptions" );
                            isComplete = true;
							break;
                        }
                    }
                    finally
                    {
                        if ( !importSuccessfull )
                        {
                            if ( string.IsNullOrWhiteSpace( importError ) )
                            {
                                importError = string.Join( "\r\n", status.GetAllMessages().ToArray() );
                            }
                        }
                        //store document
                        //add indicator of success
                        newImportId = importMgr.Add( item, entityTypeId, status.Ctid, importSuccessfull, importError, ref messages );
                        if ( newImportId > 0 && status.Messages != null && status.Messages.Count > 0 )
                            importMgr.AddMessages( newImportId, status, ref messages );

                    } //finally

                    if ( maxRecords > 0 && cntr > maxRecords )
                    {
                        break;
                    }
                } //end foreach 

                pageNbr++;
                if ( maxRecords > 0 && cntr > maxRecords )
                {
                    isComplete = true;
                    LoggingHelper.DoTrace( 2, string.Format( "{0} EARLY EXIT. Completed {1} records out of a total of {2} ", thisClassName, cntr, pTotalRows ) );

                }
			} //while

			importResults = string.Format( "{0} - Processed {1} records, with {2} exceptions. \r\n", thisClassName, cntr, exceptionCtr );
			if ( !string.IsNullOrWhiteSpace( importNote ) )
				importResults += importNote;

			return importResults;
		}

	}
}
