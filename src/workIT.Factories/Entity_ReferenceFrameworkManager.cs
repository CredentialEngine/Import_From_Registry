using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using DBEntity = workIT.Data.Tables.Entity_ReferenceFramework;
using DBParent = workIT.Data.Tables.Reference_Frameworks;

using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;
using ThisEntity = workIT.Models.Common.Entity_ReferenceFramework;

using workIT.Utilities;
//using CM = workIT.Models.Common;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

namespace workIT.Factories
{
	public class Entity_ReferenceFrameworkManager : BaseFactory
	{
		static string thisClassName = "Entity_ReferenceFrameworkManager";
		public Reference_FrameworksManager rfm = new Reference_FrameworksManager();
		public bool SaveList( int parentEntityId, int categoryId, List<CredentialAlignmentObjectProfile> list, ref SaveStatus status )
		{
			if ( list == null || list.Count == 0 )
				return true;

			bool isAllValid = true;
			foreach ( CredentialAlignmentObjectProfile item in list )
			{
				Add( parentEntityId, categoryId, item, ref status );
			}

			return isAllValid;
		}
		public bool NaicsSaveList( int parentEntityId, int categoryId, List<string> list, ref SaveStatus status )
		{
			if ( list == null || list.Count == 0 )
				return true;

			bool isAllValid = true;
			CredentialAlignmentObjectProfile item = new CredentialAlignmentObjectProfile();
			foreach ( string naics in list )
			{
				item = new CredentialAlignmentObjectProfile();
				//look up NAICS
				CodeItem record = CodesManager.Naics_Get( naics );
				if ( record != null && record.Id > 0 )
				{
					item.Id = record.Id;
					item.TargetNodeName = record.Title;
					item.CodedNotation = record.Code;
					item.TargetNode = record.URL;
					Add( parentEntityId, categoryId, item, ref status );
				}
			}

			return isAllValid;
		}
		/// <summary>
		/// Add a Entity framework Item
		/// </summary>
		/// <param name="parentEntityId"></param>
		/// <param name="categoryId"></param>
		/// <param name="entity"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public int Add( int parentEntityId, int categoryId, CredentialAlignmentObjectProfile entity, ref SaveStatus status )
		{

			DBEntity efEntity = new DBEntity();
			using ( var context = new EntityContext() )
			{
				try
				{
					//first ensure not a duplicate (until interface/search prevents dups)
					//EnumeratedItem entity = Get( parentEntityId, categoryId, codeID );
					//if ( entity != null && entity.Id > 0 )
					//{
					//	status.AddWarning( "Warning: the selected code already exists!" );
					//	return 0;
					//}
			
					ReferenceFramework rf = new ReferenceFramework()
					{
						CategoryId = categoryId,
						CodedNotation = entity.CodedNotation,
						Name = entity.TargetNodeName,
						Description = entity.Description,
						TargetNode = entity.TargetNode
					};
					//add or update, returns rfm.Id if OK
					if (!rfm.Save( rf, ref status ))
					{
						return 0;
					}
					//check if a duplicate
					if ( Exists( parentEntityId, rf.Id ) )
					{
						status.AddWarning( string.Format("Warning - Duplicate encountered for categoryId: {0}, entityId: {1}, Name: {2}", categoryId, parentEntityId, entity.TargetNodeName) );
						return 0;
					}
					//save
					efEntity.EntityId = parentEntityId;
					efEntity.CategoryId = categoryId;
					efEntity.ReferenceFrameworkId = rf.Id;
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

						string message = string.Format( thisClassName + ".Add Failed", "Attempted to add a Entity_ReferenceFramework item. The process appeared to not work, but was not an exception, so we have no message, or no clue. parentId: {0}, CategoryId: {1}", parentEntityId, categoryId );
						//?no info on error
						status.AddWarning( "Error - the add was not successful. \r\n" + message );
						//EmailManager.NotifyAdmin( thisClassName + ".ItemAdd Failed", message );
					}
				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), parentId: {0}, CategoryId: {1}", parentEntityId, categoryId ) );
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
							.FirstOrDefault( s => s.EntityId == entityId && s.ReferenceFrameworkId == referenceFrameworkId );

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

	}
}
