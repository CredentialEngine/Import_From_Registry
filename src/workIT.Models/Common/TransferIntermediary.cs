using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.ProfileModels;

namespace workIT.Models.Common
{
    public class TransferIntermediary : TopLevelObject
    {

		public TransferIntermediary()
		{
			Type = "ceterms:TransferIntermediary";
		}

		public string Type { get; set; }

		public string CodedNotation { get; set; }

		public List<ValueProfile> CreditValue { get; set; } = new List<ValueProfile>();
		public string CreditValueJson { get; set; }
		//public List<string> IntermediaryFor { get; set; }
		public string IntermediaryForJson { get; set; }
		public List<TopLevelObject> IntermediaryFor { get; set; } = new List<TopLevelObject>();

		public Enumeration OwnerRoles { get; set; } = new Enumeration();
		//??
		public List<OrganizationRoleProfile> OrganizationRole { get; set; } = new List<OrganizationRoleProfile>();

		public List<ConditionProfile> Requires { get; set; } = new List<ConditionProfile>();

		//public string SubjectWebpage { get; set; }

		public List<TextValueProfile> Subject { get; set; }
		public int HasTransferValueProfiles { get; set; }
		//import
		public List<Guid> OwnedBy { get; set; }
		public List<Guid> IntermediaryForImport { get; set; } = new List<Guid>();

	}
}
