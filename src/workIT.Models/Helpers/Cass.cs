using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace workIT.Models.Helpers.Cass
{
	public class CassObject
	{
		public CassObject()
		{
			UtilityData = new Dictionary<string, object>();
		}

		[JsonProperty( PropertyName = "@id" )]
		public string _IdAndVersion { get; set; }

		//public string _Id { get; set; }
		public string _Id {
			get	{
				if ( _IdAndVersion == null )
					return "";

				return _IdAndVersion.Substring( 0, _IdAndVersion.LastIndexOf( "/" ) );
			}
			set { _IdAndVersion = value + "/"; }
		} //Strip timestamp

		[JsonProperty( PropertyName = "@schema" )]
		public string _Schema { get; set; }

		[JsonProperty( PropertyName = "@type" )]
		public string _Type { get; set; }

		[JsonProperty( PropertyName = "name" )]
		public string Name { get; set; }

		[JsonProperty( PropertyName = "description" )]
		public string Description { get; set; }

		[JsonProperty( PropertyName = "url" )]
		public string Url { get; set; }

		public Dictionary<string, object> UtilityData { get; set; }
	}
	//

	public class CassFramework : CassObject
	{
		public CassFramework()
		{
			CompetencyUris = new List<string>();
			RelationUris = new List<string>();
			Competencies = new List<Cass.CassCompetency>();
			Relations = new List<CassRelation>();
			TopLevelCompetencyUris = new List<string>();
		}

		[JsonProperty( PropertyName = "competency" )]
		public List<string> CompetencyUris { get; set; }

		[JsonProperty( PropertyName = "relation" )]
		public List<string> RelationUris { get; set; }
		
		public List<string> TopLevelCompetencyUris { get; set; }
		[JsonProperty( PropertyName = "competencies" )]
		public List<CassCompetency> Competencies { get; set; }
		[JsonProperty( PropertyName = "relations" )]
		public List<CassRelation> Relations { get; set; }
	}
	//

	public class CassCompetency : CassObject
	{
		public CassCompetency()
		{
			ChildrenUris = new List<string>();
		}

		public string FrameworkUri { get; set; }
		public List<string> ChildrenUris { get; set; }
		public string CodedNotation { get; set; }
	}
	//

	public class CassRelation : CassObject
	{
		[JsonProperty( PropertyName = "relationType" )]
		public string RelationType { get; set; }

		[JsonProperty( PropertyName = "source" )]
		public string Source { get; set; }

		[JsonProperty( PropertyName = "target" )]
		public string Target { get; set; }
	}
	//

	public class CassFrameworkMultiGetResult : CassObject
	{
		public CassFrameworkMultiGetResult()
		{
			Framework = new CassFramework();
			Competencies = new List<CassCompetency>();
			Relations = new List<CassRelation>();
		}
		[JsonProperty( PropertyName = "framework" )]
		public CassFramework Framework { get; set; }
		[JsonProperty( PropertyName = "competencies" )]
		public List<CassCompetency> Competencies { get; set; }
		[JsonProperty( PropertyName = "relations" )]
		public List<CassRelation> Relations { get; set; }
	}
	//

	public class CassBrowserV1Config
	{
		public CassBrowserV1Config()
		{
			Attributes = new Dictionary<string, string>();
		}
		public string OnSelectCompetency { get; set; }
		public string OnSelectCompetencyList { get; set; }
		public string OnUnselectCompetency { get; set; }
		public Dictionary<string, string> Attributes { get; set; }
	}
	//
}
