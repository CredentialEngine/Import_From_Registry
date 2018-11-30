using System;
using System.Collections.Generic;
using System.Linq;

using workIT.Models;
using workIT.Models.Common;
using DBEntity = workIT.Data.Tables.Entity_FrameworkItem;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using workIT.Utilities;

using Views = workIT.Data.Views;
namespace workIT.Factories
{
	/// <summary>
	/// Will need a major change to handle these items:
	/// - use coded notation, not FK to integer
	///		- for 'other' values, the latter must be nullable
	/// - may want to denormalize, and store title, and description
	/// - the latter will easy enable handling other values
	/// - 
	/// </summary>
	public class Entity_FrameworkItemManager : BaseFactory
	{
		static string thisClassName = "Entity_FrameworkItemManager";


		public bool SaveList( int parentEntityId, int categoryId, List<CredentialAlignmentObjectProfile> list, ref SaveStatus status )
		{

			if ( list == null || list.Count == 0 )
				return true;

            Entity_ReferenceFrameworkManager erfm = new Entity_ReferenceFrameworkManager();
            return erfm.SaveList( parentEntityId, categoryId, list, ref status );
        }

		/// <summary>
		/// Add a Entity framework Item
		/// </summary>
		/// <param name="parentEntityId"></param>
		/// <param name="categoryId"></param>
		/// <param name="entity"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		//public int Add( int parentEntityId, int categoryId, CredentialAlignmentObjectProfile entity, ref SaveStatus status )
		//{

		//	DBEntity efEntity = new DBEntity();
		//	using ( var context = new EntityContext() )
		//	{
		//		try
		//		{
		//			//first ensure not a duplicate (until interface/search prevents dups)
		//			//EnumeratedItem entity = Get( parentEntityId, categoryId, codeID );
		//			//if ( entity != null && entity.Id > 0 )
		//			//{
		//			//	status.AddWarning( "Warning: the selected code already exists!" );
		//			//	return 0;
		//			//}
		//			efEntity.EntityId = parentEntityId;
		//			efEntity.CategoryId = categoryId;
		//			//efEntity.CodeId = codeID;
		//			efEntity.Created = System.DateTime.Now;

		//			if ( entity.EducationFrameworkId > 0 )
		//				efEntity.ExternalFrameworkId = entity.EducationFrameworkId;
		//			else
		//				efEntity.ExternalFrameworkId = null;

		//			//???IsOtherFramework


		//			//efEntity.FrameworkName = entity.FrameworkName;
		//			//efEntity.FrameworkUrl = entity.FrameworkUrl;

		//			efEntity.TargetNode = entity.TargetNode;
		//			efEntity.Name = entity.TargetNodeName;
		//			efEntity.CodedNotation = entity.CodedNotation;
		//			if (!string.IsNullOrWhiteSpace( entity.CodedNotation ) && entity.CodedNotation.Length > 2)
		//			{
		//				int groupId = ExtractCodeGroup( entity.CodedNotation );
		//			}
		//			context.Entity_FrameworkItem.Add( efEntity );

		//			// submit the change to database
		//			int count = context.SaveChanges();
		//			if ( count > 0 )
		//			{
		//				return efEntity.Id;
		//			}
		//			else
		//			{

		//				string message = string.Format( thisClassName + ".Add Failed", "Attempted to add a credential framework item. The process appeared to not work, but was not an exception, so we have no message, or no clue. parentId: {0}, CategoryId: {1}", parentEntityId, categoryId );
		//				//?no info on error
		//				status.AddWarning( "Error - the add was not successful. \r\n" + message );
		//				//EmailManager.NotifyAdmin( thisClassName + ".ItemAdd Failed", message );
		//			}
		//		}
		//		catch ( Exception ex )
		//		{
		//			string message = FormatExceptions( ex );
		//			LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), parentId: {0}, CategoryId: {1}", parentEntityId, categoryId ) );
		//			status.AddError( thisClassName + ".Add() - Error - the save was not successful. \r\n" + message );
		//		}
		//	}

		//	return efEntity.Id;
		//}
		//private int ExtractCodeGroup(string code)
		//{
		//	int group = 0;

		//	return group;
		//}
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
            using ( var context = new EntityContext() )
            {
                context.Entity_FrameworkItem.RemoveRange( context.Entity_FrameworkItem.Where( s => s.EntityId == parent.Id ) );
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

        #region Entity property read ===================
  //      public static Enumeration FillEnumeration( Guid parentUid, int categoryId )
		//{
		//	Enumeration entity = new Enumeration();
		//	entity = CodesManager.GetEnumeration( categoryId );

		//	entity.Items = new List<EnumeratedItem>();
		//	EnumeratedItem item = new EnumeratedItem();

		//	/*
		//	 * CANNOT USE Entity_FrameworkItemSummary !!!!!!!!!
		//	 * 
		//	 */
		//	using ( var context = new ViewContext() )
		//	{
		//		List<Views.Entity_FrameworkItemSummary> results = context.Entity_FrameworkItemSummary
		//			.Where( s => s.EntityUid == parentUid
		//				&& s.CategoryId == categoryId )
		//			.OrderBy( s => s.FrameworkCode )
		//			.ToList();

		//		if ( results != null && results.Count > 0 )
		//		{
		//			foreach ( var prop in results )
		//			{
		//				item = new EnumeratedItem();
		//				MapFromDB( prop, item );
		//				entity.Items.Add( item );
		//			}
		//		}
				
		//		return entity;
		//	}
		//}
		
	
		//private static void MapFromDB( Views.Entity_FrameworkItemSummary from, EnumeratedItem to )
		//{
		//	to.Id = from.Id;
		//	to.ParentId = ( int ) from.ParentId;
		//	to.CodeId = from.CodeId;
		//	to.URL = from.URL;
		//	to.Value = from.FrameworkCode;
		//	to.Name = from.Title;
		//	to.Description = from.Description;
		//	to.SchemaName = from.SchemaName;
  //          if (!string.IsNullOrWhiteSpace( to.Value ))
  //              to.ItemSummary = to.Name + " (" + to.Value + ")";
  //          else
  //              to.ItemSummary = to.Name;

		//}
		#endregion
	}
}
