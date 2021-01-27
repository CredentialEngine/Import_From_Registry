using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
//using System.Web.Mvc;
using System.Web.SessionState;
using SystemWebHttpContext = System.Web.HttpContext;

using workIT.Models;

using workIT.Utilities;
using workIT.Factories;
//using workIT.Data;

namespace workIT.Services
{
    public class AccountServices
    {
        private static string thisClassName = "AccountServices";

        #region Authorization methods
        public static bool IsUserAnAdmin()
        {
            AppUser user = GetUserFromSession();
            if ( user == null || user.Id == 0 )
                return false;

            return IsUserAnAdmin( user );
        }
        public static bool IsUserAnAdmin( AppUser user )
        {
            if ( user == null || user.Id == 0 )
                return false;

            if ( user.UserRoles.Contains( "Administrator" ) )
                return true;
            else
                return false;
        }
        public static bool IsUserSiteStaff()
        {
            AppUser user = GetUserFromSession();
            if ( user == null || user.Id == 0 )
                return false;

            return IsUserSiteStaff( user );
        }
        public static bool IsUserSiteStaff( AppUser user )
        {
            if ( user == null || user.Id == 0 )
                return false;

            if ( user.UserRoles.Contains( "Administrator" )
              || user.UserRoles.Contains( "Site Manager" )
              || user.UserRoles.Contains( "Site Staff" )
                )
                return true;
            else
                return false;
        }

		/// <summary>
		/// Return true if user can view all parts of site.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		public static bool CanUserViewAllOfSite( AppUser user )
		{
			if ( user == null || user.Id == 0 )
				return false;

			if ( user.UserRoles.Contains( "Administrator" )
			  || user.UserRoles.Contains( "Site Manager" )
			  || user.UserRoles.Contains( "Site Staff" )
			  || user.UserRoles.Contains( "Site Partner" )
			  || user.UserRoles.Contains( "Site Reader" )
				)
				return true;
			else
				return false;
		}

		/// <summary>
		/// If true, user can view site during the beta period.
		/// Checks for a user in the session
		/// </summary>
		/// <returns></returns>
		public static bool CanUserViewSite()
		{
			//this method will not expect a status message
			string status = "";
			if ( UtilityManager.GetAppKeyValue( "isSiteInBeta", true ) == false )
			{
				return true;
			}

			AppUser user = GetUserFromSession();
			if ( user == null || user.Id == 0 )
				return false;
			return CanUserViewSite( user, ref status );
		}

		/// <summary>
		/// If true, user can view site during the beta period
		/// </summary>
		/// <param name="user"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		private static bool CanUserViewSite( AppUser user, ref string status )
		{
			//this method will not expect a status message
			status = "";
			if ( user == null || user.Id == 0 )
			{
				status = "You must be authenticated and authorized before being allowed to view any content.";
				return false;
			}

			if ( user.UserRoles.Contains( "Administrator" )
			  || user.UserRoles.Contains( "Site Manager" )
			  || user.UserRoles.Contains( "Site Staff" )
			  || user.UserRoles.Contains( "Site Partner" )
			  || user.UserRoles.Contains( "Site Reader" )
				)
				return true;

			// allow if user is member of an org
			//depends on purpose, if site in general, ok, but not for viewing unpublished stuff
			//**if ( OrganizationManager.IsMemberOfAnyOrganization( user.Id ) )
			//	return true;

			return false;
		}
		//public static bool CanUserViewDetails( AppUser user, ref string status )
		//{
		//	//this method will not expect a status message
		//	status = "";
		//	if ( user == null || user.Id == 0 )
		//	{
		//		status = "You must be authenticated and authorized before being allowed to view any content.";
		//		return false;
		//	}

		//	if ( user.UserRoles.Contains( "Administrator" )
		//	  || user.UserRoles.Contains( "Site Manager" )
		//	  || user.UserRoles.Contains( "Site Staff" )
		//	  || user.UserRoles.Contains( "Site Partner" )
		//	  || user.UserRoles.Contains( "Site Reader" )
		//		)
		//		return true;


		//	return false;
		//}
		public static bool CanUserViewAllContent( AppUser user )
		{
			//this method will not expect a status message
			//status = "";
			if ( user == null || user.Id == 0 )
			{
				//status = "You must be authenticated and authorized before being allowed to view any content.";
				return false;
			}

			if ( user.UserRoles.Contains( "Administrator" )
			  || user.UserRoles.Contains( "Site Manager" )
			  || user.UserRoles.Contains( "Site Staff" )
			  || user.UserRoles.Contains( "Site Partner" )
			  || user.UserRoles.Contains( "Site Reader" )
				)
				return true;

			bool canEditorsViewAll = UtilityManager.GetAppKeyValue( "canEditorsViewAll", false );
			//if allowing anyone with edit for any org return true;
			//**if ( canEditorsViewAll && OrganizationServices.IsMemberOfAnOrganization( user.Id ) )
			//	return true;

			return false;
		}
        //public static bool CanUserEditAllContent()
        //{
        //	AppUser user = GetUserFromSession();
        //	if ( user == null || user.Id == 0 )
        //	{
        //		//status = "You must be authenticated and authorized before being allowed to view any content.";
        //		return false;
        //	}

        //	if ( user.UserRoles.Contains( "Administrator" )
        //	  || user.UserRoles.Contains( "Site Manager" )
        //	  || user.UserRoles.Contains( "Site Staff" )
        //		)
        //		return true;

        //	return false;
        //}


        ///// <summary>
        ///// Return true if current user can create content
        ///// Called from header. 
        ///// </summary>
        ///// <returns></returns>
        //public static bool CanUserCreateContent()
        //{
        //    //this method will not expect a status message
        //    string status = "";
        //    AppUser user = GetUserFromSession();
        //    if ( user == null || user.Id == 0 )
        //        return false;
        //    return CanUserCreateContent( user, ref status );
        //}

        ///// <summary>
        ///// Return true if user can publish content
        ///// Essentially this relates to being able to create credentials and related entities. 
        ///// </summary>
        ///// <param name="user"></param>
        ///// <param name="status"></param>
        ///// <returns></returns>
        //public static bool CanUserCreateContent( AppUser user, ref string status )
        //{
        //    status = "";
        //    if ( user == null || user.Id == 0 )
        //    {
        //        status = "You must be authenticated and authorized before being allowed to create any content.";
        //        return false;
        //    }

        //    if ( user.UserRoles.Contains( "Administrator" )
        //      || user.UserRoles.Contains( "Site Manager" )
        //      || user.UserRoles.Contains( "Site Staff" )
        //      || user.UserRoles.Contains( "Site Partner" )
        //        )
        //        return true;

        //    bool canEditorsViewAll = UtilityManager.GetAppKeyValue( "canEditorsViewAll", false );
        //    //if allowing anyone with edit for any org return true;
        //    if ( canEditorsViewAll && OrganizationServices.IsMemberOfAnOrganization( user.Id ) )
        //        return true;

        //    //allow once out of beta, and user is member of an org
        //    if ( UtilityManager.GetAppKeyValue( "isSiteInBeta", true ) == false
        //        && OrganizationManager.IsMemberOfAnyOrganization( user.Id ) )
        //        return true;

        //    status = "Sorry - You have not been authorized to add or update content on this site during this <strong>BETA</strong> period. Please contact site management if you believe that you should have access during this <strong>BETA</strong> period.";

        //    return false;
        //}


        ///// <summary>
        ///// Perform basic authorization checks. First establish an initial user object.
        ///// Used where the user object is not to be returned.
        ///// </summary>
        ///// <param name="action"></param>
        ///// <param name="mustBeLoggedIn"></param>
        ///// <param name="status"></param>
        ///// <returns></returns>
        //public static bool AuthorizationCheck( ref string status )
        //{
        //    AppUser user = GetCurrentUser();
        //    return AuthorizationCheck( "", false, ref status, ref user );
        //}
        ///// <summary>
        ///// Do auth check - where user is not expected back, so can be instantiate here and passed to next version
        ///// </summary>
        ///// <param name="action"></param>
        ///// <param name="mustBeLoggedIn"></param>
        ///// <param name="status"></param>
        ///// <returns></returns>
        //public static bool AuthorizationCheck( string action, bool mustBeLoggedIn, ref string status )
        //{

        //    AppUser user = new AppUser(); //GetCurrentUser();
        //    return AuthorizationCheck( action, false, ref status, ref user );
        //}
        ///// <summary>
        ///// Perform basic authorization checks
        ///// </summary>
        ///// <param name="action"></param>
        ///// <param name="mustBeLoggedIn"></param>
        ///// <param name="status"></param>
        ///// <param name="user"></param>
        ///// <returns></returns>
        //public static bool AuthorizationCheck( string action, bool mustBeLoggedIn, ref string status, ref AppUser user )
        //{
        //    bool isAuthorized = true;
        //    user = GetCurrentUser();
        //    bool isAuthenticated = IsUserAuthenticated( user );
        //    if ( mustBeLoggedIn && !isAuthenticated )
        //    {
        //        status = string.Format( "You must be logged in to do that ({0}).", action );
        //        return false;
        //    }

        //    if ( action == "Delete" )
        //    {

        //        //TODO: validate user's ability to delete a specific entity (though this should probably be handled by the delete method?)
        //        //if ( AccountServices.IsUserSiteStaff( user ) == false )
        //        //{
        //        //	ConsoleMessageHelper.SetConsoleInfoMessage( "Sorry - You have not been authorized to delete content on this site during this <strong>BETA</strong> period.", "", false );

        //        //	status = "You have not been authorized to delete content on this site during this BETA period.";
        //        //	return false;
        //        //}
        //    }
        //    return isAuthorized;

        //}

        ///// <summary>
        ///// Perform common checks to see if a user can edit something
        ///// </summary>
        ///// <param name="valid"></param>
        ///// <param name="status"></param>
        ////public static void EditCheck( ref bool valid,
        ////					ref string status )
        ////{
        ////	var user = GetUserFromSession();

        ////	if ( !AuthorizationCheck( "edit", true, ref status, ref user ) )
        ////	{
        ////		valid = false;
        ////		status = "ERROR - NOT AUTHENTICATED. You will not be able to add or update content";
        ////		ConsoleMessageHelper.SetConsoleInfoMessage( status, "", false );
        ////		return;
        ////	}

        ////	if ( !CanUserPublishContent( user, ref status ) )
        ////	{
        ////		valid = false;
        ////		//Status already set
        ////		ConsoleMessageHelper.SetConsoleInfoMessage( status, "", false );
        ////		return;
        ////	}

        ////	valid = true;
        ////	status = "okay";
        ////	return;
        ////}
        ////

        #endregion

        #region Create/Update
        /// <summary>
        /// Create a new account, based on the AspNetUser info!
        /// </summary>
        /// <param name="email"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="userName"></param>
        /// <param name="userKey"></param>
        /// <param name="password">NOTE: may not be necessary as the hash in the aspNetUsers table is used?</param>
        /// <param name="statusMessage"></param>
        /// <returns></returns>
        public int Create( string email, string firstName, string lastName, string userName, string userKey, string password, string externalCEAccountIdentifier,
                ref string statusMessage,
                bool doingEmailConfirmation = false,
                bool isExternalSSO = false )
        {
            int id = 0;
            statusMessage = "";
            //this password, as stored in the account table, is not actually used
            string encryptedPassword = "";
            if ( !string.IsNullOrWhiteSpace(password) )
                encryptedPassword = UtilityManager.Encrypt(password);

            AppUser user = new AppUser()
            {
                Email = email,
                UserName = email,
                FirstName = firstName,
                LastName = lastName,
                IsActive = !doingEmailConfirmation,
                AspNetUserId = userKey,
                Password = encryptedPassword,
                CEAccountIdentifier = externalCEAccountIdentifier
            };
            id = new AccountManager().Add(user, ref statusMessage);
            if ( id > 0 )
            {
                //don't want to add to session, user needs to confirm
                //AddUserToSession( HttpContext.Current.Session, user );


                string msg = string.Format("New user registration. <br/>Email: {0}, <br/>Name: {1}<br/>Type: {2}", email, firstName + " " + lastName, ( isExternalSSO ? "External SSO" : "Forms" ));
                ActivityServices.UserRegistration(user, "registration", msg);
                //EmailManager.SendSiteEmail( "New Credential Publisher Account", msg );
            }

            return id;
        } //

          /// <summary>
          /// Create a new account, based on the AspNetUser info!
          /// </summary>
          /// <param name="email"></param>
          /// <param name="firstName"></param>
          /// <param name="lastName"></param>
          /// <param name="userName"></param>
          /// <param name="userKey"></param>
          /// <param name="password">NOTE: may not be necessary as the hash in the aspNetUsers table is used?</param>
          /// <param name="statusMessage"></param>
          /// <returns></returns>
        public int Create( string email, string firstName, string lastName, string userName, string userKey, string password,
				ref string statusMessage,
				bool doingEmailConfirmation = false,
				bool isExternalSSO = false )
		{
			int id = 0;
			statusMessage = "";
			//this password, as stored in the account table, is not actually used
			string encryptedPassword = "";
			if ( !string.IsNullOrWhiteSpace( password ) )
				encryptedPassword = UtilityManager.Encrypt( password );

			AppUser user = new AppUser()
			{
				Email = email,
				UserName = email,
				FirstName = firstName,
				LastName = lastName,
				IsActive = !doingEmailConfirmation,
				AspNetUserId = userKey,
				Password = encryptedPassword
			};
			id = new AccountManager().Add( user, ref statusMessage );
			if ( id > 0 )
			{
				//don't want to add to session, user needs to confirm
				//AddUserToSession( HttpContext.Current.Session, user );

				ActivityServices.UserRegistration( user );
				string msg = string.Format( "New user registration. <br/>Email: {0}, <br/>Name: {1}<br/>Type: {2}", email, firstName + " " + lastName, ( isExternalSSO ? "External SSO" : "Forms" ) );

				EmailManager.SendSiteEmail( "New Credential Finder Account", msg );
			}

			return id;
		} //

		///// <summary>
		///// Account created by a third party, not through registering
		///// </summary>
		///// <param name="email"></param>
		///// <param name="firstName"></param>
		///// <param name="lastName"></param>
		///// <param name="userName"></param>
		///// <param name="userKey"></param>
		///// <param name="password"></param>
		///// <param name="statusMessage"></param>
		///// <returns></returns>
		//public int AddAccount( string email, string firstName, string lastName, string userName, string userKey, string password,
		//            ref string statusMessage )
		//{
		//    int id = 0;
		//    statusMessage = "";
		//    //this password, as stored in the account table, is not actually used
		//    string encryptedPassword = "";
		//    if ( !string.IsNullOrWhiteSpace( password ) )
		//        encryptedPassword = UtilityManager.Encrypt( password );

		//    AppUser user = new AppUser()
		//    {
		//        Email = email,
		//        UserName = email,
		//        FirstName = firstName,
		//        LastName = lastName,
		//        IsActive = true,
		//        AspNetUserId = userKey,
		//        Password = encryptedPassword
		//    };
		//    id = new AccountManager().Add( user, ref statusMessage );
		//    if ( id > 0 )
		//    {
		//        //don't want to add to session, user needs to confirm
		//        //AddUserToSession( HttpContext.Current.Session, user );

		//        ActivityServices.UserRegistration( user );
		//        string msg = string.Format( "New Account. <br/>Email: {0}, <br/>Name: {1}<br/>Type: {2}", email, firstName + " " + lastName, "New Account" );

		//        EmailManager.SendSiteEmail( "New Credential Finder account", msg );
		//    }

		//    return id;
		//} //

		/// <summary>
		/// update account, and AspNetUser
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		public bool Update( AppUser user, bool session, ref string statusMessage )
		{
			bool successful = true;
			new AccountManager().Update( user, ref statusMessage );
			if ( successful && session )
			{
				AddUserToSession( HttpContext.Current.Session, user );
			}

			return successful;
		}

		//public bool Delete( int userId, ref string message )
		//{
		//    return new AccountManager().Delete( userId, ref message );
		//}

		public bool ActivateUser( string aspNetId )
		{
			string statusMessage = "";
			AppUser user = GetUserByKey( aspNetId );
			if ( user != null && user.Id > 0 )
			{
				user.IsActive = true;
				if ( new AccountManager().Update( user, ref statusMessage ) )
				{
					EmailManager.SendSiteEmail( "User Activated Credential Finder account", string.Format( "{0} activated a Credential Finder account. <br/>Email: {1}", user.FullName(), user.Email ) );

					return true;
				}
				else
				{
					EmailManager.NotifyAdmin( "Activate user failed", string.Format( "Attempted to activate user: {0}. <br/>Received invalid status: {1}", user.Email, statusMessage ) );
					return false;
				}
			}
			else
			{
				EmailManager.NotifyAdmin( "Activate user failed", string.Format( "Attempted to activate user aspNetId: {0}. <br/>However latter aspNetId was not found", aspNetId ) );
				return false;
			}

		}

		public bool SetUserEmailConfirmed( string aspNetId )
		{
			string statusMessage = "";
			AppUser user = GetUserByKey( aspNetId );
			if ( user != null && user.Id > 0 )
			{
				user.IsActive = true;
				if ( new AccountManager().AspNetUsers_UpdateEmailConfirmed( aspNetId, ref statusMessage ) )
				{
					return true;
				}
				else
				{
					EmailManager.NotifyAdmin( "SetUserEmailConfirmed failed", string.Format( "Attempted to SetUserEmailConfirmed for user: {0}. <br/>Received invalid status: {1}", user.Email, statusMessage ) );
					return false;
				}
			}
			else
			{
				EmailManager.NotifyAdmin( "SetUserEmailConfirmed failed", string.Format( "Attempted to SetUserEmailConfirmed for user aspNetId: {0}. <br/>However latter aspNetId was not found", aspNetId ) );
				return false;
			}

		}

		public bool SetUserEmailConfirmedByEmail( string email )
		{
			string statusMessage = "";
			if ( new AccountManager().AspNetUsers_UpdateEmailConfirmedByEmail( email, ref statusMessage ) )
			{
				return true;
			}
			else
			{
				EmailManager.NotifyAdmin( "SetUserEmailConfirmedByEmail failed", string.Format( "Attempted to SetUserEmailConfirmedByEmail for user: {0}. <br/>Received invalid status: {1}", email, statusMessage ) );
				return false;
			}

		}
		#endregion

		#region roles
		public bool AddRole( int userId, int roleId, int createdByUserId, ref string statusMessage )
		{
			return new AccountManager().AddRole( userId, roleId, createdByUserId, ref statusMessage );
		}
		public bool DeleteRole( int userId, int roleId, int updatedByUserId, ref string statusMessage )
		{
			AppUser user = AccountManager.AppUser_Get( userId );
			if ( user == null || user.Id < 1 )
			{
				statusMessage = "Error - account was not found.";
				return false;
			}

			bool isValid = new AccountManager().DeleteRole( user, roleId, updatedByUserId, ref statusMessage );
			//TODO - add logging here or in the services
			return isValid;
		}


		//public void UpdateRoles( string aspNetUserId, string[] roles )
		//{
		//	AppUser user = GetCurrentUser();
		//	new AccountManager().UpdateRoles( aspNetUserId, roles );
		//}


		#endregion

		#region email methods
		/// <summary>
		/// Send reset password email
		/// </summary>
		/// <param name="subject"></param>
		/// <param name="toEmail"></param>
		/// <param name="url">Will be a formatted callback url</param>
		public static void SendEmail_ResetPassword( string toEmail, string url )
		{
			//should have a valid email at this point (if from identityConfig)
			AppUser user = GetUserByEmail( toEmail );

			bool isSecure = false;
			if ( UtilityManager.GetAppKeyValue( "usingSSL", false ) )
				isSecure = true;
			string bcc = UtilityManager.GetAppKeyValue( "systemAdminEmail", "yoohoo@email.org" );

			string fromEmail = UtilityManager.GetAppKeyValue( "contactUsMailFrom", "yoohoo@email.org" );
			string subject = "Reset Password for your Credential Finder account";

			string email = EmailManager.GetEmailText( "ForgotPassword" );
			string eMessage = "";

			try
			{
				//assign and substitute: 0-FirstName, 1-callback url from AccountController
				eMessage = string.Format( email, user.FirstName, url );


				EmailManager.SendEmail( toEmail, fromEmail, subject, eMessage, "", bcc );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".SendEmail_ResetPassword()" );
			}

		}

		/// <summary>
		/// Send email to confirm new account
		/// </summary>
		/// <param name="subject"></param>
		/// <param name="toEmail"></param>
		/// <param name="url">Will be a formatted callback url</param>
		public static void SendEmail_ConfirmAccount( string toEmail, string url )
		{
			//should have a valid email at this point (if from identityConfig)
			AppUser user = GetUserByEmail( toEmail );

			
			//string toEmail = user.Email;
			string bcc = UtilityManager.GetAppKeyValue( "systemAdminEmail", "yoohoo@email.org" );

			string fromEmail = UtilityManager.GetAppKeyValue( "contactUsMailFrom", "yoohoo@email.org" );
			string subject = "Confirm Your Credential Finder Account";
			string email = EmailManager.GetEmailText( "ConfirmAccount" );
			string eMessage = "";

			try
			{

				//assign and substitute: 0-FirstName, 1-body from AccountController
				eMessage = string.Format( email, user.FirstName, url );

				EmailManager.SendEmail( toEmail, fromEmail, subject, eMessage, "", bcc );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".SendEmail_ConfirmPassword()" );
			}

		}

		public static void SendEmail_OnUnConfirmedEmail( string userEmail )
		{
			//should have a valid email at this point
			AppUser user = GetUserByEmail( userEmail );
			string subject = "Forgot password attempt with unconfirmed email";

			string toEmail = UtilityManager.GetAppKeyValue( "systemAdminEmail", "yoohoo@email.org" );

			string fromEmail = UtilityManager.GetAppKeyValue( "contactUsMailFrom", "yoohoo@email.org" );
			//string subject = "Forgot Password";
			string email = "User: {0} attempted Forgot Password, and email has not been confirmed.<br/>Email: {1}<br/>Created: {2}";
			string eMessage = "";

			try
			{
				eMessage = string.Format( email, user.FullName(), user.Email, user.Created );

				EmailManager.SendEmail( toEmail, fromEmail, subject, eMessage, "", "" );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".SendEmail_OnUnConfirmedEmail()" );
			}
		}


		public static void SendEmail_MissingSubjectType( string subject, string toEmail, string body )
		{
			//should have a valid email at this point (if from identityConfig)
			AppUser user = GetUserByEmail( toEmail );

			bool isSecure = false;

			string eMessage = string.Format( "To email: {0}<br/>Subject: {1}<br/>Body: {2}", toEmail, subject, body );

			try
			{
				EmailManager.NotifyAdmin( "Email Subject Type not handled", "<p>Unexpected email subject encountered</p>" + eMessage );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".SendEmail_MissingSubjectType()" );
			}

		}
		#endregion

		#region Read methods
		///// <summary>
		///// User is authenticated, either get from session or via the Identity name
		///// </summary>
		///// <param name="identityName"></param>
		///// <returns></returns>
		public static AppUser GetCurrentUser( string identityName = "" )
        {
            AppUser user = AccountServices.GetUserFromSession();
            //if ( ( user == null || user.Id == 0 ) && !string.IsNullOrWhiteSpace( identityName ) )
            //{
            //	//NOTE identityName is related to the UserName
            //	//TODO - need to add code to prevent dups between google register and direct register
            //	user = GetUserByUserName( identityName );
            //	if ( user != null && user.Id > 0 )
            //		AddUserToSession( HttpContext.Current.Session, user );
            //}

            return user;
        } //
	
		  /// <summary>
		  /// Retrieve a user by email address
		  /// </summary>
		  /// <param name="email"></param>
		  /// <returns></returns>
		public static AppUser GetUserByEmail( string email )
		{
			AppUser user = AccountManager.AppUser_GetByEmail( email );

			return user;
		} //
		public static AppUser GetUserByUserName( string username )
		{
			AppUser user = AccountManager.GetUserByUserName( username );

			return user;
		}
		/// <summary>
		/// Get user by email, and add to the session
		/// </summary>
		/// <param name="email"></param>
		/// <returns></returns>
		public static AppUser SetUserByEmail( string email )
		{
			AppUser user = AccountManager.AppUser_GetByEmail( email );



			AddUserToSession( HttpContext.Current.Session, user );
			return user;
		} //


		public static int GetCurrentUserId()
		{
			AppUser user = AccountServices.GetUserFromSession();
			if ( user == null || user.Id == 0 )
				return 0;
			else
				return user.Id;
		} //

		/// <summary>
		/// set the current user via an identity name at session start
		/// </summary>
		/// <param name="identityName"></param>
		/// <returns></returns>
		public static AppUser SetCurrentUser( string identityName )
		{
			AppUser user = AccountServices.GetUserFromSession();
			if ( !string.IsNullOrWhiteSpace( identityName ) )
			{
				//assume identityName is email
				//TODO - need to add code to prevent dups between google register and direct register
				user = GetUserByEmail( identityName );
				if ( user != null && user.Id > 0 )
					AddUserToSession( HttpContext.Current.Session, user );
			}

			return user;
		} //

		/// <summary>
		/// get account by the aspNetId,and add to session
		/// </summary>
		/// <param name="aspNetId"></param>
		/// <returns></returns>
		public static AppUser GetUserByKey( string aspNetId )
		{
			AppUser user = AccountManager.AppUser_GetByKey( aspNetId );

			AddUserToSession( HttpContext.Current.Session, user );

			return user;
		} //
		public static AppUser GetUser( int id )
		{
			AppUser user = AccountManager.AppUser_Get( id );

			return user;
		} //
        public static AppUser GetUserByCEAccountId( string accountIdentifier )
        {
            AppUser user = AccountManager.GetUserByCEAccountId(accountIdentifier);

            return user;
        } //
        public static AppUser GetAccount( int id )
		{
			return AccountManager.Get( id );
		} //

		public static List<AppUser> EmailAutocomplete( string keyword = "", int maxTerms = 25 )
		{
			int userId = AccountServices.GetCurrentUserId();
			int pTotalRows = 0;
			string filter = string.Format( " ( email like '%{0}%'  or FirstName like '%{0}%'  or lastName like '%{0}%' ) ", keyword );
			return AccountManager.Search( filter, "Email", 1, maxTerms, ref pTotalRows );
		}

		//          public static List<AppUser> SearchByKeyword( string keywords, string pOrderBy, string sortDirection, int pageNumber, int pageSize, ref int pTotalRows )
		//          {
		//              string filter = "";
		//              SetKeywordFilter( keywords, ref filter );
		//              return Search( filter, pOrderBy, sortDirection, pageNumber, pageSize, ref pTotalRows );
		//          }

		//          /// <summary>
		//          /// Search using filter already formatted
		//          /// </summary>
		//          /// <param name="filter"></param>
		//          /// <param name="pOrderBy"></param>
		//          /// <param name="sortDirection"></param>
		//          /// <param name="pageNumber"></param>
		//          /// <param name="pageSize"></param>
		//          /// <param name="pTotalRows"></param>
		//          /// <returns></returns>
		//          public static List<AppUser> Search( string filter, string pOrderBy, string sortDirection, int pageNumber, int pageSize, ref int pTotalRows )
		//          {
		//              //probably should validate valid order by - or do in proc
		//              if ( string.IsNullOrWhiteSpace( pOrderBy ) )
		//                  pOrderBy = "LastName";

		//              if ( "firstname lastname email id created lastlogon".IndexOf( pOrderBy.ToLower() ) == -1 )
		//                  pOrderBy = "LastName";

		//              if ( sortDirection.ToLower() == "desc" )
		//                  pOrderBy += " DESC";


		//              //string filter = "";
		//              int userId = 0;
		//              AppUser user = AccountServices.GetCurrentUser();
		//              if ( user != null && user.Id > 0 )
		//                  userId = user.Id;


		//              return AccountManager.Search( filter, pOrderBy, pageNumber, pageSize, ref pTotalRows, userId );
		//          }

		//          private static void SetKeywordFilter( string keywords, ref string where )
		//          {
		//              if ( string.IsNullOrWhiteSpace( keywords ) )
		//                  return;
		//              string text = " (FirstName like '{0}' OR LastName like '{0}'  OR Email like '{0}'  ) ";

		//              string AND = "";
		//              if ( where.Length > 0 )
		//                  AND = " AND ";
		//              //
		//              keywords = ServiceHelper.HandleApostrophes( keywords );
		//              if ( keywords.IndexOf( "%" ) == -1 )
		//                  keywords = "%" + keywords.Trim() + "%";

		//              where = where + AND + string.Format( " ( " + text + " ) ", keywords );


		//          }

		//          /// <summary>
		//          /// determine which results a user may view, and eventually edit
		//          /// </summary>
		//          /// <param name="data"></param>
		//          /// <param name="user"></param>
		//          /// <param name="where"></param>
		//          private static void SetAuthorizationFilter( AppUser user, ref string where )
		//          {
		//              string AND = "";

		//              if ( where.Length > 0 )
		//                  AND = " AND ";
		//              if ( user == null || user.Id == 0 )
		//              {
		//                  //public only records
		//                  where = where + AND + string.Format( " (base.StatusId = {0}) ", CodesManager.ENTITY_STATUS_PUBLISHED );
		//                  return;
		//              }

		//              if ( AccountServices.IsUserSiteStaff( user )
		//                || AccountServices.CanUserViewAllContent( user ) )
		//              {
		//                  //can view all, edit all
		//                  return;
		//              }

		//              //can only view where status is published, or associated with the org
		//              where = where + AND + string.Format( "((base.StatusId = {0}) OR (base.Id in (SELECT cs.Id FROM [dbo].[Organization.Member] om inner join [Credential_Summary] cs on om.ParentOrgId = cs.ManagingOrgId where userId = {1}) ))", CodesManager.ENTITY_STATUS_PUBLISHED, user.Id );

		//          }

		//          public static List<AspNetRoles> GetRoles()
		//          {
		//              return AccountManager.GetRoles();
		//          }

		//  */
		#endregion
		#region Session methods
		/// <summary>
		/// Determine if current user is a logged in (authenticated) user 
		/// </summary>
		/// <returns></returns>
		public static bool IsUserAuthenticated()
		{
			bool isUserAuthenticated = false;
			try
			{
				AppUser appUser = GetUserFromSession();
				isUserAuthenticated = IsUserAuthenticated( appUser );
			}
			catch
			{

			}

			return isUserAuthenticated;
		} //
		public static bool IsUserAuthenticated( AppUser appUser )
		{
			bool isUserAuthenticated = false;
			try
			{
				if ( appUser == null || appUser.Id == 0 || appUser.IsActive == false )
				{
					isUserAuthenticated = false;
				}
				else
				{
					isUserAuthenticated = true;
				}
			}
			catch
			{

			}

			return isUserAuthenticated;
		} //
		public static AppUser GetUserFromSession()
        {
            if ( HttpContext.Current != null && HttpContext.Current.Session != null )
            {
                return GetUserFromSession( HttpContext.Current.Session );
            }
            else
                return null;
        } //

        public static AppUser GetUserFromSession( HttpSessionState session )
        {
            AppUser user = new AppUser();
            try
            {       //Get the user
                user = ( AppUser )session["user"];

                if ( user.Id == 0 || !user.IsValid )
                {
                    user.IsValid = false;
                    user.Id = 0;
                }
            }
            catch
            {
                user = new AppUser();
                user.IsValid = false;
            }
            return user;
        }

		/// <summary>
		/// Sets the current user to the session.
		/// </summary>
		/// <param name="session">HTTP Session</param>
		/// <param name="appUser">application User</param>
		public static void AddUserToSession( HttpSessionState session, AppUser appUser )
		{
			session[ "user" ] = appUser;

		} //


		#endregion

		#region Proxy Code methods

		public string Create_ForgotPasswordProxyId( int userId, ref string statusMessage )
		{
			int expiryDays = UtilityManager.GetAppKeyValue( "forgotPasswordExpiryDays", 1 );
			return new AccountManager().Create_ProxyLoginId( userId, "Forgot Password", expiryDays, ref statusMessage );
		}

		/// <summary>
		/// Store an identity code
		/// </summary>
		/// <param name="proxyCode"></param>
		/// <param name="userEmail"></param>
		/// <param name="proxyType"></param>
		/// <returns></returns>
		public bool Proxies_StoreProxyCode( string proxyCode, string userEmail, string proxyType )
		{
			AppUser user = GetUserByEmail( userEmail );
			return Proxies_StoreProxyCode( proxyCode, user.Id, "ForgotPassword" );
		}

		/// <summary>
		/// Store an identity code
		/// </summary>
		/// <param name="proxyCode"></param>
		/// <param name="userId"></param>
		/// <param name="proxyType"></param>
		/// <returns></returns>
		public bool Proxies_StoreProxyCode( string proxyCode, int userId, string proxyType )
		{
			string statusMessage = "";

			int expiryDays = UtilityManager.GetAppKeyValue( "forgotPasswordExpiryDays", 1 );
			return new AccountManager().Store_ProxyCode( proxyCode, userId, proxyType, expiryDays, ref statusMessage );

		}
		public static bool Proxy_IsCodeActive( string proxyCode )
		{
			return new AccountManager().Proxy_IsCodeActive( proxyCode );
		}
		public bool Proxy_SetInactivate( string proxyCode )
		{
			string statusMessage = "";
			return new AccountManager().InactivateProxy( proxyCode, ref statusMessage );
		}

		#endregion
	}
}
