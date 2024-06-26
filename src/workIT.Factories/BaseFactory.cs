﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Elastic;
using workIT.Models.ProfileModels;
using workIT.Utilities;
using EntityContext = workIT.Data.Tables.workITEntities;

namespace workIT.Factories
{
    public class BaseFactory
	{
		public static string REGISTRY_ACTION_DELETE = "Registry Delete";
		public static string REGISTRY_ACTION_PURGE = "Registry Purge";
		public static string REGISTRY_ACTION_PURGE_ALL = "Registry Purge ALL";
		public static string REGISTRY_ACTION_TRANSFER = "Transfer of Owner";
		public static string REGISTRY_ACTION_REMOVE_ORG = "RemoveOrganization";
		protected static string DEFAULT_GUID = "00000000-0000-0000-0000-000000000000";
		public string commonStatusMessage = string.Empty;
		public static int MaxResourceNameLength = UtilityManager.GetAppKeyValue( "maxResourceNameLength", 800 );
		public static int PortionOfDescriptionToUseForName = UtilityManager.GetAppKeyValue( "portionOfDescriptionToUseForName", 150 );
		//Has Part is the default, and should be used when parent can only have one relationship
		//relationships used for TargetCredential: Entity.Credential, TargetLearningOpportunity: Entity.LearningOpp, TargetAssessment: Entity.Assessment, and TargetCompetency: Entity.Competency (actually not used for this)
		//23-02-01 mp - HasPart is not the same as Target...
		public static int RELATIONSHIP_TYPE_HAS_PART = 1;
		public static int RELATIONSHIP_TYPE_IS_TARGET = 1;
		public static int RELATIONSHIP_TYPE_IS_PART_OF = 2;
		public static int RELATIONSHIP_TYPE_IsETPLResource = 3;
		//ex. resource for TargetPathway - or may need an additional type here. 
		public static int RELATIONSHIP_TYPE_HAS_TARGET_RESOURCE = 4;
		public static int RELATIONSHIP_TYPE_HAS_PREREQUISITE = 5;
		public static int RELATIONSHIP_TYPE_TARGET_LOPP = 6;		//TargetLearningResource

		public static string reactFinderSiteURL = UtilityManager.GetAppKeyValue( "credentialFinderMainSite" );
		public static string oldCredentialFinderSite = UtilityManager.GetAppKeyValue( "oldCredentialFinderSite" );
		public static string finderApiSiteURL = UtilityManager.GetAppKeyValue( "finderApiSiteURL" );
		//tracing
		//use of appDefaultTraceLevel would mean to 'always' include as equals the current setting
		public static int appDefaultTraceLevel = UtilityManager.GetAppKeyValue( "appTraceLevel", 5 );
		public static int appDebuggingTraceLevel = UtilityManager.GetAppKeyValue( "appDebuggingTraceLevel", 6 );
		public static int appMethodEntryTraceLevel = UtilityManager.GetAppKeyValue( "appMethodEntryTraceLevel", 7 );
        public static int appMethodExitTraceLevel = UtilityManager.GetAppKeyValue( "appMethodExitTraceLevel", 8 );
        public static int appSectionDurationTraceLevel = UtilityManager.GetAppKeyValue( "appSectionDurationTraceLevel", 8 );
		//default specialTrace to high. Code can be called to lower it
		public static int specialTrace = UtilityManager.GetAppKeyValue( "appSpecialTraceLevel", 9 );
		//
		public static bool IsDevEnv()
		{

			if ( UtilityManager.GetAppKeyValue( "environment", "no" ) == "development" )
				return true;
			else
				return false;
		}
		public static bool IsProduction()
		{

			if ( UtilityManager.GetAppKeyValue( "environment", "development" ) == "production" )
				return true;
			else
				return false;
		}
		#region Entity frameworks helpers
		public static bool HasStateChanged( EntityContext context )
		{
			if ( context.ChangeTracker.Entries().Any( e =>
					e.State == EntityState.Added ||
					e.State == EntityState.Modified ||
					e.State == EntityState.Deleted ) == true )
				return true;
			else
				return false;
		}

		//public static string SetLastUpdatedBy( int lastUpdatedById, Data.Account accountModifier )
		//{
		//	string lastUpdatedBy = string.Empty;
		//	if ( accountModifier != null )
		//	{
		//		lastUpdatedBy = accountModifier.FirstName + " " + accountModifier.LastName;
		//	}
		//	else
		//	{
		//		if ( lastUpdatedById > 0 )
		//		{
		//			AppUser user = AccountManager.AppUser_Get( lastUpdatedById );
		//			lastUpdatedBy = user.FullName();
		//		}
		//	}
		//	return lastUpdatedBy;
		//}
		#endregion
		#region Database connections
		/// <summary>
		/// Get the read only connection string for the main database
		/// </summary>
		/// <returns></returns>
		public static string DBConnectionRO()
		{
			string conn = WebConfigurationManager.ConnectionStrings[ "workIT_RO" ].ConnectionString;
			return conn;
		}

		public static string MainConnection()
		{
			string conn = WebConfigurationManager.ConnectionStrings[ "MainConnection" ].ConnectionString;
			return conn;
		}
		public static string ConnectionworkITEntities()
		{
			string conn = WebConfigurationManager.ConnectionStrings[ "workITEntities" ].ConnectionString;
			return conn;
		}
		#endregion

		#region Assignments

		public static decimal Assign( decimal input, decimal currentValue, bool doesEntityExist )
		{
			decimal value = 0;
			if ( doesEntityExist )
			{
				value = input == -99 ? 0 : input == -100 ? currentValue : input;
			}
			else if ( input > 0 )
			{
				//don't allow delete for initial
				value = input;
			}
			return value;
		}

        /// <summary>
        /// Assign an integer
        /// NOTE: currently doesn't allow negatives
        /// ALSO: assumes a previous check for required status
        /// </summary>
        /// <param name="input"></param>
        /// <param name="currentValue"></param>
        /// <param name="doesEntityExist"></param>
        /// <returns></returns>
        public static int Assign( int input, int currentValue, bool doesEntityExist )
		{
			int value = 0;
			if ( doesEntityExist )
			{
				//use -99 to force to zero
				//use -100 to force to current - why??
				value = input == -99 ? 0 : input == -100 ? currentValue : input;
			}
			else if ( input > 0 )
			{
				//don't allow delete for initial
				value = input;
			}
			return value;
		}

		public static List<string> SplitDelimitedStringToList(string input, char delimiter)
		{
			if ( string.IsNullOrWhiteSpace(input ))
				return null;
			var output = new List<string>();
            var list = input.Split( delimiter );
            foreach ( string item in list )
            {
                if ( !string.IsNullOrWhiteSpace( item ))
					output.Add( item.TrimEnd() );
            }

            return output;
        }

		public static List<CredentialAlignmentObjectProfile> GetTargetCompetency( List<string> input )
		{
			var output = new List<CredentialAlignmentObjectProfile>();
			foreach ( var item in input )
			{
				var competency = CompetencyFrameworkCompetencyManager.GetByCtid( item );
				var cao = new CredentialAlignmentObjectProfile()
				{
					TargetNodeName = competency.CompetencyText,
					TargetNodeDescription = competency.CompetencyCategory,
					TargetNodeCTID = competency.CTID,
					TargetNode = competency.CtdlId,
					Id = competency.Id
				};
				output.Add( cao );
			}

			return output;
		}
		public static List<CredentialAlignmentObjectFrameworkProfile> MapResourceSummaryToCAOFramework( List<ResourceSummary> input )
		{
			var output = new List<CredentialAlignmentObjectFrameworkProfile>();
			foreach ( var item in input )
			{
				var competency = new Models.ProfileModels.Competency();
				var comp = new CredentialAlignmentObjectFrameworkProfile();
				competency = CompetencyFrameworkCompetencyManager.GetByCtid( item.CTID );
				if ( competency == null || competency.Id == 0 )
				{
					competency = CollectionCompetencyManager.GetByCtid( item.CTID );
					var collection = CollectionManager.Get( competency.FrameworkId, false );
					comp.Id = collection.Id;
					comp.FrameworkName = collection.Name;
					comp.FrameworkCtid = collection.CTID;
					comp.Description = collection.Description;

                }
                else
                {
					var framework = CompetencyFrameworkManager.Get( competency.FrameworkId, false );
					comp.Id = framework.Id;
					comp.FrameworkName = framework.Name;
					comp.FrameworkCtid = framework.CTID;
					comp.Description = framework.Description;
				}
				var cao = new CredentialAlignmentObjectItem()
				{
					TargetNodeName = competency.CompetencyText,
					TargetNodeDescription = competency.CompetencyCategory,
					TargetNodeCTID = competency.CTID,
					TargetNode = competency.CtdlId,
					Id = competency.Id
				};
				comp.Items.Add( cao );
				output.Add( comp );
			}
			output = output
		   .GroupBy( c => c.FrameworkCtid )
		   .Select( group => new CredentialAlignmentObjectFrameworkProfile
		   {
			   FrameworkCtid = group.Key,
			   Id = group.First().Id,
			   FrameworkName = group.First().FrameworkName,
			   Description = group.First().Description,
			   Items = group.SelectMany( c => c.Items ).ToList()
		   } )
		   .ToList();
			return output;
		}
		public static ResourceSummary MapPLToResourceSummary( string input )
		{
			ResourceSummary output = new ResourceSummary();

			if ( input != null )
			{
				var pl = ProgressionModelManager.GetByConceptCtid( input );
				if ( pl != null && pl.Id > 0 )
				{
					output.CTID = pl.CTID;
					output.Name = pl.PrefLabel;
					output.Description = pl.Definition;
					output.Id = pl.Id;
					output.EntityTypeId = CodesManager.ENTITY_TYPE_PROGRESSION_LEVEL;
				}
			}
			return output;
		}

		public static List<ResourceSummary> UpdateConceptResourceSummary( List<ResourceSummary> input )
		{
			//This method adds the concept scheme CTID and concept description needed for the UI
			List<ResourceSummary> output = new List<ResourceSummary>();

			if ( input != null )
			{
				foreach(var resources in input )
				{
					var pl = ConceptSchemeManager.GetByConceptCtid( resources.CTID );
					if ( pl != null && pl.Id > 0 )
					{
						var resource = new ResourceSummary();
						resource.CTID = pl.topConceptOf;
						resource.Name = pl.PrefLabel;
						resource.Description = pl.Definition;
						//resource.Id = pl.Id;
						resource.EntityTypeId = CodesManager.ENTITY_TYPE_CONCEPT;
						output.Add( resource );
					}

				}
			}
			return output;
		}

		public static List<ResourceSummary> MapCLToResourceSummary( List<CriterionLevel> criterionLevels )
		{
			List<ResourceSummary> output = new List<ResourceSummary>();
			foreach ( var cl in criterionLevels )
			{
				var input = new ResourceSummary()
				{
					Name = cl.BenchmarkLabel,
					Description = cl.BenchmarkText,
					RowId = cl.RowId,
					EntityTypeId = CodesManager.ENTITY_TYPE_RUBRIC_CRITERION_LEVEL
				};
				output.Add( input );
			}
			return output;
		}

		public static string FormatListAsDelimitedString( List<string> input, string delimiter )
        {
			if ( input == null || !input.Any())
				return null;
			var output = string.Join( delimiter, input.ToArray()) ;

            return output;
        }
		public static string FormatCAOListAsDelimitedString( List<CredentialAlignmentObjectProfile> input, string delimiter )
		{
			if ( input == null || !input.Any() )
				return null;
			List<string> ctids= new List<string>();
			foreach (var cao in input )
            {
				ctids.Add( cao.TargetNodeCTID );
            }
			return string.Join( delimiter, ctids.ToArray() );

		}
		protected static CodeItemResult Fill_CodeItemResults( DataRow dr, string fieldName, int categoryId, bool hasSchemaName, bool hasTotals, bool hasAnIdentifer = true )
		{
			string list = GetRowPossibleColumn( dr, fieldName, string.Empty );
			//string list = dr[ fieldName ].ToString();
			return Fill_CodeItemResults( list, categoryId, hasSchemaName, hasTotals, hasAnIdentifer );

		}

		protected static CodeItemResult Fill_CodeItemResults( string list, int categoryId, bool hasSchemaName, bool hasTotals, bool hasAnIdentifer = true )
		{
			
			//string list = dr[ fieldName ].ToString();
			CodeItemResult item = new CodeItemResult() { CategoryId = categoryId };

			if ( string.IsNullOrWhiteSpace( list ) )
				return item;

			
			if ( categoryId == 10 || categoryId == 11 || categoryId == 23 )
				item.UsingCodedNotation = true;

			int totals = 0;
			int id = 0;
			string title = string.Empty;
			string schema = string.Empty;
			string code = string.Empty;

			var codeGroup = list.Split( '|' );
			foreach ( string codeSet in codeGroup )
			{
				var codes = codeSet.Split( '~' );
				schema = string.Empty;
				totals = 0;
				id = 0;
				if ( hasAnIdentifer )
				{
					if ( item.UsingCodedNotation )
					{
						//check for coded notation
						//note can be empty if 'other' type
						//so can just ignore
						code = codes[ 0 ];
						if ( code.IndexOf( "-" ) > -1 || code.IndexOf( ".00" ) > -1 )
						{ }
						//note: now passing actual soc code so will not be an integer
						//code = code.Replace( "-", string.Empty );
						if ( codes.Length > 1 )
							title = codes[ 1 ].Trim();
						if ( codes.Length > 2 && hasSchemaName )
							schema = codes[ 2 ];
						if ( codes.Length > 3 && hasTotals )
							totals = Int32.Parse( codes[ 3 ] );
					}
					else
					{
						//check for an integer (for codeId)
						//note check category, as codes like Naics can be integers
						Int32.TryParse( codes[ 0 ].Trim(), out id );
						if ( id > 0 )
						{
							title = codes[ 1 ].Trim();
							if ( hasSchemaName )
							{
								schema = codes[ 2 ];

								if ( hasTotals )
									totals = Int32.Parse( codes[ 3 ] );
							}
							else
							{
								if ( hasTotals )
									totals = Int32.Parse( codes[ 2 ] );
							}
						}
						else
						{
							//check for coded notation anyway
							code = codes[ 0 ];
							if ( codes.Length > 1 )
								title = codes[ 1 ].Trim();
							if ( codes.Length > 2 && hasSchemaName )
								schema = codes[ 2 ];
							if ( codes.Length > 3 && hasTotals )
								totals = Int32.Parse( codes[ 3 ] );
						}
					}
				}
				else
				{
					//currently if no Id, assume only text value
					title = codes[ 0 ].Trim();
				}


				item.Results.Add( new Models.CodeItem() { Id = id, Code = code, Title = title, SchemaName = schema, Totals = totals } );
			}
			if (item.Results.Any())
				item.HasAnIdentifer = hasAnIdentifer;

			return item;
		}

		protected static CodeItemResult Fill_CodeItemResults( List<IndexReferenceFramework> list, int categoryId, bool hasSchemaName, bool hasTotals, bool hasAnIdentifer = true )
		{

			//string list = dr[ fieldName ].ToString();
			CodeItemResult item = new CodeItemResult() { CategoryId = categoryId };
			if ( list == null || list.Count == 0 )
				return item;

			//item.HasAnIdentifer = hasAnIdentifer;
			if ( categoryId == 10 || categoryId == 11 || categoryId == 23 )
				item.UsingCodedNotation = true; //????

			foreach ( var codeItem in list )
			{
				//check for duplicates
				var exists = item.Results.Where( s => s.Code == codeItem.CodedNotation ).ToList();
				if ( !item.UsingCodedNotation || ( exists == null || exists.Count == 0 ))
				{
					item.Results.Add( new Models.CodeItem()
					{
						CategoryId = categoryId,
						Id = codeItem.ReferenceFrameworkItemId,
						Code = codeItem.CodedNotation,
						Title = codeItem.Name,
						SchemaName = codeItem.SchemaName,
						CodeTitle = codeItem.CodeTitle
					} );
				}
			}
			if ( item.Results.Any() )
				item.HasAnIdentifer = hasAnIdentifer;
			return item;
		}

		protected static CodeItemResult Fill_CodeItemResults( List<IndexProperty> list, int categoryId, bool hasSchemaName, bool hasTotals, bool hasAnIdentifer = true )
		{

			//string list = dr[ fieldName ].ToString();
			CodeItemResult item = new CodeItemResult() { CategoryId = categoryId };
			if ( list == null || list.Count == 0 )
				return item;

			//item.HasAnIdentifer = hasAnIdentifer;

			foreach ( var codeItem in list )
			{
				item.Results.Add( new Models.CodeItem()
				{
					CategoryId = categoryId,
					Id = codeItem.Id,
					Title = codeItem.Name != "Blended Learning" ? codeItem.Name : "Blended Delivery",
					SchemaName = codeItem.SchemaName
				} );
			}
			if ( item.Results.Any() )
				item.HasAnIdentifer = hasAnIdentifer;
			return item;
		}

		/// <summary>
		/// Format a QuantitativeValue. 
		/// There is no validation in this method, so expecting accurate data, or caller does validation on return.
		/// </summary>
		/// <param name="creditUnitTypeId"></param>
		/// <param name="creditUnitValue"></param>
		/// <param name="creditUnitMaxValue"></param>
		/// <param name="description"></param>
		/// <returns></returns>
		public static QuantitativeValue FormatQuantitativeValue( int creditUnitTypeId, decimal creditUnitValue, decimal creditUnitMaxValue, string description )
		{
			QuantitativeValue qv = new QuantitativeValue()
			{
				Description = description ?? string.Empty
			};
			if ( creditUnitMaxValue > 0 )
			{
				qv.MinValue = creditUnitValue;
				qv.MaxValue = creditUnitMaxValue;
			}
			else
			{
				qv.Value = creditUnitValue;
			}
			if ( creditUnitTypeId > 0 )
			{
				qv.CreditUnitType = new Enumeration();
				var match = CodesManager.GetEnumeration( "creditUnit" ).Items.FirstOrDefault( m => m.CodeId == creditUnitTypeId );
				if ( match != null )
				{
					qv.CreditUnitType.Items.Add( match );
					//??store schema name or label, or both?
					qv.UnitText = match.SchemaName;
					qv.Label = match.Name;
				}
			}
			return qv;
		}
		public static QuantitativeValue FormatQuantitativeValue( int? creditUnitTypeId, decimal? creditUnitValue, decimal? creditUnitMaxValue, string description, string creditUnitType = "" )
		{
			QuantitativeValue qv = new QuantitativeValue()
			{
				Description = description ?? string.Empty
			};
			if ( (creditUnitMaxValue ?? 0M) > 0 )
			{
				qv.MinValue = creditUnitValue ?? 0M;
				qv.MaxValue = creditUnitMaxValue ?? 0M;
			}
			else
			{
				qv.Value = creditUnitValue ?? 0M;
			}
			if ( (creditUnitTypeId ?? 0) > 0 )
			{
				qv.CreditUnitType = new Enumeration();
				var match = CodesManager.GetEnumeration( "creditUnit" ).Items.FirstOrDefault( m => m.CodeId == creditUnitTypeId );
				if ( match != null )
				{
					qv.CreditUnitType.Items.Add( match );
					//??store schema name or label, or both?
					qv.UnitText = match.SchemaName;
					qv.Label = match.Name;
				}
			} else if ( !string.IsNullOrEmpty( creditUnitType ))
			{
				creditUnitType = creditUnitType.Replace( "ceterms:", string.Empty );
				qv.CreditUnitType = new Enumeration();
				qv.CreditUnitType.Items.Add( new EnumeratedItem() { SchemaName = creditUnitType });
				qv.UnitText = creditUnitType;
			}
			return qv;
		}

		/// <summary>
		/// TODO - need to handle percentage - will do once using JSON
		/// DUMP THIS ASAP - A HACK
		/// </summary>
		/// <param name="creditUnitTypeId"></param>
		/// <param name="creditUnitValue"></param>
		/// <param name="creditUnitMaxValue"></param>
		/// <param name="description"></param>
		/// <param name="creditUnitType"></param>
		/// <returns></returns>
		public static ValueProfile FormatValueProfile( int? creditUnitTypeId, decimal? creditUnitValue, decimal? creditUnitMaxValue, string description, string creditUnitType = "" )
		{
			ValueProfile qv = new ValueProfile()
			{
				Description = description ?? string.Empty
			};
			if ( ( creditUnitMaxValue ?? 0M ) > 0 )
			{
				qv.MinValue = creditUnitValue ?? 0M;
				qv.MaxValue = creditUnitMaxValue ?? 0M;
			}
			else
			{
				//need to distinguish if a percentage
				qv.Value = creditUnitValue ?? 0M;
			}
			if ( ( creditUnitTypeId ?? 0 ) > 0 )
			{
				qv.CreditUnitType = new Enumeration();
				var match = CodesManager.GetEnumeration( "creditUnit" ).Items.FirstOrDefault( m => m.CodeId == creditUnitTypeId );
				if ( match != null )
				{
					qv.CreditUnitType.Items.Add( match );
					//??store schema name or label, or both?
					//qv.UnitText = match.SchemaName;
					//qv.Label = match.Name;
					qv.CreditUnitType = new Enumeration();
					qv.CreditUnitType.Items.Add( new EnumeratedItem() { SchemaName = creditUnitType, Name = match.Name } );
				}
			}
			else if ( !string.IsNullOrEmpty( creditUnitType ) )
			{
				creditUnitType = creditUnitType.Replace( "ceterms:", string.Empty );
				qv.CreditUnitType = new Enumeration();
				qv.CreditUnitType.Items.Add( new EnumeratedItem() { SchemaName = creditUnitType } );
				///qv.UnitText = creditUnitType;
			}
			return qv;
		}

		public static List<ResourceSummary> ConvertToResourceSummary( List<TopLevelObject> input, string property )
		{
			if ( input == null || !input.Any() )
				return null;
			var output = new List<ResourceSummary>();
			var resource = new ResourceSummary();
			foreach ( var item in input )
			{
				if ( item == null || string.IsNullOrWhiteSpace( item.Name ) )
					continue;
				resource = ConvertToResourceSummary( item, property );
				if ( resource != null || !string.IsNullOrWhiteSpace( resource.Name ) )
					output.Add( resource );
			}
			return output;
		}

		public static List<ResourceSummary> ConvertToResourceSummaryList( TopLevelObject input, string property )
		{
			if ( input == null || string.IsNullOrWhiteSpace( input.Name ) )
				return null;
			var output = new List<ResourceSummary>();
			var resource = new ResourceSummary
			{
				CTID = input.CTID,
				EntityTypeId = input.EntityTypeId,
				Type = input.EntityType,
				Id = input.Id,
				RowId = input.RowId,
				Name = input.Name,
				Description = input.Description,
				URI = input.SubjectWebpage
			};
			if ( resource != null || !string.IsNullOrWhiteSpace( resource.Name ) )
				output.Add( resource );

			return output;
		}

		public static ResourceSummary ConvertToResourceSummary( TopLevelObject input, string property )
		{
			if (input == null || string.IsNullOrWhiteSpace(input.Name))
				return null;

			var resource = new ResourceSummary
			{
				CTID = input.CTID,
				EntityTypeId = input.EntityTypeId,
				Type = input.EntityType,
				Id = input.Id,
				RowId = input.RowId,
				Name = input.Name,
				Description = input.Description,
				URI = input.SubjectWebpage
			};
			
		
			return resource;
		}
		#endregion
		#region data retrieval

		/// <summary>
		/// Helper method to retrieve a string column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRow</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>

		protected static CredentialConnectionsResult Fill_CredentialConnectionsResult( DataRow dr, string fieldName, int categoryId )
		{
			string list = dr[ fieldName ].ToString();
			return Fill_CredentialConnectionsResult( list, categoryId );
		}

		protected static CredentialConnectionsResult Fill_CredentialConnectionsResult( string list, int categoryId )
		{
			CredentialConnectionsResult result = new CredentialConnectionsResult() { CategoryId = categoryId };
			CredentialConnectionItem item = new CredentialConnectionItem();
			int id = 0;

			if ( !string.IsNullOrWhiteSpace( list ) )
			{
				var codeGroup = list.Split( '|' );
				foreach ( string codeSet in codeGroup )
				{
					var codes = codeSet.Split( '~' );
					item = new CredentialConnectionItem();

					id = 0;
					Int32.TryParse( codes[ 0 ].Trim(), out id );
					item.ConnectionId = id;
					if ( codes.Length > 1 )
						item.Connection = codes[ 1 ].Trim();
					if ( codes.Length > 2 )
						Int32.TryParse( codes[ 2 ].Trim(), out id );
					item.CredentialId = id;
					if ( codes.Length > 3 )
						item.Credential = codes[ 3 ].Trim();
					//if ( codes.Length > 4 )
					//    if ( !string.IsNullOrEmpty( codes[4] ) )
					//        item.Credential = string.Format( "{0} [{1}]", item.Credential, codes[4] );
					if ( codes.Length > 4 )
						Int32.TryParse( codes[ 4 ].Trim(), out id );
					item.CredentialOwningOrgId = id;
					if ( codes.Length > 5 )
						item.CredentialOwningOrg = codes[ 5 ].Trim();
					result.Results.Add( item );
				}
			}

			return result;
		}
		/// <summary>
		/// Expect
		/// - relationshipId (RoleId)
		/// - Relationship
		/// - AgentId
		/// - Agent Name
		/// </summary>
		/// <param name="dr"></param>
		/// <param name="fieldName"></param>
		/// <param name="categoryId"></param>
		/// <param name="hasSchemaName"></param>
		/// <param name="hasTotals"></param>
		/// <param name="hasAnIdentifer"></param>
		/// <returns></returns>
		public static AgentRelationshipResult Fill_AgentRelationship( DataRow dr, string fieldName, int categoryId, bool hasSchemaName, bool hasTotals, bool hasAnIdentifer = true, string entityType = "" )
		{
			//string list = dr[fieldName].ToString();
			string list = GetRowPossibleColumn( dr, fieldName );
			return Fill_AgentRelationship( list, categoryId, hasSchemaName, hasTotals, hasAnIdentifer, entityType );
		}

		//primarily for roles on owning org
		//credential still uses old properties like: AgentAndRoles
		public static AgentRelationshipResult Fill_AgentRelationship( string list, int categoryId, bool hasSchemaName, bool hasTotals, bool hasAnIdentifer = true, string entityType = "" )
		{
			AgentRelationshipResult item = new AgentRelationshipResult() { CategoryId = categoryId };
			
			AgentRelationship code = new AgentRelationship();

			int id = 0;
			try
			{
				if ( !string.IsNullOrWhiteSpace( list ) )
				{
					item.HasAnIdentifer = hasAnIdentifer;
					var codeGroup = list.Split( '|' );
					foreach ( string codeSet in codeGroup )
					{
						code = new AgentRelationship();

						var codes = codeSet.Split( '~' );
						//schema = string.Empty;

						id = 0;
						if ( hasAnIdentifer )
						{
							Int32.TryParse( codes[ 0 ].Trim(), out id );
							code.RelationshipId = id;
							code.Relationship = codes[ 1 ].Trim();

							Int32.TryParse( codes[ 2 ].Trim(), out id );
							code.AgentId = id;
							code.Agent = codes[ 3 ].Trim();
							// code.AgentUrl = codes[4].Trim();

							if ( codes.Length > 4 )
								code.AgentUrl = codes[ 4 ].Trim();

							if ( codes.Length > 5 )
								if ( Int32.TryParse( codes[ 5 ].Trim(), out id ) )
								{
									code.EntityStateId = id;
								}

							//code.IsThirdPartyOrganization = false;
							//if ( codes.Length > 6 )
							//code.IsThirdPartyOrganization = codes[6].Trim() == "1";
							if ( code.EntityStateId == 2 && !IsProduction() && code.Agent.IndexOf( "[reference]" ) == -1 )
							{
								code.Agent += " [reference] ";
								code.IsThirdPartyOrganization = true;
							}

							if ( !string.IsNullOrEmpty( entityType ) )
							{
								code.EntityType = entityType.ToLower();
								code.Relationship = entityType + " " + codes[ 1 ].Trim();
							}
						}
						else
						{
							//currently if no Id, assume only text value
							//title = codes[ 0 ].Trim();
						}
						item.Results.Add( code );
					}
				}
				//else
				//	return null;
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( " Fill_AgentRelationship( string list, int categoryId: {0}, bool hasSchemaName{1}", categoryId, hasSchemaName ) );
			}
			return item;
		}
		public static AgentRelationshipResult Fill_AgentRelationship( List<AgentRelationshipForEntity> list, int categoryId, string entityType, bool selectingQAOnly = true )
		{
			AgentRelationshipResult item = new AgentRelationshipResult() { CategoryId = categoryId };

			AgentRelationship code = new AgentRelationship();
			List<int> qaRoles = new List<int>() { 1, 2, 10, 12 };
			List<int> nonQAaRoles = new List<int>() { 6,7,11,13 };
			if ( list == null )
				return item;
			/*
			 * AgentRelationshipForEntity will have multiple relationships for an org in each record.
			 * 
			 */
			foreach ( var i in list )
			{
				item.HasAnIdentifer = true; //????

				//code.RelationshipId = i.RelationshipTypeIds.FirstOrDefault();
				int cntr = -1;
				string relationship = string.Empty;
				foreach (var r in i.RelationshipTypeIds )
				{
					cntr++;
					code = new AgentRelationship();
					code.EntityType = entityType.ToLower();
					code.AgentId = i.OrgId;
					code.Agent = i.AgentName;
					code.EntityStateId = i.EntityStateId;
					if ( code.EntityStateId == 2 && !IsProduction() && code.Agent.IndexOf( "[reference]" ) == -1 )
					{
						code.Agent += " [reference] ";
						code.IsThirdPartyOrganization = true;
					}
					code.AgentUrl = i.AgentUrl;
					code.RelationshipId = r;
					if ( i.RelationshipTypeIds.Count() == i.Relationships.Count() )
						relationship = i.Relationships[ cntr ];
					else
						relationship = string.Empty;
					//code.Relationship = i.Relationships[ cntr ];
					code.Relationship = entityType + " " + relationship;
					if ( (!selectingQAOnly && nonQAaRoles.IndexOf( r ) > -1 ) 
					|| ( selectingQAOnly  && qaRoles.IndexOf( r ) > -1 ))
						item.Results.Add( code );
				}
			}
			return item;
		}

		#region fill id~resourceName list 
		protected static CredentialConnectionsResult Fill_ResourceKeyValueResult( DataRow dr, string fieldName, int categoryId )
		{
			string list = dr[ fieldName ].ToString();
			return Fill_ResourceKeyValueResult( list, categoryId );
		}

		protected static CredentialConnectionsResult Fill_ResourceKeyValueResult( string list, int categoryId )
		{
			CredentialConnectionsResult result = new CredentialConnectionsResult() { CategoryId = categoryId };
			CredentialConnectionItem item = new CredentialConnectionItem();
			int id = 0;

			if ( !string.IsNullOrWhiteSpace( list ) )
			{
				var codeGroup = list.Split( '|' );
				foreach ( string codeSet in codeGroup )
				{
					var codes = codeSet.Split( '~' );
					item = new CredentialConnectionItem();

					id = 0;
					Int32.TryParse( codes[ 0 ].Trim(), out id );
					item.ConnectionId = id;
					if ( codes.Length > 1 )
						item.Connection = codes[ 1 ].Trim();
					if ( codes.Length > 2 )
						Int32.TryParse( codes[ 2 ].Trim(), out id );
					item.CredentialId = id;
					if ( codes.Length > 3 )
						item.Credential = codes[ 3 ].Trim();
					//if ( codes.Length > 4 )
					//    if ( !string.IsNullOrEmpty( codes[4] ) )
					//        item.Credential = string.Format( "{0} [{1}]", item.Credential, codes[4] );
					if ( codes.Length > 4 )
						Int32.TryParse( codes[ 4 ].Trim(), out id );
					item.CredentialOwningOrgId = id;
					if ( codes.Length > 5 )
						item.CredentialOwningOrg = codes[ 5 ].Trim();
					result.Results.Add( item );
				}
			}

			return result;
		}
		#endregion
		public static TargetAssertionResult Fill_TargetQaAssertion( List<QualityAssurancePerformed> list, int categoryId, string entityType = "" )
		{
			TargetAssertionResult item = new TargetAssertionResult() { CategoryId = categoryId };
			if ( list == null )
				return item;
			TargetAssertion code = new TargetAssertion();

			foreach ( var i in list )
			{
				code = new TargetAssertion();

				code.AssertionId = i.AssertionTypeIds.FirstOrDefault();
				code.TargetId = i.TargetEntityBaseId;
				code.Target = i.TargetEntityName;
				if ( code.EntityStateId == 2 && !IsProduction() && code.Target.IndexOf( "[reference]" ) == -1 )
				{
					code.Target += " [reference] ";
					code.IsThirdPartyOrganization = true;
				}

				if ( !string.IsNullOrEmpty( entityType ) )
				{
					code.EntityType = entityType.ToLower();
				}

				item.Results.Add( code );
			}
			if( item.Results.Any())
				item.HasAnIdentifer = true; //????

			return item;
		}
        #endregion

        //
        public static List<string> MapTextValueProfileToString( List<TextValueProfile> input )
        {
            var output = new List<string>();
            if ( input == null || input.Count == 0 )
                return output; //or null
            foreach ( var item in input )
            {
                //confirm that subject and keyword use TextValue
                output.Add( item.TextValue );
            }

            return output;
        }

        #region column retrieval handling not found names
        public static string GetRowColumn( DataRow row, string column, string defaultValue = "" )
		{
			string colValue = string.Empty;

			try
			{
				colValue = row[ column ].ToString();

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{
				if ( HasMessageBeenPreviouslySent( column ) == false )
				{
					string queryString = GetWebUrl();
					LoggingHelper.LogError( ex, " Exception in GetRowColumn( DataRow row, string column, string defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString() + "\r\nLocation: " + queryString );
				}

				colValue = defaultValue;
			}
			return colValue;

		}

		public static string GetRowPossibleColumn( DataRow row, string column, string defaultValue = "" )
		{
			string colValue = string.Empty;
			//SKIPPING try /catcvh now, for performance
			//colValue = row[ column ].ToString();
			try
			{
				colValue = row[ column ].ToString();

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{

				colValue = defaultValue;
			}
			return colValue;

		}


		/// <summary>
		/// Helper method to retrieve an int column from a row while handling invalid values
		/// </summary>
		/// <param name="row">DataRow</param>
		/// <param name="column">Column Name</param>
		/// <param name="defaultValue">Default value to return if column data is invalid</param>
		/// <returns></returns>
		public static int GetRowColumn( DataRow row, string column, int defaultValue )
		{
			int colValue = 0;
			//var value = row[ column ]?.ToString();
			//if ( !string.IsNullOrEmpty( value ) )
			//	colValue = Int32.Parse( value );
            //SKIPPING try /catcvh now, for performance
            try
            {
                colValue = Int32.Parse( row[column].ToString() );

            }
            catch ( System.FormatException fex )
            {
                //Assuming FormatException means null or invalid value, so can ignore
                colValue = defaultValue;

            }
            catch ( Exception ex )
            {
                if ( HasMessageBeenPreviouslySent( column ) == false )
                {
                    string queryString = GetWebUrl();
                    LoggingHelper.LogError( ex, "Exception in GetRowColumn( DataRow row, string column, int defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString() + "\r\nLocation: " + queryString );
                }
                colValue = defaultValue;
                //throw ex;
            }
            return colValue;

		}
		public static bool GetRowColumn( DataRow row, string column, bool defaultValue )
		{
			bool colValue = false;

			try
			{
				//need to properly handle int values of 0,1, as bool
				string strValue = row[column].ToString();
				if ( !string.IsNullOrWhiteSpace( strValue ) && strValue.Trim().Length == 1 )
				{
					strValue = strValue.Trim();
					if ( strValue == "0" )
						return false;
					else if ( strValue == "1" )
						return true;
				}
				colValue = bool.Parse( row[column].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{
				if ( HasMessageBeenPreviouslySent( column ) == false )
				{
					string queryString = GetWebUrl();
					LoggingHelper.LogError( ex, " Exception in GetRowColumn( DataRow row, string column, bool defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString() + "\r\nLocation: " + queryString );
				}


				colValue = defaultValue;
				//throw ex;
			}
			return colValue;

		}
		public static DateTime GetRowColumn( DataRow row, string column, DateTime defaultValue )
		{
			DateTime colValue;

			try
			{
				string strValue = row[column].ToString();
				if ( DateTime.TryParse( strValue, out colValue ) == false )
					colValue = defaultValue;
			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{
				if ( HasMessageBeenPreviouslySent( column ) == false )
				{
					string queryString = GetWebUrl();
					LoggingHelper.LogError( ex, "Exception in GetRowColumn( DataRow row, string column, DateTime defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString() + "\r\nLocation: " + queryString );
				}


				colValue = defaultValue;
				//throw ex;
			}
			return colValue;

		}
		public static int GetRowPossibleColumn( DataRow row, string column, int defaultValue )
		{
			int colValue = 0;
			//colValue = Int32.Parse( row[ column ].ToString() );
			//SKIPPING try /catch now, for performance
			//added back as encountering unhandled issues

			try
			{
				colValue = Int32.Parse( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{
				colValue = defaultValue;
				//throw ex;
			}
			return colValue;

		}
		public static bool GetRowPossibleColumn( DataRow row, string column, bool defaultValue )
		{
			bool colValue = false;

			try
			{
				//need to properly handle int values of 0,1, as bool
				string strValue = row[ column ].ToString();
				if ( !string.IsNullOrWhiteSpace( strValue ) && strValue.Trim().Length == 1 )
				{
					strValue = strValue.Trim();
					if ( strValue == "0" )
						return false;
					else if ( strValue == "1" )
						return true;
				}
				colValue = bool.Parse( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{
				colValue = defaultValue;
				//throw ex;
			}
			return colValue;

		}
		public static decimal GetRowPossibleColumn( DataRow row, string column, decimal defaultValue )
		{
			decimal colValue = 0;

			try
			{
				colValue = decimal.Parse( row[ column ].ToString() );

			}
			catch ( System.FormatException fex )
			{
				//Assuming FormatException means null or invalid value, so can ignore
				colValue = defaultValue;

			}
			catch ( Exception ex )
			{
				//string queryString = GetWebUrl();

				//LoggingHelper.LogError( "Exception in GetRowColumn( DataRow row, string column, int defaultValue ) for column: " + column + ". \r\n" + ex.Message.ToString() + "\r\nLocation: " + queryString, true );
				colValue = defaultValue;
				//throw ex;
			}
			return colValue;

		}
		

		public static bool HasMessageBeenPreviouslySent( string keyName )
		{

			string key = "missingColumn_" + keyName;
			//check cache for keyName
			if ( HttpRuntime.Cache[ key ] != null )
			{
				return true;
			}
			else
			{
				//not really much to store
				HttpRuntime.Cache.Insert( key, keyName );
			}

			return false;
		}
		protected static int GetField( int? field, int defaultValue = 0 )
		{
			int value = field != null ? ( int ) field : defaultValue;

			return value;
		} // end method
		protected static decimal GetField( decimal? field, decimal defaultValue = 0 )
		{
			decimal value = field != null ? ( decimal ) field : defaultValue;

			return value;
		} // end method
        protected static bool IsValidDecimal( decimal? field, ref decimal output )
        {
			if ( field != null ) 
			{
				output = ( decimal ) field;
				return true;
			}
			else 
				return false;

           
        } // end method
        protected static decimal GetDecimalField( string field, decimal defaultValue = 0 )
		{
			if ( string.IsNullOrWhiteSpace( field ) )
				return defaultValue;

			decimal value = 0;
			decimal.TryParse( field, out value );

			return value;
		} // end method
		protected static Guid GetField( Guid? field, Guid defaultValue )
		{
			Guid value = field != null ? ( Guid ) field : defaultValue;

			return value;
		} // end method
		protected static DateTime? GetDate( string field )
		{
			if ( IsValidDate( field ) )
				return DateTime.Parse( field );

			return null;
		} //
		protected static string GetDate( DateTime? field )
		{
			if ( IsValidDate( field ) )
				return ( ( DateTime ) field ).ToString( "yyyy-MM-dd" ); ;

			return "";
		} //
		protected static string GetMessages( List<string> messages )
		{
			if ( messages == null || messages.Count == 0 )
				return "";

			return string.Join( "<br/>", messages.ToArray() );

		}

		/// <summary>
		/// Split a comma separated list into a list of strings
		/// </summary>
		/// <param name="csl"></param>
		/// <returns></returns>
		public static List<string> CommaSeparatedListToStringList( string csl )
		{
			if ( string.IsNullOrWhiteSpace( csl ) )
				return new List<string>();

			try
			{
				return csl.Trim().Split( new string[] { "," }, StringSplitOptions.RemoveEmptyEntries ).ToList();
			}
			catch
			{
				return new List<string>();
			}
		}

		/// <summary>
		/// Get the current url for reporting purposes
		/// </summary>
		/// <returns></returns>
		public static string GetWebUrl()
		{
			string queryString = "n/a";

			if ( HttpContext.Current != null && HttpContext.Current.Request != null )
				queryString = HttpContext.Current.Request.RawUrl.ToString();

			return queryString;
		}
		public static string FormatExternalFinderUrl( string entityType, string entityCTID, string entitySubjectWebpage, int recordId )
		{
			string url = string.Empty;
			if ( string.IsNullOrEmpty( entityCTID ) )
				url = entitySubjectWebpage;
			else
				url = reactFinderSiteURL + string.Format( "{0}/{1}", entityType, recordId );

			return url;
		}
		public static string FormatExternalFinderUrl( string entityType, string entityCTID, string entitySubjectWebpage, int recordId, string friendlyName )
		{
			string url = string.Empty;
			if ( string.IsNullOrEmpty( entityCTID ) )
				url = entitySubjectWebpage;
			else
				url = reactFinderSiteURL + string.Format( "{0}/{1}/{2}", entityType, recordId, string.IsNullOrWhiteSpace( friendlyName ) ? string.Empty : friendlyName );

			return url;
		}
		public static string FormatDetailUrl( string entityType, string entityCTID, string entitySubjectWebpage, int recordId )
		{
			string url = string.Empty;
			if ( string.IsNullOrEmpty( entityCTID ) )
				url = entitySubjectWebpage;
			else
				url = oldCredentialFinderSite + string.Format( "{0}/{1}", entityType, recordId );
			return url;
		}
		public static string FormatDetailUrl( string entityType, string entityCTID, string entitySubjectWebpage, int recordId, string friendlyName )
		{
			string url = string.Empty;
			if ( string.IsNullOrEmpty( entityCTID ) )
				url = entitySubjectWebpage;
			else
				url = reactFinderSiteURL + string.Format( "{0}/{1}/{2}", entityType, recordId, string.IsNullOrWhiteSpace( friendlyName ) ? string.Empty : friendlyName );
			return url;
		}
		#endregion
		#region validations, etc
		public static bool IsValidDate( DateTime date )
		{
			if ( date != null && date > new DateTime( 1492, 1, 1 ) )
				return true;
			else
				return false;
		}

		public static bool IsValidDate( DateTime? date )
		{
			if ( date != null && date > new DateTime( 1492, 1, 1 ) )
				return true;
			else
				return false;
		}
		public static bool IsValidDate( string date )
		{
			DateTime validDate;
			if ( string.IsNullOrWhiteSpace( date ) || date.Length < 8 )
			{
				//caller should store as is (ex. just a year.
				return false;
			}

			if ( !string.IsNullOrWhiteSpace( date )
				&& DateTime.TryParse( date, out validDate )
				&& date.Length >= 8
				&& validDate > new DateTime( 1492, 1, 1 )
				)
				return true;
			else
				return false;
		}
		public static bool IsInteger( string nbr )
		{
			int validNbr = 0;
			if ( !string.IsNullOrWhiteSpace( nbr ) && int.TryParse( nbr, out validNbr ) )
				return true;
			else
				return false;
		}
		public static bool IsInteger( string nbr, ref int validNbr )
		{
			validNbr = 0;
			if ( !string.IsNullOrWhiteSpace( nbr ) && int.TryParse( nbr, out validNbr ) )
				return true;
			else
				return false;
		}
		public static bool IsDecimal( string nbr, ref decimal validNbr )
		{
			validNbr = 0;
			if ( !string.IsNullOrWhiteSpace( nbr ) && decimal.TryParse( nbr, out validNbr ) )
				return true;
			else
				return false;
		}
		public static bool IsValid( string nbr )
		{
			int validNbr = 0;
			if ( !string.IsNullOrWhiteSpace( nbr ) && int.TryParse( nbr, out validNbr ) )
				return true;
			else
				return false;
		}

		public static bool IsValidGuid( Guid field )
		{
			if ( ( field == null || field.ToString() == DEFAULT_GUID ) )
				return false;
			else
				return true;
		}
		protected bool IsValidGuid( Guid? field )
		{
			if ( ( field == null || field == Guid.Empty ) )
				return false;
			else
				return true;
		}
		public static bool IsValidGuid( string field )
		{
			if ( string.IsNullOrWhiteSpace( field )
				|| field.Trim() == DEFAULT_GUID
				|| field.Length != 36
				)
				return false;
			else
				return true;
		}
		public static bool IsGuidValid( Guid field )
		{
			if ( ( field == null || field == Guid.Empty ) )
				return false;
			else
				return true;
		}

		public static bool IsGuidValid( Guid? field )
		{
			if ( ( field == null || field == Guid.Empty ) )
				return false;
			else
				return true;
		}
		public static string GetData( string text, string defaultValue = "" )
		{
			if ( string.IsNullOrWhiteSpace( text ) == false )
			{
				//TODO - should we try to normalize use of & for later compares?
				return text.Trim();
			}
			else
				return defaultValue;
		}
		public static int? SetData( int value, int minValue )
		{
			if ( value >= minValue )
				return value;
			else
				return null;
		}
		public static decimal? SetData( decimal value, decimal minValue )
		{
			if ( value >= minValue )
				return value;
			else
				return null;
		}
		public static DateTime? SetDate( string value )
		{
			DateTime output;
			if ( DateTime.TryParse( value, out output ) )
				return output;
			else
				return null;
		}
		public static string GetUrlData( string text, string defaultValue = "" )
		{
			if ( string.IsNullOrWhiteSpace( text ) == false )
			{
				//text = text.TrimEnd( '/' );
				return text.Trim();
			}
			else
				return defaultValue;
		}
		/// <summary>
		/// Validates the format of a Url
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		//public static bool IsUrlWellFormed( string url )
		//{
		//	string responseStatus = string.Empty;

		//	if ( string.IsNullOrWhiteSpace( url ) )
		//		return true;
		//	if ( !Uri.IsWellFormedUriString( url, UriKind.Absolute ) )
		//	{
		//		responseStatus = "The URL is not in a proper format";
		//		return false;
		//	}

		//	//may need to allow ftp, and others - not likely for this context?
		//	if ( url.ToLower().StartsWith( "http" ) == false )
		//	{
		//		responseStatus = "A URL must begin with http or https";

		//		return false;
		//	}

		//	//NOTE - do NOT use the HEAD option, as many sites reject that type of request
		//	var isOk = DoesRemoteFileExists( url, ref responseStatus );
		//	//optionally try other methods, or again with GET
		//	if ( !isOk && responseStatus == "999" )
		//		isOk = true;

		//	return isOk;
		//}
		public static bool IsUrlValid( string url, ref string statusMessage, bool allowingSchemaLess = false )
		{
			statusMessage = string.Empty;
			if ( string.IsNullOrWhiteSpace( url ) )
				return true;

			if ( !Uri.IsWellFormedUriString( url, UriKind.Absolute ) )
			{
				statusMessage = "The URL is not in a proper format (for example, must begin with http or https).";
				return false;
			}

			//may need to allow ftp, and others - not likely for this context?
			if ( url.ToLower().StartsWith( "http" ) == false )
			{
				//may want to allow just //. This is more likely with resources like images, stylesheets
				if ( !allowingSchemaLess || url.ToLower().StartsWith( "//" ) == false )
				{
					statusMessage = "A URL must begin with http or https";
					return false;
				}
			}

			var isOk = DoesRemoteFileExists( url, ref statusMessage );
			//optionally try other methods, or again with GET
			if ( !isOk && statusMessage == "999" )
				isOk = true;
			//	isOk = DoesRemoteFileExists( url, ref responseStatus, "GET" );
			return isOk;
		}
		/// <summary>
		/// Checks the file exists or not.
		/// </summary>
		/// <param name="url">The URL of the remote file.</param>
		/// <returns>True : If the file exits, False if file not exists</returns>
		public static bool DoesRemoteFileExists( string url, ref string responseStatus )
		{
			//this is may be preferred during import, as slows the process.
			//given import will be importing recently updated data, risk should be low for skipping.
			if ( UtilityManager.GetAppKeyValue( "skippingLinkChecking", false ) )
				return true;

			bool treatingRemoteFileNotExistingAsError = UtilityManager.GetAppKeyValue( "treatingRemoteFileNotExistingAsError", true );
			//consider stripping off https?
			//or if not found and https, try http
			try
			{
				if ( SkippingValidation( url ) )
					return true;

				//Creating the HttpWebRequest
				HttpWebRequest request = WebRequest.Create( url ) as HttpWebRequest;
				//NOTE - do use the HEAD option, as many sites reject that type of request
				//		GET seems to be cause false 404s
				//request.Method = "GET";
				//var agent = HttpContext.Current.Request.AcceptTypes;

				//the following also results in false 404s
				//request.ContentType = "text/html;charset=\"utf-8\";image/*";
				//UserAgent appears OK
				request.UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_8_2) AppleWebKit/537.17 (KHTML, like Gecko) Chrome/24.0.1309.0 Safari/537.17";

				//users may be providing urls to sites that have invalid ssl certs installed.You can ignore those cert problems if you put this line in before you make the actual web request:
				ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback( AcceptAllCertifications );
				//Getting the Web Response.
				HttpWebResponse response = request.GetResponse() as HttpWebResponse;
				//Returns TRUE if the Status code == 200
				response.Close();
				if ( response.StatusCode != HttpStatusCode.OK )
				{
					if ( url.ToLower().StartsWith( "https:" ) )
					{
						url = url.ToLower().Replace( "https:", "http:" );
						LoggingHelper.DoTrace( 5, string.Format( "_____________Failed for https, trying again using http: {0}", url ) );

						return DoesRemoteFileExists( url, ref responseStatus );
					}
					else
					{
						LoggingHelper.DoTrace( 5, string.Format( "Url validation failed for: {0}, using method: GET, with status of: {1}", url, response.StatusCode ) );
					}
				}
				responseStatus = response.StatusCode.ToString();

				return ( response.StatusCode == HttpStatusCode.OK );
				//apparantly sites like Linked In have can be a  problem
				//http://stackoverflow.com/questions/27231113/999-error-code-on-head-request-to-linkedin
				//may add code to skip linked In?, or allow on fail - which the same.
				//or some update, refer to the latter link

				//
			}
			catch ( WebException wex )
			{
				responseStatus = wex.Message;
				//
				if ( wex.Message.IndexOf( "(404)" ) > 1 )
					return false;
				else if ( wex.Message.IndexOf( "Too many automatic redirections were attempted" ) > -1 )
					return false;
				else if ( wex.Message.IndexOf( "(999" ) > 1 )
					return true;
				else if ( wex.Message.IndexOf( "(400) Bad Request" ) > 1 )
					return true;
				else if ( wex.Message.IndexOf( "(401) Unauthorized" ) > 1 )
					return true;
				else if ( wex.Message.IndexOf( "(406) Not Acceptable" ) > 1 )
					return true;
				else if ( wex.Message.IndexOf( "(500) Internal Server Error" ) > 1 )
					return true;
				else if ( wex.Message.IndexOf( "Could not create SSL/TLS secure channel" ) > 1 )
				{
					//https://www.naahq.org/education-careers/credentials/certification-for-apartment-maintenance-technicians 
					return true;

				}
				else if ( wex.Message.IndexOf( "Could not establish trust relationship for the SSL/TLS secure channel" ) > -1 )
				{
					return true;
				}
				else if ( wex.Message.IndexOf( "The underlying connection was closed: An unexpected error occurred on a send" ) > -1 )
				{
					return true;
				}
				else if ( wex.Message.IndexOf( "Detail=CR must be followed by LF" ) > 1 )
				{
					return true;
				}
				//var pageContent = new StreamReader( wex.Response.GetResponseStream() )
				//		 .ReadToEnd();
				if ( !treatingRemoteFileNotExistingAsError )
				{
					LoggingHelper.LogError(wex, string.Format("BaseFactory.DoesRemoteFileExists url: {0}. Exception Message:{1}; URL: {2}", url, wex.Message, GetWebUrl()));

					return true;
				}

				LoggingHelper.LogError(wex, string.Format("BaseFactory.DoesRemoteFileExists url: {0}. Exception Message:{1}", url, wex.Message));
				responseStatus = wex.Message;
				return false;
			}
			catch ( Exception ex )
			{

				if ( ex.Message.IndexOf( "(999" ) > -1 )
				{
					//linked in scenario
					responseStatus = "999";
				}
				else if ( ex.Message.IndexOf( "Could not create SSL/TLS secure channel" ) > 1 )
				{
					//https://www.naahq.org/education-careers/credentials/certification-for-apartment-maintenance-technicians 
					return true;

				}
				else if ( ex.Message.IndexOf( "(500) Internal Server Error" ) > 1 )
				{
					return true;
				}
				else if ( ex.Message.IndexOf( "(401) Unauthorized" ) > 1 )
				{
					return true;
				}
				else if ( ex.Message.IndexOf( "Could not establish trust relationship for the SSL/TLS secure channel" ) > 1 )
				{
					return true;
				}
				else if ( ex.Message.IndexOf( "Detail=CR must be followed by LF" ) > 1 )
				{
					return true;
				}
				if ( !treatingRemoteFileNotExistingAsError )
				{
					LoggingHelper.LogError(ex, string.Format("BaseFactory.DoesRemoteFileExists url: {0}. Exception Message:{1}", url, ex.Message));

					return true;
				}

				LoggingHelper.LogError(ex, string.Format("BaseFactory.DoesRemoteFileExists url: {0}. Exception Message:{1}", url, ex.Message));
				//Any exception will returns false.
				responseStatus = ex.Message;
				return false;
			}
		}
		public static bool AcceptAllCertifications( object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors )
		{
			return true;
		}
		private static bool SkippingValidation( string url )
		{


			Uri myUri = new Uri( url );
			string host = myUri.Host;

			string exceptions = UtilityManager.GetAppKeyValue( "urlExceptions" );
			//quick method to avoid loop
			if ( exceptions.IndexOf( host ) > -1 )
				return true;


			//string[] domains = exceptions.Split( ';' );
			//foreach ( string item in domains )
			//{
			//	if ( url.ToLower().IndexOf( item.Trim() ) > 5 )
			//		return true;
			//}

			return false;
		}

        #endregion
        public static bool IsValidCtid( string ctid )
        {
            List<string> messages = new List<string>();

            return IsValidCtid( ctid, ref messages );
        }
        public static bool IsValidCtid( string ctid, ref List<string> messages, bool isRequired = false, bool skippingErrorMessages = true )
        {
            bool isValid = true;

            if ( string.IsNullOrWhiteSpace( ctid ) )
            {
                if ( isRequired )
                {
                    messages.Add( "Error - A valid CTID property must be entered." );
                }
                return false;
            }

            ctid = ctid.ToLower().Trim();
            if ( ctid.Length != 39 )
            {
                if ( !skippingErrorMessages )
                    messages.Add( "Error - Invalid CTID format. The proper format is ce-UUID. ex. ce-84365aea-57a5-4b5a-8c1c-eae95d7a8c9b" );
                return false;
            }

            if ( !ctid.StartsWith( "ce-" ) )
            {
                if ( !skippingErrorMessages )
                    messages.Add( "Error - The CTID property must begin with ce-" );
                return false;
            }
            //now we have the proper length and format, the remainder must be a valid guid
            if ( !IsValidGuid( ctid.Substring( 3, 36 ) ) )
            {
                if ( !skippingErrorMessages )
                    messages.Add( "Error - Invalid CTID format. The proper format is ce-UUID. ex. ce-84365aea-57a5-4b5a-8c1c-eae95d7a8c9b" );
                return false;
            }

            return isValid;
        }
        /// <summary>
        /// Extract the ctid from a properly formatted registry URI
        /// </summary>
        /// <param name="registryURL"></param>
        /// <returns></returns>
        public static string ExtractCtid( string registryURL )
		{
			string ctid = string.Empty;
			if ( string.IsNullOrWhiteSpace( registryURL ) )
				return "";
			//check for just URL
			if ( registryURL.Length == 39 && registryURL.ToLower().IndexOf( "ce-" ) == 0 )
				return registryURL;
			if ( registryURL.ToLower().IndexOf( "/ce-" ) == -1 )
				return "";
			var parts = registryURL.ToLower().Split( '/' );
			if ( parts.Length > 0 )
			{
				ctid = parts[parts.Length - 1];
				return ctid;
			}

			int pos = registryURL.ToLower().IndexOf( "/graph/ce-" );
			if ( pos > 1 )
			{
				ctid = registryURL.Substring( pos + 7 );
			}
			else
			{
				pos = registryURL.ToLower().IndexOf( "/resources/ce-" );
				if ( pos > 1 )
					ctid = registryURL.Substring( pos + 11 );
				else
				{
					//shouldn't happen, once all fixed. In case was published without ce-
					pos = registryURL.ToLower().IndexOf( "/resources/" );
					if ( pos > 10 )
					{
						ctid = "ce-" + registryURL.Substring( pos + 11 );
					}
					else
					{
						pos = registryURL.ToLower().IndexOf( "/ce-" );
						if ( pos > -1 )
							ctid = registryURL.Substring( pos + 1 );
					}
				}
			}

			return ctid;
		}
        public static int ExtractIdFromURL( string input )
        {
            if ( string.IsNullOrWhiteSpace( input ) )
                return 0;
			int identifier = 0;
            var list = input.Split( '/' );
			if ( list != null && list.Any())
			{
				var id = list[list.Length - 1];
				int.TryParse( id, out identifier );
			}
			return identifier;
        }
        public static string AssignLimitedString( string text, int maxLength = 75 )
		{
			if ( string.IsNullOrWhiteSpace( text ) )
				return "";
			text = text.Trim();
			if ( text.Length > maxLength )
				return text.Substring( 0, maxLength ) + " ...";
			else
				return text;
		}
		public static string ConvertWordFluff( string text )
		{
			if ( string.IsNullOrWhiteSpace( text ) )
				return "";

			if ( text.IndexOf( '\u2013' ) > -1 ) text = text.Replace( '\u2013', '-' ); // en dash
			if ( text.IndexOf( '\u2014' ) > -1 ) text = text.Replace( '\u2014', '-' ); // em dash
			if ( text.IndexOf( '\u2015' ) > -1 ) text = text.Replace( '\u2015', '-' ); // horizontal bar
			if ( text.IndexOf( '\u2017' ) > -1 ) text = text.Replace( '\u2017', '_' ); // double low line
			if ( text.IndexOf( '\u2018' ) > -1 ) text = text.Replace( '\u2018', '\'' ); // left single quotation mark
			if ( text.IndexOf( '\u2019' ) > -1 ) text = text.Replace( '\u2019', '\'' ); // right single quotation mark
			if ( text.IndexOf( '\u201a' ) > -1 ) text = text.Replace( '\u201a', ',' ); // single low-9 quotation mark
			if ( text.IndexOf( '\u201b' ) > -1 ) text = text.Replace( '\u201b', '\'' ); // single high-reversed-9 quotation mark
			if ( text.IndexOf( '\u201c' ) > -1 ) text = text.Replace( '\u201c', '\"' ); // left double quotation mark
			if ( text.IndexOf( '\u201d' ) > -1 ) text = text.Replace( '\u201d', '\"' ); // right double quotation mark
			if ( text.IndexOf( '\u201e' ) > -1 ) text = text.Replace( '\u201e', '\"' ); // double low-9 quotation mark
			if ( text.IndexOf( '\u2026' ) > -1 ) text = text.Replace( "\u2026", "..." ); // horizontal ellipsis
			if ( text.IndexOf( '\u2032' ) > -1 ) text = text.Replace( '\u2032', '\'' ); // prime
			if ( text.IndexOf( '\u2033' ) > -1 ) text = text.Replace( '\u2033', '\"' ); // double prime

			return text.Trim();
		} //
		public static string FormatFriendlyTitle( string text )
		{
			try
			{
				if ( text == null || text.Trim().Length == 0 )
					return "";

				string title = UrlFriendlyTitle( text );

				//encode just incase
				title = HttpUtility.HtmlEncode( title );
				return title;
			}
			catch ( Exception ex )
			{
				return "";
			}
		}
		/// <summary>
		/// Format a title (such as for a library) to be url friendly
		/// NOTE: there are other methods:
		/// ILPathways.Utilities.UtilityManager.UrlFriendlyTitle()
		/// </summary>
		/// <param name="title"></param>
		/// <returns></returns>
		private static string UrlFriendlyTitle( string title )
		{
			if ( title == null || title.Trim().Length == 0 )
				return "";

			title = title.Trim();

			string encodedTitle = title.Replace( " - ", "-" );
			encodedTitle = encodedTitle.Replace( " ", "_" );

			//for now allow embedded periods
			//encodedTitle = encodedTitle.Replace( ".", "-" );

			encodedTitle = encodedTitle.Replace( "'", string.Empty );
			encodedTitle = encodedTitle.Replace( "&", "-" );
			encodedTitle = encodedTitle.Replace( "#", string.Empty );
			encodedTitle = encodedTitle.Replace( "$", "S" );
			encodedTitle = encodedTitle.Replace( "%", "percent" );
			encodedTitle = encodedTitle.Replace( "^", string.Empty );
			encodedTitle = encodedTitle.Replace( "*", string.Empty );
			encodedTitle = encodedTitle.Replace( "+", "_" );
			encodedTitle = encodedTitle.Replace( "~", "_" );
			encodedTitle = encodedTitle.Replace( "`", "_" );
			encodedTitle = encodedTitle.Replace( "/", "_" );
			encodedTitle = encodedTitle.Replace( "://", "/" );
			encodedTitle = encodedTitle.Replace( ":", string.Empty );
			encodedTitle = encodedTitle.Replace( ";", string.Empty );
			encodedTitle = encodedTitle.Replace( "?", string.Empty );
			encodedTitle = encodedTitle.Replace( "\"", "_" );
			encodedTitle = encodedTitle.Replace( "\\", "_" );
			encodedTitle = encodedTitle.Replace( "<", "_" );
			encodedTitle = encodedTitle.Replace( ">", "_" );
			encodedTitle = encodedTitle.Replace( "__", "_" );
			encodedTitle = encodedTitle.Replace( "__", "_" );
			encodedTitle = encodedTitle.Replace( "..", "_" );
			encodedTitle = encodedTitle.Replace( ".", "_" );

			if ( encodedTitle.EndsWith( "." ) )
				encodedTitle = encodedTitle.Substring( 0, encodedTitle.Length - 1 );

			return encodedTitle;
		} //
		public static string GenerateFriendlyName( string name )
		{
			if ( name == null || name.Trim().Length == 0 )
				return "";
			//another option could be use a pattern like the following?
			//string phrase = string.Format( "{0}-{1}", Id, name );

			string str = RemoveAccent( name ).ToLower();
			// invalid chars           
			str = Regex.Replace( str, @"[^a-z0-9\s-]", string.Empty );
			// convert multiple spaces into one space   
			str = Regex.Replace( str, @"\s+", " " ).Trim();
			// cut and trim 
			str = str.Substring( 0, str.Length <= 45 ? str.Length : 45 ).Trim();
			str = Regex.Replace( str, @"\s", "-" ); // hyphens   
			return str;
		}
		private static string RemoveAccent( string text )
		{
			byte[] bytes = System.Text.Encoding.GetEncoding( "Cyrillic" ).GetBytes( text );
			return System.Text.Encoding.ASCII.GetString( bytes );
		}
		#region Common Utility Methods
		public static string HandleApostrophes( string strValue )
		{

			if ( strValue.IndexOf( "'" ) > -1 )
			{
				strValue = strValue.Replace( "'", "''" );
			}
			if ( strValue.IndexOf( "''''" ) > -1 )
			{
				strValue = strValue.Replace( "''''", "''" );
			}

			return strValue;
		}
		public static string SearchifyWord( string word )
		{
			string keyword = word.Trim() + "^";


			//if ( keyword.ToLower().LastIndexOf( "es^" ) > 4 )
			//{
			//	//may be too loose
			//	//keyword = keyword.Substring( 0, keyword.ToLower().LastIndexOf( "es" ) );
			//}
			//else 
			if ( keyword.ToLower().LastIndexOf( "s^" ) > 4 )
			{
				keyword = keyword.Substring( 0, keyword.ToLower().LastIndexOf( "s" ) );
			}

			if ( keyword.ToLower().LastIndexOf( "ing^" ) > 3 )
			{
				keyword = keyword.Substring( 0, keyword.ToLower().LastIndexOf( "ing^" ) );
			}
			else if ( keyword.ToLower().LastIndexOf( "ed^" ) > 4 )
			{
				keyword = keyword.Substring( 0, keyword.ToLower().LastIndexOf( "ed^" ) );
			}
			else if ( keyword.ToLower().LastIndexOf( "ion^" ) > 3 )
			{
				keyword = keyword.Substring( 0, keyword.ToLower().LastIndexOf( "ion^" ) );
			}
			else if ( keyword.ToLower().LastIndexOf( "ive^" ) > 3 )
			{
				keyword = keyword.Substring( 0, keyword.ToLower().LastIndexOf( "ive^" ) );
			}

			if ( UtilityManager.GetAppKeyValue( "usingElasticCredentialSearch", false ) )
			{
				var env = UtilityManager.GetAppKeyValue( "environment" );
				//not sure of this
				if ( env != "production" && keyword.IndexOf( "*" ) == -1 )
				{
					//keyword = "*" + keyword.Trim() + "*";
					//keyword = keyword.Replace( "&", "*" ).Replace( " and ", "*" ).Replace( " in ", "*" ).Replace( " of ", "*" ).Replace( " for ", "*" ).Replace( " with ", "*" );
					//keyword = keyword.Replace( " from ", "*" );
					//keyword = keyword.Replace( " a ", "*" );
					//keyword = keyword.Replace( " - ", "*" );
					//keyword = keyword.Replace( " * ", "*" );

					////just replace all spaces with *?
					//keyword = keyword.Replace( "  ", "*" );
					//keyword = keyword.Replace( " ", "*" );
					//keyword = keyword.Replace( "**", "*" );
				}
			}
			else if ( keyword.IndexOf( "%" ) == -1 )
			{
				keyword = "%" + keyword.Trim() + "%";
				keyword = keyword.Replace( "&", "%" ).Replace( " and ", "%" ).Replace( " in ", "%" ).Replace( " of ", "%" ).Replace( " for ", "%" ).Replace( " with ", "%" );
				keyword = keyword.Replace( " from ", "%" );
				keyword = keyword.Replace( " a ", "%" );
				keyword = keyword.Replace( " - ", "%" );
				keyword = keyword.Replace( " % ", "%" );

				//just replace all spaces with %?
				keyword = keyword.Replace( "  ", "%" );
				keyword = keyword.Replace( " ", "%" );
				keyword = keyword.Replace( "%%", "%" );
			}


			keyword = keyword.Replace( "^", string.Empty );
			return keyword;
		}

		#endregion
		protected string HandleDBValidationError( System.Data.Entity.Validation.DbEntityValidationException dbex, string source, string title )
		{
			string message = string.Format( "{0} DbEntityValidationException, Name: {1}", source, title );

			foreach ( var eve in dbex.EntityValidationErrors )
			{
				message += string.Format( "\rEntity of type \"{0}\" in state \"{1}\" has the following validation errors:",
					eve.Entry.Entity.GetType().Name, eve.Entry.State );
				foreach ( var ve in eve.ValidationErrors )
				{
					message += string.Format( "- Property: \"{0}\", Error: \"{1}\"",
						ve.PropertyName, ve.ErrorMessage );
				}

				LoggingHelper.LogError( message );
			}

			return message;
		}
		
		public static string FormatExceptions( Exception ex )
		{
			string message = ex.Message;

			if ( ex.InnerException != null )
			{
				message += "; \r\nInnerException: " + ex.InnerException.Message;
				if ( ex.InnerException.InnerException != null )
				{
					message += "; \r\nInnerException2: " + ex.InnerException.InnerException.Message;
				}
			}
			//if ( ex. != null )
			//{
			//	message += "; \r\nInnerException: " + ex.InnerException.Message;
			//	if ( ex.InnerException.InnerException != null )
			//	{
			//		message += "; \r\nInnerException2: " + ex.InnerException.InnerException.Message;
			//	}
			//}

			return message;
		}
		public static string FormatExceptions2( SqlException ex )
		{
			string message = ex.Message;

			if ( ex.InnerException != null )
			{
				message += "; \r\nInnerException: " + ex.InnerException.Message;
				if ( ex.InnerException.InnerException != null )
				{
					message += "; \r\nInnerException2: " + ex.InnerException.InnerException.Message;
				}
			}

			return message;
		}
		/// <summary>
		/// Strip off text that is randomly added that starts with jquery
		/// Will need additional check for numbers - determine actual format
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string StripJqueryTag( string text )
		{
			int pos2 = text.ToLower().IndexOf( "jquery" );
			if ( pos2 > 1 )
			{
				text = text.Substring( 0, pos2 );
			}

			return text;
		}

		#region Dynamic Sql
		public static DataTable ReadTable( string tableViewName, string orderBy = "" )
		{
			// Table to store the query results
			DataTable table = new DataTable();
			if ( string.IsNullOrWhiteSpace( tableViewName ) )
				return table;
			if ( tableViewName.IndexOf( "[" ) == -1 )
				tableViewName = "[" + tableViewName.Trim() + "]";
			string sql = string.Format( "SELECT * FROM {0} ", tableViewName );
			if ( !string.IsNullOrWhiteSpace( orderBy ) )
				sql += " Order by " + orderBy;

			string connectionString = DBConnectionRO();
			// Creates a SQL connection
			using ( var connection = new SqlConnection( DBConnectionRO() ) )
			{
				connection.Open();

				// Creates a SQL command
				using ( var command = new SqlCommand( sql, connection ) )
				{
					// Loads the query results into the table
					table.Load( command.ExecuteReader() );
				}

				connection.Close();
			}

			return table;
		}
		public static DataTable ReadSql( string sql )
		{
			// Table to store the query results
			DataTable table = new DataTable();
			if ( string.IsNullOrWhiteSpace( sql ) )
				return table;

			string connectionString = DBConnectionRO();
			// Creates a SQL connection
			using ( var connection = new SqlConnection( DBConnectionRO() ) )
			{
				connection.Open();

				// Creates a SQL command
				using ( var command = new SqlCommand( sql, connection ) )
				{
					// Loads the query results into the table
					table.Load( command.ExecuteReader() );
				}

				connection.Close();
			}

			return table;
		} //

		

		/// <summary>
		/// Add an entry to the beginning of a Data Table
		/// </summary>
		/// <param name="tbl"></param>
		/// <param name="displayValue"></param>
		public static void AddEntryToTable( DataTable tbl, string displayValue )
		{
			DataRow r = tbl.NewRow();
			r[0] = displayValue;
			tbl.Rows.InsertAt( r, 0 );
		}

		/// <summary>
		/// Add an entry to the beginning of a Data Table. Uses a default key name of "id" and display column of "name"
		/// </summary>
		/// <param name="tbl"></param>
		/// <param name="keyValue"></param>
		/// <param name="displayValue"></param>
		public static void AddEntryToTable( DataTable tbl, int keyValue, string displayValue )
		{
			//DataRow r = tbl.NewRow();
			//r[ 0 ] = id;
			//r[ 1 ] = displayValue;
			//tbl.Rows.InsertAt( r, 0 );

			AddEntryToTable( tbl, keyValue, displayValue, "id", "name" );

		}

		/// <summary>
		/// Add an entry to the beginning of a Data Table. Uses a default key name of "id" and display column of "name"
		/// </summary>
		/// <param name="tbl"></param>
		/// <param name="keyValue"></param>
		/// <param name="displayValue"></param>
		/// <param name="keyName"></param>
		/// <param name="displayName"></param>
		public static void AddEntryToTable( DataTable tbl, int keyValue, string displayValue, string keyName, string displayName )
		{
			DataRow r = tbl.NewRow();
			r[keyName] = keyValue;
			r[displayName] = displayValue;
			tbl.Rows.InsertAt( r, 0 );

		}
		/// <summary>
		/// Add an entry to the beginning of a Data Table. Uses the provided key name and display column
		/// </summary>
		/// <param name="tbl"></param>
		/// <param name="keyValue"></param>
		/// <param name="displayValue"></param>
		/// <param name="keyName"></param>
		/// <param name="displayName"></param>
		public static void AddEntryToTable( DataTable tbl, string keyValue, string displayValue, string keyName, string displayName )
		{
			DataRow r = tbl.NewRow();
			r[keyName] = keyValue;
			r[displayName] = displayValue;
			tbl.Rows.InsertAt( r, 0 );

		}
		/// Check is dataset is valid and has at least one table with at least one row
		/// </summary>
		/// <param name="ds"></param>
		/// <returns></returns>
		public static bool DoesDataSetHaveRows( DataSet ds )
		{

			try
			{
				if ( ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 )
					return true;
				else
					return false;
			}
			catch
			{

				return false;
			}
		}//
		#endregion

	}
}
