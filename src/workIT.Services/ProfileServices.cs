using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elasticsearch.Net;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;

namespace workIT.Services
{
	public class ProfileServices
	{
		public static string thisClassName = "ProfileServices";
		public static List<TopLevelObject> ResolveToTopLevelObject( List<Guid> input, string property, ref SaveStatus status )
		{
			var list = new List<TopLevelObject>();
			foreach(var item in input )
			{
				var tlo = GetEntityAsTopLevelObject( item );
				if ( tlo != null && tlo.Id > 0 )
					list.Add( tlo );
				else
				{
					status.AddError( string.Format( "ProfileServicesError.ResolveToTopLevelObject: For property: '{0}' unable to resolve GUID: '{1}' to a top level object.", property, item.ToString() ) );
				}
			}
			//may be common to want the output sorted by entity type? If so do before returning

			return list;
		}
		public static TopLevelObject GetEntityAsTopLevelObject(Guid uid)
		{
			TopLevelObject tlo = new TopLevelObject();

			var entity = EntityManager.GetEntity( uid, false );
			if ( entity == null || entity.Id == 0 )
				return null;
			//
			if (entity.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL)
			{
				//actually should return some type info
				tlo = CredentialManager.GetBasic( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_ORGANIZATION )
			{
				tlo = OrganizationManager.GetBasics( entity.EntityUid );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
			{
				tlo = AssessmentManager.GetBasic( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
			{
				tlo = LearningOpportunityManager.GetBasic( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_PATHWAY )
			{
				tlo = PathwayManager.GetBasic( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT )
			{
				tlo = PathwayComponentManager.Get( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_PATHWAY_SET )
			{
				tlo = PathwaySetManager.Get( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			else if ( entity.EntityTypeId == CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE )
			{
				tlo = TransferValueProfileManager.Get( entity.EntityBaseId );
				tlo.EntityTypeId = entity.EntityTypeId;
			}
			return tlo;
		}

		public static void HandleAddressGeoCoding()
		{
			//should we do all?
			int maxRecords = 0;
			LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".HandleAddressGeoCoding - maxRecords: {0}", maxRecords ) );
			DateTime started = DateTime.Now;
			string report = "";
			string messages = "";
			var list = new Entity_AddressManager().ResolveMissingGeodata( ref messages, maxRecords );

			var saveDuration = DateTime.Now.Subtract( started );
			LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".NormalizeAddresses - Completed - seconds: {0}", saveDuration.Seconds ) );
			if ( !string.IsNullOrWhiteSpace( messages ) )
				report = string.Format( "<p>Normalize Addresses. Duration: {0} seconds <br/>", saveDuration.Seconds ) + messages + "</p>";

			foreach ( var address in list )
			{
				string msg = string.Format( " - Unable to resolve address: Id: {0}, address1: {1}, city: {2}, region: {3}, postalCode: {4}, country: {5} ", address.Id, address.StreetAddress, address.AddressLocality, address.AddressRegion, address.PostalCode, address.AddressCountry );
				LoggingHelper.DoTrace( 2, msg );
				report += System.Environment.NewLine + msg;
			}
			//no reporting of successes here 

		}
		public void NormalizeAddressesExternal( string authorization, int maxRecords, ref string message )
		{
			LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".NormalizeAddressesExternal - starting maxRecords: {0}", maxRecords ) );
			//do validation
			if ( ( authorization ?? "" ).ToLower() != "bca5a70f-cf0d-4b27-8566-9f874d88741e" )
			{
				message= "You are not authorized to invoke NormalizeAddressesExternal.";
				return;
			}
			//may be OK from API
			if ( UtilityManager.GetAppKeyValue( "envType" ) == "development" )
			{
				//message="Sorry the NormalizeAddresses process is not available in the development environment (not allowed by Google)." ;
				//return;
			}

			int addressesFixed = 0;
			int addressRemaining = 0;
			DateTime started = DateTime.Now;
			
			string messages = "";
			List<Address> list = new Entity_AddressManager().ResolveMissingGeodata( ref messages, ref addressesFixed, ref addressRemaining, maxRecords );

			var saveDuration = DateTime.Now.Subtract( started );
			LoggingHelper.DoTrace( 5, thisClassName + string.Format( ".NormalizeAddressesExternal - Completed - seconds: {0}", saveDuration.Seconds ) );
			int addressesNotFixed = list.Count();
			message = string.Format( "Normalize Addresses. Duration: {0} seconds, Addresses Fixed: {1}, Not Fixed: {2} Remaining: {3}", saveDuration.Seconds, addressesFixed, addressesNotFixed, addressRemaining ) ;
			
			//where called externally, don't need details, just count. Could be used in a loop.
			//foreach ( var address in list )
			//{
			//	if ( !string.IsNullOrWhiteSpace( address.StreetAddress ) )
			//	{
			//		string msg = string.Format( " - Unable to resolve address: Id: {0}, address1: {1}, city: {2}, region: {3}, postalCode: {4}, country: {5} ", address.Id, address.StreetAddress, address.AddressLocality, address.AddressRegion, address.PostalCode, address.AddressCountry );
			//		LoggingHelper.DoTrace( 2, msg );
			//		report += System.Environment.NewLine + msg;
			//	}
			//}
		}
	}
}
