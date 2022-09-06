using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//using System.ComponentModel.DataAnnotations;
using workIT.Models.ProfileModels;

namespace workIT.Models.Common
{
	public class Pathway : TopLevelObject
	{
		public static int PathwayRelationship_IsDestinationComponentOf = 1;
		public static int PathwayRelationship_HasChild = 2;
		public static int PathwayRelationship_IsPartOf = 3;

		public enum PathwayRelationships
		{
			UNKNOWN = 0,
			IsDestinationComponentOf = 1,
			HasChild=2
			//IsPartOf = 3
		}
		public Pathway()
		{
			EntityTypeId = 8;
		}
		public int PathwayRelationshipTypeId { get; set; }

		public PathwaySet PathwaySet { get; set; } = new PathwaySet();
        //public string ExternalIdenifier { get; set; }
		//list of all component
		public List<PathwayComponent> HasPart { get; set; } = new List<PathwayComponent>();
		public List<PathwayComponent> HasChild { get; set; } = new List<PathwayComponent>();
        public List<PathwayComponent> HasDestinationComponent{ get; set; } = new List<PathwayComponent>();
		/// <summary>
		/// ConceptScheme or a ProgressionModel (TBD)
		/// URI
		/// </summary>
		public ConceptScheme HasProgressionModel { get; set; } = new ConceptScheme();
		public string ProgressionModelURI { get; set; }
		//??
		public Enumeration OwnerRoles { get; set; } = new Enumeration();
		//??
		public List<OrganizationRoleProfile> OrganizationRole { get; set; } = new List<OrganizationRoleProfile>();
		public List<OrganizationRoleProfile> OwnerOrganizationRoles { get; set; }
		public List<Organization> OwnedByOrganization { get; set; } = new List<Organization>();

		public List<Organization> OfferedByOrganization { get; set; } = new List<Organization>();

		public Enumeration IndustryType { get; set; } = new Enumeration();
		public List<TextValueProfile> AlternativeIndustries { get; set; } = new List<TextValueProfile>();

		public Enumeration OccupationType { get; set; } = new Enumeration();
		public List<TextValueProfile> AlternativeOccupations { get; set; } = new List<TextValueProfile>();



		public List<TextValueProfile> Keyword { get; set; } = new List<TextValueProfile>();
		public List<TextValueProfile> Subject { get; set; } = new List<TextValueProfile>();

		public string Properies { get; set; }

		//fake properties used by detail page
		public string AvailabilityListing { get; set; }

		#region Import
		public List<CredentialAlignmentObjectProfile> OccupationTypes { get; set; } = new List<CredentialAlignmentObjectProfile>();
		public List<CredentialAlignmentObjectProfile> IndustryTypes { get; set; } = new List<CredentialAlignmentObjectProfile>();

		public List<Guid> HasPartList { get; set; } = new List<Guid>();
		public List<Guid> HasChildList { get; set; } = new List<Guid>();
		public List<Guid> HasDestinationList { get; set; } = new List<Guid>();
		public List<Guid> OwnedBy { get; set; } = new List<Guid>();
		public List<Guid> OfferedBy { get; set; } = new List<Guid>();
		
		#endregion
	}


	public class Entity_HasPathway
	{
		public int Id { get; set; }
		public int EntityId { get; set; }
		public int PathwayId { get; set; }
		public int PathwayRelationshipTypeId { get; set; }
		public string PathwayName { get; set; }
		public System.DateTime Created { get; set; }
		public int CreatedById { get; set; }

		public virtual Entity Entity { get; set; }
		public virtual Pathway Pathway { get; set; }
	}

}
