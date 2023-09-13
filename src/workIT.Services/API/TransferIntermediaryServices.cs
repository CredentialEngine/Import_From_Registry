using System.Collections.Generic;
using System.Linq;
using System.Web;

using EntityHelper = workIT.Services.TransferIntermediaryServices;
using ThisEntity = workIT.Models.Common.TransferIntermediary;
using OutputEntity = workIT.Models.API.TransferIntermediary;
using WMA = workIT.Models.API;
namespace workIT.Services.API
{
    public class TransferIntermediaryServices
    {

		public static string searchType = "TransferIntermediary";

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
				EntityTypeId = 28,
				CredentialRegistryURL = RegistryServices.GetResourceUrl( input.CTID ),
				RegistryData = ServiceHelper.FillRegistryData( input.CTID, "Transfer Intermediary" )

			};
			if ( input.OwningOrganizationId > 0 )
			{
				output.OwnedByLabel = ServiceHelper.MapDetailLink( "Organization", input.OrganizationName, input.OwningOrganizationId, input.PrimaryOrganization.FriendlyName );
			}

            output.AlternateName = input.AlternateName;
            var orgOutline = ServiceHelper.MapToOutline( input.PrimaryOrganization, "organization" );
			//var work = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER );
			output.OwnedBy = ServiceHelper.MapOutlineToAJAX( orgOutline, "Owning Organization" );
			output.EntityLastUpdated = input.EntityLastUpdated;
			//
			//
			output.CodedNotation = input.CodedNotation;
			//CreditValue
			output.CreditValue = ServiceHelper.MapValueProfile( input.CreditValue, "" );
            //======================== for ===================================================
            var links = new List<WMA.LabelLink>();
            output.Connections = null;
            var work = new List<WMA.Outline>();
			if ( input.IntermediaryFor?.Count > 0 )
			{
				foreach ( var target in input.IntermediaryFor )
				{
					if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
				}
				output.IntermediaryFor = ServiceHelper.MapOutlineToAJAX( work, "Includes {0} Intermediary For" );
                ServiceHelper.MapTransferIntermediaryTVPSearchLink( input.Id, input.Name, input.IntermediaryFor.Count, "Has {0} Intermediary For", "transfervalue", ref links );
                output.Connections = links;
            }

            output.Requires = ServiceHelper.MapToConditionProfiles( input.Requires, searchType );
            if ( input.SubjectTVP != null && input.SubjectTVP.Any() )
                output.Subject = ServiceHelper.MapPropertyLabelLinks( input.SubjectTVP, searchType );

			//??this will be found in the condition profile???
			if ( input.RequiresCompetenciesFrameworks != null && input.RequiresCompetenciesFrameworks.Any() )
			{
				output.RequiresCompetencies = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Requires {#} Competenc{ies}", input.RequiresCompetenciesFrameworks );
			} else
			{

			}
			//
			output.InLanguage = null;
			return output;
		}

	}
}
