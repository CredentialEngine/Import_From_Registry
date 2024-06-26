using System.Collections.Generic;
using System.Linq;
using System.Web;

using EntityHelper = workIT.Services.CollectionServices;
using ThisEntity = workIT.Models.Common.Collection;
using OutputEntity = workIT.Models.API.Collection;
using WMA = workIT.Models.API;
using workIT.Factories;

namespace workIT.Services.API
{
	/// <summary>
	/// Note that this will not be used, as the react page will mostly use the registry 
	/// </summary>
	public class CollectionServices
	{

		public static string searchType = "Collection";

		public static OutputEntity GetDetailForAPI( int id, bool skippingCache = false )
		{
			var record = EntityHelper.Get( id );
			return MapToAPI( record );

		}
		public static OutputEntity GetDetailByCtidForAPI( string ctid, bool skippingCache = false )
		{
			var record = EntityHelper.GetByCtid( ctid );
			return MapToAPI( record );
		}
		private static OutputEntity MapToAPI( ThisEntity input )
		{

			var output = new OutputEntity()
			{
				Meta_Id = input.Id,
				CTID = input.CTID,
				Name = input.Name,
				Meta_FriendlyName = HttpUtility.UrlPathEncode( input.Name ),
				Description = input.Description,
				SubjectWebpage = input.SubjectWebpage,
				EntityTypeId = 9,
				CredentialRegistryURL = RegistryServices.GetResourceUrl( input.CTID ),
				RegistryData = ServiceHelper.FillRegistryData( input.CTID, "Transfer Intermediary" )

			};
			if ( input.OwningOrganizationId > 0 )
			{
				output.OwnedByLabel = ServiceHelper.MapDetailLink( "Organization", input.OrganizationName, input.OwningOrganizationId, input.PrimaryOrganization.FriendlyName );
			}

			var orgOutline = ServiceHelper.MapToOutline( input.PrimaryOrganization, "organization" );
			//var work = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER );
			output.OwnedBy = ServiceHelper.MapOutlineToAJAX( orgOutline, "Owning Organization" );
			output.EntityLastUpdated = input.EntityLastUpdated;
			//
			//
			output.CodedNotation = input.CodedNotation;
			//actually this can be a list. Set searchType blank at this time
			output.CollectionType = ServiceHelper.MapPropertyLabelLinks( input.CollectionType, "" );
			//
			output.InCatalog = input.InCatalog;
			output.DateEffective = input.DateEffective;
			output.ExpirationDate = input.ExpirationDate;
			//
			if ( input.Keyword != null && input.Keyword.Any() )
				output.Keyword = ServiceHelper.MapPropertyLabelLinks( input.Keyword, searchType );
			output.License = input.License;
			output.LifeCycleStatusType = ServiceHelper.MapPropertyLabelLink( input.LifeCycleStatusType, searchType );
			output.MembershipCondition = ServiceHelper.MapToConditionProfiles( input.MembershipCondition, searchType );
			//New
			output.IndustryType = ServiceHelper.MapReferenceFramework( input.IndustryType, searchType, CodesManager.PROPERTY_CATEGORY_NAICS );
			output.OccupationType = ServiceHelper.MapReferenceFramework( input.OccupationType, searchType, CodesManager.PROPERTY_CATEGORY_SOC );
			output.InstructionalProgramType = ServiceHelper.MapReferenceFramework( input.InstructionalProgramType, searchType, CodesManager.PROPERTY_CATEGORY_CIP );

            output.LatestVersion = ServiceHelper.MapPropertyLabelLink( input.LatestVersion, "Latest Version" );
            output.NextVersion = ServiceHelper.MapPropertyLabelLink( input.NextVersion, "Next Version" );
            output.PreviousVersion = ServiceHelper.MapPropertyLabelLink( input.PreviousVersion, "Previous Version" );
            output.VersionIdentifier = ServiceHelper.MapIdentifierValue( input.VersionIdentifier, "Version Identifier" );

            var links = new List<WMA.LabelLink>();
            var work = new List<WMA.Outline>();

            if ( input.CollectionMemberCounts.Count > 0 )
			{
				foreach ( var item in input.CollectionMemberCounts )
				{
					//need to disable some searches like competencies
					if ( item.EntityTypeId != CodesManager.ENTITY_TYPE_COMPETENCY && item.EntityTypeId != CodesManager.ENTITY_TYPE_COLLECTION_COMPETENCY )
						ServiceHelper.MapCollectionEntitySearchLink( input.Id, input.Name, item.Totals, "Has {0} {1} Members", item.EntityType, item.EntityTypeId, ref links);
				}
			}
            //
            if ( input.HasSupportService?.Count > 0 )
            {
                work = new List<WMA.Outline>();
                foreach ( var target in input.HasSupportService )
                {
                    if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
                        work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
                }
                output.HasSupportService = ServiceHelper.MapOutlineToAJAX( work, "Has {0} Support Services" );

                //ServiceHelper.MapSupportServiceSearchLink( input.Id, input.Name, input.HasSupportService.Count, "Has {0} Support Services", "supportservice", ref links );

                //output.Connections = links;
            }
            //
            if ( links.Any() )
				output.Connections = links;

			//
			//output.IndustryType = ServiceHelper.MapReferenceFrameworkLabelLink( input.Industry, searchType, CodesManager.PROPERTY_CATEGORY_NAICS );
			//output.OccupationType = ServiceHelper.MapReferenceFrameworkLabelLink( input.Occupation, searchType, CodesManager.PROPERTY_CATEGORY_SOC );
			//output.InstructionalProgramType = ServiceHelper.MapReferenceFrameworkLabelLink( input.InstructionalProgramTypes, searchType, CodesManager.PROPERTY_CATEGORY_CIP );
			//
			//======================== for ===================================================
			
			if ( input.HasMember?.Count > 0 )
			{
                work = new List<WMA.Outline>();
                //foreach ( var target in input.HasMember )
                //{
                //	if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
                //		work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
                //}
                //output.HasMember = ServiceHelper.MapOutlineToAJAX( work, "Includes {0} Intermediary For" );

            }
			


			//
			output.InLanguage = null;
			return output;
		}

	}
}
