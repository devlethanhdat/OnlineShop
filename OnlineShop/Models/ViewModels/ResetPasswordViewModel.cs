using System.ComponentModel.DataAnnotations;

namespace OnlineShop.Models.ViewModels
{
    public class ResetPasswordViewModel
    {
        public string Email { get; set; }
        [Required]
        public int RecoveryCode { get; set; }
        [Required]
        public string NewPassword { get; set; }
    }
}
