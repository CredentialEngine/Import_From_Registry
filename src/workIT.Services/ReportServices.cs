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
			CommonTotals totals = ActivityManager.SiteTotals_Get();
			totals.MainEntityTotals = ReportServices.MainEntityTotals();
			

			//vm.TotalDirectCredentials = list.FirstOrDefault( x => x.Id == 1 ).Totals;
			//vm.TotalOrganizations = list.FirstOrDefault( x => x.Id == 2 ).Totals;
			//vm.TotalQAOrganizations = list.FirstOrDefault( x => x.Id == 99 ).Totals;
			totals.AgentServiceTypes = new EnumerationServices().GetOrganizationServices(EnumerationType.MULTI_SELECT, false);

			totals.PropertiesTotals = ReportServices.PropertyTotals();
			totals.PropertiesTotalsByEntity = CodesManager.Property_GetTotalsByEntity();

			totals.SOC_Groups = CodesManager.SOC_Categories();
			//totals.NAICs_Groups = CodesManager.NAICS_Categories();
			//totals.CIP_Groups = CodesManager.CIPS_Categories();
			totals.PropertiesTotals.AddRange( CodesManager.SOC_Categories() );
			totals.PropertiesTotals.AddRange( CodesManager.NAICS_Categories() );
			totals.PropertiesTotals.AddRange( CodesManager.CIPS_Categories() );

			return totals;
		}

		/// <summary>
		/// Get Entity Codes with totals for Credential, Organization, assessments, and learning opp
		/// </summary>
		/// <returns></returns>
		public static List<CodeItem> MainEntityTotals()
		{
			List<CodeItem> list = CodesManager.CodeEntity_GetMainClassTotals();

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
	}
}
