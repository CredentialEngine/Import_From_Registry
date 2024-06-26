using System;
using System.Collections.Generic;
using System.Web;

using workIT.Factories;
using workIT.Utilities;

using WMA = workIT.Models.API;
using OutputResource = workIT.Models.API.CredentialingAction;
using ResourceHelper = workIT.Services.CredentialingActionServices;
using ThisResource = workIT.Models.Common.CredentialingAction;
using ResourceManager = workIT.Factories.CredentialingActionManager;

namespace workIT.Services.API
{
	public class CredentialingActionServices
	{
		static string thisClassName = "API.CredentialingActionServices";
		public static string searchType = "CredentialingAction";
		public static string EntityType = "CredentialingAction";
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
				CTID = input.CTID,
				Name = input.Name,
				Description = input.Description,
				EntityLastUpdated = input.EntityLastUpdated,
				Meta_StateId = input.EntityStateId,
				CTDLTypeLabel = input.CTDLTypeLabel,
				CTDLType = input.EntityType,
				CredentialRegistryURL = RegistryServices.GetResourceUrl( input.CTID )

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
			//check for others
			output.EvidenceOfAction = input.EvidenceOfAction;
			output.ActionStatusType = ServiceHelper.MapPropertyLabelLink( input.ActionStatusType, searchType );
			output.ActionType = ServiceHelper.MapPropertyLabelLink( input.ActionType, searchType );
			output.Participant = ServiceHelper.MapResourceSummaryAJAXSettings( input.Participant, "Organization" );
			output.Instrument = ServiceHelper.MapResourceSummaryAJAXSettings( input.Instrument, "Credential" );
			output.Image = input.Image;
			if ( !string.IsNullOrWhiteSpace( input.StartDate ) )
				output.StartDate = input.StartDate;
			else
				output.StartDate = "";
			//
			if ( !string.IsNullOrWhiteSpace( input.EndDate ) )
				output.EndDate = input.EndDate;
			else
				output.EndDate = "";
			var work = new List<WMA.Outline>();
			work.Add( ServiceHelper.MapToOutline( input.Object, input.Object.EntityType ) );
			//foreach ( var target in input.ObjectCredential )
			//{
			//	if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
			//		work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
			//}
			//foreach ( var target in input.ObjectAsmt )
			//{
			//	if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
			//		work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
			//}
			//foreach ( var target in input.ObjectLopp )
			//{
			//	if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
			//		work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
			//}
			//foreach ( var target in input.ObjectOrg )
			//{
			//	if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
			//		work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
			//}
			output.Object = ServiceHelper.MapOutlineToAJAX( work, "Object" );

			//
			output.Meta_FriendlyName = HttpUtility.UrlPathEncode( input.Name );

			//something for primary org, but maybe not ownedBy
			if ( input.PrimaryOrganizationId > 0 )
			{
				output.OwnedByLabel = ServiceHelper.MapDetailLink( "Organization", input.PrimaryOrganization.Name, input.PrimaryOrganizationId, input.PrimaryOrganization.FriendlyName );
			}
			var offeredBy = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_AssertedBy );
			output.ActingAgent = ServiceHelper.MapOutlineToAJAX( offeredBy, "Offered by {0} Organization(s)" );
			//should only do if different from owner! Actually only populated if by a 3rd party
			output.Publisher = ServiceHelper.MapOutlineToAJAX( ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY ), "Published By" );

			//
			return output;
		}


	}
}
