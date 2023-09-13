using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;

using DBEntity = workIT.Data.Tables.Entity_CostProfile;
using EM = workIT.Data.Tables;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisEntity = workIT.Models.ProfileModels.CostProfile;

namespace workIT.Factories
{
	public class CostProfileManager : BaseFactory
	{
		static string thisClassName = "CostProfileManager";
		#region persistance ==================
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
                status.AddError( thisClassName + ". Error - the parent entity was not found." );
                return false;
            }
			//21-03-31 mp	- back to deleteAll - problem with related tables like entity.reference (also maybe cost items?) not getting deleted, so adding dups 
			//				- consider an existance check. Easier for entity.reference, but not for jurisdictions
			DeleteAll( parent, ref status );
			//now maybe still do a delete all until implementing a balance line
			//could set date and delete all before this date!
			DateTime updateDate = DateTime.Now;
			if ( IsValidDate( status.EnvelopeUpdatedDate ) )
			{
				updateDate = status.LocalUpdatedDate;
			}

			//var current = GetAll( parent.EntityUid );
			bool isAllValid = true;
			if ( list == null || list.Count == 0 )
			{
				//if ( current != null && current.Any() )
				//{
				//	DeleteAll( parent, ref status );
				//}
				return true;
			}
			//else    //may not need this if the new list version works		
			//if ( list.Count == 1 && current.Count == 1 )
			//{
			//	//just do update of one
			//	var entity = list[ 0 ];
			//	entity.Id = current[ 0 ].Id;

			//	Save( entity, parent, ref status );
			//}
			else
			{
				//21-03-16 mp - go back to delete all for now
				//DeleteAll( parent, ref status );
				int cntr = 0;
				foreach ( ThisEntity item in list )
				{
					//if ( current != null && current.Count > cntr )
					//{
					//	item.Id = current[ cntr ].Id;
					//}
					Save( item, parent, ref status );
					cntr++;
				}
				//delete any records with last updated less than updateDate
				//this will not work if doing many re-imports - with same date
				//DeleteAll( parent, ref status, updateDate );
			}
			return isAllValid;
		}

		/// <summary>
		/// Persist Cost Profile
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="parentUid"></param>
		/// <param name="userId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Save( ThisEntity entity, Entity parent,  ref SaveStatus status )
		{
			bool isValid = true;
			//if ( !IsValidGuid( parentUid ) )
			//{
			//	status.AddError( thisClassName + " - Error: the parent identifier was not provided." );
			//	return false;
			//}

			////get parent entity
			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( thisClassName + " - Error - the parent entity was not found." );
				return false;
			}
			int count = 0;

			DBEntity efEntity = new DBEntity();


			if ( ValidateProfile( entity, ref status ) == false )
			{
				//can't really scrub from here - too late?
				//at least add some identifer
				//return false;
			}

			try
			{
				bool doingUpdateParts = true;
				using ( var context = new EntityContext() )
				{
					if ( entity.Id == 0 )
					{
						efEntity = new DBEntity();
						//check for current match - only do if not deleting
						//not unexpected that the same cost details url could be used for more than one profile
						//var exists = context.Entity_CostProfile
						//	.Where( s => s.EntityId == parent.Id
						//		&& s.Description == entity.Description
						//		&& s.DetailsUrl == entity.CostDetails 
						//	)
						//	.OrderBy( s => s.Created ).ThenBy( s => s.LastUpdated )
						//	.ToList();

						//just in case
						entity.EntityId = parent.Id;

						//add
						efEntity = new DBEntity();
						MapToDB( entity, efEntity );
						efEntity.Created = efEntity.LastUpdated = DateTime.Now;
						if ( IsValidGuid( entity.RowId ) )
							efEntity.RowId = entity.RowId;
						else
							efEntity.RowId = Guid.NewGuid();

						context.Entity_CostProfile.Add( efEntity );
						count = context.SaveChanges();
						//update profile record so doesn't get deleted
						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						if ( count == 0 )
						{
							status.AddError( thisClassName + " - Unable to add Cost Profile" );
							doingUpdateParts = false;
						}
						else
						{
							//if ( !UpdateParts( entity, ref status ) )
							//	isValid = false;
						}
					}
					else
					{
						//context.Configuration.LazyLoadingEnabled = false;

						efEntity = context.Entity_CostProfile.SingleOrDefault( s => s.Id == entity.Id );
						if ( efEntity != null && efEntity.Id > 0 )
						{
							entity.RowId = efEntity.RowId;
							//update
							MapToDB( entity, efEntity );
							//has changed?
							if ( HasStateChanged( context ) )
							{
								efEntity.LastUpdated = System.DateTime.Now;
								count = context.SaveChanges();
							}
							//always check parts
							//if ( !UpdateParts( entity, ref status ) )
							//	isValid = false;
						}
					}
				}
				//21-04-21 mparsons - end the current context before doing parts
				if ( doingUpdateParts )
				{
					//always check parts
					if ( !UpdateParts( entity, ref status ) )
						isValid = false;
				}

			}
			catch ( DbEntityValidationException dbex )
			{
				string message2 = HandleDBValidationError( dbex, thisClassName + ".Save()", entity.ProfileName );
				status.AddWarning( thisClassName + " - Error - the save was not successful. " + message2 );
				LoggingHelper.LogError( dbex, thisClassName, $".Save()-DbEntityValidationException, Parent: {parent.EntityBaseName} (type: {parent.EntityTypeId}, Id: {parent.EntityBaseId})" );
				return false;
			}
			catch ( Exception ex )
			{
				string message = BaseFactory.FormatExceptions( ex );
				status.AddError( thisClassName + " - Error - the save was not successful. " + message );
				LoggingHelper.LogError( ex, thisClassName, $".Save(), Parent: {parent.EntityBaseName} (parent type: {parent.EntityTypeId}, Id: {parent.EntityBaseId})", notifyAdmin: true );
				return false;
			}


			return isValid;
		}
		public bool DeleteAll( Entity parent, ref SaveStatus status )
		{
			LoggingHelper.DoTrace( 7, thisClassName + ".DeleteAll - entered" );

			bool isValid = true;
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( thisClassName + ". Error - the provided target parent entity was not provided." );
				return false;
			}
			bool retry = false;
			isValid = DeleteAll( parent, 1, ref status, ref retry );
			if ( !isValid && retry )
			{
				LoggingHelper.DoTrace( 5, thisClassName + ".DeleteAll - RETRYING *********" );

				return DeleteAll( parent, 1, ref status, ref retry ); ;
			}
			return isValid;
		}
        public bool DeleteAll( Entity parent, int attemptNbr, ref SaveStatus status, ref bool retry )
        {
            bool isValid = false;
			int expectedDeleteCount = 0;

			using ( var context = new EntityContext() )
            {
				try
				{
					var results = context.Entity_CostProfile.Where( s => s.EntityId == parent.Id )
						.ToList();
					if ( results == null || results.Count == 0 )
						return true;
					expectedDeleteCount = results.Count;
					foreach ( var item in results )
					{
						string statusMessage = "";
						//we have a trigger for this
						//new EntityManager().Delete( item.RowId, string.Format("CostProfile: {0} for EntityType: {1} ({2})", item.Id, parent.EntityTypeId, parent.EntityBaseId), ref statusMessage );

						context.Entity_CostProfile.Remove( item );
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
					//context.Entity_CostProfile.RemoveRange( context.Entity_CostProfile.Where( s => s.EntityId == parent.Id ) );
					
				}
				catch ( System.Data.Entity.Infrastructure.DbUpdateConcurrencyException dbcex )
				{
					if ( dbcex.Message.IndexOf( "an unexpected number of rows (0)" ) > 0 )
					{
						//don't know why this happens, quashing for now.
						LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. Parent type: {0}, ParentId: {1}, expectedDeletes: {2}. Message: {3}", parent.EntityTypeId, parent.EntityBaseId, expectedDeleteCount, dbcex.Message ) );
						isValid = true;
					}
					else
					{
						var msg = BaseFactory.FormatExceptions( dbcex );
						LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. ParentType: {0}, baseId: {1}, DbUpdateConcurrencyException: {2}", parent.EntityType, parent.EntityBaseId, msg ) );
					}

				}
				catch ( Exception ex )
				{
					var msg = BaseFactory.FormatExceptions( ex );
					LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. ParentType: {0}, baseId: {1}, exception: {2}", parent.EntityType, parent.EntityBaseId, msg ) );

					if ( msg.IndexOf( "was deadlocked on lock resources" ) > 0 )
					{
						retry = true;
					}
				}
			}

            return isValid;
        }

		//public bool DeleteAll( Entity parent, ref SaveStatus status, DateTime? lastUpdated = null )
		//{
		//	bool isValid = true;
		//	if ( parent == null || parent.Id == 0 )
		//	{
		//		status.AddError( thisClassName + ". Error - the provided target parent entity was not provided." );
		//		return false;
		//	}
		//	int expectedDeleteCount = 0;
		//	try
		//	{
		//		using ( var context = new EntityContext() )
		//		{
		//			var results = context.Entity_CostProfile.Where( s => s.EntityId == parent.Id && ( lastUpdated == null || s.LastUpdated < lastUpdated ) )
		//		.ToList();
		//			if ( results == null || results.Count == 0 )
		//				return true;
		//			expectedDeleteCount = results.Count;

		//			foreach ( var item in results )
		//			{
		//				context.Entity_CostProfile.Remove( item );
		//				var count = context.SaveChanges();
		//				if ( count > 0 )
		//				{

		//				}
		//			}
		//		}
		//	}
		//	catch ( System.Data.Entity.Infrastructure.DbUpdateConcurrencyException dbcex )
		//	{
		//		if ( dbcex.Message.IndexOf( "an unexpected number of rows (0)" ) > 0 )
		//		{
		//			//don't know why this happens, quashing for now.
		//			LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. Parent type: {0}, ParentId: {1}, expectedDeletes: {2}. Message: {3}", parent.EntityTypeId, parent.EntityBaseId, expectedDeleteCount, dbcex.Message ) );
		//		}
		//		else
		//		{
		//			var msg = BaseFactory.FormatExceptions( dbcex );
		//			LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. ParentType: {0}, baseId: {1}, DbUpdateConcurrencyException: {2}", parent.EntityType, parent.EntityBaseId, msg ) );
		//		}

		//	}
		//	catch ( Exception ex )
		//	{
		//		var msg = BaseFactory.FormatExceptions( ex );
		//		LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. ParentType: {0}, baseId: {1}, exception: {2}", parent.EntityType, parent.EntityBaseId, msg ) );
		//	}
		//	return isValid;
		//}
		//
		private bool UpdateParts( ThisEntity entity, ref SaveStatus status )
		{
			bool isAllValid = true;
			//
			try
			{
				if ( new Entity_ReferenceManager().Add( entity.Condition, entity.RowId, CodesManager.ENTITY_TYPE_COST_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_CONDITION_ITEM, false ) == false )
					isAllValid = false;

				//JurisdictionProfile 
				Entity_JurisdictionProfileManager jpm = new Entity_JurisdictionProfileManager();
				jpm.SaveList( entity.Jurisdiction, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status );
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 1, thisClassName + ".UpdateParts(). Exception while processing condition/jurisdiction. " + ex.Message );
				status.AddError( ex.Message );
			}


            if ( entity.Items != null && entity.Items.Count > 0)
			    new CostProfileItemManager().SaveList( entity.Items, entity.Id, ref status );

			return isAllValid;
		}
		public bool Delete( int recordId, ref string statusMessage )
		{
			bool isOK = true;
			using ( var context = new EntityContext() )
			{
				DBEntity p = context.Entity_CostProfile.FirstOrDefault( s => s.Id == recordId );
				if ( p != null && p.Id > 0 )
				{
					context.Entity_CostProfile.Remove( p );
					int count = context.SaveChanges();
				}
				else
				{
					statusMessage = string.Format( "Cost Profile record was not found: {0}", recordId );
					isOK = false;
				}
			}
			return isOK;

		}

		#endregion

		#region  retrieval ==================

		/// <summary>
		/// Retrieve and fill cost profiles for parent entity
		/// </summary>
		/// <param name="parentUid"></param>
		public static List<ThisEntity> GetAll( Guid parentUid )
		{
			
			ThisEntity row = new ThisEntity();
			DurationItem duration = new DurationItem();
			List<ThisEntity> profiles = new List<ThisEntity>();
			if ( parentUid == null )
				return profiles;
			Entity parent = EntityManager.GetEntity( parentUid );

			using ( var context = new EntityContext() )
			{
				List<DBEntity> results = context.Entity_CostProfile
						.Where( s => s.EntityId == parent.Id )
						.OrderBy( s => s.Id )
						.ToList();

				if ( results != null && results.Count > 0 )
				{
					foreach ( DBEntity item in results )
					{
						row = new ThisEntity();
						MapFromDB( item, row, true );
						profiles.Add( row );
					}
				}
				return profiles;
			}

		}//
		public static List<ThisEntity> GetAllForList( Guid parentUid )
		{
			ThisEntity row = new ThisEntity();
			DurationItem duration = new DurationItem();
			List<ThisEntity> profiles = new List<ThisEntity>();
			Entity parent = EntityManager.GetEntity( parentUid );

			using ( var context = new EntityContext() )
			{
				List<DBEntity> results = context.Entity_CostProfile
						.Where( s => s.EntityId == parent.Id )
						.OrderBy( s => s.Id )
						.ToList();

				if ( results != null && results.Count > 0 )
				{
					foreach ( DBEntity item in results )
					{
						row = new ThisEntity();
						MapFromDB( item, row, false );
						profiles.Add( row );
					}
				}
				return profiles;
			}

		}//

		public static ThisEntity GetBasicProfile( int profileId )
		{
			ThisEntity entity = new ThisEntity();

			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				DBEntity item = context.Entity_CostProfile
							.SingleOrDefault( s => s.Id == profileId );

				if ( item != null && item.Id > 0 )
				{
					entity.Id = item.Id;
					entity.RowId = item.RowId;
					entity.EntityId = item.EntityId;
					entity.ProfileName = item.ProfileName;
					entity.Description = item.Description;
				}
			}
			return entity;
		}//
		public static ThisEntity GetBasicProfile( Guid profileUid )
		{
			ThisEntity entity = new ThisEntity();

			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				DBEntity item = context.Entity_CostProfile
							.SingleOrDefault( s => s.RowId == profileUid );

				if ( item != null && item.Id > 0 )
				{
					entity.Id = item.Id;
					entity.RowId = item.RowId;
					entity.EntityId = item.EntityId;
					entity.ProfileName = item.ProfileName;
					entity.Description = item.Description;
				}
			}
			return entity;
		}//
	
		public bool ValidateProfile( ThisEntity profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;

			if ( !IsUrlValid( profile.CostDetails, ref commonStatusMessage ) )
			{
				status.AddWarning( "The Cost Details Url is invalid" + commonStatusMessage );
			}
			DateTime startDate = DateTime.Now;
			DateTime endDate = DateTime.Now;
			//skip edits, not likely 
			//if ( !string.IsNullOrWhiteSpace( profile.StartDate )  )
			//{
			//	if ( !IsValidDate( profile.StartDate ) )
			//		status.AddWarning( "Please enter a valid start date" );
			//	else
			//	{
			//		DateTime.TryParse( profile.StartDate, out startDate );
			//	}
			//}
			//if ( !string.IsNullOrWhiteSpace( profile.EndDate )  )
			//{
			//	if ( !IsValidDate( profile.EndDate ) )
			//		status.AddWarning( "Please enter a valid end date" );
			//	else
			//	{
			//		DateTime.TryParse( profile.EndDate, out endDate );
			//		if ( IsValidDate( profile.StartDate )
			//			&& startDate > endDate)
			//			status.AddWarning( "The end date must be greater than the start date." );
			//	}
			//}
			//currency?
			//if ( string.IsNullOrWhiteSpace( profile.Currency ) == false )
			//{
			//	//length
			//	if ( profile.Currency.Length != 3 || IsInteger( profile.Currency ) )
			//	{
			//		status.AddError( "The currency code must be a three-letter alphabetic code  " );
			//		isValid = false;
			//	}
			//}
			
			return status.WasSectionValid;
		}

		public static void MapToDB( ThisEntity from, DBEntity to )
		{
			to.Id = from.Id;
			
			if ( to.Id == 0 )
			{
				//make sure EntityId is not wiped out. Also can't actually chg
				if ( ( to.EntityId) == 0 )
					to.EntityId = from.EntityId;
			}

			to.ProfileName = from.ProfileName;
			to.Description = from.Description;

			if ( IsValidDate( from.EndDate ) )
				to.ExpirationDate = DateTime.Parse( from.EndDate );
			else
				to.ExpirationDate = null;

			if ( IsValidDate( from.StartDate ) )
				to.DateEffective = DateTime.Parse( from.StartDate );
			else
				to.DateEffective = null;

			to.DetailsUrl = from.CostDetails;

			to.CurrencyTypeId = null;
			if ( from.CurrencyTypeId > 0 )
				to.CurrencyTypeId = from.CurrencyTypeId;
			else if (!string.IsNullOrWhiteSpace(from.Currency))
			{
				var currency = CodesManager.GetCurrencyItem( from.Currency );
				if ( currency != null && currency.NumericCode > 0 )
					to.CurrencyTypeId = currency.NumericCode;
			}

		}
		public static void MapFromDB( DBEntity from, ThisEntity to, bool includingItems )
		{
			to.Id = from.Id;
			to.RowId = from.RowId;
			to.EntityId = from.EntityId;

			to.ProfileName = from.ProfileName;
			to.Description = from.Description;

			if ( IsValidDate( from.ExpirationDate ) )
				to.EndDate = ( ( DateTime ) from.ExpirationDate ).ToString("yyyy-MM-dd");
			else
				to.EndDate = "";

			if ( IsValidDate( from.DateEffective ) )
				to.StartDate = ( ( DateTime ) from.DateEffective ).ToString("yyyy-MM-dd");
			else
				to.StartDate = "";

			to.CostDetails = from.DetailsUrl;
			
			to.CurrencyTypeId = (int)(from.CurrencyTypeId ?? 0);
			var code = CodesManager.GetCurrencyItem( to.CurrencyTypeId );
			if ( code != null && code.NumericCode > 0 )
			{
				to.Currency = code.Currency;
				if ( code.Currency.ToLower() == "usd" )
					to.CurrencySymbol = "$";
				else
				{
					to.CurrencySymbol = code.HtmlCodes;
				}
			}

			to.ProfileSummary = SetCostProfileSummary( to );
			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime ) from.LastUpdated;

			to.Condition = Entity_ReferenceManager.GetAll( to.RowId, CodesManager.PROPERTY_CATEGORY_CONDITION_ITEM );

			if ( includingItems )
			{
				//TODO - the items should be part of the EF record
				if ( from.Entity_CostProfileItem != null && from.Entity_CostProfileItem.Count > 0 )
				{
					CostProfileItem row = new CostProfileItem();
					foreach ( EM.Entity_CostProfileItem item in from.Entity_CostProfileItem )
					{
						row = new CostProfileItem();
						//TODO
						CostProfileItemManager.MapFromDB( item, row, true );
						to.Items.Add( row );
					}
				}

				to.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( to.RowId );
				to.Region = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( to.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_RESIDENT );
			}
		}
		static string SetCostProfileSummary( ThisEntity to )
		{
			string summary = "Cost Profile ";
			if ( !string.IsNullOrWhiteSpace( to.ProfileName ) )
			{
				summary = to.ProfileName;
				return summary;
			}

			if ( to.Id > 1 )
			{
				summary += to.Id.ToString();
				return summary;
			}
			return summary;

		}
		#endregion

		#region  cost items ==================
		

		#endregion

	}
}
