using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.QData;
using workIT.Models.ProfileModels;
using workIT.Utilities;

using ThisResource = workIT.Models.QData.DataProfile;
using DBEntity = workIT.Data.Tables.DataProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;
using Newtonsoft.Json;

namespace workIT.Factories
{
	public class DataProfileManager : BaseFactory
	{
		static readonly string thisClassName = "DataProfileManager";

		#region DataProfile - persistance ==================

		public bool SaveList( List<ThisResource> input, int dataSetTimeFrameId, ref SaveStatus status )
		{
			bool allIsValid = true;
			int dataProfileId = 0;
			int icnt = 0;
			if ( input != null && input.Any() )
				icnt = input.Count();

			//need to handle deletes for parent?
			//DeleteAll( dataSetTimeFrameId, ref status );
			using ( var context = new EntityContext() )
			{
				bool doingDelete = false;
				var existing = context.DataProfile.Where( s => s.DataSetTimeFrameId == dataSetTimeFrameId ).ToList();
				if ( existing != null && existing.Any() )
				{
					//a possibility would to skip delete if input and output count are equal
					if ( existing.Count() == 1 && icnt == 1 )
					{
						dataSetTimeFrameId = existing[ 0 ].DataSetTimeFrameId;
						dataProfileId = existing[ 0 ].Id;
					}
					else
					{   //may always  be a delete regardless of this if?
						if ( icnt < existing.Count() || icnt > 1 )
						{
							doingDelete = true;
						}
					}
				}
				if ( doingDelete )
				{
					var messages = new List<string>();
					DeleteAll( dataSetTimeFrameId, ref messages );
					if ( messages.Any() )
						status.AddErrorRange( messages );
					dataProfileId = 0;
				}
			}

			if ( input == null || !input.Any() )
				return true;
			//status.Messages = new List<StatusMessage>();
			foreach ( var item in input )
			{
				//this would only be valid for the first record, should reset it just in case
				if ( dataProfileId > 0 )
				{
					var e = GetBasic( dataProfileId );
					if ( e != null )
						item.Id = e.Id;
					dataProfileId = 0;
				}
				item.DataSetTimeFrameId = dataSetTimeFrameId;
				//status.HasErrors = false;

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
            status.HasSectionErrors = false;

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
						context.Configuration.LazyLoadingEnabled = false;
						DBEntity efEntity = context.DataProfile
								.SingleOrDefault( s => s.Id == entity.Id );

						if ( efEntity != null && efEntity.Id > 0 )
						{
							entity.RowId = efEntity.RowId;

							MapToDB( entity, efEntity );
							//these classes should probably use the dates from the parent
							if ( IsValidDate( status.EnvelopeCreatedDate ) && status.LocalCreatedDate < efEntity.Created )
							{
								//efEntity.Created = status.LocalCreatedDate;
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
									string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a DataProfile. The process appeared to not work, but was not an exception, so we have no message, or no clue. DataSetTimeFrameId: {0}, Id: {1}", entity.DataSetTimeFrameId, entity.Id );
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
							status.AddError( "Error - update failed, as DataProfile was not found." );
						}
					}
					else
					{
						//add
						int newId = Add( entity, ref status );
						if ( newId == 0 || status.HasSectionErrors )
							isValid = false;
						//status.Messages = new List<StatusMessage>();
						//status.HasErrors = false;
					}
				}
			}
			catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
			{
				string message = HandleDBValidationError( dbex, thisClassName + string.Format( ".Save. id: {0}, DataSetTimeFrameId: {1}", entity.Id, entity.DataSetTimeFrameId ), "DataProfile" );
				status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
			}
			catch ( Exception ex )
			{
				string message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save. id: {0}, DataSetTimeFrameId: {1}", entity.Id, entity.DataSetTimeFrameId ) );
				status.AddError( thisClassName + ".Save(). Error - the save was not successful. " + message );
				isValid = false;
			}


			return isValid;
		}

		/// <summary>
		/// add a DataProfile
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

					//if ( IsValidDate( status.EnvelopeCreatedDate ) )
					//{
					//	efEntity.Created = status.LocalCreatedDate;
					//	efEntity.LastUpdated = status.LocalCreatedDate;
					//}
					//else
					{
						efEntity.Created = System.DateTime.Now;
						efEntity.LastUpdated = System.DateTime.Now;
					}

					context.DataProfile.Add( efEntity );
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
						string message = thisClassName + string.Format( ". Add Failed", "Attempted to add a DataProfile. The process appeared to not work, but was not an exception, so we have no message, or no clue. DataProfile: {0}, DataSetProfileId: {1}", entity.DataSetTimeFrameId, entity.DataSetTimeFrameId );
						status.AddError( thisClassName + ". Error - the add was not successful. " + message );
						EmailManager.NotifyAdmin( "DataProfileManager. Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "DataProfile" );
					status.AddError( thisClassName + ".Add(). Error - the save was not successful. " + message );

					LoggingHelper.LogError( message, true );
				}
				catch ( Exception ex )
				{
					string message = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), DataSetTimeFrameId: {0}\r\n", efEntity.DataSetTimeFrameId ) );
					status.AddError( thisClassName + ".Add(). Error - the save was not successful. \r\n" + message );
				}
			}

			return efEntity.Id;
		}

		/// <summary>
		/// Parts:
		/// - Jurisdiction
		/// - DataProfile
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool UpdateParts( ThisResource entity, ref SaveStatus status )
		{
			bool isAllValid = true;

			//


			return isAllValid;
		}

		public bool ValidateProfile( ThisResource profile, ref SaveStatus status )
		{
			status.HasSectionErrors = false;
			if ( string.IsNullOrWhiteSpace( profile.Description ) )
			{
				//status.AddWarning( "An DataProfile Description must be entered" );
			}


			return status.WasSectionValid;
		}
		/// <summary>
		/// Delete all profiles for parent
		/// </summary>
		/// <param name="dataSetTimeFrameId"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool DeleteAll( int dataSetTimeFrameId, ref List<string> messages )
		{
			bool isValid = true;
			int count = 0;

			if ( dataSetTimeFrameId < 1 )
			{
				messages.Add( thisClassName + "DeleteAll. Error - the dataSetTimeFrameId was not provided." );
				return false;
			}
			using ( var context = new EntityContext() )
			{
				var results = context.DataProfile
							.Where( s => s.DataSetTimeFrameId == dataSetTimeFrameId )
							.OrderBy( s => s.Created )
							.ToList();
				if ( results == null || results.Count == 0 )
				{
					return true;
				}

				foreach ( var item in results )
				{
					context.DataProfile.Remove( item );
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

		//public static ThisResource GetBasic( int id )
		//{
		//	ThisResource entity = new ThisResource();
		//	using ( var context = new EntityContext() )
		//	{
		//		DBEntity item = context.DataProfile
		//				.SingleOrDefault( s => s.Id == id );

		//		if ( item != null && item.Id > 0 )
		//		{
		//			MapFromDB( item, entity, false );
		//		}
		//	}

		//	return entity;
		//}
		/// <summary>
		/// Get All for a DataSetProfileId
		/// </summary>
		/// <param name="dataSetProfileId"></param>
		/// <returns></returns>
		public static List<ThisResource> GetAll( int dataSetTimeFrameId )
		{
			var list = new List<ThisResource>();
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				var results = context.DataProfile
							.Where( s => s.DataSetTimeFrameId == dataSetTimeFrameId )
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
		public static ThisResource GetBasic( int id )
		{
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.DataProfile
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, false );
				}
			}

			return entity;
		}
		//
		public static void MapToDB( ThisResource input, DBEntity output )
		{

			//want output ensure fields input create are not wiped
			if ( output.Id == 0 )
			{
				output.DataSetTimeFrameId = input.DataSetTimeFrameId;
			}
			output.Id = input.Id;
			//output.EntityStateId = input.EntityStateId;
			output.Description = GetData( input.Description );

			//
			output.DataProfileAttributeSummaryJson = JsonConvert.SerializeObject( input.DataProfileAttributeSummary, JsonHelper.GetJsonSettings() );
			//
			output.DataProfileAttributesJson = JsonConvert.SerializeObject( input.DataProfileAttributes, JsonHelper.GetJsonSettings() );

		}

		public static void MapFromDB( DBEntity input, ThisResource output,
				bool includingParts )
		{

			output.Id = input.Id;
			output.RowId = input.RowId;
			output.Description = input.Description == null ? "" : input.Description;

			//
			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime )input.Created;
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime )input.LastUpdated;

			//components
			if ( includingParts )
			{

				//get DataAttributes
				//
				output.DataProfileAttributeSummaryJson = input.DataProfileAttributeSummaryJson;
				output.DataProfileAttributesJson = input.DataProfileAttributesJson;

				output.DataProfileAttributeSummary = JsonConvert.DeserializeObject<DataProfileJson>( input.DataProfileAttributeSummaryJson );
				if ( !string.IsNullOrWhiteSpace( input.DataProfileAttributesJson ) )
					output.DataProfileAttributes = JsonConvert.DeserializeObject<DataProfileAttributes>( input.DataProfileAttributesJson );
				else
					output.DataProfileAttributes = new DataProfileAttributes();
			}
		} //


		#endregion


	}
}
