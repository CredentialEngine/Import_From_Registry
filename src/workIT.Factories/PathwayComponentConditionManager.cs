using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Newtonsoft.Json;

using workIT.Models;
using workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;

using ThisEntity = workIT.Models.Common.PathwayComponentCondition;
using DBEntity = workIT.Data.Tables.Pathway_ComponentCondition;
using EntityContext = workIT.Data.Tables.workITEntities;


namespace workIT.Factories
{
	public class PathwayComponentConditionManager : BaseFactory
	{
		static string thisClassName = "PathwayComponentConditionManager";


		#region persistance ==================

		/// <summary>
		/// add a Pathway_ComponentCondition
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Save( ThisEntity entity, ref List<String> messages )
		{
			bool isValid = true;
			var efEntity = new DBEntity();
			using ( var context = new EntityContext() )
			{
				try
				{
					if ( ValidateProfile( entity, ref messages ) == false )
					{
						return false;
					}

					if ( entity.Id == 0 )
					{
						MapToDB( entity, efEntity );

						if ( entity.RowId == null || entity.RowId == Guid.Empty )
							efEntity.RowId = entity.RowId = Guid.NewGuid();
						else
							efEntity.RowId = entity.RowId;

						efEntity.Created = efEntity.LastUpdated = System.DateTime.Now;

						context.Pathway_ComponentCondition.Add( efEntity );

						// submit the change to database
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							entity.Id = efEntity.Id;
							//add target components
							UpdateParts( entity, ref messages );

							return true;
						}
						else
						{
							//?no info on error
							messages.Add( "Error - the profile was not saved. " );
							string message = string.Format( "Pathway_ComponentConditionManager.Add Failed", "Attempted to add a Pathway_ComponentCondition. The process appeared to not work, but was not an exception, so we have no message, or no clue.Pathway_ComponentCondition. Pathway_ComponentCondition: {0}, createdById: {1}", entity.Name, entity.CreatedById );
							EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
						}
					}
					else
					{
						efEntity = context.Pathway_ComponentCondition
								.SingleOrDefault( s => s.Id == entity.Id );

						if ( efEntity != null && efEntity.Id > 0 )
						{
							//for updates, chances are some fields will not be in interface, don't map these (ie created stuff)
							MapToDB( entity, efEntity );
							if ( HasStateChanged( context ) )
							{
								efEntity.LastUpdated = System.DateTime.Now;
								int count = context.SaveChanges();
								//can be zero if no data changed
								if ( count >= 0 )
								{
									isValid = true;
								}
								else
								{
									//?no info on error
									messages.Add( "Error - the update was not successful. " );
									string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a Pathway_ComponentCondition. The process appeared to not work, but was not an exception, so we have no message, or no clue. Pathway_ComponentConditionId: {0}, Id: {1}, updatedById: {2}", entity.Id, entity.Id, entity.LastUpdatedById );
									EmailManager.NotifyAdmin( thisClassName + ". Pathway_ComponentCondition_Update Failed", message );
								}
							}
							//continue with parts regardless
							UpdateParts( entity, ref messages );
						}
						else
						{
							messages.Add( "Error - update failed, as record was not found." );
						}
					}

				}
				catch ( Exception ex )
				{
					var message = FormatExceptions( ex );
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Pathway_ComponentCondition_Add(), Pathway_ComponentConditionId: {0}", entity.ParentComponentId ) );
					messages.Add( string.Format( "Error encountered saving component condition. Name: {0}, Error: {1}. ", entity.Name, message ) );
					isValid = false;
				}
			}

			return isValid;
		}
		public bool UpdateParts( ThisEntity entity, ref List<string> messages )
		{
			bool isValid = true;


			return isValid;
		}

		public bool DeleteAll( int pathwayComponentId, SaveStatus status )
		{
			bool isValid = true;
			if ( pathwayComponentId < 1 )
			{
				status.AddError( thisClassName + ".DeleteAll. Error - A valid pathwayComponentId must be provided." );
				return false;
			}
			using ( var context = new EntityContext() )
			{
				context.Pathway_ComponentCondition.RemoveRange( context.Pathway_ComponentCondition.Where( s => s.ParentComponentId == pathwayComponentId ) );
				int count = context.SaveChanges();
				if ( count >= 0 )
				{
					isValid = true;
					//status.AddError( string.Format( "removed {0} related relationships.", count ) );
				}
			}
			return isValid;

		}

		/// <summary>
		/// Delete record
		/// Need to also delete the related Entity
		/// </summary>
		/// <param name="Id"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		public bool Delete( int id, ref string statusMessage )
		{
			bool isValid = false;
			if ( id == 0 )
			{
				statusMessage = "Error - missing an identifier for the Pathway_ComponentCondition";
				return false;
			}

			using ( var context = new EntityContext() )
			{
				var efEntity = context.Pathway_ComponentCondition
							.SingleOrDefault( s => s.Id == id );

				if ( efEntity != null && efEntity.Id > 0 )
				{
					//delete the relate Entity
					//20-06-03 N/A added an after delete trigger
					//new EntityManager().Delete( efEntity.RowId, ref statusMessage );
					//now remove
					context.Pathway_ComponentCondition.Remove( efEntity );
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

		private bool ValidateProfile( ThisEntity profile, ref List<string> messages )
		{
			bool isValid = true;
			//bool scriptTagsFound = false;
			//if ( profile.ParentComponentId < 1 )
			//{
			//	messages.Add( "Error: A Pathway_ComponentCondition ParentComponentId is required." );
			//}
			//if ( string.IsNullOrWhiteSpace( profile.Name ) )
			//{
			//	messages.Add( "Error: A Pathway_ComponentCondition Name is required." );
			//}
			//if ( string.IsNullOrWhiteSpace( profile.Description ) )
			//{
			//	//messages.Add( "Error: A Pathway_ComponentCondition Description is required." );
			//}
			//else
			//{
			//	if ( ValidateText( profile.Description, "Pathway_ComponentCondition Description", 15, ref messages ) )
			//	{
			//		if ( FormHelper.HasHtmlTags( profile.Description ) )
			//		{
			//			profile.Description = FormHelper.CleanText( profile.Description, ref scriptTagsFound );
			//			if ( scriptTagsFound )
			//				messages.Add( "Script Tags are not allowed in the description" );
			//			else
			//				warnings.Add( "The description contained HTML tags. These were removed. Please review the description to ensure the converted text is still appropriate." );
			//		}
			//	}
			//}
			//if ( profile.RequiredNumber == 0 )
			//{
			//	messages.Add( "Error: A Pathway_ComponentCondition RequiredNumber is required and must be >= 1." );
			//}


			//if ( messages.Count() > 0 )
			//	return false;

			return isValid;
		}
		#endregion

		#region == Retrieval =======================

		public static ThisEntity Get( int id, bool includingComponents = true )
		{
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				DBEntity item = context.Pathway_ComponentCondition
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity, includingComponents );
				}
			}

			return entity;
		}

		public static List<ThisEntity> GetAll( int ParentComponentId, bool includingComponents = true )
		{
			var output = new List<ThisEntity>();
			ThisEntity entity = new ThisEntity();
			using ( var context = new EntityContext() )
			{
				var list = context.Pathway_ComponentCondition
						.Where( s => s.ParentComponentId == ParentComponentId )
						.OrderBy( s => s.Created)
						.ToList();
				foreach ( var item in list )
				{
					if ( item != null && item.Id > 0 )
					{
						entity = new ThisEntity();
						MapFromDB( item, entity, includingComponents );
						output.Add( entity );
					}
				}
			}

			return output;
		}

		public static void MapFromDB( DBEntity from, ThisEntity to, bool includingComponents = false )
		{

			to.Id = from.Id;
			to.RowId = from.RowId;
			to.ParentComponentId = from.ParentComponentId;
			to.Name = from.Name;
			to.Description = from.Description;
			to.PathwayCTID = from.PathwayCTID;

			to.RelatedEntity = EntityManager.GetEntity( to.RowId, false );
			if ( to.RelatedEntity != null && to.RelatedEntity.Id > 0 )
				to.EntityLastUpdated = to.RelatedEntity.LastUpdated;

			if ( from.PathwayComponent != null && from.PathwayComponent.Id > 0 )
			{
				//assign parent pathway component
			}
			to.RequiredNumber = from.RequiredNumber != null ? ( int )from.RequiredNumber : 0;
			//
			if ( IsValidDate( from.Created ) )
				to.Created = ( DateTime )from.Created;
			if ( IsValidDate( from.LastUpdated ) )
				to.LastUpdated = ( DateTime )from.LastUpdated;

			//components
			//actually may always want these, but a list (shallow get) of components
			if ( includingComponents )
			{
				//get all target components
				//do we want a deep get or summary? Likely summary here
				to.TargetComponent = Entity_PathwayComponentManager.GetAll( to.RowId, PathwayComponent.PathwayComponentRelationship_TargetComponent, PathwayComponentManager.componentActionOfSummary );
			}
		}
		public static void MapToDB( ThisEntity from, DBEntity to )
		{

			to.Id = from.Id;
			if ( to.Id < 1 )
			{

				//will need to be carefull here, will this exist in the input??
				//there could be a case where an external Id was added to bulk upload for an existing record
				to.PathwayCTID = from.PathwayCTID;
			}
			else
			{
				
			}
			//don't map rowId, ctid, or dates as not on form
			//to.RowId = from.RowId;
			to.ParentComponentId = from.ParentComponentId;
			to.Name = from.Name;
			to.Description = from.Description;
			to.RequiredNumber = from.RequiredNumber;


		}

		public static List<Dictionary<string, object>> GetAllForExport_DictionaryList( string owningOrgUid, bool includingConditionProfile = true )
		{
			//
			var result = new List<Dictionary<string, object>>();
			var table = GetAllForExport_DataTable( owningOrgUid, includingConditionProfile );

			foreach ( DataRow dr in table.Rows )
			{
				var rowData = new Dictionary<string, object>();
				for ( var i = 0; i < dr.ItemArray.Count(); i++ )
				{
					rowData[ table.Columns[ i ].ColumnName ] = dr.ItemArray[ i ];
				}
				result.Add( rowData );
			}
			return result;
		}
		//
		public static DataTable GetAllForExport_DataTable( string owningOrgUid, bool includingConditionProfile )
		{
			var result = new DataTable();
			string connectionString = DBConnectionRO();
			//
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();
				using ( SqlCommand command = new SqlCommand( "[Pathway_ComponentConditions_Export]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@OwningOrgUid", owningOrgUid ) );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}

					}
					catch ( Exception ex )
					{
						var message = FormatExceptions( ex );
						LoggingHelper.LogError( ex, thisClassName + string.Format( ".GetAllForExport_DataTable() - Execute proc, Message: {0} \r\n owningOrgUid: {1} ", message, owningOrgUid ) );
					}
				}
			}
			return result;
		}

		#endregion

	}
}
