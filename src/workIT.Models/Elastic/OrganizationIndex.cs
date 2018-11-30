using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nest;

namespace workIT.Models.Elastic
{
    public class OrganizationIndex : IIndex
    {
        public OrganizationIndex()
        {
            EntityTypeId = 2;
            AlternateNames = new List<string>();
            Keyword = new List<string>();
            //Industries = new List<CredentialFramework>();
            IndustryCodeGroups = new List<string>();
            IndustryCodeNotations = new List<string>();
            OrganizationServiceTypeIds = new List<int>();
            OrganizationTypeIds = new List<int>();
            Addresses = new List<Address>();
            TextValues = new List<string>();
            PropertyValues = new List<int>();
            Codes = new List<int>();
            AgentRelationships = new List<int>();
            //ReferenceFrameworks = new List<OrganizationFramework>();
        }
        public int EntityTypeId { get; set; } = 2;
        public string FriendlyName { get; set; }
        public int Id { get; set; }
        public int OwnerOrganizationId
        {
            get
            {
                return Id;
            }
            set
            {
                Id = value;
            }
        }
        public DateTime IndexLastUpdated { get; set; } = DateTime.Now;
        public int NameIndex { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid RowId { get; set; }
        public string SubjectWebpage { get; set; }
        public string CTID { get; set; }
        public string ImageURL { get; set; }
        public int EntityStateId { get; set; }
        public string CredentialRegistryId { get; set; }

        public DateTime Created { get; set; }

        public DateTime LastUpdated { get; set; }

        public bool IsACredentialingOrg { get; set; }

        /// <summary>
        /// True if org does QA
        /// </summary>
        public bool ISQAOrganization { get; set; }

        public int AddressesCount { get; set; }
        //TBD
        //public List<AddressLocation> AddressLocations { get; set; }
        public List<Address> Addresses { get; set; } = new List<Elastic.Address>();

        public List<int> OrganizationTypeIds { get; set; }
        public List<IndexProperty> OrganizationTypes { get; set; } = new List<IndexProperty>();

        public List<int> OrganizationServiceTypeIds { get; set; }
        public List<IndexProperty> OrganizationServiceTypes { get; set; } = new List<IndexProperty>();

        //actually only one sector type
        // public int OrganizationSectorTypeId { get; set; }
        public List<int> OrganizationSectorTypeIds { get; set; } = new List<int>();
        public List<IndexProperty> OrganizationSectorTypes { get; set; } = new List<IndexProperty>();

        public List<int> OrganizationClaimTypeIds { get; set; } = new List<int>();
        public List<IndexProperty> OrganizationClaimTypes { get; set; } = new List<IndexProperty>();

        //note this is already included in TextValue
        public List<string> AlternateNames { get; set; }

        public List<string> Keyword { get; set; }

		/// <summary>
		/// Source will be Entity.SearchIndex, including:
		/// NAICS        
		/// Keyword
		/// PLUS:
		/// - Alternate name
		/// - AlternateIdentifiers
		/// - Id
		/// - CredentialRegistryId
		/// - CTID
		/// - ServiceTypes, OrgTypes, SectorTypes, ClaimTypes 
		/// - QA role by QA Org name
		/// </summary>
		public List<string> TextValues { get; set; } = new List<string>();
		public List<string> PremiumValues { get; set; } = new List<string>();

		//public List<IndexReferenceFramework> Industries { get; set; }

		//public string NaicsResults { get; set; }
		public List<string> IndustryCodeGroups { get; set; }
        public List<string> IndustryCodeNotations { get; set; }
        //public string IndustryOtherResults { get; set; }
        public List<IndexReferenceFramework> ReferenceFrameworks { get; set; } = new List<IndexReferenceFramework>();

        public List<int> PropertyValues { get; set; }
        public List<int> Codes { get; set; }
        public List<int> AgentRelationships { get; set; }
        //public List<int> TargetAssertion { get; set; }
        //public string MainPhoneNumber { get; set; }

        public string OwnedByResults { get; set; }
        public string OfferedByResults { get; set; }
        public string AsmtsOwnedByResults { get; set; }
        public string LoppsOwnedByResults { get; set; }
        public string AccreditedByResults { get; set; }
        public string ApprovedByResults { get; set; }
        public string RecognizedByResults { get; set; }
        public string RegulatedByResults { get; set; }
        public List<IndexQualityAssurance> QualityAssurance { get; set; } = new List<IndexQualityAssurance>();
        public List<IndexQualityAssurancePerformed> QualityAssurancePerformed { get; set; } = new List<IndexQualityAssurancePerformed>();
        //counts
        public int VerificationProfilesCount { get; set; }
        public int CostManifestsCount { get; set; }
        public int ConditionManifestsCount { get; set; }
        public int SubsidiariesCount { get; set; }
        public int DepartmentsCount { get; set; }
        public int HasIndustriesCount { get; set; }
        public List<int> ReportFilters { get; set; } = new List<int>();

    }

}
