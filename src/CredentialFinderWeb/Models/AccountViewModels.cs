using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CredentialFinderWeb.Models
{
	public class ExternalLoginConfirmationViewModel
	{
		[Required]
		[EmailAddress]
		[Display( Name = "Email" )]
		public string Email { get; set; }


		[Required]
		[Display( Name = "First Name" )]
		public string FirstName { get; set; }
		[Required]
		[Display( Name = "Last Name" )]
		public string LastName { get; set; }
	}

	public class ExternalLoginListViewModel
    {
        public string ReturnUrl { get; set; }
    }

    public class SendCodeViewModel
    {
        public string SelectedProvider { get; set; }
        public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
        public string ReturnUrl { get; set; }
        public bool RememberMe { get; set; }
    }

    public class VerifyCodeViewModel
    {
        [Required]
        public string Provider { get; set; }

        [Required]
        [Display(Name = "Code")]
        public string Code { get; set; }
        public string ReturnUrl { get; set; }

        [Display(Name = "Remember this browser?")]
        public bool RememberBrowser { get; set; }

        public bool RememberMe { get; set; }
    }

    public class ForgotViewModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

	public class RegisterViewModel
	{
		[Required]
		[EmailAddress]
		[Display( Name = "Email" )]
		public string Email { get; set; }

		[Required]
		[Display( Name = "First Name" )]
		public string FirstName { get; set; }
		[Required]
		[Display( Name = "Last Name" )]
		public string LastName { get; set; }

		[Required]
		[StringLength( 100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8 )]
		[DataType( DataType.Password )]
		[Display( Name = "Password" )]
		public string Password { get; set; }

		[DataType( DataType.Password )]
		[Display( Name = "Confirm Password" )]
		[Compare( "Password", ErrorMessage = "The password and confirmation password do not match." )]
		public string ConfirmPassword { get; set; }

		[Display( Name = "Organization" )]
		public string Organization { get; set; }

		[Display( Name = "OrganizationId" )]
		public int? OrganizationId { get; set; }
	}

	public class AccountViewModel
	{
		public int UserId { get; set; }

		[Required]
		[EmailAddress]
		[Display( Name = "Email" )]
		public string Email { get; set; }

		[Required]
		[Display( Name = "First Name" )]
		public string FirstName { get; set; }
		[Required]
		[Display( Name = "Last Name" )]
		public string LastName { get; set; }

		[Display( Name = "Roles" )]
		public string[] SelectedRoles { get; set; }
		public List<System.Web.Mvc.SelectListItem> Roles { get; set; }
		public int[] SelectedOrgs { get; set; }
		public List<System.Web.Mvc.SelectListItem> Organizations { get; set; }
	}

	public class UserProfileEdit
	{
		[Display( Name = "UserName" )]
		public string UserName { get; set; }


		[Required]
		[EmailAddress]
		[Display( Name = "Email" )]
		public string Email { get; set; }


		[Required]
		[Display( Name = "First Name" )]
		public string FirstName { get; set; }
		[Required]
		[Display( Name = "Last Name" )]
		public string LastName { get; set; }

	}
	public class ResetPasswordViewModel
	{
		[Required]
		[EmailAddress]
		[Display( Name = "Email" )]
		public string Email { get; set; }

		[Required]
		[StringLength( 100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8 )]
		[DataType( DataType.Password )]
		[Display( Name = "Password" )]
		public string Password { get; set; }

		[DataType( DataType.Password )]
		[Display( Name = "Confirm password" )]
		[Compare( "Password", ErrorMessage = "The password and confirmation password do not match." )]
		public string ConfirmPassword { get; set; }

		public string Code { get; set; }
	}


	public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}
