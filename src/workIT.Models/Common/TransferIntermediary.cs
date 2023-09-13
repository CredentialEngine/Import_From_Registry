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
        /// <summary>
        /// Use alternate name for display
        /// </summary>
        public List<string> AlternateName { get; set; }
        //use for import and detail, so maybe don't need AlternateName. Need for API
        public List<TextValueProfile> AlternateNames { get; set; } = new List<TextValueProfile>();
        public string CodedNotation { get; set; }

		public List<ValueProfile> CreditValue { get; set; } = new List<ValueProfile>();
		public string CreditValueJson { get; set; }
		//public List<string> IntermediaryFor { get; set; }
		public string IntermediaryForJson { get; set; }
		public List<TopLevelObject> IntermediaryFor { get; set; } = new List<TopLevelObject>();
        //
        //public Enumeration LifeCycleStatusType { get; set; } = new Enumeration();
        //public string LifeCycleStatus { get; set; }
        //public int LifecycleStatusTypeId { get; set; }
        public Enumeration OwnerRoles { get; set; } = new Enumeration();
		//??
		public List<OrganizationRoleProfile> OrganizationRole { get; set; } = new List<OrganizationRoleProfile>();

		public List<ConditionProfile> Requires { get; set; } = new List<ConditionProfile>();

		//public string SubjectWebpage { get; set; }

		public List<TextValueProfile> SubjectTVP { get; set; }
        public List<string> Subject { get; set; }
        public int HasTransferValueProfiles { get; set; }
		//import
		public List<Guid> OwnedBy { get; set; }
		public List<Guid> IntermediaryForImport { get; set; } = new List<Guid>();

		public List<CredentialAlignmentObjectFrameworkProfile> RequiresCompetenciesFrameworks { get; set; }

	}

	public class TransferIntermediaryTransferValue
	{
		public int Id { get; set; }

		public string TransferIntermediaryName { get; set; }

		public int TransferIntermediaryId { get; set; }
		public int TransferValueProfileId { get; set; }

		public string TransferValueProfileCTID { get; set; }
		/// <summary>
		/// The name or title of the TVP.
		/// </summary>
		public string TransferValueProfileName { get; set; }

		/// <summary>
		/// Description of the TVP.
		/// </summary>
		public string TransferValueProfileDescription { get; set; }


		public Nullable<System.DateTime> Created { get; set; }

		public TransferIntermediary TransferIntermediary { get; set; }
		public TransferValueProfile TransferValueProfile { get; set; }
	}
}
