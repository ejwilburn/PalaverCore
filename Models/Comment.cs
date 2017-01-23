using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Palaver.Models
{
    public class Comment
    {
        [Key]
        public int CommentId { get; set; }
        // public int UserId { get; set; }
        public User User { get; set; }
        // public int ThreadId { get; set; }
        public Thread Thread { get; set; }
        // public int ParentId { get; set; }
        // public Comment Parent { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime LastUpdatedTime { get; set; }
        public string Text { get; set; }

        public ICollection<Comment> Comments { get; set; }
        public ICollection<UnreadComment> UnreadComments { get; set; }

		[NotMapped]
		public bool IsUnread { get; set; }

        public Comment()
        {
			IsUnread = false;
            CreatedTime = DateTime.UtcNow;
            LastUpdatedTime = DateTime.UtcNow;
        }
    }
}