using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using DBEntity = workIT.Data.Tables.Entity_ReferenceFramework;

using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;
using ThisEntity = workIT.Models.Common.Entity_ReferenceFramework;

using workIT.Utilities;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;
using System.Data.Entity.Infrastructure;

namespace workIT.Factories
{
	public class Entity_ReferenceFrameworkManager : BaseFactory
	{
		static string thisClassName = "Entity_ReferenceFrameworkManager";
		public bool SaveList( int parentEntityId, int categoryId, List<CredentialAlignmentObjectProfile> list, ref SaveStatus status )
		{
			if ( list == null || list.Count == 0 )
				return true;

			bool isAllValid = true;
			//existing should have been deleted before calling, so just do Add
			foreach ( var item in list )
			{
				int newId = Add( parentEntityId, categoryId, item, ref status, false );
			}

			return isAllValid;
		}
		public bool NaicsSaveList( int parentEntityId, int categoryId, List<string> list, ref SaveStatus status )
		{
			if ( list == null || list.Count == 0 )
				return true;

			bool isAllValid = true;
			CredentialAlignmentObjectProfile item = new CredentialAlignmentObjectProfile();
			//get naics framework - why
			var naicsFramework = Reference_FrameworkManager.GetByName( Reference_FrameworkManager.NAICS_Framework );

			foreach ( string naics in list )
			{
				item = new CredentialAlignmentObjectProfile();
				//look up NAICS
				CodeItem record = CodesManager.Naics_Get( naics );
				if ( record != null && record.Id > 0 )
				{
					item.Id = record.Id;
					item.Framework = record.URL.Substring( 0, record.URL.IndexOf( "?" ) );
					item.FrameworkName = naicsFramework.Name;
					item.TargetNodeName = record.Title;
					item.CodedNotation = record.Code;
					item.TargetNode = record.URL;
					Add( parentEntityId, categoryId, item, ref status, false );
				}
			}

			return isAllValid;
		}

		//public bool OnetSaveList( int parentEntityId, int categoryId, List<string> list, ref SaveStatus status )
		//{
		//	if ( list == null || list.Count == 0 )
		//		return true;

		//	bool isAllValid = true;
		//	CredentialAlignmentObjectProfile item = new CredentialAlignmentObjectProfile();
		//	foreach ( string code in list )
		//	{
		//		item = new CredentialAlignmentObjectProfile();
		//		//look up SOC
		//		var records = CodesManager.SOC_Get( code );
		//		foreach ( var record in records )
		//		{
		//			if ( record != null && record.Id > 0 )
		//			{
		//				item.Id = record.Id;
		//				item.TargetNodeName = record.Title;
		//				item.Description = record.Description;
		//				item.CodedNotation = record.Code;
		//				item.TargetNode = record.URL;
		//				Add( parentEntityId, categoryId, item, ref status, false );
		//			}
		//		}
		//	}

		//	return isAllValid;
		//}

		//public bool CIPSaveList( int parentEntityId, int categoryId, List<string> list, ref SaveStatus status )
		//{
		//	if ( list == null || list.Count == 0 )
		//		return true;

		//	bool isAllValid = true;
		//	CredentialAlignmentObjectProfile item = new CredentialAlignmentObjectProfile();
		//	foreach ( string code in list )
		//	{
		//		item = new CredentialAlignmentObjectProfile();
		//		//look up SOC
		//		var records = CodesManager.SOC_Get( code );
		//		foreach ( var record in records )
		//		{
		//			if ( record != null && record.Id > 0 )
		//			{
		//				item.Id = record.Id;
		//				item.TargetNodeName = record.Title;
		//				item.Description = record.Description;
		//				item.CodedNotation = record.Code;
		//				item.TargetNode = record.URL;
		//				Add( parentEntityId, categoryId, item, ref status, false );
		//			}
		//		}
		//	}

		//	return isAllValid;
		//}

		/// <summary>
		/// Add a Entity framework Item
		/// </summary>
		/// <param name="parentEntityId"></param>
		/// <param name="categoryId"></param>
		/// <param name="entity"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		private int Add( int parentEntityId, int categoryId, CredentialAlignmentObjectProfile entity, ref SaveStatus status, bool warningOnDuplicates )
		{
			var rfim = new Reference_FrameworkItemManager();

			DBEntity efEntity = new DBEntity();
			using ( var context = new EntityContext() )
			{
				try
				{
					//first ensure not a duplicate (until interface/search prevents dups)
					//the decision was made to delete all and do adds. So check should not be necessary now. 
					//EnumeratedItem entity = Get( parentEntityId, categoryId, codeID );
					//if ( entity != null && entity.Id > 0 )
					//{
					//	status.AddWarning( "Warning: the selected code already exists!" );
					//	return 0;
					//}
			
					var rfi = new ReferenceFrameworkItem()
					{
						CategoryId = categoryId,
						CodedNotation = entity.CodedNotation,
						Name = entity.TargetNodeName,
						Description = entity.Description,
						TargetNode = entity.TargetNode,
						Framework = entity.Framework,
						FrameworkName = entity.FrameworkName,
					};
					//add or update, returns rfm.Id if OK
					if (!rfim.Save( rfi, ref status ))
					{
						return 0;
					}
					//check if a duplicate
					if ( Exists( parentEntityId, rfi.Id ) )
					{
						if (warningOnDuplicates)
							status.AddWarning( string.Format( "Warning - Duplicate encountered for categoryId: {0}, entityId: {1}, Name: {2}, FrameworkReferenceItemId: {3}", categoryId, parentEntityId, entity.TargetNodeName, rfi.Id) );
						return 0;
					}
					//save
					efEntity.EntityId = parentEntityId;
					efEntity.CategoryId = categoryId;
					efEntity.ReferenceFrameworkItemId = rfi.Id;
					efEntity.Created = System.DateTime.Now;
					context.Entity_ReferenceFramework.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						return efEntity.Id;
					}
					else
					{
						string message = string.Format( thisClassName + ".Add Failed", "Attempted to add a Entity_ReferenceFramework item. The process appeared to not work, but was not an exception, so we have no message, or no clue. parentId: {0}, CategoryId: {1}, Name: {2}, ReferenceId: {3}, CodedNotation: {4}", parentEntityId, categoryId, entity.TargetNodeName, rfi.Id, rfi.CodedNotation );
						//?no info on error
						status.AddWarning( thisClassName + "Error - the add was not successful. \r\n" + message );
						//EmailManager.NotifyAdmin( thisClassName + ".ItemAdd Failed", message );
					}
				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), parentId: {0}, CategoryId: {1}, Name: {2}, CodedNotation: {3}", parentEntityId, categoryId, entity.TargetNodeName, entity.CodedNotation ) );
					status.AddError( thisClassName + ".Add() - Error - the save was not successful. \r\n" + message );
				}
			}

			return efEntity.Id;
		}
		public static bool Exists( int entityId, int referenceFrameworkId )
		{
			ThisEntity entity = new ThisEntity();

			try
			{
				using ( var context = new EntityContext() )
				{
					DBEntity item = context.Entity_ReferenceFramework
							.FirstOrDefault( s => s.EntityId == entityId && s.ReferenceFrameworkItemId == referenceFrameworkId );

					if ( item != null && item.Id > 0 )
					{
						return true;
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Exists" );
			}
			return false;
		}//
		public static List<ThisEntity> GetAll( Guid parentUid,  bool getMinimumOnly = false )
		{
			ThisEntity entity = new ThisEntity();
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

					List<DBEntity> results = context.Entity_ReferenceFramework
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.CategoryId )
							.ThenBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new ThisEntity();
							//TBD
							//MapFromDB( item, entity );
							list.Add( entity );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAll" );
			}
			return list;
		}//
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
				var results = context.Entity_ReferenceFramework
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.CategoryId )
							.ThenBy( s => s.Created )
							.ToList();
				if ( results == null || !results.Any() )
					return true;

				try
				{
					context.Entity_ReferenceFramework.RemoveRange( results );
					//context.Entity_ReferenceFramework.RemoveRange( context.Entity_ReferenceFramework.Where( s => s.EntityId == parent.Id ) );
					//attempt to address DbUpdateConcurrencyException
					context.SaveChanges();
				}
				catch ( Exception cex )
				{
					isValid = false;

					// Update the values of the entity that failed to save from the store
					//don't want this
					//cex.Entries.Single().Reload();
					//OR
					// Update original values from the database
					//Don't think this is what we want either.
					//var entry = cex.Entries.Single();
					//entry.OriginalValues.SetValues( entry.GetDatabaseValues() );

					var msg = BaseFactory.FormatExceptions( cex );
					LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. ParentType: {0}, baseId: {1}, DbUpdateConcurrencyException: {2}", parent.EntityType, parent.EntityBaseId, msg ) );
				}

            }

            return isValid;
        }
    }
}
