using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;

using ThisEntity = workIT.Models.ProfileModels.CostProfile;
using DBEntity = workIT.Data.Tables.Entity_CostProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

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

		/// <summary>
		/// Persist Cost Profile
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="parentUid"></param>
		/// <param name="userId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Save( ThisEntity entity, Guid parentUid,  ref SaveStatus status )
		{
			bool isValid = true;
			if ( !IsValidGuid( parentUid ) )
			{
				status.AddError( thisClassName + " - Error: the parent identifier was not provided." );
				return false;
			}

			//get parent entity
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( thisClassName + " - Error - the parent entity was not found." );
				return false;
			}
			int count = 0;

			DBEntity efEntity = new DBEntity();

			using ( var context = new EntityContext() )
			{
				if ( ValidateProfile( entity, ref status ) == false )
				{
					//can't really scrub from here - too late?
					//at least add some identifer
					return false;
				}

				try
				{
					if ( entity.Id == 0 )
					{
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
						}
						else
						{
							if ( !UpdateParts( entity, ref status) )
								isValid = false;
						}
					}
					else
					{
						context.Configuration.LazyLoadingEnabled = false;

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
							if ( !UpdateParts( entity, ref status ) )
								isValid = false;
						}
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Save()", entity.ProfileName );

					status.AddWarning( thisClassName + " - Error - the save was not successful. " + message );
					LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );
					isValid = false;
				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					status.AddError( thisClassName + " - Error - the save was not successful. " + message );

					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save(), Parent: {0} ({1}), UserId: {2}", parent.EntityBaseName, parent.EntityBaseId ) );
					isValid = false;
				}
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
                context.Entity_CostProfile.RemoveRange( context.Entity_CostProfile.Where( s => s.EntityId == parent.Id ) );
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
        private bool UpdateParts( ThisEntity entity, ref SaveStatus status )
		{
			bool isAllValid = true;

			if ( new Entity_ReferenceManager().Add( entity.Condition, entity.RowId, CodesManager.ENTITY_TYPE_CONDITION_PROFILE, ref status, CodesManager.PROPERTY_CATEGORY_CONDITION_ITEM, false ) == false )
				isAllValid = false;

			//JurisdictionProfile 
			Entity_JurisdictionProfileManager jpm = new Entity_JurisdictionProfileManager();
			jpm.SaveList( entity.Jurisdiction, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status );

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
			if ( !string.IsNullOrWhiteSpace( profile.DateEffective )  )
			{
				if ( !IsValidDate( profile.DateEffective ) )
					status.AddWarning( "Please enter a valid start date" );
				else
				{
					DateTime.TryParse( profile.DateEffective, out startDate );
				}
			}
			if ( !string.IsNullOrWhiteSpace( profile.ExpirationDate )  )
			{
				if ( !IsValidDate( profile.ExpirationDate ) )
					status.AddWarning( "Please enter a valid end date" );
				else
				{
					DateTime.TryParse( profile.ExpirationDate, out endDate );
					if ( IsValidDate( profile.DateEffective )
						&& startDate > endDate)
						status.AddWarning( "The end date must be greater than the start date." );
				}
			}
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
			
			return !status.HasSectionErrors;
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

			if ( IsValidDate( from.ExpirationDate ) )
				to.ExpirationDate = DateTime.Parse( from.ExpirationDate );
			else
				to.ExpirationDate = null;

			if ( IsValidDate( from.DateEffective ) )
				to.DateEffective = DateTime.Parse( from.DateEffective );
			else
				to.DateEffective = null;

			to.DetailsUrl = from.CostDetails;

			to.CurrencyTypeId = null;
			if ( from.CurrencyTypeId > 0 )
				to.CurrencyTypeId = from.CurrencyTypeId;
			else if (!string.IsNullOrWhiteSpace(from.Currency))
			{
				Views.Codes_Currency currency = CodesManager.GetCurrencyItem( from.Currency );
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
				to.EndDate = ( ( DateTime ) from.ExpirationDate ).ToShortDateString();
			else
				to.EndDate = "";

			if ( IsValidDate( from.DateEffective ) )
				to.StartDate = ( ( DateTime ) from.DateEffective ).ToShortDateString();
			else
				to.StartDate = "";

			to.CostDetails = from.DetailsUrl;
			
			to.CurrencyTypeId = (int)(from.CurrencyTypeId ?? 0);
			Views.Codes_Currency code = CodesManager.GetCurrencyItem( to.CurrencyTypeId );
			if ( code != null && code.NumericCode > 0 )
			{
				to.Currency = code.Currency;
				to.CurrencySymbol = code.HtmlCodes;
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
