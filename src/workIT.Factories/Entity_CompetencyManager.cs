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
using ThisResource = workIT.Models.ProfileModels.Entity_Competency;
using Views = workIT.Data.Views;

using EM = workIT.Data.Tables;

namespace workIT.Factories
{
	public class Entity_CompetencyManager : BaseFactory
	{
		static string thisClassName = "Entity_CompetencyManager";
		#region Persistance ===================
		//public bool SaveList( List<ThisResource> list, Guid parentUid, ref SaveStatus status )
		//{
		//	if ( list == null || list.Count == 0 )
		//		return true;

		//	bool isAllValid = true;
		//	foreach ( ThisResource item in list )
		//	{
		//		Save( item, parentUid, ref status );
		//	}

		//	return isAllValid;
		//}

		//public bool SaveList( List<CredentialAlignmentObjectProfile> list, Guid parentUid, ref SaveStatus status )
		//{
		//	return SaveList("whatDefault", list, parentUid, ref status );
		//}
		public bool SaveList( string alignmentType, List<CredentialAlignmentObjectProfile> list, Guid parentUid, ref SaveStatus status )
		{
            if ( !IsValidGuid( parentUid ) )
            {
                status.AddError( string.Format( "A valid parent identifier was not provided to the {0}.Add method.", thisClassName ) );
                return false;
            }

            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( $"Error - the parent entity was not found ({parentUid})." );
                return false;
            }
			//consider how to avoid this step
			//23-01-06 mp - now more of an issue where an lopp could have both assesses and teaches. So have to delete by alignment
			//				OR the delete needs to be done from the caller!
            //DeleteAll( parent, ref status );

            if ( list == null || list.Count == 0 )
				return true;
			ThisResource entity = new ThisResource();
			bool isAllValid = true;

			//Note the list could include multiple frameworks
			// OR COLLECTIONS**************
			foreach ( CredentialAlignmentObjectProfile item in list )
			{
				entity = new ThisResource();
				MapToAlignmentObject( item, entity, alignmentType );
				
				Save( entity, alignmentType, parent, ref status );
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
		public bool Save( ThisResource entity, string alignmentType, Entity parent, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;

			DBEntity efEntity = new DBEntity();

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
					//23-01-08 mp - also cannot just rely on targetNodeName (can an lopp assess and teach the same competency?). Need to include framework
					ThisResource item = Get( parent.Id, alignmentType, entity.TargetNodeName );
					if ( entity != null && entity.Id > 0 )
					{
						//status.AddWarning( thisClassName + string.Format( "Save(). Error: the selected competency {0} already exists!", entity.TargetNodeName ) );
						return true;
					}
					//add
					efEntity = new DBEntity();
					efEntity.EntityId = parent.Id;

					if ( entity.CompetencyFrameworkId > 0 )
						efEntity.CompetencyFrameworkId = entity.CompetencyFrameworkId;
					else
						efEntity.CompetencyFrameworkId = null;
					if ( entity.CollectionId > 0 )
						efEntity.CollectionId = entity.CollectionId;
					else
						efEntity.CollectionId = null;
					//
					efEntity.FrameworkName = entity.FrameworkName;
					efEntity.FrameworkUrl = entity.Framework;
					efEntity.Alignment = entity.Alignment;
					efEntity.TargetNodeName = entity.TargetNodeName;
					efEntity.TargetNodeDescription = entity.TargetNodeDescription;
					efEntity.TargetNode = entity.TargetNode;
					if (!string.IsNullOrWhiteSpace(entity.TargetNodeCTID))
                    {
						efEntity.TargetNodeCTID = entity.TargetNodeCTID;
					}
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

					efEntity = context.Entity_Competency.FirstOrDefault( s => s.Id == entity.Id );
					if ( efEntity != null && efEntity.Id > 0 )
					{
						//update
						if ( entity.CompetencyFrameworkId > 0 )
							efEntity.CompetencyFrameworkId = entity.CompetencyFrameworkId;
						else
							efEntity.CompetencyFrameworkId = null;
						if ( entity.CollectionId > 0 )
							efEntity.CollectionId = entity.CollectionId;
						else
							efEntity.CollectionId = null;

						efEntity.FrameworkName = entity.FrameworkName;
						efEntity.FrameworkUrl = entity.Framework;
						//could check if matches previous alignment
						efEntity.Alignment = entity.Alignment;

						efEntity.TargetNodeName = entity.TargetNodeName;
						efEntity.TargetNodeDescription = entity.TargetNodeDescription;
						efEntity.TargetNode = entity.TargetNode;
						if ( !string.IsNullOrWhiteSpace( entity.TargetNodeCTID ) )
						{
							efEntity.TargetNodeCTID = entity.TargetNodeCTID;
						}
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
					var results = context.Entity_Competency.Where( s => s.EntityId == parent.Id )
					.ToList();
					if ( results == null || results.Count == 0 )
						return true;

					context.Entity_Competency.RemoveRange( context.Entity_Competency.Where( s => s.EntityId == parent.Id ) );
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
			}catch (Exception ex)
			{
				LoggingHelper.LogError( ex, thisClassName + ".DeleteAll", false );
			}
            return isValid;
        }
        public bool ValidateProfile( ThisResource profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;

			if ( profile.CompetencyFrameworkId < 1 )
			{
				//status.AddError( thisClassName + ".ValidateProfile(). A competency identifier must be included." );
			}

			if ( string.IsNullOrWhiteSpace( profile.TargetNodeName))
			{
				status.AddError( thisClassName + ".ValidateProfile(). A competency TargetNodeName must be included." );
			}
			return status.WasSectionValid;
		}

        #endregion
        #region  retrieval ==================


        /// <summary>
        /// Get all records for the parent,
		/// TODO - should this also use an alignmentType parameter?
		///		 - this method is called by a search based on tags method
        /// </summary>
        /// <param name="entityTypeId"></param>
        /// <param name="entityBaseId"></param>
        /// <param name="maxRecords"></param>
        /// <returns></returns>
        public static List<CredentialAlignmentObjectItem> GetAll( int entityTypeId, int entityBaseId, int maxRecords = 0)
		{
			CredentialAlignmentObjectItem entity = new CredentialAlignmentObjectItem();
			List<CredentialAlignmentObjectItem> list = new List<CredentialAlignmentObjectItem>();
			if ( maxRecords == 0 )
				maxRecords = 10000;

			//Entity parent = EntityManager.GetEntity( parentUid );
			//if ( parent == null || parent.Id == 0 )
			//{
			//	return list;
			//}
			try
			{
				//TODO - can't use EntityCompetencyFramework_Items_Summary unless handles collections
				using ( var context = new ViewContext() )
				{
					List<Views.EntityCompetencyFramework_Items_Summary> results = context.EntityCompetencyFramework_Items_Summary
							.Where( s => s.EntityTypeId == entityTypeId 
									  && s.EntityBaseId == entityBaseId
							)
							.OrderBy( s => s.FrameworkName )
							.ThenBy( s => s.Competency )
							.Take(maxRecords)
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( var item in results )
						{
							entity = new CredentialAlignmentObjectItem();
							MapFromDB( item, entity );
							list.Add( entity );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + string.Format(".GetAll( entityTypeId: {0}, entityBaseId: {1} )", entityTypeId, entityBaseId) );
			}
			return list;
		}//
		public static void MapFromDB( Views.EntityCompetencyFramework_Items_Summary from, CredentialAlignmentObjectItem to )
		{
			to.Id = from.EntityCompetencyId;
			//to.EntityId = from.EntityId;
			
			//to.CompetencyFrameworkId = from.EntityCompetencyFrameworkItemId;
			to.TargetNodeName = from.Competency;
			to.TargetNodeDescription = from.TargetNodeDescription;
			if ( to.TargetNodeDescription == to.TargetNodeName )
				to.TargetNodeDescription = "";

			to.AlignmentType = from.Alignment;
			to.TargetNode = from.TargetNode;
			to.TargetNodeCTID = from.TargetNodeCTID;
			to.CodedNotation = from.CodedNotation;
			//to.Weight = from.Weight ?? 0;
			//not applicable here
			to.ConnectionTypeId = from.ConnectionTypeId;

			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime )from.Created;
			//added these as used by Competencies search, determine if needed!
			//the source is determined by the search type
			to.SourceParentId = (int)from.EntityBaseId;
			to.SourceEntityTypeId = from.EntityTypeId;

			to.FrameworkName = from.FrameworkName ?? "None";
			if ( !string.IsNullOrWhiteSpace( from.FrameworkUri ) )
				to.Framework = from.FrameworkUri ?? "";
			else
				to.Framework = from.SourceUrl ?? "";

		}       //

		
		/// <summary>
		/// Need to fake this out, until enlightenment occurs
		/// re: alignment type. The calling context will handle for condition profile and asmt
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returns>
		public static List<CredentialAlignmentObjectFrameworkProfile> GetAllAs_CAOFramework( Guid parentUid, string alignmentType, ref Dictionary<string, RegistryImport> frameworksList)
		{
			/*
			 * ConditionProfile
			 *	=> cp Entity using parentUid = Entity.EntityUID
			 *		= > Entity.Competency using ec.EntityId = cpEntity.Id
			 */
			//21-07-13 - entity is initialized here and assigned values thru the loop
			CredentialAlignmentObjectFrameworkProfile entity = new CredentialAlignmentObjectFrameworkProfile()
			{
				FrameworkName = "",
				Framework = "",
                IsAnonymousFramework = true,	//valid default?
                Items = new List<CredentialAlignmentObjectItem>()
			};
			List<CredentialAlignmentObjectFrameworkProfile> list = new List<CredentialAlignmentObjectFrameworkProfile>();
			//var frameworksList = new Dictionary<string, RegistryImport>();
			string viewerUrl = ""; //UtilityManager.GetAppKeyValue( "cassResourceViewerUrl" );
			//obsolete, copied from publisher
			bool hidingFrameworksNotPublished = UtilityManager.GetAppKeyValue( "hideFrameworksNotPublished", false );
			CredentialAlignmentObjectItem caoItem = new CredentialAlignmentObjectItem();
			//get Entity for conditionProfile (RowId -> entity.EntityUID
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
							.Where( s => s.EntityId == parent.Id && (s.Alignment == alignmentType || s.Alignment == null) )
							.OrderBy( s => s.FrameworkName )
							.ThenBy( s => s.TargetNodeName )
							.ToList();
					if ( results != null && results.Count > 0 )
					{
						string prevName = "";
						//this will currently include blank frameworks
						var frameworks = results.Select( s => s.FrameworkName ).Distinct().ToList();
						var sortedList = results.OrderByDescending(s => s.FrameworkName).ThenBy(s => s.Created).ToList();
						//var hasFrameworks = results.Select( s => s.FrameworkName.Length > 0 ).Distinct().ToList().Count();

						var hasFrameworkName = results.Select( s => s.FrameworkName ).Where( s => s.Length > 0 ).Distinct().ToList().Count();
						int cntr = 0;
						foreach ( DBEntity item in sortedList )
						{
							cntr++;
							//TODO - handle where no framework found
							//		- also a mix of with and without
							//if ( cntr == 1 &&( hasFrameworkName == 0 || string.IsNullOrWhiteSpace( item.FrameworkName ) ) )
							//{
							//	//here we are assuming all comps are in the same framework
							//	entity.IsAnonymousFramework = true;
							//}
							if ( (prevName != item.FrameworkName) )
                            {
								if ( cntr > 1 && (!string.IsNullOrWhiteSpace( prevName ) || entity.IsAnonymousFramework ))
								{
									//actually try handling in detail page - not working

									if ( !entity.IsARegistryFrameworkUrl
										|| !hidingFrameworksNotPublished
										|| ( entity.IsARegistryFrameworkUrl && entity.ExistsInRegistry )
										)
									{
										if (!entity.IsDeleted)
											list.Add( entity );
									}
								}

								entity = new CredentialAlignmentObjectFrameworkProfile();
								//default, and then override as needed
								entity.Framework = item.FrameworkUrl ?? "";
								entity.FrameworkName = item.FrameworkName;
                                if ( string.IsNullOrWhiteSpace( item.FrameworkName ) ) 
                                {
                                    entity.IsAnonymousFramework = true;
                                }
                                if ( item.CompetencyFramework != null && item.CompetencyFramework.Id > 0 )
                                {									
                                    entity.FrameworkName = item.CompetencyFramework.Name;
									entity.FrameworkCtid = item.CompetencyFramework.CTID;
									//SourceUrl is for non registry content
									//OHHHHH - some of these can now be from a collection!!!!!!!!!!!!!!
									if (!string.IsNullOrWhiteSpace( item.CompetencyFramework.FrameworkUri ) )
										entity.Framework = item.CompetencyFramework.FrameworkUri;
									else 
										entity.Framework = item.CompetencyFramework.SourceUrl;

									if ( entity.IsARegistryFrameworkUrl     //21-07-13 IsARegistryFrameworkUrl is not assigned anywhere, derived from FrameworkUri
										&& ( item.CompetencyFramework.ExistsInRegistry ?? false )
										)
									{
										entity.ExistsInRegistry = true;
										if ( item.CompetencyFramework.EntityStateId == 0 )
										{
											//need to skip - need more than this. A non-registry framework will not exist in registry
											entity.IsDeleted = true;
										}
									}
									
									//18-12-13 mp - can only use viewer if is a cer URL - although this may change for other sources
									if ( !string.IsNullOrWhiteSpace( viewerUrl ) && entity.IsARegistryFrameworkUrl )
									{
										//entity.CaSSViewerUrl = string.Format( viewerUrl, UtilityManager.GenerateMD5String( entity.Framework ) );
									}
                                    //if ( item.FrameworkUrl.ToLower().IndexOf("credentialengineregistry.org/resources/ce-") == -1 )
                                    //    entity.SourceUrl = item.CompetencyFramework.SourceUrl;
                                    //else
                                    //    entity.SourceUrl = item.CompetencyFramework.SourceUrl ?? "";

                                    if ( !string.IsNullOrWhiteSpace(item.CompetencyFramework.CTID) )
                                    {
										//SLOW!!! - added index
										//
										//why are we doing this?????????????????????
										//	from import_staging????
										//the payload is now on the competencyFramework record, at least in some fashion in CompetencyFrameworkGraph
										entity.RegistryImport = ImportManager.GetByCtid(item.CompetencyFramework.CTID);

                                        if ( !string.IsNullOrWhiteSpace(entity.RegistryImport.Payload) 
                                            && frameworksList.ContainsKey(entity.FrameworkName) == false)
                                            frameworksList.Add(entity.FrameworkName, entity.RegistryImport);
                                    }
                                }
                                else if ( item.Collection != null && item.Collection.Id > 0 )
								{
									entity.FrameworkName = item.Collection.Name;
									entity.FrameworkCtid = item.Collection.CTID;
									entity.IsFromACollection = true;
									//21-07-13 IsARegistryFrameworkUrl is not assigned anywhere, derived from Framework
									if ( entity.IsARegistryFrameworkUrl && ( item.Collection.CTID?.Length == 39) )
									{
										entity.ExistsInRegistry = true;
										if ( item.Collection.EntityStateId == 0 )
										{
											//need to skip - need more than this. A non-registry framework will not exist in registry
											entity.IsDeleted = true;
										}
									}
									if ( !string.IsNullOrWhiteSpace( item.Collection.CTID ) )
									{
                                        //SLOW!!! - added index
                                        //why are we doing this?
										//Used by the **world's longest method name**. If the payload is not provided, treated as a miscellaneous framework
                                        //the payload is now on the competencyFramework record, at least in some fashion in CompetencyFrameworkGraph
                                        //Skip for now and determine if needed
                                        entity.RegistryImport = ImportManager.GetByCtid( item.Collection.CTID );
                                        if ( !string.IsNullOrWhiteSpace( entity.RegistryImport.Payload )
                                            && frameworksList.ContainsKey( entity.FrameworkName ) == false )
                                            frameworksList.Add( entity.FrameworkName, entity.RegistryImport );
                                    }
								}
								else
								{
									//this should not happen - should log this
									//already populated
									//NO - can have anonymous frameworks. what to do for different orders?
									//		- maybe should sort by framework to have anonymous together?
                                    //entity.FrameworkName = item.FrameworkName;
                                    //entity.Framework = item.FrameworkUrl ?? "";
                                    //should we populate frameworkUri as well?
                                    //entity.FrameworkUri = item.FrameworkUrl ?? "";
                                }
                               
								//Hmmm - see how parent is used
								//	if parentId is meant to be the parent Entity.Id, then this is wrong
								//22-06-07 mp - skipping for now
								//if ( entity.IsFromACollection )
								//	entity.ParentId = item.CollectionId ?? 0 ;
								//else
								//	entity.ParentId = item.CompetencyFrameworkId ?? 0;

								prevName = item.FrameworkName;
							}

							caoItem = new CredentialAlignmentObjectItem();
							MapFromDB( item, caoItem );
							entity.Items.Add( caoItem );
							entity.HasCompetencies = true;
						}
						//add last one
						if ( !string.IsNullOrWhiteSpace( prevName ) || entity.IsAnonymousFramework || ( list.Count == 0 && entity.Items?.Count > 0))
						{
							if ( !entity.IsARegistryFrameworkUrl
								|| !hidingFrameworksNotPublished
								|| ( entity.IsARegistryFrameworkUrl && entity.ExistsInRegistry )
								)
								if ( !entity.IsDeleted )
									list.Add( entity );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAllAs_CAOFramework" );
			}
			return list;
		}//


		///// <summary>
		///// Get entity to determine if one exists for the entity and alignment type
		///// </summary>
		///// <param name="entityId"></param>
		///// <param name="targetNodeName"></param>
		///// <returns></returns>
		//public static ThisResource Get( int entityId, string targetNodeName )
		//{
		//	ThisResource entity = new ThisResource();
		//	if ( string.IsNullOrWhiteSpace(targetNodeName ))
		//		return entity;
		//	try
		//	{
		//		using ( var context = new EntityContext() )
		//		{
		//			DBEntity item = context.Entity_Competency
		//					.FirstOrDefault( s => s.EntityId == entityId 
		//					&& s.TargetNodeName == targetNodeName );

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
		public static ThisResource Get( int entityId, string alignmentType, string targetNodeName )
		{
			ThisResource entity = new ThisResource();
			if ( string.IsNullOrWhiteSpace( targetNodeName ) )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					DBEntity item = context.Entity_Competency
							.FirstOrDefault( s => s.EntityId == entityId
							&& s.Alignment.ToLower()== alignmentType.ToLower()
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
		public static void MapFromDB( DBEntity from, ThisResource to )
		{
			to.Id = from.Id;
			to.EntityId = from.EntityId;

			to.CompetencyFrameworkId = from.CompetencyFrameworkId ?? 0;
			to.TargetNodeName = from.TargetNodeName;
			to.TargetNodeDescription = from.TargetNodeDescription;
			if ( to.TargetNodeDescription == to.TargetNodeName )
				to.TargetNodeDescription = "";

			to.TargetNode = from.TargetNode;
			to.TargetNodeCTID = from.TargetNodeCTID;
			to.Alignment = from.Alignment;
			to.CodedNotation = from.CodedNotation;
			to.Weight = from.Weight ?? 0;

			//to.AlignmentTypeId = from.AlignmentTypeId ?? 0;
			//to.AlignmentType = from.AlignmentType;

			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;

			if ( from.CompetencyFramework != null && from.CompetencyFramework.Name != null )
				to.FrameworkName = from.CompetencyFramework.Name;

		}       //
		
		public static void MapFromDB( DBEntity from, CredentialAlignmentObjectItem to )
		{
			to.Id = from.Id;
			to.RelatedEntityId = from.EntityId;

			to.TargetNode = from.TargetNode;
			to.TargetNodeCTID = from.TargetNodeCTID;

			to.TargetNodeDescription = from.TargetNodeDescription;
			to.TargetNodeName = from.TargetNodeName;
			if ( to.TargetNodeDescription == to.TargetNodeName )
				to.TargetNodeDescription = "";

			to.Weight = ( from.Weight ?? 0M );
			to.CodedNotation = from.CodedNotation;

			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime ) from.Created;

		}
		public static void MapToAlignmentObject( CredentialAlignmentObjectProfile input, ThisResource output, string alignmentType )
		{

			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime ) input.Created;
            //
			output.FrameworkName = input.FrameworkName;
			//add url?? to Entity for now?
            //this should not be used from Entity.Competency, rather get from CompetencyFramework
            //****unless it is possible to not have these?
            if (!string.IsNullOrWhiteSpace(input.Framework))
			    output.Framework = input.Framework;
            else
                output.Framework = "";
            //todo - latter has value, lookup frameworkId
			//ISSUE - CAN BE A FRAMEWORK OR A COLLECTION
			if (input.FrameworkIsACollection)
            {
				output.CollectionId = new CollectionManager().Lookup_OR_Add( output.Framework, output.FrameworkName );
			}
			else //TODO - is absense of a framework handled?
				output.CompetencyFrameworkId = new CompetencyFrameworkManager().Lookup_OR_Add( output.Framework, output.FrameworkName );
			output.Alignment = alignmentType;
			output.TargetNode = input.TargetNode;
			output.TargetNodeCTID = input.TargetNodeCTID;
			output.TargetNodeDescription = input.TargetNodeDescription;
			output.TargetNodeName = input.TargetNodeName;
			if ( output.TargetNodeDescription == output.TargetNodeName )
				output.TargetNodeDescription = "";

			//to.Weight = GetDecimalField(from.Weight);
			output.Weight = input.Weight;
			output.CodedNotation = input.CodedNotation;
		}

		#endregion

	}
}
