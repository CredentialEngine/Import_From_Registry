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

	}
}
