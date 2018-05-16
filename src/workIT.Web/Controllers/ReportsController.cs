﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using workIT.Models.Helpers.Reports;
using workIT.Services;

namespace WorkIT.Web.Controllers
{
    public class ReportsController : Controller
    {
        // GET: Report
        public ActionResult Index()
        {
			CommonTotals vm = new CommonTotals();
			

			vm = ReportServices.SiteTotals();

			return View( "~/Views/Reports/Reports.cshtml", vm );
        }
    }
}