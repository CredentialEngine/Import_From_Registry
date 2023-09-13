using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;

using ThisEntity = workIT.Models.Common.FinancialAssistanceProfile;
using DBEntity = workIT.Data.Tables.Entity_FinancialAssistanceProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;
using Newtonsoft.Json;

namespace workIT.Factories
{
	public class Entity_FinancialAssistanceProfileManager : BaseFactory
	{
		static string thisClassName = "Entity_FinancialAssistanceProfileManager";

		List<string> messages = new List<string>();


		#region === -Persistance ==================
		public bool SaveList(List<ThisEntity> list, Guid parentUid, ref SaveStatus status)
		{
			if( !IsValidGuid( parentUid ) )
			{
				status.AddError( string.Format( "A valid parent identifier was not provided to the {0}.Add method.", thisClassName ) );
				return false;
			}

			Entity parent = EntityManager.GetEntity( parentUid );
			if( parent == null || parent.Id == 0 )
			{
				status.AddError( "Error - the parent entity was not found." );
				return false;
			}
			bool isAllValid = true;
			try
			{
				//status.Messages = new List<StatusMessage>();
				DeleteAll( parent, ref status );

				if( list == null || list.Count == 0 )
					return true;


				foreach( ThisEntity item in list )
				{
					Save( item, parent, ref status );
				}
			}
			catch( Exception ex )
			{
				string message = FormatExceptions( ex );
				messages.Add( "Error - the SaveList was not successful. " + message );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".SaveList(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );
				isAllValid = false;
			}
			return isAllValid;
		}


		/// <summary>
		/// Persist FinancialAssistanceProfile
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="parentUid"></param>
		/// <param name="userId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Save( ThisEntity entity, Entity parent, ref SaveStatus status )
		{
			bool isValid = true;
			int intialCount = messages.Count;

			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( "Error - the parent entity was not found." );
				return false;
			}

			if ( messages.Count > intialCount )
				return false;

			int count = 0;

			DBEntity efEntity = new DBEntity();

			using ( var context = new EntityContext() )
			{
				try
				{

					if ( ValidateProfile( entity, ref messages ) == false )
					{
						//actually should always continue to the save
						//return false;
					}
					bool doingParts = true;
					if ( entity.Id == 0 )
					{
						//add
						efEntity = new DBEntity();
						MapToDB( entity, efEntity );
						efEntity.EntityId = parent.Id;
						efEntity.Created = efEntity.LastUpdated = DateTime.Now;
						efEntity.RowId = Guid.NewGuid();

						context.Entity_FinancialAssistanceProfile.Add( efEntity );
						count = context.SaveChanges();

						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						if ( count == 0 )
						{
							messages.Add( " Unable to add Financial Assistance Profile" );
							doingParts = false;
						}

					}
					else
					{

						efEntity = context.Entity_FinancialAssistanceProfile.SingleOrDefault( s => s.Id == entity.Id );
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
						}
					}
					//ALWAYS DO PARTS
					if ( doingParts )
					{
						UpdateParts( entity, ref status );
					}
				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					messages.Add( "Error - the save was not successful. " + message );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );
					isValid = false;
				}

			}

			return isValid;
		}
		public bool UpdateParts(ThisEntity entity, ref SaveStatus status)
		{

			bool isAllValid = true;
			Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( "Error - the related Entity was not found for financial assistance profile." );
				return false;
			}

			EntityPropertyManager mgr = new EntityPropertyManager();
			//first clear all properties
			mgr.DeleteAll( relatedEntity, ref status );

			if ( mgr.AddProperties( entity.FinancialAssistanceType, entity.RowId, CodesManager.ENTITY_TYPE_FINANCIAL_ASST_PROFILE, CodesManager.PROPERTY_CATEGORY_FINANCIAL_ASSISTANCE, false, ref status ) == false )
				isAllValid = false;


			return isAllValid;
		} //
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
					var results = context.Entity_FinancialAssistanceProfile.Where( s => s.EntityId == parent.Id )
					.ToList();
					if ( results == null || results.Count == 0 )
						return true;

					context.Entity_FinancialAssistanceProfile.RemoveRange( context.Entity_FinancialAssistanceProfile.Where( s => s.EntityId == parent.Id ) );
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
		/// <summary>
		/// Delete a Financial Assistance profile, and related Entity
		/// </summary>
		/// <param name="Id"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int Id, ref string statusMessage )
		{
			bool isValid = false;
			if ( Id == 0 )
			{
				statusMessage = "Error - missing an identifier for the FinancialAssistanceProfile";
				return false;
			}
			using ( var context = new EntityContext() )
			{
				try
				{
					DBEntity efEntity = context.Entity_FinancialAssistanceProfile
								.SingleOrDefault( s => s.Id == Id );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						Guid rowId = efEntity.RowId;

						context.Entity_FinancialAssistanceProfile.Remove( efEntity );
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							isValid = true;
							//do with trigger now
							//new EntityManager().Delete( rowId, ref statusMessage );
						}
					}
					else
					{
						statusMessage = "Error - delete failed, as record was not found.";
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + ".Delete()" );

                    statusMessage = FormatExceptions( ex );
                    if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
					{
						statusMessage = "Error: this Financial Assistance cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this Financial Assistance can be deleted.";
					}
				}
			}

			return isValid;
		}

		public bool ValidateProfile( ThisEntity profile, ref List<string> messages )
		{
			bool isValid = true;
			int count = messages.Count;

			//check if empty
			if ( string.IsNullOrWhiteSpace( profile.Name )
				&& string.IsNullOrWhiteSpace( profile.Description )
				&& string.IsNullOrWhiteSpace( profile.SubjectWebpage )
				)
			{
				messages.Add( "Please provide a little more information, before attempting to save this profile" );
				return false;
			}
			//
			if ( string.IsNullOrWhiteSpace( profile.Name ) )
			{
				messages.Add( "A Profile name must be entered" );
			}

			if ( !IsUrlValid( profile.SubjectWebpage, ref commonStatusMessage ) )
			{
				messages.Add( "The profile SubjectWebpage is invalid " + commonStatusMessage );
			}



			if ( messages.Count > count )
				isValid = false;

			return isValid;
		}

		#endregion

		#region == Retrieval =======================
		public static ThisEntity Get( int id )
		{
			ThisEntity entity = new ThisEntity();

			using ( var context = new EntityContext() )
			{

				DBEntity item = context.Entity_FinancialAssistanceProfile
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity );
				}
			}

			return entity;
		}
		public static ThisEntity Get( Guid rowId )
		{
			ThisEntity entity = new ThisEntity();

			using ( var context = new EntityContext() )
			{

				DBEntity item = context.Entity_FinancialAssistanceProfile
						.FirstOrDefault( s => s.RowId == rowId );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity );
				}
			}

			return entity;
		}


		/// <summary>
		/// Get all the Financial Assistances for the parent entity (ex a credential)
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returns>
		public static List<ThisEntity> GetAll( Guid parentUid, bool isForLinks = false )
		{
			ThisEntity to = new ThisEntity();
			List<ThisEntity> list = new List<ThisEntity>();
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}

			try
			{
				using ( var context = new EntityContext() )
				{
					//context.Configuration.LazyLoadingEnabled = false;

					List<EM.Entity_FinancialAssistanceProfile> results = context.Entity_FinancialAssistanceProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Name)
							.ThenBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( EM.Entity_FinancialAssistanceProfile from in results )
						{
							to = new ThisEntity();
							if ( isForLinks )
							{
								to.Id = from.Id;
								to.RowId = from.RowId;
								to.ProfileName = from.Name;
								to.SubjectWebpage = from.SubjectWebpage;
							}
							else
							{
								MapFromDB( from, to );
							}
							list.Add( to );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAll (Guid parentUid)" );
			}
			return list;
		}//

		public static List<ThisEntity> Search( int topParentTypeId, int topParentEntityBaseId )
		{
			ThisEntity to = new ThisEntity();
			List<ThisEntity> list = new List<ThisEntity>();
			Entity parent = EntityManager.GetEntity( topParentTypeId, topParentEntityBaseId );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}

			try
			{
				using ( var context = new EntityContext() )
				{
					//context.Configuration.LazyLoadingEnabled = false;

					List<EM.Entity_FinancialAssistanceProfile> results = context.Entity_FinancialAssistanceProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Name )
							.ThenBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( EM.Entity_FinancialAssistanceProfile from in results )
						{
							to = new ThisEntity();
							MapFromDB( from, to );
							list.Add( to );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Search (topParentTypeId, topParentEntityBaseId)" );
			}
			return list;
		}//

		public static void MapToDB( ThisEntity from, DBEntity to )
		{

			//want to ensure fields from create are not wiped
			if ( to.Id == 0 )
			{
				
			}


			to.Id = from.Id;
			to.Name = GetData( from.Name );
			to.SubjectWebpage = GetData( from.SubjectWebpage );
			to.Description = GetData( from.Description );
			//QuantitativeValue as json
			to.FinancialAssistanceValue = from.FinancialAssistanceValueJson;

		}
		public static void MapFromDB( DBEntity from, ThisEntity to)
		{
			to.Id = from.Id;
			to.RowId = from.RowId;
			to.RelatedEntityId = from.EntityId;
			to.Name = GetData( from.Name );
			to.SubjectWebpage = GetData( from.SubjectWebpage );
			to.Description = GetData( from.Description );
			to.FinancialAssistanceType = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_FINANCIAL_ASSISTANCE );
			to.FinancialAssistanceValueJson = from.FinancialAssistanceValue;
			if (!string.IsNullOrWhiteSpace( to.FinancialAssistanceValueJson ) )
			{
				to.FinancialAssistanceValue = JsonConvert.DeserializeObject<List<QuantitativeValue>>( to.FinancialAssistanceValueJson );
				to.FinancialAssistanceValueSummary = SummarizeFinancialAssistanceValue( to.FinancialAssistanceValue );
			}
			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;

			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime ) from.LastUpdated;

		}
		private static List<string> SummarizeFinancialAssistanceValue( List<QuantitativeValue> input )
		{
			var output = new List<string>();
			if ( input == null || !input.Any() )
				return output;
			
			foreach ( var item in input )
			{
				var summary = "";
				var units = !string.IsNullOrWhiteSpace(item.UnitText) ? item.UnitText : string.Join( ",", item.CreditUnitType.Items.ToArray().Select( m => m.Name));
				var currencySymbol = "";
				if ( !string.IsNullOrWhiteSpace( units ) )
				{
					if ( units.ToLower() == "usd" )
						currencySymbol = "$";
					else
					{
						var currency = CodesManager.GetCurrencyItem( units );
						if ( currency != null && currency.NumericCode > 0 )
						{
							currencySymbol = currency.HtmlCodes;
						}
					}
				}

				if ( item.IsRange )
				{
					if ( string.IsNullOrWhiteSpace( currencySymbol ) ) 
						summary = string.Format( "{0} to {1} {2}", item.MinValue.ToString( "#,##0" ), item.MaxValue.ToString( "#,##0" ), units );
					else
						summary = string.Format( "{2} {0} to {2} {1}", item.MinValue.ToString( "#,##0" ), item.MaxValue.ToString( "#,##0" ), currencySymbol );
				}
				else if ( item.Percentage > 0 )
				{
					summary = string.Format( "{0}% {1}", item.Percentage.ToString( "##0" ), units );
				}
				else if( item.Value > 0 )
				{
					if (string.IsNullOrWhiteSpace( currencySymbol ) )
						summary = string.Format( "{0} {1}", item.Value.ToString( "#,##0" ), units );
					else
						summary = string.Format( "{1} {0}", item.Value.ToString( "#,##0" ), currencySymbol );
				}
				output.Add( summary );
			}

			return output;
		}
		#endregion
	}
}
