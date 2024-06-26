using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS = workIT.Models.Search;

namespace workIT.Models.API
{
	public class CredentialingAction : BaseAPIType
	{
		public CredentialingAction()
		{
			EntityTypeId = 22;
			BroadType = "CredentialingAction";
			//defaults
			CTDLType = "ceterms:CredentialingAction";
			CTDLTypeLabel = "Credentialing Action";
		}
		public WMS.AJAXSettings Instrument { get; set; }

		/// <summary>
		/// Date the validity or usefulness of the information in this resource begins.
		/// </summary>
		public string StartDate { get; set; }

		/// <summary>
		/// Date this assertion ends.
		/// </summary>
		public string EndDate { get; set; }

		public string EvidenceOfAction { get; set; }

		public WMS.AJAXSettings Object { get; set; }

		public LabelLink ActionStatusType { get; set; }

		public LabelLink ActionType { get; set; }

		public WMS.AJAXSettings Participant { get; set; }
		public WMS.AJAXSettings ActingAgent { get; set; }

		public string Image { get; set; } //image URL
	}
}
