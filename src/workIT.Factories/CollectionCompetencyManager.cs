using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;
//using workIT.Models.Helpers.Cass;
using ApiFramework = workIT.Models.API.Collection;
using DBEntity = workIT.Data.Tables.Collection_Competency;
using EntityContext = workIT.Data.Tables.workITEntities;
using ThisEntity = workIT.Models.ProfileModels.Competency;
using ThisEntityItem = workIT.Models.Common.CredentialAlignmentObjectItem;


namespace workIT.Factories
{
	public class CollectionCompetencyManager : BaseFactory
	{
		static string thisClassName = "CollectionCompetencyManager";

		#region Persistance ===================

		public bool SaveList( int CollectionId, List<ThisEntity> list, ref SaveStatus status )
		{
			//will need to do a delete all or take the approach for entity address:
			//- read
			//- look up by CTID
			//- update last updated
			//- at end delete records with an older last updated date
			DateTime updateDate = DateTime.Now;
			if ( IsValidDate( status.EnvelopeUpdatedDate ) )
			{
				//an individual competency can have a modified date, so???
				updateDate = status.LocalUpdatedDate;
			}
			bool isAllValid = true;

			foreach ( var entity in list )
			{
				entity.FrameworkId = CollectionId;
				Save( entity, updateDate, ref status );
			}

			//delete any records with last updated less than updateDate
			DeleteAll( CollectionId, ref status, updateDate );
			return isAllValid;
		}

		/// <summary>
		/// Add/Update a Collection
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public bool Save( ThisEntity entity, DateTime updateDate, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;

			DBEntity efEntity = new DBEntity();
			try
			{
				using ( var context = new EntityContext() )
				{

					if ( ValidateProfile( entity, ref status ) == false )
					{
						//return false;
					}

					if ( entity.Id == 0 )
					{
						//add
						efEntity = new DBEntity();
						MapToDB( entity, efEntity );

						if ( IsValidDate( entity.Created ) )
							efEntity.Created = ( DateTime ) entity.Created;
						else if ( IsValidDate( status.EnvelopeCreatedDate ) )
							efEntity.Created = status.LocalCreatedDate;
						else
							efEntity.Created = DateTime.Now;
						//this is not the competency last updated
						efEntity.LastUpdated = updateDate;
						//future handling of last
						//if ( IsValidDate( entity.DateModified ) )
						//	efEntity.DateModified = (DateTime) entity.DateModified;

						if ( IsValidGuid( entity.RowId ) )
							efEntity.RowId = entity.RowId;
						else
							efEntity.RowId = Guid.NewGuid();

						context.Collection_Competency.Add( efEntity );

						count = context.SaveChanges();

						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;
						if ( count == 0 )
						{
							status.AddWarning( string.Format( "CollectionId: {0}. Unable to add competency: {1} <br\\> ", entity.FrameworkId, string.IsNullOrWhiteSpace( entity.CompetencyText ) ? "no description" : entity.CompetencyText ) );
						}
						else
						{
							//
						}
					}
					else
					{

						efEntity = context.Collection_Competency.FirstOrDefault( s => s.Id == entity.Id );
						if ( efEntity != null && efEntity.Id > 0 )
						{
							entity.RowId = efEntity.RowId;
							//update
							MapToDB( entity, efEntity );

							//need to do the date check here, or may not be updated
							if ( IsValidDate( entity.Created ) )
								efEntity.Created = ( DateTime ) entity.Created;

							//if ( IsValidDate( status.EnvelopeCreatedDate ) && status.LocalCreatedDate < efEntity.Created )
							//{
							//	efEntity.Created = status.LocalCreatedDate;
							//}
							//future handling of last
							//if ( IsValidDate( entity.DateModified ) )
							//	efEntity.DateModified = (DateTime) entity.DateModified;

							efEntity.LastUpdated = updateDate;
							//has changed?
							if ( HasStateChanged( context ) )
							{
								//if ( IsValidDate( status.EnvelopeUpdatedDate ) )
								//	efEntity.LastUpdated = status.LocalUpdatedDate;
								//else
								//	efEntity.LastUpdated = DateTime.Now;

								count = context.SaveChanges();

							}
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".Save()" );
			}

			return isValid;
		}

		public bool DeleteAll( int CollectionId, ref SaveStatus status, DateTime? lastUpdated = null )
		{
			bool isValid = true;
			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( CollectionId == 0 )
			{
				status.AddError( thisClassName + ". Error - the provided target parent CollectionId was not provided." );
				return false;
			}
			int expectedDeleteCount = 0;
			try
			{
				using ( var context = new EntityContext() )
				{

					var results = context.Collection_Competency.Where( s => s.CollectionId == CollectionId && ( lastUpdated == null || s.LastUpdated < lastUpdated ) )
				.ToList();
					if ( results == null || results.Count == 0 )
						return true;
					expectedDeleteCount = results.Count;

					foreach ( var item in results )
					{
						context.Collection_Competency.Remove( item );
						var count = context.SaveChanges();
						if ( count > 0 )
						{

						}
					}
				}
			}
			catch ( Exception ex )
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. CollectionId: {0},  exception: {1}", CollectionId, msg ) );
			}
			return isValid;
		}
		//

		public bool ValidateProfile( ThisEntity profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;

			if ( string.IsNullOrWhiteSpace( profile.CompetencyText ) )
			{
				status.AddError( "A CompetencyText name must be entered for competency: " + profile.CtdlId );
			}


			return status.WasSectionValid;
		}

		#endregion
		#region  retrieval ==================

		public static ThisEntity GetByCtid( string ctid )
		{
			ThisEntity entity = new ThisEntity();
			if ( string.IsNullOrWhiteSpace( ctid ) )
				return entity;
			try
			{
				using ( var context = new EntityContext() )
				{
					//lookup by frameworkUri, or SourceUrl
					DBEntity item = context.Collection_Competency
							.FirstOrDefault( s => s.CTID.ToLower() == ctid.ToLower() );

					if ( item != null && item.Id > 0 )
					{
						MapFromDB( item, entity );
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetByCtid" );
			}
			return entity;
		}//
		public static void MapToDB( ThisEntity input, DBEntity output )
		{
			//want to ensure fields from create are not wiped
			if ( output.Id == 0 )
			{
				output.CTID = input.CTID;
			}
			output.Id = input.Id;

			output.CollectionId = input.FrameworkId;

			output.CompetencyText = input.CompetencyText;
			output.CompetencyLabel = input.CompetencyLabel;
			output.CompetencyCategory = input.CompetencyCategory;
			output.CredentialRegistryURI = input.CtdlId ?? "";

			//this will have been serialized in the import step
			if ( !string.IsNullOrWhiteSpace( input.CompetencyDetailJson ) )
				output.CompetencyDetailJson = input.CompetencyDetailJson;
			else
			{
				//ensure we don't reset the store
				output.CompetencyDetailJson = null;
			}
		} //

		public static void MapFromDB( DBEntity input, ThisEntity output )
		{
			output.Id = input.Id;
			output.RowId = input.RowId;
			output.CTID = input.CTID;
			output.FrameworkId = input.CollectionId;

			output.CompetencyText = input.CompetencyText;
			output.CompetencyLabel = input.CompetencyLabel;
			output.CompetencyCategory = input.CompetencyCategory;
			output.CtdlId = input.CredentialRegistryURI ?? "";

			if ( !string.IsNullOrEmpty( output.CompetencyDetailJson ) )
			{
				//details
				output.CompetencyDetail = JsonConvert.DeserializeObject<CompetencyDetail>( output.CompetencyDetailJson );
			}

			if ( input.Created != null )
				output.Created = ( DateTime ) input.Created;
			if ( input.LastUpdated != null )
				output.LastUpdated = ( DateTime ) input.LastUpdated;
		}

		#endregion


	}
}
