using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Web.Script.Serialization;

namespace workIT.Models.Node
{
	//Core Object
	public class DatabaseObject
	{
		public DatabaseObject()
		{
			RowId = new Guid();
			Created = new DateTime();
			LastUpdated = new DateTime();
		}
		public int Id { get; set; }
		public Guid RowId { get; set; }

		public string LastUpdatedUserName { get { return !string.IsNullOrWhiteSpace( LastUpdatedBy ) ? LastUpdatedBy : !string.IsNullOrWhiteSpace( CreatedBy ) ? CreatedBy : ""; } }
		public string LastUpdatedTimestamp { get { return LastUpdated != null && LastUpdated != DateTime.MinValue ? LastUpdated.ToString() : Created != null && Created != DateTime.MinValue ? Created.ToString() : ""; } }

		[ScriptIgnore]
		public DateTime Created { get; set; }
		[ScriptIgnore]
		public DateTime LastUpdated { get; set; }
		[ScriptIgnore]
		public int CreatedById { get; set; }
		[ScriptIgnore]
		public string CreatedBy { get; set; }
		[ScriptIgnore]
		public int LastUpdatedById { get; set; }
		[ScriptIgnore]
		public string LastUpdatedBy { get; set; }
	}
	//

	//Basic Profile
	public class BaseProfile : DatabaseObject
	{
		public BaseProfile()
		{
			foreach ( var property in this.GetType().GetProperties() )
			{
				//Initialize all ProfileLinks via Reflection
				if ( property.PropertyType == typeof( List<ProfileLink> ) )
				{
					property.SetValue( this, new List<ProfileLink>() );
				}
				if ( property.PropertyType == typeof( ProfileLink ) )
				{
					property.SetValue( this, new ProfileLink() );
				}
				//Initialize all enumerations via reflection
				if ( property.PropertyType == typeof( List<int> ) )
				{
					property.SetValue( this, new List<int>() );
				}
			}
		}

		//Properties
		[Property( DBName = "ProfileName" )] //Ends up being the case more often than not
		public virtual string Name { get; set; }
		public string Description { get; set; }
		public string DateEffective { get; set; }
		/// <summary>
		/// Really a subtype - for example is credential type for profile of Credential
		/// </summary>
		public string ProfileType { get; set; }
		public string ProfileSummary{ get; set; }
		public string ViewHeading { get; set; }

		//TODO -temp
		public bool IsNewVersion { get; set; }
	}
	//

	//First-Class Citizen
	public class BaseMainProfile : BaseProfile
	{
		public BaseMainProfile()
		{
			Other = new Dictionary<string, string>();
		}

		//Basic Info
		public override string Name { get; set; } //Overrides the annotation set in Base profile

		public string Url { get; set; }
		public string SubjectWebpage { get; set; }

		//List-based Info
		public Dictionary<string, string> Other { get; set; }

		//Profile Info
		//[Property( Type = typeof( JurisdictionProfile ) )]
		//public List<ProfileLink> Jurisdiction { get; set; }

		//[Property( Type = typeof( AgentRoleProfile_Recipient ), DBName = "OrganizationRole" )]
		//public List<ProfileLink> AgentRole_Recipient { get; set; }


		//[Property( Type = typeof( QualityAssuranceActionProfile_Recipient ), DBName = "QualityAssuranceAction" )]
		//public List<ProfileLink> QualityAssuranceAction_Recipient { get; set; }
	}
	//

}
