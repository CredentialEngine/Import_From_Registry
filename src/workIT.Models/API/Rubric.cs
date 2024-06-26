using Newtonsoft.Json;

using System.Collections.Generic;

using WMS = workIT.Models.Search;

namespace workIT.Models.API
{
    public class Rubric : BaseAPIType
    {

        public Rubric()
        {
            EntityTypeId = 39;
            CTDLTypeLabel = "Rubric";
            CTDLType = "ceasn:Rubric";
            BroadType = "Rubric";
        }

        public string CodedNotation { get; set; }
        public List<string> AltCodedNotation { get; set; }

        public string HasScope { get; set; }
        public string Rights { get; set; }
        public string License { get; set; }

        public LabelLink LatestVersion { get; set; }
        public LabelLink NextVersion { get; set; } //URL
        public LabelLink PreviousVersion { get; set; }
        public List<IdentifierValue> VersionIdentifier { get; set; }
        public List<IdentifierValue> Identifier { get; set; }
        public WMS.AJAXSettings Classification { get; set; }

        public string DateCreated { get; set; }
        public string DateModified { get; set; }
        public string DateCopyrighted { get; set; }
        public string DateValidFrom { get; set; }
        public string DateValidUntil { get; set; }
        public string InCatalog { get; set; }

        public List<ReferenceFramework> OccupationType { get; set; } = new List<ReferenceFramework>();
        public List<ReferenceFramework> InstructionalProgramType { get; set; } = new List<ReferenceFramework>();
        public List<ReferenceFramework> IndustryType { get; set; } = new List<ReferenceFramework>();
        public List<LabelLink> ConceptKeyword { get; set; } = new List<LabelLink>();

        public List<LabelLink> Subject { get; set; } = new List<LabelLink>();

        //
        public WMS.AJAXSettings HasCriterionCategorySet { get; set; }
        public WMS.AJAXSettings TargetOccupation { get; set; }
        public WMS.AJAXSettings HasProgressionModel { get; set; }
        public WMS.AJAXSettings HasProgressionLevel { get; set; }

        public List<LabelLink> AudienceLevelType { get; set; } = new List<LabelLink>();
        public List<LabelLink> AudienceType { get; set; } = new List<LabelLink>();
        public List<LabelLink> DeliveryType { get; set; } = new List<LabelLink>();
        public List<LabelLink> EducationLevelType { get; set; } = new List<LabelLink>();
        public List<LabelLink> EvaluatorType { get; set; } = new List<LabelLink>();
        public List<RubricCriterion> RubricCriterions { get; set; } = new List<RubricCriterion>();
        public List<RubricLevel> RubricLevels { get; set; } = new List<RubricLevel>();

        public List<CriterionLevel> CriterionLevels { get; set; } = new List<CriterionLevel>();
        public List<ProgressionLevel> ProgressionLevels { get; set; } = new List<ProgressionLevel>();
    }

    public class RubricCriterion
    {
        public string CTID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CodedNotation { get; set; }
        public string ListID { get; set; }
        public string Weight { get; set; }
        public WMS.AJAXSettings HasProgressionLevel { get; set; }
        public WMS.AJAXSettings TargetCompetency { get; set; }
        public WMS.AJAXSettings TargetTask { get; set; }
        public List<CriterionLevel> HasCriterionLevel { get; set; }

    }
    public class RubricLevel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string CodedNotation { get; set; }
        public string ListID { get; set; }
        public WMS.AJAXSettings HasProgressionLevel { get; set; }
        public List<CriterionLevel> HasCriterionLevel { get; set; }

    }

    public class CriterionLevel
    {
        public string RowId { get; set; }
        public string BenchmarkLabel { get; set; } //??
        public string BenchmarkText { get; set; }
        public string CodedNotation { get; set; }
        public string ListID { get; set; }
        public string Feedback { get; set; }
        public decimal? Value { get; set; } = null;
        public decimal? MinValue { get; set; } = null;
        public decimal? MaxValue { get; set; } = null;
        public decimal? Percentage { get; set; } = null;
        public decimal? MinPercentage { get; set; } = null;
        public decimal? MaxPercentage { get; set; } = null;
    }
}
