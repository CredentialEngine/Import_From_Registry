using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;

using WorkIT.Web.Models;
using workIT.Services;
using workIT.Utilities;

namespace WorkIT.Web
{
	public class EmailService : IIdentityMessageService
	{
		public Task SendAsync( IdentityMessage message )
		{
			// NOTE: the passed subject is essentially a code. The actual subject will be set in the account services method
			if ( message.Subject == "Reset_Password" )
			{
				//change to treat incoming as subject code - for flexibility
				AccountServices.SendEmail_ResetPassword( message.Destination, message.Body );
			}
			else if ( message.Subject == "Confirm_Account" )
			{
				AccountServices.SendEmail_ConfirmAccount( message.Destination, message.Body );
			}
			else
			{
				//have a fall back to admin if code not recognized
				AccountServices.SendEmail_MissingSubjectType( message.Subject, message.Destination, message.Body );
			}
			return Task.FromResult( 0 );
		}
	}

	public class SmsService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }

    // Configure the application user manager used in this application. UserManager is defined in ASP.NET Identity and is used by the application.
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IUserStore<ApplicationUser> store)
            : base(store)
        {
        }

		public static ApplicationUserManager Create( IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context )
		{
			var manager = new ApplicationUserManager( new UserStore<ApplicationUser>( context.Get<ApplicationDbContext>() ) );
			// Configure validation logic for usernames
			manager.UserValidator = new UserValidator<ApplicationUser>( manager )
			{
				AllowOnlyAlphanumericUserNames = false,
				RequireUniqueEmail = true
			};

			// Configure validation logic for passwords
			manager.PasswordValidator = new CustomPasswordValidator
			{
				RequiredLength = 8,
				RequireNonLetterOrDigit = true,
				RequireDigit = false,
				RequireLowercase = true,
				RequireUppercase = true,
			};

			// Configure user lockout defaults
			manager.UserLockoutEnabledByDefault = true;
			manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes( 5 );
			manager.MaxFailedAccessAttemptsBeforeLockout = 5;

			// Register two factor authentication providers. This application uses Phone and Emails as a step of receiving a code for verifying the user
			// You can write your own provider and plug it in here.
			manager.RegisterTwoFactorProvider( "Phone Code", new PhoneNumberTokenProvider<ApplicationUser>
			{
				MessageFormat = "Your security code is {0}"
			} );
			manager.RegisterTwoFactorProvider( "Email Code", new EmailTokenProvider<ApplicationUser>
			{
				Subject = "Security Code",
				BodyFormat = "Your security code is {0}"
			} );
			manager.EmailService = new EmailService();
			manager.SmsService = new SmsService();
			var dataProtectionProvider = options.DataProtectionProvider;
			if ( dataProtectionProvider != null )
			{

				if ( UtilityManager.GetAppKeyValue( "envType" ) == "dev" )
				{
					//controlling expiration - will this affect remembering day by day?
					manager.UserTokenProvider =
				   new DataProtectorTokenProvider<ApplicationUser>
					  ( dataProtectionProvider.Create( "ASP.NET Identity" ) )
				   {
					   TokenLifespan = TimeSpan.FromHours( 24 )
				   };
				}
				else
				{
					manager.UserTokenProvider =
						new DataProtectorTokenProvider<ApplicationUser>( dataProtectionProvider.Create( "ASP.NET Identity" ) );
				}


			}
			return manager;
		}
	}

    // Configure the application sign-in manager which is used in this application.
    public class ApplicationSignInManager : SignInManager<ApplicationUser, string>
    {
        public ApplicationSignInManager(ApplicationUserManager userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager)
        {
        }

        public override Task<ClaimsIdentity> CreateUserIdentityAsync(ApplicationUser user)
        {
            return user.GenerateUserIdentityAsync((ApplicationUserManager)UserManager);
        }

        public static ApplicationSignInManager Create(IdentityFactoryOptions<ApplicationSignInManager> options, IOwinContext context)
        {
            return new ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(), context.Authentication);
        }
		public async Task<SignInStatus> CTISignInAsync( string userName, string password, bool isPersistent, bool shouldLockout )
		{
			//TODO - custom version to enable use of an admin password
			//ApplicationUser user = this.UserManager.FindByName( userName );
			//if ( null != user )
			//{
			//	if ( true == user.AccountLocked )
			//	{
			//		return ( SignInStatus.LockedOut );
			//	}
			//}

			var result = await base.PasswordSignInAsync( userName, password, isPersistent, shouldLockout );

			return ( result );
		}
	}

	public class CustomPasswordValidator : PasswordValidator
	{
		public override async Task<IdentityResult> ValidateAsync( string password )
		{
			var requireNonLetterOrDigit = base.RequireNonLetterOrDigit;
			base.RequireNonLetterOrDigit = false;
			var result = await base.ValidateAsync( password );

			if ( !requireNonLetterOrDigit )
				return result;

			if ( !Enumerable.All<char>( ( IEnumerable<char> ) password, new Func<char, bool>( this.IsLetterOrDigit ) ) )
				return result;

			// Build a new list of errors so that the custom 'PasswordRequireNonLetterOrDigit' could be added. 
			List<string> list = new List<string>();
			foreach ( var error in result.Errors )
			{
				list.Add( error );
			}
			// Add our own message: (The default by MS is: 'Passwords must have at least one non letter or digit character.')
			list.Add( "Passwords must have at least one character that is neither a letter or digit. (E.g. '- $ % _ etc.')" );
			result = await Task.FromResult<IdentityResult>( IdentityResult.Failed( string.Join( " ", ( IEnumerable<string> ) list ) ) );

			return result;
		}
	}
}
