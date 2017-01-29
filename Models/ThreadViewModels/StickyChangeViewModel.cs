using System;
using System.ComponentModel.DataAnnotations;

namespace Palaver.Models.ThreadViewModels
{
    public class StickyChangeViewModel
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public bool IsSticky { get; set; }
        [Required]
        public DateTime Updated { get; set; }
    }
}
