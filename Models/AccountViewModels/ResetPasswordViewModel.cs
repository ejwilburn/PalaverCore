using System.ComponentModel.DataAnnotations;

namespace PalaverCore.Models.AccountViewModels
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Username { get; set; }

        // TODO: Make min length config-file based.
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 4)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }
}
