using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
//using System.Web.Mvc;

using CredentialFinderWebAPI.Models;

using workIT.Models.Helpers.Reports;
using workIT.Services;
using workIT.Utilities;

namespace CredentialFinderWebAPI.Controllers
{
    public class ReportsController : BaseController
	{
		// GET: Reports
		[HttpPost, Route( "Report/stats" )]
		public ApiResponse Index()
        {
			try
			{
				var report = ReportServices.APISiteTotals();

				return new ApiResponse( report, true, null );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "CredentialFinderWebAPI.Reports. " + ex.Message  );

				return new ApiResponse( new List<Statistic>(), false, new List<string>() { string.Format( "Error encountered returning data. {0} ", ex.Message ) } );
			}			
        }

		// GET: Reports
		[HttpGet, Route( "Reports/stats" )]
		public void Get()
		{
			var response = new ApiResponse();

			try
			{
				var report = ReportServices.APISiteTotals();

				response.Successful = true;
				response.Result = report;
				SendResponse( response );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "CredentialFinderWebAPI.Reports. " + ex.Message );

				response.Messages.Add( string.Format( "Error encountered returning data. {0} ", ex.Message ) );
				response.Successful = false;
				SendResponse( response );
			}
		}

		[HttpGet, Route( "Reports/benchmarks" )]
		public void BenchmarkReportIndex( )
		{
			string classType = "Credential";
			BenchmarkReport( classType );
		}

		[HttpGet, Route( "Reports/benchmarks/{classType}" )]
		public void BenchmarkReport( string classType = "" )
		{
			//TODO - consider using the entityTypeId, while not CTDL
			if ( string.IsNullOrWhiteSpace( classType ) )
				classType = "Credential";

			classType = char.ToUpper( classType[ 0 ] ) + classType.Substring( 1 );

			ActivityServices.SiteActivityAdd( "Reports", "View", "BenchmarkReport: " + classType, string.Format( "User viewed Benchmark report for : {0}", classType ), 0, 0, 0 );

			//ViewBag.ClassType = ( classType ?? "" );
			var response = new ApiResponse();

			try
			{

				//check for saved classType
				//var classType = ( string )Session[ "FinderPropertiesCountClass" ];
				/*	*/
				//probably use a class
				BenchmarkQuery request = new BenchmarkQuery()
				{
					SearchType = classType
				};
				var report = ReportServices.APIBenchmarksSummary( request );

				var output = new BenchmarkQueryResult()
				{
					BenchmarkType = request.SearchType
				};
				output.Benchmarks = report;
				//var orderDir = "asc";
				//var where = "";
				//bool isDescending = orderDir == "desc" ? true : false;
				//var list = ReportServices.Search( classType, where, orderBy, isDescending, pageNbr, pageSize, ref totalRecords );

				response.Successful = true;
				response.Result = output;
				SendResponse( response );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "CredentialFinderWebAPI.Reports. " + ex.Message );

				response.Messages.Add( string.Format( "Error encountered returning data. {0} ", ex.Message ) );
				response.Successful = false;
				SendResponse( response );
			}
		}
	}
}