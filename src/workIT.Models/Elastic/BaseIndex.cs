using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Elastic
{

    public class BaseIndex
	{
		public int EntityTypeId { get; set; } 
		public int Id { get; set; }
		public Guid RowId { get; set; }
		public string CTID { get; set; }
		public int EntityStateId { get; set; }
		public string CredentialRegistryId { get; set; }
		public DateTime IndexLastUpdated { get; set; } = DateTime.Now;


		public string Name { get; set; }
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
		public List<int> AgentRelationships { get; set; } = new List<int>();
		public List<AgentRelationshipForEntity> AgentRelationshipsForEntity { get; set; } = new List<AgentRelationshipForEntity>();
		public List<string> SubjectAreas { get; set; } = new List<string>();
		public List<string> TextValues { get; set; } = new List<string>();

	}
}
