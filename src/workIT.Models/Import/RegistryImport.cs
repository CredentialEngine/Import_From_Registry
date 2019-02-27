using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models
{
	public class RegistryImport
	{
		public RegistryImport()
		{
			this.Messages = new List<Import_Message>();
		}

		public int Id { get; set; }
		public string EnvelopeId { get; set; }
		public int EntityTypedId { get; set; }
		public string Ctid { get; set; }
		/// <summary>
		/// Last update date of document in registry
		/// </summary>
		public System.DateTime DocumentUpdatedAt { get; set; }
		public System.DateTime DownloadDate { get; set; }
		public System.DateTime ImportDate { get; set; }
		public string Message { get; set; }
		public string ResourcePublicKey { get; set; }
		public string Payload { get; set; }
		public bool IsMostRecentDownload { get; set; }
		public List<Import_Message> Messages{ get; set; }
	}
	public partial class Import_Message
	{
		public int Id { get; set; }
		public int ParentId { get; set; }
		public System.DateTime Created { get; set; }
		public int Severity { get; set; }
		public string Message { get; set; }
	}

    public class SearchPendingReindex
    {
        public int Id { get; set; }
        public int EntityTypeId { get; set; }
        public int RecordId { get; set; }
        public int StatusId { get; set; }
        public int IsUpdateOrDeleteTypeId { get; set; }
        public System.DateTime Created { get; set; }
        public System.DateTime LastUpdated { get; set; }
    }
}
