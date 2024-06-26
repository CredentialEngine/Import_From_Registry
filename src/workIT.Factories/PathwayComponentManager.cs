﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;
using DBEntity = workIT.Data.Tables.PathwayComponent;
using EM = workIT.Data.Tables;
using EntityContext = workIT.Data.Tables.workITEntities;
using PC = workIT.Models.Common.PathwayComponent;
using ThisResource = workIT.Models.Common.PathwayComponent;
//using System.Security.Policy;

namespace workIT.Factories
{
    public class PathwayComponentManager : BaseFactory
	{
		static string thisClassName = "PathwayComponentManager";
		static string EntityType = "PathwayComponent";
		static int EntityTypeId = CodesManager.ENTITY_TYPE_PATHWAY_COMPONENT;

		public static int ComponentActionOfNone = 0;
		public static int ComponentActionOfSummary = 1;
		public static int ComponentActionOfDeep = 2;

		#region persistance ==================

		/// <summary>
		/// add a PathwayComponent
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Save( ThisResource entity, ref SaveStatus status )
		{
			bool isValid = true;
			var efEntity = new DBEntity();
			using ( var context = new EntityContext() )
			{
				try
				{
					if ( ValidateProfile( entity, ref status ) == false )
					{
						//alway trudge on
						//return false;
					}

					if ( entity.Id == 0 )
					{
						MapToDB( entity, efEntity );

						if ( entity.RowId == null || entity.RowId == Guid.Empty )
							efEntity.RowId = entity.RowId = Guid.NewGuid();
						else
							efEntity.RowId = entity.RowId;
						if ( entity.CTID != null )
						{
							efEntity.CTID = entity.CTID;
						}
						else
						{
							//can never be null. So will probably fail on save, but don't want to create a new CTID
							//wow weird major error!!!
							//efEntity.CTID = "ce-" + efEntity.RowId.ToString().ToLower();

						}

						efEntity.Created = efEntity.LastUpdated = System.DateTime.Now;

						context.PathwayComponent.Add( efEntity );

						// submit the change to database
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							entity.Id = efEntity.Id;
                            entity.Created = efEntity.Created.Value;
                            entity.LastUpdated = efEntity.LastUpdated.Value;
                            UpdateParts( entity, ref status );
                            UpdateEntityCache( entity, ref status );
                            return true;
						}
						else
						{
							//?no info on error
							status.AddError( "Error - the profile was not saved. " );
							string message = string.Format( "PathwayComponentManager.Add Failed", "Attempted to add a PathwayComponent. The process appeared to not work, but was not an exception, so we have no message, or no clue.PathwayComponent. PathwayComponent: {0}", entity.Name );
							EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
						}
					}
					else
					{
						efEntity = context.PathwayComponent
								.SingleOrDefault( s => s.Id == entity.Id );

						if ( efEntity != null && efEntity.Id > 0 )
						{
							//for updates, chances are some fields will not be in interface, don't map these (ie created stuff)
							MapToDB( entity, efEntity );
							efEntity.EntityStateId = 3;
							//??
							entity.RowId = efEntity.RowId;
							if ( HasStateChanged( context ) )
							{
								efEntity.LastUpdated = System.DateTime.Now;
								int count = context.SaveChanges();
								//can be zero if no data changed
								if ( count >= 0 )
								{
									isValid = true;
                                    entity.LastUpdated = efEntity.LastUpdated.Value;
                                    UpdateEntityCache( entity, ref status );
                                }
								else
								{
									//?no info on error
									status.AddError( "Error - the update was not successful. " );
									string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a PathwayComponent. The process appeared to not work, but was not an exception, so we have no message, or no clue. PathwayComponentId: {0}, Id: {1}.", entity.Id, entity.Id );
									EmailManager.NotifyAdmin( thisClassName + ". PathwayComponent_Update Failed", message );
								}
							}
							//continue with parts regardless
							UpdateParts( entity, ref status );
						}
						else
						{
							status.AddError( "Error - update failed, as record was not found." );
						}
					}

				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					//LoggingHelper.LogError( dbex, thisClassName + string.Format( ".ContentAdd() DBEntityValidationException, Type:{0}", entity.TypeId ) );
					string message = thisClassName + string.Format( ".Save() DBEntityValidationException, PathwayComponentId: {0}", entity.Id );
					foreach ( var eve in dbex.EntityValidationErrors )
					{
						message += string.Format( "\rEntity of type \"{0}\" in state \"{1}\" has the following validation errors:",
							eve.Entry.Entity.GetType().Name, eve.Entry.State );
						foreach ( var ve in eve.ValidationErrors )
						{
							message += string.Format( "- Property: \"{0}\", Error: \"{1}\"",
								ve.PropertyName, ve.ErrorMessage );
						}

						LoggingHelper.LogError( message );
					}
				}
				catch ( Exception ex )
				{
					var message = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".PathwayComponent_Add(), PathwayComponentId: {0}", entity.RelatedEntityId ) );
					status.AddError( string.Format( "Error encountered saving component. Type: {0}, Name: {1}, Error: {2}. ", entity.PathwayComponentType, entity.Name, message ) );
					isValid = false;
				}
			}

			return isValid;
		}
		public bool UpdateParts( ThisResource entity, ref SaveStatus status )
		{
			bool isValid = true;
			Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( thisClassName + $".UpdateParts(). Error - the related Entity was not found using {entity.RowId}." );
				return false;
			}
            Entity_ReferenceFrameworkManager erfm = new Entity_ReferenceFrameworkManager();
            erfm.DeleteAll( relatedEntity, ref status );

            if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_SOC, entity.OccupationTypes, ref status ) == false )
                isValid = false;
            if ( erfm.SaveList( relatedEntity.Id, CodesManager.PROPERTY_CATEGORY_NAICS, entity.IndustryTypes, ref status ) == false )
                isValid = false;

			var hasResourceMgr = new Entity_HasResourceManager();
			//21-01-07 mparsons - Identifier will now be saved in the Json properties, not in Entity_IdentifierValue
			//new Entity_IdentifierValueManager().SaveList( entity.Identifier, entity.RowId, Entity_IdentifierValueManager.CREDENTIAL_Identifier, ref status, false );
			Entity_CredentialManager ecm = new Entity_CredentialManager();
			ecm.DeleteAll( relatedEntity, ref status );
			//
			var eam = new Entity_AssessmentManager();
			eam.DeleteAll( relatedEntity, ref status );
			//??? issue - previously added an e.lopp and now is getting deleted?
			var elom = new Entity_LearningOpportunityManager();
			elom.DeleteAll( relatedEntity, ref status );
			//TBD
			var ecompm = new Entity_CompetencyManager();
			//ecompm.DeleteAll( relatedEntity, ref status );
			//
			int newId = 0;

			if ( entity.JsonProperties != null )
			{
                
                if (entity.JsonProperties.ProxyForResource != null && entity.JsonProperties.ProxyForResource.Id > 0)
                {
					//may want to add the reference so can get to any components that reference the resource
					//could use Entity.HasResource. Then creds, etc would need the extra step to call the latter

					hasResourceMgr.Add( relatedEntity, entity.JsonProperties.ProxyForResource.EntityTypeId, entity.JsonProperties.ProxyForResource.Id, Entity_HasResourceManager.HAS_RESOURCE_TYPE_HasTargetResource, ref status );
                }

                //TODO once latter fully implemented, remove the following
                if ( entity.JsonProperties.SourceCredential != null && entity.JsonProperties.SourceCredential.Id > 0 )
				{
					//21-07-23 mp	- These relationships are now considered inverse relationships for TargetPathway.
					//				- IsPartOf is not correct. TargetResource makes more sense!
					ecm.Add( entity.RowId, entity.JsonProperties.SourceCredential.Id, BaseFactory.RELATIONSHIP_TYPE_HAS_TARGET_RESOURCE, ref newId, ref status );
				}
				if ( entity.JsonProperties.SourceAssessment != null && entity.JsonProperties.SourceAssessment.Id > 0 )
				{
					eam.Add( entity.RowId, entity.JsonProperties.SourceAssessment.Id, BaseFactory.RELATIONSHIP_TYPE_HAS_TARGET_RESOURCE, false, ref status );
				}
				if ( entity.JsonProperties.SourceLearningOpportunity != null && entity.JsonProperties.SourceLearningOpportunity.Id > 0 )
				{
					elom.Add( entity.RowId, entity.JsonProperties.SourceLearningOpportunity.Id, BaseFactory.RELATIONSHIP_TYPE_HAS_TARGET_RESOURCE, false, ref status );
				}
				if ( entity.JsonProperties.SourceCompetency != null && entity.JsonProperties.SourceCompetency.Id > 0 )
				{
					//TBD for a competency? Will need to add a Entity for competency
					//ecompm.Add( entity.RowId, entity.JsonProperties.SourceCompetency.Id, BaseFactory.RELATIONSHIP_TYPE_IS_PART_OF, false, ref status );
				}
				//TODO need to keep adding types, like job, or try to go generic with ProxyForResource
			}
			return isValid;
		}
		public int AddPendingRecord( Guid entityUid, string ctid, string registryAtId, ref SaveStatus status, string parentCTID = "" )
		{
			DBEntity efEntity = new DBEntity();
			try
			{
				using ( var context = new EntityContext() )
				{
					if ( !IsValidGuid( entityUid ) )
					{
						status.AddError( thisClassName + " - A valid GUID must be provided to create a pending entity" );
						return 0;
					}
					//quick check to ensure not existing
					ThisResource entity = GetByCtid( ctid );
					if ( entity != null && entity.Id > 0 )
						return entity.Id;

					//only add DB required properties
					//**** DONT HAVE A COMPONENT TYPE
					efEntity.ComponentTypeId = 2;
					//NOTE - an entity will be created via trigger
					efEntity.Name = "Placeholder until full document is downloaded";
					efEntity.Description = "Placeholder until full document is downloaded";
					efEntity.EntityStateId = 1;
					efEntity.RowId = entityUid;
					//watch that Ctid can be  updated if not provided now!!
					efEntity.CTID = ctid;
					//need a pathway ctid - actually NOT
					efEntity.PathwayCTID = !string.IsNullOrWhiteSpace(parentCTID) ? parentCTID : "ce-placeholder";//??
					efEntity.SubjectWebpage = registryAtId;

					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;

					context.PathwayComponent.Add( efEntity );
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						//SiteActivity sa = new SiteActivity()
						//{
						//	ActivityType = EntityType,
						//	Activity = "Import",
						//	Event = string.Format( "Add Pending {0}", EntityType ),
						//	Comment = string.Format( "Pending {0} was added by the import. ctid: {1}, registryAtId: {2}", EntityType, ctid, registryAtId ),
						//	ActivityObjectId = efEntity.Id
						//};
						//new ActivityManager().SiteActivityAdd( sa );
						//Question should this be in the EntityCache?
						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						entity.CTID = efEntity.CTID;
						entity.EntityStateId = 1;
						entity.Name = efEntity.Name;
						entity.Description = efEntity.Description;
						entity.SubjectWebpage = efEntity.SubjectWebpage;
						entity.Created = ( DateTime )efEntity.Created;
						entity.LastUpdated = ( DateTime )efEntity.LastUpdated;
						//23-02-20 - commenting for now as not needed in cache
						//UpdateEntityCache( entity, ref status );
						return efEntity.Id;
					}

					status.AddError( thisClassName + " Error - the save was not successful, but no message provided. " );
				}
			}

			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddPendingRecord. entityUid:  {0}, ctid: {1}", entityUid, ctid ) );
				status.AddError( thisClassName + " Error - the save was not successful. " + message );

			}
			return 0;
		}
		public void UpdateEntityCache( ThisResource document, ref SaveStatus status )
		{
			EntityCache ec = new EntityCache()
			{
				EntityTypeId = EntityTypeId,
				EntityType = EntityType,
				EntityStateId = 3,// document.EntityStateId,
				IsActive = true,		//the default
				EntityUid = document.RowId,
				BaseId = document.Id,
				Description = document.Description,
				SubjectWebpage = document.SubjectWebpage,
				CTID = document.CTID,
				Created = document.Created,
				LastUpdated = document.LastUpdated,
				//ImageUrl = document.ImageUrl,
				Name = document.Name,
				//need to derive these - added from the pathway
				OwningAgentUID = document.PrimaryAgentUID,
				//this will be derived in save method
				OwningOrgId = document.OrganizationId
			};
			var statusMessage = string.Empty;
			//not sure if we want to cache these. We don't really treat them as top level
			if ( new EntityManager().EntityCacheSave( ec, ref statusMessage ) == 0 )
			{
				status.AddError( thisClassName + string.Format( ".UpdateEntityCache for '{0}' ({1}) failed: {2}", document.Name, document.Id, statusMessage ) );
			}
		}
		public bool DeleteAll( string pathwayCTID, ref SaveStatus status, DateTime? lastUpdated = null )
		{
			bool isValid = true;
			//Entity parent = EntityManager.GetEntity( parentUid );
			if (string.IsNullOrWhiteSpace( pathwayCTID ))
			{
				status.AddError( thisClassName + ". Error - a pathwayCTID was not provided." );
				return false;
			}
			try
			{
				using ( var context = new EntityContext() )
				{

					var results = context.PathwayComponent.Where( s => s.PathwayCTID == pathwayCTID ).ToList();
					if ( results == null || results.Count == 0 )
						return true;

					foreach ( var item in results )
					{
						//21-03-31 mp - just removing the profile will not remove its entity and the latter's children!
						string statusMessage = string.Empty;
                        Guid rowId = item.RowId;
                        new EntityManager().Delete( item.RowId, string.Format( "PathwayComponent: {0} ({1})", item.Name, item.Id ), ref statusMessage );
						var id = item.Id;
						context.PathwayComponent.Remove( item );
						var count = context.SaveChanges();
						if ( count > 0 )
						{
							//delete cache
							new EntityManager().EntityCacheDelete( rowId, ref statusMessage );

						}
					}
				}

			}
			catch ( Exception ex )
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. pathwayCTID: {0}, exception: {1}", pathwayCTID, msg ) );
			}
			return isValid;
		}
		public bool Delete( int Id, ref string statusMessage )
		{
			bool isValid = false;
			if ( Id == 0 )
			{
				statusMessage = "Error - missing an identifier for the PathwayComponent";
				return false;
			}
			using ( var context = new EntityContext() )
			{
				DBEntity efEntity = context.PathwayComponent
							.SingleOrDefault( s => s.Id == Id );

				if ( efEntity != null && efEntity.Id > 0 )
				{
					context.PathwayComponent.Remove( efEntity );
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						isValid = true;
					}
				}
				else
				{
					statusMessage = "Error - delete failed, as record was not found.";
				}
			}

			return isValid;
		}

		private bool ValidateProfile( PathwayComponent profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;
			if ( string.IsNullOrWhiteSpace( profile.Name ) )
			{
				status.AddError( string.Format("Error: A PathwayComponent Name is required.  CTID: {0}, Component: {1}", profile.CTID ?? "none?", profile.ComponentTypeId) );
			}
			//if ( string.IsNullOrWhiteSpace( profile.Description ) )
			//{
			//	status.AddError( "Error: A PathwayComponent Description is required." );
			//}


			return status.WasSectionValid;
		}
		#endregion

		#region == Retrieval =======================

		public static ThisResource Get( int id, int childComponentsAction = 1, bool includingPathway = false )
		{
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.PathwayComponent
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, childComponentsAction, includingPathway );
				}
			}

			return entity;
		}
		/// <summary>
		/// Get component by Guid
		/// </summary>
		/// <param name="id"></param>
		/// <param name="childComponentsAction">1-default of summary</param>
		/// <returns></returns>
		public static ThisResource Get( Guid id, int childComponentsAction = 1, bool includingPathway = false )
		{
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.PathwayComponent
						.SingleOrDefault( s => s.RowId == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, childComponentsAction, includingPathway );
				}
			}

			return entity;
		}
		/// <summary>
		/// Get a basic PathwayComponent by CTID
		/// </summary>
		/// <param name="ctid"></param>
		/// <returns></returns>
		public static ThisResource GetByCtid( string ctid, int childComponentsAction = 1, bool includingPathway = false )
		{

			PathwayComponent entity = new PathwayComponent();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;

			using ( var context = new EntityContext() )
			{
				context.Configuration.LazyLoadingEnabled = false;
				EM.PathwayComponent item = context.PathwayComponent
							.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower()
								);

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, childComponentsAction, includingPathway );
				}
			}

			return entity;
		}

		//
		public static List<ThisResource> GetAllForPathway( string pathwayCTID, int childComponentsAction = 2 )
		{
			var output = new List<ThisResource>();
			var entity = new ThisResource();

			using ( var context = new EntityContext() )
			{
				var list = context.PathwayComponent
							.Where( s => s.PathwayCTID == pathwayCTID )
							.ToList();
				foreach ( var item in list )
				{
					entity = new ThisResource();
					//when called via a pathway getAll, the subcomponents will be useally lists, and the detailed component will be in the hasPart
					MapFromDB( item, entity, childComponentsAction );
					output.Add( entity );
				}
			}

			return output;
		}
		//


		/// <summary>
		/// 
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="childComponentsAction">0-none; 1-summary; 2-deep </param>
		public static void MapFromDB( DBEntity input, ThisResource output, int childComponentsAction = 1, bool includingPathway = false )
		{

			output.Id = input.Id;
			output.RowId = input.RowId;
			output.EntityStateId = input.EntityStateId < 1 ? 1 : input.EntityStateId;
			output.CTID = input.CTID;
			output.Name = input.Name;
			output.Description = input.Description;
			output.PathwayCTID = input.PathwayCTID;

			var relatedEntity = EntityManager.GetEntity( output.RowId, false );
			if ( relatedEntity != null && relatedEntity.Id > 0 )
			{
				output.EntityLastUpdated = relatedEntity.LastUpdated;
				output.RelatedEntityId= relatedEntity.Id;
			}

			//need output get parent pathway?
			//don't want to do this when getting each component
			if (includingPathway)
			{
				//note the destination component will be populated
				output.Pathway = PathwayManager.GetByCtid( output.PathwayCTID, false );
			}

			//ispartof. Should be single, but using list for flexibility?
			//actually force one, as we are using pathway identifier an external id for a unique lookup
			//may not want output do this every time?
			//output.IsPartOf = Entity_PathwayComponentManager.GetPathwayForComponent( output.Id, PathwayComponent.PathwayComponentRelationship_HasPart );

			//may want output get all and split out
			//23-07-27 mp - with the addition of a component detail page, need to determine if 
			if ( childComponentsAction == ComponentActionOfDeep)
			{
				output.AllComponents = Entity_PathwayComponentManager.GetAll( output.RowId, ComponentActionOfSummary );
				foreach ( var item in output.AllComponents )
				{
					if ( item.ComponentRelationshipTypeId == PC.PathwayComponentRelationship_HasChild )
						output.HasChild.Add( item );
					else if ( item.ComponentRelationshipTypeId == PC.PathwayComponentRelationship_IsChildOf )
						output.IsChildOf.Add( item );
					else if ( item.ComponentRelationshipTypeId == PC.PathwayComponentRelationship_Precedes )
						output.Precedes.Add( item );
					else if ( item.ComponentRelationshipTypeId == PC.PathwayComponentRelationship_PrecededBy )
						output.PrecededBy.Add( item );
					//else if ( item.ComponentRelationshipTypeId == PC.PathwayComponentRelationship_Prerequiste )
					//	output.Prerequisite.Add( item );
				}
				//child components - details of condition, but summary of components
				//	ensure only one level
				output.HasCondition = Entity_ComponentConditionManager.GetAll( output.RowId, true );
			}

			//output.CodedNotation = input.CodedNotation;
			output.ComponentCategory = input.ComponentCategory;
			output.ComponentTypeId = (int)input.ComponentTypeId;
			if ( input.Codes_PathwayComponentType != null && input.Codes_PathwayComponentType.Id > 0 )
			{
				output.PathwayComponentType = input.Codes_PathwayComponentType.Title;
			}
			else
			{
				output.PathwayComponentType = GetComponentType( output.ComponentTypeId );
			}
			//will be validated before getting here!
			output.CredentialType = input.CredentialType;
			if ( !string.IsNullOrWhiteSpace( output.CredentialType) && output.CredentialType.IndexOf("ctdl/terms") > 0)
			{
				//21-08-12 - why was the purl url being stored?
				int pos = output.CredentialType.IndexOf( "ctdl/terms" );
				output.CredentialType = output.CredentialType.Substring( pos + 11 );
			}

			//not sure if this will just be a URI, or point output a concept
			//if a concept, would probably need entity.hasConcept
			//output.HasProgressionLevel = input.HasProgressionLevel;
			//if ( !string.IsNullOrWhiteSpace( input.HasProgressionLevel ) )
			//{
			//	output.ProgressionLevel = ConceptSchemeManager.GetByConceptCtid( output.HasProgressionLevel );
			//	output.HasProgressionLevelDisplay = output.ProgressionLevel.PrefLabel;
			//}
			//20-10-28 now storing separated list
			if ( !string.IsNullOrWhiteSpace( input.HasProgressionLevel ) )
			{
				string[] array = input.HasProgressionLevel.Split( '|' );
				if ( array.Count() > 0 )
				{
					output.HasProgressionLevelDisplay = string.Empty;

                    foreach ( var i in array )
					{
						if ( !string.IsNullOrWhiteSpace( i ) )
						{
							var pl = ProgressionModelManager.GetByConceptCtid( i );
							//need to confirm progression model import. Or should the look up be
							if ( pl != null && !string.IsNullOrWhiteSpace( pl.PrefLabel ) )
							{
								output.ProgressionLevels.Add( pl );

								output.HasProgressionLevelDisplay += pl.PrefLabel + ", ";
							}
							//really should only have one
							output.HasProgressionLevel = i;
							output.HasProgressionLevels.Add( i );
						}
					}
					output.HasProgressionLevelDisplay.Trim().TrimEnd( ',' );
				}
			}

			output.ProgramTerm = input.ProgramTerm;
			output.SubjectWebpage = input.SubjectWebpage;
			output.SourceData = input.SourceData;

            output.OccupationTypes = Reference_FrameworkItemManager.FillCredentialAlignmentObject( output.RowId, CodesManager.PROPERTY_CATEGORY_SOC );
            output.IndustryTypes = Reference_FrameworkItemManager.FillCredentialAlignmentObject( output.RowId, CodesManager.PROPERTY_CATEGORY_NAICS );

            output.ProxyFor = input.ProxyFor;
            output.FinderResource = new ResourceSummary();
            var finder = UtilityManager.GetAppKeyValue( "credentialFinderMainSite" );
            if ( !string.IsNullOrWhiteSpace( output.ProxyFor ) )
            {
				//should already be storing just the CTID
				if ( IsValidCtid( output.ProxyFor ) )
				{
					output.ProxyFor = ExtractCtid( output.ProxyFor );
					//do generic here, and replace below
					output.FinderResource = new ResourceSummary()
					{
						CTID = output.ProxyFor,
						Name = "View Resource in Credential Finder", //??
						URI = finder + "resources/" + output.ProxyFor
					};
				}
            }
            //where output store ComponentDesignation - textvalue
            //Json
            if ( !string.IsNullOrEmpty( input.Properties ) )
			{
				PathwayComponentProperties pcp = JsonConvert.DeserializeObject<PathwayComponentProperties>( input.Properties );
				if ( pcp != null )
				{
					output.ExternalPathwayCTID = pcp.ExternalPathwayCTID;
					output.RowNumber = pcp.RowNumber;
					output.ColumnNumber = pcp.ColumnNumber;
					//unpack ComponentDesignation
					output.ComponentDesignationList = pcp.ComponentDesignationList;
					//credit value
					output.CreditValue = pcp.CreditValue;
					//this is now QuantitativeValue
					output.PointValue = pcp.PointValue;

					output.Identifier = new List<IdentifierValue>();
					if ( pcp.Identifier != null )
						output.Identifier = pcp.Identifier;

					if (pcp.ProxyForResource != null && pcp.ProxyForResource.Id > 0)
					{
						output.ProxyForResource = pcp.ProxyForResource;
						output.FinderResource = new ResourceSummary()
						{
							EntityTypeId = pcp.ProxyForResource.EntityTypeId,
							Id = pcp.ProxyForResource.Id,
							Name = pcp.ProxyForResource.Name,
							Description = pcp.ProxyForResource.Description,
							CTID = pcp.ProxyForResource.CTID,
							URI = finder + "resources/" + output.ProxyForResource.CTID
						};

					}
					else
					{
						if (pcp.SourceCredential != null && pcp.SourceCredential.Id > 0)
						{
							output.SourceCredential = pcp.SourceCredential;
							output.FinderResource.Name = output.SourceCredential.Name;
							output.FinderResource.URI = finder + "resources/" + output.SourceCredential.CTID;
							output.SourceData = string.Empty;
						}
						if (pcp.SourceAssessment != null && pcp.SourceAssessment.Id > 0)
						{
							output.SourceAssessment = pcp.SourceAssessment;
							output.FinderResource.Name = output.SourceAssessment.Name;
							output.FinderResource.URI = finder + "resources/" + output.SourceAssessment.CTID;
							output.SourceData = string.Empty;
						}
						if (pcp.SourceLearningOpportunity != null && pcp.SourceLearningOpportunity.Id > 0)
						{
							output.SourceLearningOpportunity = pcp.SourceLearningOpportunity;
							output.FinderResource.Name = output.SourceLearningOpportunity.Name;
							output.FinderResource.URI = finder + "resources/" + output.SourceLearningOpportunity.CTID;
							output.SourceData = string.Empty;
						}
						if (pcp.SourceCompetency != null && pcp.SourceCompetency.Id > 0)
						{
							output.SourceCompetency = pcp.SourceCompetency;
							//may want to shorten the competency text?
							output.FinderResource.Name = output.SourceCompetency.Name;
							output.SourceData = string.Empty;
						}
					}
					if ( output.ComponentTypeId == 13 )//multicomponent
                    {
						if ( pcp.ProxyForResourceList != null && pcp.ProxyForResourceList.Count>0 )
						{
							output.ProxyForResourceList =MapTopEntityLevelToResourceSummary(pcp.ProxyForResourceList);

						}
					}
                }
			}

			//
			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime )input.Created;
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime )input.LastUpdated;

		}
		public static void MapToDB( ThisResource input, DBEntity output )
		{

			output.Id = input.Id;
			if ( output.Id < 1 )
			{

				//will need output be carefull here, will this exist in the input??
				//there could be a case where an external Id was added output bulk upload for an existing record
				output.PathwayCTID = input.PathwayCTID;
			}
			
			{

				//don't map rowId, CTID, or dates as not on form
				//output.RowId = input.RowId;
				output.Name = input.Name;
				output.Description = input.Description;
				//output.CodedNotation = input.CodedNotation;
				output.EntityStateId = 3;
				output.ComponentCategory = input.ComponentCategory;
				output.ComponentTypeId = GetComponentTypeId( input.PathwayComponentType );
				//output.PathwayCTID can never be null as it is require.
				if ( string.IsNullOrWhiteSpace( output.PathwayCTID ) || output.PathwayCTID == "ce-placeholder" )
					output.PathwayCTID = input.PathwayCTID;
				//output.ComponentTypeId = input.ComponentTypeId;
				//will be validated before getting here!
				output.CredentialType = input.CredentialType;
				//21-08-12 mp - not sure why the full purl url was being stored. Now start just storing the 
				if ( !string.IsNullOrWhiteSpace( output.CredentialType ) && output.CredentialType.IndexOf( "ctdl/terms" ) > 0 )
				{
					//21-08-12 - why was the purl url being stored?
					int pos = output.CredentialType.IndexOf( "ctdl/terms" );
					output.CredentialType = output.CredentialType.Substring( pos + 11 );
				}

				//output.ExternalIdentifier = input.ExternalIdentifier;
				//not sure if this will just be a URI, or point output a concept
				//if a concept, would probably need entity.hasConcept
				//output.HasProgressionLevel = input.HasProgressionLevels;
				if ( input.HasProgressionLevel != null && input.HasProgressionLevels.Any() )
				{
					output.HasProgressionLevel = string.Join( "|", input.HasProgressionLevels.ToArray() );
				}
				else
					output.HasProgressionLevel = null;

				//need output change ??
				//output.IsDestinationComponentOf = input.IsDestinationComponentOf;
				//this is now in JsonProperties
				//output.PointValue = input.PointValueOld;
				output.ProgramTerm = input.ProgramTerm;
				output.SubjectWebpage = input.SubjectWebpage;
				output.SourceData = input.SourceData;
                output.ProxyFor = input.ProxyFor;
                output.Properties = JsonConvert.SerializeObject( input.JsonProperties );
			}


		}

		public static List<ResourceSummary> MapTopEntityLevelToResourceSummary(List<TopLevelEntityReference> input )
        {
			var output = new List<ResourceSummary>();
			foreach(var entity in input )
            {
				var resourceSummary = new ResourceSummary
				{
					Name = entity.Name,
					CTID = entity.CTID,
					Description = entity.Description,
					Type = "ceterms:" + entity.EntityType + "Component"
				};
				output.Add( resourceSummary );
            }
			return output;
        }

		public static int GetComponentTypeId( string componentType )
		{
			if ( string.IsNullOrWhiteSpace( componentType ) )
				return 1;

			int componentTypeId = 0;
			switch ( componentType.Replace( "ceterms:", string.Empty ).ToLower() )
			{
				case "assessmentcomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Assessment;
					break;
				case "basiccomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Basic;
					break;
				case "cocurricularcomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Cocurricular;
					break;
				case "competencycomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Competency;
					break;
				case "coursecomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Course;
					break;
				case "credentialcomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Credential;
					break;
				case "extracurricularcomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Extracurricular;
					break;
				case "jobcomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Job;
					break;
				case "selectioncomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Selection;
					break;
				case "workexperiencecomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Workexperience;
					break;
				case "multicomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Multi;
					break;
				case "collectioncomponent":
					componentTypeId = PathwayComponent.PathwayComponentType_Collection;
					break;
				//
				default:
					componentTypeId = 0;
					break;
			}

			return componentTypeId;
		}

		public static string GetComponentType( int componentTypeId )
		{
			string componentType = string.Empty;
			switch ( componentTypeId )
			{
				case 1:
					componentType = PC.AssessmentComponent;
					break;
				case 2:
					componentType = PC.BasicComponent;
					break;
				case 3:
					componentType = PC.CocurricularComponent;
					break;
				case 4:
					componentType = PC.CompetencyComponent;
					break;
				case 5:
					componentType = PC.CourseComponent;
					break;
				case 6:
					componentType = PC.CredentialComponent;
					break;
				case 7:
					componentType = PC.ExtracurricularComponent;
					break;
				case 8:
					componentType = PC.JobComponent;
					break;
				case 9:
					componentType = PC.WorkExperienceComponent;
					break;
				case 10:
					componentType = PC.SelectionComponent;
					break;
				case 12:
					componentType = PC.CollectionComponent;
					break;
				case 13:
					componentType = PC.MultiComponent;
					break;
				//
				default:
					componentType = "unexpected: " + componentTypeId.ToString();
					break;
			}

			return componentType;
		}
		#endregion

	}
}
