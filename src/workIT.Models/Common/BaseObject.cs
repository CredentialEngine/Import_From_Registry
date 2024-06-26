using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
		//24-05-01 mp - moved CTID and Name from TopLevelObject. Need to be aware of any impact.
		public string CTID { get; set; }
		/// <summary>
		/// name
		/// </summary>
		public string Name { get; set; }
		public string FriendlyName { get; set; }
		/// <summary>
		/// Used to append organization name to the name in summaries or pill results
		/// </summary>
		public string NamePlusOrganization { get; set; }
		public bool IsReferenceVersion { get; set; }
		[JsonIgnore]
		public Guid RowId { get; set; }
		/// <summary>
		/// ParentId will typically be the Entity.Id related to the base class
		/// SHOULD RENAME THIS
		/// </summary>
		public int RelatedEntityId { get; set; }
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
	/// <summary>
	/// Purpose is to have a common summary object. 
	/// However, the inheritance results in a lot of chafe.
	/// </summary>
	[Serializable]
	public class TopLevelObject : BaseObject
	{

		//public LanguageMap Name_Map { get; set; }

		public string Description { get; set; }
		//public LanguageMap Description_Map { get; set; }
		public int EntityStateId { get; set; }
		private int _entityTypeId { get; set; }

		public string CodedNotation { get; set; }
		/// <summary>
		/// NEED TO SYNC WITH:
		///		EntityManager.EntityTypeId 
		///		Entity.EntityTypeId 
		/// </summary>
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
					case 12:
						EntityType = "ProgressionModel";
						break;
                    case 15:
                        EntityType = "ScheduledOffering";
                        break;
                    case 19:
						EntityType = "ConditionManifest";
						break;
					case 20:
						EntityType = "CostManifest";
						break;
					case 22:
						EntityType = "CredentialingAction";
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
					case 27:
						EntityType = "AggregateDataProfile";
						break;
					case 28:
						EntityType = "TransferIntermediary";
						break;
                    case 29:
                        EntityType = "Concept";
                        break;
                    //
                    case 30:
                        EntityType = "ProgressionLevel";
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
                    case 38:
                        EntityType = "SupportService";
                        break;
					case 39:
						EntityType = "Rubric";
						break;
					case 41:
                        EntityType = "VerificationServiceProfile";
                        break;
					case 44:
						EntityType = "RubricCriterion";
						break;
					case 45:
						EntityType = "RubricCriterionLevel";
						break;
					//OBSOLETE
					case 55:
						EntityType = "EarningsProfile";
						break;
					case 56:
						EntityType = "EmploymentOutcomeProfile";
						break;
					case 57:
						EntityType = "HoldersProfile";
						break;
					
					default:
						EntityType = string.Format( "Unexpected EntityTypeId of {0}", _entityTypeId );
						break;
				}
			}
		}
		public string EntityType { get; set; }
		/// <summary>
		/// User friendly content
		/// </summary>
		public string EntityTypeLabel { get; set; }
		public string EntityTypeSchema { get; set; }
		/// <summary>
		/// Seems a duplicate of EntityTypeLabel, but need a display label for subtypes
		/// </summary>
		public string CTDLTypeLabel { get; set; }
		
		public bool IsReferenceEntity
		{
			get
			{
				if ( string.IsNullOrWhiteSpace( CTID ) )
					return true;
                else if( EntityStateId== 2 )
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
		public Guid PrimaryAgentUID { get; set; }

		/// <summary>
		/// NOTE: cannot initialize here, as can lead to a stack overflow
		/// 22-11-22 - may want to start considering as the primary org, where no owner
		/// </summary>
		public Organization PrimaryOrganization { get; set; } //= new Organization();
		//public Organization PrimaryOrganization
		//{
		//	get
		//	{
		//		if ( OwningOrganization != null && OwningOrganization.Id > 0 )
		//			return OwningOrganization;
		//		else
		//			return null;
		//	}
		//}
		public string OrganizationName
		{
			get
			{
				if ( PrimaryOrganization != null && PrimaryOrganization.Id > 0 )
					return PrimaryOrganization.Name;
				else
					return "";
			}
		}
		public string OrganizationFriendlyName
		{
			get
			{
				if ( PrimaryOrganization != null && PrimaryOrganization.Id > 0 )
					return PrimaryOrganization.FriendlyName;
				else
					return "";
			}
		}
		//public string OrganizationFriendlyName { get; set; }

		//this would appear to be a duplicate of the latter
		//determine if can remove it.
		//public string OwningOrgDisplay { get; set; }

		/// <summary>
		/// this is functionally the same as PrimaryOrganizationId. Need to remove it to avoid confusion
		/// </summary>
		public int OwningOrganizationId
		{
			get
			{
				if ( PrimaryOrganization != null && PrimaryOrganization.Id > 0 )
					return PrimaryOrganization.Id;
				else
					return OrganizationId;
			}
		}
		/// <summary>
		/// Use separate property for OrganizationId where a PrimaryOrganization has not been established
		/// </summary>
        public int OrganizationId { get; set; }
		public int PrimaryOrganizationId
		{
			get
			{
				if ( PrimaryOrganization != null && PrimaryOrganization.Id > 0 )
					return PrimaryOrganization.Id;
				else if ( OrganizationId > 0 )
					return OrganizationId;
				else
					return 0;
			}
		}
		public string PrimaryOrganizationCTID { get; set; }
	
		//for searches
		public string PrimaryOrganizationName{ get; set; }
		public string PrimaryOrganizationFriendlyName { get; set; }

		//3rdParty PublishedBy - really only need PublishedBy now?
		public int PublishedByThirdPartyOrganizationId { get; set; }

		//public string PublishedByOrganizationName { get; set; }

		//public string PublishedByOrganizationCTID { get; set; }
		public List<Guid> PublishedBy { get; set; }

		public List<Organization> OfferedBy { get; set; } = new List<Organization>();

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

    [Serializable]
    public class CoreObject
    {
        //Lets auto-mapping methods check to see if this property should be skipped, which helps ensure critical properties don't get overwritten by mistake
        public class UpdateAttribute : Attribute
        {
            public bool SkipPropertyOnUpdate { get; set; }
        }
        public class ExportAttribute : Attribute
        {
            public bool IncludePropertyOnExport { get; set; }
        }
        //Convenience method to get skippable properties
        public static List<PropertyInfo> GetSkippableProperties( object data )
        {
            var result = new List<PropertyInfo>();
            foreach ( var property in data.GetType().GetProperties() )
            {
                var updateAttribute = ( UpdateAttribute ) property.GetCustomAttribute( typeof( UpdateAttribute ) );
                if ( updateAttribute != null && updateAttribute.SkipPropertyOnUpdate )
                {
                    result.Add( property );
                }
            }
            return result;
        }
        public List<PropertyInfo> GetSkippableProperties()
        {
            return GetSkippableProperties( this );
        }

        //Normal object stuff
        public CoreObject()
        {
            //Probably don't need to initialize anything here as long as BaseObject is still initializing things, since most stuff inherits from that
        }

        [Update( SkipPropertyOnUpdate = true )]
        public int Id { get; set; }

        [Update( SkipPropertyOnUpdate = true )]
        public Guid RowId { get; set; }

        public string RowIdString
        {
            get
            {
                if ( RowId == null && RowId != Guid.Empty )
                    return RowId.ToString();
                else
                    return "";
            }
        }

        [Update( SkipPropertyOnUpdate = true )]
        public DateTime Created { get; set; }

        [Update( SkipPropertyOnUpdate = true )]
        public int CreatedById { get; set; }
        /// <summary>
        /// Use will vary dependent on the context. 
        /// Initial use was for Pathways: if any component or condition changes, set true and then update entity lastUpdated.
        /// </summary>
        public bool IsDirty { get; set; }
        public DateTime LastUpdated { get; set; }
        public int LastUpdatedById { get; set; }
    }


}
