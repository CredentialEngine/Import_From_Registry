using System;
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

using ThisResource = workIT.Models.QData.DataSetTimeFrame;
using DBEntity = workIT.Data.Tables.DataSetTimeFrame;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;
using System.Data.Entity.Infrastructure;

namespace workIT.Factories
{
	public class DataSetTimeFrameManager : BaseFactory
	{
		static readonly string thisClassName = "DataSetTimeFrameManager";

		#region DataSetTimeFrame - persistance ==================

		public bool SaveList( List<ThisResource> input, int dataSetProfileId, ref SaveStatus status )
		{
			bool allIsValid = true;
			int dataSetTimeFrameId = 0;
			int icnt = 0;
			if ( input != null && input.Any() )
				icnt = input.Count();

			//need to handle deletes for parent?
			//if ( input == null || !input.Any() )
			//	DeleteAll( dataSetProfileId, ref status );
			//if one existing, and one input, just replace
			using ( var context = new EntityContext() )
			{
				bool doingDelete = false;
				var existing = context.DataSetTimeFrame.Where( s => s.DataSetProfileId == dataSetProfileId ).ToList();
				if (existing != null && existing.Any())
				{
					if ( existing.Count() == 1 && icnt == 1 )
						dataSetTimeFrameId = existing[ 0 ].Id;
					else
					{	//may always  be a delete regardless of this if?
						if ( icnt < existing.Count() || icnt > 1 )
						{
							doingDelete = true;
						}
					}
				}
				if ( doingDelete )
				{
					var messages = new List<string>();
					DeleteAll( dataSetProfileId, ref messages );
					if ( messages.Any() )
						status.AddErrorRange( messages );
					dataSetTimeFrameId = 0;
				}
			}

			if ( input == null || !input.Any() )
				return true;

			foreach ( var item in input )
			{
				if ( dataSetTimeFrameId > 0)
				{
					var e = GetBasic( dataSetTimeFrameId );
					if ( e != null )
						item.Id = e.Id;
				}
				item.DataSetProfileId = dataSetProfileId;
				if ( !Save( item, ref status ) )
				{
					allIsValid = false;
				}
			}

			return allIsValid;
		}
		public bool Save( ThisResource entity, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;
			bool saveFailed;
			do
			{
				saveFailed = false;
				try
				{
					using ( var context = new EntityContext() )
					{
						if ( ValidateProfile( entity, ref status ) == false )
						{
							//return false;
						}

						if ( entity.Id > 0 )
						{
							//TODO - consider if necessary, or interferes with anything
							context.Configuration.LazyLoadingEnabled = false;
							DBEntity efEntity = context.DataSetTimeFrame
									.SingleOrDefault( s => s.Id == entity.Id );

							if ( efEntity != null && efEntity.Id > 0 )
							{
								//fill in fields that may not be in entity
								entity.RowId = efEntity.RowId;

								MapToDB( entity, efEntity );
								//these classes should probably use the dates from the parent
								if ( IsValidDate( status.EnvelopeCreatedDate ) && status.LocalCreatedDate < efEntity.Created )
								{
									efEntity.Created = status.LocalCreatedDate;
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
										string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a DataSetTimeFrame. The process appeared to not work, but was not an exception, so we have no message, or no clue. DataSetTimeFrame: {0}, Id: {1}", entity.Name, entity.Id );
										status.AddError( "Error - the update was not successful. " + message );
										EmailManager.NotifyAdmin( thisClassName + ".Save Failed Failed", message );
									}
								}
							
								if ( isValid )
								{
									if ( !UpdateParts( entity, ref status ) )
										isValid = false;
								}
							}
							else
							{
								status.AddError( "Error - update failed, as DataSetTimeFrame was not found." );
							}
						}
						else
						{
							//add
							int newId = Add( entity, ref status );
							if ( newId == 0 || status.HasSectionErrors )
								isValid = false;
						}
					}
				}
				catch ( DbUpdateConcurrencyException ducex )
				{
					//Resolving optimistic concurrency exceptions with Reload (database wins)
					saveFailed = true;

					// Update the values of the entity that failed to save from the store
					ducex.Entries.Single().Reload();
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ), "DataSetTimeFrame" );
					status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save. id: {0}, Name: {1}", entity.Id, entity.Name ) );
					status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
					isValid = false;
				}
			} while ( saveFailed );

			return isValid;
		}

		/// <summary>
		/// add a DataSetTimeFrame
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		private int Add( ThisResource entity, ref SaveStatus status )
		{
			DBEntity efEntity = new DBEntity();
            status.HasSectionErrors = false;

            using ( var context = new EntityContext() )
			{
				try
				{
					MapToDB( entity, efEntity );

					if ( IsValidGuid( entity.RowId ) )
						efEntity.RowId = entity.RowId;
					else
						efEntity.RowId = Guid.NewGuid();
				
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

					context.DataSetTimeFrame.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;

						if ( UpdateParts( entity, ref status ) == false )
						{
						}

						return efEntity.Id;
					}
					else
					{
						//?no info on error

						string message = thisClassName + string.Format( ". Add Failed", "Attempted to add a DataSetTimeFrame. The process appeared to not work, but was not an exception, so we have no message, or no clue. DataSetTimeFrame: {0}, DataSetProfileId: {1}", entity.Name ?? "no name", entity.DataSetProfileId );
						status.AddError( thisClassName + ". Error - the add was not successful. " + message );
						EmailManager.NotifyAdmin( "DataSetTimeFrameManager. Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "DataSetTimeFrame" );
					status.AddError( thisClassName + ".Add(). Error - the save was not successful. " + message );

					LoggingHelper.LogError( message, true );
				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), DataSetProfileId: {0}\r\n", efEntity.DataSetProfileId ) );
					status.AddError( thisClassName + ".Add(). Error - the save was not successful. \r\n" + message );
				}
			}

			return efEntity.Id;
		}

		/// <summary>
		/// Parts:
		/// - Jurisdiction
		/// - DataSetTimeFrame
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool UpdateParts( ThisResource entity, ref SaveStatus status )
		{
			bool isAllValid = true;

			//

			//DataAttributes
			new DataProfileManager().SaveList( entity.DataAttributes, entity.Id, ref status );


			return isAllValid;
		}

		public bool ValidateProfile( ThisResource profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;
			if ( string.IsNullOrWhiteSpace( profile.Description ) )
			{
				status.AddWarning( "An DataSetTimeFrame Description must be entered" );
			}


			return status.WasSectionValid;
		}
		/// <summary>
		/// Delete all profiles for parent
		/// </summary>
		/// <param name="dataSetProfileId"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool DeleteAll( int dataSetProfileId, ref List<string> messages )
		{
			bool isValid = true;
			int count = 0;

			if ( dataSetProfileId < 1 )
			{
				messages.Add (thisClassName + "DeleteAll. Error - the dataSetProfileId was not provided.");
				return false;
			}
			using ( var context = new EntityContext() )
			{
				var results = context.DataSetTimeFrame
							.Where( s => s.DataSetProfileId == dataSetProfileId )
							.ToList();
				if ( results == null || results.Count == 0 )
				{
					return true;
				}

				foreach ( var item in results )
				{
					//need to remove DataProfile
					new DataProfileManager().DeleteAll( item.Id, ref messages );

					context.DataSetTimeFrame.Remove( item );
					count = context.SaveChanges();
					if ( count > 0 )
					{

					}
				}
			}

			return isValid;
		}

		#endregion



		#region == Retrieval =======================

		public static ThisResource GetBasic( int id )
		{
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.DataSetTimeFrame
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, false );
				}
			}

			return entity;
		}
		/// <summary>
		/// Get All for a DataSetProfileId
		/// </summary>
		/// <param name="dataSetProfileId"></param>
		/// <returns></returns>
		public static List<ThisResource> GetAll( int dataSetProfileId )
		{
			var list = new List<ThisResource>();
			ThisResource entity = new ThisResource();
			//Guid guid = new Guid( id );
			using ( var context = new EntityContext() )
			{
				var results = context.DataSetTimeFrame
							.Where( s => s.DataSetProfileId == dataSetProfileId )
							.OrderBy( s => s.Created )
							.ToList();

				if ( results != null && results.Count > 0 )
				{
					foreach ( var item in results )
					{
						entity = new ThisResource();
						MapFromDB( item, entity, true );
						list.Add( entity );
					}
				}
			}
			return list;
		}

		//

		public static void MapToDB( ThisResource input, DBEntity output )
		{

			//want output ensure fields input create are not wiped
			if ( output.Id == 0 )
			{
				output.DataSetProfileId = input.DataSetProfileId;
			}
			output.Id = input.Id;
			//output.EntityStateId = input.EntityStateId;
			output.Description = GetData( input.Description );
			output.Name = GetData( input.Name );
			if ( IsValidDate( input.StartDate ) )
				output.StartDate = input.StartDate;
			else
				output.StartDate = null;

			if ( IsValidDate( input.EndDate ) )
				output.EndDate = input.EndDate;
			else
				output.EndDate = null;
			//
			if ( input.DataSourceCoverageTypeList != null && input.DataSourceCoverageTypeList.Any() )
			{
				output.DataSourceCoverageType = string.Join( "|", input.DataSourceCoverageTypeList );
			}
			else
				output.DataSourceCoverageType = null;

		}

		public static void MapFromDB( DBEntity input, ThisResource output,
				bool includingParts )
		{
			output.DataSetProfileId = input.DataSetProfileId;
			output.Id = input.Id;
			output.RowId = input.RowId;
			output.Name = input.Name;
			output.Description = input.Description == null ? "" : input.Description;
			if ( IsValidDate( input.StartDate ) )
				output.StartDate = input.StartDate;
			else
				output.StartDate = null;

			if ( IsValidDate( input.EndDate ) )
				output.EndDate = input.EndDate;
			else
				output.EndDate = null;
			if ( input.DataSourceCoverageType != null )
			{
				var list = input.DataSourceCoverageType.Split( '|' );
				foreach ( var item in list )
				{
					if ( !string.IsNullOrWhiteSpace( item ) )
						output.DataSourceCoverageTypeList.Add( item );
				}
			}
			else
				output.DataSourceCoverageTypeList = new List<string>();

			//
			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime )input.Created;
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime )input.LastUpdated;

			//components
			if ( includingParts )
			{
				//get DataAttributes
				output.DataAttributes = DataProfileManager.GetAll( output.Id );
				
			}
		} //


		#endregion


	}
}
