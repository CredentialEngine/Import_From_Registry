using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.Entity;
using System.Linq;
using System.Web;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.Helpers.Reports;
using workIT.Utilities;

namespace workIT.Factories
{
	public class ReportsManager : BaseFactory
	{
		private static string thisClassName = "ReportsManager";

		public static List<BenchmarkPropertyTotal> Search( string classType, string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows, int userId = 0 )
		{
			string connectionString = DBConnectionRO();
			var item = new BenchmarkPropertyTotal();
			var list = new List<BenchmarkPropertyTotal>();
			var result = new DataTable();
			if ( string.IsNullOrWhiteSpace( classType ) )
				classType = "credential";

			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				if ( string.IsNullOrEmpty( pFilter ) )
				{
					pFilter = string.Empty;
				}

				using ( SqlCommand command = new SqlCommand( "ClassPropertyCountsSearch", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@ClassType", classType ) );
					command.Parameters.Add( new SqlParameter( "@Filter", pFilter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", pOrderBy ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );

					SqlParameter totalRows = new SqlParameter( "@TotalRows", pTotalRows );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					using ( SqlDataAdapter adapter = new SqlDataAdapter() )
					{
						adapter.SelectCommand = command;
						adapter.Fill( result );
					}
					string rows = command.Parameters[ 5 ].Value.ToString();
					try
					{
						pTotalRows = Int32.Parse( rows );
					}
					catch ( Exception ex )
					{
						pTotalRows = 0;
						LoggingHelper.LogError( ex, thisClassName + string.Format( ".Search() - Execute proc, Message: {0} \r\n Filter: {1} \r\n", ex.Message, pFilter ) );

						item = new BenchmarkPropertyTotal
						{
							Label = "Unexpected error encountered. System administration has been notified. Please try again later. ",
							Policy = ex.Message,
							PropertyGroup = "error"
						};
						list.Add( item );
						return list;
					}
				}

				foreach ( DataRow dr in result.Rows )
				{
					item = new BenchmarkPropertyTotal();
					item.DefaultOrder = GetRowColumn( dr, "Id", 0 );
					item.Label = GetRowColumn( dr, "Label", string.Empty );
					item.Policy = GetRowColumn( dr, "Policy", string.Empty );
					item.PropertyGroup = GetRowColumn( dr, "PropertyGroup", string.Empty );
					item.Total = GetRowColumn( dr, "Total", 0 );
					item.PercentOfOverallTotal = GetRowPossibleColumn( dr, "PercentOfOverallTotal", 0M );
					list.Add( item );
				}
				return list;
			}
		}
	}
}
