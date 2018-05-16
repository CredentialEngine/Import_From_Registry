using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
	public class OrganizationSummary : Organization
	{
		public OrganizationSummary()
		{
            QualityAssurance = new AgentRelationshipResult();

        }

		public int QARolesCount { get; set; }
		public List<string> Subjects { get; set; }
        public CodeItemResult NaicsResults { get; set; } = new CodeItemResult();
		//public CodeItemResult IndustryOtherResults { get; set; }

		public CodeItemResult OwnedByResults { get; set; } = new CodeItemResult();
        public CodeItemResult OfferedByResults { get; set; } = new CodeItemResult();
        public CodeItemResult AsmtsOwnedByResults { get; set; } = new CodeItemResult();
        public CodeItemResult LoppsOwnedByResults { get; set; } = new CodeItemResult();

        public CodeItemResult AccreditedByResults { get; set; } = new CodeItemResult();
        public CodeItemResult ApprovedByResults { get; set; } = new CodeItemResult();
        public CodeItemResult RecognizedByResults { get; set; } = new CodeItemResult();
        public CodeItemResult RegulatedByResults { get; set; } = new CodeItemResult();
        //public CredentialConnectionsResult OwnedByResults { get; set; }
        //public CredentialConnectionsResult OfferedByResults { get; set; }

        public AgentRelationshipResult AgentAndRoles { get; set; }
        public AgentRelationshipResult QualityAssurance { get; set; }
    }

}
