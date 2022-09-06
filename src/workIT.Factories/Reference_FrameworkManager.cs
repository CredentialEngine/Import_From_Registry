using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;

using workIT.Utilities;

using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;
using DBEntity = workIT.Data.Tables.Reference_Framework;
using ThisEntity = workIT.Models.Common.ReferenceFramework;


using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;
//
namespace workIT.Factories
{
	public class Reference_FrameworkManager : BaseFactory
	{
		static string thisClassName = "Reference_FrameworkManager";

		#region Persistance ===================

		public bool GetOrAdd( int categoryId, string frameworkName, string framework, ref int frameworkId, ref SaveStatus status )
		{
			if ( DoesItemExist( categoryId, frameworkName, framework, ref frameworkId ) )
			{
				return true;
			}

			ThisEntity entity = new ThisEntity()
			{
				CategoryId = categoryId,
				Name = frameworkName,
				Framework = framework,
			};

			return Save( entity, ref status );
		}
		/// <summary>
		/// Add/Update a Reference_Framework
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Save( ThisEntity entity, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;

			DBEntity efEntity = new DBEntity();
			using ( var context = new EntityContext() )
			{
				if ( ValidateProfile( entity, ref status ) == false )
					return false;

				if ( entity.Id == 0 )
				{
					// - need to check for existance
					DoesItemExist( entity );
				}

				if ( entity.Id == 0 )
				{
					// - Add
					efEntity = new DBEntity();
					MapToDB( entity, efEntity );


					efEntity.Created = DateTime.Now;
					//efEntity.RowId = Guid.NewGuid();

					context.Reference_Framework.Add( efEntity );

					count = context.SaveChanges();

					entity.Id = efEntity.Id;
					//entity.RowId = efEntity.RowId;
					if ( count == 0 )
					{
						status.AddWarning( string.Format( " Unable to add Reference_Framework: {0} ", string.IsNullOrWhiteSpace( entity.Name ) ? "no description" : entity.Name ) );
					}
				}
				else
				{
					efEntity = context.Reference_Framework.SingleOrDefault( s => s.Id == entity.Id );
					if ( efEntity != null && efEntity.Id > 0 )
					{
						//update
						MapToDB( entity, efEntity );
						//has changed?
						if ( HasStateChanged( context ) )
						{
							count = context.SaveChanges();
						}
					}
				}
			}
			return isValid;
		}

		/// <summary>
		/// Delete a record - only if no remaining references!!
		/// MAY NOT expose initially
		/// </summary>
		/// <param name="recordId"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int recordId, ref string statusMessage )
		{
			bool isOK = true;
			using ( var context = new EntityContext() )
			{
				DBEntity p = context.Reference_Framework.FirstOrDefault( s => s.Id == recordId );
				if ( p != null && p.Id > 0 )
				{
					context.Reference_Framework.Remove( p );
					int count = context.SaveChanges();
				}
				else
				{
					statusMessage = string.Format( "The record was not found: {0}", recordId );
					isOK = false;
				}
			}
			return isOK;

		}

		public bool ValidateProfile( ThisEntity profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;

			if ( string.IsNullOrWhiteSpace( profile.Name ) )
			{
				status.AddError( "A framework name must be entered" );
			}


			if ( profile.CategoryId == 0 )
			{
				status.AddError( "A categoryId is required for a reference framework " );
			}
			//if we don't require url, we can't resolve potentially duplicate framework names


			return status.WasSectionValid;
		}

		#endregion
		#region  retrieval ==================
		public void DoesItemExist( ThisEntity entity )
		{
			int frameworkId = 0;
			if ( DoesItemExist( entity.CategoryId, entity.Name, entity.Framework, ref frameworkId ) )
			{
				entity.Id = frameworkId;
			}
		}



		/// <summary>
		/// Look for existing record or add if not found
		/// </summary>
		/// <param name="frameworkUrl"></param>
		/// <param name="frameworkName"></param>
		/// <returns></returns>
		public bool DoesItemExist( int categoryId, string frameworkName, string framework, ref int frameworkId )
		{
			frameworkId = 0;
			if ( categoryId == 0
				|| string.IsNullOrWhiteSpace( frameworkName )
				|| string.IsNullOrWhiteSpace( framework ) ) //is this required? 
				return false;

			frameworkName = frameworkName.ToLower();
			//check for SOC alternatives
			//https://www.onetcenter.org/taxonomy.html
			if (framework.ToLower() == "https://www.onetonline.org" )
            {
				framework = "https://www.onetcenter.org/taxonomy.html";
			}
			using ( var context = new EntityContext() )
			{
				var results = context.Reference_Framework
							.Where( s => s.CategoryId == categoryId
							&& ( s.FrameworkName.ToLower() == frameworkName.ToLower() || s.Framework.ToLower() == framework.ToLower() )
							)
							.OrderBy( p => p.FrameworkName )
							.ToList();
				if ( results != null && results.Count > 0 )
				{
					//should only have one?
					foreach ( var item in results )
					{
						//consider URL?
						if ( !string.IsNullOrWhiteSpace( item.Framework ) )
                        {
							//could have problem with exact matches
							//may want alternate names
                        }
						frameworkId = item.Id;
						break;
					}
				}

				if ( frameworkId > 0 )
					return true;
				else
					return false;
			}
		}//

		public static ThisEntity GetByUrl( string frameworkUrl )
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( frameworkUrl ) )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					DBEntity item = context.Reference_Framework
							.FirstOrDefault( s => s.Framework == frameworkUrl );

					if ( item != null && item.Id > 0 )
					{
						MapFromDB( item, entity );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetByUrl" );
			}
			return entity;
		}//

		/// <summary>
		/// Get a competency record
		/// </summary>
		/// <param name="profileId"></param>
		/// <returns></returns>
		public static ThisEntity Get( int profileId )
		{
			ThisEntity entity = new ThisEntity();
			if ( profileId == 0 )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					DBEntity item = context.Reference_Framework
							.SingleOrDefault( s => s.Id == profileId );

					if ( item != null && item.Id > 0 )
					{
						MapFromDB( item, entity );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Get" );
			}
			return entity;
		}//

		public static Enumeration FillEnumeration( Guid parentUid, int categoryId )
		{
			Enumeration entity = new Enumeration();
			if ( parentUid == null )
				return entity;
			entity = CodesManager.GetEnumeration( categoryId );

			entity.Items = new List<EnumeratedItem>();
			EnumeratedItem item = new EnumeratedItem();
			try
			{
				using ( var context = new ViewContext() )
				{
					List<Views.Entity_ReferenceFramework_Summary> results = context.Entity_ReferenceFramework_Summary
						.Where( s => s.EntityUid == parentUid
							&& s.CategoryId == categoryId )
						.OrderBy( s => s.Name )
						.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( var prop in results )
						{
							item = new EnumeratedItem();
							MapFromDB( prop, item );
							entity.Items.Add( item );
						}
					}

					return entity;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".FillEnumeration" );
				return entity;
			}
		}


		private static void MapFromDB( Views.Entity_ReferenceFramework_Summary from, EnumeratedItem to )
		{
			to.Id = from.Id;
			to.ParentId = ( int ) from.EntityId;
			to.CodeId = from.ReferenceFrameworkItemId;
			to.URL = from.TargetNode;
			to.Value = from.CodedNotation;
			to.Name = from.Name;
			//to.Description = from.Description;
			to.SchemaName = from.SchemaName;
			to.CodeGroup = from.CodeGroup;
			to.CategoryId = from.CategoryId;
			to.ItemSummary = from.Name;
			if ( !string.IsNullOrEmpty( from.CodedNotation ) )
				to.ItemSummary = string.Format( "{0} ({1})", from.Name, from.CodedNotation );

			//to.ItemSummary = from.CodedNotation + " - " + from.Name;
		}


		public static void MapToDB( ThisEntity from, DBEntity to )
		{
			//want to ensure fields from create are not wiped
			//to.Id = from.Id;

			to.FrameworkName = from.Name;
			to.CategoryId = from.CategoryId;
			to.Framework = ( from.Framework ?? "" );

			to.Description = from.Description;

		} //

		public static void MapFromDB( DBEntity from, ThisEntity to )
		{
			to.Id = from.Id;
			//to.RowId = from.RowId;
			to.Name = from.FrameworkName;
			to.CategoryId = from.CategoryId;
			to.Framework = from.Framework;

			to.Description = from.Description;
		}


		#endregion
	}
}
