using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Palaver.Models
{
    public class Comment : TimeStamper
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Text { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public User User { get; set; }
        [Required]
        public int ThreadId { get; set; }
        [Required]
        public Thread Thread { get; set; }
        public int? ParentCommentId { get; set; }
        public Comment Parent { get; set; }
        [NotMapped]
        public bool IsUnread { get; set; }

        public List<Comment> Comments { get; set; }
        public List<UnreadComment> UnreadComments { get; set; }
        public List<FavoriteComment> FavoriteComments { get; set; }

        public Comment()
        {
            this.Parent = null;
            this.IsUnread = false;
        }

        public Comment(string text, User creator, Thread thread)
        {
            this.Text = text;
            this.User = creator;
            this.Thread = thread;
            this.Parent = null;
            this.IsUnread = false;
        }

        public Comment(string text, User creator, Thread thread, Comment parent)
        {
            this.Text = text;
            this.User = creator;
            this.Thread = thread;
            this.Parent = parent;
            this.IsUnread = false;
        }
    }
}
