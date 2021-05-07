using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ME = workIT.Models.Elastic;

using MPM =workIT.Models.ProfileModels;
using WMS = workIT.Models.Search;

namespace workIT.Models.API
{
	[Serializable]
	public class BaseDisplay
	{
		public BaseDisplay() { }

		public string BroadType { get; set; }
		public string CTDLType { get; set; }
		public string CTDLTypeLabel { get; set; }
		public int EntityTypeId { get; set; }
		public string CTID { get; set; }
		/// <summary>
		/// name
		/// </summary>
		public string Name { get; set; }
		public string FriendlyName { get; set; }
		public string Description { get; set; }
		public string SubjectWebpage { get; set; }
		public List<string> InLanguage { get; set; } = new List<string>() { "en" };
		public int? Meta_Id { get; set; }
		public int? Meta_StateId { get; set; }
		public string Meta_Language { get; set; }

		public DateTime Meta_LastUpdated { get; set; }
		public string Meta_LastUpdatedHeader
		{ 
			get { return Meta_LastUpdated.ToString( "MMM d, yyyy" ); }
		}
		//public bool IsReferenceVersion { get; set; }
		public LabelLink OwnedByLabel { get; set; }
		public WMS.AJAXSettings OwnedBy { get; set; }
		public WMS.AJAXSettings OfferedBy { get; set; }

		public int? OrganizationId { get; set; }
		public string OrganizationName { get; set; }
		public string OrganizationSubjectWebpage { get; set; }
		
		/// <summary>
		/// The geo-political region in which the described resource is applicable.
		/// </summary>
		public List<ME.JurisdictionProfile> Jurisdiction { get; set; } = new List<ME.JurisdictionProfile>();
		//
		public string CredentialRegistryURL { get; set; }
		public RegistryData RegistryData { get; set; } = new RegistryData();
	}

	[Serializable]
	public class RegistryData
	{
		public string CTID { get; set; }
		public LabelLink Envelope { get; set; }
		public LabelLink Resource { get; set; }
		public string RawMetadata { get; set; }
	}

	[Serializable]
	public class LabelLink
	{
		public string SearchType { get; set; }
		public string Label { get; set; }
		public string URL { get; set; }
		public int? Total { get; set; } = null;
		public string Description { get; set; }
		public string Value { get; set; }
		public string TestURL { get; set; }
		public WMS.MainQuery Query { get; set; }
	}

	[Serializable]
	public class Outline
	{
		/// <summary>
		/// header
		/// </summary>
		public string Label { get; set; }
		public LabelLink Provider { get; set; }
		/// <summary>
		/// If present, format the heading as a link
		/// </summary>
		public string URL { get; set; }
		public string Description { get; set; }
		/// <summary>
		/// The Role could be with or with a URL
		/// </summary>
		public List<LabelLink> Tags = new List<LabelLink>();
		public string Image { get; set; }
	}

	[Serializable]
	public class ProcessProfileGroup
	{
		public string Label { get; set; }
		public string Description { get; set; }
		public List<MPM.ProcessProfile> ProcessProfile { get; set; } = new List<MPM.ProcessProfile>();
		
	}

	//

}
