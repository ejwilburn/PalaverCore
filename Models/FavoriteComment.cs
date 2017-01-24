using System.ComponentModel.DataAnnotations;

namespace Palaver.Models
{
    public class FavoriteComment
    {
        [Required]
        public int CommentId { get; set; }
        [Required]
        public Comment Comment { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public User User { get; set; }
    }
}
