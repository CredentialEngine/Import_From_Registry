using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;

using ThisEntity = workIT.Models.ProfileModels.CostProfileItem;
using DBEntity = workIT.Data.Tables.Entity_CostProfileItem;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

namespace workIT.Factories
{
	public class CostProfileItemManager : BaseFactory
	{
		string thisClassName = "CostProfileItemManager";
		#region persistance ==================
		public bool SaveList( List<ThisEntity> list, int parentId, ref SaveStatus status )
		{
			if ( list == null || list.Count == 0 )
				return true;

			bool isAllValid = true;
			foreach ( ThisEntity item in list )
			{
				Save( item, parentId, ref status );
			}

			return isAllValid;
		}

		private bool Save( ThisEntity entity, int parentId, ref SaveStatus status )
		{
			bool isValid = true;
			if ( parentId == 0 )
			{
				status.AddError( "CostProfileItemManager.Save() - Error: the parent cost profile id was not provided." );
				return false;
			}
			
			int count = 0;

			DBEntity efEntity = new DBEntity();

			using ( var context = new EntityContext() )
			{
				if ( ValidateProfile( entity, ref status ) == false )
				{
					return false;
				}
				
				try
				{
					//just in case
					entity.CostProfileId = parentId;

					if ( entity.Id == 0 )
					{
						//add
						efEntity = new DBEntity();
						MapToDB( entity, efEntity );

						efEntity.RowId = Guid.NewGuid();
						efEntity.Created = efEntity.LastUpdated = DateTime.Now;
						context.Entity_CostProfileItem.Add( efEntity );
						count = context.SaveChanges();
						//update profile record so doesn't get deleted
						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						if ( count == 0 )
						{
							status.AddError( string.Format( " Unable to add Cost Item for CostProfileId: {0}, CostTypeId: {1}  ", parentId, entity.CostTypeId ));
							isValid = false;
						}
						else
						{
							UpdateParts( entity, ref status );
						}

					}
					else
					{
						context.Configuration.LazyLoadingEnabled = false;

						efEntity = context.Entity_CostProfileItem.SingleOrDefault( s => s.Id == entity.Id );
						if ( efEntity != null && efEntity.Id > 0 )
						{
							//update
							MapToDB( entity, efEntity );
							//has changed?
							if ( HasStateChanged( context ) )
							{
								efEntity.LastUpdated = System.DateTime.Now;

								count = context.SaveChanges();
							}
							//always check parts
							entity.RowId = efEntity.RowId;
							UpdateParts( entity, ref status);
						}
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, "CostProfileItemManager.Save()", string.Format( "CostProfileId: 0 , CostTypeId: {1}  ", parentId, entity.CostTypeId ));

					status.AddError( message );
					isValid = false;
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, string.Format( "CostProfileItemManager.Save(), CostProfileId: 0 , CostTypeId: {1}  ", parentId, entity.CostTypeId ) );
					isValid = false;
				}
			}

			return isValid;
		}
		public bool Delete( int recordId, ref string statusMessage )
		{
			bool isOK = true;
			using ( var context = new EntityContext() )
			{
				DBEntity p = context.Entity_CostProfileItem.FirstOrDefault( s => s.Id == recordId );
				if ( p != null && p.Id > 0 )
				{
					context.Entity_CostProfileItem.Remove( p );
					int count = context.SaveChanges();
				}
				else
				{
					statusMessage = string.Format( "CostProfileItem record was not found: {0}", recordId );
					isOK = false;
				}
			}
			return isOK;

		}

		private bool UpdateParts( ThisEntity entity, ref SaveStatus status )
		{
			bool isAllValid = true;

			EntityPropertyManager mgr = new EntityPropertyManager();

			if ( mgr.AddProperties( entity.AudienceType, entity.RowId, CodesManager.ENTITY_TYPE_COST_PROFILE_ITEM, CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE, false,  ref status ) == false )
			{
				isAllValid = false;
			}

			if ( mgr.AddProperties( entity.ResidencyType, entity.RowId, CodesManager.ENTITY_TYPE_COST_PROFILE_ITEM, CodesManager.PROPERTY_CATEGORY_RESIDENCY_TYPE, false, ref status ) == false )
			{
				isAllValid = false;
			}

			return isAllValid;
		}
		#endregion

		#region  retrieval ==================

		
		public static ThisEntity Get( int profileId, bool includingProperties )
		{
			ThisEntity entity = new ThisEntity();

			using ( var context = new EntityContext() )
			{
				DBEntity item = context.Entity_CostProfileItem
							.SingleOrDefault( s => s.Id == profileId );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, includingProperties );
				}
				return entity;
			}

		}//
		public bool ValidateProfile( ThisEntity profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;
		
			//if ( profile.CostTypeId == 0 && profile.DirectCostType.HasItems() )
			//	profile.CostTypeId = CodesManager.GetEnumerationSelection( profile.DirectCostType );

			profile.CostTypeId = GetCodeItemId( profile.DirectCostType, ref status );

			//&& string.IsNullOrWhiteSpace( profile.CostTypeOther ) 
			if ( profile.CostTypeId == 0 )
			{
				status.AddError( thisClassName + ".A cost type must be selected " );
			}
			//

			if ( profile.Price < 1)
				status.AddWarning( thisClassName + ". A cost must be entered with a cost item." );

		
			return status.WasSectionValid;
		}
		private int GetCodeItemId( Enumeration entity, ref SaveStatus status )
		{
			int codeId = 0;
			string schemaName = "";
			foreach ( var item in entity.Items )
			{
				if ( !string.IsNullOrWhiteSpace( item.SchemaName ) )
					schemaName = item.SchemaName;
				else if ( !string.IsNullOrWhiteSpace( item.Name ) )
					schemaName = item.Name;

				if ( !string.IsNullOrWhiteSpace( schemaName ) )
				{
					CodeItem code = CodesManager.Codes_PropertyValue_GetBySchema( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST, schemaName );
					if ( code != null && code.Id > 0 )
					{
						return code.Id;
					}
				}
			}

			return codeId;
		}
		public static void MapFromDB( DBEntity from, ThisEntity to, bool includingProperties )
		{
			to.Id = from.Id;
			to.RowId = from.RowId;
			to.CostProfileId = from.CostProfileId;
			to.CostTypeId = from.CostTypeId;

			if ( from.Codes_PropertyValue != null )
			{
				to.CostTypeName = from.Codes_PropertyValue.Title;
				to.CostTypeSchema = from.Codes_PropertyValue.SchemaName;

				to.ProfileName = from.Codes_PropertyValue.Title;

				EnumeratedItem item = new EnumeratedItem();
				item.Id = to.CostTypeId;
				item.Value = to.CostTypeId.ToString();
				item.Selected = true;

				item.Name = to.CostTypeName;
				item.SchemaName = to.CostTypeSchema;
				to.DirectCostType.Items.Add( item );
			}


			//NA 3/17/2017 - Need this to fix null errors in publishing and detail page, but it isn't working: no item is selected, and it's not clear why. 
			//mp 18-11-02 - COPIED this from publisher: no item, because the Fill method looks for data in Entity.Property, and the cost type id is stored on CostProfileItem ==> NOW HANDLED ABOVE

			//to.DirectCostType = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST ); 

			to.Price = from.Price == null ? 0 : ( decimal ) from.Price;
	
			to.PaymentPattern = from.PaymentPattern;

			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;

			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime ) from.LastUpdated;

			//properties
			if ( includingProperties )
			{
				to.AudienceType = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE );

				to.ResidencyType = EntityPropertyManager.FillEnumeration( to.RowId, CodesManager.PROPERTY_CATEGORY_RESIDENCY_TYPE );
			}

		}
		public static void MapToDB( ThisEntity from, DBEntity to )
		{
			to.Id = from.Id;
			to.CostProfileId = from.CostProfileId;
			if ( to.CostTypeId != from.CostTypeId )
			{
				//get the profile name from the code table
				//Models.CodeItem item = CodesManager.Codes_PropertyValue_Get( from.CostTypeId );
				//to.ProfileName = item.Title;
			}
			to.CostTypeId = from.CostTypeId;

			to.Price = from.Price;
			to.PaymentPattern = from.PaymentPattern;
			//TODO  remove description
			to.Description = null;// 			from.Description;

		}

		public static List<ThisEntity> Search( int topParentTypeId, int topParentEntityBaseId, string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();
			ThisEntity item = new ThisEntity();
			CostProfile cp = new CostProfile();
			List<ThisEntity> list = new List<ThisEntity>();
			var result = new DataTable();

			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				if ( string.IsNullOrEmpty( pFilter ) )
				{
					pFilter = "";
				}

				using ( SqlCommand command = new SqlCommand( "[CostProfileItems_search]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@condProfParentEntityTypeId", topParentTypeId ) );
					command.Parameters.Add( new SqlParameter( "@condProfParentEntityBaseId", topParentEntityBaseId ) );
					command.Parameters.Add( new SqlParameter( "@Filter", pFilter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", pOrderBy ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );

					SqlParameter totalRows = new SqlParameter( "@TotalRows", pTotalRows );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					using ( SqlDataAdapter adapter = new SqlDataAdapter() )
					{
						adapter.SelectCommand = command;
						adapter.Fill( result );
					}
					//string rows = command.Parameters[ 4 ].Value.ToString();
					string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();

					try
					{
						pTotalRows = Int32.Parse( rows );
					}
					catch
					{
						pTotalRows = 0;
					}
				}
				//determine if we want to return data as a list of costprofiles or costProfileItems
				//
				
				foreach ( DataRow dr in result.Rows )
				{
					//cp = new CostProfile();
					item = new ThisEntity();
					item.Id = GetRowColumn( dr, "Entity_CostProfileId", 0 );
					//include parent entity type somewhere
					item.ParentEntityType = GetRowColumn( dr, "EntityType", "" );

					item.ProfileName = GetRowColumn( dr, "CostProfileName", "Cost Profile" );

					item.CostTypeName = GetRowColumn( dr, "CostType", "" );
					
					item.Currency = GetRowColumn( dr, "Currency", "" );
					item.CurrencySymbol = GetRowColumn( dr, "CurrencySymbol", "" );
					item.Price = GetRowPossibleColumn( dr, "Price", 0M );
					//
					if ( item.Price <= 0)
						item.CostDescription = GetRowColumn( dr, "CostDescription", "" );
					list.Add( item );
				}

				return list;

			}
		} //
		#endregion
	}
}
