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

namespace workIT.Factories
{
    public class CodesManager : BaseFactory
    {
        #region constants - property categories
        public static int PROPERTY_CATEGORY_JURISDICTION = 1;
        public static int PROPERTY_CATEGORY_CREDENTIAL_TYPE = 2;
        public static int PROPERTY_CATEGORY_CREDENTIAL_PURPOSE = 3;
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
        public static int PROPERTY_CATEGORY_MOC = 12;
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
        public static int PROPERTY_CATEGORY_REVOCATION_CRITERIA_TYPE = 26;
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

        public static int PROPERTY_CATEGORY_ACTION_STATUS_TYPE = 40;
        public static int PROPERTY_CATEGORY_CLAIM_TYPE = 41;
        public static int PROPERTY_CATEGORY_EXTERNAL_INPUT_TYPE = 42;
        //      [Obsolete]
        //      public static int PROPERTY_CATEGORY_PROCESS_METHOD = 43;
        //      [Obsolete]
        //      public static int PROPERTY_CATEGORY_STAFF_EVALUATION_METHOD = 44;
        public static int PROPERTY_CATEGORY_QA_TARGET_TYPE = 45;
        public static int PROPERTY_CATEGORY_LEARNING_RESOURCE_URLS = 46;
        public static int PROPERTY_CATEGORY_OWNING_ORGANIZATION_TYPE = 47;
        public static int PROPERTY_CATEGORY_PRIMARY_EARN_METHOD = 48;

        public static int PROPERTY_CATEGORY_JurisdictionAssertionType = 52;
        public static int PROPERTY_CATEGORY_Learning_Method_Type = 53;
        public static int PROPERTY_CATEGORY_Scoring_Method = 54;

        public static int PROPERTY_CATEGORY_Assessment_Method_Type = 56;

        public static int PROPERTY_CATEGORY_SUBMISSION_ITEM = 57;

        //reporting

        //continued
        public static int PROPERTY_CATEGORY_DEGREE_CONCENTRATION = 62;
        public static int PROPERTY_CATEGORY_DEGREE_MAJOR = 63;
        public static int PROPERTY_CATEGORY_DEGREE_MINOR = 64;

        public static int PROPERTY_CATEGORY_LANGUAGE = 65;


        #endregion
        #region constants - entity types. 
        //An Entity is typically created only where it can have a child relationship, ex: Entity.Property
        public static int ENTITY_TYPE_CREDENTIAL = 1;
        public static int ENTITY_TYPE_ORGANIZATION = 2; //what about QACred
        public static int ENTITY_TYPE_ASSESSMENT_PROFILE = 3;
        public static int ENTITY_TYPE_CONNECTION_PROFILE = 4;
        public static int ENTITY_TYPE_CONDITION_PROFILE = 4;
        public static int ENTITY_TYPE_COST_PROFILE = 5;
        public static int ENTITY_TYPE_COST_PROFILE_ITEM = 6;
        public static int ENTITY_TYPE_LEARNING_OPP_PROFILE = 7;
        public static int ENTITY_TYPE_TASK_PROFILE = 8;
        public static int ENTITY_TYPE_PERSON = 9;

        public static int ENTITY_TYPE_COMPETENCY_FRAMEWORK = 10;

        public static int ENTITY_TYPE_REVOCATION_PROFILE = 12;
        public static int ENTITY_TYPE_VERIFICATION_PROFILE = 13;
        public static int ENTITY_TYPE_PROCESS_PROFILE = 14;
        public static int ENTITY_TYPE_CONTACT_POINT = 15;
        public static int ENTITY_TYPE_ADDRESS_PROFILE = 16;
        public static int ENTITY_TYPE_CASS_COMPETENCY_FRAMEWORK = 17;
        //...see below
        public static int ENTITY_TYPE_CONDITION_MANIFEST = 19;
        public static int ENTITY_TYPE_COST_MANIFEST = 20;
        /// <summary>
        /// Placeholder for stats, will not actually have an entity
        /// </summary>
        public static int ENTITY_TYPE_JURISDICTION_PROFILE = 18;
        public static int ENTITY_TYPE_DURATION_PROFILE = 22;

        #endregion
        #region constants - entity status
        public static int ENTITY_STATUS_IN_PROGRESS = 1;
        public static int ENTITY_STATUS_PUBLISHED = 2;

        public static int ENTITY_STATUS_DELETED = 6;
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
            using ( var context = new EntityContext() )
            {
                //context.Configuration.LazyLoadingEnabled = false;

                Codes_PropertyCategory category = context.Codes_PropertyCategory
                            .FirstOrDefault( s => s.CodeName.ToLower() == datasource.ToLower() && s.IsActive == true );

                return FillEnumeration( category, getAll, onlySubType1 );
            }

        }

        public static Enumeration GetEnumeration( int categoryId, bool getAll = true )
        {
            using ( var context = new EntityContext() )
            {

                Codes_PropertyCategory category = context.Codes_PropertyCategory
                            .FirstOrDefault( s => s.Id == categoryId && s.IsActive == true );

                return FillEnumeration( category, getAll, false );

            }

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
                            val.SortOrder = item.SortOrder != null ? ( int )item.SortOrder : 0;
                            val.SchemaName = item.SchemaName ?? "";
                            val.SchemaUrl = item.SchemaUrl;
                            val.ParentSchemaName = item.ParentSchemaName ?? "";
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

        /// <summary>
        /// Get the selected item from an enumeration that only allows a singles selection
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static int GetEnumerationSelection( Enumeration e )
        {
            int selectedId = 0;
            if ( e == null || e.Items == null || e.Items.Count() == 0 )
            {
                return 0;
            }

            foreach ( EnumeratedItem item in e.Items )
            {
                if ( item.Selected )
                {
                    selectedId = item.Id;
                    break;
                }
            }

            return selectedId;

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
                            val.Id = item.CodeId == null ? 0 : ( int )item.CodeId;
                            val.CodeId = item.CodeId == null ? 0 : ( int )item.CodeId;
							val.ParentId = category.Id;
                            val.Name = item.Title;
                            
							if ( category.Id == 65)
							{
								val.Value = item.Title;
							}
							else
							{
								val.Value = item.CodeId == null ? "" : (( int )item.CodeId).ToString();
							}
							val.Totals = item.Totals ?? 0;
                            if ( IsDevEnv() )
                                val.Name += string.Format( " ({0})", val.Totals );
							if (getAll || val.Totals > 0)
								entity.Items.Add( val );
                        }
                    }
                }
            }

            return entity;
        }
		#endregion
		#region Counts.EntityStatistic
		public void UpdateEntityStatistic( int entityTypeId, string schemaName, int total )
		{
			try
			{
				using ( var context = new EntityContext() )
				{
					var efEntity = context.Counts_EntityStatistic.SingleOrDefault( s => s.EntityTypeId == entityTypeId
					&& s.SchemaName == schemaName );
					if ( efEntity != null && efEntity.Id > 0 )
					{

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
				List<Counts_EntityStatistic> results = context.Counts_EntityStatistic.ToList();

				if ( results != null && results.Count > 0 )
				{

					foreach ( var item in results )
					{
						code = new CodeItem
						{
							Id = ( int )item.Id,
							CategoryId = ( int )( item.CategoryId ?? 0 ),
							Title = item.Title,
							SchemaName = item.SchemaName,
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
					code = new CodeItem();
					code.Id = ( int )item.Id;
					code.CategoryId = (int)(item.CategoryId ?? 0);
					code.Title = item.Title;
					code.Description = item.Description;
					code.SchemaName = item.SchemaName;
					code.Totals = item.Totals ?? 0;
				}
			}
			return code;
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
                    enumeration.Id = entity.Id;
                    enumeration.Name = entity.Title;
                    enumeration.Description = entity.Description;
                    enumeration.SchemaName = entity.SchemaName;
                    enumeration.Items = new List<EnumeratedItem>();

                    var results = context.Counts_EntityStatistic
                            .Where( s => s.EntityTypeId == entityTypeId
                                 && ( getAll || s.Totals > 0 ) )
                            .OrderBy( p => p.Title )
                            .ToList();

                    if ( results != null && results.Count > 0 )
                    {
                        EnumeratedItem val = new EnumeratedItem();
                        foreach ( var item in results )
                        {
                            val = new EnumeratedItem
                            {
                                Id = item.Id,
                                CodeId = 0, //??
                                ParentId = entity.Id, //??
                                Name = item.Title,
                                Value = item.Title,
								CategoryId = item.CategoryId ?? 0,
                                Totals = item.Totals ?? 0
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
        }

        public static Enumeration GetConnectionTypes( int parentEntityTypeId, bool getAll = true )
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
                            Id = ( int )item.Id,
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
        }
        public static CodeItem Codes_EntityType_Get( int entityTypeId )
        {
            CodeItem code = new CodeItem();

            using ( var context = new EntityContext() )
            {
                List<Codes_EntityTypes> results = context.Codes_EntityTypes
                    .Where( s => s.IsActive == true && s.Id == entityTypeId )
                    .OrderBy( s => s.Title )
                            .ToList();

                if ( results != null && results.Count > 0 )
                {

                    foreach ( var item in results )
                    {
                        code = new CodeItem();
                        code.Id = ( int )item.Id;
                        code.Title = item.Title;
                        code.Description = item.Description;
                        code.SchemaName = item.SchemaName;
                        code.Totals = item.Totals ?? 0;
                        break;
                    }
                }
            }
            return code;

        }

        #endregion

        #region Codes_PropertyCategory and values
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

        private static List<CodeItem> Property_GetValues( int categoryId, string categoryTitle, bool insertingSelectTitle = true, bool getAll = true )
        {
            List<CodeItem> list = new List<CodeItem>();
            CodeItem code;

            using ( var context = new EntityContext() )
            {
                List<Codes_PropertyValue> results = context.Codes_PropertyValue
                    .Where( s => s.CategoryId == categoryId
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
                        code.URL = "";
                        list.Add( code );
                    }
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

                        list.Add( code );
                    }
                }
            }
            return list;
        }


        /// <summary>
        /// Check if the provided property schema is valid
        /// </summary>
        /// <param name="category"></param>
        /// <param name="schemaName"></param>
        /// <returns></returns>
        public static bool IsPropertySchemaValid( string categoryCode, ref string schemaName )
        {
            CodeItem item = GetPropertyBySchema( categoryCode, schemaName );

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

        public static bool IsPropertySchemaValid( string categoryCode, string schemaName, ref CodeItem item )
        {
            item = GetPropertyBySchema( categoryCode, schemaName );

            if ( item != null && item.Id > 0 )
            {
                return true;
            }
            else
                return false;
        }
        /// <summary>
        /// Get a single property using the category code, and property schema name
        /// </summary>
        /// <param name="category"></param>
        /// <param name="schemaName"></param>
        /// <returns></returns>
        public static CodeItem GetPropertyBySchema( string categoryCode, string schemaName )
        {
            CodeItem code = new CodeItem();

            using ( var context = new EntityContext() )
            {
                //for the most part, the code schema name should be unique. We may want a extra check on the categoryCode?
                //TODO - need to ensure the schemas are accurate - and not make sense to check here
                Codes_PropertyCategory category = context.Codes_PropertyCategory
                            .FirstOrDefault( s => s.SchemaName.ToLower() == categoryCode.ToLower() && s.IsActive == true );

                Codes_PropertyValue item = context.Codes_PropertyValue
                    .FirstOrDefault( s => s.SchemaName == schemaName );
                if ( item != null && item.Id > 0 )
                {
                    //could have an additional check that the returned category is correct - no guarentees though
                    code = new CodeItem();
                    code.Id = ( int )item.Id;
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
        }

        public static CodeItem GetPropertyBySchema( int categoryId, string schemaName )
        {
            CodeItem code = new CodeItem();

            using ( var context = new EntityContext() )
            {

                Codes_PropertyValue item = context.Codes_PropertyValue
                    .FirstOrDefault( s => s.CategoryId == categoryId
                            && s.SchemaName.Trim() == schemaName.Trim() );
                if ( item != null && item.Id > 0 )
                {
                    //could have an additional check that the returned category is correct - no guarentees though
                    code = new CodeItem();
                    code.Id = ( int )item.Id;
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
        }

        /// <summary>
        /// Get a code item by category and title
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public static CodeItem Codes_PropertyValue_Get( int categoryId, string title )
        {
            CodeItem code = new CodeItem();

            using ( var context = new EntityContext() )
            {
                List<Codes_PropertyValue> results = context.Codes_PropertyValue
                    .Where( s => s.CategoryId == categoryId
                            && s.Title.ToLower() == title.ToLower() )
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

        public static CodeItem Codes_PropertyValue_GetBySchema( int categoryId, string schemaName )
        {
            CodeItem code = new CodeItem();

            using ( var context = new EntityContext() )
            {
                List<Codes_PropertyValue> results = context.Codes_PropertyValue
                    .Where( s => s.CategoryId == categoryId
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

        #region country/Currency/Language Codes
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
                            val = new EnumeratedItem();
                            val.Id = ( int )item.NumericCode;
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

        public static Codes_Currency GetCurrencyItem( int numericCode )
        {
            Codes_Currency item = new Codes_Currency();
            using ( var context = new ViewContext() )
            {
                Codes_Currency currency = context.Codes_Currency
                .FirstOrDefault( s => s.NumericCode == numericCode );
                if ( currency != null && currency.NumericCode > 0 )
                {
                    return currency;
                }

            }
            return item;
        }
        public static Codes_Currency GetCurrencyItem( string currencyCode )
        {
            Codes_Currency item = new Codes_Currency();
            using ( var context = new ViewContext() )
            {
                Codes_Currency currency = context.Codes_Currency
                    .FirstOrDefault( s => s.AlphabeticCode == currencyCode || s.Currency == currencyCode );
                if ( currency != null && currency.NumericCode > 0 )
                {
                    return currency;
                }

            }
            return item;
        }
        public static Enumeration GetLanguages()
		{

            Enumeration entity = new Enumeration();
            EnumeratedItem val = new EnumeratedItem();

            using ( var context = new EntityContext() )
            {
                Codes_PropertyCategory category = context.Codes_PropertyCategory
                .FirstOrDefault( s => s.Id == PROPERTY_CATEGORY_LANGUAGE );

                entity.Id = category.Id;
                entity.Name = category.Title;
                entity.Description = category.Description;

                entity.SchemaName = category.SchemaName;
                entity.Url = category.SchemaUrl;
                entity.Items = new List<EnumeratedItem>();

                using ( var vcontext = new ViewContext() )
                {
					List<Codes_Language> results = vcontext.Codes_Language
					.OrderBy( s => s.SortOrder )
					.ThenBy( s => s.LanguageName )
					.ToList();

					if ( results != null && results.Count > 0 )
                    {
                        foreach ( Codes_Language item in results )
                        {
                            val = new EnumeratedItem();
                            val.Id = ( int )item.Id;
                            val.Value = item.LangugeCode;
                            val.Name = item.LanguageName;
                            val.Description = item.LanguageName;
                            val.SortOrder = item.SortOrder != null ? ( int )item.SortOrder : 0;
							entity.Items.Add( val );
                        }
                    }
                }
            }

            return entity;
        }

        public static EnumeratedItem GetLanguage( int languageId )
        {
            EnumeratedItem val = new EnumeratedItem();

            using ( var context = new ViewContext() )
            {
                Codes_Language item = context.Codes_Language
                .FirstOrDefault( s => s.Id == languageId );
                if ( item != null && item.Id > 0 )
                {
                    val.Id = item.Id;
                    val.Value = item.LangugeCode;
                    val.Name = item.LanguageName;
                    val.Description = item.LanguageName;
                    val.SortOrder = item.SortOrder != null ? ( int )item.SortOrder : 0;
                    val.SchemaName = item.LangugeCode;
                }

            }

            return val;
        }
        public static int GetLanguageId( string language )
        {
            int id = 0;
            EnumeratedItem item = GetLanguage( language );
            if ( item != null && item.Id > 0 )
                return item.Id;

            return id;
        }
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
            string altLanguage = "";
            if ( language.IndexOf( "-" ) > 1 )
            {
                altLanguage = language.Substring( 0, language.IndexOf( "-" ) );
            }
            using ( var context = new ViewContext() )
            {
                Codes_Language item = context.Codes_Language
                .FirstOrDefault( s => s.LangugeCode == language
                        || s.LanguageName == language
                        || ( altLanguage.Length > 0 && s.LangugeCode.StartsWith( altLanguage ) )
                        );
                if ( item != null && item.Id > 0 )
                {
                    val.Id = item.Id;
                    val.Value = item.LangugeCode;
                    val.Name = item.LanguageName;
                    val.Description = item.LanguageName;
                    val.SortOrder = item.SortOrder != null ? ( int )item.SortOrder : 0;
                    val.SchemaName = item.LangugeCode;
                }
            }

            return val;
        }
        #endregion
        #region SOC

        public static List<CodeItem> SOC_Search( int headerId, string keyword, int pageNumber, int pageSize, ref int totalRows, bool getAll = true )
        {
            List<CodeItem> list = new List<CodeItem>();
            CodeItem entity = new CodeItem();
            keyword = ( keyword ?? "" ).Trim();
            if ( pageSize == 0 )
                pageSize = 100;
            int skip = 0;
            if ( pageNumber > 1 )
                skip = ( pageNumber - 1 ) * pageSize;
            string notKeyword = "Except " + keyword;

            using ( var context = new ViewContext() )
            {
                List<ONET_SOC> results = context.ONET_SOC
                    .Where( s => ( headerId == 0 || s.OnetSocCode.Substring( 0, 2 ) == headerId.ToString() )
                        && ( keyword == ""
                        || s.OnetSocCode.Contains( keyword )
                        || s.SOC_code.Contains( keyword )
                        || ( s.Title.Contains( keyword ) && s.Title.Contains( notKeyword ) == false )
                        )
                        && ( s.Totals > 0 || getAll )
                        )
                    .OrderBy( s => s.Title )
                    .Skip( skip )
                    .Take( pageSize )
                    .ToList();

                totalRows = context.ONET_SOC
                    .Where( s => ( headerId == 0 || s.OnetSocCode.Substring( 0, 2 ) == headerId.ToString() )
                        && ( keyword == ""
                        || s.OnetSocCode.Contains( keyword )
                        || s.SOC_code.Contains( keyword )
                        || s.Title.Contains( keyword ) )
                        && ( s.Totals > 0 || getAll )
                        )
                    .ToList().Count();

                if ( results != null && results.Count > 0 )
                {
                    foreach ( ONET_SOC item in results )
                    {
                        entity = new CodeItem();
                        entity.Id = item.Id;
                        entity.Name = item.Title;// +" ( " + item.OnetSocCode + " )";
                        entity.Description = item.Description;
                        entity.URL = item.URL;
                        entity.Code = item.OnetSocCode;
                        entity.CodeGroup = item.JobFamily.ToString();
                        entity.Totals = item.Totals ?? 0;

                        list.Add( entity );
                    }
                }
            }

            return list;
        }
        /// <summary>
        /// ONET SOC autocomplete
        /// </summary>
        /// <param name="headerId"></param>
        /// <param name="keyword"></param>
        /// <param name="pageSize"></param>
        /// <param name="sortField">Description or SOC_code</param>
        /// <returns></returns>
        //public static List<CodeItem> SOC_Autocomplete( int headerId = 0, string keyword = "", int pageSize = 0, string sortField = "Description" )
        //{
        //    List<CodeItem> list = new List<CodeItem>();
        //    CodeItem entity = new CodeItem();
        //    keyword = keyword.Trim();
        //    if ( pageSize == 0 )
        //        pageSize = 100;

        //    using ( var context = new ViewContext() )
        //    {
        //        var Query = from P in context.ONET_SOC
        //                    .Where( s => ( headerId == 0 || s.OnetSocCode.Substring( 0, 2 ) == headerId.ToString() )
        //                && ( keyword == ""
        //                || s.OnetSocCode.Contains( keyword )
        //                || s.Title.Contains( keyword ) ) )
        //                    select P;

        //        if ( sortField == "SOC_code" )
        //        {
        //            Query = Query.OrderBy( p => p.SOC_code );
        //        }
        //        else
        //        {
        //            Query = Query.OrderBy( p => p.Title );
        //        }
        //        var count = Query.Count();
        //        var results = Query.Take( pageSize )
        //            .ToList();

        //        if ( results != null && results.Count > 0 )
        //        {
        //            foreach ( ONET_SOC item in results )
        //            {
        //                entity = new CodeItem();
        //                entity.Id = item.Id;
        //                entity.Name = item.Title;
        //                entity.Description = " ( " + item.OnetSocCode + " )" + item.Title;
        //                list.Add( entity );
        //            }
        //        }
        //    }

        //    return list;
        //}

        public static List<CodeItem> SOC_Categories( string sortField = "Description", bool includeCategoryCode = false )
        {
            List<CodeItem> list = new List<CodeItem>();
            CodeItem code;

            using ( var context = new ViewContext() )
            {
                var Query = from P in context.ONET_SOC_JobFamily
                            select P;

                if ( sortField == "JobFamilyId" )
                {
                    Query = Query.OrderBy( p => p.JobFamilyId );
                }
                else
                {
                    Query = Query.OrderBy( p => p.Description );
                }
                var count = Query.Count();
                var results = Query.ToList();
                //List<ONET_SOC_JobFamily> results2 = context.ONET_SOC_JobFamily
                //	.OrderBy( s => s.Description )
                //	.ToList();

                if ( results != null && results.Count > 0 )
                {
                    foreach ( ONET_SOC_JobFamily item in results )
                    {
                        code = new CodeItem();
                        code.Id = item.JobFamilyId;
                        if ( includeCategoryCode )
                        {
                            if ( sortField == "JobFamilyId" )
                                code.Title = item.JobFamilyId + " - " + item.Description;
                            else
                                code.Title = item.Description + " (" + item.JobFamilyId + ")";
                        }
                        else
                            code.Title = item.Description;
                        code.Totals = ( int )( item.Totals ?? 0 );
                        code.CategorySchema = "ctdl:SocGroup";
                        list.Add( code );
                    }
                }
            }
            return list;
        }

        #endregion


        #region NAICS
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
        //				&& ( keyword == ""
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
        //				&& ( keyword == ""
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
        //				entity.Description = "";// 						item.NaicsTitle + " ( " + item.NaicsCode + " )";
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
        public static List<CodeItem> NAICS_SearchInUse( int entityTypeId, int headerId, string keyword, int pageNumber, int pageSize, ref int totalRows )
        {
            List<CodeItem> list = new List<CodeItem>();
            CodeItem entity = new CodeItem();
            keyword = keyword.Trim();
            if ( pageSize == 0 )
                pageSize = 100;
            int skip = 0;
            if ( pageNumber > 1 )
                skip = ( pageNumber - 1 ) * pageSize;
            string notKeyword = "Except " + keyword;

            using ( var context = new ViewContext() )
            {
                List<Entity_FrameworkIndustryCodeSummary> results = context.Entity_FrameworkIndustryCodeSummary
                        .Where( s => ( headerId == 0 || s.CodeGroup == headerId )
                        && ( s.EntityTypeId == entityTypeId )
                        && ( keyword == ""
                        || s.CodedNotation.Contains( keyword )
                        || s.Name.Contains( keyword ) )
                        && ( s.Totals > 0 )
                        )
                    .OrderBy( s => s.Name )
                    .Skip( skip )
                    .Take( pageSize )
                    .ToList();
                totalRows = context.Entity_FrameworkIndustryCodeSummary
                        .Where( s => ( headerId == 0 || s.CodeGroup == headerId )
                        && ( s.EntityTypeId == entityTypeId )
                        && ( keyword == ""
                        || s.CodedNotation.Contains( keyword )
                        || s.Name.Contains( keyword ) )
                        && ( s.Totals > 0 )
                        )
                    .ToList().Count();

                if ( results != null && results.Count > 0 )
                {
                    foreach ( Views.Entity_FrameworkIndustryCodeSummary item in results )
                    {
                        entity = new CodeItem();
                        entity.Id = ( int )item.Id;
                        entity.Name = item.Name;// + " ( " + item.NaicsCode + " )";
                        entity.Description = "";// 						item.NaicsTitle + " ( " + item.NaicsCode + " )";
                        entity.URL = item.TargetNode;
                        entity.SchemaName = item.CodedNotation;
                        entity.Code = item.CodeGroup.ToString();
                        entity.Totals = item.Totals ?? 0;

                        list.Add( entity );
                    }
                }
            }

            return list;
        }
        public static List<CodeItem> ReferenceFramework_SearchInUse( int categoryId, int entityTypeId, string headerId, string keyword, int pageNumber, int pageSize, ref int totalRows )
        {
            List<CodeItem> list = new List<CodeItem>();
            CodeItem entity = new CodeItem();
            keyword = keyword.Trim();
            if ( headerId == "0" )
                headerId = "";

            if ( pageSize == 0 )
                pageSize = 100;
            int skip = 0;
            if ( pageNumber > 1 )
                skip = ( pageNumber - 1 ) * pageSize;
            string notKeyword = "Except " + keyword;

            using ( var context = new ViewContext() )
            {
                List<Entity_ReferenceFramework_Totals> results = context.Entity_ReferenceFramework_Totals
                        .Where( s => ( headerId == "" || s.CodeGroup == headerId )
                        && ( s.CategoryId == categoryId )
                        && ( s.EntityTypeId == entityTypeId )
                        && ( keyword == ""
                        || s.CodedNotation.Contains( keyword )
                        || s.Name.Contains( keyword ) )
                        && ( s.Totals > 0 )
                        )
                    .OrderBy( s => s.Name )
                    .Skip( skip )
                    .Take( pageSize )
                    .ToList();
                totalRows = context.Entity_ReferenceFramework_Totals
                        .Where( s => ( headerId == "" || s.CodeGroup == headerId )
                        && ( s.EntityTypeId == entityTypeId )
                        && ( keyword == ""
                        || s.CodedNotation.Contains( keyword )
                        || s.Name.Contains( keyword ) )
                        && ( s.Totals > 0 )
                        )
                    .ToList().Count();

                if ( results != null && results.Count > 0 )
                {
                    foreach ( Views.Entity_ReferenceFramework_Totals item in results )
                    {
                        entity = new CodeItem();
                        entity.Id = ( int )item.ReferenceFrameworkId;
                        entity.Name = item.Name;
                        entity.Description = "";
                        entity.URL = item.TargetNode;
                        entity.Code = item.CodedNotation;
                        entity.CodeGroup = item.CodeGroup ?? "";
                        entity.Totals = item.Totals ?? 0;

                        list.Add( entity );
                    }
                }
            }

            return list;
        }
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
        //				&& ( keyword == ""
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

        public static List<CodeItem> NAICS_Categories( string sortField = "Description", bool includeCategoryCode = false )
        {
            List<CodeItem> list = new List<CodeItem>();
            CodeItem entity;
            using ( var context = new ViewContext() )
            {
                //List<NAICS> results = context.NAICS
                //	.Where( s => s.NaicsCode.Length == 2 && s.NaicsGroup > 10)
                //	.OrderBy( s => s.NaicsCode )
                //	.ToList();
                var Query = from P in context.NAICS
                            .Where( s => s.NaicsCode.Length == 2 && s.NaicsGroup > 10 )
                            select P;

                if ( sortField == "NaicsGroup" )
                {
                    Query = Query.OrderBy( p => p.NaicsGroup );
                }
                else
                {
                    Query = Query.OrderBy( p => p.NaicsTitle );
                }
                var results = Query.ToList();
                if ( results != null && results.Count > 0 )
                {
                    foreach ( NAIC item in results )
                    {
                        entity = new CodeItem();
                        entity.Id = Int32.Parse( item.NaicsCode );

                        if ( includeCategoryCode )
                        {
                            if ( sortField == "NaicsGroup" )
                                entity.Title = item.NaicsCode + " - " + item.NaicsTitle;
                            else
                                entity.Title = item.NaicsTitle + " (" + item.NaicsCode + ")";
                        }
                        else
                            entity.Title = item.NaicsTitle;

                        entity.URL = item.URL;
                        entity.Totals = ( int )( item.Totals ?? 0 );
                        entity.CategorySchema = "ctdl:NaicsGroup";

                        list.Add( entity );
                    }
                }
            }

            return list;
        }
        public static List<CodeItem> NAICS_CategoriesInUse( int entityTypeId )
        {
            List<CodeItem> list = new List<CodeItem>();
            //CodeItem code;
            //, string sortField = "Description"
            //using ( var context = new ViewContext() )
            //{

            //	List<Entity_FrameworkIndustryGroupSummary> results = context.Entity_FrameworkIndustryGroupSummary
            //				.Where( s => s.EntityTypeId == entityTypeId )
            //				.OrderBy( x => x.FrameworkGroupTitle )
            //				.ToList();

            //	if ( results != null && results.Count > 0 )
            //	{
            //		foreach ( Views.Entity_FrameworkIndustryGroupSummary item in results )
            //		{
            //			code = new CodeItem();
            //			code.Id = ( int ) item.CodeGroup;
            //			code.Title = item.FrameworkGroupTitle;
            //			code.Totals = ( int ) ( item.groupCount ?? 0 );
            //			code.CategorySchema = "ctdl:IndustryGroup";
            //			list.Add( code );
            //		}
            //	}
            //}
            return list;
        }
        #endregion


        #region CIPS
        //public static List<CodeItem> CIPS_Search( int headerId = 0, string keyword = "", int pageNumber = 1, int pageSize = 0 )
        //{
        //	int totalRows = 0;

        //	return CIPS_Search( headerId, keyword, pageNumber, pageSize, ref totalRows );
        //}		

        public static List<CodeItem> CIPS_Search( int headerId, string keyword, int pageNumber, int pageSize, ref int totalRows, bool getAll = true )
        {
            List<CodeItem> list = new List<CodeItem>();
            CodeItem entity = new CodeItem();
            string header = headerId.ToString();
            if ( headerId > 0 && headerId < 10 )
                header = "0" + header;
            keyword = keyword.Trim();
            if ( pageSize == 0 )
                pageSize = 100;
            int skip = 0;
            if ( pageNumber > 1 )
                skip = ( pageNumber - 1 ) * pageSize;

            using ( var context = new ViewContext() )
            {
                List<CIPCode2010> results = context.CIPCode2010
                        .Where( s => ( headerId == 0 || s.CIPCode.Substring( 0, 2 ) == header )
                        && ( keyword == ""
                        || s.CIPCode.Contains( keyword )
                        || s.CIPTitle.Contains( keyword )
                        )
                        && ( s.Totals > 0 || getAll )
                        )
                    .OrderBy( s => s.CIPTitle )
                    .Skip( skip )
                    .Take( pageSize )
                    .ToList();

                totalRows = context.CIPCode2010
                        .Where( s => ( headerId == 0 || s.CIPCode.Substring( 0, 2 ) == header )
                        && ( keyword == ""
                        || s.CIPCode.Contains( keyword )
                        || s.CIPTitle.Contains( keyword ) )
                        && ( s.Totals > 0 || getAll )
                        )
                    .ToList().Count();

                if ( results != null && results.Count > 0 )
                {
                    foreach ( CIPCode2010 item in results )
                    {
                        entity = new CodeItem();
                        entity.Id = item.Id;
                        entity.Name = item.CIPTitle + " ( " + item.CIPCode + " )";
                        entity.Description = item.CIPDefinition;
                        //entity.URL = item.URL;
                        entity.Code = item.CIPCode;
                        entity.CodeGroup = item.CIPFamily;
                        entity.Totals = item.Totals ?? 0;
                        list.Add( entity );
                    }
                }
            }

            return list;
        }
        public static List<CodeItem> CIPS_SearchInUse( int entityTypeId, int headerId, string keyword, int pageNumber, int pageSize, ref int totalRows )
        {
            List<CodeItem> list = new List<CodeItem>();
            CodeItem entity = new CodeItem();
            string header = headerId.ToString();
            if ( headerId > 0 && headerId < 10 )
                header = "0" + header;
            keyword = keyword.Trim();
            if ( pageSize == 0 )
                pageSize = 100;
            int skip = 0;
            if ( pageNumber > 1 )
                skip = ( pageNumber - 1 ) * pageSize;

            using ( var context = new ViewContext() )
            {
                List<Entity_FrameworkCIPCodeSummary> results = context.Entity_FrameworkCIPCodeSummary
                        .Where( s => ( headerId == 0 || s.CodeGroup == header )
                        && ( s.EntityTypeId == entityTypeId )
                        && ( keyword == ""
                        || s.CIPCode.Contains( keyword )
                        || s.CIPTitle.Contains( keyword )
                        )
                        && ( s.Totals > 0 )
                        )
                    .OrderBy( s => s.CIPTitle )
                    .Skip( skip )
                    .Take( pageSize )
                    .ToList();

                totalRows = context.Entity_FrameworkCIPCodeSummary
                        .Where( s => ( headerId == 0 || s.CodeGroup == header )
                        && ( s.EntityTypeId == entityTypeId )
                        && ( keyword == ""
                        || s.CIPCode.Contains( keyword )
                        || s.CIPTitle.Contains( keyword )
                        )
                        && ( s.Totals > 0 )
                        )
                    .ToList().Count();

                if ( results != null && results.Count > 0 )
                {
                    foreach ( Views.Entity_FrameworkCIPCodeSummary item in results )
                    {
                        entity = new CodeItem();
                        entity.Id = ( int )item.Id;
                        entity.Name = item.CIPTitle + " ( " + item.CIPCode + " )";
                        //entity.Description = item.CIPDefinition;
                        entity.URL = item.URL;
                        entity.Code = item.CIPCode;
                        entity.CodeGroup = item.CodeGroup;
                        entity.Totals = item.Totals ?? 0;
                        list.Add( entity );
                    }
                }
            }

            return list;
        }

        public static List<CodeItem> CIPS_Autocomplete( int headerId = 0, string keyword = "", int pageSize = 0 )
        {
            List<CodeItem> list = new List<CodeItem>();
            CodeItem entity = new CodeItem();
            keyword = keyword.Trim();
            if ( pageSize == 0 )
                pageSize = 100;

            using ( var context = new ViewContext() )
            {
                List<CIPCode2010> results = context.CIPCode2010
                        .Where( s => ( headerId == 0 || s.CIPFamily == headerId.ToString() )
                        && ( keyword == ""
                        || s.CIPTitle.Contains( keyword )
                        || s.CIPDefinition.Contains( keyword ) ) )
                        .OrderBy( s => s.CIPCode )
                        .ToList();

                if ( results != null && results.Count > 0 )
                {
                    foreach ( CIPCode2010 item in results )
                    {
                        entity = new CodeItem();
                        entity.Id = item.Id;
                        entity.Name = item.CIPTitle;
                        entity.Description = item.CIPTitle + " ( " + item.CIPCode + " )";
                        //entity.URL = item.URL;
                        entity.Totals = item.Totals ?? 0;
                        list.Add( entity );
                    }
                }
            }

            return list;
        }

        public static List<CodeItem> CIPS_Categories( string sortField = "CIPFamily" )
        {
            List<CodeItem> list = new List<CodeItem>();
            CodeItem entity;
            using ( var context = new ViewContext() )
            {
                //List<CIPS> results = context.CIPS
                //	.Where( s => s.NaicsCode.Length == 2 && s.NaicsGroup > 10)
                //	.OrderBy( s => s.NaicsCode )
                //	.ToList();
                var Query = from P in context.CIPCode2010
                            .Where( s => s.CIPCode.Length == 2 )
                            select P;

                if ( sortField == "CIPFamily" )
                {
                    Query = Query.OrderBy( p => p.CIPFamily );
                }
                else
                {
                    Query = Query.OrderBy( p => p.CIPTitle );
                }
                var results = Query.ToList();
                if ( results != null && results.Count > 0 )
                {
                    foreach ( CIPCode2010 item in results )
                    {
                        entity = new CodeItem();
                        entity.Id = Int32.Parse( item.CIPFamily );
                        if ( sortField == "CIPFamily" )
                            entity.Title = item.CIPCode + " - " + item.CIPTitle;
                        else
                            entity.Title = item.CIPTitle + " (" + item.CIPCode + ")";
                        //entity.URL = item.URL;

                        entity.Totals = ( int )( item.Totals ?? 0 );
                        entity.CategorySchema = "ctdl:CipsGroup";
                        list.Add( entity );
                    }
                }
            }

            return list;
        }

        public static List<CodeItem> CIPS_CategoriesInUse( int entityTypeId, string sortField = "codeId" )
        {
            List<CodeItem> list = new List<CodeItem>();
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
            return list;
        }
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


        
        public void UpdateEntityTypes(int id, int total )
        {
            try
            {
                using ( var context = new EntityContext() )
                {
                    var efEntity = context.Codes_EntityTypes.SingleOrDefault( s => s.Id == id );
                    if ( efEntity != null && efEntity.Id > 0 )
                    {

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

                foreach(var item in history )
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
        public static List<CodeItem> CodeEntity_GetMainClassTotals()
        {
            List<CodeItem> list = new List<CodeItem>();
            CodeItem code;

            using ( var context = new EntityContext() )
            {
                List<Codes_EntityTypes> results = context.Codes_EntityTypes
                    .Where( s => s.Id < 4 || s.Id == 7 || s.Id==10)
                            .OrderBy( s => s.Id )
                            .ToList();

                if ( results != null && results.Count > 0 )
                {

                    foreach ( Codes_EntityTypes item in results )
                    {
                        code = new CodeItem();
                        code.Id = item.Id;
                        code.Title = item.Title;
                        code.Totals = ( int )item.Totals;

                        code.Description = item.Description;
                        list.Add( code );
                    }
                }
                //add QA orgs, and others
                //
                code = new CodeItem();
                code.Id = 99;
                code.Title = "QA Organization";
                //code.Totals = OrganizationManager.QAOrgCounts();
                list.Add( code );

            }
            return list;
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

        #endregion

    }
}
