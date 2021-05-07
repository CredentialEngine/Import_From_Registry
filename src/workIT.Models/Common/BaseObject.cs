using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
    [Serializable]
    public class BaseObject
	{
		public BaseObject()
		{
			RowId = new Guid(); //Will be all 0s, which is probably desirable
			//DateEffective = new DateTime();
			Created = new DateTime();
			LastUpdated = new DateTime();
			//IsNewVersion = true;
			HasCompetencies = false;
			ChildHasCompetencies = false;
			//Publish_Type = "ceterms:entity";
			StatusMessage = "";
		}
		public int Id { get; set; }
		public bool IsReferenceVersion { get; set; }
        //public bool CanEditRecord { get; set; }
		//public bool CanUserEditEntity { 
		//	get { return this.CanEditRecord; }
		//	set { 
		//		this.CanEditRecord = value; 
		//	}

		//}
		//public bool CanViewRecord { get; set; }
		public Guid RowId { get; set; }
		/// <summary>
		/// ParentId will typically be the Entity.Id related to the base class
		/// </summary>
		public int ParentId { get; set; }
		public bool HasCompetencies { get; set; }
		public bool ChildHasCompetencies { get; set; }
		public string DateEffective { get; set; }
		public string ExpirationDate { get; set; }
		public string StatusMessage { get; set; }

		public DateTime Created { get; set; }
		public int CreatedById { get; set; }
		//public string CreatedBy { get; set; }
		public DateTime LastUpdated { get; set; }
		public string CreatedDisplay { get { return Created == null ? "" : Created.ToShortDateString(); } }
		public string LastUpdatedDisplay { get { return LastUpdated == null ? CreatedDisplay : LastUpdated.ToShortDateString(); } }
		public int LastUpdatedById { get; set; }
		//public string LastUpdatedBy { get; set; }
		public DateTime EntityLastUpdated { get; set; }

		//store language map properties
		public List<EntityLanguageMap> LanguageMaps { get; set; } = new List<EntityLanguageMap>();
        public string FirstLanguage { get; set; }
		public int ResultNumber { get; set; }

	}
	//
	[Serializable]
	public class TopLevelObject : BaseObject
	{
		/// <summary>
		/// name
		/// </summary>
		public string Name { get; set; }
		public LanguageMap Name_Map { get; set; }
		public string FriendlyName { get; set; }
		public string ProfileName
		{
			get { return this.Name; }
			set { this.Name = value; }
		}
		public string Description { get; set; }
		public LanguageMap Description_Map { get; set; }
		public int EntityStateId { get; set; }
		public int EntityTypeId { get; set; }
		public string CTID { get; set; }
		public bool IsReferenceEntity
		{
			get
			{
				if ( string.IsNullOrWhiteSpace( CTID ) )
					return true;
				else
					return false;
			}
		}


		public string SubjectWebpage { get; set; }
		#region Owner - these will not be applicable to an org?
		/// <summary>
		/// OwningAgentUid
		///  (Nov2016)
		/// </summary>
		public Guid OwningAgentUid { get; set; }

		//cannot initialize here, as can lead to a stack overflow
		public Organization OwningOrganization { get; set; } //= new Organization();
		public string OrganizationName
		{
			get
			{
				if ( OwningOrganization != null && OwningOrganization.Id > 0 )
					return OwningOrganization.Name;
				else
					return "";
			}
		}
		//this would appear to be a duplicate of the latter
		//determine if can remove it.
		//public string OwningOrgDisplay { get; set; }
		public int OwningOrganizationId
		{
			get
			{
				if ( OwningOrganization != null && OwningOrganization.Id > 0 )
					return OwningOrganization.Id;
				else
					return 0;
			}
		}
		public int PrimaryOrganizationId
		{
			get
			{
				if ( OwningOrganization != null && OwningOrganization.Id > 0 )
					return OwningOrganization.Id;
				else
					return 0;
			}
		}
		public string PrimaryOrganizationCTID { get; set; }
		public int OrganizationId { get; set; }
		//for searches
		public string PrimaryOrganizationName{ get; set; }
		//3rdParty PublishedBy - really only need PublishedBy now?
		//public int PublishedByOrganizationId { get; set; }

		//public string PublishedByOrganizationName { get; set; }

		//public string PublishedByOrganizationCTID { get; set; }
		public List<Guid> PublishedBy { get; set; }

		#endregion

		//future when added for org, asmt, and lopp
		public int StatusId { get; set; }
		public string CredentialRegistryId { get; set; }
		public string Image { get; set; }

		/// <summary>
		/// The geo-political region in which the described resource is applicable.
		/// </summary>
		public List<JurisdictionProfile> Jurisdiction { get; set; } = new List<JurisdictionProfile>();

	}

}
