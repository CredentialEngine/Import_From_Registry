using System;
using System.Collections.Generic;
using System.Linq;

using workIT.Models;
using workIT.Models.Common;
using workIT.Utilities;

using DBEntity = workIT.Data.Tables.Entity_EmploymentOutcomeProfile;
using EntityContext = workIT.Data.Tables.workITEntities;

namespace workIT.Factories
{
	public class Entity_EmploymentOutcomeProfileManager : BaseFactory
	{
		static string thisClassName = "Entity_EmploymentOutcomeProfileManager";
		public static int RelationshipType_HasPart = 1;
		#region Entity EmploymentOutcomeProfile Persistance ===================

		/// <summary>
		/// Add an Entity_EmploymentOutcomeProfile
		/// </summary>
		/// <param name="parentUid"></param>
		/// <param name="EmploymentOutcomeProfileId"></param>
		/// <param name="messages"></param>
		/// <returns></returns>
		public int Add( Guid parentUid,
					int employmentOutcomeProfileId,
					ref SaveStatus status )
		{
			int id = 0;
			int count = 0;
			if ( employmentOutcomeProfileId == 0 )
			{
				status.AddError( string.Format( "A valid EmploymentOutcomeProfile identifier was not provided to the {0}.EntityEarnings_Add method.", thisClassName ) );
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
					efEntity = context.Entity_EmploymentOutcomeProfile
							.FirstOrDefault( s => s.EntityId == parent.Id && s.EmploymentOutcomeProfileId == employmentOutcomeProfileId );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						return 0;
					}

					efEntity = new DBEntity
					{
						EntityId = parent.Id,
						EmploymentOutcomeProfileId = employmentOutcomeProfileId,
						Created = System.DateTime.Now
					};

					context.Entity_EmploymentOutcomeProfile.Add( efEntity );

					// submit the change to database
					count = context.SaveChanges();
					if ( count > 0 )
					{
						if ( efEntity.Id == 0 )
						{
							List<string> messages = new List<string>();
							Delete( efEntity.Id, ref messages );

							context.Entity_EmploymentOutcomeProfile.Add( efEntity );
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
						string message = thisClassName + string.Format( ".Add Failed", "Attempted to add an Entity_EmploymentOutcomeProfile. The process appeared to not work, but there was no exception, so we have no message, or no clue. Parent Profile: {0}, Type: {1}, EarningsId: {2}", parentUid, parent.EntityType, employmentOutcomeProfileId );
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
		/// Delete all EmploymentOutcomeProfiles for parent
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
				var results = context.Entity_EmploymentOutcomeProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Created )
							.ToList();
				if ( results == null || results.Count == 0 )
				{
					return true;
				}

				foreach ( var item in results )
				{
					if ( item.EmploymentOutcomeProfile != null && item.EmploymentOutcomeProfile.EntityStateId > 0 )
					{
						var messages = new List<string>();
						new EmploymentOutcomeProfileManager().Delete( item.Id, ref messages );
						if ( messages.Any() )
							status.AddErrorRange( messages );
						continue;
					}
					context.Entity_EmploymentOutcomeProfile.Remove( item );
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
				messages.Add( thisClassName + ".Delete() Error - a valid Entity_EmploymentOutcomeProfile id must be provided." );
				return false;
			}

			using ( var context = new EntityContext() )
			{
				try
				{
					context.Configuration.LazyLoadingEnabled = false;
					DBEntity efEntity = context.Entity_EmploymentOutcomeProfile
								.FirstOrDefault( s => s.Id == id );

					if ( efEntity != null && efEntity.Id > 0 )
					{
						//
						context.Entity_EmploymentOutcomeProfile.Remove( efEntity );
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							isValid = true;
						}
					}
					else
					{
						messages.Add( thisClassName + ".Delete() Warning No action taken, as the Entity_EmploymentOutcomeProfile was not found." );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + ".Delete()" );
					isValid = false;
					var statusMessage = FormatExceptions( ex );
					if ( statusMessage.ToLower().IndexOf( "the delete statement conflicted with the reference constraint" ) > -1 )
					{
						statusMessage = "Error: this Entity_EmploymentOutcomeProfile cannot be deleted as it is being referenced by other items, such as roles or credentials. These associations must be removed before this Entity_EmploymentOutcomeProfile can be deleted.";
					}
					messages.Add( statusMessage );
				}
			}
			return isValid;
		}
		#endregion

		/// <summary>
		/// Get all EmploymentOutcomeProfile for the provided entity
		/// The returned entities are just the base
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returnsThisEntity
		public static List<EmploymentOutcomeProfile> GetAll( Guid parentUid, bool includingParts = true )
		{
			var list = new List<EmploymentOutcomeProfile>();
			var entity = new EmploymentOutcomeProfile();

			Entity parent = EntityManager.GetEntity( parentUid );
			LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );

			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_EmploymentOutcomeProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new EmploymentOutcomeProfile();

							//need to distinguish between on a detail page for conditions and EmploymentOutcomeProfile detail
							//would usually only want basics here??
							//17-05-26 mp- change to MapFromDB_Basic
							if ( item.EmploymentOutcomeProfile != null && item.EmploymentOutcomeProfile.EntityStateId > 1 )
							{
								EmploymentOutcomeProfileManager.MapFromDB( item.EmploymentOutcomeProfile, entity, includingParts );
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
		/// Format a summary of the EmploymentOutcomeProfile for use in search and gray boxes
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returns>
		public static string GetSummary( Guid parentUid )
		{
			var list = new List<EmploymentOutcomeProfile>();
			var entity = new EmploymentOutcomeProfile();

			Entity parent = EntityManager.GetEntity( parentUid );
			LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );
			var summary = "";
			var lineBreak = "";
			try
			{
				using ( var context = new EntityContext() )
				{
					List<DBEntity> results = context.Entity_EmploymentOutcomeProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new EmploymentOutcomeProfile();
							if ( item.EmploymentOutcomeProfile != null && item.EmploymentOutcomeProfile.EntityStateId > 1 )
							{
								if ( !string.IsNullOrWhiteSpace( item.EmploymentOutcomeProfile.Name ) )
									summary = item.EmploymentOutcomeProfile.Name + lineBreak;
								else if ( !string.IsNullOrWhiteSpace( item.EmploymentOutcomeProfile.Description ) )
								{
									summary = item.EmploymentOutcomeProfile.Description.Length < 200 ? item.EmploymentOutcomeProfile.Description : item.EmploymentOutcomeProfile.Description.Substring( 0, 200 ) + "  ... " + lineBreak;
								}
								else
								{

								}
								if ( item.EmploymentOutcomeProfile.JobsObtained > 0 )
									summary += string.Format( " Jobs Obtained: {0}", item.EmploymentOutcomeProfile.JobsObtained );
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
