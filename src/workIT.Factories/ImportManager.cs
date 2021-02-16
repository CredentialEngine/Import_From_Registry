using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using WM= workIT.Models;
using ThisEntity = workIT.Models.RegistryImport;
using DBEntity = workIT.Data.Tables.Import_Staging;
using EntityContext = workIT.Data.Tables.workITEntities;
using ImportMessage = workIT.Data.Tables.Import_Message;

using workIT.Utilities;
using EM = workIT.Data.Tables;

namespace workIT.Factories
{
	public class ImportManager : BaseFactory
	{
		static string thisClassName = "ImportManager";


		#region Import_Staging
		public int Add( ThisEntity entity, ref List<string> messages )
		{
			DBEntity efEntity = new DBEntity();
			using ( var context = new EntityContext() )
			{
				try
				{
					//won't have ctid if encountered exception
					efEntity.Ctid = entity.Ctid ?? "";
					efEntity.EntityTypedId = entity.EntityTypedId;
					efEntity.DocumentUpdatedAt = entity.DocumentUpdatedAt;
					efEntity.EnvelopeId = entity.EnvelopeId;
					//efEntity.Message = entity.Message; //See Import.Message
					efEntity.Payload = entity.Payload;
					//efEntity.ResourcePublicKey = entity.ResourcePublicKey;
					
					efEntity.DownloadDate = System.DateTime.Now;
					efEntity.IsMostRecentDownload = true;

					//set any existing downloads for this entity to not most recent
					ResetIsMostRecentDownload( efEntity.Ctid );

					context.Import_Staging.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						entity.Id = efEntity.Id;
						return efEntity.Id;
					}
					else
					{
						//?no info on error
						messages.Add( "Error - the add was not successful. " );
						string message = string.Format( thisClassName + ".Add() Failed", "Attempted to add a Import document. The process appeared to not work, but was not an exception, so we have no message, or no clue. EntityTypeId: {0}; EnvelopeId: {1}", entity.EntityTypedId, entity.EnvelopeId );
						EmailManager.NotifyAdmin( thisClassName + ".Add() Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Import" );
					messages.Add( "Error - the save was not successful. " + message );
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), EntityTypeId: {0}; EnvelopeId: {1}", entity.EntityTypedId, entity.EnvelopeId )); 
					messages.Add( "Unexpected system error. The site administration has been notified." );
				}
	
			}

			return entity.Id;
		}//


		public static void ResetIsMostRecentDownload( string ctid )
		{
			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> list = context.Import_Staging
						.Where( s => s.Ctid == ctid && s.IsMostRecentDownload == true )
						.ToList();
					if ( list != null && list.Count > 0 )
					{
						foreach ( DBEntity item in list )
						{
							item.IsMostRecentDownload = false;
							context.SaveChanges();
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".ResetIsMostRecentDownload. For CTID: " + ctid );
			}
		}

        /// <summary>
        /// get all to enable deleting
        /// NOTE: there can be many entries for an entity in Import staging. do a uniqueness check.
        /// </summary>
        /// <returns></returns>
        public static List<ThisEntity> GetAll( int entityTypeId = 0)
        {
            ThisEntity entity = new ThisEntity();
            List<ThisEntity> list = new List<ThisEntity>();
           
            try
            {
                string prevCTID = "";
                using ( var context = new EntityContext() )
                {
                    List<DBEntity> search = context.Import_Staging
                            .Where( s =>( entityTypeId == 0 || s.EntityTypedId == entityTypeId))
                            .OrderBy( s => s.EntityTypedId )
                            .ThenBy( x => x.EnvelopeId )
                            .ThenByDescending( s => s.DownloadDate )
                            .ToList();

                    if ( search != null && search.Count > 0 )
                    {
                        foreach ( DBEntity item in search )
                        {
                            entity = new ThisEntity();
                            entity.EnvelopeId = item.EnvelopeId;
                            entity.Ctid = item.Ctid;
                            entity.EntityTypedId = item.EntityTypedId;
                            if ( prevCTID != entity.Ctid.ToLower() )
                                list.Add( entity );

                            prevCTID = entity.Ctid.ToLower();
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
        /// Get most recent import Record for ctid
        /// </summary>
        /// <param name="ctid"></param>
        /// <returns></returns>
        public static ThisEntity GetByCtid( string ctid )
        {
            ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return null;
            try
            {
                using ( var context = new EntityContext() )
                {
                    List<DBEntity> list = context.Import_Staging 
                            .Where( s => s.Ctid == ctid )
                            .OrderByDescending( s => s.Id)
                            .Take(1)
                            .ToList();

                    if ( list != null && list.Count > 0 )
                    {
                        DBEntity dbentity = list[ 0 ];
						entity = new ThisEntity
						{
							EnvelopeId = dbentity.EnvelopeId,
							Ctid = dbentity.Ctid,
							EntityTypedId = dbentity.EntityTypedId,
							Payload = dbentity.Payload,
							IsMostRecentDownload = dbentity.IsMostRecentDownload ?? false,
							DownloadDate = dbentity.DownloadDate
						};
						if ( dbentity.DocumentUpdatedAt != null )
                            entity.DocumentUpdatedAt = ( DateTime ) dbentity.DocumentUpdatedAt;
                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".GetByCtid" );
            }
            return entity;
        }//


        public int AddMessages( SaveStatus entity, int parentId, ref List<string> messages )
		{
			ImportMessage efEntity = new ImportMessage();
			int msgCount = 0;
			using ( var context = new EntityContext() )
			{
				try
				{
					if ( entity.Messages == null || entity.Messages.Count == 0 )
						return 0;

					foreach ( StatusMessage msg in entity.Messages)
					{
						efEntity = new ImportMessage();
						efEntity.ParentId = parentId;
						if ( !string.IsNullOrWhiteSpace( msg.Message ) )
							efEntity.Message = msg.Message.Substring( 0, (msg.Message.Length < 500 ? msg.Message.Length - 1 : 500) );
						efEntity.Severity = msg.IsWarning ? 1 : 2;
						efEntity.Created = System.DateTime.Now;

						context.Import_Message.Add( efEntity );
						int count = context.SaveChanges();
						if ( count < 1 )
						{
							messages.Add( "Error - the add was not successful, no reason. " );
						}
						else
							msgCount++;
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddMessages(), Failed for ImportId: {0}; ", parentId ) );
					messages.Add( thisClassName + string.Format( ".AddMessages(), Failed for ImportId: {0}; message: {1}", parentId, ex.Message ) );
				}
			}

			return msgCount;
		}
		#endregion

		#region Import resolution methods
		//Import_EntityResolution
		public int Import_EntityResolutionAdd( string referencedAtId,
						string referencedCtid,
						int referencedEntityTypeId,
						Guid entityUid,
						int newEntityId,
						bool isResolved,
						ref List<string> messages,
						bool setAsResolved = false)
		{
			EM.Import_EntityResolution efEntity = new EM.Import_EntityResolution();
			using ( var context = new EntityContext() )
			{
				try
				{
					//first determine if referencedAtId exists
					EM.Import_EntityResolution entity = Import_EntityResolution_GetById( referencedAtId );
                    if (entity == null || entity.Id == 0)
                    {
                        //try by ctid
                        entity = Import_EntityResolution_GetByCtid( referencedCtid );
                    }
                    if ( entity != null && entity.Id > 0 )
					{
						//indicates can ignore add, but need a way to show has been resolved
						//and on being resolved, need to call methods to activate entities like roles
                        //18-03-24 mp - no action should be necessary as roles exist
						if ( setAsResolved 
							&&( entity.IsResolved ?? false) == false)
						{
							entity.IsResolved = true;
                            if ( newEntityId  > 0)
							    entity.EntityBaseId = newEntityId;
                            if ( referencedEntityTypeId > 0 )
                                entity.ReferencedEntityTypeId = referencedEntityTypeId;
                            int count2 = context.SaveChanges();
							
							return entity.Id;
						}
                        //was returning zero?
						//return 0;
                        return entity.Id;

                    }
					else
					{
                        if (( referencedAtId ?? "" ).Length == 39)
                        {
                            LoggingHelper.DoTrace( 3, thisClassName + string.Format( ".Import_EntityResolutionAdd. Unexpected ctid in referencedAtId: {0}, referencedEntityTypeId: {1}, entityUid: {2} ", referencedAtId, referencedEntityTypeId, entityUid ) );
                        }
                        efEntity.Created = System.DateTime.Now;
                        efEntity.ReferencedId = referencedAtId.Replace("/graph/","/resources").ToLower();
                        efEntity.ReferencedCtid = ( referencedCtid ?? "" ).ToLower();
                        efEntity.ReferencedEntityTypeId = referencedEntityTypeId;
                        efEntity.EntityBaseId = newEntityId;
                        efEntity.EntityUid = entityUid;
                        efEntity.IsResolved = setAsResolved;

                        context.Import_EntityResolution.Add( efEntity );
                        int count = context.SaveChanges();
                        if (count > 0)
                        {
                            return efEntity.Id;
                        }
                        else
                        {
                            //?no info on error
                            messages.Add( "Import_EntityResolutionAdd - Error - the add was not successful. " );
                            string message = string.Format( thisClassName + ".Import_EntityResolutionAdd() Failed. The process appeared to not work, but was not an exception, so we have no message, or no clue. referencedAtId: {0}; referencedEntityTypeId: {1}", referencedAtId, referencedEntityTypeId );
                            EmailManager.NotifyAdmin( thisClassName + ".Import_EntityResolutionAdd() Failed", message );
                        }
                    }
					

				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Import_EntityResolutionAdd() ", "Import" );
					messages.Add( "Error - the save was not successful. " + message );
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Import_EntityResolutionAdd(), referencedAtId: {0}; referencedEntityTypeId: {1}", referencedAtId, referencedEntityTypeId ) );
					messages.Add( "Unexpected system error. The site administration has been notified." );
				}
			}

			return 0;
		}
		public static bool Delete_Import_EntityResolution( int id, ref string statusMessage )
		{
			bool isValid = false;
			if ( id == 0 )
			{
				statusMessage = "Error - missing an identifier for the Assessment";
				return false;
			}
			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					var efEntity = context.Import_EntityResolution
								.SingleOrDefault( s => s.Id == id );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						context.Import_EntityResolution.Remove( efEntity );
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							isValid = true;							
						}
					}
					else
					{
						statusMessage = "Error - Delete_Import_EntityResolution failed, as record was not found.";
					}
				}
				catch ( Exception ex )
				{
					statusMessage = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + ".Delete_Import_EntityResolution()" );

					if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
					{
						statusMessage = "Error: this Import_EntityResolution cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this assessment can be deleted.";
					}
				}
			}
			return isValid;

		}
		public static void HandleResolvedEntity( EM.Import_EntityResolution entity )
		{

		}

        /// <summary>
        /// For all entries in Import_EntityResolution that have been resolved, set IsResolved to true
        /// </summary>
        public void SetAllResolvedEntities()
        {
            string connectionString = MainConnection();
            try
            {
                using (SqlConnection c = new SqlConnection( connectionString ))
                {
                    c.Open();

                    using (SqlCommand command = new SqlCommand( "[Import.EntityResolution_SetResolved]", c ))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.ExecuteNonQuery();
                        command.Dispose();
                        c.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError( ex, "HandleAllResolvedEntity", false );

            }

        }
		/// <summary>
		/// Get record by provided URI
		/// TODO - we should force all to be /resources
		/// </summary>
		/// <param name="referencedAtId"></param>
		/// <returns></returns>
        public static EM.Import_EntityResolution Import_EntityResolution_GetById( string referencedAtId )
		{
			EM.Import_EntityResolution entity = new EM.Import_EntityResolution();
			if ( string.IsNullOrWhiteSpace( referencedAtId ))
			{
				return entity;
			}
			//consider, although we already have a ctid lookp fall back
			string altId = "";
			if ( referencedAtId.ToLower().IndexOf( "/graph/ce" ) > -1 )
				altId = referencedAtId.Replace( "/graph/", "/resources/" );
			else
			{
				altId = referencedAtId.Trim();
				//altId = referencedAtId.Replace( "/resources", "/graph" );
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					entity = context.Import_EntityResolution
							.FirstOrDefault( s => s.ReferencedId.ToLower() == referencedAtId.ToLower() 
							|| s.ReferencedCtid.ToLower() == referencedAtId.ToLower() );

					if ( entity != null && entity.Id > 0 )
					{
						if (!(entity.IsResolved ?? false))
                        {
                            
                        }
						//check - if entity exists. If not, then probably deleted, and so should remove this record
						var e = EntityManager.GetEntity( (Guid)entity.EntityUid, false );
						if (e == null || e.Id == 0)
						{
							string statusMessage = "";
							Delete_Import_EntityResolution( entity.Id, ref statusMessage );

							return new EM.Import_EntityResolution();
						} else
						{
							//check if entityId is accurate
							if (entity.ReferencedEntityTypeId != e.EntityTypeId)
							{

							}
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Import_EntityResolution_GetById" );
			}
			return entity;
		}//

		public static EM.Import_EntityResolution Import_EntityResolution_GetByCtid( string ctid )
		{
			EM.Import_EntityResolution entity = new EM.Import_EntityResolution();
			if ( string.IsNullOrWhiteSpace( ctid ))
			{
				return entity;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					entity = context.Import_EntityResolution
							.FirstOrDefault( s => s.ReferencedCtid.ToLower() == ctid.ToLower() );

					if ( entity != null && entity.Id > 0 )
					{
						//check - if entity exists. If not, then probably deleted, and so should remove this record
						var e = EntityManager.GetEntity( ( Guid )entity.EntityUid, false );
						if ( e == null || e.Id == 0 )
						{
							string statusMessage = "";
							Delete_Import_EntityResolution( entity.Id, ref statusMessage );

							return new EM.Import_EntityResolution();
						}
						else
						{
							//check if entityId is accurate
							if ( entity.ReferencedEntityTypeId != e.EntityTypeId )
							{

							}
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Import_EntityResolution_GetByCtid" );
			}
			return entity;
		}//

		#endregion

		#region Import.PendingRequest
		/// <summary>
		/// Select specific entity type. 
		/// Only return records not already processed, and have wasChanged = true 
		/// TODO- should deletes also be returned and have caller process as found?
		/// </summary>
		/// <param name="entityType">If blankd, will select all</param>
		/// <returns></returns>
		public static List<WM.Import_PendingRequest> SelectPendingList(string entityType )
		{
			WM.Import_PendingRequest entity = new WM.Import_PendingRequest();
			List<WM.Import_PendingRequest> list = new List<WM.Import_PendingRequest>();
			/*
			 * 
				Assessment
				CompetencyFramework
				ConceptScheme
				ConditionManifest
				CostManifest
				Credential
				LearningOpportunity
				Organization
			 */
			//may not want to depend upon WasChanged yet!!!
			bool onlySelectIfWasChanged = UtilityManager.GetAppKeyValue( "onlySelectIfWasChanged", true );
			try
			{
				string prevCTID = "";
				using ( var context = new EntityContext() )
				{
					var search = context.Import_PendingRequest
							.Where( s => (s.WasChanged == true || onlySelectIfWasChanged == false)
								&& s.WasProcessed == false
								//may want an registry action property instead
								&& s.PublishMethodURI != "DELETE" && s.PublishMethodURI != "Transfer of Owner"
								&& ( s.PublishingEntityType == entityType ) 
								)
							.OrderBy( s => s.PublishingEntityType )
							.ThenBy( s => s.EntityCtid )
							.ThenBy( x => x.Created )
							.ToList();
					//  || string.IsNullOrWhiteSpace( entityType )
					if ( search != null && search.Count > 0 )
					{
						foreach ( var item in search )
						{
							entity = new WM.Import_PendingRequest
							{
								Id = item.Id,
								EnvelopeId = item.EnvelopeId,
								Environment = item.Environment,
								EntityName = item.EntityName,
								EntityCtid = item.EntityCtid,
								PublishMethodURI = item.PublishMethodURI,
								 PublishingEntityType = item.PublishingEntityType
								 //EnvelopeLastUpdated = item.EnvelopeLastUpdated
							};
							//entity.EntityTypedId = item.EntityTypedId; //derive??
							if ( prevCTID != entity.EntityCtid.ToLower() )
								list.Add( entity );

							prevCTID = entity.EntityCtid.ToLower();
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".SelectPendingList" );
			}
			return list;
		}//

		public static List<WM.Import_PendingRequest> SelectAllPendingExceptList(string excludeEntityType)
		{
			WM.Import_PendingRequest entity = new WM.Import_PendingRequest();
			List<WM.Import_PendingRequest> list = new List<WM.Import_PendingRequest>();
			/*
			 * 
				Assessment
				CompetencyFramework
				ConceptScheme
				ConditionManifest
				CostManifest
				Credential
				LearningOpportunity
				Organization
			 */
			try
			{
				string prevCTID = "";
				using ( var context = new EntityContext() )
				{
					var search = context.Import_PendingRequest
							.Where( s => s.WasChanged == true
								&& s.WasProcessed == false
								&& s.PublishMethodURI != "Registry Delete" && s.PublishMethodURI != "Transfer of Owner"
								&& ( s.PublishingEntityType != excludeEntityType ) 
								)
							.OrderBy( s => s.PublishingEntityType )
							.ThenBy( s => s.EntityCtid )
							.ThenBy( x => x.Created )
							.ToList();

					if ( search != null && search.Count > 0 )
					{
						foreach ( var item in search )
						{
							entity = new WM.Import_PendingRequest
							{
								Id = item.Id,
								EnvelopeId = item.EnvelopeId,
								Environment = item.Environment,
								EntityName = item.EntityName,
								EntityCtid = item.EntityCtid,
								PublishMethodURI = item.PublishMethodURI,
								PublishingEntityType = item.PublishingEntityType, 
								PublisherCTID = item.PublisherCTID,
								DataOwnerCTID= item.DataOwnerCTID
								//EnvelopeLastUpdated = item.EnvelopeLastUpdated
							};
							//entity.EntityTypedId = item.EntityTypedId; //derive??
							if ( prevCTID != entity.EntityCtid.ToLower() )
								list.Add( entity );

							prevCTID = entity.EntityCtid.ToLower();
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".SelectPendingList" );
			}
			return list;
		}//

		public static List<WM.Import_PendingRequest> SelectAllPendingDeletes()
		{
			WM.Import_PendingRequest entity = new WM.Import_PendingRequest();
			List<WM.Import_PendingRequest> list = new List<WM.Import_PendingRequest>();

			try
			{
				string prevCTID = "";
				using ( var context = new EntityContext() )
				{
					var search = context.Import_PendingRequest
							.Where( s => s.WasChanged == true
								&& s.WasProcessed == false
								&& (s.PublishMethodURI == REGISTRY_ACTION_DELETE || s.PublishMethodURI == REGISTRY_ACTION_PURGE)
								)
							.OrderBy( s => s.PublishingEntityType )
							.ThenBy( s => s.EntityCtid)
							.ThenBy( x => x.Created )
							.ToList();

					if ( search != null && search.Count > 0 )
					{
						foreach ( var item in search )
						{
							entity = new WM.Import_PendingRequest
							{
								Id = item.Id,
								EnvelopeId = item.EnvelopeId,
								Environment = item.Environment,
								EntityName = item.EntityName,
								EntityCtid = item.EntityCtid,
								PublishMethodURI = item.PublishMethodURI,
								PublishingEntityType = item.PublishingEntityType
								//EnvelopeLastUpdated = item.EnvelopeLastUpdated
							};
							//entity.EntityTypedId = item.EntityTypedId; //derive??
							if ( prevCTID != entity.EntityCtid.ToLower() )
								list.Add( entity );

							prevCTID = entity.EntityCtid.ToLower();
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".SelectPendingList" );
			}
			return list;
		}//

		public bool SetImport_PendingRequestHandled( int recordId, bool importWasSuccessful )
		{
			bool isValid = false;
			if ( recordId <= 0 )
			{
				return false;
			}
			using ( var context = new EntityContext() )
			{
				var efEntity = context.Import_PendingRequest
							.FirstOrDefault( s => s.Id == recordId );

				if ( efEntity != null && efEntity.Id > 0 )
				{
					efEntity.WasProcessed = true;
					efEntity.ImportWasSuccessful = importWasSuccessful;
					if ( importWasSuccessful )
						efEntity.ImportedDate = DateTime.Now;
					int count = context.SaveChanges();
					if ( count >= 0 )
					{
						isValid = true;
					}
				}
				else
				{
					LoggingHelper.LogError( thisClassName + string.Format( ".SetImport_PendingRequestHandled - record was not found. recordId: {0}", recordId ), true );
				}
			}

			return isValid;
		}///
		#endregion
	}
}
