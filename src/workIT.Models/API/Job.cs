﻿using System.Collections.Generic;

using WMA = workIT.Models.API;
using WMS = workIT.Models.Search;

namespace workIT.Models.API
{
    public class Job : BaseEmploymentObject
    {
        /*
        ceasn:abilityEmbodied
        ceasn:comment
        ceasn:knowledgeEmbodied
        ceasn:skillEmbodied
        ceterms:alternateName
        ceterms:classification
        ceterms:codedNotation
        ceterms:ctid
        ceterms:description
        ceterms:hasJob					*
        ceterms:hasSpecialization		*
        ceterms:hasWorkforceDemand		*
        ceterms:hasWorkRole				*	
        ceterms:identifier
        ceterms:industryType			*
        ceterms:isSpecializationOf		*
        ceterms:keyword					*
        ceterms:name
        ceterms:occupationType			*
        ceterms:requires
        ceterms:sameAs					*
        ceterms:subjectWebpage
        */
        public Job()
        {
            EntityTypeId = 32;
            BroadType = "Job";
            CTDLType = "ceterms:Job";
            CTDLTypeLabel = "Job";
        }

        public List<string> AlternateName { get; set; } = new List<string>();
        public WMS.AJAXSettings HasJob { get; set; }

        //public List<IdentifierValue> Identifier { get; set; }
        public List<LabelLink> Keyword { get; set; }


        public List<ReferenceFramework> IndustryType { get; set; } = new List<ReferenceFramework>();
        public List<ReferenceFramework> OccupationType { get; set; } = new List<ReferenceFramework>();

        public WMS.AJAXSettings HasOccupation { get; set; }
        public WMS.AJAXSettings HasSupportService { get; set; }
        public WMS.AJAXSettings HasTask { get; set; }
        public WMS.AJAXSettings HasWorkRole { get; set; }
		public WMS.AJAXSettings TargetPathway { get; set; }


		public List<WMA.ConditionProfile> Requires { get; set; } = new List<ConditionProfile>();

        public List<string> SameAs { get; set; } = new List<string>();
    }
}
