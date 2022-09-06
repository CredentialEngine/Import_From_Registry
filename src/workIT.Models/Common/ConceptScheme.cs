using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.ProfileModels;

namespace workIT.Models.Common
{
	public class ConceptScheme : TopLevelObject
	{

		public Guid PublisherUid { get; set; }
		public List<Guid> Creator { get; set; } = new List<Guid>();
		public List<Guid> OwnedBy { get; set; } = new List<Guid>();
		public List<OrganizationRoleProfile> OrganizationRole { get; set; } = new List<OrganizationRoleProfile>();

		public string Source { get; set; }
		public bool IsProgressionModel { get; set; }
		public string PublicationStatusType { get; set; } = "Published";

		public int TotalConcepts { get; set; }
		/// <summary>
		/// Top Concepts - list of CTIDs
		/// </summary>
		public List<Concept> TopConcepts { get; set; } = new List<Concept>();


		/// <summary>
		/// Has Concepts - list of all Concepts
		/// </summary>
		public List<Concept> HasConcepts { get; set; } = new List<Concept>();

		//public bool IsApproved { get; set; }

		//public string CredentialRegistryId { get; set; }
		//public int LastPublishedById { get; set; }
		//public bool IsPublished { get; set; }
		public string ConceptsStore { get; set; }

		public List<Pathway> Pathways { get; set; } = new List<Pathway>();
		public List<string> HasPathway { get; set; } = new List<string>();
	}
	public class ConceptSchemeSummary : ConceptScheme
	{
	}

	public class Concept : BaseObject
	{
		/// <summary>
		/// CTID - identifier for concept. 
		/// Format: ce-UUID (lowercase)
		/// example: ce-a044dbd5-12ec-4747-97bd-a8311eb0a042
		/// </summary>
		public string CTID { get; set; }


		/// <summary>
		/// Concept 
		/// Required
		/// </summary>
		public string PrefLabel { get; set; }

		/// <summary>
		/// Alternately can provide a language map
		/// </summary>
		public LanguageMap PrefLabel_Map { get; set; } = new LanguageMap();

		/// <summary>
		/// Concetpt description 
		/// Required
		/// </summary>
		public string Definition { get; set; }

		/// <summary>
		/// Alternately can provide a language map
		/// </summary>
		public LanguageMap Definition_Map { get; set; } = new LanguageMap();

		public List<string> Notes { get; set; } = new List<string>();
		public string Note { get; set; } 
		public bool IsTopConcept { get; set; }
		/// <summary>
		/// If 
		/// </summary>
		public string topConceptOf { get; set; }

		public string inScheme { get; set; }

		/// <summary>
		/// Last modified date for concept
		/// </summary>
		public string dateModified { get; set; }

	}
}
