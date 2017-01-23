using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Palaver.Models
{
    public class Thread
    {
        [Key]
        public int ThreadId { get; set; }
        [Required]
        public string Title { get; set; }
        //public int UserId { get; set; }
        public User User { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime LastUpdatedTime { get; set; }

        public ICollection<Subscription> Subscriptions { get; set; }

		[NotMapped]
		public int unreadCount { get; set; }

        public Thread()
        {
            unreadCount = 0;
            CreatedTime = DateTime.UtcNow;
            LastUpdatedTime = DateTime.UtcNow;
        }

        public Thread(string newTitle, User creator)
        {
            unreadCount = 0;
            CreatedTime = DateTime.UtcNow;
            LastUpdatedTime = DateTime.UtcNow;
            User = creator;
            Title = newTitle;
        }
    }
}