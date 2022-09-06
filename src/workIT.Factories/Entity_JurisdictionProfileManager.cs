using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using ThisEntity = workIT.Models.Common.JurisdictionProfile;
using DBEntity = workIT.Data.Tables.Entity_JurisdictionProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using workIT.Utilities;
using MC = workIT.Models.Common;
using EM = workIT.Data.Tables;

namespace workIT.Factories
{
	public class Entity_JurisdictionProfileManager : BaseFactory
	{
		static string thisClassName = "Entity_JurisdictionProfileManager";
		//static string DEFAULT_GUID = "00000000-0000-0000-0000-000000000000";
		public static int JURISDICTION_PURPOSE_SCOPE = 1;
		public static int JURISDICTION_PURPOSE_RESIDENT = 2;
		public static int JURISDICTION_PURPOSE_OFFERREDIN = 3;

		#region JurisdictionProfile  =======================
		#region JurisdictionProfile Core  =======================
		public bool SaveList( List<ThisEntity> list, Guid parentUid, int jProfilePurposeId, ref SaveStatus status )
		{
			//a delete all is done before entering here, so can leave if input is empty
            if ( list == null || list.Count == 0 )
                return true;

            bool isAllValid = true;
			foreach ( ThisEntity item in list )
			{
				item.JProfilePurposeId = jProfilePurposeId;
				item.ParentEntityUid = parentUid;
				Add( item, "", ref status );
			}

			return isAllValid;
		}

		public bool SaveAssertedInList( Guid parentUid, int assertedInTypeId, List<ThisEntity> list,  ref SaveStatus status )
		{

			if ( list == null || list.Count == 0 )
				return true;

			bool isAllValid = true;
			foreach ( ThisEntity item in list )
			{
				item.JProfilePurposeId = JURISDICTION_PURPOSE_OFFERREDIN;
				item.ParentEntityUid = parentUid;
				item.AssertedInTypeId = assertedInTypeId;
				Add( item, "", ref status );
			}

			return isAllValid;
		}
		/// <summary>
		/// Add a jurisdiction profile
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="property">Can be blank. Set to a property where additional validation is necessary</param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Add( ThisEntity entity, string property, ref SaveStatus status, int assertedInTypeId = 0 )
		{
			bool isValid = true;

			using ( var context = new EntityContext() )
			{
				if ( entity == null || !IsValidGuid( entity.ParentEntityUid ) )
				{
					status.AddWarning( "Error - missing an identifier for the JurisdictionProfile" );
					return false;
				}

				//ensure we have a parentId/EntityId
				Entity parent = EntityManager.GetEntity( entity.ParentEntityUid );
				if ( parent == null || parent.Id == 0 )
				{
					status.AddWarning( "Error - the parent entity was not found." );
					return false;
				}

				//check for Empty
				//==> not sure what is the minimum required fields!
				bool isEmpty = false;

				if ( ValidateProfile( entity, property, ref isEmpty, ref status ) == false )
				{
					return false;
				}
				if ( isEmpty )
				{
					//status.AddWarning( "Error - Jurisdiction profile is empty. " );
					return false;
				}
				

				//id = JurisdictionProfile_Add( entity, ref status );
				DBEntity efEntity = new DBEntity();
				MapToDB( entity, efEntity );
				efEntity.EntityId = parent.Id;
                if ( IsValidGuid( entity.RowId ) )
                    efEntity.RowId = entity.RowId;
                else
                    efEntity.RowId = Guid.NewGuid();

                entity.RowId = efEntity.RowId;

				if ( efEntity.JProfilePurposeId == null || efEntity.JProfilePurposeId == 0 )
					efEntity.JProfilePurposeId = 1;

				efEntity.Created = efEntity.LastUpdated = DateTime.Now;

				context.Entity_JurisdictionProfile.Add( efEntity );

				int count = context.SaveChanges();
				if ( count > 0 )
				{
					entity.Id = efEntity.Id;
					//update parts
					UpdateParts( entity, true, ref status );
					//??????? why
					//UpdateJPRegions( entity,  ref status );
				}
	

			}

			return isValid;
		}
		
		public bool UpdateParts( ThisEntity entity, bool isAdd, ref SaveStatus status )
		{
			bool isValid = true;

			EntityPropertyManager mgr = new EntityPropertyManager();

			if ( mgr.AddProperties( entity.JurisdictionAssertion, entity.RowId, CodesManager.ENTITY_TYPE_JURISDICTION_PROFILE, CodesManager.PROPERTY_CATEGORY_JurisdictionAssertionType, false,  ref status ) == false )
				isValid = false;

			int id = GeoCoordinates_Add( entity.MainJurisdiction, entity.Id, ref status );
			if ( entity.JurisdictionException != null)
				foreach (var gc in entity.JurisdictionException)
				{
					GeoCoordinates_Add( gc, entity.Id, ref status );
				}
			return isValid;
		}
		
		/// <summary>
		/// May want to use an isLast check to better handle an empty object
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool IsEmpty( ThisEntity entity, bool isLastItem = false )
		{
			bool isEmpty = false;
			//this will be problematic as the two bools default to false
			//radio buttons?
			if ( string.IsNullOrWhiteSpace( entity.Description )
				&& ( entity.MainJurisdiction == null || entity.MainJurisdiction.GeoNamesId == 0 )
				&& ( entity.JurisdictionException == null || entity.JurisdictionException.Count == 0 )
				)
				return true;

			return isEmpty;
		}
		public bool ValidateProfile( ThisEntity profile, string property, ref bool isEmpty, ref SaveStatus status )
		{
			status.HasSectionErrors = false;
			isEmpty = false;
			//this will be problematic as the two bools default to false
			if ( string.IsNullOrWhiteSpace( profile.Description )
				&& ( profile.MainJurisdiction == null || profile.MainJurisdiction.GeoNamesId == 0 )
				&& ( profile.JurisdictionException == null || profile.JurisdictionException.Count == 0 )
				&& ( !IsValidGuid( profile.AssertedBy ) )
				&& ( profile.JurisdictionAssertion == null || profile.JurisdictionAssertion.Items.Count == 0 )
				)
			{
				//isEmpty = true;
				//status.AddWarning( "No data has been entered, save was cancelled." );
				//return false;
			}
			if ( property == "JurisdictionAssertions"  && profile.Id > 0)
			{
				if (!IsValidGuid(profile.AssertedBy))
					status.AddWarning( "Please select the Agent that makes these assertions." );
				if ( profile.JurisdictionAssertion == null || profile.JurisdictionAssertion.Items.Count == 0 )
					status.AddWarning( "Please select at least one assertion." );
			}

			if ( profile.MainJurisdiction == null || profile.MainJurisdiction.GeoNamesId == 0 )
			{
				//doesn't make sense here
				//List<MC.GeoCoordinates> regions = GetAll( profile.Id, false );
				//if ( regions != null && regions.Count > 0 )
				//{
				//	profile.MainJurisdiction = regions[ 0 ];
				//}
			}
			//need to have a main jurisdiction, or is global
			if ( profile.MainJurisdiction != null
				&& (profile.MainJurisdiction.GeoURI ?? "").Length > 0
				&& profile.MainJurisdiction.Name != "Earth" )
			{
				//should not have global
				if ((profile.IsGlobalJurisdiction ?? false) == true)
				{
					status.AddWarning( "Is Global cannot be set to 'Is Global' when an main region has been selected." );
				}
			} else
			{
				//no regions, must specify global
				//may want to make configurable
				//if ( ( profile.IsGlobalJurisdiction ?? false ) == false )
				//{
				//	if ( profile.Description != "Auto-saved Jurisdiction" )
				//	{
				//		if ( UtilityManager.GetAppKeyValue( "requireRegionOrIsGlobal", false ) == true )
				//		{
				//			status.AddWarning( "Please select a main region, OR set 'Is Global' to 'This jurisdiction is global'." );
				//		}
				//	}
				//}
			}
				
			return status.WasSectionValid;
		}

		/// <summary>
		/// Delete a JurisdictionProfile
		/// </summary>
		/// <param name="Id"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int Id, ref string statusMessage )
		{
			bool isValid = false;

			using ( var context = new EntityContext() )
			{
				if ( Id == 0 )
				{
					statusMessage = "Error - missing an identifier for the JurisdictionProfile";
					return false;
				}

				DBEntity efEntity =
					context.Entity_JurisdictionProfile.SingleOrDefault( s => s.Id == Id );
				if ( efEntity != null && efEntity.Id > 0 )
				{
					context.Entity_JurisdictionProfile.Remove( efEntity );
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						isValid = true;
					}
				}
				else
				{
					statusMessage = string.Format( "JurisdictionProfile record was not found: {0}", Id );
					isValid = false;
				}
			}

			return isValid;
		}
        /// <summary>
        /// Delete all properties for parent (in preparation for import)
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public bool DeleteAll( Entity parent, ref SaveStatus status )
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
					var results = context.Entity_JurisdictionProfile.Where( s => s.EntityId == parent.Id )
					.ToList();
					if ( results == null || results.Count == 0 )
						return true;

					context.Entity_JurisdictionProfile.RemoveRange( context.Entity_JurisdictionProfile.Where( s => s.EntityId == parent.Id ) );
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
			}
			catch ( Exception ex )
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. ParentType: {0}, baseId: {1}, exception: {2}", parent.EntityType, parent.EntityBaseId, msg ) );
			}


            return isValid;
        }
        #endregion
        #region JurisdictionProfile retrieve  =======================

        /// <summary>
        /// get all related JurisdictionProfiles for the parent
        /// </summary>
        /// <param name="parentUId"></param>
        /// <returns></returns>
        public static List<ThisEntity> Jurisdiction_GetAll( Guid parentUid, int jprofilePurposeId = 1 )
		{
			//efEntity.JProfilePurposeId
			ThisEntity entity = new ThisEntity();
			List<ThisEntity> list = new List<ThisEntity>();
			if ( parentUid == null )
				return list;
			int count = 0;

			MC.Entity  parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}

			using ( var context = new EntityContext() )
			{
				List<DBEntity> Items = context.Entity_JurisdictionProfile
							.Where( s => s.EntityId == parent.Id 
								&& s.JProfilePurposeId == jprofilePurposeId )
							.OrderBy( s => s.JProfilePurposeId )
							.ThenBy(s => s.AssertedInTypeId)
							.ThenBy(s => s.Id).ToList();

				if ( Items.Count > 0 )
				{
					foreach ( DBEntity item in Items )
					{
						entity = new ThisEntity();
						count++;
						//map and get regions
						MapFromDB( item, entity, count );
						list.Add( entity );
					}
				}
			}

			return list;
		}

		/// <summary>
		/// Get a single Jurisdiction Profile by integer Id
		/// </summary>
		/// <param name="rowId"></param>
		/// <returns></returns>
		public static ThisEntity Get( int id )
		{
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.Entity_JurisdictionProfile
							.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, 1 );
				}

			}

			return entity;
		}
		/// <summary>
		/// Get a single Jurisdiction Profile by Guid
		/// </summary>
		/// <param name="rowId"></param>
		/// <returns></returns>
		public static ThisEntity Get( Guid rowId )
		{
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.Entity_JurisdictionProfile
							.SingleOrDefault( s => s.RowId == rowId );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, 1 );
				}

			}

			return entity;
		}

		/// <summary>
		/// Mapping from interface model to entity model
		/// Assuming that for updates, the entity model is always populated from DB, so here we can make assumptions regarding what can be updated.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		private static void MapToDB( ThisEntity from, DBEntity to )
		{
			to.Id = from.Id;
			//to.EntityId = from.ParentId;
			//don't allow a change if an update
			if ( from.Id == 0 )
				to.JProfilePurposeId = from.JProfilePurposeId > 0 ? from.JProfilePurposeId : 1;
			else
			{
				//handle unexpected
				if ( to.JProfilePurposeId == null )
					to.JProfilePurposeId = 1;
			}

			//from.MainJurisdiction is likely null
			if ( from.MainJurisdiction != null && from.MainJurisdiction.GeoNamesId == 0 )
			{
				//List<MC.GeoCoordinates> regions = GetAll( to.Id, false );
				//if ( regions != null && regions.Count > 0 )
				//{
				//	from.MainJurisdiction = regions[ 0 ];
				//}
			}

			if ( from.MainJurisdiction != null && !string.IsNullOrWhiteSpace( from.MainJurisdiction.Name ) )
				to.Name = from.MainJurisdiction.Name;
			else
				to.Name = "Default jurisdiction";
			to.Description = from.Description;


			//TODO - if a main jurisdiction exists, then global should be false
			//may not be available
			if ( from.MainJurisdiction != null
				&& ( from.MainJurisdiction.GeoURI ?? "" ).Length > 0
				&& from.MainJurisdiction.Name != "Earth" )
				to.IsGlobalJurisdiction = false;

			else if ( from.IsGlobalJurisdiction != null )
				to.IsGlobalJurisdiction = from.IsGlobalJurisdiction;
			else
				to.IsGlobalJurisdiction = null;
			if ( !IsGuidValid( from.AssertedBy ) && from.AssertedByList != null && from.AssertedByList.Count > 0 )
			{
				from.AssertedBy = from.AssertedByList[ 0 ];
			}

				if ( IsGuidValid( from.AssertedBy ) )
			{
				if ( to.Id > 0 && to.AssertedByAgentUid != from.AssertedBy )
				{
					if ( IsGuidValid( to.AssertedByAgentUid ) )
					{
						//need to remove the previous roles on change of asserted by
						//string statusMessage = "";
						//new Entity_AgentRelationshipManager().Delete( to.RowId, to.AssertedByAgentUid, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER, ref statusMessage );
					}
				}
				to.AssertedByAgentUid = from.AssertedBy;
			}
			else
			{
				to.AssertedByAgentUid = null;
			}

			if ( from.AssertedInTypeId > 0 )
				to.AssertedInTypeId = ( int ) from.AssertedInTypeId;
			else
				to.AssertedInTypeId = null;

		}
		private static void MapFromDB( DBEntity from, ThisEntity to, int count )
		{
			to.Id = from.Id;
			to.RowId = from.RowId;
			to.ParentId = (int)from.EntityId;

			//these will probably no lonber be necessary
			to.ParentTypeId = from.Entity.EntityTypeId;
			to.ParentEntityUid = from.Entity.EntityUid;

			to.JProfilePurposeId = from.JProfilePurposeId != null ? ( int ) from.JProfilePurposeId : 1;

			if ( IsGuidValid( from.AssertedByAgentUid ) )
			{
				to.AssertedBy = ( Guid ) from.AssertedByAgentUid;

				to.AssertedByOrganization = OrganizationManager.GetBasics( to.AssertedBy );

			}

			if ( (from.Description ?? "") == "Auto-saved Jurisdiction" )
				to.Description = "";
			else
				to.Description = from.Description;

			if ( from.Created != null )
				to.Created = ( DateTime ) from.Created;

			if ( from.LastUpdated != null )
				to.LastUpdated = ( DateTime ) from.LastUpdated;



			List<MC.GeoCoordinates> regions = GetAll( to.Id, false );
			if ( regions != null && regions.Count > 0 )
			{
				to.MainJurisdiction = regions[ 0 ];
			}
			to.JurisdictionException = GetAll( to.Id, true );


			if ( to.MainJurisdiction != null && to.MainJurisdiction.GeoNamesId > 0 && to.MainJurisdiction.Name != "Earth")
				to.IsGlobalJurisdiction = false;
			else
				to.IsGlobalJurisdiction = from.IsGlobalJurisdiction;

			if ( !string.IsNullOrWhiteSpace( from.Description ) )
			{
				to.ProfileSummary = from.Description;
			}
			else
			{
				if ( to.MainJurisdiction != null && to.MainJurisdiction.GeoNamesId > 0 )
				{
					to.ProfileSummary = to.MainJurisdiction.ProfileSummary;
				}
				else
				{
					if ( (bool)(to.IsGlobalJurisdiction ?? false) )
						to.ProfileSummary = "Global";
					else
						to.ProfileSummary = "JurisdictionProfile Summary - " + count.ToString();
				}
			}
			if ((from.AssertedInTypeId ?? 0) > 0 && from.Codes_AssertionType != null)
			{
				to.AssertedInTypeId = (int)from.AssertedInTypeId;
				to.AssertedInType = from.Codes_AssertionType.Title;
				to.AssertedInType = ( to.AssertedInType ?? "" ).Replace( " By", " In" );
				Enumeration ja = new Enumeration() { Name = "Jurisdiction Assertions"};
				EnumeratedItem ei = new EnumeratedItem() { Id = to.AssertedInTypeId, Name = to.AssertedInType };
				to.JurisdictionAssertion.Items.Add(ei);

			}
			//***TODO ** handle differently?
			//use of properties, requires creating an Entity for Jurisdiction???
			if ( to.JProfilePurposeId == 3)
			{

			}
			//to.JurisdictionAssertion = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_JurisdictionAssertionType );
		} //


		
		#endregion
		#endregion

		#region GeoCoordinate  =======================
		#region GeoCoordinate Core  =======================
	

		public int GeoCoordinates_Add( MC.GeoCoordinates entity, int jpId, ref SaveStatus status )
		{
			if ( entity == null || string.IsNullOrWhiteSpace(entity.GeoURI))
			{
				return 0;
			}
			entity.ParentId = jpId;
			EM.GeoCoordinate efEntity = new EM.GeoCoordinate();
			MC.GeoCoordinates existing = new MC.GeoCoordinates();
			List<String> messages = new List<string>();

			//extract from "http://geonames.org/6255149/"
			string geoNamesId = UtilityManager.ExtractNameValue( entity.GeoURI, ".org", "/", "/" );
			if ( IsInteger( geoNamesId ) )
				entity.GeoNamesId = Int32.Parse( geoNamesId );

			if ( GeoCoordinates_Exists( entity.ParentId, entity.GeoNamesId, entity.IsException ) )
			{
				//status.AddWarning( "Error this Region has aleady been selected.");
				return 0;
			}

			else
			{
				MapToDB( entity, efEntity );
				//ensure parent jp Id is present
				return GeoCoordinate_Add( efEntity, ref status );
			}


		}
		/// <summary>
		/// Probably want to combine with region to have access to keys
		/// </summary>
		/// <param name="efEntity"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public int GeoCoordinate_Add( EM.GeoCoordinate efEntity, ref SaveStatus status )
		{

			using ( var context = new EntityContext() )
			{
				try
				{
					if ( efEntity.JurisdictionId  < 1 )
					{
						status.AddWarning( "Error - missing a parent identifier");
						return 0;
					}

					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;

					context.GeoCoordinate.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						return efEntity.Id;
					}
					else
					{
						//?no info on error
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".GeoCoordinate_Add() ", "GeoCoordinate" );
					status.AddError( "Error - the save was not successful. " + message );
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".RelatedRegion_Add(), Name: {0}, ParentId: {1)", efEntity.Name, efEntity.JurisdictionId ) );
				}
			}

			return 0;
		}


		//public bool GeoCoordinate_Update( MC.GeoCoordinates entity, ref SaveStatus status )
		//{
		//	bool isValid = true;

		//	using ( var context = new EntityContext() )
		//	{
		//		if ( entity == null || entity.Id == 0 || entity.GeoNamesId == 0 )
		//		{
		//			status.AddWarning( "Error - missing an identifier for the GeoCoordinate" );
		//			return false;
		//		}

		//		EM.GeoCoordinate efEntity =
		//			context.GeoCoordinate.SingleOrDefault( s => s.Id == entity.Id );
		//		if ( efEntity.JurisdictionId < 1 )
		//		{
		//			status.AddWarning( "Error - missing a parent identifier" );
		//			return false;
		//		}

		//		MapToDB( entity, efEntity );

		//		if ( HasStateChanged( context ) )
		//		{
		//			efEntity.LastUpdated = DateTime.Now;
		//			int count = context.SaveChanges();
		//			if ( count > 0 )
		//			{
		//				isValid = true;
		//			}
		//		}
		//		else
		//		{
		//			//GeoCoordinate_Update skipped, as no change to the contents
		//		}
		//	}

		//	return isValid;
		//}

		/// <summary>
		/// Delete a region
		/// </summary>
		/// <param name="Id"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		//public bool GeoCoordinate_Delete( int Id, ref string statusMessage )
		//{
		//	bool isValid = false;

		//	using ( var context = new EntityContext() )
		//	{
		//		if ( Id == 0 )
		//		{
		//			statusMessage = "Error - missing an identifier for the GeoCoordinate";
		//			return false;
		//		}

		//		EM.GeoCoordinate efEntity =
		//			context.GeoCoordinate.SingleOrDefault( s => s.Id == Id );
		//		if ( efEntity != null && efEntity.Id > 0 )
		//		{
		//			context.GeoCoordinate.Remove( efEntity );
		//			int count = context.SaveChanges();
		//			if ( count > 0 )
		//			{
		//				isValid = true;
		//			}
		//		}
		//		else
		//		{
		//			statusMessage = string.Format( "GeoCoordinate record was not found: {0}", Id );
		//			isValid = false;
		//		}
		//	}

		//	return isValid;
		//}

		#endregion
		#region GeoCoordinate retrieve  =======================

		/// <summary>
		/// get GeoCoordinates
		/// </summary>
		/// <param name="Id"></param>
		/// <returns></returns>
		public static MC.GeoCoordinates GeoCoordinates_Get( int Id )
		{
			MC.GeoCoordinates entity = new MC.GeoCoordinates();
			List<MC.GeoCoordinates> list = new List<MC.GeoCoordinates>();
			using ( var context = new EntityContext() )
			{
				EM.GeoCoordinate item = context.GeoCoordinate
							.SingleOrDefault( s => s.Id == Id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity );
				}
			}

			return entity;
		}

		/// <summary>
		/// Check if record (any) exist for the provided GeoNamesId
		/// </summary>
		/// <param name="geoNamesId"></param>
		/// <returns></returns>
		public static MC.GeoCoordinates GeoCoordinates_GetByGeoNamesId( int geoNamesId )
		{
			MC.GeoCoordinates entity = new MC.GeoCoordinates();
			List<MC.GeoCoordinates> list = new List<MC.GeoCoordinates>();
			using ( var context = new EntityContext() )
			{
				EM.GeoCoordinate item = context.GeoCoordinate
							.FirstOrDefault( s => s.GeoNamesId == geoNamesId );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity );
				}
			}

			return entity;
		}
		public static MC.GeoCoordinates GeoCoordinates_GetByUrl( string geoUri)
		{
			MC.GeoCoordinates entity = new MC.GeoCoordinates();
			if ( string.IsNullOrWhiteSpace( geoUri ) )
				return null;

			geoUri = geoUri.ToLower();
			using ( var context = new EntityContext() )
			{
				EM.GeoCoordinate item = context.GeoCoordinate
							.FirstOrDefault( s => s.Url.ToLower() == geoUri );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity );
				}
			}

			return entity;
		}
		/// <summary>
		/// Determine if a geoName already exists for the parent and type (is or is not an exception)
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="geoNamesId"></param>
		/// <param name="isException"></param>
		/// <returns></returns>
		public static bool GeoCoordinates_Exists( int parentId, int geoNamesId, bool isException )
		{
			bool isFound = false;
			using ( var context = new EntityContext() )
			{
				EM.GeoCoordinate item = context.GeoCoordinate
							.FirstOrDefault( s => s.JurisdictionId == parentId && s.GeoNamesId == geoNamesId && s.IsException == isException );

				if ( item != null && item.Id > 0 )
				{
					isFound = true;
				}
			}

			return isFound;
		}

		/// <summary>
		/// Determine if a main region already exists for a jurisdiction. If found, then an update will be done rather than an add.
		/// This requirement may change in the future.
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="isException"></param>
		/// <returns></returns>
		public static bool Jurisdiction_HasMainRegion( int parentId, ref MC.GeoCoordinates entity )
		{
			bool isFound = false;
			//MC.GeoCoordinates entity = new MC.GeoCoordinates();
			using ( var context = new EntityContext() )
			{
				List<EM.GeoCoordinate> list = context.GeoCoordinate
							.Where( s => s.JurisdictionId == parentId && s.IsException == false ).ToList();

				if ( list != null && list.Count > 0 )
				{
					isFound = true;
					MapFromDB( list[ 0 ], entity );
				}
			}

			return isFound;
		}

		/// <summary>
		/// Get a list of geocoordinates from a list of IDs
		/// </summary>
		/// <param name="Ids"></param>
		/// <returns></returns>
		public static List<MC.GeoCoordinates> GeoCoordinates_GetList( List<int> Ids )
		{
			List<MC.GeoCoordinates> entities = new List<MC.GeoCoordinates>();
			using ( var context = new EntityContext() )
			{
				List<EM.GeoCoordinate> items = context.GeoCoordinate.Where( m => Ids.Contains( m.Id ) ).ToList();
				foreach ( var item in items )
				{
					MC.GeoCoordinates entity = new MC.GeoCoordinates();
					MapFromDB( item, entity );
					entities.Add( entity );
				}
			}

			return entities;
		}
		//

		/// <summary>
		/// get all related GeoCoordinates for the parent
		/// </summary>
		/// <param name="parentId"></param>
		/// <returns></returns>
		public static List<MC.GeoCoordinates> GetAll( int parentId, bool isException = false )
		{
			MC.GeoCoordinates entity = new MC.GeoCoordinates();
			List<MC.GeoCoordinates> list = new List<MC.GeoCoordinates>();
			if ( parentId == 0 )
				return list;

			using ( var context = new EntityContext() )
			{
				List<EM.GeoCoordinate> Items = context.GeoCoordinate
							.Where( s => s.JurisdictionId == parentId && s.IsException == isException )
							.OrderBy( s => s.Id ).ToList();

				if ( Items.Count > 0 )
				{
					foreach ( EM.GeoCoordinate item in Items )
					{
						entity = new MC.GeoCoordinates();
						MapFromDB( item, entity );
						list.Add( entity );
					}
				}
			}

			return list;
		}


		private static void MapToDB( MC.GeoCoordinates from, EM.GeoCoordinate to )
		{
			to.Id = from.Id;
			to.JurisdictionId = from.ParentId;

			to.Name = from.Name;
			to.IsException = from.IsException;
			to.AddressRegion = from.Region;
			to.Country = from.Country;
			to.Latitude = from.Latitude;
			to.Longitude = from.Longitude;
			to.Url = from.GeoURI;

			if ( from.GeoNamesId > 0 )
				to.GeoNamesId = from.GeoNamesId;
			else
			{
				//extract from "http://geonames.org/6255149/"
				string geoNamesId = UtilityManager.ExtractNameValue( from.GeoURI, ".org", "/", "/" );
				if ( IsInteger( geoNamesId ) )
					to.GeoNamesId = Int32.Parse( geoNamesId );
			}

		}
		private static void MapFromDB( EM.GeoCoordinate from, MC.GeoCoordinates to )
		{
			to.Id = from.Id;
			to.ParentId = (int)from.JurisdictionId;
			to.GeoNamesId = from.GeoNamesId != null ? ( int ) from.GeoNamesId : 0;

			to.Name = from.Name;
			to.IsException = from.IsException != null ? ( bool ) from.IsException : false;
			to.Region = from.AddressRegion;
			to.Country = from.Country;
			if ( from.Latitude != null)
				to.Latitude = (double)from.Latitude;
			if ( from.Longitude != null )
				to.Longitude = ( double ) from.Longitude;
			to.GeoURI = from.Url;
			to.ProfileSummary = to.Name;
			if ( !string.IsNullOrWhiteSpace( to.Region ) && to.Name != to.Region )
			{
				to.ProfileSummary += ", " + to.Region;
			}
			if ( !string.IsNullOrWhiteSpace( to.Country ) && to.Country != to.Name )
			{
				to.ProfileSummary += ", " + to.Country;
			}

		} //

		#endregion
		#endregion
	}
}
