using System;
using System.Collections.Generic;
using System.Linq;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
//using workIT.Models.Helpers.Cass;

using workIT.Utilities;

using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;
using DBEntity = workIT.Data.Tables.Entity_Competency;
using ThisEntity = workIT.Models.ProfileModels.Entity_Competency;
using Views = workIT.Data.Views;

using EM = workIT.Data.Tables;

namespace workIT.Factories
{
	public class Entity_CompetencyManager : BaseFactory
	{
		static string thisClassName = "Entity_CompetencyManager";
		#region Persistance ===================
		public bool SaveList( List<ThisEntity> list, Guid parentUid, ref SaveStatus status )
		{
			if ( list == null || list.Count == 0 )
				return true;

			bool isAllValid = true;
			foreach ( ThisEntity item in list )
			{
				Save( item, parentUid, ref status );
			}

			return isAllValid;
		}

		public bool SaveList( List<CredentialAlignmentObjectProfile> list, Guid parentUid, ref SaveStatus status )
		{
			if ( list == null || list.Count == 0 )
				return true;
			ThisEntity entity = new ThisEntity();
			bool isAllValid = true;
			foreach ( CredentialAlignmentObjectProfile item in list )
			{
				entity = new ThisEntity();
				MapToAlignmentObject( item, entity );
				Save( entity, parentUid, ref status );
			}

			return isAllValid;
		}

		/// <summary>
		/// Add a competency
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="parentUid"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Save( ThisEntity entity,
				Guid parentUid,
				ref SaveStatus status )
		{
			bool isValid = true;

			if ( !IsValidGuid( parentUid ) )
			{
				status.AddWarning( "Error: the parent identifier was not provided." );
				return false;
			}

			int count = 0;

			DBEntity efEntity = new DBEntity();

			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( "Error - the parent entity was not found." );
				return false;
			}

			using ( var context = new EntityContext() )
			{
				if ( ValidateProfile( entity, ref status ) == false )
				{
					return false;
				}

				if ( entity.Id == 0 )
				{
					//check if already exists
					//TODO - will need to add alignment type
					ThisEntity item = Get( parent.Id, entity.TargetNodeName );
					if ( entity != null && entity.Id > 0 )
					{
						status.AddWarning( thisClassName + string.Format( "Save(). Error: the selected competency {0} already exists!", entity.TargetNodeName ) );
						return false;
					}
					//add
					efEntity = new DBEntity();
					efEntity.EntityId = parent.Id;

					if ( entity.EducationFrameworkId > 0 )
						efEntity.EducationFrameworkId = entity.EducationFrameworkId;
					else
						efEntity.EducationFrameworkId = null;

					efEntity.FrameworkName = entity.FrameworkName;
					efEntity.FrameworkUrl = entity.FrameworkUrl;

					efEntity.TargetNodeName = entity.TargetNodeName;
					efEntity.TargetNodeDescription = entity.TargetNodeDescription;
					efEntity.TargetNode = entity.TargetNode;
					efEntity.CodedNotation = entity.CodedNotation;
					efEntity.Weight = entity.Weight;

					efEntity.Created = DateTime.Now;

					context.Entity_Competency.Add( efEntity );
					count = context.SaveChanges();

					//update profile record so doesn't get deleted
					entity.Id = efEntity.Id;

					if ( count == 0 )
					{
						status.AddWarning( string.Format( " Unable to add Competency: {0} <br\\> ", string.IsNullOrWhiteSpace( entity.TargetNodeName) ? "no description" : entity.TargetNodeName ) );
					}

				}
				else
				{
					//no update possible at this time
					entity.EntityId = parent.Id;

					efEntity = context.Entity_Competency.SingleOrDefault( s => s.Id == entity.Id );
					if ( efEntity != null && efEntity.Id > 0 )
					{
						//update
						if ( entity.EducationFrameworkId > 0 )
							efEntity.EducationFrameworkId = entity.EducationFrameworkId;
						else
							efEntity.EducationFrameworkId = null;

						efEntity.FrameworkName = entity.FrameworkName;
						efEntity.FrameworkUrl = entity.FrameworkUrl;


						efEntity.TargetNodeName = entity.TargetNodeName;
						efEntity.TargetNodeDescription = entity.TargetNodeDescription;
						efEntity.TargetNode = entity.TargetNode;
						efEntity.CodedNotation = entity.CodedNotation;
						efEntity.Weight = entity.Weight;

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
		/// Delete a competency
		/// </summary>
		/// <param name="recordId"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int recordId, ref string statusMessage )
		{
			bool isOK = true;
			using ( var context = new EntityContext() )
			{
				DBEntity p = context.Entity_Competency.FirstOrDefault( s => s.Id == recordId );
				if ( p != null && p.Id > 0 )
				{
					context.Entity_Competency.Remove( p );
					int count = context.SaveChanges();
				}
				else
				{
					statusMessage = string.Format( "Entity Competency record was not found: {0}", recordId );
					isOK = false;
				}
			}
			return isOK;
		}


		public bool ValidateProfile( ThisEntity profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;

			if ( profile.EducationFrameworkId < 1 )
			{
				//status.AddError( thisClassName + ".ValidateProfile(). A competency identifier must be included." );
			}

			if ( string.IsNullOrWhiteSpace( profile.TargetNodeName))
			{
				status.AddError( thisClassName + ".ValidateProfile(). A competency TargetNodeName must be included." );
			}
			return !status.HasSectionErrors;
		}

		#endregion
		#region  retrieval ==================

		/// <summary>
		/// Get all records for the parent
		/// Uses the parent Guid to retrieve the related ThisEntity, then uses the EntityId to retrieve the child objects.
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="alignmentType">If blank, get all types</param>
		public static List<ThisEntity> GetAll( Guid parentUid, string alignmentType )
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
				using ( var context = new ViewContext() )
				{
					List<Views.EntityCompetencyFramework_Items_Summary> results = context.EntityCompetencyFramework_Items_Summary
							.Where( s => s.EntityId == parent.Id
							)
							.OrderBy( s => s.FrameworkName )
							.ThenBy( s => s.Competency)
							.ToList();
					
					if ( results != null && results.Count > 0 )
					{
						foreach ( var item in results )
						{
							entity = new ThisEntity();
							MapFromDB( item, entity );
							list.Add( entity );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAll( Guid parentUid, string alignmentType )" );
			}
			return list;
		}//
		public static void MapFromDB( Views.EntityCompetencyFramework_Items_Summary from, ThisEntity to )
		{
			to.Id = from.EntityCompetencyId;
			to.EntityId = from.EntityId;

			to.EducationFrameworkId = from.EntityCompetencyFrameworkItemId;
			to.TargetNodeName = from.Competency;
			to.TargetNodeDescription = from.TargetNodeDescription;
			to.TargetNode = from.TargetNode;
			to.CodedNotation = from.CodedNotation;
			//to.Weight = from.Weight ?? 0;

			//to.AlignmentTypeId = from.AlignmentTypeId ?? 0;
			//to.AlignmentType = from.AlignmentType;

			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;

			to.FrameworkName = from.FrameworkName ?? "None";
			to.FrameworkUrl = from.FrameworkUrl ?? "";

		}       //

		/// <summary>
		/// May be workaround, may be permanent, getting combined
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="alignmentType"></param>
		/// <returns></returns>
		public static List<CredentialAlignmentObjectProfile> GetAllAs_CredentialAlignmentObjectProfile( Guid parentUid )
		{
			CredentialAlignmentObjectProfile entity = new CredentialAlignmentObjectProfile();
			List<CredentialAlignmentObjectProfile> list = new List<CredentialAlignmentObjectProfile>();

			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}
			try
			{
				using ( var context = new ViewContext() )
				{
					/*
					List<DBEntity> results = context.Entity_Competency
							.Where( s => s.EntityId == parent.Id
							)
							.OrderBy( s => s.EducationFramework )
							.ThenBy( s => s.TargetNodeName )
							.ToList();
					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new CredentialAlignmentObjectProfile();
							MapFromDB( item, entity );
							list.Add( entity );
						}
					}
					*/
					List<Views.EntityCompetencyFramework_Items_Summary> results = context.EntityCompetencyFramework_Items_Summary
							.Where( s => s.EntityId == parent.Id
							)
							.OrderBy( s => s.FrameworkName )
							.ThenBy( s => s.Competency )
							.ToList();
					if ( results != null && results.Count > 0 )
					{
						foreach ( var item in results )
						{
							entity = new CredentialAlignmentObjectProfile();
							MapFromDB( item, entity );
							list.Add( entity );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAllAs_CredentialAlignmentObjectProfile" );
			}
			return list;
		}//

		public static void MapFromDB( Views.EntityCompetencyFramework_Items_Summary from, CredentialAlignmentObjectProfile to )
		{
			to.Id = from.EntityCompetencyId;
			to.ParentId = from.EntityId;

			to.FrameworkName = from.FrameworkName;
			//add url?? to Entity for now?
			to.FrameworkUrl = from.FrameworkUrl;
			//todo - latter has value, lookup frameworkId

			//to.EducationFrameworkId = from.EducationFrameworkId ?? 0;

			to.TargetNode = from.TargetNode;
			to.TargetNodeDescription = from.TargetNodeDescription;
			to.TargetNodeName = from.Competency;
			to.Weight = ( from.Weight ?? 0M );
			to.CodedNotation = from.CodedNotation;

			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;

		}

		/// <summary>
		/// Need to fake this out, until enlightenment occurs
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returns>
		public static List<CredentialAlignmentObjectFrameworkProfile> GetAllAs_CAOFramework( Guid parentUid )
		{
			CredentialAlignmentObjectFrameworkProfile entity = new CredentialAlignmentObjectFrameworkProfile();
			List<CredentialAlignmentObjectFrameworkProfile> list = new List<CredentialAlignmentObjectFrameworkProfile>();

			CredentialAlignmentObjectItem caoItem = new CredentialAlignmentObjectItem();
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_Competency
							.Where( s => s.EntityId == parent.Id
							)
							.OrderBy( s => s.FrameworkName )
							.ThenBy( s => s.TargetNodeName )
							.ToList();
					if ( results != null && results.Count > 0 )
					{
						string prevName = "";
						foreach ( DBEntity item in results )
						{
							if (prevName != item.FrameworkName)
							{
								if (!string.IsNullOrWhiteSpace( prevName ) )
									list.Add( entity );

								entity = new CredentialAlignmentObjectFrameworkProfile();
								entity.FrameworkName = item.FrameworkName;
								entity.FrameworkUrl = item.FrameworkUrl;
								entity.ParentId = item.EducationFrameworkId ?? 0;
								prevName = item.FrameworkName;
							}
							caoItem = new CredentialAlignmentObjectItem();
							MapFromDB( item, caoItem );
							entity.Items.Add( caoItem );
						}

						if ( !string.IsNullOrWhiteSpace( prevName ) )
							list.Add( entity );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAllAs_CAOFramework" );
			}
			return list;
		}//

		//public static List<CredentialAlignmentObjectItem> GetAllAsAlignmentObjects( Guid parentUid, string alignmentType )
		//{
		//	//ThisEntity entity = new ThisEntity();
		//	List<CredentialAlignmentObjectItem> list = new List<CredentialAlignmentObjectItem>();
		//	CredentialAlignmentObjectItem entity = new CredentialAlignmentObjectItem();

		//	Entity parent = EntityManager.GetEntity( parentUid );
		//	if ( parent == null || parent.Id == 0 )
		//	{
		//		return list;
		//	}
		//	try
		//	{
		//		using ( var context = new EntityContext() )
		//		{
		//			List<DBEntity> results = context.Entity_Competency
		//					.Where( s => s.EntityId == parent.Id
		//					)
		//					.OrderBy( s => s.EducationFramework_Competency.EducationFramework.Name )
		//					.ThenBy( s => s.EducationFramework_Competency.Name )
		//					.ToList();
		//			//&& ( alignmentType == "" || s.AlignmentType == alignmentType ) 
		//			if ( results != null && results.Count > 0 )
		//			{
		//				foreach ( DBEntity item in results )
		//				{
		//					entity = new CredentialAlignmentObjectItem();
		//					ToMapAsAlignmentObjects( item, entity );
		//					list.Add( entity );
		//				}
		//			}
		//		}
		//	}
		//	catch ( Exception ex )
		//	{
		//		LoggingHelper.LogError( ex, thisClassName + ".GetAllAsAlignmentObjects" );
		//	}
		//	return list;
		//}//

		/// <summary>
		/// Get a competency record
		/// </summary>
		/// <param name="profileId"></param>
		/// <returns></returns>
		//public static ThisEntity Get( int profileId )
		//{
		//	ThisEntity entity = new ThisEntity();
		//	if ( profileId == 0 )
		//		return entity;
		//	try
		//	{
		//		using ( var context = new EntityContext() )
		//		{
		//			DBEntity item = context.Entity_Competency
		//					.SingleOrDefault( s => s.Id == profileId );

		//			if ( item != null && item.Id > 0 )
		//			{
		//				MapFromDB( item, entity );
		//			}
		//		}
		//	}
		//	catch ( Exception ex )
		//	{
		//		LoggingHelper.LogError( ex, thisClassName + ".Get" );
		//	}
		//	return entity;
		//}//

		/// <summary>
		/// Get entity to determine if one exists for the entity and alignment type
		/// </summary>
		/// <param name="entityId"></param>
		/// <param name="targetNodeName"></param>
		/// <returns></returns>
		public static ThisEntity Get( int entityId, string targetNodeName )
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace(targetNodeName ))
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					DBEntity item = context.Entity_Competency
							.FirstOrDefault( s => s.EntityId == entityId 
							&& s.TargetNodeName == targetNodeName );

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

		public static void MapFromDB( DBEntity from, ThisEntity to )
		{
			to.Id = from.Id;
			to.EntityId = from.EntityId;

			to.EducationFrameworkId = from.EducationFrameworkId ?? 0;
			to.TargetNodeName = from.TargetNodeName;
			to.TargetNodeDescription = from.TargetNodeDescription;
			to.TargetNode = from.TargetNode;
			to.CodedNotation = from.CodedNotation;
			to.Weight = from.Weight ?? 0;

			//to.AlignmentTypeId = from.AlignmentTypeId ?? 0;
			//to.AlignmentType = from.AlignmentType;

			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;

			if ( from.EducationFramework != null && from.EducationFramework.FrameworkName != null )
				to.FrameworkName = from.EducationFramework.FrameworkName;

		}       //
		
		public static void MapFromDB( DBEntity from, CredentialAlignmentObjectItem to )
		{
			to.Id = from.Id;
			to.ParentId = from.EntityId;

			

			to.TargetNode = from.TargetNode;
			to.TargetNodeDescription = from.TargetNodeDescription;
			to.TargetNodeName = from.TargetNodeName;
			to.Weight = ( from.Weight ?? 0M );
			to.CodedNotation = from.CodedNotation;

			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;

		}
		public static void MapToAlignmentObject( CredentialAlignmentObjectProfile from, ThisEntity to )
		{

			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;
			to.FrameworkName = from.FrameworkName;
			//add url?? to Entity for now?
			to.FrameworkUrl = from.FrameworkUrl;
			//todo - latter has value, lookup frameworkId
			to.EducationFrameworkId = new EducationFrameworkManager().Lookup_OR_Add( from.FrameworkUrl, from.FrameworkName );

			to.TargetNode = from.TargetNode;
			to.TargetNodeDescription = from.TargetNodeDescription;
			to.TargetNodeName = from.TargetNodeName;
			//to.Weight = GetDecimalField(from.Weight);
			to.Weight = from.Weight;
			to.CodedNotation = from.CodedNotation;
		}

		#endregion

	}
}
