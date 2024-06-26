using System;
using System.Collections.Generic;
using System.Web;

using workIT.Factories;
using workIT.Utilities;

using WMA = workIT.Models.API;
using OutputResource = workIT.Models.API.Job;
using ResourceHelper = workIT.Services.JobServices;
using ThisResource = workIT.Models.Common.Job;
using ResourceManager = workIT.Factories.JobManager;
using System.Linq;

namespace workIT.Services.API
{
    public class JobServices
    {
        static string thisClassName = "API.JobServices";
        public static string searchType = "Job";
        public static string EntityType = "Job";
        public static OutputResource GetDetailForAPI( int id, bool skippingCache = false )
        {
            OutputResource outputEntity = new OutputResource();

            //only cache longer processes
            DateTime start = DateTime.Now;
            var entity = ResourceManager.GetForDetail( id );

            DateTime end = DateTime.Now;
            //for now don't include the mapping in the elapsed
            int elasped = ( DateTime.Now - start ).Seconds;
            outputEntity = MapToAPI( entity );

            return outputEntity;

        }
        public static OutputResource GetDetailByCtidForAPI( string ctid, bool skippingCache = false )
        {
            var credential = ResourceManager.GetMinimumByCtid( ctid );
            return GetDetailForAPI( credential.Id, skippingCache );

        }

        private static OutputResource MapToAPI( ThisResource input )
        {

            var output = new OutputResource()
            {
                Meta_Id = input.Id,
				Meta_RowId = input.RowId,
				CTID = input.CTID,
                Name = input.Name,
                Description = input.Description,
                EntityLastUpdated = input.EntityLastUpdated,
                SubjectWebpage = input.SubjectWebpage,
                Meta_StateId = input.EntityStateId,

            };
            if ( input.EntityStateId == 0 )
            {
                return output;
            }

            if ( !string.IsNullOrWhiteSpace( input.CTID ) )
            {
                output.CredentialRegistryURL = RegistryServices.GetResourceUrl( input.CTID );

                output.RegistryData = ServiceHelper.FillRegistryData( input.CTID, searchType );
                //experimental - not used in UI yet
                output.RegistryDataList.Add( output.RegistryData );
            }
            //
			if ( string.IsNullOrWhiteSpace( input.CTID ) )
			{
				//output.IsReferenceVersion = true;
				output.Name += " [reference]";
			}
			else
				//output.IsReferenceVersion = false;

			//
			output.Meta_FriendlyName = HttpUtility.UrlPathEncode( input.Name );
            output.AlternateName = input.AlternateName;

            //something for primary org, but maybe not ownedBy
            if ( input.PrimaryOrganizationId > 0 )
            {
                output.OwnedByLabel = ServiceHelper.MapDetailLink( "Organization", input.PrimaryOrganizationName, input.PrimaryOrganizationId, input.PrimaryOrganization.FriendlyName );
            }
            var offeredBy = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY );
            output.OfferedBy = ServiceHelper.MapOutlineToAJAX( offeredBy, "Offered by {0} Organization(s)" );

            //should only do if different from owner! Actually only populated if by a 3rd party
            output.Publisher = ServiceHelper.MapOutlineToAJAX( ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY ), "Published By" );

            try
            {
                output.Comment = input.Comment;
                output.CodedNotation = input.CodedNotation;
                output.Identifier = ServiceHelper.MapIdentifierValue( input.Identifier );
                output.VersionIdentifier = ServiceHelper.MapIdentifierValue( input.VersionIdentifier );
                output.Keyword = ServiceHelper.MapPropertyLabelLinks( input.Keyword, searchType );
                output.IndustryType = ServiceHelper.MapReferenceFramework( input.IndustryType, searchType, CodesManager.PROPERTY_CATEGORY_NAICS );
                output.OccupationType = ServiceHelper.MapReferenceFramework( input.OccupationType, searchType, CodesManager.PROPERTY_CATEGORY_SOC );
                output.LifeCycleStatusType = ServiceHelper.MapPropertyLabelLink( input.LifeCycleStatusType, searchType );
                output.TargetCompetency =ServiceHelper.ConvertCredentialAlignmentObjectProfileToAJAXSettingsForDetail("Requires {#} Competenc{ies}", input.TargetCompetency); 
                output.Requires = ServiceHelper.MapToConditionProfiles( input.Requires, searchType );
                output.SameAs = input.SameAs;
                output.InCatalog = input.InCatalog;

                output.HasTask = ServiceHelper.MapResourceSummaryAJAXSettings( input.HasTask, "Task" );
                output.HasWorkRole = ServiceHelper.MapResourceSummaryAJAXSettings( input.HasWorkRole, "WorkRole" );
                output.KnowledgeEmbodied = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Knowledge Embodied {#} Competenc{ies}", input.KnowledgeEmbodiedOutput );
                output.SkillEmbodied = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Skill Embodied {#} Competenc{ies}", input.SkillEmbodiedOutput );
                output.AbilityEmbodied = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Ability Embodeied {#} Competenc{ies}", input.AbilityEmbodiedOutput );
                output.PerformanceLevelType = ServiceHelper.MapResourceSummaryAJAXSettings( input.PerformanceLevelType, "Concept" );
                output.PhysicalCapabilityType = ServiceHelper.MapResourceSummaryAJAXSettings( input.PhysicalCapabilityType, "Concept" );
                output.EnvironmentalHazardType = ServiceHelper.MapResourceSummaryAJAXSettings( input.EnvironmentalHazardType, "Concept" );
                output.SensoryCapabilityType = ServiceHelper.MapResourceSummaryAJAXSettings( input.SensoryCapabilityType, "Concept" );
                output.Classification = ServiceHelper.MapResourceSummaryAJAXSettings( input.Classification, "Concept" );

                output.ProvidesTransferValueFor = ServiceHelper.MapResourceSummaryAJAXSettings( input.ProvidesTransferValueFor, "TransferValue" );
                output.ReceivesTransferValueFrom = ServiceHelper.MapResourceSummaryAJAXSettings( input.ReceivesTransferValueFrom, "TransferValue" );
                output.HasRubric = ServiceHelper.MapResourceSummaryAJAXSettings( input.HasRubric, "Rubric" );

                output.RelatedWorkRole = ServiceHelper.MapResourceSummaryAJAXSettings( input.RelatedWorkRole, "WorkRole" );
                output.RelatedOccupation = ServiceHelper.MapResourceSummaryAJAXSettings( input.RelatedOccupation, "Occupation" );
                output.RelatedTask = ServiceHelper.MapResourceSummaryAJAXSettings( input.RelatedTask, "Task" );
                if ( input.RelatedCollection != null && input.RelatedCollection.Count > 0 )
                {
                    output.Collections = ServiceHelper.MapCollectionMemberToOutline( input.RelatedCollection );
                }
                //owned by and offered by 
                //need a label link for header

            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 1, Name: {0}, Id: {1}", input.Name, input.Id ) );

            }

            //=======================================
            try
            {
                //
                var work = new List<WMA.Outline>();
                if ( input.HasWorkRole?.Count > 0 )
                {
                    foreach ( var target in input.HasWorkRole )
                    {
                        if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
                            work.Add( ServiceHelper.MapToOutline( target, "workrole" ) );
                    }
                    output.HasWorkRole = ServiceHelper.MapOutlineToAJAX( work, "Includes {0} Work Roles" );
                }
                work = new List<WMA.Outline>();
                if ( input.HasOccupation?.Count > 0 )
                {
                    foreach ( var target in input.HasOccupation )
                    {
                        if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
                            work.Add( ServiceHelper.MapToOutline( target, "occupation" ) );
                    }
                    output.HasOccupation = ServiceHelper.MapOutlineToAJAX( work, "Includes {0} Occupations" );
                }
                work = new List<WMA.Outline>();
                if ( input.HasTask?.Count > 0 )
                {
                    foreach ( var target in input.HasTask )
                    {
                        if ( target != null && !string.IsNullOrWhiteSpace( target.Description ) )
                            if ( string.IsNullOrWhiteSpace( target.Name ) )
                            {
                                target.Name = target.Description.Length > 150 ? target.Description.Substring( 0, 150 ) : target.Description;
                            }
                        work.Add( ServiceHelper.MapToOutline( target, "task" ) );
                    }
                    output.HasTask = ServiceHelper.MapOutlineToAJAX( work, "Includes {0} Tasks" );
                }

                //
                if ( input.HasSupportService?.Count > 0 )
                {
                    work = new List<WMA.Outline>();
                    foreach ( var target in input.HasSupportService )
                    {
                        if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
                            work.Add( ServiceHelper.MapToOutline( target, EntityType ) );
                    }
                    output.HasSupportService = ServiceHelper.MapOutlineToAJAX( work, "Has {0} Support Services" );
                }

				output.TargetPathway = ServiceHelper.MapPathwayToAJAXSettings( input.TargetPathway, "Has {0} Target Pathway(s)" );

			}
			catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 3, Name: {0}, Id: {1}", input.Name, input.Id ) );

            }

            //
            return output;
        }


    }
}
