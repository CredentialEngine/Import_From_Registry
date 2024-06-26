using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using workIT.Utilities;

using DBEntity = workIT.Data.Tables.Entity_HasVerificationService;
using EntityContext = workIT.Data.Tables.workITEntities;

using workIT.Models;
using MC = workIT.Models.Common;
using MP = workIT.Models.ProfileModels;
using ThisResource = workIT.Models.ProfileModels.Entity_HasVerificationService;

namespace workIT.Factories
{
    public class Entity_HasVerificationServiceManager : BaseFactory
    {
        static string thisClassName = "Factories.Entity_HasVerificationServiceManager";

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
            //need to remove those no longer in the list, or just delete all
            return isAllValid;
        }
        /// <summary>
        /// Add an Entity_HasVerificationService
        /// </summary>
        /// <param name="parentEntityId">Entity.Id for the parent credential</param>
        /// <param name="vspId"></param>
        /// <param name="userId"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public int Add( MC.Entity relatedEntity, int vspId, ref SaveStatus status )
        {
            int id = 0;
            int count = status.Messages.Count();
            if ( vspId == 0 )
            {
                status.AddError( string.Format( "A valid VerificationService identifier was not provided to the {0}.Add method.", thisClassName ) );
                return 0;
            }
         

            using ( var context = new EntityContext() )
            {
                var efEntity = new DBEntity();
                try
                {
                    //first check for duplicates
                    efEntity = context.Entity_HasVerificationService
                            .SingleOrDefault( s => s.EntityId == relatedEntity.Id
                            && s.VerificationServiceId == vspId );

                    if ( efEntity != null && efEntity.Id > 0 )
                    {
                        //messages.Add( string.Format( "Error - this UsesVerificationService has already been added to this profile.", thisClassName ) );
                        return efEntity.Id;
                    }

                    efEntity = new DBEntity
                    {
                        EntityId = relatedEntity.Id,
                        VerificationServiceId = vspId,
                        Created = System.DateTime.Now
                    };

                    context.Entity_HasVerificationService.Add( efEntity );

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
                        string message = thisClassName + string.Format( ".Add Failed", "Attempted to add a UsesVerificationService for a profile. The process appeared to not work, but there was no exception, so we have no message, or no clue. Credential: '{0}'({1}), verificationServiceId: {2}", relatedEntity.EntityBaseName, relatedEntity.EntityBaseId, vspId );
                        EmailManager.NotifyAdmin( thisClassName + ".Add Failed", message );
                    }
                }
                catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
                {
                    string message = HandleDBValidationError( dbex, thisClassName + ".Add() ", "Entity_HasVerificationService" );
                    status.AddError( "Error - the save was not successful. " + message );
                    LoggingHelper.LogError( dbex, thisClassName + string.Format( ".Add(). Credential: '{0}'({1}), verificationServiceId: {2}", relatedEntity.EntityBaseName, relatedEntity.EntityBaseId, vspId ));
                }
                catch ( Exception ex )
                {
                    string message = FormatExceptions( ex );
                    status.AddError( "Error - the save was not successful. " + message );
                    LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(). Credential: '{0}'({1}), verificationServiceId: {2}", relatedEntity.EntityBaseName, relatedEntity.EntityBaseId, vspId ) );
                }
            }
            return id;
        }

        public bool DeleteAll( int verificationServiceId, ref List<string> messages )
        {
            bool isValid = true;
            int count = 0;
            try
            {
                using ( var context = new EntityContext() )
                {
                    var results = context.Entity_HasVerificationService
                                .Where( s => s.VerificationServiceId == verificationServiceId )
                                .ToList();
                    if ( results == null || results.Count == 0 )
                        return true;

                    foreach ( var item in results )
                    {
                        context.Entity_HasVerificationService.Remove( item );
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
                LoggingHelper.LogError( ex, thisClassName + $".DeleteAll failed for VerificationServiceId: {verificationServiceId}" );
                messages.Add( msg );
                isValid = false;
            }
            return isValid;
        }


        public bool DeleteAll( int verificationServiceId, ref SaveStatus status )
        {
            bool isValid = true;
            int count = 0;
            try
            {
                using ( var context = new EntityContext() )
                {
                    var results = context.Entity_HasVerificationService
                                .Where( s => s.VerificationServiceId == verificationServiceId )
                                .ToList();
                    if ( results == null || results.Count == 0 )
                        return true;

                    foreach ( var item in results )
                    {
                        context.Entity_HasVerificationService.Remove( item );
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
                LoggingHelper.LogError( ex, thisClassName + $".DeleteAll failed for VerificationServiceId: {verificationServiceId}" );
                isValid = false;
            }
            return isValid;
        }

        /// <summary>
        /// Get all of the vsps for a credential.
        /// Not sure how much detail is needed yet.
        /// </summary>
        /// <param name="parent">The Entity for this parent</param>
        /// <returns></returns>
        public static List<MP.VerificationServiceProfile> GetAll( MC.Entity parent )
        {
            var list = new List<ThisResource>();
            ThisResource entity = new ThisResource();
            var output = new List<MP.VerificationServiceProfile>();
            //MC.Entity parent = EntityManager.GetEntity( parentUid );

            try
            {
                using ( var context = new EntityContext() )
                {
                    var results = context.Entity_HasVerificationService
                            .Where( s => s.EntityId == parent.Id )
                            .OrderBy( s => s.VerificationServiceId )
                            .ToList();

                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( var from in results )
                        {
                            //TODO - do we really only care about the vsp, and not the Entity_HasVerificationService content? Perhaps user and created for edit or detail view?
                            entity = new ThisResource
                            {
                                Id = from.Id,
                                VerificationServiceId = from.VerificationServiceId,
                                EntityId = from.EntityId
                            };

                            if ( IsValidDate( from.Created ) )
                                entity.Created = ( DateTime ) from.Created;
                            //just in case
                            if ( from.VerificationServiceProfile != null && from.VerificationServiceProfile.Id > 0 )
                            {
                                var vsp = new MP.VerificationServiceProfile();
                                VerificationServiceProfileManager.MapFromDB( from.VerificationServiceProfile, vsp, true );
                                entity.VerificationServiceProfile = vsp;
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


    }
}
