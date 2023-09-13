using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RA.Models.Input
{

	/// <summary>
	/// Class for handling references to an organization
	/// Either the Id as an resolvable URL, a CTID (that will be use to format the Id as a URI) or provide all of the properities:
	/// - Type
	/// - Name
	/// - Description
	/// - Subject webpage
	/// - Social media
	/// 2020-07-01 With the addition of many additional properties to EntityReference, changed OrganizationReference to no lonber inherit from EntityReference.
	/// </summary>
	public class OrganizationReference 
	{
		public static string CredentialOrganization = "CredentialOrganization";
		public static string QACredentialOrganization = "QACredentialOrganization";
		/// <summary>
		/// The type of organization is one of :
		/// - CredentialOrganization
		/// - QACredentialOrganization
		/// Required
		/// </summary>
		public string Type { get; set; }

		/// <summary>
		/// Id is a resovable URI
		/// If the entity exists in the registry, provide the URI. 
		/// If not sure of the exact URI, especially if just publishing the entity, then provide the CTID and the API will format the URI.
		/// Alterate URIs are under consideration. For example
		/// http://dbpedia.com/Stanford_University
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// Optionally, a CTID can be entered instead of an Id. 
		/// A CTID is recommended for flexibility.
		/// Only enter Id or CTID, but not both.
		/// </summary>
		public string CTID { get; set; }

		/// <summary>
		/// Name of the entity (required)
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Alternately can provide a language map
		/// </summary>
		public LanguageMap Name_Map { get; set; } = new LanguageMap();

		/// <summary>
		/// Subject webpage of the entity (required)
		/// This should be for the referenced entity. 
		/// For example, if the reference is for an organization, the subject webpage should be on the organization site.
		/// </summary>
		public string SubjectWebpage { get; set; }

		/// <summary>
		/// Description of the entity (optional)
		/// This should be the general description of the entity. 
		/// For example, for an organization, the description should be about the organization specifically not, how the organization is related to, or interacts with the refering entity. 
		/// </summary>
		public string Description { get; set; }
		/// <summary>
		/// Alternately can provide a language map
		/// </summary>
		public LanguageMap Description_Map { get; set; } = new LanguageMap();

		/// <summary>
		/// Social Media URL links
		/// For example, Facebook, LinkedIn
		/// </summary>
		public List<string> SocialMedia { get; set; } //URL


		//additional optional information - why not everything!
		///// <summary>
		///// List of Places
		///// In this context - an organization reference, partial addresses are allowed. 
		///// This means a street address and/or a postal would not be required, just say city, region, and country
		///// </summary>
		//public List<Place> Address { get; set; } = new List<Place>();
		/// <summary>
		/// Listing of online and/or physical locations
		/// List of URLs
		/// </summary>
		public List<string> AvailabilityListing { get; set; } = new List<string>();
		/// <summary>
		/// List of email addresses
		/// </summary>
		public List<string> Email { get; set; } = new List<string>();

		/// <summary>
		/// check if has the necessary properties
		/// </summary>
		/// <returns></returns>
		public bool HasNecessaryProperties()
		{
			//skip social media for now
			//	|| ( SocialMedia == null || SocialMedia.Count == 0 )
			//				|| string.IsNullOrWhiteSpace( Description )
			if ( string.IsNullOrWhiteSpace( Type )
				|| (string.IsNullOrWhiteSpace( Name ) && Name_Map?.Count == 0)
				|| string.IsNullOrWhiteSpace( SubjectWebpage )
				)
				return false;
			else
				return true;
		}

		/// <summary>
		/// Purpose is to determine if class has data
		/// </summary>
		/// <returns></returns>
		public bool  IsEmpty()
		{
			if ( string.IsNullOrWhiteSpace( Id )
				&& string.IsNullOrWhiteSpace( Name )
                && string.IsNullOrWhiteSpace( CTID )
                && string.IsNullOrWhiteSpace( Description )
				&& string.IsNullOrWhiteSpace( SubjectWebpage )
				&& ( SocialMedia == null || SocialMedia.Count == 0 )
				)
				return true;
			else
				return false;
		}
	}

}
