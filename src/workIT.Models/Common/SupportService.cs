using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.ProfileModels;

namespace workIT.Models.Common
{
    public class SupportService : TopLevelObject
    {
        public SupportService()
        {
            EntityTypeId = 38;
            EntityType = "SupportService";
        }

        public List<ResourceSummary> OwnedBy { get; set; }
        /// <summary>
        /// Organization(s) that offer this resource
        /// </summary>
        public List<ResourceSummary> OfferedBy { get; set; }

        public List<OrganizationRoleProfile> OrganizationRole { get; set; }


        /// <summary>
        /// Type of official status of this resource. Select a valid concept from the LifeCycleStatus concept scheme.
        /// Provide the string value. API will format correctly. The name space of lifecycle doesn't have to be included
        /// Required
        /// lifecycle:Developing, lifecycle:Active", lifecycle:Suspended, lifecycle:Ceased
        /// <see href="https://credreg.net/ctdl/terms/LifeCycleStatus">ceterms:LifeCycleStatus</see>
        /// </summary>
        public Enumeration LifeCycleStatusType { get; set; } = new Enumeration();
        public int LifeCycleStatusTypeId { get; set; } 
        /// <summary>
        /// Type of modification to facilitate equal access for people to a physical location, resource, or service.
        /// Accomodation?
        /// <see href="https://credreg.net/ctdl/terms/Accomodation"></see>
        /// </summary>
        public Enumeration AccommodationType { get; set; } = new Enumeration();

        /// <summary>
        /// Online location where the credential, assessment, or learning opportunity can be pursued.
        /// URL
        /// </summary>
        public List<string> AvailableOnlineAt { get; set; } = new List<string> { };

        /// <summary>
        /// Listing of online and/or physical locations where a credential can be pursued.
        /// URL
        /// </summary>
        public List<string> AvailabilityListing { get; set; } = new List<string> { };

        /// <summary>
        /// Physical location where the credential, assessment, or learning opportunity can be pursued.
        /// Place
        /// </summary>
        public List<Address> AvailableAt { get; set; }

        //=========== optional ================================
        /// <summary>
        /// List of Alternate Names for this resource
        /// </summary>
        public List<string> AlternateName { get; set; } = new List<string>();

        /// <summary>
        /// List of CTIDs or full URLs for a ConditionManifest published by the owning organization
        /// Set constraints, prerequisites, entry conditions, or requirements that are shared across an organization, organizational subdivision, set of credentials, or category of entities and activities.
        /// </summary>
        public List<ConditionManifest> CommonConditions { get; set; }


        /// <summary>
        /// List of CTIDs (recommended) or full URLs for a CostManifest published by the owning organization.
        /// Set of costs maintained at an organizational or sub-organizational level, which apply to this learning opportunity.
        /// </summary>
		public List<CostManifest> CommonCosts { get; set; }


        ///// <summary>
        ///// Start Date of this resource
        ///// </summary>
        //public string DateEffective { get; set; }

        /// <summary>
        /// Type of means by which a learning opportunity or assessment is delivered to credential seekers and by which they interact; select from an existing enumeration of such types.
        /// deliveryType:BlendedDelivery deliveryType:InPerson deliveryType:OnlineOnly
        /// <see href="https://credreg.net/ctdl/terms/Delivery"></see>
        /// </summary>
        public Enumeration DeliveryType { get; set; } = new Enumeration();

        /// <summary>
        /// Estimated cost of a credential, learning opportunity or assessment.
        /// </summary>
        public List<CostProfile> EstimatedCost { get; set; }

        ///// <summary>
        ///// End date of the learning opportunity if applicable
        ///// </summary>
        //public string ExpirationDate { get; set; }

        /// <summary>
        /// Entity that describes financial assistance that is offered or available.
        /// </summary>
        public List<FinancialAssistanceProfile> FinancialAssistance { get; set; } = new List<FinancialAssistanceProfile>();

        public List<TopLevelObject> SupportServiceReferencedBy { get; set; } = new List<TopLevelObject>();

        public List<TopLevelObject> IsSpecificServiceOf { get; set; } = new List<TopLevelObject>();
        public List<TopLevelObject> HasSpecificService { get; set; } = new List<TopLevelObject>();

        //
        /// <summary>
        /// Alphanumeric token that identifies this resource and information about the token's originating context or scheme.
        /// <see href="https://purl.org/ctdl/terms/identifier"></see>
        /// ceterms:identifier
        /// </summary>
        public List<IdentifierValue> Identifier { get; set; } = new List<IdentifierValue>();
        public string IdentifierJSON { get; set; }

        /// <summary>
        /// The primary language or languages of the entity, even if it makes use of other languages; e.g., a course offered in English to teach Spanish would have an inLanguage of English, while a credential in Quebec could have an inLanguage of both French and English.
        /// List of language codes. ex: en, es
        /// </summary>
        public List<TextValueProfile> InLanguage { get; set; }

        /// <summary>
        /// Keyword or key phrase describing relevant aspects of an entity.
        /// </summary>
        public List<string> Keyword { get; set; } = new List<string> { };



        #region Occupation type
        /// <summary>
        /// OccupationType
        /// Type of occupation; select from an existing enumeration of such types.
        ///  For U.S. credentials, best practice is to identify an occupation using a framework such as the O*Net. 
        ///  Other credentials may use any framework of the class ceterms:OccupationClassification, such as the EU's ESCO, ISCO-08, and SOC 2010.
        /// </summary>
        public List<CredentialAlignmentObjectProfile> OccupationType { get; set; } = new List<CredentialAlignmentObjectProfile>();



        #endregion

        /// <summary>
        /// List of Organizations that offer this learning opportunity in a specific Jurisdiction. 
        /// </summary>
        public List<JurisdictionProfile> OfferedIn { get; set; }

        /// <summary>
        /// Qualifying requirements for receiving a support service.
        /// </summary>
        public List<ConditionProfile> SupportServiceCondition { get; set; } = new List<ConditionProfile>();

        /// <summary>
        /// Resource to which this support service is applicable.
        /// NOTE: LIKELY AN INVERSE PROPERTY THAT WILL NOT BE USED HERE!
        /// </summary>
        public List<ResourceSummary> SupportServiceFor { get; set; } = new List<ResourceSummary>();

        /// <summary>
        /// Types of support services offered by an agent; select from an existing enumeration of such types.
        /// SupportService?
        /// <see href="https://credreg.net/ctdl/terms/SupportServiceType"></see>
        /// </summary>
        public Enumeration SupportServiceType { get; set; } = new Enumeration { };


        ///// <summary>
        ///// Types of methods used to conduct the learning opportunity; 
        ///// Concepts: Applied, Gaming, Laboratory, Lecture, Prerecorded, SelfPaced, Seminar, WorkBased
        ///// ConceptScheme: <see href="https://credreg.net/ctdl/terms/LearningMethod">LearningMethod</see>
        ///// </summary>
        //public Enumeration LearningMethodType { get; set; } = new Enumeration { };

        #region Import 
        public List<int> CostManifestIds { get; set; }
        public List<int> ConditionManifestIds { get; set; }
        public List<int> HasSpecificServiceIds { get; set; } = new List<int>();
        public List<int> IsSpecificServiceOfIds { get; set; } = new List<int>();
        public List<Guid> OfferedByList { get; set; }
        public List<Guid> OwnedByList { get; set; } = new List<Guid>();

        #endregion
    }

    public class SupportServiceSummary : SupportService
    {
        public SupportServiceSummary() { }
    }

    public class Entity_HasSupportService
    {
        public int Id { get; set; }
        public int EntityId { get; set; }
        public int SupportServiceId { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public int CreatedById { get; set; }

        public virtual Entity Entity { get; set; }
        public virtual SupportService SupportService { get; set; }
    }
    public class Entity_IsPartOfSupportService
    {
        public int Id { get; set; }
        public int EntityId { get; set; }
        public int SupportServiceId { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public int CreatedById { get; set; }

        public virtual Entity Entity { get; set; }
        public virtual SupportService SupportService { get; set; }
    }
}
