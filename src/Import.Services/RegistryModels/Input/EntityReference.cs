using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RA.Models.Input
{

	/// <summary>
	/// An outline entity used to determine the type of a entity to be processed.
	/// </summary>
	public class BaseEntityReference
	{
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

		//if there is no available Id/CTID, enter the following, where Type, Name, Description, and subjectwebpage would typically be required

		/// <summary>
		/// the type of the entity must be provided if the Id was not provided. examples
		/// ceterms:AssessmentProfile
		/// ceterms:LearningOpportunityProfile
		/// ceterms:ConditionManifest
		/// ceterms:CostManifest
		/// or the many credential subclasses!!
		/// </summary>
		public string Type { get; set; }

		/// <summary>
		/// Name of the entity (required)
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// the input classes don't use ceterms, etc. Ensure that each controller sets the default type.
		/// This could be an issue with lists of objects like for TVP and collections.
		/// </summary>
		public bool IsAssessmentType
		{
			get
			{
				if ( Type == "ceterms:AssessmentProfile"
					|| Type == "AssessmentProfile"
					)
					return true;
				else
					return false;
			}
		}
		public bool IsCompetencyType
		{
			get
			{
				if ( Type == "ceasn:Competency"
					|| Type == "Competency"
					)
					return true;
				else
					return false;
			}
		}
		//public bool IsCredentialType
		//{
		//	get
		//	{
				
		//		if ( Type == "ceasn:Competency"
		//			|| Type == "Competency"
		//			)
		//			return true;
		//		else
		//			return false;
		//	}
		//}
		public bool IsLearningOpportunityType
		{
			get 
			{
				if ( Type == "ceterms:LearningOpportunityProfile"
					|| Type == "LearningOpportunityProfile"
					|| Type == "Course"
					|| Type == "LearningProgram"
					)
					return true;
				else
					return false;
			}
		}

		public bool IsJobType
		{
			get
			{
				if ( Type == "Job"
					|| Type == "ceterms:Job"
					)
					return true;
				else
					return false;
			}
		}
		public bool IsOccupationType
		{
			get
			{
				if ( Type == "Occupation"
					|| Type == "ceterms:Occupation"
					)
					return true;
				else
					return false;
			}
		}
		public bool IsTaskType
		{
			get
			{
				if ( Type == "Task"
					|| Type == "ceterms:Task"
					)
					return true;
				else
					return false;
			}
		}
		public bool IsWorkRoleType
		{
			get
			{
				if ( Type == "WorkRole"
					|| Type == "ceterms:WorkRole"
					)
					return true;
				else
					return false;
			}
		}
	}

	/// <summary>
	/// Class for handling references to an entity such as an Assessment, Organization, Learning opportunity, or credential that may or may not be in the Credential Registry.
	/// Either the Id as an resolvable URL, a CTID where the document exists in the Credential Registry, or provide specific properities for the entity.
	/// If neither a CTID or Id is provided, a blank node will be added the @graph.
	/// </summary>
	public class EntityReference
	{
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

		//if there is no available Id/CTID, enter the following, where Type, Name, Description, and subjectwebpage would typically be required

		/// <summary>
		/// the type of the entity must be provided if the Id was not provided. examples
		/// ceterms:AssessmentProfile
		/// ceterms:LearningOpportunityProfile
		/// ceterms:ConditionManifest
		/// ceterms:CostManifest
		/// or the many credential subclasses!!
		/// </summary>
		public string Type { get; set; }

		/// <summary>
		/// Name of the entity (required)
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Alternately can provide a language map
		/// </summary>
		public LanguageMap Name_Map { get; set; } = new LanguageMap();
		/// <summary>
		/// If the entity described below does exist in the registry, use this SameAs property to relate the two. 
		/// Provide a CTID(recommended) or a URI to the thing in the credential registry.
		/// </summary>
		public string SameAs { get; set; }

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
		/// Check if all properties for a reference request are present
		/// 17-08-27 We do need a type if only providing reference data
		/// </summary>
		/// <returns></returns>
		public bool HasNecessaryProperties()
		{
			//	|| string.IsNullOrWhiteSpace( Description )
			if ( (string.IsNullOrWhiteSpace( Name ) || Name_Map?.Count == 0)
				|| string.IsNullOrWhiteSpace( Type )
				|| string.IsNullOrWhiteSpace( SubjectWebpage )
				)
				return false;
			else
				return true;
		}
		public virtual bool IsEmpty()
		{
			if ( string.IsNullOrWhiteSpace( Id )
				&& string.IsNullOrWhiteSpace( CTID )
				&& ( string.IsNullOrWhiteSpace( Name ) || Name_Map?.Count == 0 )
				&& ( string.IsNullOrWhiteSpace( Description ) || Description_Map?.Count == 0 )
				&& string.IsNullOrWhiteSpace( SubjectWebpage )
				)
				return true;
			else
				return false;
		}
	}
}
