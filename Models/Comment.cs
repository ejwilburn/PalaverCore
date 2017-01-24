using System;
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
        public User User { get; set; }
        [Required]
        public Thread Thread { get; set; }
        public int? ParentCommentId { get; set; }
        public Comment Parent { get; set; }

        public ICollection<Comment> Comments { get; set; }
        public ICollection<UnreadComment> UnreadComments { get; set; }
        public ICollection<FavoriteComment> FavoriteComments { get; set; }

        public Comment(string text, User creator, Thread thread)
        {
            this.Text = text;
            this.User = creator;
            this.Thread = thread;
            this.Parent = null;
        }

        public Comment(string text, User creator, Thread thread, Comment parent)
        {
            this.Text = text;
            this.User = creator;
            this.Thread = thread;
            this.Parent = parent;
        }

		[NotMapped]
		public bool IsUnread {
            get {
                if (UnreadComments == null || UnreadComments.Count == 0)
                    return false;
                else
                    return true;
            }
        }
    }
}
