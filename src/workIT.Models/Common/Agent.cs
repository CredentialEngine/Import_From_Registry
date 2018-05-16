using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.ProfileModels;

namespace workIT.Models.Common
{
	/// <summary>
	/// Agent could be an org or a person
	/// This could just inherit from org?
	/// </summary>
	public class Agent : BaseObject
	{
		public Agent()
		{
			Address = new Address();
			Addresses = new List<Address>();
			AlternateName = new List<TextValueProfile>();
			AlternateNames = new List<string>();
			IdentificationCodes = new List<TextValueProfile>();
			Keyword = new List<TextValueProfile>();
			//Subjects = new List<TextValueProfile>();
			Emails = new List<TextValueProfile>();
			SocialMediaPages = new List<TextValueProfile>();
			SameAs = new List<TextValueProfile>();
			ContactPoint = new List<Common.ContactPoint>();

			AlternativeIdentifierList = new List<Entity_IdentifierValue>();
		}
		/// <summary>
		/// Organization or QAOrganization
		/// </summary>
		public string AgentDomainType { get; set; }
		/// <summary>
		/// 1-Organization; 2-QAOrganization
		/// </summary>
		public int AgentTypeId { get; set; }
		//????
		//public int AgentRelativeId { get; set; }
		public string Name { get; set; }
		public string FriendlyName { get; set; }
		public string Description { get; set; }
		
		public string SubjectWebpage { get; set; }

		public string ImageUrl { get; set; }

		public string CredentialRegistryId { get; set; }
		public string CTID { get; set; }
		public List<TextValueProfile> AlternateName { get; set; }
		public List<string> AlternateNames { get; set; }

		public Address Address { get; set; }

		public List<Address> Addresses { get; set; }
		//Alias used for search
		public List<Address> Auto_Address
		{
			get
			{
				var result = new List<Address>();
				if ( Address != null && Address.Id > 0 )
				{
					result.Add( Address );
				}
				result = result.Concat( Addresses ).ToList();

				return result;
			}
		}
		public string AvailabilityListing { get; set; }
        public List<string> AvailabilityListings { get; set; } = new List<string>();

        public List<JurisdictionProfile> JurisdictionProfiles { get; set; }
		//SocialMedia is saved as an OrganizationProperty
		// -not anymore
		//public List<TextValueProfile> SocialMedia { get; set; }
		public List<TextValueProfile> SocialMediaPages { get; set; }
        //public List<TextValueProfile> Auto_SocialMedia { get { return SocialMediaPages; } set { SocialMediaPages = value; } } //Alias used for publishing

        //public List<TextValueProfile> PhoneNumbers { get; set; } = new List<TextValueProfile>();

		public List<TextValueProfile> Emails { get; set; }
		
		public List<TextValueProfile> Keyword { get; set; }

		public List<ContactPoint> ContactPoint { get; set; }

		public List<TextValueProfile> SameAs { get; set; }
		public List<TextValueProfile> Auto_SameAs { get { return SameAs; } set { SameAs = value; } } //Alias used for publishing
		public List<TextValueProfile> IdentificationCodes { get; set; }

		/// <summary>
		/// AlternativeIdentifier should just be added to IdentificationCodes
		/// </summary>
		public string AlternativeIdentifier { get; set; }
		public List<Entity_IdentifierValue> AlternativeIdentifierList { get; set; }
		public List<IdentifierValue> Auto_AlternativeIdentifier
		{
			get
			{
				var result = new List<IdentifierValue>();
				if ( !string.IsNullOrWhiteSpace( AlternativeIdentifier ) )
				{
					result.Add( new IdentifierValue()
					{
						IdentifierValueCode = AlternativeIdentifier
					} );
				}
				return result;
			}
		}
		//Identifier Aliases used for publishing
		public string ID_DUNS { get { return IdentificationCodes.FirstOrDefault( m => m.CodeSchema == "ceterms:duns" )?.TextValue; } }
		public string ID_FEIN { get { return IdentificationCodes.FirstOrDefault( m => m.CodeSchema == "ceterms:fein" )?.TextValue; } }
		public string ID_IPEDSID { get { return IdentificationCodes.FirstOrDefault( m => m.CodeSchema == "ceterms:ipedsID" )?.TextValue; } }
		public string ID_OPEID { get { return IdentificationCodes.FirstOrDefault( m => m.CodeSchema == "ceterms:opeID" )?.TextValue; } }
		public List<IdentifierValue> ID_AlternativeIdentifier {
			get {
				return IdentificationCodes.Where( m => m.CodeSchema == "ceterms:alternativeIdentifier" ).ToList().ConvertAll( m =>
				new IdentifierValue()
				{
					IdentifierType = m.TextTitle,
					IdentifierValueCode = m.TextValue
				} );
			} }

	}
}
