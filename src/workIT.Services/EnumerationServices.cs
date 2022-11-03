using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using workIT.Models;
using MC = workIT.Models.Common;
using MSR = workIT.Models.Search;
using workIT.Factories;

namespace workIT.Services
{
    public class EnumerationServices
    {

		#region enumerations 
		/// <summary>
		/// Get an MC.Enumeration (by default a checkbox list) by schemaName
		/// </summary>
		/// <param name="dataSource"></param
		/// <param name="interfaceType"></param>
		/// <param name="showOtherValue">If true, a text box for entering other values will be displayed</param>
		/// <returns></returns>
		public MC.Enumeration GetEnumeration( string dataSource, MC.EnumerationType interfaceType = MC.EnumerationType.MULTI_SELECT,
                bool showOtherValue = false,
                bool getAll = true )
        {
            MC.Enumeration e = CodesManager.GetEnumeration( dataSource, getAll );
            e.InterfaceType = interfaceType;
            e.ShowOtherValue = showOtherValue;
            return e;
        }
        public MC.Enumeration GetEnumeration( int categoryId, MC.EnumerationType interfaceType = MC.EnumerationType.MULTI_SELECT,
                bool showOtherValue = false,
                bool getAll = true )
        {
            MC.Enumeration e = CodesManager.GetEnumeration( categoryId, getAll );
            e.InterfaceType = interfaceType;
            e.ShowOtherValue = showOtherValue;
            return e;
        }
        public MC.Enumeration EntityStatisticGetEnumeration( int entityTypeId, MC.EnumerationType interfaceType = MC.EnumerationType.MULTI_SELECT, bool getAll = true )
		{
			MC.Enumeration e = CodesManager.GetEntityStatisticsAsEnumeration( entityTypeId, getAll );
			e.InterfaceType = interfaceType;
			return e;
		}
		public MC.Enumeration GetEnumerationForRadioButtons( string dataSource, int preselectId = -1, bool getAll = true )
        {
            MC.Enumeration e = CodesManager.GetEnumeration( dataSource, getAll );
            e.InterfaceType = MC.EnumerationType.SINGLE_SELECT;
            if ( preselectId > -1 && e.HasItems() )
            {
                int cntr = 0;
                foreach ( MC.EnumeratedItem item in e.Items )
                {
                    if ( cntr == preselectId )
                    {
                        item.Selected = true;
                        break;
                    }
                    cntr++;
                }
            }
            return e;
        }

        public MC.Enumeration GetJurisdictionAssertions( string filter, MC.EnumerationType interfaceType = MC.EnumerationType.MULTI_SELECT,
            bool showOtherValue = true,
            bool getAll = true )
        {
            MC.Enumeration e = CodesManager.GetJurisdictionAssertions_Filtered( filter );
            e.InterfaceType = interfaceType;
            e.ShowOtherValue = showOtherValue;
            return e;
        }
        #endregion

        /// <summary>
        /// Get a list of properties - typically called from Views
        /// </summary>
        /// <param name="dataSource"></param>
        /// <param name="getAll"></param>
        /// <returns></returns>
        public List<CodeItem> GetPropertiesList( string dataSource, bool getAll = true )
        {
            bool insertSelectTitle = false;
            List<CodeItem> list = CodesManager.Property_GetValues( dataSource, insertSelectTitle, getAll );

            return list;
        }
        public List<CodeItem> GetPropertiesList( string dataSource, bool insertSelectTitle, bool getAll = true )
        {
            List<CodeItem> list = CodesManager.Property_GetValues( dataSource, insertSelectTitle, getAll );

            return list;
        }

        public static CodeItem GetPropertyBySchema( string categoryCode, string schemaName )
        {
            CodeItem item = CodesManager.GetPropertyBySchema( categoryCode, schemaName );

            return item;
        }
        public static List<CodeItem> Codes_GetStates()
        {
            List<CodeItem> list = CodesManager.Codes_GetStates();

            return list;
        }

		public static CodeItem GetEntityRegionTotal( int entityTypeId, int recordId )
		{
			var item = CodesManager.GetEntityRegionTotal( entityTypeId, recordId );

			return item;
		}
		#region credential enumerations
		//21-05-13 update to only return one badge type - ensure this doesn't affect the current site
		//			probably need to make a custom method
		public MC.Enumeration GetCredentialType( MC.EnumerationType interfaceType, bool getAll = true, bool includingAllBadges = false )
        {
			//MC.Enumeration e = CodesManager.GetEnumeration( "credentialType", getAll );
			MC.Enumeration e = CodesManager.GetCredentialTypes( "credentialType", getAll, includingAllBadges );
			e.ShowOtherValue = true;
            e.InterfaceType = interfaceType;
            return e;
        }
		public MC.Enumeration GetCredentialStatusType( MC.EnumerationType interfaceType, bool getAll = true )
		{

			MC.Enumeration e = CodesManager.GetEnumeration( "credentialStat", getAll );
			e.ShowOtherValue = true;
			e.InterfaceType = interfaceType;
			return e;
		}
		//
		public MC.Enumeration GetEducationCredentialType( MC.EnumerationType interfaceType, bool getAll = true )
        {

            MC.Enumeration e = CodesManager.GetEnumeration( "credentialType", getAll, true );
            e.ShowOtherValue = true;
            e.InterfaceType = interfaceType;
            return e;
        }
        //
        public MC.Enumeration GetCredentialPurpose( MC.EnumerationType interfaceType, bool getAll = true )
        {
            MC.Enumeration e = CodesManager.GetEnumeration( "purpose", getAll );
            e.ShowOtherValue = true;
            e.InterfaceType = interfaceType;
            return e;
        }
        //
        public MC.Enumeration GetAudienceLevel( MC.EnumerationType interfaceType, bool getAll = true )
        {
            MC.Enumeration e = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL, getAll );
            e.InterfaceType = interfaceType;
            e.ShowOtherValue = true;
            return e;
        }

        public MC.Enumeration GetSiteTotals( MC.EnumerationType interfaceType, int categoryId, int entityTypeId, bool getAll = true )
        {
			MC.Enumeration e = CodesManager.GetSiteTotalsAsEnumeration( categoryId, entityTypeId, getAll );
			e.InterfaceType = interfaceType;
            e.ShowOtherValue = true;
            return e;
        }//
        public MC.Enumeration GetLearningObjectType( MC.EnumerationType interfaceType, bool getAll = true )
        {

            MC.Enumeration e = CodesManager.GetLearningObjectTypesEnumeration( getAll );
            e.ShowOtherValue = true;
            e.InterfaceType = interfaceType;
            return e;
        }
        public MC.Enumeration GetOrgSubclasses( MC.EnumerationType interfaceType, bool getAll = true )
        {

            MC.Enumeration e = CodesManager.GetOrgSubclasses( getAll );
            e.ShowOtherValue = true;
            e.InterfaceType = interfaceType;
            return e;
        }
        //
        [Obsolete]
        public MC.Enumeration GetCredentialLevel( MC.EnumerationType interfaceType, bool getAll = true )
        {
            MC.Enumeration e = CodesManager.GetEnumeration( "credentialLevel", getAll );
            e.InterfaceType = interfaceType;
            e.ShowOtherValue = true;
            return e;
        }
        //
        #endregion

        #region Condition profile related
        /// <summary>
        /// Get credential connections codes.
        /// Used by search
        /// Note: a custom type that includes is part, and part of, primarily for search filters.
        /// NOTE: ensure same one used for the editor?
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <param name="getAll"></param>
        /// <returns></returns>
        //public MC.Enumeration GetCredentialConnections( MC.EnumerationType interfaceType, bool getAll = true )
        //{

        //	MC.Enumeration e = CodesManager.GetEnumeration( "conditionProfileType", getAll, true );
        //	e.ShowOtherValue = true;
        //	e.InterfaceType = interfaceType;
        //	return e;
        //}

        /// <summary>
        /// Get credential connections codes.
        /// Used by search
        /// Note: a custom type that includes is part, and part of, primarily for search filters.
        /// NOTE: ensure same one used for the editor?
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <param name="getAll"></param>
        /// <returns></returns>
        public MC.Enumeration GetCredentialConnectionsFilters( MC.EnumerationType interfaceType, bool getAll = true )
        {

            //MC.Enumeration e = CodesManager.GetCredentialsConditionProfileTypes();
            //e.ShowOtherValue = false;
            //e.InterfaceType = interfaceType;
            //return e;
            return GetConnectionTypes( interfaceType, 1, getAll );
        }

        public MC.Enumeration GetAssessmentsConditionProfileTypes( MC.EnumerationType interfaceType, bool getAll = true )
        {
            return GetConnectionTypes( interfaceType, 3, getAll );
        }
        public MC.Enumeration GetLearningOppsConditionProfileTypes( MC.EnumerationType interfaceType, bool getAll = true )
        {
            return GetConnectionTypes( interfaceType, 7, getAll );
        }
        public MC.Enumeration GetConnectionTypes( MC.EnumerationType interfaceType, int parentEntityTypeId, bool getAll = true )
        {

            MC.Enumeration e = CodesManager.GetConnectionTypes( parentEntityTypeId, getAll );
            e.ShowOtherValue = false;
            e.InterfaceType = interfaceType;
            return e;
        }

		#endregion

		#region agent role enums
		/// <summary>
		/// this may get more than actually needed
		/// </summary>
		/// <param name="interfaceType"></param>
		/// <returns></returns>
		public MC.Enumeration GetCredentialAllAgentRoles( MC.EnumerationType interfaceType )
		{
			MC.Enumeration e = OrganizationRoleManager.GetCredentialOrg_AllRoles( false );
			e.InterfaceType = interfaceType;
			e.ShowOtherValue = true;
			return e;
		}



		/// <summary>
		/// Get agent roles for assessments
		/// Ex: Created By (not any more)
		/// </summary>
		/// <param name="interfaceType"></param>
		/// <returns></returns>
		public MC.Enumeration GetAssessmentAgentRoles( MC.EnumerationType interfaceType )
		{
			//MC.Enumeration e = Entity_AgentRelationshipManager.GetAssessmentAgentRoles( false );
			MC.Enumeration e = Entity_AgentRelationshipManager.GetCommonPlusQAAgentRoles( false );
			e.InterfaceType = interfaceType;
			e.ShowOtherValue = true;
			return e;
		}
		/// <summary>
		/// Get agent roles for learning opportunities
		/// </summary>
		/// <param name="interfaceType"></param>
		/// <returns></returns>
		//public MC.Enumeration GetLearningOppAgentRoles( MC.EnumerationType interfaceType )
		//{
		//    MC.Enumeration e = Entity_AgentRelationshipManager.GetCommonPlusQAAgentRoles( false );
		//    e.InterfaceType = interfaceType;
		//    e.ShowOtherValue = true;
		//    return e;
		//}
		public MC.Enumeration GetCommonPlusQAAgentRoles( MC.EnumerationType interfaceType, bool includingPublishedBy = false )
        {
            MC.Enumeration e = Entity_AgentRelationshipManager.GetCommonPlusQAAgentRoles( false, includingPublishedBy );
            e.InterfaceType = interfaceType;
            e.ShowOtherValue = true;
            return e;
        }

        public MC.Enumeration GetAllAgentReverseRoles( MC.EnumerationType interfaceType )
        {
            MC.Enumeration e = OrganizationRoleManager.GetAgentToAgentRolesCodes( false );
            e.InterfaceType = interfaceType;
            e.ShowOtherValue = true;
            return e;
        }


        public MC.Enumeration GetEntityAgentQAActions( MC.EnumerationType interfaceType, int parentEntityTypeId, bool getAll = true, bool isInverseRole = false )
        {
            //get roles as entity to org
            MC.Enumeration e = OrganizationRoleManager.GetEntityAgentQAActions( isInverseRole, parentEntityTypeId, getAll );
            e.InterfaceType = interfaceType;
            e.ShowOtherValue = true;
            return e;
        }
		public MC.Enumeration GetEntityAgentNONQAActions( MC.EnumerationType interfaceType, int parentEntityTypeId, bool getAll = true, bool isInverseRole = false )
		{
			//get roles as entity to org
			MC.Enumeration e = OrganizationRoleManager.GetOrgEntityToNONQARoleCodes( isInverseRole, parentEntityTypeId, getAll );
			e.InterfaceType = interfaceType;
			e.ShowOtherValue = true;
			return e;
		}
		#endregion


		#region org related codes and enumurations
		public MC.Enumeration GetOrganizationType( MC.EnumerationType interfaceType,
                bool getAll = true )
        {

            MC.Enumeration e = CodesManager.GetEnumeration( "orgType", getAll );
            e.ShowOtherValue = true;
            e.InterfaceType = interfaceType;
            return e;
        }
        //
        public static MC.Enumeration GetOrganizationIdentifier( MC.EnumerationType interfaceType, bool getAll = true )
        {

            MC.Enumeration e = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_ORGANIZATION_IDENTIFIERS, getAll );
            e.ShowOtherValue = true;
            e.InterfaceType = interfaceType;
            return e;
        }
        //
        /// <summary>
        /// Get candidate list of services for an org
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        public MC.Enumeration GetOrganizationServices( MC.EnumerationType interfaceType,
                bool getAll = true )
        {
            MC.Enumeration e = CodesManager.GetEnumeration( "serviceType", getAll );
            //MC.Enumeration e = OrganizationServiceManager.GetOrgServices( getAll );
            e.InterfaceType = interfaceType;
            e.ShowOtherValue = true;
            return e;
        }


        #endregion

        #region //Temporary
        //Get a sample enumeration
        //public MC.Enumeration GetSampleEnumeration( string dataSource, string schemaName, MC.EnumerationType interfaceType )
        //{
        //	var result = CodesManager.GetSampleEnumeration( dataSource, schemaName );
        //	result.InterfaceType = interfaceType;

        //	return result;
        //}
        //
        #endregion         //End Temporary

        #region currencies/countries
        public List<CodeItem> GetExistingCountries()
        {
            List<CodeItem> list = CodesManager.GetExistingCountries();
            return list;
        }
        /// <summary>
        /// Return list as CodeItems - only Region is available, no Id of any sort
        /// </summary>
        /// <param name="country"></param>
        /// <returns></returns>
        public List<CodeItem> GetExistingRegionsForCountry( string country )
        {
            List<CodeItem> list = CodesManager.GetExistingRegionsForCountry( country );
            return list;
        }


        /// <summary>
        /// Return list as CodeItems - only Region is available, no Id of any sort
        /// </summary>
        /// <param name="countries"></param>
        /// <returns></returns>
        public List<CodeItem> GetExistingRegionsForCountries( string[] countries )
        {
            if ( countries == null || countries.Count() == 0 )
            {
                return new List<CodeItem>();
            }
            List<CodeItem> list = CodesManager.GetExistingRegionsForCountry( countries[ 0 ] );
            //List<CodeItem> list = CodesManager.GetExistingRegionsForCountries( countries );
            return list;
        }
        public List<CodeItem> GetExistingCitiesForRegion( string country, string region )
        {
            List<CodeItem> list = CodesManager.GetExistingCitiesForRegion( country, region );
            return list;
        }
        public List<CodeItem> GetCountries()
        {
            List<CodeItem> list = CodesManager.GetCountries_AsCodes();
            return list;
        }

        //GetCurrencies
        public MC.Enumeration GetCurrencies( MC.EnumerationType interfaceType )
        {
            MC.Enumeration e = CodesManager.GetCurrencies();
            e.ShowOtherValue = false;
            e.InterfaceType = interfaceType;
            return e;
        }

        #endregion

        #region SOC	NOT USED
        //public static List<CodeItem> Occupation_Search( int headerId, string keyword, int pageNumber, int pageSize, ref int totalRows, bool getAll = true )
        //{
        //    //return CodesManager.SOC_Search( headerId, keyword, pageNumber, pageSize,  ref totalRows, getAll );
        //    return CodesManager.ReferenceFramework_SearchInUse( 11, 1, headerId.ToString(), keyword, pageNumber, pageSize, ref totalRows );
        //}
       
        //public static MC.Enumeration SOC_Categories_Enumeration( bool getAll = true )
        //{
        //    var data = new List<CodeItem>();
        //    if ( getAll )
        //        data = CodesManager.SOC_Categories();
        //    else
        //    {
        //        //show all until the custom one is fixed
        //        //data = CodesManager.SOC_Categories();
        //        data = CodesManager.SOC_CategoriesInUse();
        //    }

        //    var result = new MC.Enumeration()
        //    {
        //        Id = 11,
        //        Name = "Standard Occupation Codes (SOC)",
        //        Items = ConvertCodeItemsToEnumeratedItems( data )
        //    };
        //    return result;
        //}

        #endregion
        #region NAICS		NOT UISED
        //public static List<CodeItem> Industry_Search( int entityTypeId, int headerId, string keyword, int pageNumber, int pageSize, ref int totalRows, bool getAll = true )
        //{
        //    //int totalRows = 0;
        //    //if ( entityTypeId == 0)
        //    //	return CodesManager.NAICS_Search( headerId, keyword, pageNumber, pageSize, getAll, ref totalRows );
        //    //else
        //    return CodesManager.ReferenceFramework_SearchInUse( 10, entityTypeId, headerId.ToString(), keyword, pageNumber, pageSize, ref totalRows );
        //}
        //public static List<CodeItem> NAICS_Autocomplete( int credentialId, int headerId = 0, string keyword = "", int maxRows = 25 )
        //{
        //	//need a getAll option for this as well!
        //	return CodesManager.NAICS_Autocomplete( headerId, keyword, maxRows );
        //}
        //public static List<CodeItem> NAICS_Categories()
        //{
        //	return CodesManager.NAICS_Categories();
        //}

        /// <summary>
        /// Get all NAICS groups
        /// </summary>
        /// <returns></returns>
        //public static MC.Enumeration NAICS_Categories_Enumeration()
        //{
        //	var data = CodesManager.NAICS_Categories();
        //	var result = new MC.Enumeration()
        //	{
        //		Id = 10,
        //		Name = "North American Industry Classification System (NAICS)",
        //		Items = ConvertCodeItemsToEnumeratedItems( data )
        //	};
        //	return result;
        //}
        //public static MC.Enumeration NAICS_CategoriesInUse_Enumeration( int entityTypeId )
        //{
        //    var data = new List<CodeItem>();
        //    //show all until the custom one is fixed
        //    //data = CodesManager.NAICS_Categories()

        //    data = CodesManager.NAICS_CategoriesInUse( entityTypeId );

        //    var result = new MC.Enumeration()
        //    {
        //        Id = 10,
        //        Name = "North American Industry Classification System (NAICS)",
        //        Items = ConvertCodeItemsToEnumeratedItems( data )
        //    };
        //    return result;
        //}
        #endregion
        #region CIPS NOT USED
        //public static List<CodeItem> CIPS_Search( int entityTypeId, int headerId, string keyword, int pageNumber, int pageSize, ref int totalRows, bool getAll = true )
        //{
        //    //int totalRows = 0;
        //    //if ( entityTypeId == 0 )
        //    //	return CodesManager.CIPS_Search( headerId, keyword, pageNumber, pageSize, ref totalRows, getAll );
        //    //else
        //    return CodesManager.ReferenceFramework_SearchInUse( 23, entityTypeId, headerId.ToString(), keyword, pageNumber, pageSize, ref totalRows );
        //}
        //public static List<CodeItem> CIPS_Search( int headerId = 0, string keyword = "", int pageNumber = 1, int maxRows = 25 )
        //{
        //	int totalRows = 0;
        //	return CodesManager.CIPS_Search( headerId, keyword, pageNumber, maxRows, ref totalRows );
        //}
        //public static List<CodeItem> CIPS_Autocomplete( int credentialId, int headerId = 0, string keyword = "", int maxRows = 25 )
        //{
        //	return CodesManager.CIPS_Autocomplete( headerId, keyword, maxRows );
        //}
        //public static List<CodeItem> CIPS_Categories()
        //{
        //	return CodesManager.CIPS_Categories();
        //}
        /// <summary>
        /// Get all CIPs groups
        /// </summary>
        /// <returns></returns>
        //public static MC.Enumeration CIPS_Categories_Enumeration()
        //{
        //	var data = CodesManager.CIPS_Categories();
        //	var result = new MC.Enumeration()
        //	{
        //		Id = 23,
        //		Name = "Classification of Instructional Programs (CIP)",
        //		Items = ConvertCodeItemsToEnumeratedItems( data )
        //	};
        //	return result;
        //}
        //public static MC.Enumeration CIPS_CategoriesInUse_Enumeration( int entityTypeId )
        //{
        //    //show all until the custom one is fixed
        //    //var data1 = CodesManager.CIPS_Categories();
        //    var data = CodesManager.CIPS_CategoriesInUse( entityTypeId );

        //    var result = new MC.Enumeration()
        //    {
        //        Id = 23,
        //        Name = "Classification of Instructional Programs (CIP)",
        //        Items = ConvertCodeItemsToEnumeratedItems( data )
        //    };
        //    return result;
        //}
        #endregion

        #region Helpers
        public static List<MC.EnumeratedItem> ConvertCodeItemsToEnumeratedItems( List<CodeItem> input )
        {
            var output = new List<MC.EnumeratedItem>();

            foreach ( var item in input )
            {
                output.Add( new MC.EnumeratedItem()
                {
                    CodeId = item.Id,
                    Id = item.Id,
                    Value = item.Id.ToString(),
                    Name = item.Name,
                    Description = item.Description,
                    SchemaName = item.SchemaName,
                    SortOrder = item.SortOrder,
                    //not necessary here
                    //ReverseTitle = item.ReverseTitle,
                    //ReverseSchemaName = item.ReverseSchemaName,
                    //ReverseDescription = item.ReverseDescription,
                    URL = item.URL
                } );
            }

            return output;
        }
        #endregion
    }
}