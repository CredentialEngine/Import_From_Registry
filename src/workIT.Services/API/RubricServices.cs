using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Web;

using workIT.Models;
using MC = workIT.Models.Common;
using WMA = workIT.Models.API;
using WMS = workIT.Models.Search;
using workIT.Factories;

using workIT.Utilities;
using ElasticHelper = workIT.Services.ElasticServices;
using EntityHelper = workIT.Services.RubricServices;

using ThisResource = workIT.Models.Common.Rubric;
using ThisResourceDetail = workIT.Models.API.Rubric;


namespace workIT.Services.API
{
	public class RubricServices
	{
        static string thisClassName = "API.RubricServices";
        public static string searchType = "rubric";

        public static ThisResourceDetail GetDetailForAPI( int id, bool skippingCache = false )
        {
            var output = EntityHelper.GetDetail( id, skippingCache );
            return MapToAPI( output );

        }
        public static ThisResourceDetail GetDetailByCtidForAPI( string ctid, bool skippingCache = false )
        {
            var output = EntityHelper.GetDetailByCtid( ctid, skippingCache );
            return MapToAPI( output );
        }

        private static ThisResourceDetail MapToAPI( ThisResource input )
        {


            var output = new ThisResourceDetail()
            {
                Meta_Id = input.Id,
                CTID = input.CTID,
                Name = input.Name,
                Meta_FriendlyName = HttpUtility.UrlPathEncode( input.Name ),
                Description = input.Description,
                SubjectWebpage = input.SubjectWebpage,
                EntityTypeId = CodesManager.ENTITY_TYPE_RUBRIC,
                CredentialRegistryURL = RegistryServices.GetResourceUrl( input.CTID ),
                RegistryData = ServiceHelper.FillRegistryData( input.CTID, searchType )
            };
            output.EntityLastUpdated = input.EntityLastUpdated;
            output.Meta_StateId = input.EntityStateId;
            if ( input.PrimaryOrganizationId > 0 )
            {
                output.OwnedByLabel = ServiceHelper.MapDetailLink( "Organization", input.PrimaryOrganization.Name, input.PrimaryOrganizationId, input.PrimaryOrganization.FriendlyName );
            }

            try
            {
                output.CodedNotation = input.CodedNotation;
                output.AltCodedNotation = input.AltCodedNotation;
                output.InCatalog = input.InCatalog;

                output.DateCreated = input.DateCreated;
                output.DateModified = input.DateModified;
                output.DateCopyrighted = input.DateCopyrighted;
                output.DateValidFrom = input.DateValidFrom;
                output.DateValidUntil = input.DateValidUntil;

                output.HasScope = input.HasScope;
                output.License = input.License;
                output.Rights = input.Rights;

                output.Identifier = ServiceHelper.MapIdentifierValue( input.Identifier );
                output.LatestVersion = ServiceHelper.MapPropertyLabelLink( input.LatestVersion, "Latest Version" );
                output.NextVersion = ServiceHelper.MapPropertyLabelLink( input.NextVersion, "Next Version" );
                output.PreviousVersion = ServiceHelper.MapPropertyLabelLink( input.PreviousVersion, "Previous Version" );
                output.VersionIdentifier = ServiceHelper.MapIdentifierValue( input.VersionIdentifier );
                output.LifeCycleStatusType = ServiceHelper.MapPropertyLabelLink( input.LifeCycleStatusType, searchType );

                output.AudienceLevelType = ServiceHelper.MapPropertyLabelLinks( input.AudienceLevelType, searchType );
                output.AudienceType = ServiceHelper.MapPropertyLabelLinks( input.AudienceType, searchType );
                output.DeliveryType = ServiceHelper.MapPropertyLabelLinks( input.DeliveryType, searchType );
                output.EducationLevelType = ServiceHelper.MapPropertyLabelLinks( input.EducationLevelType, searchType );
                output.EvaluatorType = ServiceHelper.MapPropertyLabelLinks( input.EvaluatorType, searchType );

                var creator = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER );
                output.Creator = ServiceHelper.MapOutlineToAJAX( creator, "Created by {0} Organization(s)" );

                //should only do if different from owner! Actually only populated if by a 3rd party
                output.Publisher = ServiceHelper.MapOutlineToAJAX( ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY ), "Published By" );

                output.IndustryType = ServiceHelper.MapReferenceFramework( input.IndustryTypes, searchType, CodesManager.PROPERTY_CATEGORY_NAICS );
                output.OccupationType = ServiceHelper.MapReferenceFramework( input.OccupationTypes, searchType, CodesManager.PROPERTY_CATEGORY_SOC );
                output.InstructionalProgramType = ServiceHelper.MapReferenceFramework( input.InstructionalProgramTypes, searchType, CodesManager.PROPERTY_CATEGORY_CIP );
                //
                if ( input.ConceptKeyword != null && input.ConceptKeyword.Any() )
                    output.ConceptKeyword = ServiceHelper.MapPropertyLabelLinks( input.ConceptKeyword, searchType );
                if ( input.Subject != null && input.Subject.Any() )
                    output.Subject = ServiceHelper.MapPropertyLabelLinks( input.Subject, searchType );

                output.Classification = ServiceHelper.MapResourceSummaryAJAXSettings( input.Classification, "Concept" );
                output.TargetOccupation = ServiceHelper.MapResourceSummaryAJAXSettings( input.TargetOccupation, "Occupation" );

                try
                {
                    //
                    var work = new List<WMA.Outline>();
                    if ( input.HasCriterionCategorySet != null && !string.IsNullOrWhiteSpace( input.HasCriterionCategorySet.Name ) )
                        work.Add( ServiceHelper.MapToOutline( input.HasCriterionCategorySet, "ConceptScheme" ) );
                    output.HasCriterionCategorySet = ServiceHelper.MapOutlineToAJAX( work, "Includes {0} ConceptScheme" );

                    // work = new List<WMA.Outline>();
                    //if ( input.HasProgressionModel != null && !string.IsNullOrWhiteSpace( input.HasProgressionModel.Name ) )
                    //    work.Add( ServiceHelper.MapToOutline( input.HasProgressionModel, "HasProgressionModel" ) );
                    //output.HasProgressionModel = ServiceHelper.MapOutlineToAJAX( work, "Includes {0} HasProgressionModel" );
                    // work = new List<WMA.Outline>();
                    //if ( input.HasProgressionLevel != null && !string.IsNullOrWhiteSpace( input.HasProgressionLevel.Name ) )
                    //    work.Add( ServiceHelper.MapToOutline( input.HasProgressionLevel, "HasProgressionLevel" ) );
                    //output.HasProgressionLevel = ServiceHelper.MapOutlineToAJAX( work, "Includes {0} HasProgressionLevel" );

                }
                catch ( Exception ex )
                {
                    LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 1, Name: {0}, Id: {1}", input.Name, input.Id ) );

                }
                var progressionLevels = new List<WMA.ProgressionLevel>();
                if(input.ProgressionModel!= null && input.ProgressionModel.HasConcepts != null )
                {
                    foreach ( var criterion in input.ProgressionModel.HasConcepts )
                    {
                        var plevel = new WMA.ProgressionLevel
                        {
                            PrefLabel = criterion.PrefLabel,
                            Definition = criterion.Definition,
                            CTID = criterion.CTID,
                        };
                        progressionLevels.Add( plevel );
                    }
                    output.ProgressionLevels = progressionLevels;
                }
             
                var rubricCriterions = new List<WMA.RubricCriterion>();
                foreach ( var criterion in input.RubricCriterion )
                {
                    var rc = new WMA.RubricCriterion
                    {
                        Name = criterion.Name,
                        Description = criterion.Description,
                        CTID = criterion.CTID,
                        ListID = criterion.ListID,
                        Weight = criterion.Weight != 0? criterion.Weight.ToString(): null,
                        CodedNotation=criterion.CodedNotation,
                        TargetTask = ServiceHelper.MapResourceSummaryAJAXSettings( criterion.TargetTask, "Task" ),
                    TargetCompetency = ServiceHelper.ConvertCredentialAlignmentObjectProfileToAJAXSettingsForDetail( "Requires {#} Competenc{ies}", criterion.TargetCompetency ),
                        HasCriterionLevel = MapCriterionLevel(criterion.HasCriterionLevel),
                    };
                    try
                    {
                        var work = new List<WMA.Outline>();
                        if ( criterion.HasProgressionLevel != null && !string.IsNullOrWhiteSpace( criterion.HasProgressionLevel.Name ) )
                            work.Add( ServiceHelper.MapToOutline( criterion.HasProgressionLevel, "HasProgressionLevel" ) );
                        rc.HasProgressionLevel = ServiceHelper.MapOutlineToAJAX( work, "Includes {0} HasProgressionLevel" );

                    }
                    catch ( Exception ex )
                    {
                        LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 1, Name: {0}, Id: {1}", input.Name, input.Id ) );

                    }
                    rubricCriterions.Add( rc );
                }
                output.RubricCriterions = rubricCriterions;

                var rubricLevels = new List<WMA.RubricLevel>();
                foreach ( var level in input.RubricLevel )
                {
                    var rl = new WMA.RubricLevel
                    {
                        Name = level.Name,
                        Description = level.Description,
                        ListID = level.ListID,
                        CodedNotation=level.CodedNotation,
                        HasCriterionLevel = MapCriterionLevel( level.HasCriterionLevel ),
                    };
                    try
                    {
                        var work = new List<WMA.Outline>();
                        if ( level.HasProgressionLevel != null && !string.IsNullOrWhiteSpace( level.HasProgressionLevel.Name ) )
                            work.Add( ServiceHelper.MapToOutline( level.HasProgressionLevel, "HasProgressionLevel" ) );
                        rl.HasProgressionLevel = ServiceHelper.MapOutlineToAJAX( work, "Includes {0} HasProgressionLevel" );

                    }
                    catch ( Exception ex )
                    {
                        LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 1, Name: {0}, Id: {1}", input.Name, input.Id ) );

                    }
                    rubricLevels.Add( rl);
                }
                output.RubricLevels = rubricLevels;

                

            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 1, Name: {0}, Id: {1}", input.Name, input.Id ) );

            }
            return output;
        }

        private static List<WMA.CriterionLevel> MapCriterionLevel(List<MC.CriterionLevel> input )
        {
            var criterionLevels = new List<WMA.CriterionLevel>();
            foreach ( var level in input )
            {
                var rl = new WMA.CriterionLevel
                {
                    RowId=level.RowId.ToString(),
                    BenchmarkLabel = level.BenchmarkLabel,
                    BenchmarkText = level.BenchmarkText,
                    ListID = level.ListID,
                    CodedNotation = level.CodedNotation,
                    Feedback = level.Feedback,
                    Value = level.Value,
                    MinValue = level.MinValue,
                    MaxValue = level.MaxValue,
                    Percentage = level.Percentage,
                    MaxPercentage = level.MaxPercentage,
                    MinPercentage = level.MinPercentage
                };
                criterionLevels.Add( rl );
            }
            return criterionLevels;
        }

    }
}
