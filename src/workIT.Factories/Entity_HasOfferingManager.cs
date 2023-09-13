using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using workIT.Utilities;

using DBEntity = workIT.Data.Tables.Entity_HasOffering;
using EntityContext = workIT.Data.Tables.workITEntities;

using workIT.Models;
using MC = workIT.Models.Common;
using MP = workIT.Models.ProfileModels;
using ThisResource = workIT.Models.Common.Entity_HasOffering;

namespace workIT.Factories
{
    public class Entity_HasOfferingManager : BaseFactory
    {
        static string thisClassName = "Factories.Entity_HasOfferingManager";

        public bool SaveList( List<int> list, MC.Entity relatedEntity, ref SaveStatus status )
        {
            if ( list == null || list.Count == 0 )
                return true;
            int newId = 0;

            bool isAllValid = true;
            foreach ( int item in list )
            {
                newId = Add( relatedEntity, item, ref status );
                if ( newId == 0 )
                    isAllValid = false;
            }

            return isAllValid;
        }
        /// <summary>
        /// Add an Entity_HasOffering
        /// </summary>
        /// <param name="relatedEntity">Entity for the parent resource</param>
        /// <param name="recordId"></param>
        /// <param name="userId"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public int Add( MC.Entity relatedEntity, int recordId, ref SaveStatus status )
        {
            int id = 0;
            int count = status.Messages.Count();
            if ( recordId == 0 )
            {
                status.AddError( string.Format( "A valid scheduled Offering identifier was not provided to the {0}.Add method.", thisClassName ) );
                return 0;
            }

            using ( var context = new EntityContext() )
            {
                var efEntity = new DBEntity();
                try
                {
                    //first check for duplicates
                    efEntity = context.Entity_HasOffering
                            .SingleOrDefault( s => s.EntityId == relatedEntity.Id
                            && s.ScheduledOfferingId == recordId );

                    if ( efEntity != null && efEntity.Id > 0 )
                    {
                        //messages.Add( string.Format( "Error - this HasOffering has already been added to this profile.", thisClassName ) );
                        return efEntity.Id;
                    }

                    efEntity = new DBEntity
                    {
                        EntityId = relatedEntity.Id,
                        ScheduledOfferingId = recordId,
                        Created = System.DateTime.Now
                    };

                    context.Entity_HasOffering.Add( efEntity );

                    // submit the change to database
                    count = context.SaveChanges();
                    if ( count > 0 )
                    {
                        //messages.Add( "Successful" );
                        id = efEntity.Id;
                        return efEntity.Id;
                    }
                    else
                    {
                        //?no info on error
                        status.AddError( "Error - the add was not successful." );
                        string message = thisClassName + $".Add Failed. Attempted to add a HasOffering for a profile. The process appeared to not work, but there was no exception, so we have no message, or no clue. Parent: type: {relatedEntity.EntityTypeId}, name: '{relatedEntity.EntityBaseName}'({ relatedEntity.EntityBaseId}), scheduledOfferingId: { recordId}.";
                        EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
                    }
                }
                catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
                {
                    string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Entity_HasOffering" );
                    status.AddError( "Error - the save was not successful. " + message );
                    LoggingHelper.LogError( dbex, thisClassName + $".Add(). Parent: type: {relatedEntity.EntityTypeId}, name: '{relatedEntity.EntityBaseName}'({relatedEntity.EntityBaseId}), scheduledOfferingId: {recordId}" );
                }
                catch ( Exception ex )
                {
                    string message = FormatExceptions( ex );
                    status.AddError( "Error - the save was not successful. " + message );
                    LoggingHelper.LogError( ex, thisClassName + $".Add(). Parent: type: {relatedEntity.EntityTypeId}, name: '{relatedEntity.EntityBaseName}'({relatedEntity.EntityBaseId}), scheduledOfferingId: {recordId}" );
                }
            }
            return id;
        }
        //

        /// <summary>
        /// Get all of the offerings for a resource.
        /// </summary>
        /// <param name="parent">The Entity for this parent</param>
        /// <returns></returns>
        public static List<MC.ScheduledOffering> GetAll( Guid parentId )
        {
            var parentEntity = EntityManager.GetEntity( parentId, false );
            return GetAll( parentEntity );
        }

        /// <summary>
        /// Get all of the offerings for a resource.
        /// Not sure how much detail is needed yet.
        /// </summary>
        /// <param name="parent">The Entity for this parent</param>
        /// <returns></returns>
        public static List<MC.ScheduledOffering> GetAll( MC.Entity parent )
        {
            var list = new List<ThisResource>();
            ThisResource entity = new ThisResource();
            var output = new List<MC.ScheduledOffering>();

            try
            {
                using ( var context = new EntityContext() )
                {
                    var results = context.Entity_HasOffering
                            .Where( s => s.EntityId == parent.Id )
                            .OrderBy( s => s.ScheduledOfferingId )
                            .ToList();

                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( var from in results )
                        {
                            //TODO - do we really only care about the vsp, and not the Entity_HasOffering content? Perhaps user and created for edit or detail view?
                            entity = new ThisResource
                            {
                                Id = from.Id,
                                ScheduledOfferingId = from.ScheduledOfferingId,
                                EntityId = from.EntityId
                            };

                            if ( IsValidDate( from.Created ) )
                                entity.Created = ( DateTime ) from.Created;
                            //just in case
                            if ( from.ScheduledOffering != null && from.ScheduledOffering.Id > 0 )
                            {
                                var vsp = new MC.ScheduledOffering();
                                ScheduledOfferingManager.MapFromDB( from.ScheduledOffering, vsp, includingProperties: false );
                                entity.ScheduledOffering = vsp;
                                output.Add( vsp );
                            }
                            list.Add( entity );
                        }
                    }
                    return output;
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".GetAll" );
            }
            return output;
        }

        public bool DeleteAll( MC.Entity parentEntity, ref SaveStatus status )
        {
            bool isValid = true;
            int count = 0;
            try
            {
                using ( var context = new EntityContext() )
                {
                    var results = context.Entity_HasOffering
                                .Where( s => s.EntityId == parentEntity.Id )
                                .ToList();
                    if ( results == null || results.Count == 0 )
                        return true;

                    foreach ( var item in results )
                    {
                        context.Entity_HasOffering.Remove( item );
                        count = context.SaveChanges();
                        if ( count > 0 )
                        {

                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                var msg = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + $".DeleteAll failed for Entity: {parentEntity.EntityTypeId}" );
                status.AddError( msg );
                isValid = false;
            }
            return isValid;
        }
    
        public bool DeleteAll( int scheduledOfferingId, ref List<string> messages )
        {
            bool isValid = true;
            int count = 0;
            try
            {
                using ( var context = new EntityContext() )
                {
                    var results = context.Entity_HasOffering
                                .Where( s => s.ScheduledOfferingId == scheduledOfferingId )
                                .ToList();
                    if ( results == null || results.Count == 0 )
                        return true;

                    foreach ( var item in results )
                    {
                        context.Entity_HasOffering.Remove( item );
                        count = context.SaveChanges();
                        if ( count > 0 )
                        {

                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                var msg = FormatExceptions( ex );
                LoggingHelper.LogError( ex, thisClassName + $".DeleteAll failed for ScheduledOfferingId: {scheduledOfferingId}" );
                messages.Add( msg );
                isValid = false;
            }
            return isValid;
        }
    }
}
