using System.ComponentModel.DataAnnotations;

namespace Pustok.Areas.manage.ViewModels
{
    public class AdminLoginViewModel
    {
        [MaxLength(20)]
        [MinLength(4)]
        [Required]
        public string UserName { get; set; }
        [Required]
        [MinLength(8)]
        [MaxLength(25)]
        [DataType(DataType.Password)]
        public string Password { get; set; }    
    }
}
