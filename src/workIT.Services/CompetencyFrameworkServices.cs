using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;

using ThisEntity = workIT.Models.Common.CostManifest;
using EntityMgr = workIT.Factories.CostManifestManager;
using workIT.Utilities;
using workIT.Factories;

namespace workIT.Services
{
    public class CompetencyFrameworkServices
    {
        string thisClassName = "CompetencyFrameworkServices";
        ActivityServices activityMgr = new ActivityServices();
        public List<string> messages = new List<string>();
    }
}
