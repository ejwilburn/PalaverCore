using System;
using System.ComponentModel.DataAnnotations;

namespace Palaver.Models.ThreadViewModels
{
    public class ListViewModel
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public DateTime Updated { get; set; }
    }
}
