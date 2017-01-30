using System.ComponentModel.DataAnnotations;

namespace Palaver.Models.AccountViewModels
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at amax {1} characters long.", MinimumLength = 3)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
