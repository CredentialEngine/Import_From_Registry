using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;

namespace workIT.Models.ProfileModels
{
    public class LearningOpportunity_Summary : BaseObject
    {
        public LearningOpportunity_Summary()
        {
            OwningOrganization = new Organization();


            DeliveryType = new Enumeration();
            InstructionalProgramType = new Enumeration();

         
            OrganizationRole = new List<OrganizationRoleProfile>();
            WhereReferenced = new List<string>();
            Subject = new List<string>();
            Keyword = new List<string>();
            Addresses = new List<Address>();

            LearningMethodType = new Enumeration();
            OwnerRoles = new Enumeration();

            InLanguageCodeList = new List<string>();
            VersionIdentifierList = new List<Entity_IdentifierValue>();

            //not sure
            EstimatedCost = new List<CostProfile>();
            EstimatedDuration = new List<DurationProfile>();
        }

        public string Name { get; set; }
        public string FriendlyName { get; set; }
        public string SubjectWebpage { get; set; }

        public int EntityStateId { get; set; }
        public string CredentialRegistryId { get; set; }
        public string CTID { get; set; }

        /// <summary>
        /// Single is the primary for now
        /// </summary>
        public string VersionIdentifier { get; set; }
        /// <summary>
        /// Also doing import of list
        /// </summary>
        public List<Entity_IdentifierValue> VersionIdentifierList { get; set; }

        public string AvailableOnlineAt { get; set; }

        /// <summary>
        /// OwningAgentUid
        ///  (Nov2016)
        /// </summary>
        public Guid OwningAgentUid { get; set; }
        /// <summary>
        /// Inflate OwningAgentUid for display 
        /// </summary>
        public Organization OwningOrganization { get; set; }
        public string OrganizationName
        {
            get
            {
                if (OwningOrganization != null && OwningOrganization.Id > 0)
                    return OwningOrganization.Name;
                else
                    return "";
            }
        }
        public int OwningOrganizationId
        {
            get
            {
                if (OwningOrganization != null && OwningOrganization.Id > 0)
                    return OwningOrganization.Id;
                else
                    return 0;
            }
        }

        //???
        public Enumeration OwnerRoles { get; set; }
        //???
        public List<OrganizationRoleProfile> OrganizationRole { get; set; }

        /// <summary>
        /// CodedNotation replaces IdentificationCode
        /// </summary>
        public string CodedNotation { get; set; }

        public List<string> InLanguageCodeList { get; set; }


        //public string CreditHourType { get; set; }
        //public decimal CreditHourValue { get; set; }
        //public Enumeration CreditUnitType { get; set; } //Used for publishing
        //public int CreditUnitTypeId { get; set; }
        //public string CreditUnitTypeDescription { get; set; }
        //public decimal CreditUnitValue { get; set; }

        public List<DurationProfile> EstimatedDuration { get; set; }

        public Enumeration DeliveryType { get; set; }

        public Enumeration InstructionalProgramType { get; set; }

        public Enumeration LearningMethodType { get; set; }

        public List<CostProfile> EstimatedCost { get; set; }
 

        //simplify
        public List<CredentialAlignmentObjectProfile> InstructionalProgramTypes { get; set; }

        //these may be combined
        public List<string> Keyword { get; set; }
        public List<string> Subject { get; set; }

        public List<string> WhereReferenced { get; set; }
        //for addresses, follow credential, use a count, and store lat, lng and full region names in index
        public List<Address> Addresses { get; set; }
        public string AvailabilityListing { get; set; }

        //may want some indicator of having condition profiles?
        //will need to build a cache table, or check performance with direct joins - OK if a 'little' slow for the build, or update steps
        public int HasPartCount { get; set; }
        public int IsPartOfCount { get; set; }
        public int RequiresCount { get; set; }
        public int RecommendsCount { get; set; }
        public int RequiredForCount { get; set; }
        public int IsRecommendedForCount { get; set; }
        public int RenewalCount { get; set; }
        public int IsAdvancedStandingForCount { get; set; }
        public int AdvancedStandingFromCount { get; set; }
        public int PreparationForCount { get; set; }
        public int PreparationFromCount { get; set; }

        public int TeachesCompetenciesCount { get; set; }
        public int RequiresCompetenciesCount { get; set; }
        //public List<CredentialAlignmentObjectProfile> TeachesCompetencies { get; set; }
        public AgentRelationshipResult AgentAndRoles { get; set; }



    }
    //

}
