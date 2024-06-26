using System;
using System.Collections.Generic;

using workIT.Models.Common;

namespace workIT.Models.Elastic
{
    public interface IIndex
    {
        int Id { get; set; }
		string Name { get; set; }
		string NameOrganizationKey { get; set; }
		string ListTitle { get; set; }
		string Description{ get; set; }
		string SubjectWebpage { get; set; }

		//string ListTitle { get; set; }
		int PrimaryOrganizationId { get; set; }
		string PrimaryOrganizationName { get; set; }
		int PublishedByThirdPartyOrganizationId { get; set; }
		int EntityStateId { get; set; }

		bool IsAvailableOnline { get; }

        DateTime Created { get; set; }
		DateTime LastUpdated { get; set; }
		List<string> QualityAssurancePhrase { get; set; }
		List<string> Keyword { get; set; }
		List<AgentRelationshipForEntity> AgentRelationshipsForEntity { get; set; }
		List<AgentRelationshipForEntity> OutcomeProvidersForEntity { get; set; }

		List<IndexCompetency> Competencies { get; set; }

		//List<QualityAssurancePerformed> QualityAssurancePerformed { get; set; }
		List<IndexReferenceFramework> Industries { get; set; }

		List<string> Industry { get; set; }
		//20-10-29 renamed from Classifications
		List<IndexReferenceFramework> InstructionalPrograms { get; set; }
		List<string> InstructionalProgram { get; set; }
		/// <summary>
		/// Occupations are used with Occupation searches from the filter box
		/// </summary>
		List<IndexReferenceFramework> Occupations { get; set; }
		/// <summary>
		/// Occupation list is used with keyword filters
		/// </summary>
		List<string> Occupation { get; set; }

		//
		List<Address> Addresses { get; set; }
		//List<string> Locations { get; set; }
		List<string> Cities { get; set; } 
		List<string> Regions { get; set; } 
		List<string> Countries { get; set; }
		/// <summary>
		/// Identifier - primarily for region identifiers like LWIA, etc
		/// </summary>
		List<IdentifierValue> RegionIdentifier { get; set; }
		List<string> LWIAList { get; set; }
		List<string> EDRList { get; set; }
		List<string> TextValues { get; set; }
		List<string> SubjectAreas { get; set; }
		List<string> PremiumValues { get; set; }
		List<int> ReportFilters { get; set; }
		/// <summary>
		/// List of widget ids that a resource is part of
		/// </summary>
		List<int> ResourceForWidget { get; set; }

        /// <summary>
        /// List of Collection Names a resource is part of
        /// </summary>
        List<string> Collection { get; set; }
        /// <summary>
        /// List of collection ids that a resource is part of
        /// </summary>
        List<int> ResourceForCollection { get; set; }
        List<int> ResourceProvidesTransferValues { get; set; }
		List<int> ResourceReceivesTransferValues { get; set; }
		List<int> ResourceHasSupportService { get; set; }
        List<IndexSubject> Subjects { get; set; }
	}
}
