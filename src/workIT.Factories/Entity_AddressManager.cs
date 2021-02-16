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
using ThisEntity = workIT.Models.Common.Address;
using DBEntity = workIT.Data.Tables.Entity_Address;
using EntityContext = workIT.Data.Tables.workITEntities;

using workIT.Utilities;
using workIT.Models.Search.ThirdPartyApiModels;

namespace workIT.Factories
{
	public class Entity_AddressManager : BaseFactory
	{
		static string thisClassName = "Entity_AddressManager";

		#region Persistance - Entity_Address
		public bool SaveList( List<ThisEntity> list, Guid parentUid, ref SaveStatus status )
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

			//now be still do a delete all until implementing a balance line
			//could set date and delete all before this date!
			DateTime updateDate = DateTime.Now;
			if ( IsValidDate( status.EnvelopeUpdatedDate ) )
			{
				updateDate = status.LocalUpdatedDate;
			}

			var currentAddresses = GetAll( parent.EntityUid );
			bool isAllValid = true;
			if ( list == null || list.Count == 0 )
			{
				if ( currentAddresses != null && currentAddresses.Any() )
				{
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
				foreach ( ThisEntity item in list )
				{
					Save( item, parent, updateDate, ref status );
				}
				//delete any addresses with last updated less than updateDate
				DeleteAll( parent, ref status, updateDate );
				//extra check where input count is less than output count
				var latestAddresses = GetAll( parent.EntityUid );
				if (latestAddresses.Count() > list.Count() )
				{

				}


			}
			return isAllValid;
		}
		private bool Save( ThisEntity entity, Entity parent, DateTime updateDate, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;

			var doingGeoCoding = UtilityManager.GetAppKeyValue( "doingGeoCodingImmediately", false );
			try
			{
				using ( var context = new EntityContext() )
				{
					DBEntity efEntity = new DBEntity();
					if ( ValidateProfile( entity, parent, ref status ) == false )
					{
						return false;
					}
					bool resetIsPrimaryFlag = false;
					//typically all will have an entity.Id of zero
					//could do a lookup
					if ( entity.Id == 0 )
					{
						efEntity = new DBEntity();
						//check for current match - only do if not deleting
						var exists = context.Entity_Address
							.Where( s => s.EntityId == parent.Id
								&& s.Address1 == entity.Address1
								&& s.City == entity.City
								&& ( s.PostalCode == entity.PostalCode || ( s.PostalCode ?? "" ).IndexOf( entity.PostalCode ?? "" ) == 0 )  //could have been normalized to full 
									//&& s.Region == from.AddressRegion	//could have been expanded
							)
							.OrderBy( s => s.Address1 ).ThenBy( s => s.City ).ThenBy( s => s.PostalCode ).ThenBy( s => s.Region )
							.ToList();
						if ( exists != null && exists.Count > 0 )
						{
							//should only be one
							efEntity = exists[ 0 ];
							entity.Id = efEntity.Id;
							entity.ParentId = parent.Id;
							entity.RowId = efEntity.RowId;
							MapToDB( entity, efEntity, ref resetIsPrimaryFlag, doingGeoCoding );
							if ( HasStateChanged( context ) )
							{
								efEntity.LastUpdated = updateDate;
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
							entity.ParentId = parent.Id;
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
								entity.ParentId = parent.Id;
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
						entity.ParentId = parent.Id;

						efEntity = context.Entity_Address.SingleOrDefault( s => s.Id == entity.Id );
						if ( efEntity != null && efEntity.Id > 0 )
						{
							entity.RowId = efEntity.RowId;
							//update
							MapToDB( entity, efEntity, ref resetIsPrimaryFlag, doingGeoCoding );
							//has changed?
							if ( HasStateChanged( context ) )
							{
								//Hmm this should come from the envelope!!
								efEntity.LastUpdated = updateDate;

								count = context.SaveChanges();
							}
							if ( resetIsPrimaryFlag )
							{
								Reset_Prior_ISPrimaryFlags( entity.ParentId, entity.Id );
							}

							//handle contact points - very wierd approach, but shouldn't have updates
							new Entity_ContactPointManager().SaveList( entity.ContactPoint, entity.RowId, ref status );
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
			try
			{
				using ( var context = new EntityContext() )
				{
					//????
					//context.Database.ExecuteSqlCommand( "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;" );
					//context.Entity_Address.RemoveRange( context.Entity_Address.Where( s => s.EntityId == parent.Id ) );
					//            int count = context.SaveChanges();
					//            if ( count > 0 )
					//            {
					//                isValid = true;
					//            }
					//
					var results = context.Entity_Address.Where( s => s.EntityId == parent.Id && ( lastUpdated == null || s.LastUpdated < lastUpdated) )
				.ToList();
					if ( results == null || results.Count == 0 )
						return true;

					foreach ( var item in results )
					{
						context.Entity_Address.Remove( item );
						var count = context.SaveChanges();
						if ( count > 0 )
						{

						}
					}
				}
			} catch (Exception ex)
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. ParentType: {0}, baseId: {1}, exception: {2}", parent.EntityType, parent.EntityBaseId, msg ) );
			}
            return isValid;
        }
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
				DBEntity p = context.Entity_Address.FirstOrDefault( s => s.Id == profileId );
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

		public bool ValidateProfile( ThisEntity profile, Entity parent, ref SaveStatus status )
		{
			status.HasSectionErrors = false;
			//note can have an address with just contact points, no actual address!
			//check minimum
			//if ( string.IsNullOrWhiteSpace( profile.Address1 )
			//&& string.IsNullOrWhiteSpace( profile.PostOfficeBoxNumber )
			//	)
			//{
			//	status.AddWarning( "Please enter at least Street Address 1 or a Post Office Box Number" );
			//}
			//if ( string.IsNullOrWhiteSpace( profile.City ) )
			//{
			//	status.AddWarning( "Please enter a valid Locality/City" );
			//}
			//if ( string.IsNullOrWhiteSpace( profile.AddressRegion ) )
			//{
			//	status.AddWarning( "Please enter a valid Region/State/Province" );
			//}
			//if ( string.IsNullOrWhiteSpace( profile.PostalCode )
			//   )
			//{
			//	status.AddWarning( "Please enter a valid Postal Code" );
			//}
			if ( string.IsNullOrWhiteSpace( profile.Name ) )
			{
				//if for org, always default to org name
				if ( parent.EntityTypeId == CodesManager.ENTITY_TYPE_ORGANIZATION )
					profile.Name = parent.EntityBaseName;
				else
				{
					if ( profile.Id == 0 )
					{


					}
					//status.AddError( "A profile name must be entered" );
					//isValid = false;
					if ( !string.IsNullOrWhiteSpace( profile.City ) )
						profile.Name = profile.City;
					else if ( !string.IsNullOrWhiteSpace( profile.Address1 ) )
						profile.Name = profile.Address1;
					else
						profile.Name = "Main Address";
				}

			}

			if ( ( profile.Name ?? "").Length > 200 ) 
				status.AddError( "The address name must be less than 200 characters" );
			if ( ( profile.Address1 ?? "" ).Length > 200 )
				status.AddError( "The address1 must be less than 200 characters" );


			return status.WasSectionValid;
		}
        #endregion
        #region  retrieval ==================


        #region  entity address
        public static List<ThisEntity> GetAll( Guid parentUid )
        {
            List<ContactPoint> orphans = new List<ContactPoint>();
            return GetAll( parentUid, ref orphans );
        }

        public static List<ThisEntity> GetAll( Guid parentUid, ref List<ContactPoint> orphanContacts )
		{
			ThisEntity entity = new ThisEntity();
			List<ThisEntity> list = new List<ThisEntity>();
			try
			{
				using ( var context = new EntityContext() )
				{
					List<EM.Entity_Address> results = context.Entity_Address
							.Where( s => s.Entity.EntityUid == parentUid )
							.OrderByDescending( s => s.IsPrimaryAddress )
							.ThenBy( s => s.Id )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( EM.Entity_Address item in results )
						{
							entity = new ThisEntity();
							MapFromDB( item, entity );
                            if (entity.HasAddress() == false && entity.HasContactPoints())
                            {
                                orphanContacts.AddRange( entity.ContactPoint );
                            }
							list.Add( entity );
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


		//public static ThisEntity Entity_Address_Get( int profileId )
		//{
		//	ThisEntity entity = new ThisEntity();
		//	try
		//	{

		//		using ( var context = new EntityContext() )
		//		{
		//			DBEntity item = context.Entity_Address
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
		public static void MapFromDB( EM.Entity_Address from, ThisEntity to )
		{
			to.Id = from.Id;
			to.RowId = from.RowId;
			to.ParentId = from.EntityId;
			if ( from.Entity != null )
				to.ParentRowId = from.Entity.EntityUid;

			to.Name = from.Name;
			to.IsMainAddress = from.IsPrimaryAddress ?? false;
			to.Address1 = from.Address1;
			to.PostOfficeBoxNumber = from.PostOfficeBoxNumber ?? "";
			to.City = from.City;
			to.PostalCode = from.PostalCode;
			to.AddressRegion = from.Region;
			to.Country = from.Country;
			//to.CountryId = ( int ) ( from.CountryId ?? 0 );
			//if ( from.Codes_Countries != null )
			//{
			//	to.Country = from.Codes_Countries.CommonName;
			//}
			to.Latitude = from.Latitude ?? 0;
			to.Longitude = from.Longitude ?? 0;

			//to.ContactPoint = Entity_ContactPointManager.GetAll( to.RowId );

			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;
			
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime ) from.LastUpdated;
			//get address specific contacts
            //address could be empty
			to.ContactPoint = Entity_ContactPointManager.GetAll( to.RowId );

		}

		public static void MapToDB( ThisEntity from, EM.Entity_Address to, ref bool resetIsPrimaryFlag, bool doingGeoCodingCheck = true )
		{
			resetIsPrimaryFlag = false;
	
			//NOTE: the parentId - currently orgId, is handled in the update code
			to.Id = from.Id;
			to.Name = from.Name;
			//if this address is primary, and not previously primary, set indicator to reset existing settings
			//will need setting to default first address to primary if not entered
			if ( from.IsMainAddress && ( bool ) ( !( to.IsPrimaryAddress ?? false ) ) )
			{
				//initially attempt to only allow adding new primary,not unchecking
				resetIsPrimaryFlag = true;
			}
			to.IsPrimaryAddress = from.IsMainAddress;

			//bool hasChanged = false;
			//bool hasAddress = false;

			//if ( from.HasAddress() )
			//{
			//	hasAddress = true;
			//	if ( to.Latitude == null || to.Latitude == 0
			//	  || to.Longitude == null || to.Longitude == 0 )
			//		hasChanged = true;
			//}
			//if ( hasChanged == false )
			//{
			//	if ( to.Id == 0 )
			//		hasChanged = true;
			//	else
			//		hasChanged = HasAddressChanged( from, to );
			//}

			to.Address1 = from.Address1;
			to.PostOfficeBoxNumber = GetData( from.PostOfficeBoxNumber, null );
			to.City = from.City;
			to.PostalCode = from.PostalCode;
			to.Region = from.AddressRegion ?? "";
			to.SubRegion = from.SubRegion ?? "";

			to.Country = from.Country;
			//likely provided
			to.Latitude = from.Latitude;
			to.Longitude = from.Longitude;

			if ( from.HasAddress() )
			{
				//check if lat/lng were not provided with address
				//may want to always do this to expand region!
				if ( to.Latitude == null || to.Latitude == 0
				  || to.Longitude == null || to.Longitude == 0
				  || ( to.Region ?? "" ).Length == 2
				  )
				{
					//20-12-05 mp - as this could be 20+ seconds from import, consider deferring to end of cycle
					if ( UtilityManager.GetAppKeyValue( "envType" ) != "development" )
					{
						if ( doingGeoCodingCheck )
							UpdateGeo( from, to );
					}
				}
			}

			//these will likely not be present? 
			//If new, or address has changed, do the geo lookup
			//if ( hasAddress )
			//{
			//	if ( hasChanged )
			//	{
			//		UpdateGeo( from, to );
			//	}
			//}
			//else
			//{
			//	to.Latitude = 0;
			//	to.Longitude = 0;
			//}

		}
        public List<ThisEntity> ResolveMissingGeodata( ref string messages, int maxRecords = 300 )
        {
            ThisEntity entity = new ThisEntity();
            List<ThisEntity> list = new List<ThisEntity>();
            List<string> messageList = new List<string>();
            bool resetIsPrimaryFlag = false;
            string prevAddr = "";
            string prevAddr2 = "";
            string prevCity = "";
            string prevRegion = "";
            string prevPostalCode = "";
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
					//.ThenBy( s => s.Address2 )
					if ( results != null && results.Count > 0 )
                    {
                        foreach ( EM.Entity_Address efEntity in results )
                        {
                            cntr++;
                            entity = new ThisEntity();
                            if ( ( efEntity.City ?? "" ).ToLower() == "city"
                                || ( efEntity.Address1 ?? "" ).ToLower() == "address"
                                || ( efEntity.Address1 ?? "" ).ToLower().IndexOf( "123 main" ) > -1
                                || ( efEntity.Address1 ?? "" ).ToLower().IndexOf( "some street" ) > -1
                                || ( efEntity.Region ?? "" ).ToLower().IndexOf( "state" ) == 0
                                )
                                continue;

							//quick approach, map to address, which will call the geo code. If there was an update, update the entity
							//check if the same address to avoid many hits against google endpoint
							bool updatedViaCopy = false;
                            if ( efEntity.Address1 == prevAddr
                                //&& ( efEntity.Address2 ?? "" ) == prevAddr2
                                && ( efEntity.City ?? "" ) == prevCity
                                && ( efEntity.Region ?? "" ) == prevRegion
                                && ( efEntity.PostalCode ?? "" ) == prevPostalCode
                                )
                            {
                                efEntity.Latitude = prevLat;
                                efEntity.Longitude = prevLng;
								updatedViaCopy = true;
							}
                            else
                            {
                                //save prev region now, in case it gets expanded, although successive ones will not be expanded!
                                prevRegion = efEntity.Region ?? "";
                                MapFromDB( efEntity, entity );
                                MapToDB( entity, efEntity, ref resetIsPrimaryFlag, true );
                            }
                            if ( HasStateChanged( context ) )
                            {
                                efEntity.LastUpdated = System.DateTime.Now;
                                //efEntity.LastUpdatedById = userId;

                                int count = context.SaveChanges();
                                messageList.Add( string.Format( "___Updated address: {0}, viaCopy: {1}", DisplayAddress( efEntity ), updatedViaCopy ) );
                                prevLat = ( double ) ( efEntity.Latitude ?? 0.0 );
                                prevLng = ( double ) ( efEntity.Longitude ?? 0.0 );
                            }
                            else
                            {
                                //addresses that failed
                                list.Add( entity );
                            }
                            prevAddr = efEntity.Address1 ?? "";
                            //prevAddr2 = efEntity.Address2 ?? "";
                            prevCity = efEntity.City ?? "";
                            //prevRegion = efEntity.Region ?? "";
                            prevPostalCode = efEntity.PostalCode ?? "";
                            if ( maxRecords > 0 && cntr > maxRecords )
                            {
                                messages = string.Format( "Early completion. Processed {0} of {1} candidate records.", cntr, results.Count );
                                break;
                            }

                        }
                    } else
						LoggingHelper.DoTrace( 5, thisClassName + ".ResolveMissingGeodata - No records were found to normalize. " );
				}
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".ResolveMissingGeodata" );
            }

            messages += string.Join( "<br/>", messageList.ToArray() );

            return list;
        }//

        public static void UpdateGeo( CM.Address from, EM.Entity_Address to )
        {
			LoggingHelper.DoTrace( 7, thisClassName + ".UpdateGeo - entered" );

			//GoogleGeocoding.Results results = GeoServices.GeocodeAddress( from.DisplayAddress() );
			bool doingExpandOfRegion = UtilityManager.GetAppKeyValue( "doingExpandOfRegion", false );
			//check for existing address with lat/lng
			try
			{
				using ( var context = new EntityContext() )
				{
					//have to handle where region was expanded
					var  exists = context.Entity_Address
							.Where( s =>  
									s.Address1 == from.Address1
								&& s.City == from.City		//need to handle for different regions/countries
								&& (s.PostalCode == from.PostalCode || ( s.PostalCode ?? "" ).IndexOf( from.PostalCode ?? "" ) == 0 )  //could have been normalized to full 9
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
							if ( item.Country.IndexOf( "United" ) == 0 )
								existsIsUSA = true;

							//may remove this check if use in search is OK.
							if ( item.PostalCode == from.PostalCode || (item.PostalCode ?? "").IndexOf(from.PostalCode) == 0 )
							{
								bool isOK = false;
								//no input country
								if ( string.IsNullOrWhiteSpace( from.Country ) )
								{
									//assume if street, city, and postal match to USA, then OK
									if ( existsIsUSA )
										isOK = true;
									else
									{
										//how likely if street, city, and postal match, that the country is wrong? Google would have to have figured it out, so just go for it?
										isOK = true;
									}
								} else 
								if (  "usa u.s.a. united states united states of america".IndexOf((from.Country ??"").ToLower()) > -1
									&& existsIsUSA ) 
								{
									isOK = true;
								} else
								{
									//what
								}
								if ( isOK )
								{
									to.Latitude = item.Latitude;
									to.Longitude = item.Longitude;
									to.Region = item.Region;
									to.PostalCode = item.PostalCode;
									to.Country = item.Country;
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
                from.DisplayAddress(),
                from.LooseDisplayAddress(),
                from.PostalCode ?? "",
                from.AddressRegion ?? ""
                //,from.Country ?? ""
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
				System.Threading.Thread.Sleep( 1000 ); 
            }

            //Continue
            if ( results != null )
            {
				LoggingHelper.DoTrace( 7, thisClassName + ".UpdateGeo - have results." );

				GoogleGeocoding.Location location = results.GetLocation();
                if ( location != null )
                {
                    to.Latitude = location.lat;
                    to.Longitude = location.lng;
                }
                try
                {
                    if ( results.results.Count > 0 )
                    {
                        //this is inconsistant [0] -postal code, [5]-country, [4] region
                        //                  int pIdx = 0;// results.results[ 0 ].address_components.Count - 1;
                        //int cIdx = results.results[ 0 ].address_components.Count - 1;
                        //                  int regionIdx = results.results[ 0 ].address_components.Count - 2;
                        string postalCode = "";// results.results[ 0 ].address_components[ 0 ].short_name;
                        string country = "";// results.results[ 0 ].address_components[ cIdx ].long_name;
                        string fullRegion = ""; // results.results[ 0 ].address_components[ regionIdx ].long_name;
                        //can we expand the region here? - determine the index number of the region
                        string suffix = "";
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

                        if ( string.IsNullOrEmpty( to.PostalCode ) ||
                            to.PostalCode != postalCode )
                        {
                            //?not sure if should assume the google result is accurate
                            to.PostalCode = postalCode;
                        }
                        if ( !string.IsNullOrEmpty( country ) && 
                            to.CountryId == null )
                        {
                            //set country string, and perhaps plan update process.
                            to.Country = country;
                            //do lookup, OR at least notify for now
                            //probably should make configurable - or spin off process to attempt update
                            //EmailManager.NotifyAdmin( "CTI Missing country to update", string.Format( "Address without country entered, but resolved via GoogleGeocoding.Location. entity.ParentId: {0}, country: {1}", from.ParentId, country ) );
                        }
                        //expand region
                        if ( doingExpandOfRegion
                            && ( to.Region ?? "" ).Length < fullRegion.Length )
                        {
                            to.Region = fullRegion;
                        }
                    }

                }
                catch ( Exception ex )
                {
                    LoggingHelper.LogError( ex, thisClassName + "UpdateGeo" );
                }
            } else
			{
				LoggingHelper.DoTrace( 7, thisClassName + ".UpdateGeo - ***unable to resolve lat/lng. for : " + from.DisplayAddress() );

			}
		}

        public string DisplayAddress( DBEntity dbaddress, string separator = ", " )
        {
            string address = "";
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
            catch
            {
                hasLatLng = false;
                return null;
            }
        }
        public static bool HasAddressChanged( ThisEntity from, EM.Entity_Address to )
		{
			bool hasChanged = false;

			if ( to.Address1 != from.Address1
			|| to.City != from.City
			|| to.PostalCode != from.PostalCode
			|| to.Region != from.AddressRegion
			|| to.Country != from.Country )
				hasChanged = true;

			return hasChanged;
		}
		#endregion

		public static List<string> Autocomplete( string keyword, int typeId, int maxTerms = 25 )
		{
			int pTotalRows = 0;
			List<string> results = new List<string>();
			string address1 = "";
			string city = "";
			string postalCode = "";
			if ( typeId == 3 )
				postalCode = keyword;
			else if ( typeId == 2 )
				city = keyword;
			else
				address1 = keyword;
			string result = "";

			List<ThisEntity> list = QuickSearch( address1, city, postalCode, 1, maxTerms, ref pTotalRows );

			string prevName = "";
			string suffix = "";
			foreach ( ThisEntity item in list )
			{
				result = "";
				suffix = "";

				if ( typeId == 3 )
				{
					result = item.PostalCode;
					suffix = " [[" + item.City + "]] ";
				}
				else if ( typeId == 2 )
				{
					result = item.City;
				}
				else
				{
					result = item.Address1;
					suffix = " [[" + item.City + "]] ";
				}

				if ( result.ToLower() != prevName )
					results.Add( result + suffix );

				prevName = result.ToLower();
			}

			return results;
		}
		public static List<ThisEntity> QuickSearch( string keyword, int pageNumber, int pageSize, ref int pTotalRows )
		{
			List<ThisEntity> list = new List<ThisEntity>();
			ThisEntity entity = new ThisEntity();
			keyword = CleanTerm( keyword );

			int skip = ( pageNumber - 1 ) * pageSize;
			using ( var context = new EntityContext() )
			{
				//turn off - so Entity et will not be included!!!!
				context.Configuration.LazyLoadingEnabled = false;


				var addresses = context.Entity_Address
					.Where( s => keyword == ""
								|| s.Entity.EntityBaseName.Contains( keyword )
								|| s.Address1.Contains( keyword )
								|| s.City.Contains( keyword )
								|| s.PostalCode.Contains( keyword )
								)
					.GroupBy( a => new
					{
						Name = a.Name,
						Address1 = a.Address1,
						City = a.City,
						PostalCode = a.PostalCode,
						AddressRegion = a.Region,
						Country = a.Country ?? ""
					} )
					.Select( g => new ThisEntity
					{
						Name = g.Key.Name,
						Address1 = g.Key.Address1,
						//Address2 = g.Key.Address2 ?? "",
						City = g.Key.City,
						PostalCode = g.Key.PostalCode,
						AddressRegion = g.Key.AddressRegion,
						Country = g.Key.Country
					} )
					.OrderByDescending( a => a.Address1 )
					.ThenByDescending( a => a.City );
				//.ToList();


				pTotalRows = addresses.Count();
				List<ThisEntity> results = addresses
					.OrderBy( s => s.Address1 )
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
		public static List<ThisEntity> QuickSearch( string address1, string city, string postalCode, int pageNumber, int pageSize, ref int pTotalRows )
		{
			List<ThisEntity> list = new List<ThisEntity>();
			ThisEntity entity = new ThisEntity();
			address1 = CleanTerm( address1 );
			city = CleanTerm( city );
			postalCode = CleanTerm( postalCode );

			int skip = ( pageNumber - 1 ) * pageSize;
			using ( var context = new EntityContext() )
			{

				List<DBEntity> results = context.Entity_Address
					.Where( s =>
						   ( address1 == "" || s.Address1.Contains( address1 ) )
						&& ( city == "" || s.City.Contains( city ) )
						&& ( postalCode == "" || s.PostalCode.Contains( postalCode ) )
						)
					.OrderBy( s => s.Address1 )
					.Skip( skip )
					.Take( pageSize )
					.ToList();

				if ( results != null && results.Count > 0 )
				{
					foreach ( DBEntity item in results )
					{
						entity = new ThisEntity();
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
			string term = item == null ? "" : item.Trim();
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
