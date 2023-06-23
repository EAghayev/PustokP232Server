using System.ComponentModel.DataAnnotations;

namespace Pustok.Models
{
    public class Setting
    {
        [Required]
        [MaxLength(35)]
        public string Key { get; set; }
        [MaxLength(500)]
        public string Value { get; set; }
    }
}
