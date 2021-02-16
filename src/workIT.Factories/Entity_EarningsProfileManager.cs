using System;
using System.Collections.Generic;
using System.Linq;

using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;

using DBEntity = workIT.Data.Tables.Entity_EarningsProfile;
using EntityContext = workIT.Data.Tables.workITEntities;

namespace workIT.Factories
{
	public class Entity_EarningsProfileManager : BaseFactory
	{
		static string thisClassName = "Entity_EarningsProfileManager";
		public static int RelationshipType_HasPart = 1;
		#region Entity EarningsProfile Persistance ===================

		/// <summary>
		/// Add an Entity_EarningsProfile
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="EarningsProfileId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public int Add( Guid parentUid,
					int earningsProfileId,
					ref SaveStatus status )
		{
			int id = 0;
			int count = 0;
			if ( earningsProfileId == 0 )
			{
				status.AddError( string.Format( "A valid EarningsProfile identifier was not provided to the {0}.EntityEarnings_Add method.", thisClassName ) );
				return 0;
			}

			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( "Error - the parent entity was not found." );
				return 0;
			}
			using ( var context = new EntityContext() )
			{
				DBEntity efEntity = new DBEntity();
				try
				{
					efEntity = context.Entity_EarningsProfile
							.FirstOrDefault( s => s.EntityId == parent.Id && s.EarningsProfileId == earningsProfileId );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						return 0;
					}

					efEntity = new DBEntity();
					efEntity.EntityId = parent.Id;
					efEntity.EarningsProfileId = earningsProfileId;
					efEntity.Created = System.DateTime.Now;

					context.Entity_EarningsProfile.Add( efEntity );

					// submit the change to database
					count = context.SaveChanges();
					if ( count > 0 )
					{
						if ( efEntity.Id == 0 )
						{
							List<string> messages = new List<string>();
							Delete( efEntity.Id, ref messages );

							context.Entity_EarningsProfile.Add( efEntity );
							count = context.SaveChanges();
							id = efEntity.Id;
						}
						else
						{
							id = efEntity.Id;
						}
						return efEntity.Id;
					}
					else
					{
						//?no info on error
						status.AddError( "Error - the add was not successful." );
						string message = thisClassName + string.Format( ".Add Failed", "Attempted to add an Entity_EarningsProfile. The process appeared to not work, but there was no exception, so we have no message, or no clue. Parent Profile: {0}, Type: {1}, EarningsId: {2}", parentUid, parent.EntityType, earningsProfileId );
						EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Entity_Earnings" );
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

		/// <summary>
		/// Delete all EarningsProfiles for parent
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool DeleteAll( Guid parentUid, ref SaveStatus status )
		{
			bool isValid = true;
			int count = 0;
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( thisClassName + ".DeleteAll Error - the provided target parent entity was not provided." );
				return false;
			}
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( thisClassName + ". Error - the provided target parent entity was not provided." );
				return false;
			}
			using ( var context = new EntityContext() )
			{
				//check if target is a reference object and is only in use here
				var results = context.Entity_EarningsProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Created )
							.ToList();
				if ( results == null || results.Count == 0 )
				{
					return true;
				}

				foreach ( var item in results )
				{
					if ( item.EarningsProfile != null && item.EarningsProfile.EntityStateId > 0 )
					{
						var messages = new List<string>();
						new EarningsProfileManager().Delete( item.Id, ref messages );
						if ( messages.Any() )
							status.AddErrorRange( messages );
						continue;
					}
					context.Entity_EarningsProfile.Remove( item );
					count = context.SaveChanges();
					if ( count > 0 )
					{

					}
				}
			}

			return isValid;
		}

		public bool Delete( int id, ref List<string> messages )
		{
			bool isValid = true;
			if ( id < 1 )
			{
				messages.Add( thisClassName + ".Delete() Error - a valid Entity_EarningsProfile id must be provided." );
				return false;
			}

			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					DBEntity efEntity = context.Entity_EarningsProfile
								.FirstOrDefault( s => s.Id == id );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						//
						context.Entity_EarningsProfile.Remove( efEntity );
						int count = context.SaveChanges();
						if ( count > 0 )
						{
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
					LoggingHelper.LogError( ex, thisClassName + ".Delete()" );
					isValid = false;
					var statusMessage = FormatExceptions( ex );
					if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
					{
						statusMessage = "Error: this EmploymentOutcomeProfile cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this EmploymentOutcomeProfile can be deleted.";
					}
					messages.Add( statusMessage );
				}
			}
			return isValid;
		}

		#endregion

		/// <summary>
		/// Get all EarningsProfile for the provided entity
		/// The returned entities are just the base
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returnsThisEntity
		public static List<EarningsProfile> GetAll( Guid parentUid, bool includingParts = true )
		{
			var list = new List<EarningsProfile>();
			var entity = new EarningsProfile();

			Entity parent = EntityManager.GetEntity( parentUid );
			LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );

			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_EarningsProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new EarningsProfile();

							//need to distinguish between on a detail page for conditions and EarningsProfile detail
							//would usually only want basics here??
							//17-05-26 mp- change to MapFromDB_Basic
							if ( item.EarningsProfile != null && item.EarningsProfile.EntityStateId > 1 )
							{
								EarningsProfileManager.MapFromDB( item.EarningsProfile, entity, includingParts );
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
		/// Format a summary of the EarningsProfile for use in search and gray boxes
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returns>
		public static string GetSummary( Guid parentUid )
		{
			var list = new List<EarningsProfile>();
			var entity = new EarningsProfile();

			Entity parent = EntityManager.GetEntity( parentUid );
			LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );
			var summary = "";
			var lineBreak = "";
			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_EarningsProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new EarningsProfile();
							if ( item.EarningsProfile != null && item.EarningsProfile.EntityStateId > 1 )
							{
								if ( !string.IsNullOrWhiteSpace( item.EarningsProfile.Name ) )
									summary = item.EarningsProfile.Name + lineBreak;
								else if ( !string.IsNullOrWhiteSpace( item.EarningsProfile.Description ) )
								{
									summary = item.EarningsProfile.Description.Length < 200 ? item.EarningsProfile.Description : item.EarningsProfile.Description.Substring( 0, 200 ) + "  ... " + lineBreak;
								}
								else
								{

								}
								if ( item.EarningsProfile.MedianEarnings > 0 )
									summary += string.Format( " Median Earnings: {0}", item.EarningsProfile.MedianEarnings );
							}
							lineBreak = "<\br>";
						}
					}
					return summary;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetSummary" );
			}
			return summary;
		}

	}
}
