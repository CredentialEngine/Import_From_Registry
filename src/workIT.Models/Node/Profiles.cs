using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Node
{
	//Object to hold code data that may or may not involve user entry - used only to store input, never to carry code tables!
	[Profile( DBType = typeof( Models.ProfileModels.TextValueProfile ) )]
	public class TextValueProfile : BaseProfile
	{
		[Property( DBName = "TextTitle" )]
		public string CodeOther { get; set; } //User-defined alternative name (used when CodeId/CodeName is equivalent to "other")
		[Property( DBName = "TextValue" )]
		public string Value { get; set; } //Actual value from user

		public int CategoryId { get; set; } //ID of the Category of the TextValueProfile (so the database knows what it belongs to)
		public int CodeId { get; set; } //ID from a drop-down list (e.g., "Dun and Bradstreet DUNS Number")
	}
	//

	//Used with Micro Searches
	public class MicroProfile : BaseProfile
	{
		public MicroProfile()
		{
			Properties = new Dictionary<string, object>();
			Heading2 = "";
			CodedNotation = "";
		}

		public string Heading2 { get; set; }
		public string CodedNotation { get; set; }
		//in Base
		//public string Name { get; set; }

		public Dictionary<string, object> Properties { get; set; }
		public Dictionary<string, object> Selectors { get; set; }
	}
	//

	//Used to help create and immediately associate a profile with a microsearch
	//[Profile( DBType = typeof( Models.Node.StarterProfile ) )] //hack
	//public class StarterProfile : BaseProfile
	//{
	//	[Property( DBName = "Name" )] //hack
	//	public override string Name { get; set; }
	//	//public string ProfileType { get; set; }
	//	public string SearchType { get; set; } //hack
	//	public string SubjectWebpage { get; set; }

	//	//List-based Info
	//	[Property( DBName = "OrganizationType", DBType = typeof( Models.Common.Enumeration ) )]
	//	public List<int> OrganizationTypeIds { get; set; }

	//	[Property( DBName = "CredentialType", DBType = typeof( Models.Common.Enumeration ) )]
	//	public int CredentialType { get { return CredentialTypeIds.FirstOrDefault(); } set { CredentialTypeIds = new List<int>() { value }; } }

	//	[Property( DBName = "null" )] //Database processes need to skip this item
	//	public List<int> CredentialTypeIds { get; set; }
	//}

	//
	[Profile( DBType = typeof( Models.Node.OrgReference ) )] //hack
	public class OrgReference : BaseProfile
	{
		public OrgReference()
		{
			IsThirdPartyOrganization = true;
		}
		[ Property( DBName = "Name" )] //hack
		public override string Name { get; set; }
		
		public string SearchType { get; set; } //hack
		public string SubjectWebpage { get; set; }

		//List-based Info
		[Property( DBName = "OrganizationType", DBType = typeof( Models.Common.Enumeration ) )]
		public List<int> OrganizationTypeIds { get; set; }

		public bool IsThirdPartyOrganization { get; set; }
	}
	//[Profile( DBType = typeof( Models.ProfileModels.DurationProfile ) )]
	//public class DurationProfile : BaseProfile
	//{
	//	public DurationProfile()
	//	{
	//		ExactDuration = new DurationItem();
	//		MinimumDuration = new DurationItem();
	//		MaximumDuration = new DurationItem();
	//	}

	//	//These are always handled inline and never separate from the DurationProfile, so they are not ProfileLink objects
	//	public DurationItem ExactDuration { get; set; }
	//	public DurationItem MinimumDuration { get; set; }
	//	public DurationItem MaximumDuration { get; set; }

	//	public string MinimumDurationISO8601 { get; set; }
	//	public string MaximumDurationISO8601 { get; set; }
	//	public string ExactDurationISO8601 { get; set; }

	//	public bool IsRange { get { return this.MinimumDuration != null && this.MaximumDuration != null && this.MinimumDuration.HasValue && this.MaximumDuration.HasValue; } }

	//	//Override the usage of description
	//	[Property( DBName = "Conditions" )]
	//	new public string Description { get; set; }

	//}
	//
	//public class DurationProfileExact : DurationProfile { }
	//public class DurationItem //Not sure if this needs to inherit from database
	//{
	//	public int Years { get; set; }
	//	public int Months { get; set; }
	//	public int Weeks { get; set; }
	//	public int Days { get; set; }
	//	public int Hours { get; set; }
	//	public int Minutes { get; set; }
	//	public bool HasValue { get { return Years + Months + Weeks + Days + Hours + Minutes > 0; } }

	//	public string Print()
	//	{
	//		var parts = new List<string>();
	//		if ( Years > 0 ) { parts.Add( Years + " year" + ( Years == 1 ? "" : "s" ) ); }
	//		if ( Months > 0 ) { parts.Add( Months + " month" + ( Months == 1 ? "" : "s" ) ); }
	//		if ( Weeks > 0 ) { parts.Add( Weeks + " week" + ( Weeks == 1 ? "" : "s" ) ); }
	//		if ( Days > 0 ) { parts.Add( Days + " day" + ( Days == 1 ? "" : "s" ) ); }
	//		if ( Hours > 0 ) { parts.Add( Hours + " hour" + ( Hours == 1 ? "" : "s" ) ); }
	//		if ( Minutes > 0 ) { parts.Add( Minutes + " minute" + ( Minutes == 1 ? "" : "s" ) ); }

	//		return string.Join( ", ", parts );
	//	}
	//}
	//



	//[Profile( DBType = typeof( Models.ProfileModels.QualityAssuranceActionProfile ) )]
	//public class QualityAssuranceActionProfile : BaseProfile
	//{

	//	[Property( DBName = "ActingAgent", DBType = typeof( workIT.Models.Common.Organization ), SaveAsProfile = true )]
	//	public ProfileLink Actor { get; set; }

	//	[Property( DBName = "ParticipantAgent", DBType = typeof( workIT.Models.Common.Organization ), SaveAsProfile = true )]
	//	public ProfileLink ParticipantAgent { get; set; }

	//	//Could be one of many types - requires special handling in the services layer
	//	public ProfileLink Recipient { get; set; }

	//	[Property( DBName = "IssuedCredential", DBType = typeof( workIT.Models.Common.Credential ), SaveAsProfile = true )]
	//	public ProfileLink IssuedCredential { get; set; }

	//	[Property( DBName = "RoleTypeId" )]
	//	public int QualityAssuranceTypeId { get; set; }

	//	public int ActionStatusTypeId { get; set; }
	//	public string StartDate { get { return this.DateEffective; } set { this.DateEffective = value; } }
	//	public string EndDate { get; set; }

	//	//Not used yet
	//	public ProfileLink RelatedQualityAssuranceAction { get; set; } //Enables a revoke action to apply to an accredit action
	//	public List<ProfileLink> SecondaryActor { get; set; } //Enables another org to take part in the action
	//}
	//public class QualityAssuranceActionProfile_Recipient : QualityAssuranceActionProfile { }
	//public class QualityAssuranceActionProfile_Actor : QualityAssuranceActionProfile { }
	//


	

	//[Profile( DBType = typeof( workIT.Models.ProfileModels.CostProfile ) )]
	//public class CostProfile : BaseProfile
	//{
	//	public CostProfile ()
	//	{
	//		Condition = new List<TextValueProfile>();
	//	}
	//	[Property( DBName = "DateEffective" )]
	//	public string StartDate { get { return this.DateEffective; } set { this.DateEffective = value; } }

	//	[Property( DBName = "ExpirationDate" )]
	//	public string ExpirationDate { get; set; }

	//	public string DetailsUrl { get; set; }

	//	//[Property( DBName = "ReferenceUrl" )]
	//	public List<TextValueProfile> ReferenceUrl { get; set; }

	//	[Property( DBName = "Items", DBType = typeof( workIT.Models.ProfileModels.CostProfileItem ) )]
	//	public List<ProfileLink> CostItem { get; set; }

	//	public string Currency { get; set; }
	//	public string CurrencySymbol { get; set; }
	//	//[Property( DBName = "CurrencyType", DBType = typeof( workIT.Models.Common.Enumeration ) )]
	//	public int CurrencyTypeId { get; set; } 
	//	public List<ProfileLink> Jurisdiction { get; set; }


	//	//Profile Info
	//	[Property( Type = typeof( JurisdictionProfile ) )]
	//	public List<ProfileLink> Region { get; set; }


	//	public List<TextValueProfile> Condition { get; set; }
	//}
	//

	//[Profile( DBType = typeof( workIT.Models.ProfileModels.CostProfileItem ) )]
	//public class CostItemProfile : BaseProfile
	//{
	//	public int CostTypeId { get; set; }
	//	[Property( DBName = "ResidencyType", DBType = typeof( workIT.Models.Common.Enumeration ) )]
	//	public List<int> ResidencyTypeIds { get; set; }
	//	//[Property( DBName = "EnrollmentType", DBType = typeof( workIT.Models.Common.Enumeration ) )]
	//	//public List<int> EnrollmentTypeIds { get; set; }

	//	[Property( DBName = "ApplicableAudienceType", DBType = typeof( workIT.Models.Common.Enumeration ) )]
	//	public List<int> AudienceType { get; set; }

	//	[Property( DBName = "PaymentPattern" )]
	//	public string Payments { get; set; }

	//	//[Property( DBName = "PayeeUid", DBType = typeof( Guid ) )]
	//	//public ProfileLink Recipient { get; set; }

	//	public decimal Price { get; set; }
	//}
	////

	//[Profile( DBType = typeof( workIT.Models.Common.CredentialAlignmentObjectProfile ) )]
	//public class CredentialAlignmentObjectProfile : BaseProfile
	//{
	//	[Property( DBName = "Name" )]
	//	public override string Name { get; set; }
	//	public string EducationalFramework { get; set; }
	//	public string CodedNotation { get; set; }
	//	public string TargetNodeName { get; set; }
	//	public string TargetName { get; set; } //Name of the target, not the profile. Not currently used.
	//	public string TargetDescription { get; set; } //Description of the target, not the profile. Not currently used.
	//	public string TargetUrl { get; set; }
	//}
	////

	//[Profile( DBType = typeof( workIT.Models.Common.CredentialAlignmentObjectFrameworkProfile ) )]
	//public class CredentialAlignmentObjectFrameworkProfile : BaseProfile
	//{
	//	public string EducationalFrameworkName { get; set; }
	//	public string EducationalFrameworkUrl { get; set; }
	//	public List<ProfileLink> Items { get; set; }
	//}
	////

	//[Profile( DBType = typeof( workIT.Models.Common.CredentialAlignmentObjectItemProfile ) )]
	//public class CredentialAlignmentObjectItemProfile : BaseProfile
	//{
	//	[Property( DBName = "Name" )]
	//	public override string Name { get; set; }
	//	public string TargetNodeName { get; set; }
	//	public string CodedNotation { get; set; }
	//	public string TargetName { get; set; } //Name of the target, not the profile. Not currently used.
	//	public string TargetDescription { get; set; } //Description of the target, not the profile. Not currently used.
	//	public string TargetUrl { get; set; } //TargetNode
	//	public string AlignmentDate { get; set; }
	//}
	//

	//

	//[Profile( DBType = typeof( workIT.Models.ProfileModels.ProcessProfile ) )]
	//public class ProcessProfile : BaseProfile
	//{
	//	public ProfileLink RolePlayer { get; set; }
	//	public List<int> ProcessTypeIds { get; set; }
	//	public List<int> ExternalStakeholderTypeIds { get; set; }
	//	public List<int> ProcessMethodTypeIds { get; set; }
	//	public List<ProfileLink> MoreInformationUrl { get; set; }
	//	public List<ProfileLink> Context { get; set; }
	//	public List<ProfileLink> Frequency { get; set; } //Duration
	//	public List<ProfileLink> Jurisdiction { get; set; }
	//}
	//

		


	//Attribute to make saving stuff easier
	[AttributeUsage(AttributeTargets.Class)]
	public class Profile : Attribute
	{
		public Profile()
		{
			DBType = typeof( string );
		}
		public Type DBType { get; set; }
	}
	//

}
