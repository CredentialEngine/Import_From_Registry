using System.Collections.Generic;
using System.Linq;
using System.Web;

using EntityHelper = workIT.Services.TransferValueServices;
using ThisEntity = workIT.Models.Common.TransferValueProfile;
using WMA = workIT.Models.API;

namespace workIT.Services.API
{
    public class TransferValueServices
	{
		public static string searchType = "TransferValue";

		public static WMA.TransferValueProfile GetDetailForAPI( int id, bool skippingCache = false )
		{
			var record = EntityHelper.Get( id );
			return MapToAPI( record );

		}
		public static WMA.TransferValueProfile GetDetailByCtidForAPI( string ctid, bool skippingCache = false )
		{
			var record = EntityHelper.GetByCtid( ctid );
			return MapToAPI( record );
		}
		private static WMA.TransferValueProfile MapToAPI( ThisEntity input )
		{

			var output = new WMA.TransferValueProfile()
			{
				Meta_Id = input.Id,
				CTID = input.CTID,
				Name = input.Name,
				Meta_FriendlyName = HttpUtility.UrlPathEncode ( input.Name ),
				Description = input.Description,
				SubjectWebpage = input.SubjectWebpage,
				EntityTypeId = 26,
				StartDate = input.StartDate,
				EndDate = input.EndDate,
				CredentialRegistryURL = RegistryServices.GetResourceUrl( input.CTID ),
				RegistryData = ServiceHelper.FillRegistryData( input.CTID, "Transfer Value Profile" )

			};
			if ( input.OwningOrganizationId > 0 )
			{
				output.OwnedByLabel = ServiceHelper.MapDetailLink( "Organization", input.OrganizationName, input.OwningOrganizationId, input.OwningOrganization.FriendlyName );
			}
			output.LifeCycleStatusType = ServiceHelper.MapPropertyLabelLink( input.LifeCycleStatusType, searchType );

			var orgOutline = ServiceHelper.MapToOutline( input.OwningOrganization, "organization" );
			//var work = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER );
			output.OwnedBy = ServiceHelper.MapOutlineToAJAX( orgOutline, "Owning Organization" );
			output.EntityLastUpdated = input.EntityLastUpdated;
			output.LifeCycleStatusType = ServiceHelper.MapPropertyLabelLink( input.LifeCycleStatusType, searchType );

			//
			output.Identifier = ServiceHelper.MapIdentifierValue( input.Identifier );
			//transferValue
			output.TransferValue = ServiceHelper.MapValueProfile( input.TransferValue, "" );
			//======================== for ===================================================
			var work = new List<WMA.Outline>();
			if ( input.TransferValueForCredential != null && input.TransferValueForCredential.Any() )
			{
				foreach ( var target in input.TransferValueForCredential )
				{
					if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
				}
				//do this after checking all 
				//output.TransferValueFor = ServiceHelper.MapOutlineToAJAX( work, "Includes {0} Transfer Value For" );
			}
			if (input.TransferValueForLopp != null && input.TransferValueForLopp .Any())
			{
				foreach ( var target in input.TransferValueForLopp )
				{
					if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
				}
			}
			if ( input.TransferValueForAsmt != null && input.TransferValueForAsmt.Any() )
			{
				foreach ( var target in input.TransferValueForAsmt )
				{
					if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
				}
			}


			//foreach ( var target in input.TransferValueFor )
			//{
			//	if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
			//		work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
			//}
			output.TransferValueFor = ServiceHelper.MapOutlineToAJAX( work, "Includes {0} Transfer Value For" );

			//======================== from ===================================================
			work = new List<WMA.Outline>();
			foreach ( var target in input.TransferValueFromCredential )
			{
				if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
					work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
			}
			foreach ( var target in input.TransferValueFromAsmt )
			{
				if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
					work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
			}
			foreach ( var target in input.TransferValueFromLopp )
			{
				if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
					work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
			}

			//foreach ( var target in input.TransferValueFrom )
			//{
			//	if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
			//		work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
			//}
			output.TransferValueFrom = ServiceHelper.MapOutlineToAJAX( work, "Includes {0} Transfer Value From" );
			//
			work = new List<WMA.Outline>();
			//if ( input.HasTransferIntermediary?.Count > 0 )
			//{
			//	foreach ( var target in input.HasTransferIntermediary )
			//	{
			//		if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
			//			work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
			//	}
			//	output.HasIntermediaryFor = ServiceHelper.MapOutlineToAJAX( work, "Includes {0} Intermediary For" );
			//}
			//
			if ( input.DevelopmentProcess.Any() )
			{
				output.DevelopmentProcess = ServiceHelper.MapAJAXProcessProfile( "Development Process", "", input.DevelopmentProcess );
			}
			//HasTransferIntermediary

			//
			work = new List<WMA.Outline>();
			foreach ( var target in input.DerivedFrom )
			{
				if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
					work.Add( ServiceHelper.MapToOutline( target, "" ) );
			}
			output.DerivedFrom = ServiceHelper.MapOutlineToAJAX( work, "Includes {0} Derived From" );
			//
			output.InLanguage = null;
			return output;
		}
	}
}
