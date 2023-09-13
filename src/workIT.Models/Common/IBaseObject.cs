using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.ProfileModels;

namespace workIT.Models.Common
{

	public interface IBaseObject
	{
		int Id { get; set; }
		string CTID { get; set; }
		string Name { get; set; }
		string Description { get; set; }
		string SubjectWebpage { get; set; }

		Organization PrimaryOrganization { get; set; }

		int EntityStateId { get; set; }

		DateTime Created { get; set; }
		DateTime LastUpdated { get; set; }

		List<DurationProfile> EstimatedDuration { get; set; }
	}
}
