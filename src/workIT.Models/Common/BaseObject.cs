using Newtonsoft.Json;

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
		public string FriendlyName { get; set; }

		public bool IsReferenceVersion { get; set; }
		[JsonIgnore]
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

		[JsonIgnore]
		public DateTime Created { get; set; }
		public string CreatedDisplay { get; set; }
		[JsonIgnore]
		public DateTime LastUpdated { get; set; }
		public string LastUpdatedDisplay { get; set; }
		[JsonIgnore]
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

		public string Description { get; set; }
		public LanguageMap Description_Map { get; set; }
		public int EntityStateId { get; set; }
		private int _entityTypeId { get; set; }
		public int EntityTypeId
		{
			get { return _entityTypeId; }
			set
			{
				_entityTypeId = value;
				switch ( _entityTypeId )
				{
					case 1:
						EntityType = "Credential";
						break;
					case 2:
						EntityType = "Organization";
						break;
					case 3:
						EntityType = "Assessment";
						break;
					case 7:
						EntityType = "LearningOpportunity";
						break;
					case 8:
						EntityType = "Pathway";
						break;
					case 9:
						EntityType = "Collection";
						break;
					case 10:
					case 17:
						EntityType = "CompetencyFramework";
						break;
					case 11:
						EntityType = "ConceptScheme";
						break;
					case 19:
						EntityType = "ConditionManifest";
						break;
					case 20:
						EntityType = "CostManifest";
						break;

					case 23:
						EntityType = "PathwaySet";
						break;
					case 24:
						EntityType = "PathwayComponent";
						break;
					case 26:
						EntityType = "TransferValue";
						break;
					case 28:
						EntityType = "EarningsProfile";
						break;
					case 29:
						EntityType = "HoldersProfile";
						break;
					case 30:
						EntityType = "EmploymentOutcomeProfile";
						break;
					case 31:
						EntityType = "DataSetProfile";
						break;
					case 32:
						EntityType = "JobProfile";
						break;
					case 33:
						EntityType = "TaskProfile";
						break;
					case 34:
						EntityType = "WorkRoleProfile";
						break;
					case 35:
						EntityType = "OccupationProfile";
						break;
					case 36:
						EntityType = "LearningProgram";
						break;
					case 37:
						EntityType = "Course";
						break;
					default:
						EntityType = string.Format( "Unexpected EntityTypeId of {0}", _entityTypeId );
						break;
				}
			}
		}
		public string EntityType { get; set; }
		public string EntityTypeLabel { get; set; }
		public string EntityTypeSchema { get; set; }
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
		public string OrganizationFriendlyName
		{
			get
			{
				if ( OwningOrganization != null && OwningOrganization.Id > 0 )
					return OwningOrganization.FriendlyName;
				else
					return "";
			}
		}
		//public string OrganizationFriendlyName { get; set; }

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
				else if (OrganizationId > 0)
					return OrganizationId;
				else
					return 0;
			}
		}
		public string PrimaryOrganizationCTID { get; set; }
		public int OrganizationId { get; set; }
		//for searches
		public string PrimaryOrganizationName{ get; set; }
		public string PrimaryOrganizationFriendlyName { get; set; }

		//3rdParty PublishedBy - really only need PublishedBy now?
		public int PublishedByThirdPartyOrganizationId { get; set; }

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


	//
	[Serializable]
	public class BaseEmploymentObject : TopLevelObject 
	{
		/// <summary>
		/// URI
		/// </summary>
		public string CtdlId { get; set; }



		/// <summary>
		/// AbilityEmbodied
		/// Enduring attributes of the individual that influence performance are embodied either directly or indirectly in this resource.
		/// ceasn:abilityEmbodied
		/// </summary>
		public List<string> AbilityEmbodied { get; set; }

		/// <summary>
		/// Category or classification of this resource.
		/// Where a more specific property exists, such as ceterms:naics, ceterms:isicV4, ceterms:credentialType, etc., use that property instead of this one.
		/// URI to a competency
		/// ceterms:classification
		/// </summary>
		public Enumeration Classification { get; set; }

		public string CodedNotation { get; set; }


		/// <summary>
		/// Comment
		/// Definition:	en-US: Supplemental text provided by the promulgating body that clarifies the nature, scope or use of this competency.
		/// ceasn:comment
		/// </summary>
		public List<string> Comment { get; set; } = new List<string>();


		/// <summary>
		/// Alphanumeric token that identifies this resource and information about the token's originating context or scheme.
		/// <see cref="http://purl.org/ctdl/terms/identifier"/>
		/// </summary>
		public List<IdentifierValue> Identifier { get; set; }
		public string IdentifierJson { get; set; }

		/// <summary>
		/// Body of information embodied either directly or indirectly in this resource.
		/// List of URIs for a competency
		/// ceasn:knowledgeEmbodied
		/// </summary>
		public List<string> KnowledgeEmbodied { get; set; }



		/// <summary>
		///Ability to apply knowledge and use know-how to complete tasks and solve problems including types or categories of developed proficiency or dexterity in mental operations and physical processes is embodied either directly or indirectly in this resource.
		/// </summary>
		public List<string> SkillEmbodied { get; set; }


		/// <summary>
		/// Alphanumeric identifier of the version of the credential that is unique within the organizational context of its owner.
		/// ceterms:versionIdentifier
		/// </summary>
		public List<IdentifierValue> VersionIdentifier { get; set; }
		public string VersionIdentifierJson { get; set; }

	}

}
