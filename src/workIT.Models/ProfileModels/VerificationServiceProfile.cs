using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;

namespace workIT.Models.ProfileModels
{
    [Serializable]
    public class VerificationServiceProfile : TopLevelObject
    {
		public VerificationServiceProfile()
		{
			EstimatedCost = new List<CostProfile>();
			VerifiedClaimType = new Enumeration();
			TargetCredential = new List<Credential>();
		}
        //public string CTID { get; set; }
        //public string SubjectWebpage { get; set; }


        public Guid OfferedByAgentUid { get; set; }
        /// <summary>
        /// Inflate OfferedByAgentUid for display 
        /// </summary>
        public Organization OfferedByAgent { get; set; }
        public List<ResourceSummary> OfferedBy { get; set; } = new List<ResourceSummary>();
        public List<OrganizationRoleProfile> OrganizationRole { get; set; }

        public bool? HolderMustAuthorize { get; set; }
		public List<CostProfile> EstimatedCost { get; set; }
        //public List<CostProfile> EstimatedCosts
        //{ get { return EstimatedCost; } set { EstimatedCost = value; }
        //} //Convenience for publishing



        //for display
        [Obsolete] //??
        public List<Credential> TargetCredential { get; set; }
        //[Obsolete]
        //public string VerificationDirectoryOLD { get; set; }
        public List<string> VerificationDirectory { get; set; }
        public string VerificationMethodDescription { get; set; }
        //[Obsolete]
        //public string VerificationServiceOLD { get; set; }
        public List<string> VerificationService { get; set; }

        public Enumeration VerifiedClaimType { get; set; }



        public List<JurisdictionProfile> Region { get; set; }
		public List<JurisdictionProfile> OfferedIn { get; set; }

        #region Import 
        //for import 
        public List<int> TargetCredentialIds { get; set; }
        public List<Guid> OfferedByList { get; set; }
        #endregion

    }
    public class Entity_UsesVerificationService
    {
        public int Id { get; set; }
        public int EntityId { get; set; }
        public int VerificationServiceId { get; set; }
        public System.DateTime Created { get; set; }

        //TBD if to be used
        public VerificationServiceProfile VerificationServiceProfile { get; set; }

    }
    public class Entity_HasVerificationService : Entity_UsesVerificationService
    {

    }
}
