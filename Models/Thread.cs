using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Palaver.Models
{
    public class Thread : TimeStamper
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        public bool IsSticky { get; set; }
        [Required]
        public User User { get; set; }

        public ICollection<Subscription> Subscriptions { get; set; }
        public ICollection<FavoriteThread> FavoriteThreads { get; set; }

		[NotMapped]
		public int UnreadCount { get; set; }

        public Thread(string newTitle, User creator)
        {
            this.Title = newTitle;
            this.User = creator;

            this.IsSticky = false;
            this.UnreadCount = 0;
        }

        public Thread(string newTitle, User creator, bool isSticky)
        {
            this.Title = newTitle;
            this.User = creator;
            this.IsSticky = isSticky;

            this.UnreadCount = 0;
        }
    }
}
