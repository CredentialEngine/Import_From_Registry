using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.ProfileModels;

namespace workIT.Models.Common
{
	public class ConditionManifestExpanded : TopLevelObject 
	{
		public ConditionManifestExpanded()
		{
			//CommonConditions = new List<ConditionManifestExpanded>();
			//Auto_OrgURI = new List<string>();
			foreach ( var prop in this.GetType().GetProperties().Where( m => m.PropertyType == typeof( List<ConditionProfile> ) ).ToList() )
			{
				prop.SetValue( this, new List<ConditionProfile>() );
			}
		}
		//public string Name { get; set; }
		//public string Description { get; set; }
		//public string SubjectWebpage { get; set; }
		//public string CTID { get; set; }

		///// <summary>
		///// Inflate OwningAgentUid for display 
		///// </summary>
		//public Organization OwningOrganization { get; set; }
		
		public List<ConditionProfile> ConditionProfiles { get; set; }

		public List<ConditionProfile> Requires { get; set; }
		public List<ConditionProfile> Recommends { get; set; }
		public List<ConditionProfile> AdvancedStandingFrom { get; set; }
		public List<ConditionProfile> PreparationFrom { get; set; }
		public List<ConditionProfile> IsRequiredFor { get; set; }
		public List<ConditionProfile> IsRecommendedFor { get; set; }
		public List<ConditionProfile> IsAdvancedStandingFor { get; set; }
		public List<ConditionProfile> IsPreparationFor { get; set; }
		public List<ConditionProfile> EntryCondition { get; set; }
		public List<ConditionProfile> Corequisite { get; set; }
        public List<ConditionProfile> CoPrerequisite { get; set; }
        public List<ConditionProfile> Renewal { get; set; }
		//public List<ConditionManifestExpanded> CommonConditions { get; set; }

		public static List<ConditionManifestExpanded> ExpandConditionManifestList( List<ConditionManifest> input )
		{
			var result = new List<ConditionManifestExpanded>();

			foreach( var item in input )
			{
				var manifest = DisambiguateConditionProfiles( item.ConditionProfiles );
				manifest.Name = item.Name;
				manifest.Description = item.Description;
				manifest.SubjectWebpage = item.SubjectWebpage;
				manifest.CTID = item.CTID;
				//manifest.Auto_OrgURI = item.Auto_OrgURI;
				result.Add( manifest );
			}

			return result;
		}

		public static ConditionManifestExpanded DisambiguateConditionProfiles( List<ConditionProfile> input )
		{
			var processed = ConditionProfile.DisambiguateConditionProfiles( input );
			var result = new ConditionManifestExpanded()
			{
				Requires = processed[ ConditionProfile.ConditionProfileTypes.REQUIRES ],
				Recommends = processed[ ConditionProfile.ConditionProfileTypes.RECOMMENDS ],
				PreparationFrom = processed[ ConditionProfile.ConditionProfileTypes.PREPARATION_FROM ],
				AdvancedStandingFrom = processed[ ConditionProfile.ConditionProfileTypes.ADVANCED_STANDING_FROM ],
				IsRequiredFor = processed[ ConditionProfile.ConditionProfileTypes.IS_REQUIRED_FOR ],
				IsRecommendedFor = processed[ ConditionProfile.ConditionProfileTypes.IS_RECOMMENDED_FOR ],
				IsPreparationFor = processed[ ConditionProfile.ConditionProfileTypes.IS_PREPARATION_FOR ],
				IsAdvancedStandingFor = processed[ ConditionProfile.ConditionProfileTypes.IS_ADVANCED_STANDING_FOR ],
				EntryCondition = processed[ ConditionProfile.ConditionProfileTypes.ENTRY_CONDITION ],
				Corequisite = processed[ ConditionProfile.ConditionProfileTypes.COREQUISITE ],
                CoPrerequisite = processed[ConditionProfile.ConditionProfileTypes.COPREREQUISITE],
            };
			return result;
		}
	}
    //
    [Serializable]
    public class ConditionManifest : TopLevelObject
	{
		
		public ConditionManifest ()
		{
			EntityTypeId = 19;
			//OwningOrganization = new Organization();
			//TargetCredential = new List<Credential>();
			//CommonConditions = new List<ConditionManifest>();
			ConditionProfiles = new List<ConditionProfile>();

			Requires = new List<ConditionProfile>();
			Recommends = new List<ConditionProfile>();
			EntryCondition = new List<ConditionProfile>();
			Corequisite = new List<ConditionProfile>();
			Renewal = new List<ConditionProfile>();
		}
		//public int OrganizationId { get; set; }

		//public Guid OwningAgentUid { get; set; }
		/// <summary>
		/// Inflate OwningAgentUid for display 
		/// </summary>
		//public Organization OwningOrganization { get; set; }
		
		//public string OrganizationName { get; set; }

		//public string Name { get; set; }

		//public string ProfileName
		//{
		//	get { return this.Name; }
		//	set { this.Name = value; }
		//}
		//public string Description { get; set; }
		//public string SubjectWebpage { get; set; }
		//public int EntityStateId { get; set; }
		//public string CTID { get; set; }
		//public string CredentialRegistryId { get; set; }


		public List<ConditionProfile> ConditionProfiles { get; set; }

		public List<ConditionProfile> Requires { get; set; }
		public List<ConditionProfile> Recommends { get; set; }
		
		public List<ConditionProfile> EntryCondition { get; set; }
		public List<ConditionProfile> Corequisite { get; set; }
		public List<ConditionProfile> CoPrerequisite { get; set; } 
		public List<ConditionProfile> Renewal { get; set; }
		
	}
	//
	public class Entity_CommonCondition
	{
		public int Id { get; set; }
		public int EntityId { get; set; }
		public int ConditionManifestId { get; set; }
		public System.DateTime Created { get; set; }
		public int CreatedById { get; set; }
	
		public ConditionManifest ConditionManifest { get; set; }
		public string ProfileSummary { get; set; }
	}

	public class Entity_ConditionManifest
	{
		public int Id { get; set; }
		public int EntityId { get; set; }
		public int ConditionManifestId { get; set; }
		public System.DateTime Created { get; set; }
		public int CreatedById { get; set; }

		public ConditionManifest ConditionManifest { get; set; }
		public string ProfileSummary { get; set; }
	}
}
