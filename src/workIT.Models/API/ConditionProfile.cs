﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ME = workIT.Models.Elastic;
using WMS = workIT.Models.Search;

namespace workIT.Models.API
{

	public class ConditionProfile
	{
		public ConditionProfile()
		{
			CTDLTypeLabel = "Condition Profile";
		}
		public string CTDLTypeLabel { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }

		public WMS.AJAXSettings AssertedBy { get; set; }
		public List<LabelLink> AudienceLevelType { get; set; } 
		public List<LabelLink> AudienceType { get; set; } 
		public List<string> Condition { get; set; }
		//
		public string CreditUnitTypeDescription { get; set; }
		public List<ValueProfile> CreditValue { get; set; }

		public string DateEffective { get; set; }
		//
		public List<CostManifest> CommonCosts { get; set; }

		public List<ME.CostProfile> EstimatedCost { get; set; }
		public string Experience { get; set; }
		public List<ME.JurisdictionProfile> Jurisdiction { get; set; }
		public int? MinimumAge { get; set; }
		public List<ME.JurisdictionProfile> ResidentOf { get; set; }
		public string SubjectWebpage { get; set; }
		public List<string> SubmissionOf { get; set; }
		public string SubmissionOfDescription { get; set; }
		//
		public WMS.AJAXSettings TargetAssessment { get; set; }
		public WMS.AJAXSettings TargetCredential { get; set; }

		public WMS.AJAXSettings TargetLearningOpportunity { get; set; }
		//
		public decimal? Weight { get; set; }
		public decimal? YearsOfExperience { get; set; }

	}
}
