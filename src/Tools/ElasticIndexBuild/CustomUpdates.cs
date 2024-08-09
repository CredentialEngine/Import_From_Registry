using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Factories;
using workIT.Models;
using workIT.Services;
using workIT.Utilities;

namespace ElasticIndexBuild
{
	public class CustomUpdates
	{
		public void CredentialAddJsonProperties()
		{

		}

		public void CredentialCheckAllRecords()
		{
			string statusMessage = "";
			var entityType = "credential";
			int entityTypeId = 1;
			RequestParms parms = new RequestParms( entityTypeId );

			//TODO
			var eManager = new EntityManager();
			var resourceServices = new CredentialServices();
			SaveStatus status = new SaveStatus();
			DisplayMessages( "CredentialCheckAllRecords Starting..." );
			int pTotalRows = 0;
			int actualTotalRows = 0;
			int maxRecords = UtilityManager.GetAppKeyValue( "maxRecords", 0 );
			bool isComplete = false;
			int pageNbr = 1;
			int cntr = 0;
			parms.OrderBy = "oldest";
			//eventually will want skip options, or finer filtering
			parms.Filter = " base.EntityStateId > 1 ";
			try
			{
				while ( pageNbr > 0 && !isComplete )
				{
					//do search with minimum results (autocomplete = true)
					var list = CredentialManager.Search( parms.Filter, parms.OrderBy, pageNbr, parms.PageSize, ref pTotalRows, true );
					if ( list == null || list.Count == 0 )
					{
						isComplete = true;
						if ( pageNbr == 1 )
						{
							LoggingHelper.DoTrace( 4, " --- No records where found for filter --- " );
						}
						else if ( cntr < actualTotalRows )
						{
							
					
								//if no data found and have not processed actual rows, could have been an issue with the search.
								//perhaps should be an error to ensure followup
								LoggingHelper.DoTrace( 2, string.Format( "**************** WARNING -CredentialCheckAllRecords for '{0}' didn't find data on this pass, but has only processed {1} of an expected {2} records.", entityType, cntr, actualTotalRows ) );
						
						}
						break;
					}
					//
					if ( pageNbr == 1 )
					{
						actualTotalRows = pTotalRows;
						LoggingHelper.DoTrace( 2, string.Format( "{0}.CredentialCheckAllRecords Found {1} records to process.", entityType, pTotalRows ) );
					}
					//
					foreach ( var item in list )
					{
						cntr++;
						//DisplayMessages( string.Format( " {0}.	===== {1}: {2} ({3}) ==========", cntr, entityType, item.Name, item.Id ) );

						statusMessage = "";
						try
						{
							//look up record in elastic
							//or maybe do groups
						}
						catch ( Exception ex )
						{
							string msg = BaseFactory.FormatExceptions( ex );
							LoggingHelper.DoTrace( 1, string.Format( "CredentialCheckAllRecords.{0}: ", entityType ) + msg );
							// LoggingHelper.LogError( ex, string.Format( "CredentialCheckAllRecords.{0}: ", entityType ) );
						}
					}//

					pageNbr++;
					if ( ( maxRecords > 0 && cntr >= maxRecords ) )
					{
						isComplete = true;
						LoggingHelper.DoTrace( 2, string.Format( entityType + ".CredentialCheckAllRecords EARLY EXIT. Completed {0} records out of a total of {1} for {2} ", cntr, pTotalRows, entityType ) );

					}
					else if ( cntr >= actualTotalRows )
					{
						isComplete = true;
						LoggingHelper.DoTrace( 2, string.Format( "Completed {0} records out of a total of {1} for {2}", cntr, actualTotalRows, entityType ) );
					}
				}
			}
			catch ( Exception ex )
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.DoTrace( 1, $"CredentialPopulateResourceDetail Exception: " + msg );
			}

		}



		public static string DisplayMessages( string message, bool loggingError = false )
		{
			LoggingHelper.DoTrace( 1, message );
			//Console.WriteLine( message );

			return message;
		}
	}
	public class RequestParms
	{
		public RequestParms( int entityTypeId )
		{
			EntityTypeId = entityTypeId;
			SetType();
			PageNumber = 1;
			PageSize = 100;
			Filter = "";
			OrderBy = "newest";
			PayloadPrefix = "";
			Community = "";
			//??we are already setting Type, what is the difference?
			switch ( entityTypeId )
			{
				case 1:
					EntityType = "Credential"; break;
				case 2:
					EntityType = "Organization"; break;
				case 3:
					EntityType = "Assessment"; break;
				case 7:
					EntityType = "LearningOpp"; break;
				case 8:
					EntityType = "Pathway"; break;
				case 9:
					EntityType = "Collection"; break;
				case 10:
					EntityType = "CompetencyFramework";
					break;
				case 11:
					EntityType = "ConceptScheme";
					break;
				case 19:
					EntityType = "ConditionManifest"; break;
				case 20:
					EntityType = "CostManifest"; break;
				case 23:
					EntityType = "PathwaySet"; break;
				case 26:
					EntityType = "TransferValue"; break;
				case 31:
					EntityType = "DatasetProfile"; break;
				default:
					EntityType = string.Format( "Unknown EntityTypeId: {0}", entityTypeId );
					break;
			}
		}
		public string EntityType { get; set; }
		public int EntityTypeId { get; set; }
		public string Type { get; set; }
		public string Filter { get; set; }
		public string OrderBy { get; set; }
		public int PageNumber { get; set; }
		public int PageSize { get; set; }

		public string PayloadPrefix { get; set; }
		public string Community { get; set; }
		public bool DoingGenerate { get; set; }
		public bool DoingPublish { get; set; }
		public bool DoingDelete { get; set; }
		public string DeleteType { get; set; }
		public bool DoingPopulateDetail { get; set; }
		public bool DoingPopulateAgentRelationships { get; set; }
		//
		public bool DoingDeleteBeforePublish { get; set; }
		public string SetAppKey( string keyType )
		{
			return Type + keyType;
		}
		public void SetType()
		{
			if ( EntityTypeId == 1 )
				Type = "cred";
			else if ( EntityTypeId == 2 )
				Type = "org";
			else if ( EntityTypeId == 3 )
				Type = "asmt";
			else if ( EntityTypeId == 7 )
				Type = "lopp";

			else if ( EntityTypeId == 8 )
				Type = "pathway";
			else if ( EntityTypeId == 9 )
				Type = "collection";

			else if ( EntityTypeId == 10 || EntityTypeId == 17 )
				Type = "competencyFramework";
			else if ( EntityTypeId == 11 )
				Type = "conceptScheme";

			else if ( EntityTypeId == 19 )
				Type = "condManifest";
			else if ( EntityTypeId == 20 )
				Type = "costManifest";

			else if ( EntityTypeId == 23 )
				Type = "pathwaySet";
			else if ( EntityTypeId == 26 )
				Type = "tvp";

			else if ( EntityTypeId == 31 )
				Type = "dsp";
			else
				Type = "unknown";
		}
	}

}
