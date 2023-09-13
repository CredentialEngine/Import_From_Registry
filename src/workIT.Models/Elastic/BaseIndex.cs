using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;

using workIT.Models.Common;

namespace workIT.Models.Elastic
{

	public class BaseIndex
	{
		public int EntityTypeId { get; set; }
		public string EntityType { get; set; }
		/// <summary>
		/// Need EntityId for the common index as the unique Id
		/// </summary>
		public int EntityId { get; set; }
		public int Id { get; set; }
		public Guid RowId { get; set; }
		public string CTID { get; set; }
		public int EntityStateId { get; set; }
		public string CredentialRegistryId { get; set; }
		public DateTime IndexLastUpdated { get; set; } = DateTime.Now;

		public string NameOrganizationKey { get; set; }
		public string Name { get; set; }
		public string ListTitle { get; set; }
		//TBD
		public string NameAlphanumericOnly { get; set; }
		//
		public string FriendlyName { get; set; }
		public string Description { get; set; }
		public string SubjectWebpage { get; set; }

		public DateTime Created { get; set; }
		public DateTime LastUpdated { get; set; }
		public string StatusMessage { get; set; }
		public List<string> InLanguage { get; set; } = new List<string>();

		public int PrimaryOrganizationId { get; set; }

		public string PrimaryOrganizationName { get; set; }

		public string PrimaryOrganizationCTID { get; set; }
		public int PublishedByThirdPartyOrganizationId { get; set; }

		public List<int> AgentRelationships { get; set; } = new List<int>();
		public List<AgentRelationshipForEntity> AgentRelationshipsForEntity { get; set; } = new List<AgentRelationshipForEntity>();
		public List<AgentRelationshipForEntity> OutcomeProvidersForEntity { get; set; } = new List<AgentRelationshipForEntity>();

		public List<string> CodedNotation { get; set; } = new List<string>();
		//
		public int? PublishedByOrganizationId { get; set; }
		//don't include the name, not typically something we would to get filter hits on. 
		//public string PublishedByOrganizationName { get; set; }

		public string PublishedByOrganizationCTID { get; set; }
		//

		public List<Address> Addresses { get; set; } = new List<Elastic.Address>();
		//public List<string> Locations { get; set; } = new List<string>();
		public List<string> Cities { get; set; } = new List<string>();
		public List<string> Regions { get; set; } = new List<string>();
		public List<string> Countries { get; set; } = new List<string>();
		/// <summary>
		/// Identifier - primarily for region identifiers like LWIA, etc
		/// </summary>
		public List<IdentifierValue> RegionIdentifier { get; set; } = new List<IdentifierValue>();
		public List<string> LWIAList { get; set; } = new List<string>();
		public List<string> EDRList { get; set; } = new List<string>();
		//
		public List<string> Keyword { get; set; } = new List<string>();
		//
		public List<IndexCompetency> Competencies { get; set; } = new List<IndexCompetency>();

		public List<string> Collection { get; set; } = new List<string>();
		//
		public List<IndexReferenceFramework> Industries { get; set; } = new List<IndexReferenceFramework>();
		/// <summary>
		/// Occupations are used with Occupation searches from the filter box
		/// </summary>
		public List<IndexReferenceFramework> Occupations { get; set; } = new List<IndexReferenceFramework>();
		public List<IndexReferenceFramework> InstructionalPrograms { get; set; } = new List<IndexReferenceFramework>();
		public List<string> Industry { get; set; } = new List<string>();
		/// <summary>
		/// Occupation list is used with keyword filters
		/// </summary>
		public List<string> Occupation { get; set; } = new List<string>();
		public List<string> InstructionalProgram { get; set; } = new List<string>();
		public List<string> QualityAssurancePhrase { get; set; } = new List<string>();

		public List<string> SubjectAreas { get; set; } = new List<string>();
		/*TextValues can include:
		 * ===Credential
		 * AgentRelationship  xxx yyy (Microsoft accredits)
		 * AvailabilityListing
		* AvailableOnlineAt
		* city
		* region
		* CodedNotation
		* CodedNotation without any dashes
		* CredentialRegistryId (why, seemed like a good idea at the time)
		* CTID
		* Keywords

		* CredentialType
		* CredentialId
		* 
		* industry
		* occupation
		* program
		* PropertySchemaName [NOT property for now]
		* TextValue from proc
		* 
		* ===Assessment
		* AgentRelationship  xxx yyy (Microsoft accredits)
		* AssessmentExampleUrl
		* AvailabilityListing
		* AvailableOnlineAt
		* city
		* CodedNotation
		* CredentialRegistryId
		* CTID
		* ExternalResearch
		* id
		* Keywords
		* ProcessStandards
		* property (added 20-07-23)
		* PropertySchemaName
		* region
		* ScoringMethodExample
		* TextValue from proc

		* ===Learning Opp
		* AgentRelationship  xxx yyy (Microsoft accredits)
		* AvailabilityListing
		* AvailableOnlineAt
		* city
		* CodedNotation
		* CredentialRegistryId
		* CTID
		* id
		* Keywords
		* property (added 20-07-23)
		* PropertySchemaName
		* region
		* TextValue from proc
		* 
		* 
		* ===Organization
		* AlternateIdentifiers
		* AlternatesNames
		* AvailabilityListing
		* ClaimTypes
		* city
		* CTID
		* HasVerificationService (20-07-24 removed orgReport:)
		* HasNoVerificationService (20-07-24 removed orgReport:)
		* Keywords
		* Property
		* PropertySchema
		* region
		 */
		public List<string> TextValues { get; set; } = new List<string>();
		//
		public List<int> ReportFilters { get; set; } = new List<int>();
		//will need more, for each of the types of list:
		//provider, QA, entity list
		//although could just handle lists for now
		public List<int> ResourceForWidget {get; set; } = new List<int>();
		//OR
		public List<IndexWidgetTag> WidgetTags { get; set; } = new List<IndexWidgetTag>();

		public List<int> ResourceForCollection { get; set; } = new List<int>();
		public List<int> ResourceInTransferValue { get; set; } = new List<int>();
        public List<int> ResourceHasSupportService { get; set; } = new List<int>();
        
        /// <summary>
        /// Hold all detail for a resource. 
        /// Make sure it is non-indexed
        /// </summary>
        public JObject ResourceDetail { get; set; }
    }
}
