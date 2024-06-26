using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using workIT.Factories;
using workIT.Models.Common;
using workIT.Models.Search;
using workIT.Utilities;


using ElasticHelper = workIT.Services.ElasticServices;
using ResourceManager = workIT.Factories.TaskManager;
using ThisResource = workIT.Models.Common.Task;
using OutputResource = workIT.Models.API.Task;
using ResourceHelper = workIT.Services.TaskServices;

using WMA = workIT.Models.API;
using WMP = workIT.Models.ProfileModels;
using WorkITSearchServices = workIT.Services.SearchServices;

namespace workIT.Services.API
{
    public class TaskServices
    {
        static string thisClassName = "API.TaskServices";
        public static string searchType = "Task";
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
        public static OutputResource GetDetailForElastic( int id, bool skippingCache )
        {
            var record = ResourceHelper.GetDetail( id );
            return MapToAPI( record );
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
           
            try
            {

                //

               // output.Identifier = ServiceHelper.MapIdentifierValue( input.Identifier );
                output.Comment = input.Comment;
                output.CodedNotation = input.CodedNotation;
                output.ListId = input.ListId;
                output.InCatalog = input.InCatalog;
                output.Identifier = ServiceHelper.MapIdentifierValue( input.Identifier );
                output.VersionIdentifier = ServiceHelper.MapIdentifierValue( input.VersionIdentifier );
                output.LifeCycleStatusType = ServiceHelper.MapPropertyLabelLink( input.LifeCycleStatusType, searchType );
                output.TargetCompetency = ServiceHelper.ConvertCredentialAlignmentObjectProfileToAJAXSettingsForDetail( "Requires {#} Competenc{ies}", input.TargetCompetency );
                //output.HasOccupation = ServiceHelper.MapResourceSummaryAJAXSettings( input.HasOccupation, "Occupation" );
                //output.HasJob = ServiceHelper.MapResourceSummaryAJAXSettings( input.HasJob, "Job" );
                //output.HasWorkRole = ServiceHelper.MapResourceSummaryAJAXSettings( input.HasWorkRole, "WorkRole" );
                output.HasChild = ServiceHelper.MapResourceSummaryAJAXSettings( input.HasChild, "Task" );
                output.KnowledgeEmbodied = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Knowledge Embodied {#} Competenc{ies}", input.KnowledgeEmbodiedOutput );
                output.SkillEmbodied = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Skill Embodied {#} Competenc{ies}", input.SkillEmbodiedOutput );
                output.AbilityEmbodied = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Ability Embodeied {#} Competenc{ies}", input.AbilityEmbodiedOutput );
                output.PerformanceLevelType = ServiceHelper.MapResourceSummaryAJAXSettings( input.PerformanceLevelType, "Concept" );
                output.PhysicalCapabilityType = ServiceHelper.MapResourceSummaryAJAXSettings( input.PhysicalCapabilityType, "Concept" );
                output.EnvironmentalHazardType = ServiceHelper.MapResourceSummaryAJAXSettings( input.EnvironmentalHazardType, "Concept" );
                output.SensoryCapabilityType = ServiceHelper.MapResourceSummaryAJAXSettings( input.SensoryCapabilityType, "Concept" );
                output.Classification = ServiceHelper.MapResourceSummaryAJAXSettings( input.Classification, "Concept" );
                //owned by and offered by 
                //need a label link for header
                output.HasRubric = ServiceHelper.MapResourceSummaryAJAXSettings( input.HasRubric, "Rubric" );

                output.RelatedWorkRole = ServiceHelper.MapResourceSummaryAJAXSettings( input.RelatedWorkRole, "WorkRole" );
                output.RelatedJob = ServiceHelper.MapResourceSummaryAJAXSettings( input.RelatedJob, "Job" );
                output.RelatedOccupation = ServiceHelper.MapResourceSummaryAJAXSettings( input.RelatedOccupation, "Occupation" );
                if ( input.RelatedCollection != null && input.RelatedCollection.Count > 0 )
                {
                    output.Collections = ServiceHelper.MapCollectionMemberToOutline( input.RelatedCollection );
                }
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
                    if ( input.HasJob?.Count > 0 )
                    {
                        foreach ( var target in input.HasJob )
                        {
                            if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
                                work.Add( ServiceHelper.MapToOutline( target, "Job" ) );
                        }
                        output.HasJob = ServiceHelper.MapOutlineToAJAX( work, "Includes {0} Jobs" );
                    }
                    work = new List<WMA.Outline>();
                    if ( input.HasChild?.Count > 0 )
                    {
                        foreach ( var target in input.HasChild )
                        {
                            if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
                                work.Add( ServiceHelper.MapToOutline( target, "Task" ) );
                        }
                        output.HasChild = ServiceHelper.MapOutlineToAJAX( work, "Has {0} Child" );
                    }
                }
                catch ( Exception ex )
                {
                    LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 3, Name: {0}, Id: {1}", input.Name, input.Id ) );

                }
                if ( input.OwningOrganizationId > 0 )
                {
                    output.OwnedByLabel = ServiceHelper.MapDetailLink( "Organization", input.OrganizationName, input.OwningOrganizationId, input.PrimaryOrganization.FriendlyName );
                }
                 var assertedBy = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_AssertedBy );
                if ( assertedBy?.Count > 0 && assertedBy != null )
                {
                    output.AssertedBy = ServiceHelper.MapOutlineToAJAX( assertedBy, "" );
                }

            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 1, Name: {0}, Id: {1}", input.Name, input.Id ) );

            }

            //=======================================
            try
            {
                //

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
