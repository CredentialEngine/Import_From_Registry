using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.ProfileModels;
namespace workIT.Models.Common
{
    [Serializable]
    public class CostManifest : BaseObject
	{

		public CostManifest()
		{
			OwningOrganization = new Organization();
			//???
			//CommonCosts = new List<CostManifest>();
			EstimatedCosts = new List<CostProfile>();

		}
		public int OrganizationId { get; set; }
		public Guid OwningAgentUid { get; set; }

		/// <summary>
		/// Inflate OwningAgentUid for display 
		/// </summary>
		public Organization OwningOrganization { get; set; }
		
		public string OrganizationName { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }
		public string CostDetails { get; set; }
		public int EntityStateId { get; set; }

		public string StartDate { get; set; }
		public string EndDate { get; set; }

		public string CTID { get; set; }
		public string CredentialRegistryId { get; set; }

		public List<CostProfile> EstimatedCosts { get; set; }
		//[Obsolete]
		//public List<CostProfile> EstimatedCost { get { return EstimatedCosts; } set { EstimatedCosts = value; } } //Alias used for publishing
		//public List<CostProfileMerged> EstimatedCost_Merged	{ get { return CostProfileMerged.FlattenCosts( EstimatedCosts ); } } //Alias used for publishing
	}

	public class Entity_CommonCost
	{
		public int Id { get; set; }
		public int EntityId { get; set; }
		public int CostManifestId { get; set; }
		public System.DateTime Created { get; set; }
		public int CreatedById { get; set; }

		public CostManifest CostManifest { get; set; }
		public string ProfileSummary { get; set; }
	}

	public class Entity_CostManifest
	{
		public int Id { get; set; }
		public int EntityId { get; set; }
		public int CostManifestId { get; set; }
		public System.DateTime Created { get; set; }
		public int CreatedById { get; set; }

		public CostManifest CostManifest { get; set; }
		public string ProfileSummary { get; set; }
	}
}
