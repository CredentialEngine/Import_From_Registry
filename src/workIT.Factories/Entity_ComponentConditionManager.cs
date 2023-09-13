using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Newtonsoft.Json;

using workIT.Models;
using MC=workIT.Models.Common;
using workIT.Models.ProfileModels;
using workIT.Utilities;

using ThisResource = workIT.Models.Common.ComponentCondition;
using DBResource = workIT.Data.Tables.Entity_ComponentCondition;
using EntityContext = workIT.Data.Tables.workITEntities;


namespace workIT.Factories
{
	public class Entity_ComponentConditionManager : BaseFactory
	{
		static string thisClassName = "Entity_ComponentConditionManager";


        #region persistance ==================

        /// <summary>
        /// add a Entity_ComponentCondition
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public bool Save(ThisResource resource, string componentCTID, ref List<String> messages )
		{
			bool isValid = true;
			var efEntity = new DBResource();
			using ( var context = new EntityContext() )
			{
				try
				{
					if ( ValidateProfile( resource, ref messages ) == false )
					{
						return false;
					}
                    //If the profile.EntityId is 0 and have the ParentComponentId, get the resource
                    if ( resource.EntityId == 0 )
                    {
                        if ( IsGuidValid( resource.ParentIdentifier ) )
                        {
                            var parentEntity = EntityManager.GetEntity( resource.ParentIdentifier, false );
                            if ( parentEntity != null && parentEntity.Id > 0 )
                                resource.EntityId = parentEntity.Id;
                            else
                            {
                                messages.Add( String.Format( "Error: a valid parent identifier was not provided for the component condition. ComponentCTID: {0}, Name: {1}", componentCTID, string.IsNullOrWhiteSpace( resource.Name ) ? resource.Description : resource.Name ) );
                                return false;
                            }
                        }
                        else
                        {
                            messages.Add( String.Format( "Error: a parent identifier or parent.EntityId was not provided for the component condition. ComponentCTID: {0}, Name: {1}", componentCTID, string.IsNullOrWhiteSpace( resource.Name ) ? resource.Description : resource.Name ) );
                            return false;
                        }
                    }
                    if ( resource.Id == 0 )
					{
						MapToDB( resource, efEntity );

						if ( resource.RowId == null || resource.RowId == Guid.Empty )
							efEntity.RowId = resource.RowId = Guid.NewGuid();
						else
							efEntity.RowId = resource.RowId;

						efEntity.Created = efEntity.LastUpdated = System.DateTime.Now;

						context.Entity_ComponentCondition.Add( efEntity );

						// submit the change to database
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							resource.Id = efEntity.Id;
							//add target components
							UpdateParts( resource, ref messages );

							return true;
						}
						else
						{
							//?no info on error
							messages.Add( "Error - the profile was not saved. " );
							string message = string.Format( "Entity_ComponentConditionManager.Add Failed", "Attempted to add a Entity_ComponentCondition. The process appeared to not work, but was not an exception, so we have no message, or no clue.Entity_ComponentCondition. Entity_ComponentCondition: {0}", resource.Name );
							EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
						}
					}
					else
					{
						efEntity = context.Entity_ComponentCondition
								.SingleOrDefault( s => s.Id == resource.Id );

						if ( efEntity != null && efEntity.Id > 0 )
						{
							//for updates, chances are some fields will not be in interface, don't map these (ie created stuff)
							MapToDB( resource, efEntity );
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
									string message = string.Format( thisClassName + ".Save Failed", "Attempted to update a Entity_ComponentCondition. The process appeared to not work, but was not an exception, so we have no message, or no clue. Entity_ComponentConditionId: {0}, Id: {1}", resource.Id, resource.Id );
									EmailManager.NotifyAdmin( thisClassName + ".Save' Entity_ComponentCondition Update Failed", message );
								}
							}
							//continue with parts regardless
							UpdateParts( resource, ref messages );
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
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Save(), ParentIdentifier: {0}", resource.ParentIdentifier ) );
					messages.Add( string.Format( "Error encountered saving component condition. Name: {0}, Error: {1}. ", resource.Name, message ) );
					isValid = false;
				}
			}

			return isValid;
		}
		public bool UpdateParts(ThisResource resource, ref List<string> messages )
		{
			bool isValid = true;


			return isValid;
		}


		public bool DeleteAll( MC.PathwayComponent parentComponent, ref SaveStatus status )
		{
			var parentEntity = EntityManager.GetEntity( parentComponent.RowId, false );
			if ( parentEntity == null || parentEntity.Id == 0 )
			{
				status.AddError( $"{thisClassName}.DeleteAll(parentComponent). Error - Delete failed, the parentComponent ({parentComponent.CTID}/{parentComponent.RowId}) for the Entity_ComponentCondition was not found " );
				return false;
			}

			return DeleteAll( parentEntity, ref status );
		}

		/// <summary>
		/// Delete all component conditions for a pathway
		/// </summary>
		/// <param name="parentEntity"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public bool DeleteAll( MC.Entity parentEntity, ref SaveStatus status )
		{
			bool isValid = false;
			if ( parentEntity == null || parentEntity.Id == 0 )
			{
				status.AddError( $"{thisClassName}.DeleteAll(parentEntity). Error - Delete failed, missing the parentComponentId for the Entity_ComponentCondition" );
				return false;
			}
			try
			{
				using ( var context = new EntityContext() )
				{
					var list = context.Entity_ComponentCondition
								.Where( s => s.EntityId == parentEntity.Id ).ToList();

					foreach ( var efEntity in list )
					{
						//need to ensure related Entity and target components are also removed
						if ( efEntity.Entity != null & efEntity.Entity.Id > 0 )
						{
							var thisParentEntity = EntityManager.GetEntity( efEntity.RowId, false );

							DeleteAll( thisParentEntity, ref status );
						}
						//22-07-07 now that ComponentCondition can have conditions, will need to check for child ComponentConditions to deletel
						context.Entity_ComponentCondition.Remove( efEntity );
						int count = context.SaveChanges();
						if ( count > 0 )
						{
							isValid = true;
						}
					}

				}
			}
			catch ( Exception ex )
			{
				var message = FormatExceptions( ex );
				LoggingHelper.LogError( ex, thisClassName + $".DeleteAll(parentEntity), parent TypeId: {parentEntity.EntityTypeId}, BaseId: {parentEntity.EntityBaseId}. " );
				status.AddError( $"Error encountered saving component condition. Parent TypeId: {parentEntity.EntityTypeId}, BaseId: {parentEntity.EntityBaseId}., Error: {message}. " );
				isValid = false;
			}
			return isValid;
		}

		/// <summary>
		/// Delete all component conditions for a pathway
		/// </summary>
		/// <param name="pathwayCTID"></param>
		/// <param name="status"></param>
		/// <returns></returns>
        public bool DeleteAll( string pathwayCTID, ref SaveStatus status )
        {
            bool isValid = false;
            try
            {
                using ( var context = new EntityContext() )
                {
                    var list = context.Entity_ComponentCondition
                                .Where( s => s.PathwayCTID == pathwayCTID ).ToList();

                    foreach ( var efEntity in list )
                    {
                        //need to ensure related Entity and target components are also removed
                        //22-07-07 now that ComponentCondition can have conditions, will need to check for child ComponentConditions to delete
						//	==> should be covered by this delete process
                        if ( efEntity.Entity != null & efEntity.Entity.Id > 0 )
                        {
                            var thisParentEntity = EntityManager.GetEntity( efEntity.RowId, false );

                            DeleteAll( thisParentEntity, ref status );
                        }
                        //
                        context.Entity_ComponentCondition.Remove( efEntity );
                        int count = context.SaveChanges();
                        if ( count > 0 )
                        {
                            isValid = true;
                        }
                    }

                }
            }
            catch ( Exception ex )
            {
                var message = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + string.Format( ".DeleteAll(pathwayCTID), pathwayCTID: {0}", pathwayCTID ) );
                status.AddError( string.Format( "Error encountered deleting all component conditions for Pathway: {0}, Error: {1}. ", pathwayCTID, message ) );
                isValid = false;
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
				statusMessage = "Error - missing an identifier for the Entity_ComponentCondition";
				return false;
			}

			using ( var context = new EntityContext() )
			{
				var efEntity = context.Entity_ComponentCondition
							.SingleOrDefault( s => s.Id == id );

				if ( efEntity != null && efEntity.Id > 0 )
				{
					//delete the relate Entity
					//20-06-03 N/A added an after delete trigger
					//new EntityManager().Delete( efEntity.RowId, ref statusMessage );
					//now remove
					context.Entity_ComponentCondition.Remove( efEntity );
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

		private bool ValidateProfile(ThisResource profile, ref List<string> messages )
		{
			bool isValid = true;
			//bool scriptTagsFound = false;
			//if ( profile.ParentComponentId < 1 )
			//{
			//	messages.Add( "Error: A Entity_ComponentCondition ParentComponentId is required." );
			//}
			//if ( string.IsNullOrWhiteSpace( profile.Name ) )
			//{
			//	messages.Add( "Error: A Entity_ComponentCondition Name is required." );
			//}
			//if ( string.IsNullOrWhiteSpace( profile.Description ) )
			//{
			//	//messages.Add( "Error: A Entity_ComponentCondition Description is required." );
			//}
			//else
			//{
			//	if ( ValidateText( profile.Description, "Entity_ComponentCondition Description", 15, ref messages ) )
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
			//	messages.Add( "Error: A Entity_ComponentCondition RequiredNumber is required and must be >= 1." );
			//}


			//if ( messages.Count() > 0 )
			//	return false;

			return isValid;
		}
		#endregion

		#region == Retrieval =======================

		public static ThisResource Get( int id, bool includingComponents = true )
		{
            ThisResource resource = new ThisResource();
			using ( var context = new EntityContext() )
			{
                DBResource item = context.Entity_ComponentCondition
						.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, resource, includingComponents );
				}
			}

			return resource;
		}

		public static List<ThisResource> GetAll( Guid parentRowId, bool includingComponents = true )
		{
			var output = new List<ThisResource>();
            ThisResource resource = new ThisResource();
			var parentEntity = EntityManager.GetEntity( parentRowId, false );
			if ( parentEntity == null || parentEntity.Id == 0 )
			{
				return output;
			}
			using ( var context = new EntityContext() )
			{
				var list = context.Entity_ComponentCondition
						.Where( s => s.EntityId == parentEntity.Id )
						.OrderBy( s => s.Created )
						.ToList();
				foreach ( var item in list )
				{
					if ( item != null && item.Id > 0 )
					{
						resource = new ThisResource()
						{
							ParentIdentifier = parentRowId,
							EntityId = parentEntity.Id,
						};
						MapFromDB( item, resource, includingComponents );
						output.Add( resource );
					}
				}
			}

			return output;
		}

		public static void MapFromDB(DBResource input, ThisResource output, bool includingComponents = false )
		{
            output.EntityId = input.EntityId;
            output.Id = input.Id;
			output.RowId = input.RowId;
			output.Name = input.Name;
			output.Description = input.Description;
			output.PathwayCTID = input.PathwayCTID;

			output.RelatedEntity = EntityManager.GetEntity( output.RowId, false );
			if ( output.RelatedEntity != null && output.RelatedEntity.Id > 0 )
				output.EntityLastUpdated = output.RelatedEntity.LastUpdated;
			if ( input.Entity != null && input.Entity.Id > 0 )
			{
				output.ParentIdentifier = input.Entity.EntityUid;
			}
			output.RequiredNumber = input.RequiredNumber != null ? ( int ) input.RequiredNumber : 0;
			//Json, may not be used here
			if ( !string.IsNullOrEmpty( input.ConditionProperties ) )
			{
				output.ConditionProperties = JsonConvert.DeserializeObject<MC.ConditionProperties>( input.ConditionProperties );
				if ( output.ConditionProperties != null )
				{
					//TODO - if zero, have a process for setting the default. Row-same as parent, col-parent + 1
					output.RowNumber = output.ConditionProperties.RowNumber;
					output.ColumnNumber = output.ConditionProperties.ColumnNumber;
					output.HasProgressionLevel = output.ConditionProperties.HasProgressionLevel;
				}
			}
			else
			{
				//likely want a default so property is present?
				//output.RowNumber = 0;
				//output.ColumnNumber = 0;
			}

			MapConstraints( input.HasConstraint, output );
			//
			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime ) input.Created;
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime ) input.LastUpdated;

			//components
			//actually may always want these, but a list (shallow get) of components
			if ( includingComponents )
			{
				//get all target components
				//do we want a deep get or summary? Likely summary here
				output.TargetComponent = Entity_PathwayComponentManager.GetAll( output.RowId, MC.PathwayComponent.PathwayComponentRelationship_TargetComponent, PathwayComponentManager.ComponentActionOfSummary );

				//check for child conditions
				output.HasCondition = GetAll( output.RowId, includingComponents );

			}
		}
		public static void MapConstraints( string hasConstraint, ThisResource output )
		{
			if ( string.IsNullOrWhiteSpace( hasConstraint ) )
				return;
			//any processing? or just verbatim map?
			var constraints = new List<Constraint>();
			output.HasConstraint = JsonConvert.DeserializeObject<List<MC.Constraint>>( hasConstraint );
			//maybe ensure the parentIdentifier is there
		}
		public static void MapToDB(ThisResource input, DBResource output )
		{

			output.Id = input.Id;
			if ( output.Id < 1 )
			{

				//will need to be carefull here, will this exist in the input??
				//there could be a case where an external Id was added to bulk upload for an existing record
				output.PathwayCTID = input.PathwayCTID;
			}
			else
			{

			}
			//don't map rowId, ctid, or dates as not on form
			//to.RowId = from.RowId;
			//to.ParentComponentId = from.ParentComponentId;
			output.EntityId = input.EntityId;
			output.Name = input.Name;
			output.Description = input.Description;
			output.RequiredNumber = input.RequiredNumber;
			output.LogicalOperator = input.LogicalOperator;

			if ( input.HasConstraint != null && input.HasConstraint.Any() )
			{
				output.HasConstraint = JsonConvert.SerializeObject( input.HasConstraint, JsonHelper.GetJsonSettings( false ) );
			}
			else
				output.HasConstraint = null;
			//TBD. not handled yet. 
			if ( input.ConditionProperties != null/* && input.ConditionProperties.RowNumber > 0*/ )
			{
				output.ConditionProperties = JsonConvert.SerializeObject( input.ConditionProperties );
			}
			else
				output.ConditionProperties = null;
		}


		#endregion

	}
}
