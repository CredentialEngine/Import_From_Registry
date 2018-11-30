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
using EM = workIT.Data;
using workIT.Utilities;

using workIT.Data.Views;

using ThisEntity = workIT.Models.ProfileModels.TextValueProfile;
using DBEntity = workIT.Data.Tables.Entity_Reference;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

namespace workIT.Factories
{
    public class Entity_ReferenceManager : BaseFactory
    {
        static string thisClassName = "Entity_ReferenceManager";
        static int defaultCategoryId = CodesManager.PROPERTY_CATEGORY_REFERENCE_URLS;
        int maxReferenceTextLength = UtilityManager.GetAppKeyValue( "maxReferenceTextLength", 600 );
        int maxKeywordLength = UtilityManager.GetAppKeyValue( "maxKeywordLength", 200 );
        int maxReferenceUrlLength = UtilityManager.GetAppKeyValue( "maxReferenceUrlLength", 600 );

        #region Entity Persistance ===================
        public void AddLanguages( List<ThisEntity> profiles,
                Guid parentUid,
                int parentTypeId,
                ref SaveStatus status,
                int categoryId )
        {
            if ( profiles == null || profiles.Count == 0 )
                return;
            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + string.Format( ". Error - the parent entity was not found. parentUid: {0}", parentUid ) );
                return;
            }
            EnumeratedItem code = new EnumeratedItem();
            string textValue = "";
            foreach ( var item in profiles )
            {
                if ( string.IsNullOrWhiteSpace( item.TextValue ) )
                    continue;
                code = CodesManager.GetLanguage( item.TextValue );
                if ( code.Id > 0 )
                {
                    textValue = string.Format( "{0} ", code.Name );
                    AddTextValue( textValue, parent.Id, ref status, categoryId );
                }
            }
        } //

        /// <summary>
        /// Persist Entity Reference
        /// </summary>
        /// <param name="profiles"></param>
        /// <param name="parentUid"></param>
        /// <param name="parentTypeId"></param>
        /// <param name="status"></param>
        /// <param name="categoryId"></param>
        /// <param name="isTitleRequired">If true, a title must exist</param>
        /// <returns></returns>
        public bool Add( List<ThisEntity> profiles,
                Guid parentUid,
                int parentTypeId,
                ref SaveStatus status,
                int categoryId,
                bool isTitleRequired )
        {
            if ( profiles == null || profiles.Count == 0 )
                return true;

            bool isValid = true;
            status.HasSectionErrors = false;

            if ( !IsValidGuid( parentUid ) )
            {
                status.AddError( "Error: the parent identifier was not provided." );
            }
            if ( parentTypeId == 0 )
            {
                status.AddError( "Error: the parent type was not provided." );
            }
            if ( status.HasSectionErrors )
                return false;

            int count = 0;
            if ( profiles == null )
                profiles = new List<ThisEntity>();

            DBEntity efEntity = new DBEntity();

            //Views.Entity_Summary parent2 = EntityManager.GetDBEntity( parentUid );
            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + string.Format( ". Error - the parent entity was not found. parentUid: {0}", parentUid ) );
                return false;
            }
            using ( var context = new EntityContext() )
            {
                //check add/updates first
                if ( profiles.Count() > 0 )
                {
                    bool isEmpty = false;

                    foreach ( ThisEntity entity in profiles )
                    {
                        entity.CategoryId = categoryId;
                        if ( Validate( entity, isTitleRequired, ref isEmpty, ref status ) == false )
                        {
                            isValid = false;
                            continue;
                        }

                        if ( isEmpty ) //skip
                            continue;
                        entity.EntityBaseId = parent.EntityBaseId;

                        //if ( entity.Id == 0 )
                        //{
                        //add
                        efEntity = new DBEntity();
                        MapToDB( entity, efEntity );
                        efEntity.EntityId = parent.Id;
                        efEntity.Created = efEntity.LastUpdated = DateTime.Now;

                        context.Entity_Reference.Add( efEntity );
                        count = context.SaveChanges();
                        //update profile record so doesn't get deleted
                        entity.Id = efEntity.Id;
                        entity.ParentId = parent.Id;

                        if ( count == 0 )
                        {
                            status.AddWarning( string.Format( " Unable to add Profile: {0} <br\\> ", string.IsNullOrWhiteSpace( entity.TextTitle ) ? "no description" : entity.TextTitle ) );
                        }
                        else
                        {
                            if ( categoryId == CodesManager.PROPERTY_CATEGORY_SUBJECT )
                                AddConnections( entity.Id );
                        }

                    } //foreach

                }

            }

            return isValid;
        } //

        /// <summary>
        /// Add for simple strings - textValue only
        /// </summary>
        /// <param name="profiles"></param>
        /// <param name="parentUid"></param>
        /// <param name="status"></param>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        public bool AddTextValue( List<string> profiles,
                Guid parentUid,
                ref SaveStatus status,
                int categoryId )
        {
            bool isValid = true;
            if ( profiles == null || profiles.Count == 0 )
                return true;
            status.HasSectionErrors = false;

            if ( !IsValidGuid( parentUid ) )
            {
                status.AddError( "Error: the parent identifier was not provided." );
                return false;
            }

            int count = 0;
            DBEntity efEntity = new DBEntity();
            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + string.Format( ". Error - the parent entity was not found. parentUid: {0}", parentUid ) );
                return false;
            }
            //using ( var context = new EntityContext() )
            //{
            foreach ( string textValue in profiles )
            {

                if ( string.IsNullOrWhiteSpace( textValue ) )
                {
                    continue;
                }
                AddTextValue( textValue, parent.Id, ref status, categoryId );
                ////add
                //efEntity = new DBEntity();
                //efEntity.CategoryId = categoryId;
                //efEntity.EntityId = parent.Id;
                //efEntity.TextValue = textValue;
                //efEntity.Created = efEntity.LastUpdated = DateTime.Now;

                //context.Entity_Reference.Add( efEntity );
                //count = context.SaveChanges();

                //if ( count == 0 )
                //{
                //	status.AddWarning( string.Format( " Unable to add Entity_Reference. EntityId: {0}, categoryId: {1}, textValue: {2}  ", parent.Id, categoryId, textValue ));
                //}
                //else
                //{
                //	if ( categoryId == CodesManager.PROPERTY_CATEGORY_SUBJECT )
                //		AddConnections( efEntity.Id );
                //}

            } //foreach
              //}

            return isValid;
        } //
        public bool AddTextValue( string textValue,
                int entityId,
                ref SaveStatus status,
                int categoryId )
        {
            if ( string.IsNullOrWhiteSpace( textValue ) )
            {
                return true;
            }
            using ( var context = new EntityContext() )
            {
                //add
                DBEntity efEntity = new DBEntity();
                efEntity.CategoryId = categoryId;
                efEntity.EntityId = entityId;
                efEntity.TextValue = textValue;
                efEntity.Created = efEntity.LastUpdated = DateTime.Now;

                context.Entity_Reference.Add( efEntity );
                int count = context.SaveChanges();

                if ( count == 0 )
                {
                    status.AddWarning( string.Format( " Unable to add Entity_Reference. EntityId: {0}, categoryId: {1}, textValue: {2}  ", entityId, categoryId, textValue ) );
                    return false;
                }
                else
                {
                    if ( categoryId == CodesManager.PROPERTY_CATEGORY_SUBJECT )
                        AddConnections( efEntity.Id );
                }
            }
            return true;
        }
        public bool AddLanguage( string textValue,
                int entityId,
                ref SaveStatus status,
                int categoryId )
        {
            if ( string.IsNullOrWhiteSpace( textValue ) )
            {
                return true;
            }
            using ( var context = new EntityContext() )
            {
                EnumeratedItem code = new EnumeratedItem();
                code = CodesManager.GetLanguage( textValue );
                if ( code.Id > 0 )
                {
                    textValue = string.Format( "{0} ({1})", code.Name, code.Value );
                    AddTextValue( textValue, entityId, ref status, categoryId );
                }

                else
                {
                    status.AddWarning( thisClassName + string.Format( ". Warning - the langugage code was not found. parentUid: {0}, languagecode: {1}", entityId, textValue ) );
                }
            }
            return true;
        }
        //??? Is this needed - It has no references
        public void AddRelatedConnections( int entityRefId )
        {
            System.Threading.ThreadPool.QueueUserWorkItem( delegate
            {
                AddConnections( entityRefId );
            } );
        }

        private void AddConnections( int entityRefId )
        {
            if ( entityRefId < 0 )
                return;

            string connectionString = MainConnection();
            using ( SqlConnection c = new SqlConnection( connectionString ) )
            {
                using ( SqlCommand command = new SqlCommand( "[Entity_ReferenceConnection_Populate]", c ) )
                {
                    c.Open();
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add( new SqlParameter( "@EntityReferenceId", entityRefId ) );

                    command.ExecuteNonQuery();
                    command.Dispose();
                    c.Close();

                }
            }
        } //

        public bool Delete( int recordId, ref string statusMessage )
        {
            bool isOK = true;
            using ( var context = new EntityContext() )
            {
                DBEntity p = context.Entity_Reference.FirstOrDefault( s => s.Id == recordId );
                if ( p != null && p.Id > 0 )
                {
                    context.Entity_Reference.Remove( p );
                    int count = context.SaveChanges();
                }
                else
                {
                    statusMessage = string.Format( "Requested record was not found: {0}", recordId );
                    isOK = false;
                }
            }
            return isOK;

        }
        /// <summary>
        /// Delete all properties for parent (in preparation for import)
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public bool DeleteAll( Entity parent, ref SaveStatus status )
        {
            bool isValid = true;
            //Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + ". Error - the provided target parent entity was not provided." );
                return false;
            }
            using ( var context = new EntityContext() )
            {
                context.Entity_Reference.RemoveRange( context.Entity_Reference.Where( s => s.EntityId == parent.Id ) );
                int count = context.SaveChanges();
                if ( count > 0 )
                {
                    isValid = true;
                }
                else
                {
                    //if doing a delete on spec, may not have been any properties
                }
            }

            return isValid;
        }

        private bool Validate( ThisEntity profile, bool isTitleRequired,
            ref bool isEmpty,
            ref SaveStatus status )
        {
            status.HasSectionErrors = false;

            isEmpty = false;
            //check if empty
            if ( string.IsNullOrWhiteSpace( profile.TextTitle )
                && string.IsNullOrWhiteSpace( profile.TextValue )
                )
            {
                isEmpty = true;
                return true;
            }
            profile.TextTitle = ( profile.TextTitle ?? "" );
            profile.TextValue = ( profile.TextValue ?? "" );
            //16-07-22 mparsons - changed to, for now, let user enter one or the other (except for urls), this gives flexibility to the interface choosing which to show or require
            //ultimately, we will make the profile configurable
            if ( profile.CategoryId == CodesManager.PROPERTY_CATEGORY_REFERENCE_URLS )
            {
                if ( isTitleRequired && string.IsNullOrWhiteSpace( profile.TextTitle ) )
                {
                    status.AddWarning( string.Format( "A title must be entered with this categoryId: {0}", profile.CategoryId ) );

                }
                //text is normally required, unless a competency item
                if ( string.IsNullOrWhiteSpace( profile.TextValue ) )
                {
                    status.AddWarning( string.Format( "A URL must be entered with this categoryId: {0}", profile.CategoryId ) );

                }
            }

            if ( profile.CategoryId == CodesManager.PROPERTY_CATEGORY_REFERENCE_URLS ||
                 profile.CategoryId == CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SOCIAL_MEDIA ||
                 profile.CategoryId == CodesManager.PROPERTY_CATEGORY_CREDENTIAL_URLS ||
                 profile.CategoryId == CodesManager.PROPERTY_CATEGORY_LEARNING_RESOURCE_URLS )
            {

                if ( ( profile.TextValue ?? "" ).Length > maxReferenceUrlLength )
                {
                    status.AddWarning( string.Format( "The Url is too long. It must be less than {0} characters", maxReferenceUrlLength ) );

                }
                else if ( !IsUrlValid( profile.TextValue, ref commonStatusMessage ) )
                {
                    status.AddWarning( string.Format( "The Url is invalid: {0}. {1}", profile.TextValue, commonStatusMessage ) );
                }

                profile.TextValue = ( profile.TextValue ?? "" ).TrimEnd( '/' );
            }
            else
            if ( profile.CategoryId == CodesManager.PROPERTY_CATEGORY_KEYWORD )
            {
                if ( !string.IsNullOrWhiteSpace( profile.TextValue ) && profile.TextValue.Length > maxKeywordLength )
                {
                    status.AddWarning( string.Format( "Error - the keyword must be less than {0} characters.", maxKeywordLength ) );
                }
            }
            else
            if ( profile.CategoryId == CodesManager.PROPERTY_CATEGORY_SUBJECT )
            {
                if ( !string.IsNullOrWhiteSpace( profile.TextValue ) && profile.TextValue.Length > maxKeywordLength )
                {
                    status.AddWarning( string.Format( "Error - the subject must be less than {0} characters.", maxKeywordLength ) );
                }
            } //
            else
            if ( profile.CategoryId == CodesManager.PROPERTY_CATEGORY_SOC )
            {
                if ( !string.IsNullOrWhiteSpace( profile.TextValue ) && profile.TextValue.Length > maxKeywordLength )
                {
                    status.AddWarning( string.Format( "Error - An other occupation must be less than {0} characters.", maxKeywordLength ) );
                }
            }
            else
            if ( profile.CategoryId == CodesManager.PROPERTY_CATEGORY_NAICS )
            {
                if ( !string.IsNullOrWhiteSpace( profile.TextValue ) && profile.TextValue.Length > maxKeywordLength )
                {
                    status.AddWarning( string.Format( "Error - An other industry must be less than {0} characters.", maxKeywordLength ) );
                }
            }
            else if ( profile.CategoryId == CodesManager.PROPERTY_CATEGORY_PHONE_TYPE )
            {
                string phoneNbr = PhoneNumber.StripPhone( GetData( profile.TextValue ) );

                if ( string.IsNullOrWhiteSpace( phoneNbr ) )
                {
                    status.AddWarning( "Error - a phone number must be entered." );

                }
                else if ( !string.IsNullOrWhiteSpace( phoneNbr ) && phoneNbr.Length < 10 )
                {
                    status.AddWarning( string.Format( "Error - A phone number ({0}) must have at least 10 numbers.", profile.TextValue ) );
                }
                //need an other check

            }
            else
            {
                if ( !string.IsNullOrWhiteSpace( profile.TextTitle ) && profile.TextTitle.Length > maxReferenceTextLength )
                {
                    status.AddWarning( string.Format( "Warning - the TextTitle must be less than {0} characters, categoryId: {1}.", maxReferenceTextLength, profile.CategoryId ) );
                }

                if ( !string.IsNullOrWhiteSpace( profile.TextValue ) && profile.TextValue.Length > maxReferenceTextLength )
                {
                    status.AddWarning( string.Format( "Warning - the text value must be less than {0} characters, categoryId: {1}.", maxReferenceTextLength, profile.CategoryId ) );
                }
            }
            if ( profile.CategoryId != CodesManager.PROPERTY_CATEGORY_CONDITION_ITEM
                && string.IsNullOrWhiteSpace( profile.TextTitle ) )
            {
                //status.AddWarning( "A title must be entered" );
                //isValid = false;
            }

            //text is normally required, unless a competency item
            //if ( profile.CategoryId != CodesManager.PROPERTY_CATEGORY_COMPETENCY
            //	&& string.IsNullOrWhiteSpace( profile.TextValue ) )
            //{
            //	status.AddWarning( "A text value must be entered" );
            //	isValid = false;
            //}
            return !status.HasSectionErrors;
        }

        #endregion

        #region  retrieval ==================

        /// <summary>
        /// Get all profiles for the parent
        /// Uses the parent Guid to retrieve the related Entity, then uses the EntityId to retrieve the child objects.
        /// NOTE: the view: uses Entity_Cache, so this method is only for use where the related entity parent is in the Entity_Cache table.
        /// 
        /// TODO - minimize dependency on this method, ie stop using entity_cache
        /// </summary>
        /// <param name="parentUid"></param>
        public static List<ThisEntity> GetAllOLD( Guid parentUid, int categoryId )
        {
            return GetAll( parentUid, categoryId );


            //ThisEntity entity = new ThisEntity();
            //List<ThisEntity> list = new List<ThisEntity>();
            //List<ThisEntity> results = new List<ThisEntity>();
            //Entity parent = EntityManager.GetEntity( parentUid );
            //if ( parent == null || parent.Id == 0 )
            //{
            //    return list;
            //}
            //try
            //{
            //    using ( var context = new ViewContext() )
            //    {
            //        List<Entity_Reference_Summary> search = context.Entity_Reference_Summary
            //                .Where( s => s.EntityId == parent.Id
            //                && s.CategoryId == categoryId )
            //                .OrderBy( s => s.Title ).ThenBy( x => x.TextValue )
            //                .ToList();

            //        if ( categoryId == CodesManager.PROPERTY_CATEGORY_SUBJECT
            //          || categoryId == CodesManager.PROPERTY_CATEGORY_KEYWORD )
            //        {
            //            search = search.OrderBy( s => s.TextValue ).ToList();
            //        }
            //        else
            //        {
            //            search = search.OrderBy( s => s.EntityReferenceId ).ToList();
            //        }

            //        if ( search != null && search.Count > 0 )
            //        {
            //            foreach ( Entity_Reference_Summary item in search )
            //            {
            //                entity = new ThisEntity();
            //                MapFromDB( item, entity );

            //                list.Add( entity );
            //            }
            //        }
            //    }
            //}
            //catch ( Exception ex )
            //{
            //    LoggingHelper.LogError( ex, thisClassName + ".GetAll" );
            //}
            //return list;
        }//

        /// <summary>
        /// Get All Entity_Reference records for the parent 
        /// </summary>
        /// <param name="parentUid"></param>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        public static List<ThisEntity> GetAll( Guid parentUid, int categoryId )
        {
            ThisEntity entity = new ThisEntity();
            List<ThisEntity> list = new List<ThisEntity>();
            List<ThisEntity> results = new List<ThisEntity>();
            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                return list;
            }
            try
            {
                using ( var context = new EntityContext() )
                {
                    List<DBEntity> search = context.Entity_Reference
                            .Where( s => s.EntityId == parent.Id
                            && s.CategoryId == categoryId )
                            .OrderBy( s => s.Title ).ThenBy( x => x.TextValue )
                            .ToList();
                    //this appears wrong, the ToList means query not deferred???
                    if ( categoryId == CodesManager.PROPERTY_CATEGORY_SUBJECT
                      || categoryId == CodesManager.PROPERTY_CATEGORY_KEYWORD )
                    {
                        search = search.OrderBy( s => s.TextValue ).ToList();
                    }
                    else
                    {
                        search = search.OrderBy( s => s.Created ).ToList();
                    }

                    if ( search != null && search.Count > 0 )
                    {
                        foreach ( DBEntity item in search )
                        {
                            entity = new ThisEntity();
                            MapFromDB( item, entity );

                            list.Add( entity );
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".GetAllDirect" );
            }
            return list;
        }//
        public static List<string> GetAllToList( Guid parentUid, int categoryId )
        {
            List<string> list = new List<string>();
            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                return list;
            }
            try
            {
                using ( var context = new EntityContext() )
                {
                    List<DBEntity> search = context.Entity_Reference
                            .Where( s => s.EntityId == parent.Id
                            && s.CategoryId == categoryId )
                            .OrderBy( s => s.Title ).ThenBy( x => x.TextValue )
                            .ToList();

                    if ( categoryId == CodesManager.PROPERTY_CATEGORY_SUBJECT
                      || categoryId == CodesManager.PROPERTY_CATEGORY_KEYWORD )
                    {
                        search = search.OrderBy( s => s.TextValue ).ToList();
                    }
                    else
                    {
                        search = search.OrderBy( s => s.Created ).ToList();
                    }

                    if ( search != null && search.Count > 0 )
                    {
                        foreach ( DBEntity item in search )
                        {
                            list.Add( item.TextValue );
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".GetAllToList" );
            }
            return list;
        }//
        
        public static List<ThisEntity> GetAllSubjects( Guid parentUid )
        {

            ThisEntity entity = new ThisEntity();
            List<ThisEntity> list = new List<ThisEntity>();

            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                return list;
            }
            try
            {
                string prevSubject = "";
                using ( var context = new ViewContext() )
                {
                    List<Entity_Subjects> results = context.Entity_Subjects
                                .Where( s => s.EntityUid == parentUid )
                                .OrderBy( s => s.Subject )
                                .ToList();

                    if ( results != null && results.Count > 0 )
                    {
                        foreach ( Entity_Subjects item in results )
                        {
                            entity = new ThisEntity();
                            if ( item.Subject != prevSubject )
                            {
                                entity.EntityId = item.EntityId;
                                entity.TextValue = item.Subject;
                                list.Add( entity );
                            }
                            prevSubject = item.Subject;
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".Entity_GetAllSubjects" );
            }
            return list;
        }//		

        /// <summary>
        /// quick search for subjects
        /// </summary>
        /// <param name="entityTypeId"></param>
        /// <param name="keyword"></param>
        /// <param name="maxTerms"></param>
        /// <returns></returns>
        public static List<string> QuickSearch_Subjects( int entityTypeId, string keyword, int maxTerms = 0 )
        {
            List<string> list = new List<string>();

            keyword = keyword.Trim();

            if ( maxTerms == 0 ) maxTerms = 50;

            using ( var context = new ViewContext() )
            {
                list = context.Entity_Subjects.Where( s => s.EntityTypeId == entityTypeId && s.Subject.Contains( keyword ) ).OrderBy( s => s.Subject ).Take( maxTerms ).Select( x => x.Subject ).Distinct().ToList();

                //if ( results != null && results.Count > 0 )
                //{
                //    string prev = "";

                //    foreach ( Entity_Subjects item in results )
                //    {
                //        if ( prev != item.Subject )
                //        {
                //            list.Add( item.Subject );
                //            prev = item.Subject;
                //        }
                //    }
                //}
            }

            return list;
        } //

        //public static List<string> QuickSearch_TextValue( int entityTypeId, int categoryId, string keyword, int maxTerms = 0 )
        //{
        //    List<string> list = new List<string>();

        //    keyword = keyword.Trim();

        //    if ( maxTerms == 0 )
        //        maxTerms = 50;

        //    using ( var context = new ViewContext() )
        //    {
        //        // will only return active credentials
        //        var results = context.Entity_Reference_Summary
        //            .Where( s => s.EntityTypeId == entityTypeId
        //                && s.CategoryId == categoryId
        //                && (
        //                    ( s.TextValue.Contains( keyword ) ||
        //                    ( s.Title.Contains( keyword ) )
        //                    )
        //                ) )
        //            .OrderBy( s => s.TextValue )
        //            .Select( m => m.TextValue ).Distinct()
        //            .Take( maxTerms )
        //            .ToList();

        //        if ( results != null && results.Count > 0 )
        //        {

        //            foreach ( string item in results )
        //            {
        //                list.Add( item );
        //            }

        //        }
        //    }

        //    return list;
        //}

        public static List<string> QuickSearch_ReferenceFrameworks( int entityTypeId, int categoryId, string headerId, string keyword, int maxTerms = 0 )
        {
            var list = new List<string>();

            keyword = keyword.Trim();
            var coded = keyword.Replace( "-", "" ).Replace( " ", "" );

            if ( maxTerms == 0 ) maxTerms = 50;

            if ( headerId == "0" ) headerId = "";

            using ( var context = new ViewContext() )
            {
                var results = ( from rf in context.Entity_ReferenceFramework_Totals                         
                         let cn = rf.CodedNotation.Replace( "-", "" ).Replace( " ", "" )
                         where ( headerId == "" || rf.CodeGroup == headerId )
                                 && ( rf.CategoryId == categoryId )
                                 && ( rf.EntityTypeId == entityTypeId )
                                 && ( keyword == ""
                                 || rf.CodedNotation.Contains( keyword )
                                 || cn.Contains( coded )
                                 || rf.Name.Contains( keyword ) )
                                 && ( rf.Totals > 0 )
                         orderby rf.Name
                         select rf ).Distinct().Take( maxTerms ).ToList();

                results.ForEach( x => {
                    var cd = "";
                   if ( !string.IsNullOrEmpty( x.CodedNotation ) )
                        cd = string.Format( " ({0})", x.CodedNotation );

                    list.Add( string.Format( "{0}{1}", x.Name, cd ) );
                } );
                //list = context.Entity_ReferenceFramework_Totals
                //        .Where( s => ( headerId == "" || s.CodeGroup == headerId )
                //        && ( s.CategoryId == categoryId )
                //        && ( s.EntityTypeId == entityTypeId )
                //        && ( keyword == ""
                //        || s.CodedNotation.Contains( keyword )
                //        || s.Name.Contains( keyword ) )
                //        && ( s.Totals > 0 ) )
                //    .OrderBy( s => s.Name )
                //    .Take( maxTerms )
                //    .Select( x => x.Name )
                //    .Distinct()
                //    .ToList();
            }

            return list;
        } //
          
          /// <summary>
          /// Get single entity reference, summary
          /// </summary>
          /// <param name="entityReferenceId"></param>
          /// <returns></returns>
        public static ThisEntity Get( int entityReferenceId )
        {
            ThisEntity entity = new ThisEntity();
            if ( entityReferenceId == 0 )
            {
                return entity;
            }
            try
            {
                using ( var context = new ViewContext() )
                {
                    Entity_Reference_Summary item = context.Entity_Reference_Summary
                            .SingleOrDefault( s => s.EntityReferenceId == entityReferenceId );

                    if ( item != null && item.EntityReferenceId > 0 )
                    {
                        MapFromDB( item, entity );
                    }
                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".Entity_Get" );
            }
            return entity;
        }//
        private static void MapToDB( ThisEntity from, DBEntity to )
        {
            //want to ensure fields from create are not wiped
            if ( to.Id == 0 )
            {
                //if ( IsValidDate( from.Created ) )
                //	to.Created = from.Created;
                //to.CreatedById = from.CreatedById;
            }
            //to.Id = from.Id;
            to.CategoryId = from.CategoryId;

            //in some cases may not require text, so fill with empty string
            to.Title = from.TextTitle != null ? from.TextTitle : "";
            if ( to.CategoryId == CodesManager.PROPERTY_CATEGORY_PHONE_TYPE )
            {
                to.TextValue = PhoneNumber.StripPhone( GetData( from.TextValue ) );
            }
            else if ( to.CategoryId == CodesManager.PROPERTY_CATEGORY_ORGANIZATION_IDENTIFIERS )
            {
                //if other, notify admin, and need to handle separately
                if ( from.CodeId == 88 )
                {
                    if ( from.Id == 0 )
                    {
                        //will want to create a property value, and send email
                        //could append to text for now
                        //op.OtherValue += "{" + ( frameworkName ?? "missing framework name" ) + "; " + schemaUrl + "}";
                        LoggingHelper.DoTrace( 2, "A new organization identifier of 'other' has been added:" + from.TextValue );
                        SendNewOtherIdentityNotice( from );
                    }
                }
                else
                {
                    //should ignore to.Title
                    to.Title = "";
                }
                to.TextValue = from.TextValue;
            }
            else
            {
                to.TextValue = from.TextValue;
            }

            if ( from.CodeId > 0 )
                to.PropertyValueId = from.CodeId;
            else if ( !string.IsNullOrWhiteSpace( from.CodeSchema ) )
            {
                CodeItem item = CodesManager.GetPropertyBySchema( to.CategoryId, from.CodeSchema );
                if ( item != null && item.Id > 0 )
                {
                    to.PropertyValueId = item.Id;
                    if ( string.IsNullOrWhiteSpace( to.Title ) )
                        to.Title = item.Title;
                }
            }
            else
                to.PropertyValueId = null;

        }
        private static void MapFromDB( DBEntity from, ThisEntity to )
        {
            to.Id = from.Id;
            to.ParentId = from.EntityId;
            to.TextTitle = from.Title ?? "";
            to.CategoryId = from.CategoryId;
            if ( to.CategoryId == CodesManager.PROPERTY_CATEGORY_PHONE_TYPE )
            {
                to.TextValue = PhoneNumber.DisplayPhone( from.TextValue );
            }
            else
                to.TextValue = from.TextValue;
            //else if ( to.CategoryId == CodesManager.PROPERTY_CATEGORY_ORGANIZATION_IDENTIFIERS )
            //{
            //    to.TextValue = from.TextValue;
            //}
            //else
            //{
            //    to.TextValue = from.TextValue;
            //}

            to.CodeId = from.PropertyValueId ?? 0;
            if (string.IsNullOrWhiteSpace(to.TextTitle ) )
                to.ProfileSummary = to.TextValue;
            else
                to.ProfileSummary = to.TextTitle + " - " + to.TextValue;
            to.CodeTitle = "";
            to.CodeSchema = "";
            if ( from.Codes_PropertyValue != null && from.Codes_PropertyValue.Id > 0 )
            {
                to.CodeTitle = from.Codes_PropertyValue.Title;
                to.CodeSchema = from.Codes_PropertyValue.SchemaName ?? "";
            }
            if ( from.Entity != null && from.Entity.Id > 0 )
            {
                to.EntityBaseId = from.Entity.EntityBaseId ?? 0;
                to.EntityTypeId = from.Entity.EntityTypeId;
            }
            if ( IsValidDate( from.Created ) )
                to.Created = ( DateTime )from.Created;

            if ( IsValidDate( from.LastUpdated ) )
                to.LastUpdated = ( DateTime )from.LastUpdated;

        }

        private static void MapFromDB( Entity_Reference_Summary from, ThisEntity to )
        {
            //core properties
            to.Id = from.EntityReferenceId;
            to.EntityId = from.EntityId;

            to.TextTitle = from.Title ?? "";
            to.CategoryId = from.CategoryId;
            if ( to.CategoryId == CodesManager.PROPERTY_CATEGORY_PHONE_TYPE )
                to.TextValue = PhoneNumber.DisplayPhone( from.TextValue );
            else
                to.TextValue = from.TextValue;

            to.CodeId = ( int )( from.PropertyValueId ?? 0 );
            to.CodeTitle = from.PropertyValue;
            to.CodeSchema = from.PropertySchema ?? "";

            to.ProfileSummary = to.TextTitle + " - " + to.TextValue;

            //from entity
            to.EntityBaseId = from.EntityBaseId;
        }

        private static void SendNewOtherIdentityNotice( ThisEntity entity )
        {
            string message = string.Format( "New identity. <ul><li>OrganizationId: {0}</li><li>PersonId: {1}</li><li>Title: {2}</li><li>Value: {3}</li></ul>", entity.EntityBaseId, entity.LastUpdatedById, entity.TextTitle, entity.TextValue );
            workIT.Utilities.EmailManager.NotifyAdmin( "New Organization Identity has been created", message );
        }
        #endregion

    }
}
