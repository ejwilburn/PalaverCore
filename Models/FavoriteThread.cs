using System.ComponentModel.DataAnnotations;

namespace Palaver.Models
{
    public class FavoriteThread
    {
        [Required]
        public int ThreadId { get; set; }
        [Required]
        public Thread Thread { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public User User { get; set; }
    }
}
