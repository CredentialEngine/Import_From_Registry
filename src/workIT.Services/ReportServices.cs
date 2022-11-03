using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Helpers.Reports;

namespace workIT.Services
{
	public class ReportServices
	{
		public static CommonTotals SiteTotals()
		{
            var currentDate = DateTime.Now;
            currentDate = currentDate.AddDays( -2 );
			CommonTotals totals = ActivityManager.SiteTotals_Get();
			totals.MainEntityTotals = MainEntityTotals();
            totals.CredentialHistory = HistoryReports( 1);
            totals.OrganizationHistory = HistoryReports( 2 );
            totals.AssessmentHistory = HistoryReports( 3 );
            totals.LearningOpportunityHistory = ReportServices.HistoryReports( 7 );

			totals.EntityRegionTotals = CodesManager.GetEntityRegionTotals( 1, "United States");
			//vm.TotalDirectCredentials = list.FirstOrDefault( x => x.Id == 1 ).Totals;
			//vm.TotalOrganizations = list.FirstOrDefault( x => x.Id == 2 ).Totals;
			//vm.TotalQAOrganizations = list.FirstOrDefault( x => x.Id == 99 ).Totals;
			totals.AgentServiceTypes = new EnumerationServices().GetOrganizationServices(EnumerationType.MULTI_SELECT, false);

			totals.PropertiesTotals = PropertyTotals();
			//
			//get totals from view: CodesProperty_Counts_ByEntity.
			//	the latter has a union with Counts.SiteTotals
			totals.PropertiesTotalsByEntity = CodesManager.Property_GetTotalsByEntity();
            totals.PropertiesTotals.AddRange( CodesManager.GetAllEntityStatistics());
			totals.PropertiesTotals.AddRange( CodesManager.GetAllPathwayComponentStatistics() );

			
			//using counts.SiteTotals - so based on the above, this should not be needed???
			//var allSiteTotals = CodesManager.CodeEntity_GetCountsSiteTotals();
			//totals.SOC_Groups = allSiteTotals.Where( s => s.EntityTypeId == 1 && s.CategoryId == 11 ).ToList();
			//totals.CredentialIndustry_Groups = allSiteTotals.Where( s => s.EntityTypeId == 1 && s.CategoryId == 10 ).ToList();
			//totals.CredentialCIP_Groups = allSiteTotals.Where( s => s.EntityTypeId == 3 && s.CategoryId == 23 ).ToList();
			//totals.OrgIndustry_Groups = allSiteTotals.Where( s => s.EntityTypeId == 2 && s.CategoryId == 10 ).ToList();
			//totals.AssessmentCIP_Groups = allSiteTotals.Where( s => s.EntityTypeId == 3 && s.CategoryId == 23 ).ToList();
			//totals.LoppCIP_Groups = allSiteTotals.Where( s => s.EntityTypeId == 7 && s.CategoryId == 23 ).ToList();

			return totals;
		}

        public static List<HistoryTotal> HistoryReports( int entityTypeId )
        {
            var result = CodesManager.GetHistoryTotal( entityTypeId );
            return result;


        }

		/// <summary>
		/// Get Entity Codes with totals for top level entities like: Credential, Organization, assessments, and learning opp
		/// </summary>
		/// <returns></returns>
		public static List<CodeItem> MainEntityTotals( bool gettingAll = true )
		{
			List<CodeItem> list = CodesManager.CodeEntity_GetTopLevelEntity( gettingAll );

			return list;
		}

		/// <summary>
		/// Get property totals, by category or all active properties
		/// </summary>
		/// <param name="categoryId"></param>
		/// <returns></returns>
		public static List<CodeItem> PropertyTotals( int categoryId = 0)
		{
			List<CodeItem> list = CodesManager.Property_GetSummaryTotals( categoryId );

			return list;
		}

		public static List<BenchmarkPropertyTotal> Search( string classType, string pFilter, string pOrderBy, bool IsDescending, int pageNumber, int pageSize, ref int pTotalRows )
		{

			//probably should validate valid order by - or do in proc
			if ( string.IsNullOrWhiteSpace( pOrderBy ) )
			{
				//not handling desc yet				
				//parms.IsDescending = true;
				pOrderBy = "Id";
			}
			else
			{
				if ( pOrderBy == "Order" )
					pOrderBy = "Id";
				else if ( pOrderBy == "Title" )
					pOrderBy = "Label";
				else if ( pOrderBy == "CodeTitle" )
					pOrderBy = "Policy";
				else if ( pOrderBy == "Group" )
					pOrderBy = "PropertyGroup";
				else if ( pOrderBy == "Totals" )
					pOrderBy = "Total";

				if ( "id label policy propertygroup total".IndexOf( pOrderBy.ToLower() ) == -1 )
				{
					pOrderBy = "Id";
				}
			}
			if ( IsDescending )
				pOrderBy += " DESC";

			var list = ReportsManager.Search( classType, pFilter, pOrderBy, pageNumber, pageSize, ref pTotalRows );
			return list;
		}
	}
}
