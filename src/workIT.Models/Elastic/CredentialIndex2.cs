using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;

namespace workIT.Models.Elastic
{
	public class CredentialIndex2 : BaseIndex
	{
		public CredentialIndex2 ()
		{
			//Addresses = new List<Place>();
			AlternateNames = new List<string>();
			CodedNotation = new List<string>();
			//CredentialsList = new CredentialConnectionsResult();
			//CredentialConnections = new List<Elastic.Connection>();
			//CredentialType = new IndexProperty();
			EmbeddedCredentials = new List<EntityReference>();
			Keywords = new List<string>();
			//Industries = new List<IndexFramework>();
			Languages = new List<string>();
		//	Occupations = new List<IndexFramework>();
			//OwningOrganizations = new List<OrganizationReference>();
			//QAOnCredentialRoles = new List<OrganizationRole>();
		//	QAOnOwningOrgRoles = new List<OrganizationRole>();
			Regions = new List<string>();
			Subjects = new List<string>();
		}
		//public List<OrganizationReference> OwningOrganizations { get; set; }
		public string OwningOrganization { get; set; }
		public int OwningOrganizationId { get; set; }

		public string Image { get; set; }

		//public IndexProperty CredentialType { get; set; }

		public string CredentialType { get; set; }
		public int CredentialTypeId { get; set; }

		public List<string> Keywords { get; set; }
		public List<string> Subjects { get; set; }

		public List<string> AlternateNames { get; set; }
		public List<string> CodedNotation { get; set; }
		public List<string> Languages { get; set; }

		public List<string> Regions { get; set; }
	//	public List<Place> Addresses { get; set; }

		public List<EntityReference> EmbeddedCredentials { get; set; }

		//public CredentialConnectionsResult CredentialsList { get; set; }
		////public List<Connection> CredentialConnections { get; set; }
		//public List<OrganizationRole> QAOnCredentialRoles { get; set; }
		//public List<OrganizationRole> QAOnOwningOrgRoles { get; set; }

		//public List<IndexCompetency> Competencies { get; set; }
		//public List<IndexFramework> Occupations { get; set; }
		//public List<IndexFramework> Industries { get; set; }


		public bool HasVerificationType_Badge { get; set; }

		public int HasPartCount { get; set; }
		public int IsPartOfCount { get; set; }
		public int RequiresCount { get; set; }
		public int RecommendsCount { get; set; }
		public int RequiredForCount { get; set; }
		public int IsRecommendedForCount { get; set; }
		public int RenewalCount { get; set; }
		public int IsAdvancedStandingForCount { get; set; }
		public int AdvancedStandingFromCount { get; set; }
		public int PreparationForCount { get; set; }
		public int PreparationFromCount { get; set; }

		public bool IsAQACredential { get; set; }
		public bool HasQualityAssurance { get; set; }

		public int NumberOfCostProfileItems { get; set; }
		//public List<CostProfile> EstimatedCost { get; set; }

	}

	
}
