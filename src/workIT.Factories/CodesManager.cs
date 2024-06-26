using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Data;
using EM = workIT.Data;
using Views = workIT.Data.Views;
using workIT.Data.Views;
using workIT.Models.Helpers.Reports;


using ThisEntity = workIT.Models.Common.Enumeration;
using DBEntity = workIT.Data.Tables.Entity_Property;

using ViewContext = workIT.Data.Views.workITViews;
using EntityContext = workIT.Data.Tables.workITEntities;
using workIT.Data.Tables;
using workIT.Utilities;
using System.Linq.Expressions;
using System.Runtime.Caching;
using Newtonsoft.Json;

namespace workIT.Factories
{
	public class CodesManager : BaseFactory
	{
		#region constants - property categories
		public static int PROPERTY_CATEGORY_ORGANIZATION_CLASS_TYPE = 1;
		public static int PROPERTY_CATEGORY_CREDENTIAL_TYPE = 2;
		public static int PROPERTY_CATEGORY_LEARNING_OBJECT_TYPE = 3;

		//public static int PROPERTY_CATEGORY_CREDENTIAL_PURPOSE = 3;
		/// <summary>
		/// AudienceLevelType
		/// </summary>
		public static int PROPERTY_CATEGORY_AUDIENCE_LEVEL = 4;

		public static int PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST = 5;
		public static int PROPERTY_CATEGORY_ORG_SERVICE = 6;
		public static int PROPERTY_CATEGORY_ORGANIZATION_TYPE = 7;
		public static int PROPERTY_CATEGORY_ORGANIZATION_SOCIAL_MEDIA = 8;
		public static int PROPERTY_CATEGORY_ORGANIZATION_IDENTIFIERS = 9;

		public static int PROPERTY_CATEGORY_NAICS = 10;
		public static int PROPERTY_CATEGORY_SOC = 11;
		public static int PROPERTY_CATEGORY_NAVY_RATING = 12;
		public static int PROPERTY_CATEGORY_CIP = 23;

		public static int PROPERTY_CATEGORY_ENTITY_AGENT_ROLE = 13;
		public static int PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE = 13;
		public static int PROPERTY_CATEGORY_AUDIENCE_TYPE = 14;
		public static int PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE = 15;
		public static int PROPERTY_CATEGORY_ASSESSMENT_TYPE = 16;

		public static int PROPERTY_CATEGORY_ASMT_DELIVERY_TYPE = 18;
		public static int PROPERTY_CATEGORY_ENROLLMENT_TYPE = 19;
		public static int PROPERTY_CATEGORY_RESIDENCY_TYPE = 20;

		public static int PROPERTY_CATEGORY_DELIVERY_TYPE = 21;
		public static int PROPERTY_CATEGORY_JURISDICTION_PROFILE_PURPOSE = 22;
		//public static int PROPERTY_CATEGORY_CIPCODE = 23;
		public static int PROPERTY_CATEGORY_CURRENCIES = 24;
		public static int PROPERTY_CATEGORY_REFERENCE_URLS = 25;
		//NEW
		public static int PROPERTY_CATEGORY_CREDENTIALING_ACTION_TYPE = 26;
		public static int PROPERTY_CATEGORY_CREDENTIAL_URLS = 27;
		public static int PROPERTY_CATEGORY_CONDITION_ITEM = 28;
		public static int PROPERTY_CATEGORY_COMPETENCY = 29;

		public static int PROPERTY_CATEGORY_ORGANIZATION_SECTORTYPE = 30;
		public static int PROPERTY_CATEGORY_PHONE_TYPE = 31;
		public static int PROPERTY_CATEGORY_EMAIL_TYPE = 32;
		public static int PROPERTY_CATEGORY_AGENT_QAPURPOSE_TYPE = 33;
		public static int PROPERTY_CATEGORY_SUBJECT = 34;
		public static int PROPERTY_CATEGORY_KEYWORD = 35;
		public static int PROPERTY_CATEGORY_ALIGNMENT_TYPE = 36;
		public static int PROPERTY_CATEGORY_ASSESSMENT_USE_TYPE = 37;
		public static int PROPERTY_CATEGORY_ALTERNATE_NAME = 38;
		public static int PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE = 39;
		public static string PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE_ACTIVE = "credentialStat:Active";
		public static string PROPERTY_CATEGORY_CREDENTIAL_STATUS_TYPE_DEPRECATED = "credentialStat:Deprecated";

		public static int PROPERTY_CATEGORY_ACTION_STATUS_TYPE = 40;
		public static int PROPERTY_CATEGORY_CLAIM_TYPE = 41;
		public static int PROPERTY_CATEGORY_EXTERNAL_INPUT_TYPE = 42;
		public static int PROPERTY_CATEGORY_FINANCIAL_ASSISTANCE = 43;
		//      [Obsolete]
		//      public static int PROPERTY_CATEGORY_STAFF_EVALUATION_METHOD = 44;
		public static int PROPERTY_CATEGORY_QA_TARGET_TYPE = 45;
		public static int PROPERTY_CATEGORY_PATHWAY_COMPONENT_TYPE = 46;
		//public static int PROPERTY_CATEGORY_LEARNING_RESOURCE_URLS = 46;
		public static int PROPERTY_CATEGORY_OWNING_ORGANIZATION_TYPE = 47;
		public static int PROPERTY_CATEGORY_PRIMARY_EARN_METHOD = 48;

		public static int PROPERTY_CATEGORY_CREDIT_UNIT_TYPE = 50;
		public static int PROPERTY_CATEGORY_JurisdictionAssertionType = 52;
		public static int PROPERTY_CATEGORY_Learning_Method_Type = 53;
		public static int PROPERTY_CATEGORY_Scoring_Method = 54;

		public static int PROPERTY_CATEGORY_Assessment_Method_Type = 56;

		public static int PROPERTY_CATEGORY_SUBMISSION_ITEM = 57;

		//reporting

		public static int PROPRTY_CREDENTIAL_REPORT_ITEM = 58;
		public static int PROPRTY_ORGANIZATION_REPORT_ITEM = 59;
		public static int PROPRTY_ASSESSMENT_REPORT_ITEM = 60;
		public static int PROPRTY_LOPP_REPORT_ITEM = 61;
		//
		public static int PROPRTY_PATHWAY_REPORT_ITEM = 70;
		public static int PROPRTY_TRANSFERVALUE_REPORT_ITEM = 71;
		public static int PROPRTY_TRANSFERINTERMEDIARY_REPORT_ITEM = 72;

		//continued
		public static int PROPERTY_CATEGORY_DEGREE_CONCENTRATION = 62;
		public static int PROPERTY_CATEGORY_DEGREE_MAJOR = 63;
		public static int PROPERTY_CATEGORY_DEGREE_MINOR = 64;

		public static int PROPERTY_CATEGORY_LANGUAGE = 65;
		//
		public static int PROPERTY_CATEGORY_PHONE_TYPE_FAX = 73; //change to 73 from 77
																 //no 74
																 //public static int PROPERTY_CATEGORY_ACTION_STATUS = 74;
		public static int PROPERTY_CATEGORY_DATA_COLLECTION_METHOD_TYPE = 75;
		public static int PROPERTY_CATEGORY_SAME_AS = 76;
		public static int PROPERTY_CATEGORY_DATA_SOURCE_COVERAGE = 77;
		public static int PROPERTY_CATEGORY_DATA_WITHHOLDING_CATEGORY = 78;
		public static int PROPERTY_CATEGORY_INCOME_DETERMINATION_METHOD = 79;
		public static int PROPERTY_CATEGORY_SUBJECT_CATEGORY = 80;
		//
		public static int PROPERTY_PUBLICATION_STATUS = 81;
		public static int PROPERTY_CATEGORY_ADMINISTRATIVE_RECORD_CATEGORY = 82;
		public static int PROPERTY_CATEGORY_KSA_TYPE = 83;
		public static int PROPERTY_CATEGORY_LIFE_CYCLE_STATUS = 84;
		public static string PROPERTY_CATEGORY_LIFE_CYCLE_STATUS_ACTIVE = "lifeCycle:Active";
		public static string PROPERTY_CATEGORY_LIFE_CYCLE_STATUS_CEASED = "lifeCycle:Ceased";
		public static int PROPERTY_CATEGORY_COLLECTION_CATEGORY = 85;
		//??	86
		public static int PROPERTY_CATEGORY_Environmental_Hazard_Type = 87;
		public static int PROPERTY_CATEGORY_Demand_Level_Type = 88;
		public static int PROPERTY_CATEGORY_Performance_Level_Type = 89;
		public static int PROPERTY_CATEGORY_Physical_Capability_Type = 90;

		//
		public static int PROPERTY_CATEGORY_ARRAY_OPERATION_CATEGORY = 91;
		public static int PROPERTY_CATEGORY_COMPARATOR_CATEGORY = 92;
		public static int PROPERTY_CATEGORY_LOGICAL_OPERATOR_CATEGORY = 93;
		//schedule and offer frequency have the same concept scheme, but will need to retrieve separately!
		public static int PROPERTY_CATEGORY_SCHEDULE_FREQUENCY = 94;
		public static int PROPERTY_CATEGORY_SCHEDULE_TIMING = 95;
		public static int PROPERTY_CATEGORY_OFFER_FREQUENCY = 96;
		//proxy/virtual ?? applicable
		public static int PROPERTY_CATEGORY_US_REGIONS = 98;
		public static int PROPERTY_CATEGORY_OTHER_FILTERS = 99;

		public static int PROPERTY_CATEGORY_ACCOMMODATION = 100;
		public static int PROPERTY_CATEGORY_SUPPORT_SERVICE_CATEGORY = 101; 
		public static int PROPERTY_CATEGORY_EVALUATOR_CATEGORY = 102;
		public static int PROPERTY_CATEGORY_PLACEHOLDER_CATEGORY = 103;


		//Virtual placeholder categories for use with FinderAPI
		public static int PROPERTY_CATEGORY_HISTORY = 200;
		public static int PROPERTY_CATEGORY_QA_PERFORMED = 201;
		public static int PROPERTY_CATEGORY_ORG_OWNS_OFFERS = 202;
		public static int PROPERTY_CATEGORY_ORG_HAS_FOR_TVPS = 203;
		public static int PROPERTY_CATEGORY_ORG_HAS_FROM_TVPS = 204;



		#endregion
		#region constants - entity types. 
		//An Entity is typically created only where it can have a child relationship, ex: Entity.Property
		public static int ENTITY_TYPE_CREDENTIAL = 1;
		public static int ENTITY_TYPE_CREDENTIAL_ORGANIZATION = 2; //ENTITY_TYPE_QAORGANIZATION = 13
		public static int ENTITY_TYPE_ASSESSMENT_PROFILE = 3;
		public static string ENTITY_TYPE_LABEL_ASSESSMENT_PROFILE = "AssessmentProfile";

		public static int ENTITY_TYPE_CONNECTION_PROFILE = 4;
		public static int ENTITY_TYPE_CONDITION_PROFILE = 4;
		public static int ENTITY_TYPE_COST_PROFILE = 5;
		public static int ENTITY_TYPE_COST_PROFILE_ITEM = 6;
		public static int ENTITY_TYPE_LEARNING_OPP_PROFILE = 7;
		public static int ENTITY_TYPE_PATHWAY = 8;
		public static int ENTITY_TYPE_COLLECTION = 9;

		public static int ENTITY_TYPE_COMPETENCY_FRAMEWORK = 10;    //	(ENTITY_TYPE_COMPETENCY = 17)
		public static int ENTITY_TYPE_CONCEPT_SCHEME = 11;          //	(ENTITY_TYPE_CONCEPT = 29)
		public static int ENTITY_TYPE_PROGRESSION_MODEL = 12;       //	(ENTITY_TYPE_PROGRESSION_LEVEL = 30)

		public static int ENTITY_TYPE_QAORGANIZATION = 13;
		//for now use plain until addressed all chgs
		public static int ENTITY_TYPE_PLAIN_ORGANIZATION = 14;

		public static int ENTITY_TYPE_SCHEDULED_OFFERING = 15;

		public static int ENTITY_TYPE_ADDRESS_PROFILE = 16;
		public static int ENTITY_TYPE_COMPETENCY = 17;
		public static int ENTITY_TYPE_COLLECTION_COMPETENCY = 18;
		public static int ENTITY_TYPE_CONDITION_MANIFEST = 19;
		public static int ENTITY_TYPE_COST_MANIFEST = 20;
		//
		public static int ENTITY_TYPE_FINANCIAL_ASST_PROFILE = 21;
		public static int ENTITY_TYPE_CREDENTIALING_ACTION = 22;
		public static int ENTITY_TYPE_PATHWAY_SET = 23;
		public static int ENTITY_TYPE_PATHWAY_COMPONENT = 24;
		public static int ENTITY_TYPE_COMPONENT_CONDITION = 25;
		//
		public static int ENTITY_TYPE_TRANSFER_VALUE_PROFILE = 26;
		public static int ENTITY_TYPE_AGGREGATE_DATA_PROFILE = 27;
		public static int ENTITY_TYPE_TRANSFER_INTERMEDIARY = 28;
		public static int ENTITY_TYPE_CONCEPT = 29;
		public static int ENTITY_TYPE_PROGRESSION_LEVEL = 30;
		//
		public static int ENTITY_TYPE_DATASET_PROFILE = 31;
		public static int ENTITY_TYPE_JOB_PROFILE = 32;
		public static int ENTITY_TYPE_TASK_PROFILE = 33;
		public static int ENTITY_TYPE_WORKROLE_PROFILE = 34;
		public static int ENTITY_TYPE_OCCUPATIONS_PROFILE = 35;
		//
		public static int ENTITY_TYPE_LEARNING_PROGRAM = 36;
		public static int ENTITY_TYPE_COURSE = 37;
		public static int ENTITY_TYPE_SUPPORT_SERVICE = 38;
		public static int ENTITY_TYPE_RUBRIC = 39;

		//refactored non top level resources
		public static int ENTITY_TYPE_REVOCATION_PROFILE = 40;
		public static int ENTITY_TYPE_VERIFICATION_PROFILE = 41;
		public static int ENTITY_TYPE_PROCESS_PROFILE = 42;
		public static int ENTITY_TYPE_CONTACT_POINT = 43;

		public static int ENTITY_TYPE_RUBRIC_CRITERION = 44;
		public static int ENTITY_TYPE_RUBRIC_CRITERION_LEVEL = 45;
		public static int ENTITY_TYPE_RUBRIC_LEVEL = 46;

		public static int ENTITY_TYPE_JURISDICTION_PROFILE = 50;
		//obsolete/unused yet
		public static int ENTITY_TYPE_EARNINGS_PROFILE = 55;
		public static int ENTITY_TYPE_EMPLOYMENT_OUTCOME_PROFILE = 56;
		public static int ENTITY_TYPE_HOLDERS_PROFILE = 57;

		/// <summary>
		/// Placeholder for stats, will not actually have an entity
		/// </summary>

		public static int ENTITY_TYPE_DURATION_PROFILE = 61;


		#endregion
		#region constants - EntityStateId
		public static int ENTITY_STATEID_DELETED = 0;
		public static int ENTITY_STATEID_PENDING = 1;
		public static int ENTITY_STATEID_REFERENCE = 2;
		public static int ENTITY_STATEID_FULL_ENTITY = 3;
		#endregion

		#region NON-json emumerations retrieve
		/// <summary>
		/// Get an enumeration
		/// </summary>
		/// <param name="datasource"></param>
		/// <param name="getAll">If false, only return codes with Totals > 0</param>
		/// <returns></returns>
		public static Enumeration GetEnumeration( string datasource, bool getAll = true, bool onlySubType1 = false )
		{

			return GetFromCacheOrDB( GetCacheName( "aee81bb9-f6b0-47cf-bcc4-cb309d93646d", datasource, getAll, onlySubType1 ), () =>
			{
				using ( var context = new EntityContext() )
				{
					//context.Configuration.LazyLoadingEnabled = false;

					Codes_PropertyCategory category = context.Codes_PropertyCategory
								.FirstOrDefault( s => s.CodeName.ToLower() == datasource.ToLower() && s.IsActive == true );

					return FillEnumeration( category, getAll, onlySubType1 );
				}
			} );
		}

		public static Enumeration GetEnumeration( int categoryId, bool getAll = true )
		{
			return GetFromCacheOrDB( GetCacheName( "27b3776a-3c2b-49e7-8b0b-e30e7c3313db", categoryId, getAll ), () =>
			{
				using ( var context = new EntityContext() )
				{

					Codes_PropertyCategory category = context.Codes_PropertyCategory
								.FirstOrDefault( s => s.Id == categoryId && s.IsActive == true );

					return FillEnumeration( category, getAll, false );

				}
			} );

		}
		private static Enumeration FillEnumeration( Codes_PropertyCategory category, bool getAll, bool onlySubType1 )
		{
			Enumeration entity = new Enumeration();
			using ( var context = new EntityContext() )
			{
				if ( category != null && category.Id > 0 )
				{
					entity.Id = category.Id;
					entity.Name = category.Title;
					entity.Description = category.Description;

					entity.SchemaName = category.SchemaName;
					entity.Url = category.SchemaUrl;
					entity.Items = new List<EnumeratedItem>();


					var results = context.Codes_PropertyValue
							.Where( s => s.IsActive == true && s.CategoryId == category.Id
							&& ( getAll || s.Totals > 0 )
							)
							.OrderBy( p => p.Title )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						EnumeratedItem val = new EnumeratedItem();
						var sortedList = results.Where( s => s.IsActive == true ).OrderBy( x => x.SortOrder ).ThenBy( z => z.Title ).ToList();

						//foreach ( Codes_PropertyValue item in category.Codes_PropertyValue )
						foreach ( Codes_PropertyValue item in results )
						{
							val = new EnumeratedItem();
							val.Id = item.Id;
							val.CodeId = item.Id;
							val.ParentId = category.Id;

							val.Name = item.Title;
							val.Description = item.Description != null ? item.Description : "";
							val.SortOrder = item.SortOrder != null ? ( int ) item.SortOrder : 0;
							val.SchemaName = item.SchemaName ?? string.Empty;
							val.SchemaUrl = item.SchemaUrl;
							val.ParentSchemaName = item.ParentSchemaName ?? string.Empty;
							val.Value = item.Id.ToString();
							val.Totals = item.Totals ?? 0;
							if ( IsDevEnv() )
								val.Name += string.Format( " ({0})", val.Totals );
							if ( val.SchemaName == "{none}" )
							{
								//skip
								//consider other exceptions
							}
							else
								entity.Items.Add( val );
						}
						//need to reorder the Items by sortOrder, then name. 
					}
					else
					{
						//typically categories without properties, like Naics, SOC, etc
						if ( " 6 10 11 12 13 23".IndexOf( category.Id.ToString() ) == -1 )
						{
							workIT.Utilities.LoggingHelper.DoTrace( 6, string.Format( "$$$$$$ no properties were found for categoryId: {0}, Category: {1}", category.Id, category.Title ) );
						}
					}
				}
			}

			return entity;
		}

		///// <summary>
		///// Get the selected item from an enumeration that only allows a singles selection
		///// </summary>
		///// <param name="e"></param>
		///// <returns></returns>
		//public static int GetEnumerationSelection( Enumeration e )
		//{
		//	int selectedId = 0;
		//	if ( e == null || e.Items == null || e.Items.Count() == 0 )
		//	{
		//		return 0;
		//	}

		//	foreach ( EnumeratedItem item in e.Items )
		//	{
		//		if ( item.Selected )
		//		{
		//			selectedId = item.Id;
		//			break;
		//		}
		//	}

		//	return selectedId;

		//}

		public static Enumeration GetCredentialTypes( string datasource, bool getAll, bool includingAllBadges = false, string parentSchemaName = "" )
		{
			Enumeration entity = new Enumeration();
			using ( var context = new EntityContext() )
			{
				//context.Configuration.LazyLoadingEnabled = false;

				Codes_PropertyCategory category = context.Codes_PropertyCategory
							.FirstOrDefault( s => s.CodeName.ToLower() == datasource.ToLower()
												&& s.IsActive == true
												);

				if ( category != null && category.Id > 0 )
				{
					entity.Id = category.Id;
					entity.Name = category.Title;
					entity.Description = category.Description;

					entity.SchemaName = category.SchemaName;
					entity.Url = category.SchemaUrl;
					entity.Items = new List<EnumeratedItem>();

					var results = context.Codes_PropertyValue
							.Where( s => s.IsActive == true && s.CategoryId == category.Id
							&& ( getAll || s.Totals > 0 )
							&& ( parentSchemaName == string.Empty || s.ParentSchemaName == parentSchemaName )
							)
							.OrderBy( p => p.Title )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						EnumeratedItem val = new EnumeratedItem();
						var sortedList = results.Where( s => s.IsActive == true ).OrderBy( x => x.SortOrder ).ThenBy( z => z.Title ).ToList();

						//21-05-13 - only return one badge type
						bool hasABadge = false;
						foreach ( Codes_PropertyValue item in results )
						{
							val = new EnumeratedItem();
							val.Id = item.Id;
							val.CodeId = item.Id;
							val.ParentId = category.Id;

							val.Name = item.Title;
							val.Description = item.Description != null ? item.Description : "";
							val.SortOrder = item.SortOrder != null ? ( int ) item.SortOrder : 0;
							val.SchemaName = item.SchemaName ?? string.Empty;
							val.SchemaUrl = item.SchemaUrl;
							val.ParentSchemaName = item.ParentSchemaName ?? string.Empty;
							if ( val.ParentSchemaName == "Badge" )
							{
								if ( hasABadge && !includingAllBadges )
									continue;
								hasABadge = true;
							}
							val.Value = item.Id.ToString();
							val.Totals = item.Totals ?? 0;
							if ( IsDevEnv() )
								val.Name += string.Format( " ({0})", val.Totals );
							if ( val.SchemaName == "{none}" )
							{
								//skip
								//consider other exceptions
							}
							else
								entity.Items.Add( val );
						}
						//need to reorder the Items by sortOrder, then name. 
					}

				}


			}
			return entity;
		}
		public static Enumeration GetCredentialBadgeTypes( string datasource, bool getAll )
		{
			Enumeration entity = new Enumeration();
			using ( var context = new EntityContext() )
			{
				Codes_PropertyCategory category = context.Codes_PropertyCategory
							.FirstOrDefault( s => s.CodeName.ToLower() == datasource.ToLower() && s.IsActive == true );

				if ( category != null && category.Id > 0 )
				{
					entity.Id = category.Id;
					entity.Name = category.Title;
					entity.Description = category.Description;
					entity.SchemaName = category.SchemaName;
					entity.Url = category.SchemaUrl;
					entity.Items = new List<EnumeratedItem>();

					var results = context.Codes_PropertyValue
							.Where( s => s.IsActive == true && s.CategoryId == category.Id
							&& ( getAll || s.Totals > 0 )
							&& ( s.ParentSchemaName == "Badge" )
							)
							.OrderBy( p => p.Title )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						EnumeratedItem val = new EnumeratedItem();
						var sortedList = results.Where( s => s.IsActive == true ).OrderBy( x => x.SortOrder ).ThenBy( z => z.Title ).ToList();

						//21-05-13 - only return one badge type
						bool hasABadge = false;
						foreach ( Codes_PropertyValue item in results )
						{
							val = new EnumeratedItem();
							val.Id = item.Id;
							val.CodeId = item.Id;
							val.ParentId = category.Id;

							val.Name = item.Title;
							val.Description = item.Description != null ? item.Description : "";
							val.SortOrder = item.SortOrder != null ? ( int ) item.SortOrder : 0;
							val.SchemaName = item.SchemaName ?? string.Empty;
							val.SchemaUrl = item.SchemaUrl;
							val.ParentSchemaName = item.ParentSchemaName ?? string.Empty;
							if ( val.ParentSchemaName == "Badge" )
							{
								if ( hasABadge )
									continue;
								hasABadge = true;
							}
							val.Value = item.Id.ToString();
							val.Totals = item.Totals ?? 0;
							if ( IsDevEnv() )
								val.Name += string.Format( " ({0})", val.Totals );
							if ( val.SchemaName == "{none}" )
							{
								//skip
								//consider other exceptions
							}
							else
								entity.Items.Add( val );
						}
						//need to reorder the Items by sortOrder, then name. 
					}

				}


			}
			return entity;
		}
		#endregion

		#region Counts.SiteTotals
		public static Enumeration GetSiteTotalsAsEnumeration( int categoryId, int entityTypeId, bool getAll = true )
		{
			using ( var context = new EntityContext() )
			{
				Codes_PropertyCategory category = context.Codes_PropertyCategory
							.FirstOrDefault( s => s.Id == categoryId && s.IsActive == true );
				return FillSiteTotalsAsEnumeration( category, entityTypeId, getAll, false );
			}
		}
		private static Enumeration FillSiteTotalsAsEnumeration( Codes_PropertyCategory category, int entityTypeId, bool getAll, bool onlySubType1 )
		{
			Enumeration entity = new Enumeration();
			using ( var context = new EntityContext() )
			{
				if ( category != null && category.Id > 0 )
				{
					entity.Id = category.Id;
					entity.Name = category.Title;
					entity.Description = category.Description;

					entity.SchemaName = category.SchemaName;
					entity.Url = category.SchemaUrl;
					entity.Items = new List<EnumeratedItem>();


					var results = context.Counts_SiteTotals
							.Where( s => s.CategoryId == category.Id && s.EntityTypeId == entityTypeId
								 && ( getAll || s.Totals > 0 ) )
							.OrderBy( p => p.Title )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						EnumeratedItem val = new EnumeratedItem();
						foreach ( var item in results )
						{
							val = new EnumeratedItem();
							//21-05-11 mparsons - found that for at least language, want to use Id. Not sure if this will be a proble for other codes
							if ( item.CodeId == null || item.CodeId == 0 )
								val.Id = item.Id;
							else
								val.Id = item.CodeId == null ? 0 : ( int ) item.CodeId;

							val.CodeId = item.CodeId == null ? 0 : ( int ) item.CodeId;
							val.ParentId = category.Id;
							val.Name = item.Title;
							val.SchemaName = item.SchemaName;
							//20-04-15 mparsons - added use of CategoryId here, looking for consisent use of this vs sometimes id, and sometimes categoryId
							val.CategoryId = item.CategoryId;
							if ( category.Id == 65 )
							{
								val.Value = item.Title;
								//just in case the above chg gets reversed
								val.Id = item.Id;
							}
							else
							{
								val.Value = item.CodeId == null ? string.Empty : ( ( int ) item.CodeId ).ToString();
							}
							if ( category.Id == 4 || category.Id == 14 || category.Id == 18 || category.Id == 21 )
							{
								//get description
								var cd = Codes_PropertyValue_Get( category.Id, item.Title );
								if ( cd != null && !string.IsNullOrWhiteSpace( cd.Description ) )
									val.Description = cd.Description;
							}
							val.Totals = item.Totals ?? 0;
							if ( IsDevEnv() )
								val.Name += string.Format( " ({0})", val.Totals );
							if ( getAll || val.Totals > 0 )
								entity.Items.Add( val );
						}
					}
				}
			}

			return entity;
		}
		/// <summary>
		/// Get a code item by category and title
		/// </summary>
		/// <param name="categoryId"></param>
		/// <param name="title"></param>
		/// <returns></returns>
		private static CodeItem Codes_PropertyValue_Get( int categoryId, string title )
		{
			CodeItem code = new CodeItem();

			using ( var context = new EntityContext() )
			{
				List<Codes_PropertyValue> results = context.Codes_PropertyValue
					.Where( s => s.CategoryId == categoryId
							&& ( s.IsActive == true )
							&& s.Title.ToLower() == title.ToLower() )
							.ToList();

				if ( results != null && results.Count > 0 )
				{
					foreach ( Codes_PropertyValue item in results )
					{
						code = new CodeItem();
						code.Id = ( int ) item.Id;
						code.Title = item.Title;
						code.Description = item.Description;
						code.URL = item.SchemaUrl;
						code.SchemaName = item.SchemaName;
						code.ParentSchemaName = item.ParentSchemaName;
						code.Totals = item.Totals ?? 0;
						break;
					}
				}
			}
			return code;
		}

		public static List<CodeItem> GetAllPathwayComponentStatistics()
		{
			List<CodeItem> list = new List<CodeItem>();
			CodeItem code;
			using ( var context = new EntityContext() )
			{
				var results = context.Codes_PathwayComponentType.ToList();

				if ( results != null && results.Count > 0 )
				{
					foreach ( var item in results )
					{
						code = new CodeItem
						{
							Id = ( int ) item.Id,
							CategoryId = item.Id,
							Title = item.Title,
							SchemaName = item.SchemaName,
							ParentSchemaName = "ceterms:PathwayComponentType",
							Description = item.Description,
						};
						code.Description = item.Description;
						code.Totals = item.Totals ?? 0;
						list.Add( code );
					}
				}
			}
			return list;
		}
		#endregion
		#region Counts.EntityStatistic
		public void UpdateEntityStatistic( int entityTypeId, string schemaName, int total, bool allowingZero = true )
		{
			try
			{
				using ( var context = new EntityContext() )
				{
					var efEntity = context.Counts_EntityStatistic.SingleOrDefault( s => s.EntityTypeId == entityTypeId
					&& s.SchemaName == schemaName );
					if ( efEntity != null && efEntity.Id > 0 )
					{
						if ( total > 0 || allowingZero )
							efEntity.Totals = total;

						if ( HasStateChanged( context ) )
						{
							int count = context.SaveChanges();

							if ( count >= 0 )
							{

							}
							else
							{

							}
						}
					}
				}
			}

			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, string.Format( "CodesManager.UpdateEntityStatistic. entityTypeId: {0}, schemaName: {1}", entityTypeId, schemaName ) );
			}
		}

		public static List<CodeItem> GetAllEntityStatistics()
		{
			List<CodeItem> list = new List<CodeItem>();
			CodeItem code;
			using ( var context = new EntityContext() )
			{
				List<Counts_EntityStatistic> results = context.Counts_EntityStatistic.Where( s => s.IsActive == true ).ToList();

				if ( results != null && results.Count > 0 )
				{

					foreach ( var item in results )
					{
						//20-04-15 mparsons - appears that for entity statistics, we have to use the entitytypeId for the categoryId
						code = new CodeItem
						{
							Id = ( int ) item.Id,
							//CategoryId = ( int )( item.CategoryId ?? 0 ),
							//21-08-09 mp - force this to be 101 for better handling in new finder
							//21-08-13 mp - but now doesn't work in old finder
							CategoryId = CodesManager.PROPERTY_CATEGORY_OTHER_FILTERS, // item.EntityTypeId,
							EntityTypeId = item.EntityTypeId,
							Title = item.Title,
							SchemaName = item.SchemaName,
							Description = item.Description,
						};
						code.Description = item.Description;
						code.Totals = item.Totals ?? 0;
						if ( item.SchemaName == "frameworkReport:Competencies" )
						{

						}
						list.Add( code );
					}
				}
			}
			return list;
		}
		public static CodeItem GetEntityStatisticBySchema( int categoryId, string schemaName )
		{
			CodeItem code = new CodeItem();

			using ( var context = new EntityContext() )
			{

				var item = context.Counts_EntityStatistic
					.FirstOrDefault( s => s.CategoryId == categoryId
							&& s.SchemaName.Trim() == schemaName.Trim() );
				if ( item != null && item.Id > 0 )
				{
					//could have an additional check that the returned category is correct - no guarentees though
					code = new CodeItem
					{
						Id = ( int ) item.Id,
						CategoryId = ( int ) ( item.CategoryId ?? 0 ),
						Title = item.Title,
						Description = item.Description,
						SchemaName = item.SchemaName,
						Totals = item.Totals ?? 0
					};
				}
			}
			return code;
		}

		/// <summary>
		/// As we don't really need to use categoryId anymore, start getting by entityTypeId and schemaName (and yes may only really need schemaName)
		/// </summary>
		/// <param name="entityTypeId"></param>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		public static CodeItem GetEntityStatisticUsingEntityTypeId( int entityTypeId, string schemaName )
		{
			CodeItem code = new CodeItem();

			using ( var context = new EntityContext() )
			{

				var item = context.Counts_EntityStatistic
					.FirstOrDefault( s => s.EntityTypeId == entityTypeId
							&& s.SchemaName.Trim() == schemaName.Trim() );
				if ( item != null && item.Id > 0 )
				{
					//could have an additional check that the returned category is correct - no guarentees though
					code = new CodeItem
					{
						Id = ( int ) item.Id,
						CategoryId = ( int ) ( item.CategoryId ?? 0 ),
						Title = item.Title,
						Description = item.Description,
						SchemaName = item.SchemaName,
						Totals = item.Totals ?? 0
					};
				}
			}
			return code;
		}
		public static List<int> GetEntityStatisticBySchema( List<string> schemaNames )
		{
			var ids = new List<int>();

			using ( var context = new EntityContext() )
			{
				var list = context.Counts_EntityStatistic.Where( s => s.IsActive == true && schemaNames.Contains( s.SchemaName.Trim() ) );
				foreach ( var item in list )
					ids.Add( item.Id );
			}
			return ids;
		}
		public static Enumeration GetEntityStatisticsAsEnumeration( int entityTypeId, bool getAll = true )
		{
			Enumeration enumeration = new Enumeration();

			using ( var context = new EntityContext() )
			{
				//
				if ( entityTypeId == 0 )
				{
					enumeration.Id = 0; //??
					enumeration.Name = "All Entities";
					enumeration.Description = "All";
					enumeration.SchemaName = "All";
				}
				var entity = context.Codes_EntityTypes
							.FirstOrDefault( s => s.Id == entityTypeId && s.IsActive == true );

				if ( entity != null && entity.Id > 0 )
				{
					//
					//enumeration.Id = entity.Id;
					//maybe??
					enumeration.Id = CodesManager.PROPERTY_CATEGORY_OTHER_FILTERS;
					enumeration.Name = entity.Title;
					enumeration.Description = entity.Description;
					enumeration.SchemaName = entity.SchemaName;
					enumeration.Items = new List<EnumeratedItem>();

					var results = context.Counts_EntityStatistic
							.Where( s => s.EntityTypeId == entityTypeId && s.IsActive == true
								 && ( getAll || s.Totals > 0 )
								 )
							.OrderBy( p => p.SortOrder ).ThenBy( p => p.Title )
							.ToList();

					if ( results != null && results.Count() > 0 )
					{
						EnumeratedItem val = new EnumeratedItem();
						foreach ( var item in results )
						{
							val = new EnumeratedItem
							{
								//note the search appears to expect categoryId to be in Id
								Id = item.Id,
								CodeId = 0, //??
								ParentId = entity.Id, //??
								Name = item.Title,
								Value = item.Title,
								//prototype using same approach as GetAllEntityStatistics
								//CategoryId = item.CategoryId ?? 0,
								CategoryId = CodesManager.PROPERTY_CATEGORY_OTHER_FILTERS,

								Totals = item.Totals ?? 0,
								SchemaName = item.SchemaName
							};
							if ( IsDevEnv() )
								val.Name += string.Format( " ({0})", val.Totals );

							if ( getAll || val.Totals > 0 )
								enumeration.Items.Add( val );
						}
					}
				}
			}

			return enumeration;
		}

		#endregion

		#region Condition profile type
		public static Enumeration GetCommonConditionProfileTypes()
		{
			Enumeration entity = new Enumeration();

			using ( var context = new EntityContext() )
			{
				//get the property category
				Codes_PropertyCategory category = context.Codes_PropertyCategory
							.FirstOrDefault( s => s.Id == CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );

				if ( category != null && category.Id > 0 )
				{
					entity.Id = category.Id;
					entity.Name = category.Title;
					entity.Description = category.Description;
					entity.SchemaName = category.SchemaName;
					entity.Url = category.SchemaUrl;
					entity.Items = new List<EnumeratedItem>();

					EnumeratedItem val = new EnumeratedItem();
					var results = context.Codes_ConditionProfileType
							.Where( s => s.IsActive == true && s.IsCommonCondtionType == true )
							.OrderBy( p => p.Title )
							.ToList();

					foreach ( Codes_ConditionProfileType item in results )
					{
						val = new EnumeratedItem();
						val.Id = item.Id;
						val.CodeId = item.Id;
						val.Value = item.Id.ToString();
						val.Description = item.Description;
						val.Name = item.Title;
						entity.Items.Add( val );
					}

				}
			}

			return entity;
		}

		public static Enumeration GetCredentialsConditionProfileTypes( bool getAll = true )
		{
			return GetFromCacheOrDB( GetCacheName( "51134689-528f-46b2-b1aa-1c673ddc9a8d", getAll ), () =>
			{
				Enumeration entity = new Enumeration();

				using ( var context = new EntityContext() )
				{
					//get the property category
					Codes_PropertyCategory category = context.Codes_PropertyCategory
								.FirstOrDefault( s => s.Id == CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );

					if ( category != null && category.Id > 0 )
					{
						entity.Id = category.Id;
						entity.Name = category.Title;
						entity.Description = category.Description;

						entity.SchemaName = category.SchemaName;
						entity.Url = category.SchemaUrl;
						entity.Items = new List<EnumeratedItem>();

						EnumeratedItem val = new EnumeratedItem();

						var results = context.Codes_ConditionProfileType
							.Where( s => s.IsActive == true && s.IsCredentialsConnectionType == true )
							.OrderBy( p => p.Title )
							.ToList();

						foreach ( Codes_ConditionProfileType item in results )
						{
							val = new EnumeratedItem();
							val.Id = item.Id;
							val.CodeId = item.Id;
							val.Value = item.Id.ToString();
							val.Description = item.Description;
							val.Name = item.Title;
							val.SchemaName = item.SchemaName;
							val.Totals = item.CredentialTotals ?? 0;

							if ( getAll || val.Totals > 0 )
								entity.Items.Add( val );
						}

					}
				}

				return entity;
			} );

		}

		public static Enumeration GetConnectionTypes( int parentEntityTypeId, bool getAll = true )
		{
			return GetFromCacheOrDB( GetCacheName( "9f5a3285-b29a-4bc1-a2d6-453652f3d396", parentEntityTypeId, getAll ), () =>
			{
				Enumeration entity = new Enumeration();
				using ( var context = new EntityContext() )
				{
					//get the property category
					Codes_PropertyCategory category = context.Codes_PropertyCategory
								.FirstOrDefault( s => s.Id == CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );

					if ( category != null && category.Id > 0 )
					{
						entity.Id = category.Id;
						entity.Name = category.Title;
						entity.Description = category.Description;

						entity.SchemaName = category.SchemaName;
						entity.Url = category.SchemaUrl;
						entity.Items = new List<EnumeratedItem>();

						EnumeratedItem val = new EnumeratedItem();
						var results = context.Codes_ConditionProfileType
								.Where( s => s.IsActive == true &&
								 (
									 ( parentEntityTypeId == 1 && s.IsCredentialsConnectionType == true ) ||
									 ( parentEntityTypeId == 3 && s.IsAssessmentType == true ) ||
									 ( parentEntityTypeId == 7 && s.IsLearningOpportunityType == true )
								 )
								)
								.OrderBy( p => p.Title )
								.ToList();

						foreach ( Codes_ConditionProfileType item in results )
						{
							val = new EnumeratedItem();
							val.Id = item.Id;
							val.CodeId = item.Id;
							val.Value = item.Id.ToString();
							val.Description = item.Description;
							val.Name = item.ConditionManifestTitle;
							val.SchemaName = item.SchemaName;

							if ( parentEntityTypeId == 3 )
								val.Totals = item.AssessmentTotals ?? 0;
							else if ( parentEntityTypeId == 7 )
								val.Totals = item.LoppTotals ?? 0;
							else if ( parentEntityTypeId == 1 )
								val.Totals = item.CredentialTotals ?? 0;
							if ( IsDevEnv() )
								val.Name += string.Format( " ({0})", val.Totals );

							if ( getAll || val.Totals > 0 )
								entity.Items.Add( val );
						}
					}
				}

				return entity;
			} );
		}



		#endregion

		#region Jurisdiction assertions
		public static Enumeration GetJurisdictionAssertions_ForCredentials()
		{
			Enumeration entity = new Enumeration();
			EnumeratedItem val = new EnumeratedItem();
			using ( var context = new EntityContext() )
			{
				//get the property category
				Codes_PropertyCategory category = context.Codes_PropertyCategory
							.FirstOrDefault( s => s.Id == CodesManager.PROPERTY_CATEGORY_JurisdictionAssertionType );

				if ( category != null && category.Id > 0 )
				{
					entity.Id = category.Id;
					entity.Name = category.Title;
					entity.Description = category.Description;
					entity.SchemaName = category.SchemaName;
					entity.Url = category.SchemaUrl;
					entity.Items = new List<EnumeratedItem>();

					var codes_PropertyValue = context.Codes_PropertyValue
							.Where( s => s.IsActive == true && s.CategoryId == category.Id )
							.OrderBy( p => p.Title )
							.ToList();
					foreach ( Codes_PropertyValue item in codes_PropertyValue )
					{
						val = new EnumeratedItem();
						val.Id = item.Id;
						val.CodeId = item.Id;
						val.Value = item.Id.ToString();
						val.Description = item.Description;
						val.Name = item.Title;
						entity.Items.Add( val );
					}

				}
			}

			return entity;
		}

		public static Enumeration GetJurisdictionAssertions_Filtered( string filter )
		{
			Enumeration entity = new Enumeration();
			EnumeratedItem val = new EnumeratedItem();
			using ( var context = new EntityContext() )
			{
				//get the property category
				Codes_PropertyCategory category = context.Codes_PropertyCategory
							.FirstOrDefault( s => s.Id == CodesManager.PROPERTY_CATEGORY_JurisdictionAssertionType );

				if ( category != null && category.Id > 0 )
				{
					entity.Id = category.Id;
					entity.Name = category.Title;
					entity.Description = category.Description;
					entity.SchemaName = category.SchemaName;
					entity.Url = category.SchemaUrl;
					entity.Items = new List<EnumeratedItem>();

					var codes_PropertyValue = context.Codes_PropertyValue
							.Where( s => s.IsActive == true && s.CategoryId == category.Id )
							.OrderBy( p => p.Title )
							.ToList();
					foreach ( Codes_PropertyValue item in codes_PropertyValue )
					{
						if ( item.ParentSchemaName.IndexOf( filter ) > -1 )
						{
							val = new EnumeratedItem();
							val.Id = item.Id;
							val.CodeId = item.Id;
							val.Value = item.Id.ToString();
							val.Description = item.Description;
							val.Name = item.Title;

							entity.Items.Add( val );
						}

					}

				}
			}

			return entity;
		}

		#endregion

		#region Codes as Code Items
		public static List<CodeItem> Codes_EntityTypes_GetAll()
		{
			return GetFromCacheOrDB( GetCacheName( "500ba655-8f42-45b6-8542-b9113c898deb" ), () =>
			{
				List<CodeItem> list = new List<CodeItem>();
				CodeItem code;

				using ( var context = new EntityContext() )
				{
					List<Codes_EntityTypes> results = context.Codes_EntityTypes
						.Where( s => s.IsActive == true )
						.OrderBy( s => s.Title )
								.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( var item in results )
						{
							code = new CodeItem
							{
								Id = ( int ) item.Id,
								Title = item.Title,
								Description = item.Description,
								SchemaName = item.SchemaName,
								Totals = item.Totals ?? 0
							};

							list.Add( code );
						}
					}
				}
				return list;
			} );
		}
		public static CodeItem Codes_EntityType_Get( int entityTypeId )
		{
			return GetFromCacheOrDB( GetCacheName( "5715b3ad-37f0-47ce-99fe-9a9b95f4c374", entityTypeId ), () =>
			{
				CodeItem code = new CodeItem();
				using ( var context = new EntityContext() )
				{
					var results = context.Codes_EntityTypes
						.Where( s => s.IsActive == true && s.Id == entityTypeId )
						.OrderBy( s => s.Title )
								.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( var item in results )
						{
							code = new CodeItem();
							code.Id = ( int ) item.Id;
							code.Title = item.Title;
							code.Description = item.Description;
							code.SchemaName = item.SchemaName;
							code.Totals = item.Totals ?? 0;
							//should only have one entry
							break;
						}
					}
				}
				return code;
			} );
		}
		public static string Codes_EntityType_Get( int entityTypeId, string defaultType )
		{
			var entityType = string.Empty;
			using ( var context = new EntityContext() )
			{
				var results = context.Codes_EntityTypes
					.Where( s => s.IsActive == true && s.Id == entityTypeId )
					.FirstOrDefault();

				if ( results != null && results.Id > 0 )
				{
					entityType = results.Title;
				} else
					entityType = defaultType;
			}
			return entityType;

		}

		/// <summary>
		/// Return the EntityTypeId for the passed CTDL class
		/// </summary>
		/// <param name="entityType"></param>
		/// <returns></returns>
		public static int Codes_GetEntityTypeId( string entityType )
		{
			int entityTypeId = 0;
			using ( var context = new EntityContext() )
			{
				var results = context.Codes_EntityTypes
					.Where( s => s.IsActive == true && s.SchemaName == entityType )
					.FirstOrDefault();

				if ( results != null && results.Id > 0 )
				{
					entityTypeId = results.Id;
				}
				if ( entityTypeId == 0 )
				{
					//for now only exception should be credentials
					var codeItem = GetPropertyBySchema( 2, entityType );
					if ( codeItem != null && codeItem.Id > 0 )
						entityTypeId = 1;
				}

				if ( entityTypeId == 0 )
				{
					var codeItem = CodesManager.GetCredentialingActionType( entityType );
					if ( codeItem != null && codeItem.Id > 0 )
						entityTypeId = ENTITY_TYPE_CREDENTIALING_ACTION;
				}
			}
			return entityTypeId;

		}
		#endregion

		#region Codes_PropertyCategory and values as List<CodeItem> 
		//public static CodeItem Codes_PropertyCategory_Get( int categoryId )
		//{
		//    CodeItem code = new CodeItem();
		//    using ( var context = new EntityContext() )
		//    {
		//        List<Codes_PropertyCategory> results = context.Codes_PropertyCategory
		//            .Where( s => s.PropertyTableName == "Codes.PropertyValue"
		//                && s.IsActive == true )
		//                    .ToList();

		//        if ( results != null && results.Count > 0 )
		//        {
		//            foreach ( Codes_PropertyCategory item in results )
		//            {
		//                code = new CodeItem();
		//                code.Id = ( int )item.Id;
		//                code.Title = item.Title;
		//                code.Description = item.Description;
		//                code.URL = item.SchemaUrl;
		//                code.SchemaName = item.SchemaName;

		//                break;
		//            }
		//        }
		//    }
		//    return code;
		//}
		public static List<CodeItem> Property_GetValues( string categoryCodeName, bool insertSelectTitle, bool getAll = true )
		{
			using ( var context = new EntityContext() )
			{
				Codes_PropertyCategory category = context.Codes_PropertyCategory
							.FirstOrDefault( s => s.CodeName.ToLower() == categoryCodeName && s.IsActive == true );

				return Property_GetValues( category.Id, category.Title, insertSelectTitle, getAll );
			}

		}

		public static List<CodeItem> Property_GetValues( int categoryId, string categoryTitle, bool insertingSelectTitle = true, bool getAll = true )
		{
			return GetFromCacheOrDB( GetCacheName( "0157aac0-95e7-4ce6-8ba8-d2c50d6fc4e3", categoryId, categoryTitle, insertingSelectTitle, getAll ), () =>
			{
				List<CodeItem> list = new List<CodeItem>();
				CodeItem code;

				using ( var context = new EntityContext() )
				{
					List<Codes_PropertyValue> results = context.Codes_PropertyValue
						.Where( s => s.CategoryId == categoryId
								&& ( s.IsActive == true )
								&& ( s.Totals > 0 || getAll ) )
								.OrderBy( s => s.SortOrder ).ThenBy( s => s.Title )
								.ToList();

					if ( results != null && results.Count > 0 )
					{
						if ( insertingSelectTitle )
						{
							code = new CodeItem();
							code.Id = 0;
							code.Title = "Select " + categoryTitle;
							code.URL = string.Empty;
							list.Add( code );
						}
						foreach ( var item in results )
						{
							code = new CodeItem();
							code.Id = ( int ) item.Id;
							code.Title = item.Title;
							code.Description = item.Description;
							code.URL = item.SchemaUrl;
							code.SchemaName = item.SchemaName;
							code.ParentSchemaName = item.ParentSchemaName;
							code.Totals = item.Totals ?? 0;

							list.Add( code );
						}
					}
				}
				return list;
			} );
		}


		/// <summary>
		/// Check if the provided property schema is valid
		/// </summary>
		/// <param name="categorySchemaName"></param>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		public static bool IsPropertySchemaValid( string categorySchemaName, ref string schemaName )
		{
			CodeItem item = GetPropertyBySchema( categorySchemaName, schemaName );

			if ( item != null && item.Id > 0 )
			{
				//the lookup is case insensitive
				//return the actual schema name value
				schemaName = item.SchemaName;
				return true;
			}
			else
				return false;
		}

		public static bool IsPropertySchemaValid( string categorySchemaName, string schemaName, ref CodeItem item )
		{
			item = GetPropertyBySchema( categorySchemaName, schemaName );

			if ( item != null && item.Id > 0 )
			{
				return true;
			}
			else
				return false;
		}
		/// <summary>
		/// Get a single property using the category SchemaName, and property schema name
		/// </summary>
		/// <param name="categorySchemaName"></param>
		/// <param name="propertySchemaName"></param>
		/// <returns></returns>
		public static CodeItem GetPropertyBySchema( string categorySchemaName, string propertySchemaName )
		{
			return GetFromCacheOrDB( GetCacheName( "2cd2a58a-0f33-48b0-80a7-78b6534c2928", categorySchemaName, propertySchemaName ), () =>
			{
				CodeItem code = new CodeItem();

				using ( var context = new EntityContext() )
				{
					//for the most part, the code schema name should be unique. We may want a extra check on the categoryCode?
					//TODO - need to ensure the schemas are accurate - and not make sense to check here
					Codes_PropertyCategory category = context.Codes_PropertyCategory
								.FirstOrDefault( s => s.SchemaName.ToLower() == categorySchemaName.ToLower() && s.IsActive == true );

					Codes_PropertyValue item = context.Codes_PropertyValue
						.FirstOrDefault( s => s.SchemaName == propertySchemaName );
					if ( item != null && item.Id > 0 )
					{
						//could have an additional check that the returned category is correct - no guarentees though
						code = new CodeItem();
						code.Id = ( int ) item.Id;
						code.CategoryId = item.CategoryId;
						code.Title = item.Title;
						code.Description = item.Description;
						code.URL = item.SchemaUrl;
						code.SchemaName = item.SchemaName;
						code.ParentSchemaName = item.ParentSchemaName;
						code.Totals = item.Totals ?? 0;
					}
				}
				return code;
			} );
		}

		public static int GetPropertyIdBySchema( int categoryId, string propertySchemaName )
		{

			var code = GetPropertyBySchema( categoryId, propertySchemaName );
			if (code != null && code.Id > 0)
				return code.Id;
			else 
				return 0;
		}
		/// <summary>
		/// Get a single property using the category Id, and property schema name
		/// </summary>
		/// <param name="categoryId"></param>
		/// <param name="propertySchemaName"></param>
		/// <returns></returns>
		public static CodeItem GetPropertyBySchema( int categoryId, string propertySchemaName )
		{
			return GetFromCacheOrDB( GetCacheName( "6849484a-39cc-4159-a967-361ca4a6e4c3", categoryId, propertySchemaName ), () =>
			{
				CodeItem code = new CodeItem();
				using ( var context = new EntityContext() )
				{
					var item = context.Codes_PropertyValue
						.FirstOrDefault( s => s.CategoryId == categoryId
								&& ( s.IsActive == true )
								&& s.SchemaName.ToLower().Trim() == propertySchemaName.ToLower().Trim() );
					if ( item != null && item.Id > 0 )
					{
						//could have an additional check that the returned category is correct - no guarentees though
						code = new CodeItem
						{
							Id = ( int ) item.Id,
							CategoryId = item.CategoryId,
							Title = item.Title,
							Description = item.Description,
							URL = item.SchemaUrl,
							SchemaName = item.SchemaName,
							ParentSchemaName = item.ParentSchemaName,
							Totals = item.Totals ?? 0
						};
					}
				}
				return code;
			} );
		}

		/// <summary>
		/// Get a propertyValue by Schema.
		/// This is mostly safe, but the next method is better (think delivery type)
		/// </summary>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		public static CodeItem GetPropertyBySchema( string schemaName )
		{
			CodeItem code = new CodeItem();

			using ( var context = new EntityContext() )
			{

				Codes_PropertyValue item = context.Codes_PropertyValue
					.FirstOrDefault( s => ( s.IsActive == true )
							&& s.SchemaName.Trim() == schemaName.Trim() );
				if ( item != null && item.Id > 0 )
				{
					//could have an additional check that the returned category is correct - no guarentees though
					code = new CodeItem
					{
						Id = ( int ) item.Id,
						CategoryId = item.CategoryId,
						Title = item.Title,
						Description = item.Description,
						URL = item.SchemaUrl,
						SchemaName = item.SchemaName,
						ParentSchemaName = item.ParentSchemaName,
						Totals = item.Totals ?? 0
					};
					try
					{
						if ( item.Codes_PropertyCategory != null && item.Codes_PropertyCategory.Id > 0 )
						{
							code.Category = item.Codes_PropertyCategory.Title;
						}
					} catch(Exception ex)
					{

					}
				}
			}
			return code;
		}
		/// <summary>
		/// Get a propertyValue by CategoryId, and Schema.
		/// </summary>
		/// <param name="categoryId"></param>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		public static CodeItem Codes_PropertyValue_GetBySchema( int categoryId, string schemaName )
		{
			CodeItem code = new CodeItem();

			using ( var context = new EntityContext() )
			{
				List<Codes_PropertyValue> results = context.Codes_PropertyValue
					.Where( s => s.CategoryId == categoryId
							&& ( s.IsActive == true )
							&& ( s.SchemaName.ToLower() == schemaName.ToLower() )
							)
							.ToList();

				if ( results != null && results.Count > 0 )
				{
					foreach ( Codes_PropertyValue item in results )
					{
						code = new CodeItem();
						code.Id = ( int )item.Id;
						code.Title = item.Title;
						code.Description = item.Description;
						code.URL = item.SchemaUrl;
						code.SchemaName = item.SchemaName;
						code.ParentSchemaName = item.ParentSchemaName;
						code.Totals = item.Totals ?? 0;
						break;
					}
				}
			}
			return code;
		}

		/// <summary>
		/// Get a propertyValue by CategoryId, and Schema.
		/// </summary>
		/// <param name="categoryId"></param>
		/// <param name="schemaName"></param>
		/// <returns></returns>
		public static CodeItem GetSchemaById( int categoryId, int Id )
		{
			CodeItem code = new CodeItem();

			using ( var context = new EntityContext() )
			{
				List<Codes_PropertyValue> results = context.Codes_PropertyValue
					.Where( s => s.CategoryId == categoryId
							&& ( s.IsActive == true )
							&& ( s.Id == Id)
							)
							.ToList();

				if ( results != null && results.Count > 0 )
				{
					foreach ( Codes_PropertyValue item in results )
					{
						code = new CodeItem();
						code.Id = ( int ) item.Id;
						code.Title = item.Title;
						code.Description = item.Description;
						code.URL = item.SchemaUrl;
						code.SchemaName = item.SchemaName;
						code.ParentSchemaName = item.ParentSchemaName;
						code.Totals = item.Totals ?? 0;
						break;
					}
				}
			}
			return code;
		}

		public static CodeItem GetLifeCycleStatus( int categoryId, string schemaName )
		{
			return GetFromCacheOrDB( GetCacheName( "952eb575-9401-48e9-b725-3a78fee83ba1", categoryId, schemaName ), () =>
			{
				CodeItem code = new CodeItem();

				using ( var context = new EntityContext() )
				{
					List<Codes_PropertyValue> results = context.Codes_PropertyValue
						.Where( s => s.CategoryId == categoryId
								&& ( s.IsActive == true )
								&& ( s.SchemaName.ToLower() == schemaName.ToLower() )
								)
								.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( Codes_PropertyValue item in results )
						{
							code = new CodeItem();
							code.Id = ( int ) item.Id;
							code.Title = item.Title;
							code.Description = item.Description;
							code.URL = item.SchemaUrl;
							code.SchemaName = item.SchemaName;
							code.ParentSchemaName = item.ParentSchemaName;
							code.Totals = item.Totals ?? 0;
							break;
						}
					}
				}
				return code;
			} );
		}
		public static CodeItem GetLifeCycleStatus( int propertyId )
		{
			return GetFromCacheOrDB( GetCacheName( "5aef0982-2955-470a-aac8-d641ba5ae9dd", propertyId ), () =>
			{
				CodeItem code = new CodeItem();

				using ( var context = new EntityContext() )
				{
					List<Codes_PropertyValue> results = context.Codes_PropertyValue
						.Where( s => s.Id == propertyId )
								.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( Codes_PropertyValue item in results )
						{
							code = new CodeItem();
							code.Id = ( int ) item.Id;
							code.Title = item.Title;
							code.Description = item.Description;
							code.URL = item.SchemaUrl;
							code.SchemaName = item.SchemaName;
							code.ParentSchemaName = item.ParentSchemaName;
							code.Totals = item.Totals ?? 0;
							break;
						}
					}
				}
				return code;
			} );
		}
		public static CodeItem Codes_PropertyValue_Get( int propertyId )
		{
			CodeItem code = new CodeItem();

			using ( var context = new EntityContext() )
			{
				List<Codes_PropertyValue> results = context.Codes_PropertyValue
					.Where( s => s.Id == propertyId )
							.ToList();

				if ( results != null && results.Count > 0 )
				{
					foreach ( Codes_PropertyValue item in results )
					{
						code = new CodeItem();
						code.Id = ( int )item.Id;
						code.Title = item.Title;
						code.Description = item.Description;
						code.URL = item.SchemaUrl;
						code.SchemaName = item.SchemaName;
						code.ParentSchemaName = item.ParentSchemaName;
						code.Totals = item.Totals ?? 0;
						break;
					}
				}
			}
			return code;
		}
		#endregion

		public static CodeItem GetCredentialingActionType( int actionTypeId )
		{
			return GetFromCacheOrDB( GetCacheName( "06d44f8d-13c7-4b0e-a4d0-7d333426e4d8", actionTypeId ), () =>
			{
				CodeItem code = new CodeItem();

				using ( var context = new EntityContext() )
				{
					var item = context.Codes_CredentialingActionType
						.FirstOrDefault( s => s.Id == actionTypeId );

					if ( item != null && item.Id > 0 )
					{
						code = new CodeItem
						{
							Id = item.Id,
							Title = item.Name,
							Description = item.Description,
							URL = item.SchemaName,
							SchemaName = item.SchemaName,
							ParentSchemaName = "",
							Totals = item.Totals ?? 0
						};
					}
				}
				return code;
			} );
		}
		public static CodeItem GetCredentialingActionType( string term )
		{
			return GetFromCacheOrDB( GetCacheName( "5f0902c3-62b0-4ca3-af70-e0dff14fa785", term ), () =>
			{
				CodeItem code = new CodeItem();

				using ( var context = new EntityContext() )
				{
					var item = context.Codes_CredentialingActionType
						.FirstOrDefault( s => s.SchemaName == term );

					if ( item != null && item.Id > 0 )
					{
						code = new CodeItem
						{
							Id = item.Id,
							Title = item.Name,
							Description = item.Description,
							URL = item.SchemaName,
							SchemaName = item.SchemaName,
							ParentSchemaName = "",
							Totals = item.Totals ?? 0
						};
					}
				}
				return code;
			} );
		}

		#region country, locations, etc
		public static List<CodeItem> GetExistingCountries()
		{
			List<CodeItem> list = new List<CodeItem>();
			CodeItem entity = new CodeItem();
			using ( var context = new ViewContext() )
			{
				var results = context.ExistingCountries_list
					.OrderBy( s => s.Country ).ToList();

				if ( results != null && results.Count > 0 )
				{
					foreach ( var item in results )
					{
						entity = new CodeItem();
						entity.Id = item.CountryNumber;
						entity.Name = item.Country;

						list.Add( entity );
					}
				}
			}

			return list;
		}
		public static List<CodeItem> GetExistingRegionsForCountry( string country )
		{
			List<CodeItem> list = new List<CodeItem>();
			CodeItem entity = new CodeItem();
			using ( var context = new ViewContext() )
			{
				var results = context.ExistingCountryRegions_list
					.Where( s => s.Country.ToLower() == country.ToLower() )
					.OrderBy( s => s.Region ).ToList();

				if ( results != null && results.Count > 0 )
				{
					foreach ( var item in results )
					{
						entity = new CodeItem();
						entity.Id = item.CountryNumber;
						entity.Name = item.Region;

						list.Add( entity );
					}
				}
			}

			return list;
		}
		public static List<CodeItem> GetExistingCitiesForRegion( string country, string region )
		{
			List<CodeItem> list = new List<CodeItem>();
			CodeItem entity = new CodeItem();
			using ( var context = new ViewContext() )
			{
				var results = context.ExistingRegionCities_list
					.Where( s => s.Country.ToLower() == country.ToLower() && s.Region.ToLower() == region.ToLower() )
					.OrderBy( s => s.City ).ToList();

				if ( results != null && results.Count > 0 )
				{
					foreach ( var item in results )
					{
						entity = new CodeItem();
						//entity.Id = item.CountryNumber;
						entity.Name = item.City;

						list.Add( entity );
					}
				}
			}

			return list;
		}

		public static List<CodeItem> Codes_GetStates()
		{
			List<CodeItem> list = new List<CodeItem>();
			CodeItem code;

			using ( var context = new EntityContext() )
			{
				List<Codes_State> results = context.Codes_State
					.OrderBy( s => s.State ).ToList();

				if ( results != null && results.Count > 0 )
				{

					foreach ( var item in results )
					{
						code = new CodeItem();
						code.Id = ( int )item.Id;
						code.Title = item.State;
						code.Code = item.StateCode;

						list.Add( code );
					}
				}
			}
			return list;

		}

		public static bool Codes_IsState(string region, ref string fullRegion, ref int boostLevel)
		{
			List<CodeItem> list = new List<CodeItem>();
			CodeItem code;
			fullRegion = string.Empty;
			boostLevel = 20;
			using ( var context = new EntityContext() )
			{
				if ( region.Length == 2 )
				{
					//need to be careful this, but OK if there were only two letters.
					var results1 = context.Codes_State
							.Where( a => ( a.StateCode == region.ToUpper() ) )
							.OrderBy( s => s.State ).ToList();
					if ( results1 != null && results1.Count > 0 )
					{
						//can only be one
						foreach ( var item in results1 )
						{
							fullRegion = item.State;
							boostLevel = 60;
							break;
						}
						return true;
					}
					return false;
				}
				List<Codes_State> results = context.Codes_State
					.Where( a =>  a.State.ToLower() == region.ToLower() 
							|| a.State.ToLower().IndexOf( region.ToLower()) == 0 )
					.OrderBy( s => s.State ).ToList();

				if ( results != null && results.Count > 0 )
				{
					foreach ( var item in results )
					{
						if( item.State.ToLower() == region.ToLower())
						{
							fullRegion = item.State;
							boostLevel = 60;
							break;
						}
						
						//just take first one
						var lenDiff = item.State.Length - region.Length;
						if( region.Length > ( item.State.Length / 2 ) )
						{
							fullRegion = item.State;
							boostLevel = 40;
						}
					}
					return true;

				}
			}
			return false;

		}
		public static List<CodeItem> GetCountries_AsCodes()
		{
			List<CodeItem> list = new List<CodeItem>();
			CodeItem entity = new CodeItem();
			using ( var context = new ViewContext() )
			{
				List<Codes_Countries> results = context.Codes_Countries
					.Where( s => s.IsActive == true )
									.OrderBy( s => s.SortOrder ).ThenBy( s => s.CommonName )
									.ToList();

				if ( results != null && results.Count > 0 )
				{
					foreach ( Codes_Countries item in results )
					{
						entity = new CodeItem();
						entity.Id = item.Id;
						entity.Name = item.CommonName;

						list.Add( entity );
					}
				}
			}

			return list;
		}
		public static Enumeration GetAllCountries()
		{

			Enumeration entity = new Enumeration();
			EnumeratedItem val = new EnumeratedItem();

			using ( var context = new EntityContext() )
			{
				Codes_PropertyCategory category = context.Codes_PropertyCategory
				.FirstOrDefault( s => s.Id == PROPERTY_CATEGORY_CURRENCIES );

				entity.Id = category.Id;
				entity.Name = category.Title;
				entity.Description = category.Description;

				entity.SchemaName = category.SchemaName;
				entity.Url = category.SchemaUrl;
				entity.Items = new List<EnumeratedItem>();

				using ( var vcontext = new ViewContext() )
				{
					List<Codes_Countries> results = vcontext.Codes_Countries
					.Where( s => s.IsActive == true )
					.OrderBy( s => s.SortOrder )
					.ThenBy( s => s.CommonName )
					.ToList();

					if ( results != null && results.Count > 0 )
					{

						foreach ( Codes_Countries item in results )
						{
							val = new EnumeratedItem();
							//not sure if should use Id or countryNumber. The latter should be the published value. 
							//there are duplicate country numbers, all of which have set inactive for now
							val.Id = ( int )item.CountryNumber;
							val.CodeId = val.Id;
							val.Name = item.CommonName + " (" + item.CurrencyCode + ")";
							val.Description = item.CommonName + " (" + item.CurrencyCode + ")";
							val.SortOrder = item.SortOrder;
							val.Value = val.Id.ToString();

							entity.Items.Add( val );
						}
					}
				}
			}

			return entity;
		}

		public static Enumeration GetAddressRegionsAsEnumeration( int regionTypeId, string codeSuffix )
		{
			Enumeration entity = new Enumeration();
			using ( var context = new EntityContext() )
			{
				//entity.Id = regionTypeId;
				//entity.Name = "LWIA";
				//entity.Description = string.Empty;
				//entity.Items = new List<EnumeratedItem>();
				var regionType = context.Codes_ReqionType
						.FirstOrDefault( s => s.Id == regionTypeId && s.IsActive == true );
				if ( regionType?.Id > 0 )
				{
					entity = new Enumeration()
					{
						Id = regionType.Id,
						Name = regionType.Name,
						Description = regionType.Description

					};

					var results = context.Codes_ReqionTypeRegion
							.Where( s => s.RegionTypeId == regionTypeId && s.IsActive == true )
							.OrderBy( p => p.Id ) //this will be proper order until possible updates
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						EnumeratedItem val = new EnumeratedItem();
						foreach ( var item in results )
						{
							val = new EnumeratedItem();
							if (IsInteger( item.Value ) )
                            {
								val.Id = Int32.Parse( item.Value );
                            } else 
								val.Id = item.Id;
							val.Name = item.Name;
							//the current react filters don't seem to handle a string?
							//seems like they may, as there is a Text property.
							val.Value = item.Value;
							//possible option to suffix name with equiv of (LWIA 15)
							if ( !string.IsNullOrWhiteSpace( codeSuffix ) || IsDevEnv() )
								val.Name += string.Format( " ( {0} {1} )", codeSuffix, item.Value );

							entity.Items.Add( val );
						}
					}
				}
			}

			return entity;
		}
        #endregion
        #region pathway related
        /// <summary>
        /// Get pathway components
        /// </summary>
        /// <param name="getAll">If true get all types, otherwise just pathway components</param>
        /// <returns></returns>
        public static Enumeration PathwayComponentTypesAsEnumeration( bool includeComponentCondition = false )
        {
            Enumeration entity = new Enumeration();
            var code = new CodeItem();
            using ( var context = new EntityContext() )
            {
                Codes_PropertyCategory category = context.Codes_PropertyCategory
                            .FirstOrDefault( s => s.IsActive == true && ( s.Id == PROPERTY_CATEGORY_PATHWAY_COMPONENT_TYPE ) );

                if ( category != null && category.Id > 0 )
                {
                    entity.Id = category.Id;
                    entity.Name = category.Title;
                    entity.Description = category.Description;
                    entity.SchemaName = category.SchemaName;
                    entity.Url = category.SchemaUrl;
                    entity.Items = new List<EnumeratedItem>();
                    //
                    var results = context.Codes_PathwayComponentType
                                    .Where( s => s.IsActive == true
                                    && ( ( includeComponentCondition && s.ComponentClassTypeId == 2 ) || s.ComponentClassTypeId == 1 ) )
                                    .OrderBy( s => s.ComponentClassTypeId ).ThenBy( s => s.Id )
                                    .ToList();

                    if ( results != null && results.Count > 0 )
                    {
                        EnumeratedItem val = new EnumeratedItem();
                        foreach ( var item in results )
                        {
                            val = new EnumeratedItem();
                            val.Id = item.Id;
                            val.CodeId = item.Id;
                            val.Name = item.Title;
                            val.Description = item.Description != null ? item.Description : "";
                            val.SortOrder = item.Id;
                            val.SchemaName = item.SchemaName ?? string.Empty;
                            val.SchemaUrl = item.SchemaName ?? string.Empty;
                            val.Icon = item.ComponentIcon ?? string.Empty;
                            val.Value = item.Id.ToString();
                            val.Totals = item.Totals ?? 0;

                            //if ( IsDevEnv() )
                            //	val.Name += string.Format( " ({0})", val.Totals );
                            entity.Items.Add( val );
                        }
                    }

                }
            }

            return entity;
        }
      

        #endregion

        #region		Currencies
        public static Enumeration GetCurrencies()
		{

			Enumeration entity = new Enumeration();
			EnumeratedItem val = new EnumeratedItem();

			using ( var context = new EntityContext() )
			{
				Codes_PropertyCategory category = context.Codes_PropertyCategory
				.FirstOrDefault( s => s.Id == PROPERTY_CATEGORY_CURRENCIES );

				entity.Id = category.Id;
				entity.Name = category.Title;
				entity.Description = category.Description;

				entity.SchemaName = category.SchemaName;
				entity.Url = category.SchemaUrl;
				entity.Items = new List<EnumeratedItem>();

				using ( var vcontext = new ViewContext() )
				{
					List<Codes_Currency> results = vcontext.Codes_Currency
					.OrderBy( s => s.SortOrder )
					.ThenBy( s => s.Currency )
					.ToList();

					if ( results != null && results.Count > 0 )
					{

						foreach ( Codes_Currency item in results )
						{
							val = new EnumeratedItem
							{
								Id = ( int )item.NumericCode
							};
							val.CodeId = val.Id;
							val.Name = item.Currency + " (" + item.AlphabeticCode + ")";
							val.Description = item.Currency;
							val.SortOrder = item.SortOrder != null ? ( int )item.SortOrder : 0;
							val.Value = val.Id.ToString();
							val.SchemaName = item.AlphabeticCode; //Need this in publishing and other places - NA 3/17/2017

							entity.Items.Add( val );
						}
					}
				}
			}

			return entity;
		}

		public static CurrencyCode GetCurrencyItem( int numericCode )
		{
			var item = new CurrencyCode();
			using ( var context = new ViewContext() )
			{
				Codes_Currency currency = context.Codes_Currency.FirstOrDefault( s => s.NumericCode == numericCode );
				if ( currency != null && currency.NumericCode > 0 )
				{
					item = new CurrencyCode()
					{
						AlphabeticCode = currency.AlphabeticCode,
						Currency = currency.Currency,
						HtmlCodes = currency.HtmlCodes,
						NumericCode = currency.NumericCode,
						UnicodeDecimal = currency.UnicodeDecimal,
						UnicodeHex = currency.UnicodeHex
					};
					//return currency;
					return item;
				}

			}
			return item;
		}
		public static CurrencyCode GetCurrencyItem( string currencyCode )
		{
			var item = new CurrencyCode();
			using ( var context = new ViewContext() )
			{
				Codes_Currency currency = context.Codes_Currency.FirstOrDefault( s => s.AlphabeticCode == currencyCode || s.Currency == currencyCode );
				if ( currency != null && currency.NumericCode > 0 )
				{
					item = new CurrencyCode()
					{
						AlphabeticCode = currency.AlphabeticCode,
						Currency = currency.Currency,
						HtmlCodes = currency.HtmlCodes,
						NumericCode = currency.NumericCode,
						UnicodeDecimal = currency.UnicodeDecimal,
						UnicodeHex = currency.UnicodeHex
					};
					return item;
				}

			}
			return item;
		}

        #endregion
        #region Language Codes
        //public static Enumeration GetLanguages()
        //{

        //	Enumeration entity = new Enumeration();
        //	EnumeratedItem val = new EnumeratedItem();

        //	using ( var context = new EntityContext() )
        //	{
        //		Codes_PropertyCategory category = context.Codes_PropertyCategory
        //		.FirstOrDefault( s => s.Id == PROPERTY_CATEGORY_LANGUAGE );

        //		entity.Id = category.Id;
        //		entity.Name = category.Title;
        //		entity.Description = category.Description;

        //		entity.SchemaName = category.SchemaName;
        //		entity.Url = category.SchemaUrl;
        //		entity.Items = new List<EnumeratedItem>();

        //		using ( var vcontext = new ViewContext() )
        //		{
        //			List<Codes_Language> results = vcontext.Codes_Language
        //			.OrderBy( s => s.SortOrder )
        //			.ThenBy( s => s.LanguageName )
        //			.ToList();

        //			if ( results != null && results.Count > 0 )
        //			{
        //				foreach ( Codes_Language item in results )
        //				{
        //					val = new EnumeratedItem();
        //					val.Id = ( int )item.Id;
        //					val.Value = item.LanguageCode;
        //					val.Name = item.LanguageName;
        //					val.Description = item.LanguageName;
        //					val.SortOrder = item.SortOrder != null ? ( int )item.SortOrder : 0;
        //					entity.Items.Add( val );
        //				}
        //			}
        //		}
        //	}

        //	return entity;
        //}

        //public static EnumeratedItem GetLanguage( int languageId )
        //{
        //	EnumeratedItem val = new EnumeratedItem();

        //	using ( var context = new ViewContext() )
        //	{
        //		Codes_Language item = context.Codes_Language
        //		.FirstOrDefault( s => s.Id == languageId );
        //		if ( item != null && item.Id > 0 )
        //		{
        //			val.Id = item.Id;
        //			val.Value = item.LanguageCode;
        //			val.Name = item.LanguageName;
        //			val.Description = item.LanguageName;
        //			val.SortOrder = item.SortOrder != null ? ( int )item.SortOrder : 0;
        //			val.SchemaName = item.LanguageCode;
        //		}

        //	}

        //	return val;
        //}
        //public static int GetLanguageId( string language )
        //{
        //	int id = 0;
        //	EnumeratedItem item = GetLanguage( language );
        //	if ( item != null && item.Id > 0 )
        //		return item.Id;

        //	return id;
        //}
        public static EnumeratedItem GetLanguage( string language )
		{
			EnumeratedItem val = new EnumeratedItem();
			//may want to trim region
			if ( string.IsNullOrWhiteSpace( language ) )
			{
				return val;
			}
			if ( language.Trim().ToLower().StartsWith( "eng" ) )
			{
				language = "english";
			}
			//21-04-05 mp - TODO try to retain the orginal language code with region
			string altLanguage = string.Empty;
			if ( language.IndexOf( "-" ) > 1 )
			{
				altLanguage = language.Substring( 0, language.IndexOf( "-" ) );
			}
			using ( var context = new ViewContext() )
			{
				Codes_Language item = context.Codes_Language
				.FirstOrDefault( s => s.LanguageCode == language
						|| s.LanguageName == language
						|| ( altLanguage.Length > 0 && s.LanguageCode.StartsWith( altLanguage ) )
						);
				if ( item != null && item.Id > 0 )
				{
					val.Id = item.Id;
					val.Value = item.LanguageCode;
					val.Name = item.LanguageName;
					val.Description = item.LanguageName;
					val.SortOrder = item.SortOrder != null ? ( int )item.SortOrder : 0;
					val.SchemaName = item.LanguageCode; 
					if ( language.IndexOf( "-" ) > 1 && item.LanguageCode != language)
					{
						val.Value = language;
					}
				}
			}

			return val;
		}
		#endregion
		#region SOC - not used
		//may need to handle wild cards? Not yet. If suggested, then consider a job family option
		public static List<CodeItem> SOC_Get( string code )
		{
			string search = code;
			if ( code.IndexOf( "." ) == -1 )
			{
				//if exact is provided, with decimals, use it only, otherwise getall related
				search = code.Replace( "-", string.Empty );
				search = search.Substring( 0, search.IndexOf( "." ) );
			}
			var list = new List<CodeItem>();
			CodeItem item = new CodeItem();
			using ( var context = new ViewContext() )
			{
				var records = context.ONET_SOC
				.Where( s => s.OnetSocCode == search ).ToList();
				foreach ( var record in records )
				{
					if ( record != null && record.Id > 0 )
					{
						item.Id = record.Id;
						item.Name = record.Title;
						item.Description = record.Description;
						item.Code = record.OnetSocCode;
						item.URL = record.URL;
						list.Add( item );
					}
				}
			}
			return list;
		}
		public static CodeItem IsSOCCode( string code, string name )
		{
			string search = code;
			if ( code.IndexOf( "." ) == -1 )
			{
				//if exact is provided, with decimals, use it only, otherwise getall related
				search = code.Replace( "-", string.Empty );
				search = search.Substring( 0, search.IndexOf( "." ) );
			}
			CodeItem item = new CodeItem();
			using ( var context = new ViewContext() )
			{

				var record = context.ONET_SOC.FirstOrDefault( s => s.OnetSocCode == code );

				if ( record != null && record.Id > 0 )
				{
					item.Id = record.Id;
					item.Name = record.Title;
					item.Description = record.Description;
					item.CodeGroup = record.JobFamily.ToString();
					item.Code = item.SchemaName = record.OnetSocCode;
					item.URL = record.URL;
					return item;
				}
			}
			return item;
		}
		//public static List<CodeItem> SOC_Search( int headerId, string keyword, int pageNumber, int pageSize, ref int totalRows, bool getAll = true )
		//{
		//	List<CodeItem> list = new List<CodeItem>();
		//	CodeItem entity = new CodeItem();
		//	keyword = ( keyword ?? string.Empty ).Trim();
		//	if ( pageSize == 0 )
		//		pageSize = 100;
		//	int skip = 0;
		//	if ( pageNumber > 1 )
		//		skip = ( pageNumber - 1 ) * pageSize;
		//	string notKeyword = "Except " + keyword;

		//	using ( var context = new ViewContext() )
		//	{
		//		List<ONET_SOC> results = context.ONET_SOC
		//			.Where( s => ( headerId == 0 || s.OnetSocCode.Substring( 0, 2 ) == headerId.ToString() )
		//				&& ( keyword == string.Empty
		//				|| s.OnetSocCode.Contains( keyword )
		//				|| s.SOC_code.Contains( keyword )
		//				|| ( s.Title.Contains( keyword ) && s.Title.Contains( notKeyword ) == false )
		//				)
		//				&& ( s.Totals > 0 || getAll )
		//				)
		//			.OrderBy( s => s.Title )
		//			.Skip( skip )
		//			.Take( pageSize )
		//			.ToList();

		//		totalRows = context.ONET_SOC
		//			.Where( s => ( headerId == 0 || s.OnetSocCode.Substring( 0, 2 ) == headerId.ToString() )
		//				&& ( keyword == string.Empty
		//				|| s.OnetSocCode.Contains( keyword )
		//				|| s.SOC_code.Contains( keyword )
		//				|| s.Title.Contains( keyword ) )
		//				&& ( s.Totals > 0 || getAll )
		//				)
		//			.ToList().Count();

		//		if ( results != null && results.Count > 0 )
		//		{
		//			foreach ( ONET_SOC item in results )
		//			{
		//                      entity = new CodeItem
		//                      {
		//                          Id = item.Id,
		//                          Name = item.Title,// +" ( " + item.OnetSocCode + " )";
		//                          Description = item.Description,
		//                          URL = item.URL,
		//                          Code = item.OnetSocCode,
		//                          CodeGroup = item.JobFamily.ToString(),
		//                          Totals = item.Totals ?? 0
		//                      };

		//                      list.Add( entity );
		//			}
		//		}
		//	}

		//	return list;
		//}


		//public static List<CodeItem> SOC_Categories( string sortField = "Description", bool includeCategoryCode = false )
		//{
		//	List<CodeItem> list = new List<CodeItem>();
		//	CodeItem code;

		//	using ( var context = new ViewContext() )
		//	{
		//		var Query = from P in context.ONET_SOC_JobFamily
		//					select P;

		//		if ( sortField == "JobFamilyId" )
		//		{
		//			Query = Query.OrderBy( p => p.JobFamilyId );
		//		}
		//		else
		//		{
		//			Query = Query.OrderBy( p => p.Description );
		//		}
		//		var count = Query.Count();
		//		var results = Query.ToList();
		//		//List<ONET_SOC_JobFamily> results2 = context.ONET_SOC_JobFamily
		//		//	.OrderBy( s => s.Description )
		//		//	.ToList();

		//		if ( results != null && results.Count > 0 )
		//		{
		//			foreach ( ONET_SOC_JobFamily item in results )
		//			{
		//				code = new CodeItem();
		//				code.Id = item.JobFamilyId;
		//				if ( includeCategoryCode )
		//				{
		//					if ( sortField == "JobFamilyId" )
		//						code.Title = item.JobFamilyId + " - " + item.Description;
		//					else
		//						code.Title = item.Description + " (" + item.JobFamilyId + ")";
		//				}
		//				else
		//					code.Title = item.Description;
		//				code.Totals = ( int )( item.Totals ?? 0 );
		//				code.CategorySchema = "ctdl:SocGroup";
		//				list.Add( code );
		//			}
		//		}
		//	}
		//	return list;
		//}

		#endregion

		#region NAICS
		/// <summary>
		/// look up a NAICS code
		/// Note the NAICS points to ce_externalData.dbo.Naics table
		/// </summary>
		/// <param name="naicsCode"></param>
		/// <returns></returns>
		public static CodeItem Naics_Get( string naicsCode )
		{
			CodeItem item = new CodeItem();
			using ( var context = new ViewContext() )
			{
				Views.NAIC record = context.NAICS
					.FirstOrDefault( s => s.NaicsCode == naicsCode );

				if ( record != null && record.Id > 0 )
				{
					item.Id = record.Id;
					item.Name = record.NaicsTitle;
					item.Code = record.NaicsCode;
					item.URL = record.URL;
					return item;
				}

			}
			return item;
		}
		//public static List<CodeItem> NAICS_Search( int headerId = 0, string keyword = "", int pageNumber = 1, int pageSize = 0, bool getAll = true )
		//{
		//	int totalRows = 0;

		//	return NAICS_Search( headerId, keyword, pageNumber, pageSize, getAll, ref totalRows );
		//}
		//public static List<CodeItem> NAICS_Search( int headerId, string keyword, int pageNumber, int pageSize, bool getAll, ref int totalRows )
		//{
		//	List<CodeItem> list = new List<CodeItem>();
		//	CodeItem entity = new CodeItem();
		//	keyword = keyword.Trim();
		//	if ( pageSize == 0 )
		//		pageSize = 100;
		//	int skip = 0;
		//	if ( pageNumber > 1 )
		//		skip = ( pageNumber - 1 ) * pageSize;
		//	string notKeyword = "Except " + keyword;

		//	using ( var context = new ViewContext() )
		//	{
		//		List<NAIC> results = context.NAICS
		//				.Where( s => ( headerId == 0 || s.NaicsCode.Substring( 0, 2 ) == headerId.ToString() )
		//				&& ( keyword == string.Empty
		//				|| s.NaicsCode.Contains( keyword )
		//				|| s.NaicsTitle.Contains( keyword ) )
		//				&& ( s.Totals > 0 || getAll )
		//				)
		//			.OrderBy( s => s.NaicsTitle )
		//			.Skip( skip )
		//			.Take( pageSize )
		//			.ToList();
		//		totalRows = context.NAICS
		//				.Where( s => ( headerId == 0 || s.NaicsCode.Substring( 0, 2 ) == headerId.ToString() )
		//				&& ( keyword == string.Empty
		//				|| s.NaicsCode.Contains( keyword )
		//				|| s.NaicsTitle.Contains( keyword ) )
		//				&& ( s.Totals > 0 || getAll )
		//				)
		//			.ToList().Count();

		//		if ( results != null && results.Count > 0 )
		//		{
		//			foreach ( NAIC item in results )
		//			{
		//				entity = new CodeItem();
		//				entity.Id = item.Id;
		//				entity.Name = item.NaicsTitle;// + " ( " + item.NaicsCode + " )";
		//				entity.Description = string.Empty;// 						item.NaicsTitle + " ( " + item.NaicsCode + " )";
		//				entity.URL = item.URL;
		//				entity.Code = item.NaicsCode;
		//				entity.CodeGroup = item.NaicsGroup.ToString();
		//				entity.Totals = item.Totals ?? 0;

		//				list.Add( entity );
		//			}
		//		}
		//	}

		//	return list;
		//}
		//public static List<CodeItem> NAICS_SearchInUse( int entityTypeId, int headerId, string keyword, int pageNumber, int pageSize, ref int totalRows )
		//{
		//    List<CodeItem> list = new List<CodeItem>();
		//    CodeItem entity = new CodeItem();
		//    keyword = keyword.Trim();
		//    if ( pageSize == 0 )
		//        pageSize = 100;
		//    int skip = 0;
		//    if ( pageNumber > 1 )
		//        skip = ( pageNumber - 1 ) * pageSize;
		//    string notKeyword = "Except " + keyword;

		//    using ( var context = new ViewContext() )
		//    {
		//        List<Entity_FrameworkIndustryCodeSummary> results = context.Entity_FrameworkIndustryCodeSummary
		//                .Where( s => ( headerId == 0 || s.CodeGroup == headerId )
		//                && ( s.EntityTypeId == entityTypeId )
		//                && ( keyword == string.Empty
		//                || s.CodedNotation.Contains( keyword )
		//                || s.Name.Contains( keyword ) )
		//                && ( s.Totals > 0 )
		//                )
		//            .OrderBy( s => s.Name )
		//            .Skip( skip )
		//            .Take( pageSize )
		//            .ToList();
		//        totalRows = context.Entity_FrameworkIndustryCodeSummary
		//                .Where( s => ( headerId == 0 || s.CodeGroup == headerId )
		//                && ( s.EntityTypeId == entityTypeId )
		//                && ( keyword == string.Empty
		//                || s.CodedNotation.Contains( keyword )
		//                || s.Name.Contains( keyword ) )
		//                && ( s.Totals > 0 )
		//                )
		//            .ToList().Count();

		//        if ( results != null && results.Count > 0 )
		//        {
		//            foreach ( Views.Entity_FrameworkIndustryCodeSummary item in results )
		//            {
		//                entity = new CodeItem();
		//                entity.Id = ( int )item.Id;
		//                entity.Name = item.Name;// + " ( " + item.NaicsCode + " )";
		//                entity.Description = string.Empty;// 						item.NaicsTitle + " ( " + item.NaicsCode + " )";
		//                entity.URL = item.TargetNode;
		//                entity.SchemaName = item.CodedNotation;
		//                entity.Code = item.CodeGroup.ToString();
		//                entity.Totals = item.Totals ?? 0;

		//                list.Add( entity );
		//            }
		//        }
		//    }

		//    return list;
		//}
		//public static List<CodeItem> ReferenceFramework_SearchInUse( int categoryId, int entityTypeId, string headerId, string keyword, int pageNumber, int pageSize, ref int totalRows )
		//{
		//    List<CodeItem> list = new List<CodeItem>();
		//    CodeItem entity = new CodeItem();
		//    keyword = keyword.Trim();
		//    if ( headerId == "0" )
		//        headerId = string.Empty;

		//    if ( pageSize == 0 )
		//        pageSize = 100;
		//    int skip = 0;
		//    if ( pageNumber > 1 )
		//        skip = ( pageNumber - 1 ) * pageSize;
		//    string notKeyword = "Except " + keyword;

		//    using ( var context = new ViewContext() )
		//    {
		//        List<Entity_ReferenceFramework_Totals> results = context.Entity_ReferenceFramework_Totals
		//                .Where( s => ( headerId == string.Empty || s.CodeGroup == headerId )
		//                && ( s.CategoryId == categoryId )
		//                && ( s.EntityTypeId == entityTypeId )
		//                && ( keyword == string.Empty
		//                || s.CodedNotation.Contains( keyword )
		//                || s.Name.Contains( keyword ) )
		//                && ( s.Totals > 0 )
		//                )
		//            .OrderBy( s => s.Name )
		//            .Skip( skip )
		//            .Take( pageSize )
		//            .ToList();
		//        totalRows = context.Entity_ReferenceFramework_Totals
		//                .Where( s => ( headerId == string.Empty || s.CodeGroup == headerId )
		//                && ( s.EntityTypeId == entityTypeId )
		//                && ( keyword == string.Empty
		//                || s.CodedNotation.Contains( keyword )
		//                || s.Name.Contains( keyword ) )
		//                && ( s.Totals > 0 )
		//                )
		//            .ToList().Count();

		//        if ( results != null && results.Count > 0 )
		//        {
		//            foreach ( Views.Entity_ReferenceFramework_Totals item in results )
		//            {
		//                entity = new CodeItem();
		//                entity.Id = ( int )item.ReferenceFrameworkId;
		//                entity.Name = item.Name;
		//                entity.Description = string.Empty;
		//                entity.URL = item.TargetNode;
		//                entity.Code = item.CodedNotation;
		//                entity.CodeGroup = item.CodeGroup ?? string.Empty;
		//                entity.Totals = item.Totals ?? 0;

		//                list.Add( entity );
		//            }
		//        }
		//    }

		//    return list;
		//}
		//public static List<CodeItem> NAICS_Autocomplete( int headerId = 0, string keyword = "", int pageSize = 0 )
		//{
		//	List<CodeItem> list = new List<CodeItem>();
		//	CodeItem entity = new CodeItem();
		//	keyword = keyword.Trim();
		//	if ( pageSize == 0 )
		//		pageSize = 100;

		//	using ( var context = new ViewContext() )
		//	{
		//		List<NAIC> results = context.NAICS
		//				.Where( s => ( headerId == 0 || s.NaicsCode.Substring( 0, 2 ) == headerId.ToString() )
		//				&& ( keyword == string.Empty
		//				|| s.NaicsCode.Contains( keyword )
		//				|| s.NaicsTitle.Contains( keyword ) ) )
		//				.OrderBy( s => s.NaicsCode )
		//				.ToList();

		//		if ( results != null && results.Count > 0 )
		//		{
		//			foreach ( NAIC item in results )
		//			{
		//				entity = new CodeItem();
		//				entity.Id = item.Id;
		//				entity.Name = item.NaicsTitle;
		//				entity.Description = item.NaicsTitle + " ( " + item.NaicsCode + " )";
		//				entity.URL = item.URL;
		//				entity.Totals = item.Totals ?? 0;
		//				list.Add( entity );
		//			}
		//		}
		//	}

		//	return list;
		//}

		//public static List<CodeItem> NAICS_Categories( string sortField = "Description", bool includeCategoryCode = false )
		//{
		//    List<CodeItem> list = new List<CodeItem>();
		//    CodeItem entity;
		//    using ( var context = new ViewContext() )
		//    {
		//        //List<NAICS> results = context.NAICS
		//        //	.Where( s => s.NaicsCode.Length == 2 && s.NaicsGroup > 10)
		//        //	.OrderBy( s => s.NaicsCode )
		//        //	.ToList();
		//        var Query = from P in context.NAICS
		//                    .Where( s => s.NaicsCode.Length == 2 && s.NaicsGroup > 10 )
		//                    select P;

		//        if ( sortField == "NaicsGroup" )
		//        {
		//            Query = Query.OrderBy( p => p.NaicsGroup );
		//        }
		//        else
		//        {
		//            Query = Query.OrderBy( p => p.NaicsTitle );
		//        }
		//        var results = Query.ToList();
		//        if ( results != null && results.Count > 0 )
		//        {
		//            foreach ( NAIC item in results )
		//            {
		//                entity = new CodeItem();
		//                entity.Id = Int32.Parse( item.NaicsCode );

		//                if ( includeCategoryCode )
		//                {
		//                    if ( sortField == "NaicsGroup" )
		//                        entity.Title = item.NaicsCode + " - " + item.NaicsTitle;
		//                    else
		//                        entity.Title = item.NaicsTitle + " (" + item.NaicsCode + ")";
		//                }
		//                else
		//                    entity.Title = item.NaicsTitle;

		//                entity.URL = item.URL;
		//                entity.Totals = ( int )( item.Totals ?? 0 );
		//                entity.CategorySchema = "ctdl:NaicsGroup";

		//                list.Add( entity );
		//            }
		//        }
		//    }

		//    return list;
		//}
		//public static List<CodeItem> NAICS_CategoriesInUse( int entityTypeId )
		//{
		//    List<CodeItem> list = new List<CodeItem>();
		//    //CodeItem code;
		//    //, string sortField = "Description"
		//    //using ( var context = new ViewContext() )
		//    //{

		//    //	List<Entity_FrameworkIndustryGroupSummary> results = context.Entity_FrameworkIndustryGroupSummary
		//    //				.Where( s => s.EntityTypeId == entityTypeId )
		//    //				.OrderBy( x => x.FrameworkGroupTitle )
		//    //				.ToList();

		//    //	if ( results != null && results.Count > 0 )
		//    //	{
		//    //		foreach ( Views.Entity_FrameworkIndustryGroupSummary item in results )
		//    //		{
		//    //			code = new CodeItem();
		//    //			code.Id = ( int ) item.CodeGroup;
		//    //			code.Title = item.FrameworkGroupTitle;
		//    //			code.Totals = ( int ) ( item.groupCount ?? 0 );
		//    //			code.CategorySchema = "ctdl:IndustryGroup";
		//    //			list.Add( code );
		//    //		}
		//    //	}
		//    //}
		//    return list;
		//}
		#endregion

		#region CIPS
		public static CodeItem CIP_Get( string code )
		{
			string search = code;
			//for now, just exact

			CodeItem item = new CodeItem();
			using ( var context = new ViewContext() )
			{
				var record = context.CIPCode2010
					.FirstOrDefault( s => s.CIPCode == search );
				
				if ( record != null && record.Id > 0 )
				{
					item.Id = record.Id;
					item.Name = record.CIPTitle;
					item.Description = record.CIPDefinition;
					item.Code = record.CIPCode;
					item.URL = record.Url;
				}
			
			}
			return item;
		}

		//public static List<CodeItem> CIPS_Search( int headerId, string keyword, int pageNumber, int pageSize, ref int totalRows, bool getAll = true )
		//      {
		//          List<CodeItem> list = new List<CodeItem>();
		//          CodeItem entity = new CodeItem();
		//          string header = headerId.ToString();
		//          if ( headerId > 0 && headerId < 10 )
		//              header = "0" + header;
		//          keyword = keyword.Trim();
		//          if ( pageSize == 0 )
		//              pageSize = 100;
		//          int skip = 0;
		//          if ( pageNumber > 1 )
		//              skip = ( pageNumber - 1 ) * pageSize;

		//          using ( var context = new ViewContext() )
		//          {
		//              List<CIPCode2010> results = context.CIPCode2010
		//                      .Where( s => ( headerId == 0 || s.CIPCode.Substring( 0, 2 ) == header )
		//                      && ( keyword == string.Empty
		//                      || s.CIPCode.Contains( keyword )
		//                      || s.CIPTitle.Contains( keyword )
		//                      )
		//                      && ( s.Totals > 0 || getAll )
		//                      )
		//                  .OrderBy( s => s.CIPTitle )
		//                  .Skip( skip )
		//                  .Take( pageSize )
		//                  .ToList();

		//              totalRows = context.CIPCode2010
		//                      .Where( s => ( headerId == 0 || s.CIPCode.Substring( 0, 2 ) == header )
		//                      && ( keyword == string.Empty
		//                      || s.CIPCode.Contains( keyword )
		//                      || s.CIPTitle.Contains( keyword ) )
		//                      && ( s.Totals > 0 || getAll )
		//                      )
		//                  .ToList().Count();

		//              if ( results != null && results.Count > 0 )
		//              {
		//                  foreach ( CIPCode2010 item in results )
		//                  {
		//                      entity = new CodeItem();
		//                      entity.Id = item.Id;
		//                      entity.Name = item.CIPTitle + " ( " + item.CIPCode + " )";
		//                      entity.Description = item.CIPDefinition;
		//                      //entity.URL = item.URL;
		//                      entity.Code = item.CIPCode;
		//                      entity.CodeGroup = item.CIPFamily;
		//                      entity.Totals = item.Totals ?? 0;
		//                      list.Add( entity );
		//                  }
		//              }
		//          }

		//          return list;
		//      }
		//public static List<CodeItem> CIPS_SearchInUse( int entityTypeId, int headerId, string keyword, int pageNumber, int pageSize, ref int totalRows )
		//{
		//    List<CodeItem> list = new List<CodeItem>();
		//    CodeItem entity = new CodeItem();
		//    string header = headerId.ToString();
		//    if ( headerId > 0 && headerId < 10 )
		//        header = "0" + header;
		//    keyword = keyword.Trim();
		//    if ( pageSize == 0 )
		//        pageSize = 100;
		//    int skip = 0;
		//    if ( pageNumber > 1 )
		//        skip = ( pageNumber - 1 ) * pageSize;

		//    using ( var context = new ViewContext() )
		//    {
		//        List<Entity_FrameworkCIPCodeSummary> results = context.Entity_FrameworkCIPCodeSummary
		//                .Where( s => ( headerId == 0 || s.CodeGroup == header )
		//                && ( s.EntityTypeId == entityTypeId )
		//                && ( keyword == string.Empty
		//                || s.CIPCode.Contains( keyword )
		//                || s.CIPTitle.Contains( keyword )
		//                )
		//                && ( s.Totals > 0 )
		//                )
		//            .OrderBy( s => s.CIPTitle )
		//            .Skip( skip )
		//            .Take( pageSize )
		//            .ToList();

		//        totalRows = context.Entity_FrameworkCIPCodeSummary
		//                .Where( s => ( headerId == 0 || s.CodeGroup == header )
		//                && ( s.EntityTypeId == entityTypeId )
		//                && ( keyword == string.Empty
		//                || s.CIPCode.Contains( keyword )
		//                || s.CIPTitle.Contains( keyword )
		//                )
		//                && ( s.Totals > 0 )
		//                )
		//            .ToList().Count();

		//        if ( results != null && results.Count > 0 )
		//        {
		//            foreach ( Views.Entity_FrameworkCIPCodeSummary item in results )
		//            {
		//                entity = new CodeItem();
		//                entity.Id = ( int )item.Id;
		//                entity.Name = item.CIPTitle + " ( " + item.CIPCode + " )";
		//                //entity.Description = item.CIPDefinition;
		//                entity.URL = item.URL;
		//                entity.Code = item.CIPCode;
		//                entity.CodeGroup = item.CodeGroup;
		//                entity.Totals = item.Totals ?? 0;
		//                list.Add( entity );
		//            }
		//        }
		//    }

		//    return list;
		//}

		//public static List<CodeItem> CIPS_Autocomplete( int headerId = 0, string keyword = "", int pageSize = 0 )
		//{
		//    List<CodeItem> list = new List<CodeItem>();
		//    CodeItem entity = new CodeItem();
		//    keyword = keyword.Trim();
		//    if ( pageSize == 0 )
		//        pageSize = 100;

		//    using ( var context = new ViewContext() )
		//    {
		//        List<CIPCode2010> results = context.CIPCode2010
		//                .Where( s => ( headerId == 0 || s.CIPFamily == headerId.ToString() )
		//                && ( keyword == string.Empty
		//                || s.CIPTitle.Contains( keyword )
		//                || s.CIPDefinition.Contains( keyword ) ) )
		//                .OrderBy( s => s.CIPCode )
		//                .ToList();

		//        if ( results != null && results.Count > 0 )
		//        {
		//            foreach ( CIPCode2010 item in results )
		//            {
		//                entity = new CodeItem();
		//                entity.Id = item.Id;
		//                entity.Name = item.CIPTitle;
		//                entity.Description = item.CIPTitle + " ( " + item.CIPCode + " )";
		//                //entity.URL = item.URL;
		//                entity.Totals = item.Totals ?? 0;
		//                list.Add( entity );
		//            }
		//        }
		//    }

		//    return list;
		//}

		//public static List<CodeItem> CIPS_Categories( string sortField = "CIPFamily" )
		//{
		//    List<CodeItem> list = new List<CodeItem>();
		//    CodeItem entity;
		//    using ( var context = new ViewContext() )
		//    {
		//        //List<CIPS> results = context.CIPS
		//        //	.Where( s => s.NaicsCode.Length == 2 && s.NaicsGroup > 10)
		//        //	.OrderBy( s => s.NaicsCode )
		//        //	.ToList();
		//        var Query = from P in context.CIPCode2010
		//                    .Where( s => s.CIPCode.Length == 2 )
		//                    select P;

		//        if ( sortField == "CIPFamily" )
		//        {
		//            Query = Query.OrderBy( p => p.CIPFamily );
		//        }
		//        else
		//        {
		//            Query = Query.OrderBy( p => p.CIPTitle );
		//        }
		//        var results = Query.ToList();
		//        if ( results != null && results.Count > 0 )
		//        {
		//            foreach ( CIPCode2010 item in results )
		//            {
		//                entity = new CodeItem();
		//                entity.Id = Int32.Parse( item.CIPFamily );
		//                if ( sortField == "CIPFamily" )
		//                    entity.Title = item.CIPCode + " - " + item.CIPTitle;
		//                else
		//                    entity.Title = item.CIPTitle + " (" + item.CIPCode + ")";
		//                //entity.URL = item.URL;

		//                entity.Totals = ( int )( item.Totals ?? 0 );
		//                entity.CategorySchema = "ctdl:CipsGroup";
		//                list.Add( entity );
		//            }
		//        }
		//    }

		//    return list;
		//}

		//public static List<CodeItem> CIPS_CategoriesInUse( int entityTypeId, string sortField = "codeId" )
		//{
		//    List<CodeItem> list = new List<CodeItem>();
		//CodeItem code;

		//using ( var context = new ViewContext() )
		//{
		//	var Query = from P in Entity_FrameworkCIPGroupSummary
		//				.Where( a => a.EntityTypeId == entityTypeId )
		//				select P;

		//	if ( sortField == "codeId" )
		//	{
		//		Query = Query.OrderBy( p => p.CodeGroup );
		//	}
		//	else
		//	{
		//		Query = Query.OrderBy( p => p.FrameworkGroupTitle );
		//	}
		//	var count = Query.Count();
		//	var results = Query.ToList();

		//	if ( results != null && results.Count > 0 )
		//	{
		//		foreach ( Views.Entity_FrameworkCIPGroupSummary item in results )
		//		{
		//			code = new CodeItem();
		//			//???
		//			code.Id = Int32.Parse( item.CodeGroup );
		//			code.Code = item.CodeGroup;
		//			code.Title = item.FrameworkGroupTitle;
		//			code.Totals = ( int ) ( item.groupCount ?? 0 );
		//			code.CategorySchema = "ctdl:CIP";
		//			list.Add( code );
		//		}
		//	}
		//}
		//    return list;
		//}
		#endregion

		#region Reporting 

		/// <summary>
		/// Get Properties Summary with totals
		/// </summary>
		/// <param name="categoryId">If zero, will return all</param>
		/// <returns></returns>
		public static List<CodeItem> Property_GetSummaryTotals( int categoryId = 0 )
		{
			List<CodeItem> list = new List<CodeItem>();
			CodeItem code;

			using ( var context = new ViewContext() )
			{
				List<CodesProperty_Summary> results = context.CodesProperty_Summary
					.Where( s => s.CategoryId == categoryId || categoryId == 0 )
							.OrderBy( s => s.Category )
							.ThenBy( s => s.SortOrder )
							.ThenBy( s => s.Property )
							.ToList();

				if ( results != null && results.Count > 0 )
				{

					foreach ( CodesProperty_Summary item in results )
					{
						code = new CodeItem
						{
							Id = ( int )item.PropertyId,
							Title = item.Property,
							SchemaName = item.PropertySchemaName,
							//note this is used as a hack on some properties
							ParentSchemaName = item.ParentSchemaName,
							URL = item.PropertySchemaUrl
						};
						if ( item.CategoryId == 6 )
						{

						}
						code.Description = item.PropertyDescription;
						code.CategoryId = item.CategoryId;
						code.Category = item.Category;
						code.CategorySchema = item.CategorySchemaName;
						code.Totals = item.Totals;

						list.Add( code );
					}
				}
			}
			return list;
		}
		#region Competency Frameworks
		//public static List<CodeItem> CompetencyFrameworks_GetAll()
		//{
		//	List<CodeItem> list = new List<CodeItem>();
		//	CodeItem code;

		//	using ( var context = new EntityContext() )
		//	{
		//		List<Data.CompetencyFramework> results = context.CompetencyFramework
		//					.OrderBy( s => s.Name )
		//					.ToList();

		//		if ( results != null && results.Count > 0 )
		//		{
		//			foreach ( CompetencyFramework item in results )
		//			{
		//				code = new CodeItem();
		//				code.Id = item.Id;
		//				code.Title = item.Name;
		//				code.URL = item.Url;

		//				list.Add( code );
		//			}
		//		}
		//	}
		//	return list;
		//}
		#endregion



		public void UpdateEntityTypes( int id, int total, bool allowingZero = true )
		{
			try
			{
				using ( var context = new EntityContext() )
				{
					var efEntity = context.Codes_EntityTypes.SingleOrDefault( s => s.Id == id );
					if ( efEntity != null && efEntity.Id > 0 )
					{
						if ( total > 0 || allowingZero )
						{
							if(efEntity.Id == 10 && (total - efEntity.Totals == 3))
							{
								//what - could just send email, and admin will have to check if legit. If not, then running from menu will either set to correct (or stay the same), or be rejected here if valid!!!!
								//could do a compare from database?
								//24-04-05 mp - don't recall the original issue that prompted this check. 
								//				- disabling for now
								if ( UtilityManager.GetAppKeyValue( "environment" ) == "production" )
								{
									//var oldFinder = UtilityManager.GetAppKeyValue( "oldCredentialFinderSite" );
									//EmailManager.NotifyAdmin( "<p>Competency Framework count +3 warning", string.Format( "Competency Framework count updated by 3. Please validate. Old count: {0}, new count: {1}. </p><p><a href='{2}Admin/Site/UpdateCompetencyFrameworkTotals'>Update count from menu</a></p>", efEntity.Totals, total, oldFinder) );
									//return;
								}
							}
							efEntity.Totals = total;
						}

						if ( HasStateChanged( context ) )
						{

							int count = context.SaveChanges();
							if ( count >= 0 )
							{

							}
							else
							{

							}
						}
					}
				}
			}

			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, string.Format( "CodesManager.UpdateEntityTypes id: {0}", id ) );
			}
		}

		public static List<CodeItem> Property_GetTotalsByEntity( int categoryId = 0 )
		{
			List<CodeItem> list = new List<CodeItem>();
			CodeItem code;

			using ( var context = new ViewContext() )
			{
				List<Views.CodesProperty_Counts_ByEntity> results = context.CodesProperty_Counts_ByEntity
					.Where( s => s.CategoryId == categoryId || categoryId == 0 )
							.OrderBy( s => s.Entity )
							.ThenBy( s => s.Category )
							.ThenBy( s => s.SortOrder )
							.ThenBy( s => s.Property )
							.ToList();
				//
				if ( results != null && results.Count > 0 )
				{

					foreach ( Views.CodesProperty_Counts_ByEntity item in results )
					{
						code = new CodeItem();
						code.EntityType = item.Entity;
						code.EntityTypeId = item.EntityTypeId;
						code.Id = ( int )item.PropertyId;
						code.Title = item.Property;
						code.SchemaName = item.SchemaName;
						//note this is used as a hack on some properties
						code.ParentSchemaName = item.CategorySchema;
						//code.URL = item.PropertySchemaUrl;
						if (item.CategoryId == 84)
						{

						}
						code.Description = item.Description;
						code.CategoryId = item.CategoryId;
						code.Category = item.Category;
						code.CategorySchema = item.CategorySchema;
						code.Totals = ( int )item.EntityPropertyCount;

						list.Add( code );
					}
				}
			}
			return list;
		}

		public static List<HistoryTotal> GetHistoryTotal( int entityTypeId )
		{
			var list = new List<HistoryTotal>();
			var record = new HistoryTotal();
			using ( var context = new EntityContext() )
			{
				var history = context.Counts_EntityMonthlyTotals
					.Where( x => x.EntityTypeId == entityTypeId )
					.OrderByDescending( m => m.Period )
					.ToList();

				foreach ( var item in history )
				{
					record = new HistoryTotal();
					record.Period = item.Period;
					record.CreatedCount = item.CreatedTotal;
					record.UpdatedCount = item.UpdatedTotal;
					record.DeletedCount = item.DeletedTotal;
					record.EntityTypeId = item.EntityTypeId;
					list.Add( record );
				}
			}

			return list;
		}
		/// <summary>
		/// Get Entity Codes with totals for Credential, Organization, assessments, and learning opp
		/// </summary>
		/// <returns></returns>
		public static List<CodeItem> CodeEntity_GetTopLevelEntity( bool gettingAll = true, string onlyTheseEntities = "", string excludeTheseEntities = "" )
        {
            List<CodeItem> list = new List<CodeItem>();
			CodeItem code;
			List<int> includeList = new List<int>();
			if(!string.IsNullOrWhiteSpace(onlyTheseEntities))
            {
				includeList = onlyTheseEntities.Split( ',' ).Select( x => int.Parse( x.Trim() ) ).ToList();
			}
			List<int> excludeList = new List<int>();
			if ( !string.IsNullOrWhiteSpace( excludeTheseEntities ) )
			{
				excludeList = excludeTheseEntities.Split( ',' ).Select( x => int.Parse( x.Trim() ) ).ToList();
			}
			using (var context = new EntityContext() )
            {
				//21-01-05 mparsons - need to future proof this so that don't have to update every time a new top level entity is identified. 
				//					- currently the calling method will select out what is needed, so could return almost everything?  
				//22-05-16 mp	- added IsTopLevelEntity check
				var query = context.Codes_EntityTypes
					.Where( s => s.IsTopLevelEntity == true );
							//.OrderBy( s => s.SortOrder )
							//.ThenBy ( s => s.Title )
							//.ToList();
				if ( includeList.Any() )
				{
					query = query.Where( t2 => includeList.Contains(t2.Id) );
				}

				if ( excludeList != null &&  excludeList.Any() )
				{
					query = query.Where( t2 => !excludeList.Contains( t2.Id ) );
				}

				var results = query.OrderBy( s => s.SortOrder ).ThenBy( s => s.Title )
									.ToList();
				if (results != null && results.Count > 0 )
                {
                    foreach (var item in results )
                    {
                        //if ( excludeList?.Count > 0 && excludeList.Contains( item.Id ))
                        //	continue;
                        code = new CodeItem
                        {
                            Id = item.Id,
                            Title = item.Title,
                            //description should contain the display friendly label
							//	Now use label
                            Description = item.Label,
                            Totals = item.Totals ?? 0
                        };

                        if (code.Totals > 0 || gettingAll)
							list.Add( code );
                    }
                }
                //add QA orgs, and others
                //N/A for finder
                //code = new CodeItem();
                //code.Id = 99;
                //code.Title = "QA Organization";
                ////code.Totals = OrganizationManager.QAOrgCounts();
                //list.Add( code );

            }
            return list;
        }
		public static Enumeration GetLearningObjectTypesEnumeration( bool getAll = true )
		{
			return GetFromCacheOrDB( GetCacheName( "3383a79a-f26f-4bae-9f97-2a69b66b0523", getAll ), () =>
			{
				Enumeration entity = new Enumeration();
				entity.Id = 3; //why 3 - changed this in PropertyCategory
				entity.Name = "Learning Types";
				entity.Description = "Different types of learning opportunities.";

				entity.SchemaName = string.Empty;
				entity.Url = string.Empty;
				entity.Items = new List<EnumeratedItem>();

				using ( var context = new EntityContext() )
				{
					var results = context.Codes_EntityTypes
								.Where( s => s.IsActive == true && ( s.Id == 7 || s.Id == 36 || s.Id == 37 ) )
								.OrderBy( a => a.Id )
								.ToList();
					if ( results != null && results.Count > 0 )
					{
						EnumeratedItem val = new EnumeratedItem();
						foreach ( var item in results )
						{
							val = new EnumeratedItem();
							val.CategoryId = 3;
							val.Id = item.Id;
							val.CodeId = item.Id;
							val.ParentId = 0;
							val.Name = item.Title;
							val.Description = item.Description != null ? item.Description : "";
							val.SortOrder = item.Id;
							val.SchemaName = item.SchemaName ?? string.Empty;
							val.SchemaUrl = item.SchemaName;
							val.Value = item.Id.ToString();
							val.Totals = item.Totals ?? 0;
							if ( IsDevEnv() )
								val.Name += string.Format( " ({0})", val.Totals );
							if ( getAll || item.Totals > 0 )
								entity.Items.Add( val );
						}
					}
				}

				return entity;
			} );
		}

		public static Enumeration GetOrgSubclasses( bool getAll = true )
		{
			return GetFromCacheOrDB( GetCacheName( "05298c46-96c5-4fa4-9966-8d43d1d54d20", getAll ), () =>
			{
				Enumeration entity = new Enumeration();
				entity.Id = 1; //why 1 - changed this in PropertyCategory
				entity.Name = "Organization Classes";
				entity.Description = "Different organization classes.";

				entity.SchemaName = string.Empty;
				entity.Url = string.Empty;
				entity.Items = new List<EnumeratedItem>();
				using ( var context = new EntityContext() )
				{
					var results = context.Codes_EntityTypes
								.Where( s => s.IsActive == true
									&& ( s.Id == 2 || s.Id == 13 || s.Id == 14 )
								//&& (getAll || s.Totals > 0)
								)
								.OrderBy( a => a.Id )
								.ToList();
					if ( results != null && results.Count > 0 )
					{
						EnumeratedItem val = new EnumeratedItem();
						foreach ( var item in results )
						{
							val = new EnumeratedItem();
							val.CategoryId = 1;
							val.Id = item.Id;
							val.CodeId = item.Id;
							val.ParentId = 0;
							val.Name = item.Title;
							val.Description = item.Description != null ? item.Description : "";
							val.SortOrder = item.Id;
							val.SchemaName = item.SchemaName ?? string.Empty;
							val.SchemaUrl = item.SchemaName;
							val.Value = item.Id.ToString();
							val.Totals = item.Totals ?? 0;
							if ( IsDevEnv() )
								val.Name += string.Format( " ({0})", val.Totals );
							if ( getAll || item.Totals > 0 )
								entity.Items.Add( val );
						}
					}
				}

				return entity;
			} );
		}

		public static List<CodeItem> CodeEntity_GetCountsSiteTotals()
        {
            List<CodeItem> list = new List<CodeItem>();
			CodeItem code;

			using ( var context = new EntityContext() )
			{
				List<Counts_SiteTotals> results = context.Counts_SiteTotals
							.OrderBy( s => s.CategoryId )
							.ThenBy( x => x.EntityTypeId )
							.ThenBy( y => y.CodeId )
							.ToList();

				if ( results != null && results.Count > 0 )
				{

					foreach ( Counts_SiteTotals item in results )
					{
						code = new CodeItem();
						code.Id = item.Id;
						code.CategoryId = item.CategoryId;
						//?? - need entity type for filtering
						code.EntityTypeId = item.EntityTypeId;
						code.EntityType = item.EntityTypeId.ToString();

						code.CodeGroup = item.CodeId.ToString();
						code.Title = item.Title;
						code.Totals = ( int ) item.Totals;

						list.Add( code );
					}
				}


			}
			return list;
        }

		public static List<CodeItem> GetEntityRegionTotals( int entityTypeId, string country = "")
		{
			List<CodeItem> list = new List<CodeItem>();
			CodeItem code;

			using ( var context = new EntityContext() )
			{
				var results = context.Counts_RegionTotals
					.Where( a => a.EntityTypeId == entityTypeId 
						&& (country == string.Empty || a.Country == country))
							.OrderBy( s => s.Country )
							.ThenBy( x => x.Region )
							.ToList();

				if ( results != null && results.Count > 0 )
				{

					foreach ( var item in results )
					{
						code = new CodeItem();
						code.Id = item.Id;
						code.CategoryId = PROPERTY_CATEGORY_US_REGIONS;
						//?? - need entity type for filtering
						code.EntityTypeId = item.EntityTypeId;
						code.EntityType = item.EntityTypeId.ToString();

						code.CodeGroup = item.Country;
						code.Title = item.Region;
						code.Totals = ( int )item.Totals;

						list.Add( code );
					}
				}


			}
			return list;
		}

		public static CodeItem GetEntityRegionTotal( int entityTypeId, int recordId )
		{
			CodeItem code = new CodeItem();

			using ( var context = new EntityContext() )
			{
				var results = context.Counts_RegionTotals
					.Where( a => a.EntityTypeId == entityTypeId && a.Id == recordId )
							.ToList();

				if ( results != null && results.Count > 0 )
				{

					foreach ( var item in results )
					{
						code = new CodeItem();
						code.Id = item.Id;
						code.CategoryId = PROPERTY_CATEGORY_US_REGIONS;
											 //?? - need entity type for filtering
						code.EntityTypeId = item.EntityTypeId;
						code.EntityType = item.EntityTypeId.ToString();

						code.CodeGroup = item.Country;
						code.Title = item.Region;
						code.Totals = ( int )item.Totals;
					}
				}
			}
			return code;
		}

		#endregion


		/// <summary>
		/// Attempt to get a value from the cache, or if not found, get it from the database and store it in the cache for future use.<br />
		/// It is critical that the cacheID be globally unique not just to a given method, but also to a particular set of arguments passed to that method (e.g. "SomeMethod(1,2,3)" and "SomeMethod(4,5,6)" should have different cache keys).<br />
		/// Don't forget to account for otherwise-identical methods in different namespaces when determining the cache key!
		/// </summary>
		/// <typeparam name="T">The type of value to get.</typeparam>
		/// <param name="cacheID">The key for the value. Must be globally unique to a given method and set of argument values (method signature + parameter values passed to the method) in order to ensure the right value is stored/retrieved from the cache.</param>
		/// <param name="GetFromDBMethod">Method to get a value from the database. Will only be called if the value is not found in the cache.</param>
		/// <param name="enabled">Indicates whether to enable caching for this call. Intended for debugging slow caching attempts.</param>
		/// <param name="cacheLifeMinutes">How long the cache for this value should live, in minutes.</param>
		/// <returns></returns>
		public static T GetFromCacheOrDB<T>( string cacheID, Func<T> GetFromDBMethod, bool enabled = true, int cacheLifeMinutes = 0 )
		{
			if ( !enabled )
			{
				return GetFromDBMethod();
			}
			if ( cacheLifeMinutes == 0 )
				cacheLifeMinutes = UtilityManager.GetAppKeyValue( "enumCacheLifetimeMinutes", 60 );

			try
			{
				if ( MemoryCache.Default.Contains( cacheID ) )
				{
					//By serializing/deserializing the object, we ensure that a brand new object is being returned, in order to avoid issues with the original object being referenced/altered unintentionally afterwards
					return JsonConvert.DeserializeObject<T>( ( string ) MemoryCache.Default.Get( cacheID ) );
				}
				else
				{
					var result = GetFromDBMethod();
					var serialized = JsonConvert.SerializeObject( result, Formatting.None );
					MemoryCache.Default.Remove( cacheID );
					MemoryCache.Default.Add( cacheID, serialized, new DateTimeOffset( DateTime.Now.AddMinutes( cacheLifeMinutes ) ) );
					return result;
				}
			}
			catch ( Exception ex )
			{
				return GetFromDBMethod();
			}
		}
		//

		//public static string GetCacheName( MethodBase methodBase, params object[] parameters )
		//{
		//	//For some reason getting the MethodInfo is the only way to get the ReturnType
		//	var methodInfo = ( MethodInfo ) methodBase;
		//	//ReturnType + containing class + method name +
		//	return methodInfo.ReturnType.Name + " " + methodInfo.DeclaringType.FullName + "." + methodInfo.Name + ": " +
		//		//Argument type list +
		//		//string.Join( ", ", methodInfo.GetParameters().Select( m => m.ParameterType.Name ) ) + 
		//		//Actual values for the parameters
		//		" ( " + string.Join( ", ", parameters.Select( m => m?.ToString() ?? "null" ).ToList() ) + " )";
		//}
		//

		public static string GetCacheName( string prefix, params object[] parameters )
		{
			return prefix + " ( " + string.Join( ", ", parameters.Select( m => m?.ToString() ?? "null" ).ToList() ) + " )";
		}
		//

	}
}
