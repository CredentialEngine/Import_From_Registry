using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
	/// <summary>
	/// Will there be a separate class for this, or uses CredentialingAction.
	/// Will it be a top level object with a CTID?
	/// </summary>
	public class WorkforceDemandAction : CredentialingAction
	{
		/// <summary>
		/// type
		/// </summary>
		public WorkforceDemandAction()
		{
			Type = "ceterms:WorkforceDemandAction";
		}


	}
}
