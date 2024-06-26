using System;
using System.Collections.Generic;
using System.Linq;

using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;

using DBResource = workIT.Data.Tables.Entity_HasResource;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisResourceSummary = workIT.Models.Common.ResourceSummary;
using ViewContext = workIT.Data.Views.workITViews;

namespace workIT.Factories
{
	public class Entity_HasResourceManager : BaseFactory
    {
        static string thisClassName = "Entity_HasResourceManager";
        static string ResourceType = "Entity_HasResource";
        static string RelatedResourceType = "Resource";
        static string ResourceLabel = "Entity_HasResource";

		#region  relationships - keep in sync with [Codes.HasResourceRelationshipType]
        //be careful to not abuse the ideal intent of HasResource
        //The type can be typically thought of as the property name!
        /// <summary>
        /// Default type
        /// </summary>
		public static int HAS_RESOURCE_TYPE_HasResource= 1;
        /// <summary>
        /// Occupation has specialization 
        /// </summary>
		public static int HAS_RESOURCE_TYPE_HasSpecialization = 2;
		/// <summary>
        /// this is an inverse, so should not be storing the 3, rather looking up reverse using 2??
        /// </summary>
		public static int HAS_RESOURCE_TYPE_IsSpecializationOf = 3;
        /// <summary>
        /// Used with PathwayComponent
        /// </summary>
		public static int HAS_RESOURCE_TYPE_HasTargetResource = 4;
        /// <summary>
        /// Competencies
        /// </summary>
        public static int HAS_RESOURCE_TYPE_AbilityEmbodied = 5;
        public static int HAS_RESOURCE_TYPE_KnowledgeEmbodied = 6;
        public static int HAS_RESOURCE_TYPE_SkillEmbodied = 7;
        /// <summary>
        /// Currently where a Task has a child task
        /// </summary>
        public static int HAS_RESOURCE_TYPE_HasChild = 8;
        /// <summary>
        /// Inverse of HasChild, also not clear whether this should be used
        /// </summary>
        public static int HAS_RESOURCE_TYPE_IsChildOf = 9;

        /// <summary>
        /// concept schemes
        /// </summary>
        public static int HAS_RESOURCE_TYPE_PhysicalCapabilityType = 10;
        public static int HAS_RESOURCE_TYPE_PerformanceLevelType = 11;
        public static int HAS_RESOURCE_TYPE_EnvironmentalHazardType = 12;
        public static int HAS_RESOURCE_TYPE_SensoryCapabiltyType = 13;
        /// <summary>
        /// A custom concept scheme
        /// </summary>
        public static int HAS_RESOURCE_TYPE_Classification = 14;
        /// <summary>
        /// A resource is referenced in a transfer value for
        /// </summary>
        public static int HAS_RESOURCE_TYPE_ProvidesTransferValueFor = 15;
		/// <summary>
		/// A resource is referenced in a transfer value from
		/// </summary>
		public static int HAS_RESOURCE_TYPE_ReceivesTransferValueFrom = 16;
        /// <summary>
        /// A TVP references a resource in transfer value from  
        /// </summary>
        public static int HAS_RESOURCE_TYPE_TransferValueFrom = 17;
		/// <summary>
		/// A TVP references a resource in transfer value for  
		/// </summary>
		public static int HAS_RESOURCE_TYPE_TransferValueFor = 18;
        #endregion
        //
        #region Entity Resource Persistance ===================
        /// <summary>
        /// Save a list of hasResource.
        /// Caller is responsble to do a delete all before invoking (could be multiple entity types)
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="entityTypeId"></param>
        /// <param name="list"></param>
        /// <param name="status"></param>
        /// <param name="relationshipTypeId"></param>
        /// <returns></returns>
        public bool SaveList( Entity parent, int entityTypeId, List<int> list, ref SaveStatus status, int relationshipTypeId )
        {
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + ".SaveList. Error - the provided target parent entity was not provided." );
                return false;
            }
            //any value????
            DateTime updateDate = DateTime.Now;
            if ( IsValidDate( status.EnvelopeUpdatedDate ) )
            {
                updateDate = status.LocalUpdatedDate;
            }
			bool isAllValid = true;

			//TODO - USE A REPLACE PATTERN.
			//for now do a delete all ===> must be done in the caller
            
            if ( list == null || list.Count == 0 )
            {
				//23-10-05 - doing delete all in caller now, so skip
				//var currentEntityList = GetAllEntityType( parent, entityTypeId );

				//if ( currentEntityList != null && currentEntityList.Any() )
    //            {
    //                //no input, and existing records, delete all for the type
    //                DeleteAllEntityType( parent, entityTypeId, ref status );
    //            }
                return true;
            }

            foreach ( var item in list )
            {
                Add( parent, entityTypeId, item, relationshipTypeId, ref status );
            }

            return isAllValid;
        }

		public bool SaveList( Entity parent, List<ResourceSummary> list, ref SaveStatus status, int relationshipTypeId )
		{
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( thisClassName + ".SaveList. Error - the provided target parent entity was not provided." );
				return false;
			}
			//
			bool isAllValid = true;

			if ( list == null || list.Count == 0 )
			{
				return true;
			}

			foreach ( var item in list )
			{
				Add( parent, item.EntityTypeId, item.Id, relationshipTypeId, ref status );
			}

			return isAllValid;
		}
		/// <summary>
		/// Add an Resource to a parent (typically a stub was created, so can be associated before completing the full profile)
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="resourceId">The just create lopp</param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public int Add( Entity parent, int entityTypeId,
                    int resourceId, 
                    int relationshipTypeId,
                    ref SaveStatus status,
                    bool warnOnDuplicate = false
            )
        {
            int id = 0;
            int count = 0;
            if ( resourceId == 0 )
            {
                status.AddError( $"A valid {RelatedResourceType} identifier was not provided to the {thisClassName}.Add method." );
                return 0;
            }
            if ( relationshipTypeId == 0 )
                relationshipTypeId = 1;

//            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( "Error - the parent entity was not found." );
                return 0;
            }

            using ( var context = new EntityContext() )
            {
                DBResource efEntity = new DBResource();
                try
                {
                    //first check for duplicates
                    //including relationship?
                    efEntity = context.Entity_HasResource
                            .FirstOrDefault( s => s.EntityId == parent.Id 
                                        && s.EntityTypeId == entityTypeId
                                        && s.ResourceId == resourceId 
										&& s.RelationshipTypeId == relationshipTypeId
										);

                    if ( efEntity != null && efEntity.Id > 0 )
                    {
                        if ( warnOnDuplicate )
                        {
                            status.AddWarning( $"This {RelatedResourceType} has already been added to this profile {thisClassName}." );
                        }
                        id = efEntity.Id;
                        return id;
                    }

                    efEntity = new DBResource
                    {
                        EntityId = parent.Id,
                        EntityTypeId = entityTypeId,
                        ResourceId = resourceId,
						RelationshipTypeId = relationshipTypeId,
						Created = System.DateTime.Now
                    };

                    context.Entity_HasResource.Add( efEntity );

                    // submit the change to database
                    count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        id = efEntity.Id;
                        return efEntity.Id;
                    }
                    else
                    {
                        //?no info on error

                        string message = $"{thisClassName}.Add Failed. Attempted to add a {RelatedResourceType} for a connection profile. The process appeared to not work, but there was no exception, so we have no message, or no clue. Parent Profile: {parent.Id}, {ResourceType}.Id: {resourceId}";
                        status.AddError( thisClassName + ". Error - the add was not successful. " + message );
                        EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
                    }
                }
                catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
                {
                    string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", $"{ResourceType}" );
                    status.AddError( thisClassName + ". Error - the Add was not successful. " + message );
                    LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );

                }
                catch ( Exception ex )
                {
                    string message = FormatExceptions( ex );
                    status.AddError( thisClassName + "Error - the Add was not successful. " + message );
                    LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );
                }


            }
            return id;
        }

        public bool DeleteAllEntityType( Entity parent, int entityTypeId, ref SaveStatus status )
        {
            bool isValid = true;
            int count = 0;
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + ".DeleteAllEntityType. Error - the provided target parent entity was not provided." );
                return false;
            }
            if ( entityTypeId == 0 )
            {
                status.AddError( thisClassName + $".DeleteAllEntityType. Error - the entity type Id was not provided for entity: {parent.EntityType}/{parent.EntityBaseName} ({parent.EntityBaseId})." );
                return false;
            }
            using ( var context = new EntityContext() )
            {
                //check if target is a reference object and is only in use here
                var results = context.Entity_HasResource
                            .Where( s => s.EntityId == parent.Id && s.EntityTypeId == entityTypeId )
                            .OrderBy( s => s.EntityTypeId ).ThenBy( s => s.ResourceId )
                            .ToList();
                if ( results == null || results.Count == 0 )
                {
                    return true;
                }

                foreach ( var item in results )
                {
                    context.Entity_HasResource.Remove( item );
                    count = context.SaveChanges();
                    if ( count > 0 )
                    {

                    }
                }
            }

            return isValid;
        }


        /// <summary>
        /// Delete all relationships for parent
        /// NOTE: there should be a check for reference entities, and delete if no other references.
        /// OR: have a clean up process to delete orphans. 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool DeleteAll( Entity parent, ref SaveStatus status )
        {
            bool isValid = true;
            int count = 0;
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + ". Error - the provided target parent entity was not provided." );
                return false;
            }
            using ( var context = new EntityContext() )
            {
                //check if target is a reference object and is only in use here
                var results = context.Entity_HasResource
                            .Where( s => s.EntityId == parent.Id )
                            .OrderBy( s => s.EntityTypeId ).ThenBy( s => s.ResourceId )
                            .ToList();
                if ( results == null || results.Count == 0 )
                {
                    return true;
                }

                foreach ( var item in results )
                {
                    //may be better to use the view?
                        //do a getall. If only one, delete it.
                        var exists = context.Entity_HasResource
                            .Where( s => s.ResourceId == item.ResourceId )
                            .ToList();
                        if ( exists != null && exists.Count() == 1 )
                        {
                            var statusMsg = string.Empty;
                            //this method will also add pending request to remove from elastic.
                            //20-12-18 mp - Only done for a reference lopp but what about a full lopp that may now be an orphan? We are not allowing lopps without parent, but will still exist in registry!!!
                            //actually this delete will probably also delete the Entity_HasResource
                            //new ResourceManager().Delete( item.ResourceId, ref statusMsg );
                            //continue;
                        }
              
                    context.Entity_HasResource.Remove( item );
                    count = context.SaveChanges();
                    if ( count > 0 )
                    {

                    }
                }
            }

            return isValid;
        }
		#endregion
		public static List<ThisResourceSummary> GetAll( Guid parentUID )
		{
            Entity parent = EntityManager.GetEntity( parentUID );
			if ( parent == null || parent.Id == 0 )
			{
				return null;
			}
			return GetAllForEntityType( parent, 0 );
		}

		public static List<ThisResourceSummary> GetAll( int entityTypeId, int baseRecordId )
		{
			Entity parent = EntityManager.GetEntity( entityTypeId, baseRecordId );
			if ( parent == null || parent.Id == 0 )
			{
				return null;
			}
			return GetAllForEntityType( parent, 0 );
		}
		/// <summary>
		/// Can be more efficient to just get all of the related records and then split up in the caller
		/// </summary>
		/// <param name="parent">Entity related to calling source</param>
		/// <returns></returns>
		public static List<ThisResourceSummary> GetAll( Entity parent )
        {

            if ( parent == null || parent.Id == 0 )
            {
                return null;
            }
            return GetAllForEntityType( parent, 0 );
        }


		/// <summary>
		/// Get all Resources for the parent
		/// Uses the parent Guid to retrieve the related Entity, then uses the EntityId to retrieve the child objects.
        /// NOTE: this may be preferred, then the caller can filter by relationshipTypeId as needed.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="relationshipTypeId">Not sure if will use this</param>
		/// <returns></returns>
		public static List<ThisResourceSummary> GetAllForEntityType( Entity parent, int entityTypeId )
        {
            List<ThisResourceSummary> list = new List<ThisResourceSummary>();
            ThisResourceSummary entity = new ThisResourceSummary();

            //Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                return list;
            }

            LoggingHelper.DoTrace( 7, $"{thisClassName}.GetAllEntityType: parent:{parent.EntityBaseName}, parentId:{parent.EntityBaseId}, e.EntityTypeId:{parent.EntityTypeId}" );
            try
            {
                using ( var context = new ViewContext() )
                {
                    var results = context.Entity_HasResourceSummary
                            .Where( s => s.EntityId == parent.Id 
                                && ( entityTypeId== 0 || s.EntityTypeId == entityTypeId ) 
                               // && (relationshipTypeId == 0 || s.RelationshipTypeId == relationshipTypeId )
                                )
                            .OrderBy( s => s.Name )
                            .ToList();

                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( var item in results )
                        {
                            entity = new ThisResourceSummary()
                            {
                                Id = item.ResourceId ??item.Id,
                                EntityTypeId = (int)item.EntityTypeId,
                                Name = item.Name,
                                Description = item.Description,
                                CTID = item.CTID,
                                URI = item.SubjectWebpage,
                                Type = item.EntityType,
                                RelationshipTypeId=item.RelationshipTypeId, //for embodies
								//Organization = item.Organization
								ResourcePrimaryOrgId = item.ResourceOwningOrgId ?? 0,
								ResourcePrimaryOrganizationName = item.ResourceOrganizationName
							};                            
                               
                            list.Add( entity );
                        }
                    }
                    return list;
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, $"{thisClassName}.GetAllEntityType: parent:{parent.EntityBaseName}, parentId:{parent.EntityBaseId}, e.EntityTypeId:{parent.EntityTypeId}" );
            }
            return list;
        }

        /// <summary>
        /// Get 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="entityTypeId"></param>
        /// <param name="relationshipTypeId"></param>
        /// <returns></returns>
		public static List<ThisResourceSummary> GetAllForEntityType( Entity parent, int entityTypeId, int relationshipTypeId )
		{
			List<ThisResourceSummary> list = new List<ThisResourceSummary>();
			ThisResourceSummary entity = new ThisResourceSummary();

			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}

			try
			{
				using ( var context = new ViewContext() )
				{
					var results = context.Entity_HasResourceSummary
							.Where( s => s.EntityId == parent.Id
								&& ( entityTypeId == 0 || s.EntityTypeId == entityTypeId )
								 && (relationshipTypeId == 0 || s.RelationshipTypeId == relationshipTypeId )
								)
							.OrderBy( s => s.Name )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( var item in results )
						{
							entity = new ThisResourceSummary()
							{
								Id = item.ResourceId ?? item.Id,
								EntityTypeId = ( int ) item.EntityTypeId,
								Name = item.Name,
								Description = item.Description,
								CTID = item.CTID,
								URI = item.SubjectWebpage,
								Type = item.EntityType,
								RelationshipTypeId = item.RelationshipTypeId ,
                                ResourcePrimaryOrgId = item.ResourceOwningOrgId ?? 0,
                                ResourcePrimaryOrganizationName = item.ResourceOrganizationName
							};

							list.Add( entity );
						}
					}
					return list;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, $"{thisClassName}.GetAllEntityType: parent:{parent.EntityBaseName}, parentId:{parent.EntityBaseId}, e.EntityTypeId:{parent.EntityTypeId}, relationshipTypeId: {relationshipTypeId} " );
			}
			return list;
		}

		/// <summary>
		/// Get all of the HasResource records for a parent.
		/// Actually this is very specific, just for where a transfer value is the object of an Entity.HasResource entry.
		/// </summary>
		/// <param name="resourceId"></param>
		/// <returns></returns>
		public static List<ThisResourceSummary> GetParentsForTVPResourceId(  int resourceId )
        {
            List<ThisResourceSummary> list = new List<ThisResourceSummary>();
            ThisResourceSummary entity = new ThisResourceSummary();
            //Entity parent = EntityManager.GetEntity( parentUid );
            if ( resourceId == 0 )
            {
                return list;
            }

            try
            {
                using ( var context = new ViewContext() )
                {
                    var results = context.Entity_HasResourceSummary
                             .Where( s => s.ResourceId == resourceId &&
                                     ( s.RelationshipTypeId == HAS_RESOURCE_TYPE_ProvidesTransferValueFor ||
                                        s.RelationshipTypeId == HAS_RESOURCE_TYPE_ReceivesTransferValueFrom ) )
                             .OrderBy( s => s.Name )
                             .ToList();

                    foreach ( var item in results )
                    {
                        entity = new ThisResourceSummary()
                        {
                            EntityTypeId = ( int ) item.ParentEntityTypeId,
                            Name = item.ParentName,
                            Description = item.ParentDescription,
                            CTID = item.ParentCTID,
                            RelationshipTypeId = item.RelationshipTypeId ,
							ResourcePrimaryOrgId = item.ResourceOwningOrgId ?? 0,
							ResourcePrimaryOrganizationName = item.ResourceOrganizationName
						};

                        list.Add( entity );
                    }
                    return list;
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, $"{thisClassName}.GetByResourceId: resource:{resourceId}" );
            }
            return list;
        }

        public static List<ThisResourceSummary> GetParentsForResourceId( int resourceId, int entityTypeId )
        {
            List<ThisResourceSummary> list = new List<ThisResourceSummary>();
            ThisResourceSummary entity = new ThisResourceSummary();
            //Entity parent = EntityManager.GetEntity( parentUid );
            if ( resourceId == 0 )
            {
                return list;
            }

            try
            {
                using ( var context = new ViewContext() )
                {
                    var results = context.Entity_HasResourceSummary
                             .Where( s => s.ResourceId == resourceId && s.RelationshipTypeId== Entity_HasResourceManager.HAS_RESOURCE_TYPE_HasResource && s.EntityTypeId==entityTypeId )
                             .OrderBy( s => s.Name )
                             .ToList();

                    foreach ( var item in results )
                    {
                        entity = new ThisResourceSummary()
                        {
                            EntityTypeId = ( int ) item.ParentEntityTypeId,
                            Name = item.ParentName,
                            Description = item.ParentDescription,
                            CTID = item.ParentCTID,
                            RelationshipTypeId = item.RelationshipTypeId,
                            ResourcePrimaryOrgId = item.ResourceOwningOrgId ?? 0,
                            ResourcePrimaryOrganizationName = item.ResourceOrganizationName
                        };

                        list.Add( entity );
                    }
                    return list;
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, $"{thisClassName}.GetByResourceId: resource:{resourceId}" );
            }
            return list;
        }
        //This is to get related entities for Embodies,Classification used for Competency Framework, Competency and Concept Scheme
        public static List<ThisResourceSummary> GetRelatedResource( string ctid, int relatedEntityTypeId )
        {
            List<ThisResourceSummary> list = new List<ThisResourceSummary>();
            ThisResourceSummary entity = new ThisResourceSummary();
            var results = new List<Data.Views.Entity_HasResourceSummary>();
            if ( !IsValidCtid(ctid) )
            {
                return list;
            }
            var entityCache = EntityManager.EntityCacheGetByCTID( ctid );
            if(entityCache != null )
			{
                try
                {
                    using ( var context = new ViewContext() )
                    {
                        if ( entityCache.EntityTypeId== CodesManager.ENTITY_TYPE_COMPETENCY_FRAMEWORK|| entityCache.EntityTypeId==CodesManager.ENTITY_TYPE_COLLECTION|| entityCache.EntityTypeId == CodesManager.ENTITY_TYPE_CONCEPT_SCHEME )
						{
                            results = context.Entity_HasResourceSummary
                                                                .Where( s => s.EntityParentUid == entityCache.EntityUid && s.ParentEntityTypeId == relatedEntityTypeId )
                                                                .GroupBy( s => s.ParentEntityId )
                                                                .Select( g => g.FirstOrDefault() )
                                                                .OrderBy( s => s.Name )
                                                                .ToList();
                        }else if(entityCache.EntityTypeId==CodesManager.ENTITY_TYPE_COLLECTION_COMPETENCY|| entityCache.EntityTypeId==CodesManager.ENTITY_TYPE_COMPETENCY||entityCache.EntityTypeId==CodesManager.ENTITY_TYPE_CONCEPT){
                            results = context.Entity_HasResourceSummary
                                                                .Where( s => s.EntityUid == entityCache.EntityUid && s.ParentEntityTypeId == relatedEntityTypeId )
                                                                .GroupBy( s => s.ParentEntityId )
                                                                .Select( g => g.FirstOrDefault() )
                                                                .OrderBy( s => s.Name )
                                                                .ToList();

						}
						else
						{
                            LoggingHelper.LogError( $"Invalid Type + {thisClassName}.GetByResourceId: resource:{ctid}" );
                        }

						if ( results.Count > 0 )
						{
                            foreach ( var item in results )
                            {
                                entity = new ThisResourceSummary()
                                {
                                    EntityTypeId = ( int ) item.ParentEntityTypeId,
                                    Name = item.ParentName,
                                    Description = item.ParentDescription,
                                    CTID = item.ParentCTID,
                                    RelationshipTypeId = item.RelationshipTypeId,
                                    ResourcePrimaryOrgId = item.ResourceOwningOrgId ?? 0,
                                    ResourcePrimaryOrganizationName = item.ResourceOrganizationName
                                };

                                list.Add( entity );
                            }
                        }
                        return list;
                    }
                }
                catch ( Exception ex )
                {
                    LoggingHelper.LogError( ex, $"{thisClassName}.GetByResourceId: resource:{ctid}" );
                }
            }
			else
			{
                LoggingHelper.LogError( $"This CTID does not exist in the EntityCache {thisClassName}.GetByResourceId: resource:{ctid}" );
            }

            return list;
        }

    }
}
