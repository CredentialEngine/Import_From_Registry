using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.API
{
    public class QuerySummary
    {
        public string Publisher { get; set; }
        public string PublisherCTID { get; set; }
        public string Organization { get; set; }
        public string DataOwnerCTID { get; set; }
        public string Name { get; set; }
        public string EntityCTID { get; set; }
        public string EntityType { get; set; }
        public string EntitySubType { get; set; }
        public int Id { get; set; }

        public string SubjectWebpage { get; set; }
        public string Description { get; set; }
        public string FinderURL { get; set; }
        public System.DateTime LastUpdated { get; set; }
        public bool IsInPublisher { get; set; }
        //Summary
        public int TotalItems { get; set; }
        //Link Checker Summary 
        public string BadURL { get; set; }
        public string Status { get; set; }
        public string LinkType { get; set; }
        public string Property { get; set; }
        public int URLIssues { get; set; }
        public int URIIssues { get; set; }

    }
}
