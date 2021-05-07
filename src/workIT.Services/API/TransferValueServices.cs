using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using workIT.Models;
using workIT.Models.Common;
using WMA = workIT.Models.API;
using workIT.Models.Search;
using workIT.Factories;
using workIT.Utilities;
using ElasticHelper = workIT.Services.ElasticServices;
using EntityHelper = workIT.Services.TransferValueServices;
using ThisEntity = workIT.Models.Common.TransferValueProfile;
using ThisEntityDetail = workIT.Models.API.TransferValueProfile;

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
				FriendlyName = HttpUtility.UrlPathEncode ( input.Name ),
				Description = input.Description,
				SubjectWebpage = input.SubjectWebpage,
				EntityTypeId = 26,
				StartDate = input.StartDate,
				EndDate = input.EndDate,
				CredentialRegistryURL = RegistryServices.GetResourceUrl( input.CTID ),
				RegistryData = ServiceHelper.FillRegistryData(input.CTID)

			};
			if ( input.OwningOrganizationId > 0 )
			{
				output.OwnedByLabel = ServiceHelper.MapDetailLink( "Organization", input.OrganizationName, input.OwningOrganizationId );
			}

			var orgOutline = ServiceHelper.MapToOutline( input.OwningOrganization, "organization" );
			//var work = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER );
			output.OwnedBy = ServiceHelper.MapOutlineToAJAX( orgOutline, "Owning Organization" );
			//
			//
			output.Identifier = ServiceHelper.MapIdentifierValue( input.Identifier );
			//transferValue
			output.TransferValue = ServiceHelper.MapValueProfile( input.TransferValue, "" );
			//for
			var work = new List<WMA.Outline>();
			foreach ( var target in input.TransferValueFor )
			{
				if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
					work.Add( ServiceHelper.MapToOutline( target, "" ) );
			}
			output.TransferValueFor = ServiceHelper.MapOutlineToAJAX( work, "Includes {0} Transfer Value For" );

			//from
			work = new List<WMA.Outline>();
			foreach ( var target in input.TransferValueFrom )
			{
				if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
					work.Add( ServiceHelper.MapToOutline( target, "" ) );
			}
			output.TransferValueFrom = ServiceHelper.MapOutlineToAJAX( work, "Includes {0} Transfer Value From" );
			//
			if ( input.DevelopmentProcess.Any() )
			{
				output.DevelopmentProcess = ServiceHelper.MapAJAXProcessProfile( "Development Process", "", input.DevelopmentProcess );
			}

			//
			output.InLanguage = null;
			return output;
		}
	}
}
