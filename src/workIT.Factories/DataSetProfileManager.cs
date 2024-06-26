﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;
using APIM = workIT.Models.API;
using ThisResource = workIT.Models.QData.DataSetProfile;
using ThisResourceSummary = workIT.Models.QData.DataSetProfileSummary;

using DBEntity = workIT.Data.Tables.DataSetProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace workIT.Factories
{
	public class DataSetProfileManager : BaseFactory
	{
		static readonly string thisClassName = "DataSetProfileManager";
		static string EntityType = "DataSetProfile";
		static int EntityTypeId = CodesManager.ENTITY_TYPE_DATASET_PROFILE;


		#region DataSetProfile - persistance ==================

		/// <summary>
		/// For this method version, the datasetProfile already exists (although could have pending state), just need to confirm the Entity.DataSetProfile
		/// Example
		/// ADP
		///		Entity
		///			Entity_DSP
		/// </summary>
		/// <param name="input">List of integers relating to DSPs</param>
		/// <param name="parentEntity"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool SaveList( List<int> input, Entity parentEntity, ref SaveStatus status )
		{
			bool allIsValid = true;

			try
			{
				//need to handle deletes for parent?
				//will be an issue for shared datasets
				//DeleteAll( parentEntity, ref status );
				using ( var context = new EntityContext() )
				{
					//get all datasetProfiles for this parent
					var existing = context.Entity_DataSetProfile.Where( s => s.EntityId == parentEntity.Id ).ToList();
					//huh - only deleting 
					//object get all e_dsp for current parent where the ctid is not found in the input list.
					//	- we will want to delete the e_dsp for the latter (and maybe dsp if no other connections)
					var result = existing.Where( ex => input.All( p2 => p2 != ex.DataSetProfile.Id ) ).ToList();
					var messages = new List<string>();
					foreach ( var item in result )
					{
						//23-02-10 mp - this is not necessary if done sucessively with triggers
						Delete( item.Id, ref messages );
					}
					if ( messages.Any() )
						status.AddErrorRange( messages );
				}
				if ( input == null || !input.Any() )
					return true;

				new Entity_DataSetProfileManager().SaveList( input, parentEntity, ref status );
				
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".SaveList(List<int> )" );
			}
			return allIsValid;
		}


		/// <summary>
		/// This version is mostly obsolete, as used with holderProfile, employmentOutcomeProfile, and earningsProfile
		/// </summary>
		/// <param name="input"></param>
		/// <param name="parentEntity"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		[Obsolete]
		public bool SaveList( List<ThisResource> input, Entity parentEntity, ref SaveStatus status )
		{
			bool allIsValid = true;

			try
			{
				//need to handle deletes for parent?
				//will be an issue for shared datasets
				//DeleteAll( parentEntity, ref status );
				using ( var context = new EntityContext() )
				{
					//get all datasetProfiles for this parent
					var existing = context.Entity_DataSetProfile.Where( s => s.EntityId == parentEntity.Id ).ToList();
					//huh - only deleting 
					//object get all e_dsp for current parent where the ctid is not found in the input list.
					//	- we will want to delete the e_dsp for the latter (and maybe dsp if no other connections)
					var result = existing.Where( ex => input.All( p2 => p2.CTID != ex.DataSetProfile.CTID ) ).ToList();
					var messages = new List<string>();
					foreach ( var item in result )
					{
						Delete( item.Id, ref messages );
					}
					if ( messages.Any() )
						status.AddErrorRange( messages );
				}
				if ( input == null || !input.Any() )
					return true;

				foreach ( var item in input )
				{
					//current the datasetProfile is not being deleted - may be OK - TBD
					var e = GetByCtid( item.CTID, false, true );
					item.Id = e.Id;
					if ( !Save( item, parentEntity, ref status ) )
					{
						allIsValid = false;

					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".SaveList()" );
			}
			return allIsValid;
		}


		public bool Save( ThisResource entity, Entity parentEntity, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;
			DateTime lastUpdated = System.DateTime.Now;
			try
			{
				using ( var context = new EntityContext() )
				{
					if ( ValidateProfile( entity, ref status ) == false )
					{
						//return false;
					}

                    context.Configuration.LazyLoadingEnabled = false;
                    DBEntity efEntity = context.DataSetProfile
                            .FirstOrDefault( s => s.CTID == entity.CTID );
                    if ( efEntity!= null && efEntity.Id > 0 )
					{
						//TODO - consider if necessary, or interferes with anything
						//context.Configuration.LazyLoadingEnabled = false;
						//DBEntity efEntity = context.DataSetProfile
						//		.SingleOrDefault( s => s.Id == entity.Id );
						entity.Id = efEntity.Id;
						
						//fill in fields that may not be in entity
						entity.RowId = efEntity.RowId;

						MapToDB( entity, efEntity );

						if ( efEntity.EntityStateId == 0 )
						{
							//log?
							//perhaps not with the approach of vdeleting on each lopp import
							//SiteActivity sa = new SiteActivity()
							//{
							//	ActivityType = "DataSetProfile",
							//	Activity = "Import",
							//	Event = "Reactivate",
							//	Comment = string.Format( "DataSetProfile had been marked as deleted, and was reactivted by the import. CTID: {0}, SWP: {1}", entity.CTID, entity.Source ),
							//	ActivityObjectId = entity.Id
							//};
							//new ActivityManager().SiteActivityAdd( sa );
						}
						//assume and validate, that if we get here we have a full record
						//if ( efEntity.EntityStateId != 2 )
							efEntity.EntityStateId = 3;
                        entity.EntityStateId = efEntity.EntityStateId;
                        if ( IsValidDate( status.EnvelopeCreatedDate ) && status.LocalCreatedDate < efEntity.Created )
						{
							efEntity.Created = status.LocalCreatedDate;
						}
						//will always use the envelop last updated?
						if ( IsValidDate( status.EnvelopeUpdatedDate ) && status.LocalUpdatedDate != efEntity.LastUpdated )
						{
							efEntity.LastUpdated = status.LocalUpdatedDate;
							lastUpdated = status.LocalUpdatedDate;
						}
						//has changed?
						if ( HasStateChanged( context ) )
						{
							if ( IsValidDate( status.EnvelopeUpdatedDate ) )
								efEntity.LastUpdated = status.LocalUpdatedDate;
							else
								efEntity.LastUpdated = DateTime.Now;
							//NOTE efEntity.EntityStateId is set to 0 in delete method )
							count = context.SaveChanges();
							//can be zero if no data changed
							if ( count >= 0 )
							{
								isValid = true;
							}
							else
							{
								//?no info on error

								isValid = false;
								string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a DataSetProfile. The process appeared to not work, but was not an exception, so we have no message, or no clue. DataSetProfile: {0}, Id: {1}", entity.Name, entity.Id );
								status.AddError( "Error - the update was not successful. " + message );
								EmailManager.NotifyAdmin( thisClassName + ".Save Failed Failed", message );
							}

						}

						entity.LastUpdated = lastUpdated;
						UpdateEntityCache( entity, ref status );
						if ( isValid )
						{
							if ( !UpdateParts( entity, ref status ) )
								isValid = false;
							//should not be necessary, but seems the e_DSP is missing somehow
							//N/A for a directly imported dataSetProfile
							if ( parentEntity != null )
							{
								new Entity_DataSetProfileManager().Add( parentEntity, entity.Id, ref status );
							}
							else
							{
								//
								//21-04-22 only add activity if was a standalone dataset
								SiteActivity sa = new SiteActivity()
								{
									ActivityType = "DataSetProfile",
									Activity = "Import",
									Event = "Update",
									Comment = string.Format( "DataSetProfile was updated by the import. CTID: {0}, Source: {1}", entity.CTID, entity.Source ),
									ActivityObjectId = entity.Id
								};
								new ActivityManager().SiteActivityAdd( sa );
							}
							//if ( isValid || partsUpdateIsValid )
							//new EntityManager().UpdateModifiedDate( entity.RowId, ref status, efEntity.LastUpdated );
						}
					
					}
					else
					{
						//add
						entity.Id = Add( entity, parentEntity, ref status );
						if ( entity.Id == 0 || status.HasErrors )
							isValid = false;
					}
					//fill for later
					if ( IsGuidValid( entity.DataProviderUID ) )
					{
						entity.DataProviderUID = ( Guid )entity.DataProviderUID;
						entity.DataProviderOld = OrganizationManager.GetForSummary( entity.DataProviderUID );
					}
				}
			}
			catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
			{
				string message = HandleDBValidationError( dbex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ), "DataSetProfile" );
				status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ) );
				status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
				isValid = false;
			}


			return isValid;
		}

        public void ReActivate( List<string> input, ref SaveStatus status )
        {
			if ( input == null || !input.Any() )
				return;
            bool isValid = true;
            ThisResource entity = new ThisResource();
            int count = 0;
            DateTime lastUpdated = System.DateTime.Now;
            try
            {
                using ( var context = new EntityContext() )
                {
					foreach ( var item in input )
					{
						context.Configuration.LazyLoadingEnabled = false;
						DBEntity efEntity = context.DataSetProfile
								.FirstOrDefault( s => s.CTID.ToLower() == item.ToLower().Trim() );
						if ( efEntity.Id > 0 )
						{
							//probably ok to ignore pending
							if ( efEntity.EntityStateId == 0 )
							{
								efEntity.EntityStateId = 3;

								if ( HasStateChanged( context ) )
								{
									count = context.SaveChanges();
									//can be zero if no data changed
									if ( count >= 0 )
									{
										isValid = true;
                                        if ( isValid )
                                        {

                                            MapFromDB( efEntity, entity, false, false );
                                            //may not be necessary
                                            UpdateEntityCache( entity, ref status );
											//the parent could be the e.ADP entity which could exist now, but
                                            //if ( parentEntity != null )
                                            //{
                                            //    new Entity_DataSetProfileManager().Add( parentEntity, entity.Id, ref status );
                                            //}

                                        }
                                    }
									else
									{
										//?no info on error

										isValid = false;
										string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a DataSetProfile. The process appeared to not work, but was not an exception, so we have no message, or no clue. DataSetProfile: {0}, Id: {1}", efEntity.Name, efEntity.Id );
										status.AddError( "Error - the update was not successful. " + message );
										EmailManager.NotifyAdmin( thisClassName + ".Save Failed Failed", message );
									}
								}

							}
						}
					}
                }
            }
            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ) );
                status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
                isValid = false;
            }

        }
        public void ReActivate( string dspCTID, ref SaveStatus status )
        {
            if ( string.IsNullOrWhiteSpace( dspCTID ))
                return;
            ThisResource entity = new ThisResource();
            int count = 0;
            DateTime lastUpdated = System.DateTime.Now;
            try
            {
                using ( var context = new EntityContext() )
                {                    
                    context.Configuration.LazyLoadingEnabled = false;
                    DBEntity efEntity = context.DataSetProfile
                            .FirstOrDefault( s => s.CTID.ToLower() == dspCTID.ToLower().Trim() );
                    if ( efEntity.Id > 0 )
                    {
                        //probably ok to ignore pending
                        if ( efEntity.EntityStateId == 0 )
                        {
                            efEntity.EntityStateId = 3;
                          
                            count = context.SaveChanges();
                            //can be zero if no data changed
                            if ( count >= 0 )
                            {
                                MapFromDB( efEntity, entity, false, false );
                                //may not be necessary, as may have been reactivated in a trigger
                                UpdateEntityCache( entity, ref status );
                            }
                            else
                            {
                                //?no info on error

                                string message = string.Format( thisClassName + ".ReActivate Failed", "Attempted to ReActivate a DataSetProfile . The process appeared to not work, but we have no message, or no clue. DataSetProfile CTID: {0}", dspCTID );
                                status.AddError( "Error - the ReActivate was not successful. " + message );
                                EmailManager.NotifyAdmin( thisClassName + ".Save Failed Failed", message );
                            }
                        
                        }
                    }
                    
            }
            }
            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".ReActivate. DataSetProfile CTID: {0}", dspCTID ) );
                status.AddError( thisClassName + $".Save(). Error - the ReActivate was not successful for DataSetProfile CTID: {dspCTID}. " + message );
            }

        }
        public void ReActivate( int dspId, ref SaveStatus status )
        {
            if ( dspId < 1 )
                return;
            ThisResource entity = new ThisResource();
            int count = 0;
            DateTime lastUpdated = System.DateTime.Now;
			var dspCTID = string.Empty;
            try
            {
                using ( var context = new EntityContext() )
                {
                    context.Configuration.LazyLoadingEnabled = false;
                    DBEntity efEntity = context.DataSetProfile
                            .FirstOrDefault( s => s.Id == dspId );
                    if ( efEntity.Id > 0 )
                    {
                        //probably ok to ignore pending
                        if ( efEntity.EntityStateId == 0 )
                        {
                            efEntity.EntityStateId = 3;
							dspCTID = efEntity.CTID;
                            count = context.SaveChanges();
                            //can be zero if no data changed
                            if ( count >= 0 )
                            {
                                MapFromDB( efEntity, entity, false, false );
                                //may not be necessary, as may have been reactivated in a trigger
                                UpdateEntityCache( entity, ref status );
                            }
                            else
                            {
                                //?no info on error

                                string message = string.Format( thisClassName + ".ReActivate Failed", "Attempted to ReActivate a DataSetProfile . The process appeared to not work, but we have no message, or no clue. DataSetProfile CTID: {0}", efEntity.CTID );
                                status.AddError( "Error - the ReActivate was not successful. " + message );
                                EmailManager.NotifyAdmin( thisClassName + ".Save Failed Failed", message );
                            }

                        }
                    }

                }
            }
            catch ( Exception ex )
            {
                string message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".ReActivate. DataSetProfile CTID: {0}", dspCTID ) );
                status.AddError( thisClassName + $".Save(). Error - the ReActivate was not successful for DataSetProfile CTID: {dspCTID}. " + message );
            }

        }

        /// <summary>
        /// add a DataSetProfile
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        private int Add( ThisResource entity, Entity parentEntity, ref SaveStatus status )
		{
			DBEntity efEntity = new DBEntity();
			using ( var context = new EntityContext() )
			{
				try
				{
					MapToDB( entity, efEntity );

					if ( IsValidGuid( entity.RowId ) )
						efEntity.RowId = entity.RowId;
					else
						efEntity.RowId = Guid.NewGuid();
					efEntity.EntityStateId = entity.EntityStateId = 3;
					if ( IsValidDate( status.EnvelopeCreatedDate ) )
					{
						efEntity.Created = status.LocalCreatedDate;
						efEntity.LastUpdated = status.LocalCreatedDate;
					}
					else
					{
						efEntity.Created = System.DateTime.Now;
						efEntity.LastUpdated = System.DateTime.Now;
					}

					context.DataSetProfile.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						entity.RowId = efEntity.RowId;
						entity.Created = ( DateTime )efEntity.Created;
						entity.LastUpdated = ( DateTime )efEntity.LastUpdated;
						entity.Id = efEntity.Id;
						//entity.EntityStateId = 3;
						//
						UpdateEntityCache( entity, ref status );

						//
						if ( parentEntity != null )
						{
							new Entity_DataSetProfileManager().Add( parentEntity, entity.Id, ref status );
						}
						else
						{
							//add log entry
							SiteActivity sa = new SiteActivity()
							{
								ActivityType = "DataSetProfile",
								Activity = "Import",
								Event = "Add",
								Comment = string.Format( "Full DataSetProfile was added by the import. CTID: {0}, Source: {1}", entity.CTID, entity.Source ),
								ActivityObjectId = entity.Id
							};
							new ActivityManager().SiteActivityAdd( sa );
						}
						//
						if ( UpdateParts( entity, ref status ) == false )
						{
						}

						return efEntity.Id;
					}
					else
					{
						//?no info on error

						string message = thisClassName + string.Format( ". Add Failed", "Attempted to add a DataSetProfile. The process appeared to not work, but was not an exception, so we have no message, or no clue. DataSetProfile: {0}, ctid: {1}", entity.Name, entity.CTID );
						status.AddError( thisClassName + ". Error - the add was not successful. " + message );
						EmailManager.NotifyAdmin( "DataSetProfileManager. Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "DataSetProfile" );
					status.AddError( thisClassName + ".Add(). Error - the save was not successful. " + message );

					LoggingHelper.LogError(dbex, message);
				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), CTID: {0}\r\n", efEntity.CTID ) );
					status.AddError( thisClassName + ".Add(). Error - the save was not successful. \r\n" + message );
				}
			}

			return efEntity.Id;
		}
		public int AddPendingRecord( Guid entityUid, string ctid, string registryAtId, ref SaveStatus status )
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
					//	this will miss deleted. What are the implications of two records with same ctid?
					//		the adp could be referring to the deleted one? maybe not
					//	- could check for deleted here, and reset to pending?
					ThisResource entity = GetByCtid( ctid, false, true );
					if ( entity != null && entity.Id > 0 )
						return entity.Id;

					//only add DB required properties
					//NOTE - an entity will be created via trigger
					efEntity.Name = "Placeholder until full document is downloaded";
					efEntity.Description = "Placeholder until full document is downloaded";
					efEntity.EntityStateId = 1;
					efEntity.RowId = entityUid;
					//watch that Ctid can be  updated if not provided now!!
					efEntity.CTID = ctid;
					efEntity.Source = registryAtId;

					efEntity.Created = System.DateTime.Now;
					efEntity.LastUpdated = System.DateTime.Now;

					context.DataSetProfile.Add( efEntity );
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						SiteActivity sa = new SiteActivity()
						{
							ActivityType = EntityType,
							Activity = "Import",
							Event = string.Format( "Add Pending {0}", EntityType ),
							Comment = string.Format( "Pending {0} was added by the import. ctid: {1}, registryAtId: {2}", EntityType, ctid, registryAtId ),
							ActivityObjectId = efEntity.Id
						};
						new ActivityManager().SiteActivityAdd( sa );
						//Question should this be in the EntityCache?
						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						entity.CTID = efEntity.CTID;
						entity.EntityStateId = 1;
						entity.Name = efEntity.Name;
						entity.Description = efEntity.Description;
						entity.Source = efEntity.Source;
						entity.Created = ( DateTime )efEntity.Created;
						entity.LastUpdated = ( DateTime )efEntity.LastUpdated;
						UpdateEntityCache( entity, ref status );
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
				EntityStateId = document.EntityStateId,
				EntityUid = document.RowId,
				BaseId = document.Id,
				Description = document.Description,
				SubjectWebpage = document.Source,
				CTID = document.CTID,
				Created = document.Created,
				LastUpdated = document.LastUpdated,
				//ImageUrl = document.ImageUrl,
				Name = string.IsNullOrWhiteSpace(document.Name) ? "Dataset Profile": document.Name,
				OwningAgentUID = document.DataProviderUID,
				OwningOrgId = document.DataProviderId
			};
			var statusMessage = string.Empty;
			if ( new EntityManager().EntityCacheSave( ec, ref statusMessage ) == 0 )
			{
				status.AddError( thisClassName + string.Format( ".UpdateEntityCache. EntityType{0}, document.CTID: {1}, document.Id: {2}. Failed: {3} ", EntityType, document.CTID, document.Id, statusMessage ) );
			}
		}

		public void UpdateEntityCache( DBEntity document, ref SaveStatus status )
		{
			EntityCache ec = new EntityCache()
			{
				EntityTypeId = EntityTypeId,
				EntityType = EntityType,
				EntityStateId = document.EntityStateId,
				EntityUid = document.RowId,
				BaseId = document.Id,
				Description = document.Description,
				SubjectWebpage = document.Source,
				CTID = document.CTID,
				Created = (DateTime)document.Created,
				LastUpdated = ( DateTime ) document.LastUpdated,
				//ImageUrl = document.ImageUrl,
				Name = document.Name,
				OwningAgentUID = (Guid)document.DataProviderUID,
				OwningOrgId = 0
			};
			var statusMessage = string.Empty;
			if ( new EntityManager().EntityCacheSave( ec, ref statusMessage ) == 0 )
			{
				status.AddError( thisClassName + string.Format( ".UpdateEntityCache. EntityType{0}, document.CTID: {1}, document.Id: {2}. Failed: {3} ", EntityType, document.CTID, document.Id, statusMessage ) );
			}
		}

		/// <summary>
		/// Parts:
		/// - Jurisdiction
		/// - DataSetProfile
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool UpdateParts( ThisResource entity, ref SaveStatus status )
		{
			bool isAllValid = true;
			Entity resourceEntity = EntityManager.GetEntity( entity.RowId );
			if ( resourceEntity == null || resourceEntity.Id == 0 )
			{
				status.AddError( "Error - the related Entity was not found." );
				return false;
			}
			//will have either AboutUids or RelevantDataSetForUids
			//may not have to be concerned with //will have either AboutUids or RelevantDataSetForUids??
			//About
			HandleAbout( resourceEntity, entity, ref status );
			//
			Entity_AgentRelationshipManager mgr = new Entity_AgentRelationshipManager();
			//do deletes - should this be done here, should be no other prior updates?
			mgr.DeleteAll( resourceEntity, ref status );
			mgr.SaveList( resourceEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY, entity.PublishedBy, ref status );
			//need to store provided by for the search. NEW: ROLE_TYPE_PROVIDED_BY
			mgr.Save( resourceEntity.Id, entity.DataProviderUID, Entity_AgentRelationshipManager.ROLE_TYPE_PROVIDED_BY, ref status );

			//ProcessProfile
			Entity_ProcessProfileManager ppm = new Factories.Entity_ProcessProfileManager();
			ppm.DeleteAll( resourceEntity, ref status );
			try
			{
				ppm.SaveList( entity.AdministrationProcess, Entity_ProcessProfileManager.ADMIN_PROCESS_TYPE, entity.RowId, ref status );
			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".AddProfiles() - ProcessProfiles. id: {0}", entity.Id ) );
				status.AddWarning( thisClassName + ".AddProfiles(). Exceptions encountered handling ProcessProfiles. " + message );
			}
			//
			Entity_ReferenceFrameworkManager erfm = new Entity_ReferenceFrameworkManager();
			erfm.DeleteAll( resourceEntity, ref status );
			if ( erfm.SaveList( resourceEntity.Id, CodesManager.PROPERTY_CATEGORY_CIP, entity.InstructionalProgramTypes, ref status ) == false )
				isAllValid = false;

			//JurisdictionProfile 
			Entity_JurisdictionProfileManager jpm = new Entity_JurisdictionProfileManager();
			//do deletes - NOTE: other jurisdictions are added in: UpdateAssertedIns
			jpm.DeleteAll( resourceEntity, ref status );
			jpm.SaveList( entity.Jurisdiction, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status );

            //DataSetTimeFrames
            new DataSetTimeFrameManager().SaveList( entity.DataSetTimePeriod, entity.Id, ref status );


			return isAllValid;
		}

		/// <summary>
		/// Manage About relationships
		/// 23-02-12 Updated to NOT create these if an Entity.AggregateDataProfile relationship exists!
		/// </summary>
		/// <param name="resourceEntity"></param>
		/// <param name="entity"></param>
		/// <param name="status"></param>
		public void HandleAbout( Entity resourceEntity, ThisResource entity, ref SaveStatus status )
		{
			
			if ( entity.AboutUids == null || entity.AboutUids.Count == 0 )
				return;
			//clear
			Entity_CredentialManager ecm = new Entity_CredentialManager();
			ecm.DeleteAll( resourceEntity, ref status );
			//
			var eam = new Entity_AssessmentManager();
			eam.DeleteAll( resourceEntity, ref status );
			//
			var elom = new Entity_LearningOpportunityManager();
			elom.DeleteAll( resourceEntity, ref status );

			//Entity_AssertionManager eaMgr = new Entity_AssertionManager();
			//eaMgr.DeleteAll( resourceEntity, ref status );

			//23-02-10 mp - is this reversed? May not really matter as processed correctly
			//Having 
			//		DSP
			//			Entity
			//				Entity.Lopp (etc.)
			//versus
			//		Lopp
			//			Entity
			//				Entity.HasDsp	?????
			foreach ( var item in entity.AboutUids )
			{
				var ec = EntityManager.EntityCacheGetByGuid( item );
				if ( ec != null && ec.Id > 0 )
				{
                    //first check for ADP relationship. Confirm if this is always true. It is for the ProPath scenario. If there is an independent dsp, it will not be referenced from the lopp.eadp
                    //	entity.adp (ec.Id=entity.adp.EntityId) -> adpEntity(entity.entityUID=adp.RowId) -> e.DataSetProfile(adpEntity.Id=adsp.EntityId).DataSetProfileId = this Dsp.Id
					if (Entity_AggregateDataProfileManager.ReferencesDataSetProfile(ec.Id, entity.Id))
					{
						//hmm, maybe shouldn't do this? There are aleady checks in the detail map from db methods
						continue;
					}
					//TODO - could just use Entity.HasResource
                    int newId = 0;
					switch ( ec.EntityTypeId )
					{
						case 1:
							//simple hasPart
							ecm.Add( resourceEntity.EntityUid, ec.BaseId, 1, ref newId, ref status );
							if ( newId > 0 )
							{
								entity.CredentialIds.Add( ec.BaseId );
								//eaMgr.Save( resourceEntity.Id, 1, newId, Entity_AgentRelationshipManager.ROLE_TYPE_PROVIDES_OUTCOMES, ref status );
							}
							break;
						case 3:
							//simple hasPart
							eam.Add( resourceEntity.EntityUid, ec.BaseId, 1, false, ref status );
							if ( newId > 0 )
							{
								entity.AssessmentIds.Add( ec.BaseId );
								//eaMgr.Save( resourceEntity.Id, 3, newId, Entity_AgentRelationshipManager.ROLE_TYPE_PROVIDES_OUTCOMES, ref status );
							}
							break;
						case 7:
						case 36:
						case 37:                          
                            elom.Add( resourceEntity.EntityUid, ec.BaseId, 1, false, ref status );
							if ( newId > 0 )
							{
								entity.LearningOpportunityIds.Add( ec.BaseId );
								//eaMgr.Save( resourceEntity.Id, 7, newId, Entity_AgentRelationshipManager.ROLE_TYPE_PROVIDES_OUTCOMES, ref status );
							}
							break;
						default:

							break;
					}
				}
				else
				{
					status.AddError( string.Format( "DataSetProfileManager.HandleAbout. An EntityCache record was not found for GUID: {0}", item ) );
					//then a pending record should be created. Could be the pending record was not added to the cache!
				}
			}
			//About - will need a method to get the type, and then add to Entity.Credential, E.Assessment, etc.
			//mgr.SaveList( resourceEntity.Id, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERS, entity.AboutUids, ref status );
		}

		public bool ValidateProfile( ThisResource profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;
			if ( string.IsNullOrWhiteSpace( profile.Description ) )
			{
				//status.AddWarning( "An DataSetProfile Description must be entered" );
			}


			return status.WasSectionValid;
		}
		/// <summary>
		/// Delete all profiles for parent
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool DeleteAll( Guid parentUid, ref List<string> messages )
		{
			bool isValid = true;
			int count = 0;
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				messages.Add( thisClassName + ".DeleteAll Error - the provided target parent entity was not provided." );
				return false;
			}

			try
			{
				using ( var context = new EntityContext() )
				{
					//check if target is a reference object and is only in use here
					var results = context.Entity_DataSetProfile
								.Where( s => s.EntityId == parent.Id )
								.OrderBy( s => s.Created )
								.ToList();
					if ( results == null || results.Count == 0 )
					{
						return true;
					}

					foreach ( var item in results )
					{
						//need to check if reference by other entities
						var exists = context.Entity_DataSetProfile
							.Where( s => s.EntityId != parent.Id && s.DataSetProfileId == item.DataSetProfileId )
							.ToList();
						if ( exists != null && exists.Any() )
						{
							//only removeEntity_DataSetProfile
							context.Entity_DataSetProfile.Remove( item );
							count = context.SaveChanges();
							if ( count > 0 )
							{

							}
						}
						else if ( item.DataSetProfile != null && item.DataSetProfile.Id > 0 )
						{
							//this will delete the Entity_DataSetProfile as well.

							Delete( item.DataSetProfile.Id, ref messages );
						}
						else //unlikely, but remove orphan Entity_DataSetProfile
						{
							context.Entity_DataSetProfile.Remove( item );
							count = context.SaveChanges();
							if ( count > 0 )
							{

							}
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".DeleteAll" );
				messages.Add( ex.Message );
			}
			return isValid;
		}
		/// <summary>
		/// Delete profile 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int id, ref List<string> messages )
		{
			bool isValid = true;
			if ( id < 1 )
			{
				messages.Add( thisClassName + ".Delete() Error - a valid dataSet profile id must be provided." );
				return false;
			}

			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					DBEntity efEntity = context.DataSetProfile
								.FirstOrDefault( s => s.Id == id );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						Guid rowId = efEntity.RowId;
						//need to remove from Entity.

						//need to remove timeframe 
						new DataSetTimeFrameManager().DeleteAll( efEntity.Id, ref messages );

						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " DataSetProfile. Id: {0}, Ctid: {1}.", efEntity.Id, efEntity.CTID );
						//21-03-31 mp - just removing the profile will not remove its entity and the latter's children!
						string statusMessage = string.Empty;
						new EntityManager().Delete( rowId, string.Format( "DataSetProfile: {0}", id ), ref statusMessage );

						//
						context.DataSetProfile.Remove( efEntity );
						//efEntity.EntityStateId = 0;
						//efEntity.LastUpdated = System.DateTime.Now;
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							new ActivityManager().SiteActivityAdd( new SiteActivity()
							{
								ActivityType = "DataSetProfile",
								Activity = "Import",
								Event = "Delete",
								Comment = msg,
								ActivityObjectId = efEntity.Id
							} );
							isValid = true;
						}
					}
					else
					{
						messages.Add( thisClassName + ".Delete() Warning No action taken, as the record was not found." );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + ".Delete(id)" );
					isValid = false;
					var statusMessage = FormatExceptions( ex );
					if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
					{
						statusMessage = "Error: this DataSetProfile cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this DataSetProfile can be deleted.";
					}
					messages.Add( statusMessage );
				}
			}
			return isValid;
		}
		public bool Delete( string ctid, ref List<string> messages )
		{
			//List<string> messages = new List<string>();
			bool isValid = true;
			if ( string.IsNullOrWhiteSpace( ctid ) || ctid.Length != 39 )
			{
				messages.Add( thisClassName + ".Delete() Error - a valid dataSet profile id must be provided." );
				return false;
			}

			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					DBEntity efEntity = context.DataSetProfile
								.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						Guid rowId = efEntity.RowId;
						//need to remove timeframe 
						new DataSetTimeFrameManager().DeleteAll( efEntity.Id, ref messages );

						//-using before delete trigger - verify won't have RI issues
						string msg = string.Format( " DataSetProfile. Id: {0}, Ctid: {1}.", efEntity.Id, efEntity.CTID );
						//21-03-31 mp - just removing the profile will not remove its entity and the latter's children!
						//need to remove from Entity.
						//handled by trgDataSetProfileAfterDelete
						//string statusMessage = string.Empty;
						//new EntityManager().Delete( rowId, string.Format( "DataSetProfile: {0}", id ), ref statusMessage );

						//
						context.DataSetProfile.Remove( efEntity );
						//efEntity.EntityStateId = 0;
						//efEntity.LastUpdated = System.DateTime.Now;
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							new ActivityManager().SiteActivityAdd( new SiteActivity()
							{
								ActivityType = "DataSetProfile",
								Activity = "Import",
								Event = "Delete",
								Comment = msg,
								ActivityObjectId = efEntity.Id
							} );
							isValid = true;
							//delete cache
							var statusMessage = string.Empty;
							new EntityManager().EntityCacheDelete( rowId, ref statusMessage );
						}
					}
					else
					{
						messages.Add( thisClassName + ".Delete() Warning No action taken, as the record was not found." );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + ".Delete(id)" );
					isValid = false;
					var statusMessage = FormatExceptions( ex );
					if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
					{
						statusMessage = "Error: this DataSetProfile cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this DataSetProfile can be deleted.";
					}
					messages.Add( statusMessage );
				}
			}
			return isValid;
		}

		#endregion



		#region == Retrieval =======================
		/// <summary>
		/// Get all dataset profiles for a parent entity
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="includingParts"></param>
		/// <param name="isAPIRequest"></param>
		/// <returns></returns>
		public static List<ThisResource> GetAll( Guid parentUid, bool includingParts = true, bool isAPIRequest = false )
		{
			var list = new List<ThisResource>();
			var entity = new ThisResource();

			Entity parent = EntityManager.GetEntity( parentUid );
			LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".GetAll: parentUid:{0} entityId:{1}, parent.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );

			try
			{
				using ( var context = new EntityContext() )
				{
					var results = context.Entity_DataSetProfile
							.Where( s => s.EntityId == parent.Id && s.DataSetProfile.EntityStateId == 3 )
							.OrderBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( var item in results )
						{
							entity = new ThisResource();

							//need to distinguish between on a detail page for conditions and Holders detail
							//would usually only want basics here??
							//17-05-26 mp- change to MapFromDB_Basic
							if ( item.DataSetProfile != null && item.DataSetProfile.EntityStateId > 1 )
							{
								MapFromDB( item.DataSetProfile, entity, includingParts, isAPIRequest );
								list.Add( entity );
							}
						}
					}
					return list;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAll" );
			}
			return list;
		}

		/// <summary>
		/// Get all dataset profiles for the data owner
		/// NOTE: this may not be ideal for showing on a page. More likely to use a search?
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="includingParts"></param>
		/// <param name="isAPIRequest"></param>
		/// <returns></returns>
		public static List<ThisResource> GetAllForDataOwner( Guid dataOwnerUid, bool includingParts = true, bool isAPIRequest = false )
		{
			var list = new List<ThisResource>();
			var entity = new ThisResource();

			try
			{
				using ( var context = new EntityContext() )
				{
					var results = context.DataSetProfile
							.Where( s => s.DataProviderUID == dataOwnerUid && s.EntityStateId == 3 )
							.OrderBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( var item in results )
						{
							entity = new ThisResource();
							if ( item != null && item.EntityStateId > 1 )
							{
								MapFromDB( item, entity, includingParts, isAPIRequest );
								list.Add( entity );
							}
						}
					}
					return list;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAllForDataOwner" );
			}
			return list;
		}
		//
		public static int GetCountForDataOwner( Guid dataOwnerUid )
		{
			var list = new List<ThisResource>();

			try
			{
				using ( var context = new EntityContext() )
				{
					var results = context.DataSetProfile
							.Where( s => s.DataProviderUID == dataOwnerUid && s.EntityStateId == 3 )
							.OrderBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						return results.Count;
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetCountForDataOwner" );
			}
			return 0;
		}
		/// <summary>
		/// get basic record - no dataset time periods
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static ThisResource Get( int id, bool includingParts = true, bool isAPIRequest = false )
		{
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.DataSetProfile
						.FirstOrDefault( s => s.Id == id && s.EntityStateId == 3 );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, includingParts, isAPIRequest );
				}
			}

			return entity;
		}
		public static ThisResource GetByCtid( string ctid, bool includingParts = true, bool allowPendingState = false )
		{
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.DataSetProfile
						.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() && ( s.EntityStateId == 3 || ( allowPendingState && s.EntityStateId == 1 ) ));

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, includingParts, false );
				}
			}

			return entity;
		}

		public static List<TopLevelObject> GetAllDataSetCredentials( int orgId, int maxRecords, bool isAPIRequest = false )
		{
			var list = new List<TopLevelObject>();
			var entity = new TopLevelObject();

			//Entity parent = EntityManager.GetEntity( parentUid );
			//LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );

			try
			{
				using ( var context = new EntityContext() )
				{
					var list1 = from org in context.Organization
								join dsp in context.DataSetProfile on org.RowId equals dsp.DataProviderUID
								join dspEntity in context.Entity on dsp.RowId equals dspEntity.EntityUid
								join entityCred in context.Entity_Credential on dspEntity.Id equals entityCred.EntityId
								join cred in context.Credential on entityCred.CredentialId equals cred.Id
								join codes in context.Codes_PropertyValue on cred.CredentialTypeId equals codes.Id
								where org.Id == orgId && dsp.EntityStateId == 3 && cred.EntityStateId > 1
								select new
								{
									cred.Id,
									cred.Name,
									codes.Title,
									cred.SubjectWebpage,
									cred.CTID,
									cred.ImageUrl
								};

					var results = list1.Take( maxRecords * 3 ).OrderBy( s => s.Id ).ToList();
					if ( results != null && results.Count > 0 )
					{
						var prevId = 0;
						foreach ( var item in results )
						{
							if ( item.Id == prevId )
								continue;
							entity = new TopLevelObject()
							{
								Id = item.Id,
								Name = item.Name,
								SubjectWebpage = item.SubjectWebpage,
								CTID = item.CTID,
								Image = item.ImageUrl
							};
							list.Add( entity );
							prevId = item.Id;
						}

						list = list.Take( maxRecords ).ToList();
					}
					return list;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAllDataSetCredentials" );
			}
			return list;
		}


		public static void MapToDB( ThisResource input, DBEntity output )
		{

			//want output ensure fields input create are not wiped
			if ( output.Id == 0 )
			{
				output.CTID = input.CTID;
			}
			//if ( !string.IsNullOrWhiteSpace( input.CredentialRegistryId ) )
			//	output.CredentialRegistryId = input.CredentialRegistryId;

			//output.Id = input.Id;
			//output.EntityStateId = input.EntityStateId;
			output.Description = GetData( input.Description );
			output.Name = GetData( input.Name );
			output.Source = GetUrlData( input.Source );
			output.DataSuppressionPolicy = input.DataSuppressionPolicy;
			output.SubjectIdentification = input.SubjectIdentification;
			if ( input.DataProviderUID == null || input.DataProviderUID.ToString() == DEFAULT_GUID )
			{
				output.DataProviderUID = null;//			
			}
			else
			{
				output.DataProviderUID = input.DataProviderUID;
			}
			//
			if ( input.DistributionFile != null && input.DistributionFile.Any() )
			{
				output.DistributionFile = string.Join( "|", input.DistributionFile );
			}
			else
				output.DistributionFile = null;
			output.DataSetTimePeriodJson = input.DataSetTimePeriodJson;

		}

		public static void MapFromDB( DBEntity input, ThisResource output, bool includingParts, bool isAPIRequest )
		{

			output.Id = input.Id;
			output.RowId = input.RowId;
			output.Name = input.Name;
			output.EntityStateId = input.EntityStateId;
			output.Description = input.Description == null ? string.Empty : input.Description;
			output.CTID = input.CTID;
			output.CredentialRegistryId = input.CredentialRegistryId;
			output.DataSuppressionPolicy = input.DataSuppressionPolicy;
			output.SubjectIdentification = input.SubjectIdentification;
			output.Source = GetUrlData( input.Source );
			//=====
			var resourceEntity = EntityManager.GetEntity( output.RowId, false );
			if ( resourceEntity != null && resourceEntity.Id > 0 )
				output.EntityLastUpdated = resourceEntity.LastUpdated;
			//NOTE: EntityLastUpdated should really be the last registry update now. Check how LastUpdated is assigned on import
			output.EntityLastUpdated = output.LastUpdated;
			//
			if ( IsGuidValid( input.DataProviderUID ) )
			{
				output.DataProviderUID = ( Guid )input.DataProviderUID;
				//21-05-16 mp - not being displayed, so try defaulting to use Outline
				output.DataProviderOld = OrganizationManager.GetForSummary( output.DataProviderUID );
				if ( output.DataProviderOld != null && output.DataProviderOld.Id > 0 )
				{
					output.DataProviderId = output.DataProviderOld.Id;
					output.DataProviderName = output.DataProviderOld.Name;
					output.DataProvider = new Models.API.Outline()
					{
						Description = output.DataProviderOld.Description,
						Meta_Id= output.DataProviderOld.Id,
						Label = output.DataProviderOld.Name,
						Image = output.DataProviderOld.Image,
						URL = FormatExternalFinderUrl( "organization", output.DataProviderOld.CTID, output.DataProviderOld.SubjectWebpage, output.DataProviderOld.Id, output.DataProviderOld.FriendlyName )
					};

					output.PrimaryOrganization = output.DataProviderOld;

                }
				if ( isAPIRequest )
					output.DataProviderOld = null;
			}
			else
				output.DataProviderOld = null;
			//this will get ProvidedBy, which is covered by DataProviderUID, so may not need?
			//output.OrganizationRole = Entity_AgentRelationshipManager.AgentEntityRole_GetAll_ToEnumeration( output.RowId, true );

			//
			output.InstructionalProgramTypes = Reference_FrameworkItemManager.FillCredentialAlignmentObject( output.RowId, CodesManager.PROPERTY_CATEGORY_CIP );

			//output.InstructionalProgramType = Reference_FrameworkItemManager.FillEnumeration( output.RowId, CodesManager.PROPERTY_CATEGORY_CIP );
			//
			if ( input.DistributionFile != null )
			{
				var list = input.DistributionFile.Split( '|' );
				foreach ( var item in list )
				{
					if ( !string.IsNullOrWhiteSpace( item ) )
						output.DistributionFile.Add( item );
				}
			}
			else
				output.DistributionFile = new List<string>();

			//
			if ( !isAPIRequest )
			{
				if ( IsValidDate( input.Created ) )
					output.Created = ( DateTime )input.Created;

			}
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime )input.LastUpdated;

			output.AboutInternal = new List<TopLevelEntityReference>();
			output.About = new List<Models.API.Outline>();
            output.DataSetTimePeriodJson = input.DataSetTimePeriodJson;
            if ( !includingParts )
				return;
			//
			//if ( string.IsNullOrWhiteSpace( output.CTID ) || output.EntityStateId < 3 )
			//{
			//	//not possible
			//	output.IsReferenceVersion = true;
			//	return;
			//}
			//================================================================================

			//ABOUT: start with credentials
			var creds = Entity_CredentialManager.GetAll( output.RowId, RELATIONSHIP_TYPE_HAS_PART );
			if ( creds != null && creds.Any() )
			{
				//format url for later

				foreach ( var item in creds )
				{
					if ( isAPIRequest )
					{
						var about = new Models.API.Outline()
						{
							OutlineType = "Credential",
							Label = item.Name,
							Description = item.Description,
							Image = item.Image,
							URL = FormatExternalFinderUrl( "credential", item.CTID, item.SubjectWebpage, item.Id, item.FriendlyName ),
							Tags = new List<Models.API.LabelLink>() { new Models.API.LabelLink()
							{
								 Label=item.CredentialType
							} },
						};
						if ( item.PrimaryOrganization != null && item.PrimaryOrganization.Id > 0 )
						{
							about.Provider = new Models.API.Outline()
							{
								Label = item.PrimaryOrganization.Name,
								Meta_Id = item.PrimaryOrganization.Id,
							};
							//21-09-13 mp - not sure why I was using reactFinderSiteURL below
							about.Provider.URL = FormatDetailUrl( "organization", item.PrimaryOrganization.CTID, item.PrimaryOrganization.SubjectWebpage, item.PrimaryOrganization.Id, item.PrimaryOrganization.FriendlyName );

							//if ( !string.IsNullOrWhiteSpace( item.OwningOrganization.CTID ) )
							//	about.Provider.URL = reactFinderSiteURL + "organization/" + item.OwningOrganization.Id;
							//else if ( !string.IsNullOrWhiteSpace( item.OwningOrganization.SubjectWebpage ) )
							//	about.Provider.URL = item.OwningOrganization.SubjectWebpage;
						}
						output.About.Add( about );
						output.AboutInternal = null;
					}
					else
					{
						output.AboutInternal.Add( new TopLevelEntityReference()
						{
							Id = item.Id,
							Name = item.Name,
							Description = item.Description,
							SubjectWebpage = item.SubjectWebpage,
							CTID = item.CTID,
							Image = item.Image,
							EntityType = "Credential",
							EntityTypeId = 1,
							DetailURL = FormatDetailUrl( "credential", item.CTID, item.SubjectWebpage, item.Id, item.FriendlyName )//can do this later
						} );
					}
				}
			}
			//
			var lopps = Entity_LearningOpportunityManager.GetAllSummary( output.RowId, RELATIONSHIP_TYPE_HAS_PART );
			if ( lopps != null && lopps.Any() )
			{
				//seems like a lot of work for this
				foreach ( var item in lopps )
				{
					if ( isAPIRequest )
					{
						var about = new Models.API.Outline()
						{
							OutlineType = "LearningOpportunity",
							Label = item.Name,
							Description = item.Description,
							URL = FormatExternalFinderUrl( item.EntityType, item.CTID, item.SubjectWebpage, item.Id, item.FriendlyName ),
							Tags = new List<Models.API.LabelLink>() { new Models.API.LabelLink()
							{
								 Label=item.EntityType
							} },
						};
						if ( item.PrimaryOrganization != null && item.PrimaryOrganization.Id > 0 )
						{
							about.Provider = new Models.API.Outline()
							{
								Label = item.PrimaryOrganization.Name,
								Meta_Id = item.PrimaryOrganization.Id,
							};
							//21-09-13 mp - not sure why I was using reactFinderSiteURL below
							about.Provider.URL = FormatDetailUrl( "organization", item.PrimaryOrganization.CTID, item.PrimaryOrganization.SubjectWebpage, item.PrimaryOrganization.Id, item.PrimaryOrganization.FriendlyName );

							//if ( !string.IsNullOrWhiteSpace( item.OwningOrganization.CTID ) )
							//	about.Provider.URL = reactFinderSiteURL + "organization/" + item.OwningOrganization.Id;
							//else if ( !string.IsNullOrWhiteSpace( item.OwningOrganization.SubjectWebpage ) )
							//	about.Provider.URL = item.OwningOrganization.SubjectWebpage;
						}
						output.About.Add( about );
						output.AboutInternal = null;
					}
					else
					{
						output.AboutInternal.Add( new TopLevelEntityReference()
						{
							Id = item.Id,
							Name = item.Name,
							Description = item.Description,
							SubjectWebpage = item.SubjectWebpage,
							CTID = item.CTID,
							EntityType = item.EntityType,
							EntityTypeId = item.EntityTypeId,
							DetailURL = FormatDetailUrl( item.EntityType, item.CTID, item.SubjectWebpage, item.Id, item.FriendlyName )//can do this later
						} );
					}
				}
			}
			//
			var asmts = Entity_AssessmentManager.GetAllSummary( output.RowId, RELATIONSHIP_TYPE_HAS_PART );
			if ( lopps != null && lopps.Any() )
			{
				//seems like a lot of work for this
				foreach ( var item in asmts )
				{
					if ( isAPIRequest )
					{
						var about = new Models.API.Outline()
						{
							OutlineType = "Assessment",
							Label = item.Name,
							Description = item.Description,
							URL = FormatExternalFinderUrl( item.EntityType, item.CTID, item.SubjectWebpage, item.Id, item.FriendlyName ),
							Tags = new List<Models.API.LabelLink>() { new Models.API.LabelLink()
							{
								 Label=item.EntityType
							} },
						};
						if ( item.PrimaryOrganization != null && item.PrimaryOrganization.Id > 0 )
						{
							about.Provider = new Models.API.Outline()
							{
								Label = item.PrimaryOrganization.Name,
								Meta_Id = item.PrimaryOrganization.Id,
							};
							about.Provider.URL = FormatDetailUrl( "organization", item.PrimaryOrganization.CTID, item.PrimaryOrganization.SubjectWebpage, item.PrimaryOrganization.Id, item.PrimaryOrganization.FriendlyName );

							//if ( !string.IsNullOrWhiteSpace( item.OwningOrganization.CTID ) )
							//	about.Provider.URL = reactFinderSiteURL + "organization/" + item.OwningOrganization.Id;
							//else if ( !string.IsNullOrWhiteSpace( item.OwningOrganization.SubjectWebpage ) )
							//	about.Provider.URL = item.OwningOrganization.SubjectWebpage;
						}
						output.About.Add( about );
						output.AboutInternal = null;
					}
					else
					{
						output.AboutInternal.Add( new TopLevelEntityReference()
						{
							Id = item.Id,
							Name = item.Name,
							Description = item.Description,
							SubjectWebpage = item.SubjectWebpage,
							CTID = item.CTID,
							EntityType = item.EntityType,
							EntityTypeId = item.EntityTypeId,
							DetailURL = FormatDetailUrl( item.EntityType, item.CTID, item.SubjectWebpage, item.Id, item.FriendlyName )//can do this later
						} );
					}
				}
			}



			//components
			if ( includingParts )
			{
				var processProfiles = Entity_ProcessProfileManager.GetAll( input.RowId );
				foreach ( ProcessProfile item in processProfiles )
				{
					if ( item.ProcessTypeId == Entity_ProcessProfileManager.ADMIN_PROCESS_TYPE )
						output.AdministrationProcess.Add( item );
				}
				//
				output.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId );
				//get DataSetTimePeriod
				output.DataSetTimePeriod = DataSetTimeFrameManager.GetAll( output.Id );

			}
		} //

		public static List<ThisResourceSummary> Search( string pFilter, string pOrderBy, int pageNumber, int pageSize, ref int pTotalRows, bool autocomplete = false )
		{
			string connectionString = DBConnectionRO();
			var item = new ThisResourceSummary();
			var list = new List<ThisResourceSummary>();
			var result = new DataTable();

			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				if ( string.IsNullOrEmpty( pFilter ) )
				{
					pFilter = string.Empty;
				}

				using ( SqlCommand command = new SqlCommand( "[DataSetProfile.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", pFilter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", pOrderBy ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
					SqlParameter totalRows = new SqlParameter( "@TotalRows", pTotalRows );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );


					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						//string rows = command.Parameters[ 5 ].Value.ToString();
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
						pTotalRows = Int32.Parse( rows );
						if ( pTotalRows > 0 && result.Rows.Count == 0 )
						{
							//actual this can be a credential.Cache sync issue
							item = new ThisResourceSummary();
							item.Name = "Error: invalid page number. Or this could mean a record is not in the credential cache. ";
							item.Description = "Error: invalid page number. Select displayed page button only.";

							list.Add( item );
							return list;
						}
					}
					catch ( Exception ex )
					{
						pTotalRows = 0;
						LoggingHelper.LogError( ex, thisClassName + string.Format( ".Search() - Execute proc, Message: {0} \r\n Filter: {1} \r\n", ex.Message, pFilter ) );

						item = new ThisResourceSummary();
						item.Name = "Unexpected error encountered. System administration has been notified. Please try again later. ";
						item.Description = ex.Message;

						list.Add( item );
						return list;
					}
				}

				//Used for costs. Only need to get these once. See below. - NA 5/12/2017
				//var currencies = CodesManager.GetCurrencies();
				//var costTypes = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST );
				int cntr = 0;
				try
				{
					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr == 10 )
						{

						}
						//avgMinutes = 0;
						item = new ThisResourceSummary();
						//item.SearchRowNumber = GetRowColumn( dr, "RowNumber", 0 );
						item.Id = GetRowColumn( dr, "Id", 0 );

						//item.Name = GetRowColumn( dr, "Name", "missing" );
						item.Name = dr[ "Name" ].ToString();
						item.Description = dr["Description"].ToString();
						if (string.IsNullOrWhiteSpace(item.Name))
                        {
							//use description?
							item.Name = AssignLimitedString( item.Description );
						}
						//item.FriendlyName = FormatFriendlyTitle( item.Name );
						item.CTID = GetRowColumn( dr, "CTID" );
						if ( string.IsNullOrWhiteSpace( item.CTID ) )
							item.IsReferenceVersion = true;

						item.DataProviderId = GetRowPossibleColumn( dr, "DataProviderId", 0 );
						item.DataProviderName = GetRowPossibleColumn( dr, "DataProviderName" );
						item.DataProviderCTID = GetRowPossibleColumn( dr, "DataProviderCTID" );
						if ( item.DataProviderId > 0 )
						{
							item.DataProvider = new Models.API.Outline()
							{
								Meta_Id = item.DataProviderId,
								Label = item.DataProviderName,
								//CTID = providingOrgCTID
							};
						}
						var agentUid = GetRowColumn( dr, "DataProviderUID" );
						if ( Guid.TryParse( agentUid, out Guid aUid ) )
						{
							item.DataProviderUID = aUid;
						}
						string rowId = GetRowColumn( dr, "RowId" );
						item.RowId = new Guid( rowId );


						try
						{
							var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
							if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
							{
								var resource = JsonConvert.DeserializeObject<APIM.DataSetProfile>( resourceDetail );
								item.ResourceDetail = resource.ToString();
							}
						}
						catch
						{
						}

						//for autocomplete, only need name
						if ( autocomplete )
						{
							list.Add( item );
							continue;
						}


						item.Source = dr[ "Source" ].ToString();
						//

						//

						DateTime testdate;
						//=====================================
						string date = GetRowPossibleColumn( dr, "EntityLastUpdated", string.Empty );
						if ( DateTime.TryParse( date, out testdate ) )
							item.EntityLastUpdated = testdate;

						//item.CredentialRegistryId = dr[ "CredentialRegistryId" ].ToString();

						//=====================================================================


						date = GetRowColumn( dr, "Created", string.Empty );
						if ( IsValidDate( date ) )
							item.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", string.Empty );
						if ( IsValidDate( date ) )
							item.LastUpdated = DateTime.Parse( date );

						list.Add( item );
					}

					return list;
				}
				catch ( Exception ex )
				{
					pTotalRows = 0;
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".BasicSearch() - Execute proc, Row: {0}, Message: {1} \r\n Filter: {2} \r\n", cntr, ex.Message, pFilter ) );

					item = new ThisResourceSummary();
					item.Name = "Unexpected error encountered. System administration has been notified. Please try again later. ";
					item.Description = ex.Message;
					list.Add( item );
					return list;
				}
			}
		}

		#endregion


	}
}
