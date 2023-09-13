using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.QData;
using ThisResource = workIT.Models.QData.Entity_DataSetProfile;
using DBEntity = workIT.Data.Tables.Entity_DataSetProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using workIT.Utilities;
using CM = workIT.Models.Common;
using workIT.Models.ProfileModels;
using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;

namespace workIT.Factories
{
	public class Entity_DataSetProfileManager : BaseFactory
	{
		static string thisClassName = "Entity_DataSetProfileManager";
		public static int RelationshipType_HasPart = 1;
        #region Entity_DataSetProfile Persistance ===================

        /// <summary>
        /// Save list of datasetProfile ids under Entity.DataSetProfile
        /// </summary>
        /// <param name="list"></param>
        /// <param name="parent"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool SaveList( List<int> list, Entity parent, ref SaveStatus status )
		{
			if ( list == null || list.Count == 0 )
				return true;
			int newId = 0;

			bool isAllValid = true;
			foreach ( int item in list )
			{
				newId = Add( parent, item, ref status );
				if ( newId == 0 )
					isAllValid = false;
			}

			return isAllValid;
		}

		/// <summary>
		/// Add an Entity_DataSetProfileManager
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="dataSetProfileId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public int Add( Entity parent,
					int dataSetProfileId,
					ref SaveStatus status )
		{
			int id = 0;
			int count = 0;
			if ( dataSetProfileId == 0 )
			{
				status.AddError( string.Format( "A valid identifier was not provided to the {0}.Entity_DataSetProfileManager Add method.", thisClassName ) );
				return 0;
			}

			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( "Error - the parent entity for Entity.DataSetProfile was not found." );
				return 0;
			}
			using ( var context = new EntityContext() )
			{
				DBEntity efEntity = new DBEntity();
				try
				{
					efEntity = context.Entity_DataSetProfile
							.FirstOrDefault( s => s.EntityId == parent.Id && s.DataSetProfileId == dataSetProfileId );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						return 0;
					}

					efEntity = new DBEntity();
					efEntity.EntityId = parent.Id;
					efEntity.DataSetProfileId = dataSetProfileId;
					efEntity.Created = System.DateTime.Now;
					//bleep: on import a dsp is found, and id is stored in relevantDSPs. then on delete of the adp, the dsp is also deleted, so at this point the dspId is invalid
					//Feb 2023. OK now. Changed the e.ADP trigger to do a virtual delete. have to reactivate the dsp
					context.Entity_DataSetProfile.Add( efEntity );

					// submit the change to database
					count = context.SaveChanges();
					if ( count > 0 )
					{
						//activate the DSP just in case
						new DataSetProfileManager().ReActivate( dataSetProfileId, ref status );
						id = efEntity.Id;
						return efEntity.Id;
					}
					else
					{
						//?no info on error
						status.AddError( thisClassName + "Error - the add was not successful." );
						string message = thisClassName + string.Format( ".Add Failed", "Attempted to add an Entity_DataSetProfileManager. The process appeared to not work, but there was no exception, so we have no message, or no clue. Parent EntityId: {0}, Type: {1}, dataSetProfileId: {2}", parent.Id, parent.EntityType, dataSetProfileId );
						EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Entity_dataSetProfile" );
					status.AddError( "Error - the save was not successful. " + message );
					LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );

				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					status.AddError( "Error - the save was not successful. " + message );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save(), Parent: {0} ({1})", parent.EntityBaseName, parent.EntityBaseId ) );
				}


			}
			return id;
		}

		#endregion

		/// <summary>
		/// Get all dataSetProfileProfile for the provided entity
		/// The returned entities are just the base
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returnsThisEntity
		public static List<DataSetProfile> GetAll( Guid parentUid, bool includingParts = true , bool isAPIRequest = false)
		{
			var list = new List<DataSetProfile>();
			var entity = new DataSetProfile();

			Entity parent = EntityManager.GetEntity( parentUid );
			LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );

			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_DataSetProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new DataSetProfile();
							if ( item.DataSetProfile != null && item.DataSetProfile.EntityStateId > 2 )
							{
								DataSetProfileManager.MapFromDB( item.DataSetProfile, entity, includingParts, isAPIRequest );
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



	}
}
