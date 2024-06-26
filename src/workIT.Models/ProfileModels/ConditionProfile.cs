﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;
//using MN = workIT.Models.Node;

namespace workIT.Models.ProfileModels
{

	public class ConditionProfile : BaseProfile
	{
		public ConditionProfile()
		{
			AssertedBy = new Organization();
			AudienceLevel = new Enumeration();
			ApplicableAudienceType = new Enumeration();
			CreditUnitType = new Enumeration();

			//ResidentOf = new List<GeoCoordinates>();
			ResidentOf = new List<JurisdictionProfile>();
			//TargetCompetency = new List<Enumeration>();
			TargetCompetency = new List<CredentialAlignmentObjectProfile>();
			RequiresCompetenciesFrameworks = new List<CredentialAlignmentObjectFrameworkProfile>();

			TargetAssessment = new List<AssessmentProfile>();
			TargetLearningOpportunity = new List<LearningOpportunityProfile>();
			TargetCredential = new List<Credential>();
			TargetOccupation = new List<OccupationProfile>();
			TargetJob = new List<Job>();
			//ReferenceUrl = new List<TextValueProfile>();
			//AssertedByOrgProfileLink = new MN.ProfileLink();
			//ReferenceUrl = new List<TextValueProfile>();
			Condition = new List<TextValueProfile>();
			SubmissionOf = new List<TextValueProfile>();



			AlternativeCondition = new List<ConditionProfile>();
			//AdditionalCondition = new List<ConditionProfile>();
			ParentConditionManifest = new ConditionManifest();
		}

		//Hack, but useful when condition profiles are grouped together
		public enum ConditionProfileTypes
		{
			UNKNOWN = 0,
			REQUIRES = 1,
			RECOMMENDS = 2,
			IS_REQUIRED_FOR = 3,
			IS_RECOMMENDED_FOR = 4,
			RENEWAL = 5,
			IS_ADVANCED_STANDING_FOR = 6,
			ADVANCED_STANDING_FROM = 7,
			IS_PREPARATION_FOR = 8,
			PREPARATION_FROM = 9,
			COREQUISITE = 10,
			ENTRY_CONDITION = 11,
            COPREREQUISITE = 12,
        }
		public static int CodeIdForType( ConditionProfileTypes type )
		{
			try
			{
				return ( int )type;
			}
			catch
			{
				return 0;
			}
		}
		public static ConditionProfileTypes TypeForCodeId( int codeID )
		{
			try
			{
				return ( ConditionProfileTypes )codeID;
			}
			catch
			{
				return ConditionProfileTypes.UNKNOWN;
			}
		}
		public static Dictionary<ConditionProfileTypes, List<ConditionProfile>> DisambiguateConditionProfiles( List<ConditionProfile> input )
		{
			var result = new Dictionary<ConditionProfileTypes, List<ConditionProfile>>();
			if ( input != null )
			{
				foreach ( ConditionProfileTypes item in Enum.GetValues( typeof( ConditionProfileTypes ) ) )
				{
					result.Add( item, input.Where( m => m.ConnectionProfileTypeId == CodeIdForType( item ) ).ToList() );
				}
			}
			return result;
		}
		//


		#region common properties
		//Alias used for publishing
		public string Name { get { return ProfileName; } set { ProfileName = value; } }

		public string ConnectionProfileType { get; set; }
		public int ConnectionProfileTypeId { get; set; }
		public int ConditionSubTypeId { get; set; }


		public Guid AssertedByAgentUid { get; set; }
		public List<Guid> AssertedByAgent { get; set; } = new List<Guid>();
		/// <summary>
		/// Inflate AssertedByAgentUid for display 
		/// </summary>
		public Organization AssertedBy { get; set; }
		#endregion

		#region general condition
		public string Experience { get; set; }
		public int MinimumAge { get; set; }
		public string SubjectWebpage { get; set; }

		public decimal Weight { get; set; }
		public decimal YearsOfExperience { get; set; }


		//public List<TextValueProfile> Auto_SubjectWebpage { get { return string.IsNullOrWhiteSpace( SubjectWebpage ) ? null : new List<TextValueProfile>() { new TextValueProfile() { TextValue = SubjectWebpage } }; } }

		//
		//21-02-15 mparsons - changing to a ValueProfile
		//public List<QuantitativeValue> CreditValueList2 { get; set; } = new List<QuantitativeValue>();
		public ValueProfile CreditValue { get; set; } = new ValueProfile();
	
		//20-07-24 updating to handle a list
		public List<ValueProfile> CreditValueList { get; set; } = new List<ValueProfile>();
		public string CreditValueJson { get; set; }


		//[Obsolete]
		//public string CreditHourType { get; set; }
		//[Obsolete]
		//public decimal CreditHourValue { get; set; }
		public int CreditUnitTypeId { get; set; }
		public Enumeration CreditUnitType { get; set; } //Used for publishing
		public string CreditUnitTypeDescription { get; set; }
		public decimal CreditUnitValue { get; set; }
		//public decimal CreditUnitMinValue { get; set; }

		//public decimal CreditUnitMaxValue { get; set; }
		//public bool CreditValueIsRange { get; set; }
		//
		public Enumeration AudienceLevel { get; set; }
		public Enumeration AudienceLevelType { get { return AudienceLevel; } set { AudienceLevel = value; } } //Alias used for publishing
		public Enumeration AudienceType
		{
			get { return ApplicableAudienceType; }
			set { ApplicableAudienceType = value; }
		} //Alias used for publishing

		//public string OtherCredentialType { get; set; }
		public Enumeration ApplicableAudienceType { get; set; }
		//public string OtherAudienceType { get; set; }
		//public List<GeoCoordinates> ResidentOf { get; set; }
		public List<JurisdictionProfile> ResidentOf { get; set; }
		#endregion


		#region Prperties for Import

		public List<int> TargetCredentialIds { get; set; }
		public List<int> TargetAssessmentIds { get; set; }
		public List<int> TargetLearningOpportunityIds { get; set; }
        public List<int> TargetOccupationIds { get; set; }
		public List<int> TargetJobIds { get; set; }
		#endregion

		#region Prperties for Display
		public List<CostProfile> EstimatedCosts { get; set; }
		public List<CostProfile> EstimatedCost { get { return EstimatedCosts; } set { EstimatedCosts = value; } } //Alias
		public List<CostManifest> CommonCosts { get; set; }

		public List<int> CostManifestIds { get; set; } = new List<int>();

		public Dictionary<string, RegistryImport> FrameworkPayloads = new Dictionary<string, RegistryImport>();
		public List<CredentialAlignmentObjectFrameworkProfile> RequiresCompetenciesFrameworks { get; set; }
		//IMPORT ?????
		public List<CredentialAlignmentObjectProfile> TargetCompetency { get; set; }
		public List<TextValueProfile> Condition { get; set; }

		public string SubmissionOfDescription { get; set; }
		public List<TextValueProfile> SubmissionOf { get; set; }
		public List<ConditionProfile> AlternativeCondition { get; set; }

		public List<Credential> TargetCredential { get; set; }

		public List<AssessmentProfile> TargetAssessment { get; set; }
		public List<LearningOpportunityProfile> TargetLearningOpportunity { get; set; }
        public List<OccupationProfile> TargetOccupation{ get; set; }
        public List<Job> TargetJob { get; set; }
        #endregion




        #region parents properties (HOW USED??)

        /// <summary>
        /// If referenced, indicates that the ParentCredential is the parent of the condition
        /// </summary>
        public Credential ParentCredential { get; set; }

		/// <summary>
		/// If referenced, indicates that the ParentAssessment is the parent of the condition
		/// </summary>
		public AssessmentProfile ParentAssessment { get; set; }

		/// <summary>
		/// If referenced, indicates that the ParentLearningOpportunity is the parent of the condition
		/// </summary>
		public LearningOpportunityProfile ParentLearningOpportunity { get; set; }

		public ConditionManifest ParentConditionManifest { get; set; }
		#endregion

		public string ConditionSubType
		{
			get
			{
				string conditionSubType = "Basic";
				if ( ConditionSubTypeId == 2 )
				{
					conditionSubType = "Credential Connection";
				}
				else if ( ConditionSubTypeId == 3 )
				{
					conditionSubType = "Assessment Connection";
				}
				else if ( ConditionSubTypeId == 4 )
				{
					conditionSubType = "Learning Opportunity Connection";
				}
				else
					conditionSubType = "Basic";

				return conditionSubType;
			}
		}




		public bool IsWorthDisplaying //Because credentials, assessments, and learning opportunities are stripped out on the detail page, we need an easy way to determine whether or not there is anything else worth showing in this profile
		{
			get
			{
				//Name alone is insufficient
				//Weight alone is insufficient
				//Ignore credentials, assessments, learning opportunities, competencies
				return !string.IsNullOrWhiteSpace( Description ) ||
					!string.IsNullOrWhiteSpace( Experience ) ||
					!string.IsNullOrWhiteSpace( SubmissionOfDescription ) ||
					( Condition != null && SubmissionOf.Count() > 0 ) ||
					YearsOfExperience > 0 ||
					MinimumAge > 0 ||
					!string.IsNullOrWhiteSpace( SubjectWebpage ) ||
					( Condition != null && Condition.Count() > 0 ) ||
					( AudienceLevel != null && AudienceLevel.Items != null && AudienceLevel.Items.Where( m => m != null ).Count() > 0 ) ||
					( ApplicableAudienceType != null && ApplicableAudienceType.Items != null && ApplicableAudienceType.Items.Where( m => m != null ).Count() > 0 ) ||
					//!string.IsNullOrWhiteSpace( CreditHourType ) ||
					//CreditHourValue > 0 ||
					( CreditUnitType != null && CreditUnitType.Items != null && CreditUnitType.Items.Where( m => m != null ).Count() > 0 ) ||
					!string.IsNullOrWhiteSpace( CreditUnitTypeDescription ) ||
					CreditUnitValue > 0 ||
					( ResidentOf != null && ResidentOf.Count() > 0 ) ||
					( Jurisdiction != null && Jurisdiction.Count() > 0 );// ||
																		 //Not sure how to handle these yet
																		 //(AlternativeCondition != null && AlternativeCondition.Count() > 0) ||
																		 //(AdditionalCondition != null && AdditionalCondition.Count() > 0);
			}
		}
	}
	//TBD - use or not
	public class ConditionProfileJson
	{

		public List<string> AudienceType { get; set; } = new List<string>();
		public List<string> AudienceLevel { get; set; } = new List<string>();

		public ValueProfile CreditValue { get; set; } = new ValueProfile();
		public List<JurisdictionProfile> Jurisdiction { get; set; }
		public List<JurisdictionProfile> ResidentOf { get; set; }
	}

}
