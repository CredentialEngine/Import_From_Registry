using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RA.Models.Input;

namespace RA.Models.Navy.Input
{
	public class RatingRequest : BaseRequest
	{
		public RatingRequest()
		{
			Rating = new Rating();
		}

		public Rating Rating { get; set; }
	}

	public class Rating
	{
		/// <summary>
		/// CTID
		/// Globally unique Credential Transparency Identifier (CTID) by which the creator, owner or provider of a resource recognizes it in transactions with the external environment (e.g., in verifiable claims involving the resource).
		/// </summary>
		public string CTID { get; set; }

		public List<string> InLanguage { get; set; } = new List<string>();

		/// <summary>
		/// CodedNotation
		/// An alphanumeric notation or ID code as defined by the promulgating body to identify this resource.
		/// </summary>
		public string CodedNotation { get; set; }

		/// <summary>
		/// Comment
		/// Supplemental text provided by the promulgating body that clarifies the nature, scope or use of this competency.
		/// </summary>
		public List<string> Comment { get; set; } = new List<string>();
		public LanguageMapList Comment_Map { get; set; }


		/// <summary>
		/// DatePublished
		/// Date when this resource was published.
		/// </summary>
		public string DatePublished { get; set; }

		/// <summary>
		/// Description
		/// Description of the resource.
		/// </summary>
		public string Description { get; set; }
		/// <summary>
		/// Alternately can provide a language map
		/// </summary>
		public LanguageMap Description_Map { get; set; } = new LanguageMap();

		/// <summary>
		/// HasCredential
		/// HasCredentialCredential related to this resource
		/// List of URIs
		/// </summary>
		public List<string> HasCredential{ get; set; } = new List<string>();

		/// <summary>
		/// HasDoDOccupationType
		/// Type of occupation as categorized by the U.S. Department of Defense.
		/// </summary>
		public List<string> HasDoDOccupationType { get; set; } = new List<string>();

		/// <summary>
		/// HasOccupationType
		/// Type of occupation.
		/// </summary>
		public List<string> HasOccupationType { get; set; } = new List<string>();

		/// <summary>
		/// Name
		/// Name of the resource.
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Alternately can provide a language map
		/// </summary>
		public LanguageMap Name_Map { get; set; } = new LanguageMap();

		/// <summary>
		/// UploadDate
		/// Date when this resource was uploaded.
		/// </summary>
		public string UploadDate { get; set; }

		/// <summary>
		/// Version
		/// Version of this resource.
		/// </summary>
		public string Version { get; set; }
	}
}
