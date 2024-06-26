using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using workIT.Factories;
using workIT.Models.Common;
using workIT.Models.Search;
using workIT.Utilities;


using ElasticHelper = workIT.Services.ElasticServices;
using ResourceManager = workIT.Factories.WorkRoleManager;
using ThisResource = workIT.Models.Common.WorkRole;
using OutputResource = workIT.Models.API.WorkRole;
using ResourceHelper = workIT.Services.WorkRoleServices;

using WMA = workIT.Models.API;
using WMP = workIT.Models.ProfileModels;
using WorkITSearchServices = workIT.Services.SearchServices;

namespace workIT.Services.API
{
    public class WorkRoleServices
    {
        static string thisClassName = "API.WorkRoleServices";
        public static string searchType = "WorkRole";
        public static string EntityType = "WorkRole";
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

            //should only do if different from owner! Actually only populated if by a 3rd party
            //output.Publisher = ServiceHelper.MapOutlineToAJAX( ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY ), "Published By" );

            try
            {

                //
                output.Comment = input.Comment;
                output.CodedNotation = input.CodedNotation;
                output.InCatalog = input.InCatalog;
                output.Identifier = ServiceHelper.MapIdentifierValue( input.Identifier );
                output.VersionIdentifier = ServiceHelper.MapIdentifierValue( input.VersionIdentifier );
                output.LifeCycleStatusType = ServiceHelper.MapPropertyLabelLink( input.LifeCycleStatusType, searchType );
                output.TargetCompetency = ServiceHelper.ConvertCredentialAlignmentObjectProfileToAJAXSettingsForDetail( "Requires {#} Competenc{ies}", input.TargetCompetency );
				//output.HasTask = ServiceHelper.MapResourceSummaryAJAXSettings( input.HasTask, "Task" );
				//output.HasOccupation = ServiceHelper.MapResourceSummaryAJAXSettings( input.HasOccupation, "Occupation" );
				// output.KnowledgeEmbodied = ServiceHelper.MapResourceSummaryAJAXSettings( input.KnowledgeEmbodied, "Competency" );
				//output.AbilityEmbodied = ServiceHelper.MapResourceSummaryAJAXSettings( input.AbilityEmbodied, "Competency" );
				//output.SkillEmbodied = ServiceHelper.MapResourceSummaryAJAXSettings( input.SkillEmbodied, "Competency" );
				output.PerformanceLevelType = ServiceHelper.MapResourceSummaryAJAXSettings( input.PerformanceLevelType, "Concept" );
				output.PhysicalCapabilityType = ServiceHelper.MapResourceSummaryAJAXSettings( input.PhysicalCapabilityType, "Concept" );
				output.EnvironmentalHazardType = ServiceHelper.MapResourceSummaryAJAXSettings( input.EnvironmentalHazardType, "Concept" );
				output.SensoryCapabilityType = ServiceHelper.MapResourceSummaryAJAXSettings( input.SensoryCapabilityType, "Concept" );
				output.Classification = ServiceHelper.MapResourceSummaryAJAXSettings( input.Classification, "Concept" );
                //Commenting the previous for reference now the competencies are using the GetCompetenices method to reduce the load time. #US285
                //output.KnowledgeEmbodied = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Knowledge Embodied {#} Competenc{ies}", input.KnowledgeEmbodiedOutput);
                //output.SkillEmbodied = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Skill Embodied {#} Competenc{ies}", input.SkillEmbodiedOutput );
                //output.AbilityEmbodied = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Ability Embodeied {#} Competenc{ies}", input.AbilityEmbodiedOutput );
                ////owned by and offered by 
                //need a label link for header
                if ( input.OwningOrganizationId > 0 )
                {
                    output.OwnedByLabel = ServiceHelper.MapDetailLink( "Organization", input.OrganizationName, input.OwningOrganizationId, input.PrimaryOrganization.FriendlyName );
                }
                var assertedBy = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_AssertedBy );
                if ( assertedBy?.Count > 0 && assertedBy != null )
                {
                    output.AssertedBy = ServiceHelper.MapOutlineToAJAX( assertedBy, "" );
                }
                output.RelatedJob = ServiceHelper.MapResourceSummaryAJAXSettings( input.RelatedJob, "Job" );
                output.RelatedOccupation = ServiceHelper.MapResourceSummaryAJAXSettings( input.RelatedOccupation, "Occupation" );
                output.RelatedTask = ServiceHelper.MapResourceSummaryAJAXSettings( input.RelatedTask, "Task" );
                if ( input.RelatedCollection != null && input.RelatedCollection.Count > 0 )
                {
                    output.Collections = ServiceHelper.MapCollectionMemberToOutline( input.RelatedCollection );
                }

            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 1, Name: {0}, Id: {1}", input.Name, input.Id ) );

            }
            try
            {
                //
                var work = new List<WMA.Outline>();
				if ( input.HasTask?.Count > 0 )
				{
					foreach ( var target in input.HasTask )
					{
						if ( target != null && !string.IsNullOrWhiteSpace( target.Description ) )
							if ( string.IsNullOrWhiteSpace( target.Name ) )
							{
								target.Name = target.Description.Length > 150 ? target.Description.Substring( 0, 150 ) : target.Description;
							}
						work.Add( ServiceHelper.MapToOutline( target, "Task" ) );
					}
					output.HasTask = ServiceHelper.MapOutlineToAJAX( work, "Includes {0} Tasks" );
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
               
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 3, Name: {0}, Id: {1}", input.Name, input.Id ) );

            }

            //
            return output;
        }

        public static OutputResource GetCompetencies( string ctid )
        {
            var details = WorkRoleManager.GetCompetencies( ctid );
            var output = new OutputResource();
            output.KnowledgeEmbodied = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Knowledge Embodied {#} Competenc{ies}", details.KnowledgeEmbodiedOutput );
            output.SkillEmbodied = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Skill Embodied {#} Competenc{ies}", details.SkillEmbodiedOutput );
            output.AbilityEmbodied = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Ability Embodeied {#} Competenc{ies}", details.AbilityEmbodiedOutput );
            return output;
        }


    }
}
