using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;
using MAPI=workIT.Models.API;
using WMS = workIT.Models.Search;

namespace workIT.Models.Elastic
{
	/*
	ceterms:commonCosts
ceterms:condition
ceterms:creditUnitTypeDescription
ceterms:creditValue
ceterms:dateEffective
ceterms:description
ceterms:estimatedCost
ceterms:experience
ceterms:jurisdiction
ceterms:minimumAge
ceterms:name
ceterms:residentOf
ceterms:subjectWebpage
ceterms:submissionOf
ceterms:submissionOfDescription
ceterms:targetAssessment
ceterms:targetCompetency
ceterms:targetCredential
ceterms:targetLearningOpportunity
ceterms:weight
ceterms:yearsOfExperience


	*/
	public class ConditionProfile
	{

		public string Name { get; set; }
		public string Description { get; set; }

		public WMS.AJAXSettings AssertedBy { get; set; }
		public List<MAPI.LabelLink> AudienceLevelType { get; set; } = new List<MAPI.LabelLink>();
		public List<MAPI.LabelLink> AudienceType { get; set; } = new List<MAPI.LabelLink>();
		public List<string> Condition { get; set; }
		//
		public string CreditUnitTypeDescription { get; set; }
		public MAPI.ValueProfile CreditValue { get; set; }
		public List<MAPI.LabelLink> CreditUnitType { get; set; } 

		public decimal CreditUnitValue { get; set; }
		public decimal CreditUnitMinValue { get; set; }

		public decimal CreditUnitMaxValue { get; set; }
		public bool CreditValueIsRange { get; set; }
		public string DateEffective { get; set; }
		//
		public List<CostManifest> CommonCosts { get; set; }

		public List<CostProfile> EstimatedCost { get; set; }
		public string Experience { get; set; }
		public List<JurisdictionProfile> Jurisdiction { get; set; }
		public int MinimumAge { get; set; }
		public List<JurisdictionProfile> ResidentOf { get; set; }
		public string SubjectWebpage { get; set; }
		public List<string> SubmissionOf { get; set; }
		public string SubmissionOfDescription { get; set; }
		//
		public WMS.AJAXSettings TargetAssessment { get; set; }
		public WMS.AJAXSettings TargetCredential { get; set; }
		
		public WMS.AJAXSettings TargetLearningOpportunity { get; set; }
		//
		public decimal Weight { get; set; }
		public decimal YearsOfExperience { get; set; }
		
	}
}
