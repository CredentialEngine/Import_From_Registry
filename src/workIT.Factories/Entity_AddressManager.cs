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
            DeleteAll( parent, ref status );

            if ( list == null || list.Count == 0 )
				return true;

			bool isAllValid = true;
			foreach ( ThisEntity item in list )
			{
				Save( item, parentUid, ref status );
			}

			return isAllValid;
		}
		public bool Save( ThisEntity entity, Guid parentUid, ref SaveStatus status )
		{
			bool isValid = true;

			if ( !IsValidGuid( parentUid ) )
			{
				status.AddError( "Error: a valid parent identifier was not provided." );
				return false;
			}

			int count = 0;
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.EntityBaseId == 0 )
			{
				status.AddError( "Error - the parent entity was not found." );
				return false;
			}

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

					if ( entity.Id == 0 )
					{
						//add
						efEntity = new DBEntity();
						efEntity.EntityId = parent.Id;
						entity.ParentId = parent.Id;
						MapToDB( entity, efEntity, ref resetIsPrimaryFlag );

						//could just have contact points without address
						if (entity.HasAddress() )
						{
							efEntity.Created = efEntity.LastUpdated = DateTime.Now;
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
								status.AddError( string.Format( " Unable to add address. parentUid: {0}, City: {1}, Region: {2} <br\\> ", parentUid, efEntity.City, efEntity.Region ) );
							}
							else
							{
								if ( resetIsPrimaryFlag )
								{
									Reset_Prior_ISPrimaryFlags( efEntity.EntityId, entity.Id );
								}
							}
						}
                        //handle contact points
                        //if address present, these need to be closely related
                        if ( entity.Id > 0 )
                            new Entity_ContactPointManager().SaveList( entity.ContactPoint, entity.RowId, ref status );
                        else
                        {
                            // put under parent
                            //should log this. If under parent should onlybe an org, and delete all has already been done. 
                            new Entity_ContactPointManager().SaveList( entity.ContactPoint, parentUid, ref status, false );
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
							MapToDB( entity, efEntity, ref resetIsPrimaryFlag );
							//has changed?
							if ( HasStateChanged( context ) )
							{
								efEntity.LastUpdated = System.DateTime.Now;

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
        public bool DeleteAll( Entity parent, ref SaveStatus status )
        {
            bool isValid = true;
            //Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + ". Error - the provided target parent entity was not provided." );
                return false;
            }
            using ( var context = new EntityContext() )
            {
                context.Entity_Address.RemoveRange( context.Entity_Address.Where( s => s.EntityId == parent.Id ) );
                int count = context.SaveChanges();
                if ( count > 0 )
                {
                    isValid = true;
                }
                else
                {
                    //if doing a delete on spec, may not have been any properties
                }
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
			if ( ( profile.Address2 ?? "" ).Length > 200 )
				status.AddError( "The address2 must be less than 200 characters" );

			return !status.HasSectionErrors;
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
			to.Address2 = from.Address2 ?? "";
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

		public static void MapToDB( ThisEntity from, EM.Entity_Address to, ref bool resetIsPrimaryFlag )
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
			to.Address2 = GetData( from.Address2, null );
			to.PostOfficeBoxNumber = GetData( from.PostOfficeBoxNumber, null );
			to.City = from.City;
			to.PostalCode = from.PostalCode;
			to.Region = from.AddressRegion ?? "";
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
					UpdateGeo( from, to );
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
        public List<ThisEntity> ResolveMissingGeodata( ref string messages, int maxRecords = 100 )
        {
            ThisEntity entity = new ThisEntity();
            List<ThisEntity> list = new List<ThisEntity>();
            List<string> messageList = new List<string>();
            bool resetIsPrimaryFlag = false;
            string prevAddr = "";
            string prevAddr2 = "";
            string prevCity = "";
            string prevRegion = "";
            string prevFullRegion = "";
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
                            .OrderBy( s => s.Address1 ).ThenBy( s => s.Address2 ).ThenBy( s => s.City ).ThenBy( s => s.PostalCode ).ThenBy( s => s.Region )
                            .ToList();

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
                            if ( efEntity.Address1 == prevAddr
                                && ( efEntity.Address2 ?? "" ) == prevAddr2
                                && ( efEntity.City ?? "" ) == prevCity
                                && ( efEntity.Region ?? "" ) == prevRegion
                                && ( efEntity.PostalCode ?? "" ) == prevPostalCode
                                )
                            {
                                efEntity.Latitude = prevLat;
                                efEntity.Longitude = prevLng;
                            }
                            else
                            {
                                //save prev region now, in case it gets expanded, although successive ones will not be expanded!
                                prevRegion = efEntity.Region ?? "";
                                MapFromDB( efEntity, entity );
                                MapToDB( entity, efEntity, ref resetIsPrimaryFlag );
                            }
                            if ( HasStateChanged( context ) )
                            {
                                efEntity.LastUpdated = System.DateTime.Now;
                                //efEntity.LastUpdatedById = userId;

                                int count = context.SaveChanges();
                                messageList.Add( string.Format( "___Updated address: {0}", DisplayAddress( efEntity ) ) );
                                prevLat = ( double ) ( efEntity.Latitude ?? 0.0 );
                                prevLng = ( double ) ( efEntity.Longitude ?? 0.0 );
                            }
                            else
                            {
                                //addresses that failed
                                list.Add( entity );
                            }
                            prevAddr = efEntity.Address1 ?? "";
                            prevAddr2 = efEntity.Address2 ?? "";
                            prevCity = efEntity.City ?? "";
                            //prevRegion = efEntity.Region ?? "";
                            prevPostalCode = efEntity.PostalCode ?? "";
                            if ( maxRecords > 0 && cntr > maxRecords )
                            {
                                messages = string.Format( "Early completion. Processed {0} of {1} candidate records.", cntr, results.Count );
                                break;
                            }

                        }
                    }
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
            //GoogleGeocoding.Results results = GeoServices.GeocodeAddress( from.DisplayAddress() );
            bool doingExpandOfRegion = UtilityManager.GetAppKeyValue( "doingExpandOfRegion", false );
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

                results = TryGetAddress( test, ref hasLatLng );
                if ( hasLatLng )
                {
                    break;
                }
                System.Threading.Thread.Sleep( 3000 ); //Don't spam the Geocoding API
            }

            //Continue
            if ( results != null )
            {
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
            }
        }

        public string DisplayAddress( DBEntity dbaddress, string separator = ", " )
        {
            string address = "";
            if ( !string.IsNullOrWhiteSpace( dbaddress.Address1 ) )
                address = dbaddress.Address1;
            if ( !string.IsNullOrWhiteSpace( dbaddress.Address2 ) )
                address += separator + dbaddress.Address2;
            if ( !string.IsNullOrWhiteSpace( dbaddress.City ) )
                address += separator + dbaddress.City;
            if ( !string.IsNullOrWhiteSpace( dbaddress.Region ) )
                address += separator + dbaddress.Region;
            if ( !string.IsNullOrWhiteSpace( dbaddress.PostalCode ) )
                address += " " + dbaddress.PostalCode;
            if ( !string.IsNullOrWhiteSpace( dbaddress.Country ) )
                address += separator + dbaddress.Country;

            address += separator + string.Format( "Lat: {0}, lng: {1}", dbaddress.Latitude, dbaddress.Longitude );
            return address;
        }
        private static GoogleGeocoding.Results TryGetAddress( string address, ref bool hasLatLng )
        {
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
			|| to.Address2 != from.Address2
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
						Address2 = a.Address2 ?? "",
						City = a.City,
						PostalCode = a.PostalCode,
						AddressRegion = a.Region,
						Country = a.Country ?? ""
					} )
					.Select( g => new ThisEntity
					{
						Name = g.Key.Name,
						Address1 = g.Key.Address1,
						Address2 = g.Key.Address2 ?? "",
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
