using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using CM = workIT.Models.Common;
using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;
using ViewContext = workIT.Data.Views.workITViews;
using ThisResource = workIT.Models.Common.Address;
using DBResource = workIT.Data.Tables.Entity_Address;
using EntityContext = workIT.Data.Tables.workITEntities;

using workIT.Utilities;
using workIT.Models.Search.ThirdPartyApiModels;
using workIT.Models.ProfileModels;
using Newtonsoft.Json;

namespace workIT.Factories
{
	public class Entity_AddressManager : BaseFactory
	{
		static string thisClassName = "Entity_AddressManager";

		#region Persistance - Entity_Address
		public bool SaveList( List<ThisResource> list, Guid parentUid, ref SaveStatus status )
		{
            if ( !IsValidGuid( parentUid ) )
            {
                status.AddError( string.Format( "A valid parent identifier was not provided to the {0}.Add method.", thisClassName ) );
                return false;
            }

            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( "Error - the parent entity was not found." );
                return false;
            }
			//20-12-05 - skip delete all, and check in save
			//DeleteAll( parent, ref status );

			//now maybe still do a delete all until implementing a balance line
			//could set date and delete all before this date!
			DateTime updateDate = DateTime.Now;
			if ( IsValidDate( status.EnvelopeUpdatedDate ) )
			{
				updateDate = status.LocalUpdatedDate;
			}

			var currentAddresses = GetAll( parent.EntityUid );
			bool isAllValid = true;
			//use addressCount to track actual addresses. Used later for validation
			var addressCount = 0;
			if ( list == null || list.Count == 0 )
			{
				if ( currentAddresses != null && currentAddresses.Any() )
				{
					LoggingHelper.DoTrace( LoggingHelper.appTraceLevel, $"{thisClassName}.SaveList. No addresses in input, cleared existing address ({currentAddresses.Count}) for parent: {parent.EntityBaseName} ({parent.EntityTypeId})." );
					DeleteAll( parent, ref status );
				}
				return true;
			}
			//may not need this if the new list version works
			else if ( list.Count == 1 && currentAddresses.Count == 1 )
			{
				//just do update of one
				var entity = list[ 0 ];
				entity.Id = currentAddresses[ 0 ].Id;

				Save( entity, parent, updateDate, ref status );
			}
			else
			{
				foreach ( ThisResource item in list )
				{
					if (item.HasAddress())
					{
						addressCount++;
					}
					Save( item, parent, updateDate, ref status );
				}
				//delete any addresses with last updated less than updateDate
				DeleteAll( parent, ref status, updateDate );
				//extra check where input count is less than output count
				//the input could contain an entry with just contact points
				var latestAddresses = GetAll( parent.EntityUid );
				if (latestAddresses.Count() != addressCount )
				{
					//21-11-02 mp - noticed that this can happen when there are duplicate addresses in the input
					status.AddWarning( string.Format( "The number of addresses in the import: {0} is different than the number of addresses after the import: {1}. Need to determine why? Parent: '{2}' ({3}).", list.Count(), latestAddresses.Count(), parent.EntityBaseName, parent.EntityBaseId ) );

				}


			}
			return isAllValid;
		}
		private bool Save( ThisResource entity, Entity parent, DateTime updateDate, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;

			var doingGeoCoding = UtilityManager.GetAppKeyValue( "doingGeoCodingImmediately", false );
			try
			{
				using ( var context = new EntityContext() )
				{
					DBResource efEntity = new DBResource();
					if ( ValidateProfile( entity, parent, ref status ) == false )
					{
						//always try to save
						//return false;
					}
					bool resetIsPrimaryFlag = false;
					//typically all will have an entity.Id of zero
					//could do a lookup
					if ( entity.Id == 0 )
					{
						efEntity = new DBResource();
						//check for current match - only do if not deleting
						var exists = context.Entity_Address
							.Where( s => s.EntityId == parent.Id
								&& s.Address1 == entity.StreetAddress
								&& s.City == entity.AddressLocality
								&& ( s.PostalCode == entity.PostalCode || ( s.PostalCode ?? string.Empty ).IndexOf( entity.PostalCode ?? string.Empty ) == 0 )  //could have been normalized to full 
									//&& s.Region == from.AddressRegion	//could have been expanded
							)
							.OrderBy( s => s.Address1 ).ThenBy( s => s.City ).ThenBy( s => s.PostalCode ).ThenBy( s => s.Region )
							.ToList();
						if ( exists != null && exists.Count > 0 )
						{
							//should only be one
							efEntity = exists[ 0 ];
							entity.Id = efEntity.Id;
							entity.RelatedEntityId = parent.Id;
							entity.RowId = efEntity.RowId;
							MapToDB( entity, efEntity, ref resetIsPrimaryFlag, doingGeoCoding );
							efEntity.LastUpdated = updateDate;
							if ( HasStateChanged( context ) )
							{
								//efEntity.LastUpdated = updateDate;
								count = context.SaveChanges();
							}

							//if more than one, should delete the others
							if ( exists.Count > 1 )
							{
								foreach ( var item in exists )
								{
									if ( item.Id != entity.Id )
									{
										context.Entity_Address.Remove( item );
										var count2 = context.SaveChanges();
										if ( count2 > 0 )
										{

										}
									}
								}
							}
						}
						else
						{
							efEntity.EntityId = parent.Id;
							entity.RelatedEntityId = parent.Id;
							MapToDB( entity, efEntity, ref resetIsPrimaryFlag, doingGeoCoding );
							//could just have contact points without address
							if ( entity.HasAddress() )
							{
								efEntity.Created = efEntity.LastUpdated = updateDate;
								if ( IsValidGuid( entity.RowId ) )
									efEntity.RowId = entity.RowId;
								else
									efEntity.RowId = Guid.NewGuid();

								context.Entity_Address.Add( efEntity );
								count = context.SaveChanges();

								//update profile record so doesn't get deleted
								entity.Id = efEntity.Id;
								entity.RelatedEntityId = parent.Id;
								entity.RowId = efEntity.RowId;
								if ( count == 0 )
								{
									status.AddError( string.Format( " Unable to add address. EntityParent: type: {0}, (id: {1}), City: {2}, Region: {3} <br\\> ", parent.EntityType, parent.EntityBaseId, efEntity.City, efEntity.Region ) );
								}
								else
								{
									//not used in finder!
									if ( resetIsPrimaryFlag )
									{
										//Reset_Prior_ISPrimaryFlags( efEntity.EntityId, entity.Id );
									}
								}
							}
						}

						
						//handle contact points
						//if address present, these need to be closely related
						//if no address, then need to have created an empty Entity.Address!!! or put under parent.
						if ( entity.Id > 0 )
						{
							new Entity_ContactPointManager().SaveList( entity.ContactPoint, entity.RowId, ref status );
						}
						else
						{
							// put under parent
							//should log this. If under parent should only be an org, and delete all has already been done. 
							new Entity_ContactPointManager().SaveList( entity.ContactPoint, parent.EntityUid, ref status, false );
						}
					}
					else
					{
						entity.RelatedEntityId = parent.Id;

						efEntity = context.Entity_Address.SingleOrDefault( s => s.Id == entity.Id );
						if ( efEntity != null && efEntity.Id > 0 )
						{
							entity.RowId = efEntity.RowId;
							//update
							MapToDB( entity, efEntity, ref resetIsPrimaryFlag, doingGeoCoding );
							//has changed?
							//AHHH problem. If we don't update the record it could get deleted with the method that checks last updated - so always include the updateDate
							efEntity.LastUpdated = updateDate;
							if ( HasStateChanged( context ) )
							{
								//Hmm this should come from the envelope!!
								//efEntity.LastUpdated = updateDate;

								count = context.SaveChanges();
							}
							//this not used in the finder
							//if ( resetIsPrimaryFlag )
							//{
							//	Reset_Prior_ISPrimaryFlags( entity.ParentId, entity.Id );
							//}

							//handle contact points - very wierd approach, but shouldn't have updates
							new Entity_ContactPointManager().SaveList( entity.ContactPoint, entity.RowId, ref status );
						}
						else
						{
							//if not found weird could be during testing, so add?
							//make these new methods
							efEntity = new DBResource();
							efEntity.EntityId = parent.Id;
							entity.RelatedEntityId = parent.Id;
							MapToDB( entity, efEntity, ref resetIsPrimaryFlag, doingGeoCoding );
							//could just have contact points without address
							if ( entity.HasAddress() )
							{
								efEntity.Created = efEntity.LastUpdated = updateDate;
								if ( IsValidGuid( entity.RowId ) )
									efEntity.RowId = entity.RowId;
								else
									efEntity.RowId = Guid.NewGuid();

								context.Entity_Address.Add( efEntity );
								count = context.SaveChanges();

								//update profile record so doesn't get deleted
								entity.Id = efEntity.Id;
								entity.RelatedEntityId = parent.Id;
								entity.RowId = efEntity.RowId;
								if ( count == 0 )
								{
									status.AddError( string.Format( " Unable to add address. EntityParent: type: {0}, (id: {1}), City: {2}, Region: {3} <br\\> ", parent.EntityType, parent.EntityBaseId, efEntity.City, efEntity.Region ) );
								}
								else
								{
									//not used in finder!
									if ( resetIsPrimaryFlag )
									{
										//Reset_Prior_ISPrimaryFlags( efEntity.EntityId, entity.Id );
									}
								}

							}
						}
						
					}
				}
			}
			catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
			{

				string message = HandleDBValidationError( dbex, thisClassName + ".Save() ", "Address Profile" );
				status.AddError( "Error - the save was not successful. " + message );
				LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );
				isValid = false;
			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				status.AddError( "Error - the save was not successful. " + message );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );
				isValid = false;
			}
			return isValid;
		}
        public bool DeleteAll( Entity parent, ref SaveStatus status, DateTime? lastUpdated = null )
        {
            bool isValid = true;
            //Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + ". Error - the provided target parent entity was not provided." );
                return false;
            }
			int expectedDeleteCount = 0;
			try
			{
				using ( var context = new EntityContext() )
				{

					var results = context.Entity_Address.Where( s => s.EntityId == parent.Id && ( lastUpdated == null || s.LastUpdated < lastUpdated ) )
				.ToList();
					if ( results == null || results.Count == 0 )
						return true;
					expectedDeleteCount = results.Count;

					foreach ( var item in results )
					{
						//21-03-31 mp - just removing the profile will not remove its entity and the latter's children!
						//21-04-22 mp - we have a trigger for this
						//string statusMessage = string.Empty;
						//new EntityManager().Delete( item.RowId, string.Format( "EntityAddress: {0} for EntityType: {1} ({2})", item.Id, parent.EntityTypeId, parent.EntityBaseId ), ref statusMessage );

						context.Entity_Address.Remove( item );
						var count = context.SaveChanges();
						if ( count > 0 )
						{

						}
					}
				}
			}
			catch ( System.Data.Entity.Infrastructure.DbUpdateConcurrencyException dbcex )
			{
				if ( dbcex.Message.IndexOf( "an unexpected number of rows (0)" ) > 0 )
				{
					//don't know why this happens, quashing for now.
					LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. Parent type: {0}, ParentId: {1}, expectedDeletes: {2}. Message: {3}", parent.EntityTypeId, parent.EntityBaseId, expectedDeleteCount, dbcex.Message ) );
				}
				else
				{
					var msg = BaseFactory.FormatExceptions( dbcex );
					LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. ParentType: {0}, baseId: {1}, DbUpdateConcurrencyException: {2}", parent.EntityType, parent.EntityBaseId, msg ) );
				}

			}
			catch (Exception ex)
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. ParentType: {0}, baseId: {1}, exception: {2}", parent.EntityType, parent.EntityBaseId, msg ) );
			}
            return isValid;
        }
		//
        public bool Reset_Prior_ISPrimaryFlags( int entityId, int newPrimaryProfileId )
		{
			bool isValid = true;
			string sql = string.Format( "UPDATE [dbo].[Entity.Address]   SET [IsPrimaryAddress] = 0 WHERE EntityId = {0} AND [IsPrimaryAddress] = 1  AND Id <> {1}", entityId, newPrimaryProfileId );
			using ( var context = new EntityContext() )
			{
				try
				{
					context.Database.ExecuteSqlCommand( sql );
				}

				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, "AddressManager.ResetPriorISPrimaryFlags()" );
					isValid = false;
				}
			}
			return isValid;
		}
		public bool Entity_Address_Delete( int profileId, ref string statusMessage )
		{
			bool isOK = true;
			using ( var context = new EntityContext() )
			{
				DBResource p = context.Entity_Address.FirstOrDefault( s => s.Id == profileId );
				if ( p != null && p.Id > 0 )
				{
					context.Entity_Address.Remove( p );
					int count = context.SaveChanges();
				}
				else
				{
					statusMessage = string.Format( "Requested record was not found: {0}", profileId );
					isOK = false;
				}
			}
			return isOK;

		}

		public bool ValidateProfile( ThisResource profile, Entity parent, ref SaveStatus status )
		{
			status.HasSectionErrors = false;
			//note can have an address with just contact points, no actual address!
			//no minimum checks, relying on being checked during publishing

			if ( string.IsNullOrWhiteSpace( profile.Name ) )
			{
				//24-02-23 mp - no longer setting a default name
				//		- as these are one at a time, don't know if there is a "main address" ==> this should be done in the import!
				//		Reconsider. 
				//if for org, always default to org name
				if ( parent.EntityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION )
				{
					if ( parent.EntityBaseName?.ToLower().StartsWith( "placeholder" ) == true )
					{
						profile.Name = profile.AddressLocality;
					}
					else
					{
						profile.Name = parent.EntityBaseName;
					}
				}
				else
				{
					if ( profile.Id == 0 )
					{


					}
					//	//status.AddError( "A profile name must be entered" );
					//	//isValid = false;
					//	if ( !string.IsNullOrWhiteSpace( profile.AddressLocality ) )
					//		profile.Name = profile.AddressLocality;
					//	else if ( !string.IsNullOrWhiteSpace( profile.StreetAddress ) )
					//		profile.Name = profile.StreetAddress;
					//	else
					//		profile.Name = "Main Address";
				}

			}

			if ( ( profile.Name ?? string.Empty ).Length > 200 )
			{
				profile.Name = profile.Name.Substring( 0, 200 );
				status.AddError( "The address name must be less than 200 characters" );
			}
			if ( ( profile.StreetAddress ?? string.Empty ).Length > 200 )
			{
				profile.StreetAddress = profile.StreetAddress.Substring( 0, 200 );
				status.AddError( "The address1 must be less than 200 characters" );
			}


			return status.WasSectionValid;
		}
        #endregion
        #region  retrieval ==================


        #region  entity address
        public static List<ThisResource> GetAll( Guid parentUid )
        {
            List<ContactPoint> orphans = new List<ContactPoint>();
            return GetAll( parentUid, ref orphans );
        }

        public static List<ThisResource> GetAll( Guid parentUid, ref List<ContactPoint> orphanContacts )
		{
			ThisResource resource = new ThisResource();
			List<ThisResource> list = new List<ThisResource>();
			try
			{
				using ( var context = new EntityContext() )
				{
					//note address name may not be present. Check as had considered not setting a default if name is not present.
					List<EM.Entity_Address> results = context.Entity_Address
							.Where( s => s.Entity.EntityUid == parentUid )
							.OrderByDescending( s => s.IsPrimaryAddress )
							.ThenBy( s => s.Name )
							.ThenBy( s => s.City )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( EM.Entity_Address item in results )
						{
							resource = new ThisResource();
							MapFromDB( item, resource );
                            if (resource.HasAddress() == false && resource.HasContactPoints())
                            {
                                orphanContacts.AddRange( resource.ContactPoint );
                            }
							if ( resource.HasAddress())
								list.Add( resource );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + string.Format(".GetAll. Guid parentUid: {0}", parentUid) );
			}
			return list;
		}//


		//public static ThisResource Entity_Address_Get( int profileId )
		//{
		//	ThisResource entity = new ThisResource();
		//	try
		//	{

		//		using ( var context = new EntityContext() )
		//		{
		//			DBResource item = context.Entity_Address
		//					.SingleOrDefault( s => s.Id == profileId );

		//			if ( item != null && item.Id > 0 )
		//			{
		//				MapFromDB( item, entity );
		//			}
		//		}
		//	}
		//	catch ( Exception ex )
		//	{
		//		LoggingHelper.LogError( ex, thisClassName + string.Format( ".Entity_Address_Get. profileId: {0}", profileId ) );
		//	}
		//	return entity;
		//}//
		public static void MapFromDB( EM.Entity_Address input, ThisResource output )
		{
			output.Id = input.Id;
			output.RowId = input.RowId;
			output.RelatedEntityId = input.EntityId;
			if ( input.Entity != null )
				output.ParentRowId = input.Entity.EntityUid;

			output.Name = input.Name;
			output.Description = input.Description;
			output.IsMainAddress = input.IsPrimaryAddress ?? false;
			output.StreetAddress = input.Address1;
			output.PostOfficeBoxNumber = input.PostOfficeBoxNumber ?? string.Empty;
			output.AddressLocality = input.City;
			output.PostalCode = input.PostalCode;
			output.AddressRegion = input.Region;
			output.AddressCountry = input.Country;
			//output.CountryId = ( int ) ( input.CountryId ?? 0 );
			//if ( input.Codes_Countries != null )
			//{
			//	output.Country = input.Codes_Countries.CommonName;
			//}
			output.Latitude = input.Latitude ?? 0;
			output.Longitude = input.Longitude ?? 0;
			//
			output.IdentifierJson = input.IdentifierJson;

			if ( !string.IsNullOrWhiteSpace( output.IdentifierJson ) )
			{
				output. IdentifierOLD = JsonConvert.DeserializeObject<List<Entity_IdentifierValue>>( input.IdentifierJson );
				if ( output.IdentifierOLD  != null && output.IdentifierOLD .Any())
                {

                }
			}


			//output.ContactPoint = Entity_ContactPointManager.GetAll( output.RowId );

			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime ) input.Created;
			
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime ) input.LastUpdated;
			//get address specific contacts
            //address could be empty
			output.ContactPoint = Entity_ContactPointManager.GetAll( output.RowId );

		}

		public static void MapToDB( ThisResource input, EM.Entity_Address output, ref bool resetIsPrimaryFlag, bool doingGeoCodingCheck = true )
		{
			resetIsPrimaryFlag = false;
	
			//NOTE: the parentId - currently orgId, is handled in the update code
			output.Id = input.Id;
			output.Name = input.Name;
			output.Description = input.Description;
			//if this address is primary, and not previously primary, set indicator output reset existing settings
			//will need setting output default first address output primary if not entered
			if ( input.IsMainAddress && ( bool ) ( !( output.IsPrimaryAddress ?? false ) ) )
			{
				//initially attempt output only allow adding new primary,not unchecking
				resetIsPrimaryFlag = true;
			}
			output.IsPrimaryAddress = input.IsMainAddress;

			//bool hasChanged = false;
			//bool hasAddress = false;

			//if ( input.HasAddress() )
			//{
			//	hasAddress = true;
			//	if ( output.Latitude == null || output.Latitude == 0
			//	  || output.Longitude == null || output.Longitude == 0 )
			//		hasChanged = true;
			//}
			//if ( hasChanged == false )
			//{
			//	if ( output.Id == 0 )
			//		hasChanged = true;
			//	else
			//		hasChanged = HasAddressChanged( input, output );
			//}

			output.Address1 = input.StreetAddress;
			output.PostOfficeBoxNumber = GetData( input.PostOfficeBoxNumber, null );
			output.City = input.AddressLocality;
			output.PostalCode = input.PostalCode;
			output.Region = input.AddressRegion ?? string.Empty;
			output.SubRegion = input.SubRegion ?? string.Empty;

			output.Country = input.AddressCountry ?? string.Empty;
			if ( output.Country.ToLower() == "us" || output.Country.ToLower() == "usa"  || output.Country.ToLower() == "u.s.a." || output.Country.ToLower() == "u.s." )
			{
				output.Country = "United States";
			}
			//likely provided
			output.Latitude = input.Latitude;
			output.Longitude = input.Longitude;
			//just store the json
			//22-07
			output.IdentifierJson = input.IdentifierJson;

			if ( input.HasAddress() )
			{
				//check if lat/lng were not provided with address
				//may want output always do this output expand region!
				if ( output.Latitude == null || output.Latitude == 0
				  || output.Longitude == null || output.Longitude == 0
				  || ( output.Region ?? string.Empty ).Length == 2
				  )
				{
					//20-12-05 mp - as this could be 20+ seconds input import, consider deferring output end of cycle
					if ( UtilityManager.GetAppKeyValue( "environment" ) != "development" )
					{
						if ( doingGeoCodingCheck )
							UpdateGeo( input, output );
						else
                        {
							//add parent for reindex
                        }
					}
				}
			}

			//these will likely not be present? 
			//If new, or address has changed, do the geo lookup
			//if ( hasAddress )
			//{
			//	if ( hasChanged )
			//	{
			//		UpdateGeo( input, output );
			//	}
			//}
			//else
			//{
			//	output.Latitude = 0;
			//	output.Longitude = 0;
			//}
		}

		/// <summary>
		/// Check all addresses missing lat/lng and geocode
		/// NEW: need to reindex the parent resource
		/// </summary>
		/// <param name="messages"></param>
		/// <param name="maxRecords"></param>
		/// <returns></returns>
		public List<ThisResource> ResolveMissingGeodata( ref string messages, int maxRecords = 300 )
		{
			int addressesFixed = 0;
			int addressRemaining = 0;
			return ResolveMissingGeodata( ref messages, ref addressesFixed, ref addressRemaining, maxRecords );
		}
		/// <summary>
		/// Check all addresses missing lat/lng and geocode
		/// NEW: need to reindex the parent resource
		/// </summary>
		/// <param name="message"></param>
		/// <param name="addressesFixed"></param>
		/// <param name="addressRemaining"></param>
		/// <param name="maxRecords"></param>
		/// <returns></returns>
		public List<ThisResource> ResolveMissingGeodata( ref string message, ref int addressesFixed, ref int addressRemaining, int maxRecords = 300 )
        {
            ThisResource entity = new ThisResource();
            List<ThisResource> list = new List<ThisResource>();
            List<string> messageList = new List<string>();
            bool resetIsPrimaryFlag = false;
            string prevAddr = string.Empty;
            string prevAddr2 = string.Empty;
            string prevCity = string.Empty;
            string prevRegion = string.Empty;
            string prevPostalCode = string.Empty;
            double prevLat = 0.0;
            double prevLng = 0.0;
            int cntr = 0;
            try
            {
                using ( var context = new EntityContext() )
                {
                    List<EM.Entity_Address> results = context.Entity_Address
                            .Where( s => ( s.Latitude == null || s.Latitude == 0.0 )
                            || ( s.Longitude == null || s.Longitude == 0.0 ) )
                            .OrderBy( s => s.Address1 ).ThenBy( s => s.City ).ThenBy( s => s.PostalCode ).ThenBy( s => s.Region )
                            .ToList();
					if ( results != null && results.Count > 0 )
					{
						foreach ( EM.Entity_Address efEntity in results )
						{
							cntr++;
							entity = new ThisResource();
							if ( ( efEntity.City ?? string.Empty ).ToLower() == "city"
								|| ( efEntity.Address1 ?? string.Empty ).ToLower() == "address"
								|| ( efEntity.Address1 ?? string.Empty ).ToLower().IndexOf( "123 main" ) > -1
								|| ( efEntity.Address1 ?? string.Empty ).ToLower().IndexOf( "some street" ) > -1
								|| ( efEntity.Region ?? string.Empty ).ToLower().IndexOf( "state" ) == 0
								)
								continue;

							if ( ( efEntity.City ?? string.Empty ).ToLower() == string.Empty
								&& ( efEntity.Address1 ?? string.Empty ).ToLower() == string.Empty
								)
								continue;
							//quick approach, map to address, which will call the geo code. If there was an update, update the entity
							//check if the same address to avoid many hits against google endpoint
							bool updatedViaCopy = false;
							if ( efEntity.Address1 == prevAddr
								//&& ( efEntity.Address2 ?? string.Empty ) == prevAddr2
								&& ( efEntity.City ?? string.Empty ) == prevCity
								&& ( efEntity.Region ?? string.Empty ) == prevRegion
								&& ( efEntity.PostalCode ?? string.Empty ) == prevPostalCode
								)
							{
								efEntity.Latitude = prevLat;
								efEntity.Longitude = prevLng;
								updatedViaCopy = true;
							}
							else
							{
								//save prev region now, in case it gets expanded, although successive ones will not be expanded!
								prevRegion = efEntity.Region ?? string.Empty;
								//TODO - may want to get the related Entity.Id and type to allow a reindex request
								MapFromDB( efEntity, entity );
								//the geocode method will be called from this method
								MapToDB( entity, efEntity, ref resetIsPrimaryFlag, true );
							}
							//
							if ( HasStateChanged( context ) )
							{
								efEntity.LastUpdated = System.DateTime.Now;
								//efEntity.LastUpdatedById = userId;

								int count = context.SaveChanges();
								messageList.Add( string.Format( "___Updated address: {0}, viaCopy: {1}", DisplayAddress( efEntity ), updatedViaCopy ) );
								LoggingHelper.DoTrace( 6, string.Format( "___Updated address: {0}, viaCopy: {1}", DisplayAddress( efEntity ), updatedViaCopy ) );

								addressesFixed++;
								prevLat = ( double )( efEntity.Latitude ?? 0.0 );
								prevLng = ( double )( efEntity.Longitude ?? 0.0 );

								//add parent to pending reindex
								if (efEntity.Entity != null && efEntity.Entity.Id > 0)
                                {
									var messages = new List<string>();
									new SearchPendingReindexManager().Add( efEntity.Entity.EntityTypeId, (int)efEntity.Entity.EntityBaseId, 1, ref messages );
								} else
                                {
									//add a note if Entity not found
                                }
							}
							else
							{
								//addresses that failed
								list.Add( entity );
							}
							prevAddr = efEntity.Address1 ?? string.Empty;
							//prevAddr2 = efEntity.Address2 ?? string.Empty;
							prevCity = efEntity.City ?? string.Empty;
							//prevRegion = efEntity.Region ?? string.Empty;
							prevPostalCode = efEntity.PostalCode ?? string.Empty;
							if ( maxRecords > 0 && cntr > maxRecords )
							{
								message = string.Format( "Early completion. Processed {0} of {1} candidate records.", cntr, results.Count );
								addressRemaining = results.Count() - cntr;

								break;
							}
							//not sure if we should sleep here. It works fine from website click. Adding a sleep of even one second, means doing 200 addresses would take 3 min. 
							//could try 1/2 a second
							//or sleep after every 'n' records
							if ( cntr % 10 == 0 )
								System.Threading.Thread.Sleep( 400 );
						}

					}
					else
					{
						message = thisClassName + ".ResolveMissingGeodata - No records were found to normalize. ";
						LoggingHelper.DoTrace( 5, message );
						return list;
					}
				}
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".ResolveMissingGeodata" );
            }

            message += string.Join( "<br/>", messageList.ToArray() );

            return list;
        }//

		/// <summary>
		/// Update Geo coding
		/// NOTE the output address has already been assigned the basic data.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="output"></param>
        public static void UpdateGeo( CM.Address input, EM.Entity_Address output )
        {
			LoggingHelper.DoTrace( 7, thisClassName + ".UpdateGeo - entered" );

			//GoogleGeocoding.Results results = GeoServices.GeocodeAddress( from.DisplayAddress() );
			bool doingExpandOfRegion = UtilityManager.GetAppKeyValue( "doingExpandOfRegion", false );
			
			try
			{
				//*** check for existing address with lat/lng - to handle expand of region. If found, skip geocode step
				using ( var context = new EntityContext() )
				{
					//have to handle where region was expanded
					var  exists = context.Entity_Address
							.Where( s =>  
									s.Address1 == input.StreetAddress
								&& s.City == input.AddressLocality		//need to handle for different regions/countries
								&& (s.PostalCode == input.PostalCode || ( s.PostalCode ?? string.Empty ).IndexOf( input.PostalCode ?? string.Empty ) == 0 )  //could have been normalized to full 9
																																	//&& s.Region == from.AddressRegion	//could have been expanded
								&& ( s.Latitude != null && s.Latitude != 0.0 && s.Longitude != null && s.Longitude != 0.0 )
							)
							.OrderBy( s => s.Address1 ).ThenBy( s => s.City ).ThenBy( s => s.PostalCode ).ThenBy( s => s.Region )
							.ToList();

					if ( exists != null && exists.Count > 0 )
					{
						foreach ( var item in exists )
						{
							bool existsIsUSA = false;
							//risky?
							if ( item.Country != null ) 
							{
								if ( "usa us u.s.a. united states of america".IndexOf( item.Country.ToLower() ) == 0 )
									existsIsUSA = true;
							}

							//may remove this check if use in search is OK.
							if ( item.PostalCode == input.PostalCode || (item.PostalCode ?? string.Empty).IndexOf(input.PostalCode) == 0 )
							{
								bool isOK = false;
								//no input country
								if ( string.IsNullOrWhiteSpace( input.AddressCountry ) )
								{
									//assume if street, city, and postal match to USA, then OK
									if ( existsIsUSA )
										isOK = true;
									else
									{
										//how likely if street, city, and postal match, that the country is wrong? Google would have to have figured it out, so just go for it?
										isOK = true;
									}
								}
								else
								{
									//risk?
									if ( "usa u.s.a. united states united states of america".IndexOf( ( input.AddressCountry ?? string.Empty ).ToLower() ) > -1
										&& existsIsUSA )
									{
										isOK = true;
									}
									else
									{
										//what
									}
								}
								if ( isOK )
								{
									output.Latitude = item.Latitude;
									output.Longitude = item.Longitude;
									output.Region = item.Region;
									output.PostalCode = item.PostalCode;
									output.Country = item.Country;
									return;
								}
							}
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".UpdateGeo" );
			}

			//otherwise
			//Try with a looser address if 0/0 lat/lng
			var hasLatLng = false;
            var results = new GoogleGeocoding.Results();
            var addressesToTry = new List<string>()
            {
                input.DisplayAddress(),
                input.LooseDisplayAddress(),
                input.PostalCode ?? string.Empty,
                input.AddressRegion ?? string.Empty
                //,from.Country ?? string.Empty
            };
            foreach ( var test in addressesToTry )
            {
				if ( string.IsNullOrWhiteSpace( test ) || test.Trim().Length < 5 )
					continue;
				//LoggingHelper.DoTrace( 7, thisClassName + ".UpdateGeo - testing: " + test );
				results = TryGetAddress( test, ref hasLatLng );
                if ( hasLatLng )
                {
                    break;
                }
				//Don't spam the Geocoding API
				//20-08-17 mp - changed to 1sec from 3
				//21-03-02 mp - changed back to 3 sec. Had troubles in sandbox of not working
				System.Threading.Thread.Sleep( 3000 ); 
            }

            //Continue
            if ( results != null )
            {
				LoggingHelper.DoTrace( 7, thisClassName + ".UpdateGeo - have results." );

				GoogleGeocoding.Location location = results.GetLocation();
                if ( location != null )
                {
                    output.Latitude = location.lat;
                    output.Longitude = location.lng;
                }
                try
                {
                    if ( results.results.Count > 0 )
                    {
                        //this is inconsistant [0] -postal code, [5]-country, [4] region
                        //                  int pIdx = 0;// results.results[ 0 ].address_components.Count - 1;
                        //int cIdx = results.results[ 0 ].address_components.Count - 1;
                        //                  int regionIdx = results.results[ 0 ].address_components.Count - 2;
                        string postalCode = string.Empty;// results.results[ 0 ].address_components[ 0 ].short_name;
                        string country = string.Empty;// results.results[ 0 ].address_components[ cIdx ].long_name;
                        string fullRegion = string.Empty; // results.results[ 0 ].address_components[ regionIdx ].long_name;
                        //can we expand the region here? - determine the index number of the region
                        string suffix = string.Empty;
                        //want to at least implement in the import
                        foreach ( var part in results.results[ 0 ].address_components )
                        {
                            if ( part.types.Count > 0 )
                            {
                                if ( part.types[ 0 ] == "country" )
                                    country = part.long_name;
                                else if ( part.types[ 0 ] == "administrative_area_level_1" )
                                    fullRegion = part.long_name;
                                else if ( part.types[ 0 ] == "postal_code" )
                                    postalCode = part.long_name;
                                else if ( part.types[ 0 ] == "postal_code_suffix" )
                                {
                                    suffix = part.long_name;
                                    postalCode += "-" + suffix;
                                }
                            }
                            //
                        }

                        if ( string.IsNullOrEmpty( output.PostalCode ) ||
                            output.PostalCode != postalCode )
                        {
                            //?not sure if should assume the google result is accurate
                            output.PostalCode = postalCode;
                        }
                        if ( !string.IsNullOrEmpty( country ) && 
                            output.CountryId == null )
                        {
                            //set country string, and perhaps plan update process.
                            output.Country = country;
                            //do lookup, OR at least notify for now
                            //probably should make configurable - or spin off process to attempt update
                            //EmailManager.NotifyAdmin( "CTI Missing country to update", string.Format( "Address without country entered, but resolved via GoogleGeocoding.Location. entity.ParentId: {0}, country: {1}", from.ParentId, country ) );
                        }
                        //expand region
                        if ( doingExpandOfRegion
                            && ( output.Region ?? string.Empty ).Length < fullRegion.Length )
                        {
                            output.Region = fullRegion;
                        }
                    }

                }
                catch ( Exception ex )
                {
                    LoggingHelper.LogError( ex, thisClassName + "UpdateGeo" );
                }
            } else
			{
				LoggingHelper.DoTrace( 7, thisClassName + ".UpdateGeo - ***unable to resolve lat/lng. for : " + input.DisplayAddress() );

			}
		}

        public string DisplayAddress( DBResource dbaddress, string separator = ", " )
        {
            string address = string.Empty;
            if ( !string.IsNullOrWhiteSpace( dbaddress.Address1 ) )
                address = dbaddress.Address1;
            //if ( !string.IsNullOrWhiteSpace( dbaddress.Address2 ) )
            //    address += separator + dbaddress.Address2;
            if ( !string.IsNullOrWhiteSpace( dbaddress.City ) )
                address += separator + dbaddress.City;
            if ( !string.IsNullOrWhiteSpace( dbaddress.Region ) )
                address += separator + dbaddress.Region;
            if ( !string.IsNullOrWhiteSpace( dbaddress.PostalCode ) )
                address += " " + dbaddress.PostalCode;
            if ( !string.IsNullOrWhiteSpace( dbaddress.Country ) )
                address += separator + dbaddress.Country;

			if ( dbaddress.Latitude != null && dbaddress.Latitude != 0)
				address += separator + string.Format( "Lat: {0}, lng: {1}", dbaddress.Latitude, dbaddress.Longitude );
            return address;
        }
        private static GoogleGeocoding.Results TryGetAddress( string address, ref bool hasLatLng )
        {
			LoggingHelper.DoTrace( 7, thisClassName + ".TryGetAddress : " + address );

			try
			{
                var results = GeoServices.GeocodeAddress( address );
                hasLatLng = results != null && results.GetLocation().lat != 0 && results.GetLocation().lng != 0;
                return results;
            }
            catch (Exception ex)
            {
				LoggingHelper.DoTrace( 6, "Google TryGetAddress failed: " + ex.Message + " for address: " + address );
                hasLatLng = false;
                return null;
            }
        }
        public static bool HasAddressChanged( ThisResource from, EM.Entity_Address to )
		{
			bool hasChanged = false;

			if ( to.Address1 != from.StreetAddress
			|| to.City != from.AddressLocality
			|| to.PostalCode != from.PostalCode
			|| to.Region != from.AddressRegion
			|| to.Country != from.AddressCountry )
				hasChanged = true;

			return hasChanged;
		}
		#endregion

		public static List<string> Autocomplete( string keyword, int typeId, int maxTerms = 25 )
		{
			int pTotalRows = 0;
			List<string> results = new List<string>();
			string address1 = string.Empty;
			string city = string.Empty;
			string postalCode = string.Empty;
			if ( typeId == 3 )
				postalCode = keyword;
			else if ( typeId == 2 )
				city = keyword;
			else
				address1 = keyword;
			string result = string.Empty;

			List<ThisResource> list = QuickSearch( address1, city, postalCode, 1, maxTerms, ref pTotalRows );

			string prevName = string.Empty;
			string suffix = string.Empty;
			foreach ( ThisResource item in list )
			{
				result = string.Empty;
				suffix = string.Empty;

				if ( typeId == 3 )
				{
					result = item.PostalCode;
					suffix = " [[" + item.AddressLocality + "]] ";
				}
				else if ( typeId == 2 )
				{
					result = item.AddressLocality;
				}
				else
				{
					result = item.StreetAddress;
					suffix = " [[" + item.AddressLocality + "]] ";
				}

				if ( result.ToLower() != prevName )
					results.Add( result + suffix );

				prevName = result.ToLower();
			}

			return results;
		}
		public static List<ThisResource> QuickSearch( string keyword, int pageNumber, int pageSize, ref int pTotalRows )
		{
			List<ThisResource> list = new List<ThisResource>();
			ThisResource entity = new ThisResource();
			keyword = CleanTerm( keyword );

			int skip = ( pageNumber - 1 ) * pageSize;
			using ( var context = new EntityContext() )
			{
				//turn off - so Entity et will not be included!!!!
				context.Configuration.LazyLoadingEnabled = false;


				var addresses = context.Entity_Address
					.Where( s => keyword == string.Empty
								|| s.Entity.EntityBaseName.Contains( keyword )
								|| s.Address1.Contains( keyword )
								|| s.City.Contains( keyword )
								|| s.PostalCode.Contains( keyword )
								)
					.GroupBy( a => new
					{
						Name = a.Name,
						StreetAddress = a.Address1,
						City = a.City,
						PostalCode = a.PostalCode,
						AddressRegion = a.Region,
						Country = a.Country ?? string.Empty
					} )
					.Select( g => new ThisResource
					{
						Name = g.Key.Name,
						StreetAddress = g.Key.StreetAddress,
						//Address2 = g.Key.Address2 ?? string.Empty,
						AddressLocality = g.Key.City,
						PostalCode = g.Key.PostalCode,
						AddressRegion = g.Key.AddressRegion,
						AddressCountry = g.Key.Country
					} )
					.OrderByDescending( a => a.StreetAddress )
					.ThenByDescending( a => a.AddressLocality );
				//.ToList();


				pTotalRows = addresses.Count();
				List<ThisResource> results = addresses
					.OrderBy( s => s.StreetAddress )
					.Skip( skip )
					.Take( pageSize )
					.ToList();
				if ( results != null && results.Count > 0 )
				{
					//??enough
					list = results;
					//return list;
				}
				
			}

			return list;
		}
		public static List<ThisResource> QuickSearch( string address1, string city, string postalCode, int pageNumber, int pageSize, ref int pTotalRows )
		{
			List<ThisResource> list = new List<ThisResource>();
			ThisResource entity = new ThisResource();
			address1 = CleanTerm( address1 );
			city = CleanTerm( city );
			postalCode = CleanTerm( postalCode );

			int skip = ( pageNumber - 1 ) * pageSize;
			using ( var context = new EntityContext() )
			{

				List<DBResource> results = context.Entity_Address
					.Where( s =>
						   ( address1 == string.Empty || s.Address1.Contains( address1 ) )
						&& ( city == string.Empty || s.City.Contains( city ) )
						&& ( postalCode == string.Empty || s.PostalCode.Contains( postalCode ) )
						)
					.OrderBy( s => s.Address1 )
					.Skip( skip )
					.Take( pageSize )
					.ToList();

				if ( results != null && results.Count > 0 )
				{
					foreach ( DBResource item in results )
					{
						entity = new ThisResource();
						MapFromDB( item, entity );

						list.Add( entity );
					}

					//Other parts
				}
			}

			return list;
		}
		private static string CleanTerm( string item )
		{
			string term = item == null ? string.Empty : item.Trim();
			if ( term.IndexOf( "[[" ) > 1 )
			{
				term = term.Substring( 0, term.IndexOf( "[[" ) );
				term = term.Trim();
			}
			return term;
		}
		#endregion
	}
}
