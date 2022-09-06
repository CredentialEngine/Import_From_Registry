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
using DBEntity = workIT.Data.Tables.Reference_FrameworkItem;
using ThisEntity = workIT.Models.Common.ReferenceFrameworkItem;
using ThisEntityItem = workIT.Models.Common.Entity_ReferenceFramework;


using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;
//
namespace workIT.Factories
{
	public class Reference_FrameworkItemManager : BaseFactory
	{
		static string thisClassName = "Reference_FrameworkItemManager";
		
		#region Persistance ===================

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

				//ReferenceFrameworkItemId should not be present, may want to QA it if present?
				if ( entity.ReferenceFrameworkId == 0 )
				{
					int frameworkId = 0;
					if ( !string.IsNullOrWhiteSpace( entity.FrameworkName ) && !string.IsNullOrWhiteSpace( entity.Framework ) )
					{
						if ( new Reference_FrameworkManager().GetOrAdd( entity.CategoryId, entity.FrameworkName, entity.Framework, ref frameworkId, ref status ) )
						{
							entity.ReferenceFrameworkId = frameworkId;
						}
					}
				}

				if ( entity.Id == 0 )
				{
					// - need to check for existance. May want to resolve framework first
					DoesItemExist( entity );
				}

				
				//TBD
				//if ( entity.ReferenceFrameworkId > 0 && entity.FrameworkName == "Illinois Career Information" )
    //            {
				//	if (entity.CodedNotation?.Length > 5)
    //                {
				//		//actually check if ProPath is sending both - YES, so never mind for now
    //                }
    //            }
				if ( entity.Id == 0 )
				{
					// - Add
					efEntity = new DBEntity();
					MapToDB( entity, efEntity );


					efEntity.Created = DateTime.Now;
					//efEntity.RowId = Guid.NewGuid();

					context.Reference_FrameworkItem.Add( efEntity );

					count = context.SaveChanges();

					entity.Id = efEntity.Id;
					//entity.RowId = efEntity.RowId;
					if ( count == 0 )
					{
						status.AddWarning( string.Format( " Unable to add Profile: {0} <br\\> ", string.IsNullOrWhiteSpace( entity.Name ) ? "no description" : entity.Name ) );
					}
				}
				else
				{
					efEntity = context.Reference_FrameworkItem.SingleOrDefault( s => s.Id == entity.Id );
					if ( efEntity != null && efEntity.Id > 0 )
					{
						//entity.RowId = efEntity.RowId;
						//update - generally can't have an update!
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
				DBEntity p = context.Reference_FrameworkItem.FirstOrDefault( s => s.Id == recordId );
				if ( p != null && p.Id > 0 )
				{
					context.Reference_FrameworkItem.Remove( p );
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
				status.AddError( "A reference framework item name must be entered" );
			}


			if ( profile.CategoryId == 0 )
			{
				status.AddError( "A categoryId is required for a reference framework item " );
			}
			//if we don't require url, we can't resolve potentially duplicate framework names


			return status.WasSectionValid;
		}

		#endregion
		#region  retrieval ==================
		/// <summary>
		/// Check if a matching ReferenceFrameworkId exists
		/// </summary>
		/// <param name="entity"></param>
		/// <returns></returns>
		public bool DoesItemExist( ThisEntity entity )
		{
			//int existingId = 0;
			//if ( DoesItemExist( entity.CategoryId, entity.CodedNotation, entity.Name, ref existingId ) )
			//{
			//	entity.Id = existingId;
			//}



			//TODO - need to check if just name as well
			if ( entity.CategoryId == 0
				|| string.IsNullOrWhiteSpace( entity.Name ) )
				return false;

			var codedNotation = !string.IsNullOrWhiteSpace( entity.CodedNotation ) ? entity.CodedNotation.ToLower() : "";
			var name = entity.Name.ToLower();
			//22-05-20 - now frameworkId will be present if found
			var frameworkName = string.IsNullOrWhiteSpace( entity.FrameworkName ) ? "" : entity.FrameworkName.ToLower();
			
			using ( var context = new EntityContext() )
			{
				var query = context.Reference_FrameworkItem
							.Where( s => s.CategoryId == entity.CategoryId 
							&& ( s.Name.ToLower() == name.ToLower() )
							);
				if ( !string.IsNullOrWhiteSpace( entity.CodedNotation ) )
					query = query.Where( s => s.CodedNotation == codedNotation );
				else
                {
					//need to NOT return something with a code
					query = query.Where( s => s.CodedNotation == null);
				}
				if ( entity.ReferenceFrameworkId > 0 )
					query = query.Where( s => s.ReferenceFrameworkId == entity.ReferenceFrameworkId );
				else //compare against NO framework
					query = query.Where( s => s.ReferenceFrameworkId == null );

				//if ( !string.IsNullOrWhiteSpace( entity.FrameworkName ) )
				//	query = query.Where( s => s.Reference_Framework.FrameworkName.ToLower() == frameworkName );
				//else //compare against NO framework
				//	query = query.Where( s => s.ReferenceFrameworkId == null );

				var results = query.OrderBy( s => s.Name ).ThenBy( s => s.ReferenceFrameworkId).ToList();

				if ( results != null && results.Count > 0 )
				{
					//should only have one?
					foreach ( var item in query )
					{
						//may not be necessary to check if the query is correct
						if ( codedNotation == (item.CodedNotation ?? "") )
						{
							entity.Id = item.Id;
						}
						else
						{//
							entity.Id = item.Id;
						}
						//there will be a lot of existing duplicates, so probably should break
						break;
					}
				}
			}

			if ( entity.Id > 0 )
				return true;
			else
				return false;
		}

		/// <summary>
		/// Look for existing record or add if not found
		/// </summary>
		/// <param name="categoryId"></param>
		/// <param name="codedNotation"></param>
		/// <param name="name"></param>
		/// <param name="frameworkName"></param>
		/// <returns>existingId</returns>
		public bool DoesItemExist( int categoryId, string codedNotation, string name, string frameworkName, ref int existingId )
		{
			existingId = 0;
			codedNotation = !string.IsNullOrWhiteSpace( codedNotation ) ? codedNotation : "";
			if ( categoryId == 0
				|| string.IsNullOrWhiteSpace( codedNotation )
				|| string.IsNullOrWhiteSpace( name ) )
				return false;

			name = name.ToLower();
			frameworkName = string.IsNullOrWhiteSpace(frameworkName) ? "" : frameworkName.ToLower();

			if ( string.IsNullOrWhiteSpace( codedNotation ) )
			{
				//want to check for just the name as well 
				//may still be a framework without codedNotation?
				using ( var context = new EntityContext() )
				{
					var results = context.Reference_FrameworkItem
								.Where( s => s.CategoryId == categoryId
								&& ( s.Name.ToLower() == name.ToLower() )
								&& ( frameworkName== "" || 
									( s.Reference_Framework != null && s.Reference_Framework.FrameworkName == frameworkName )  )
								)
								.OrderBy( p => p.Name )
								.ToList();
					if ( results != null && results.Count > 0 )
					{
						//should only have one?
						foreach ( var item in results )
						{
							existingId = item.Id;
							break;
						}
					}

					if ( existingId > 0 )
						return true;
					else
						return false;
				}
			}
			else
			{
				using ( var context = new EntityContext() )
				{
					var results = context.Reference_FrameworkItem
								.Where( s => s.CategoryId == categoryId
								&& ( s.Name.ToLower() == name.ToLower() )
								&& ( s.CodedNotation == codedNotation )
								)
								.OrderBy( p => p.Name )
								.ToList();
					if ( results != null && results.Count > 0 )
					{
						//should only have one?
						foreach ( var item in results )
						{
							existingId = item.Id;
							break;
						}
					}

					if ( existingId > 0 )
						return true;
					else
						return false;
				}
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
					DBEntity item = context.Reference_FrameworkItem
							.FirstOrDefault( s => s.TargetNode == frameworkUrl );

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
					DBEntity item = context.Reference_FrameworkItem
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
			}catch(Exception ex)
			{
				LoggingHelper.LogError( ex, thisClassName + ".FillEnumeration" );
				return entity;
			}
		}

		public static List<CredentialAlignmentObjectProfile> FillCredentialAlignmentObject( Guid parentUid, int categoryId )
		{
			var output = new List<CredentialAlignmentObjectProfile>();
			var entity = new CredentialAlignmentObjectProfile();
			if ( parentUid == null )
				return output;

			try
			{
				using ( var context = new EntityContext() )
				{
					var results = context.Entity_ReferenceFramework
						.Where( s => s.Entity.EntityUid == parentUid
							&& s.CategoryId == categoryId )
						.OrderBy( s => s.Reference_FrameworkItem.Name )
						.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( var item in results )
						{
							if ( item.Reference_FrameworkItem != null )
							{
								entity = new CredentialAlignmentObjectProfile();
								MapFromDB( item.Reference_FrameworkItem, entity );
								output.Add( entity );
							}
						}
					}

					return output;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".FillCredentialAlignmentObject" );
				return output;
			}
		}

		private static void MapFromDB( Views.Entity_ReferenceFramework_Summary from, EnumeratedItem to )
		{			
			to.Id = from.Id;
			to.ParentId = ( int ) from.EntityId;
			to.CodeId = from.ReferenceFrameworkItemId;
			to.URL = from.TargetNode;
			to.Value = from.CodedNotation;
			to.Name = to.ItemSummary = from.Name;
			//to.Description = from.Description;
			to.SchemaName = from.SchemaName;
			to.CodeGroup = from.CodeGroup;
			to.CategoryId = from.CategoryId;

            if ( !string.IsNullOrEmpty( from.CodedNotation ) )
                to.ItemSummary = string.Format( "{0} ({1})", from.Name, from.CodedNotation );

            //to.ItemSummary = from.CodedNotation + " - " + from.Name;
		}

		public static void MapFromDB( DBEntity from, ThisEntity to )
		{
			to.Id = from.Id;
			//to.RowId = from.RowId;
			to.Name = from.Name;
			to.CategoryId = from.CategoryId;
			to.CodedNotation = from.CodedNotation;
			to.CodeGroup = from.CodeGroup;
			
			to.Description = from.Description;
			to.TargetNode = from.TargetNode;
			to.ReferenceFrameworkId = from.ReferenceFrameworkId??0;
		}


		public static void MapFromDB( DBEntity from, CredentialAlignmentObjectProfile to )
		{
			to.Id = from.Id;
			to.CategoryId = from.CategoryId;
			to.TargetNodeName = to.ItemSummary = from.Name;
			to.TargetNode = from.TargetNode;
			to.CodedNotation = from.CodedNotation;
			to.Description = from.Description;
			if (from.Reference_Framework?.Framework != null)
            {
				to.FrameworkName = from.Reference_Framework?.FrameworkName;
				to.Framework = from.Reference_Framework?.Framework;
			}
			if ( !string.IsNullOrEmpty( from.CodedNotation ) )
				to.ItemSummary = string.Format( "{0} ({1})", from.Name, from.CodedNotation );
		}

		public static void MapToDB( ThisEntity from, DBEntity to )
		{
			//want to ensure fields from create are not wiped
			//to.Id = from.Id;

			to.Name = from.Name;
			to.CategoryId = from.CategoryId;
			to.CodedNotation = ( from.CodedNotation ?? "" );

			if ( !string.IsNullOrWhiteSpace( from.CodedNotation ) && from.CodedNotation.Length > 1 )
			{
				to.CodeGroup = from.CodedNotation.Substring( 0, 2 );
				if ( !int.TryParse( to.CodeGroup, out int familyId ) )
				{
					to.CodeGroup = "";
				}
			}
			to.Description = from.Description;
			to.TargetNode = from.TargetNode ?? "";
			if ( from.ReferenceFrameworkId > 0 )
				to.ReferenceFrameworkId = from.ReferenceFrameworkId;
			else
				to.ReferenceFrameworkId = null;

		} //

		#endregion
	}
}
