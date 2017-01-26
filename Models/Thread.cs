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

        public List<Comment> Comments { get; set; }
        public List<Subscription> Subscriptions { get; set; }
        public List<FavoriteThread> FavoriteThreads { get; set; }

		[NotMapped]
		public int UnreadCount { get; set; }
        [NotMapped]
        public List<Comment> ImmediateChildren {
            get {
                if (_immediateChildren != null)
                    return _immediateChildren;
                else
                {
                    _immediateChildren = new List<Comment>();
                    if (Comments != null && Comments.Count > 0)
                    {
                        _immediateChildren = Comments.FindAll(c => !c.ParentCommentId.HasValue);
                    }
                    return _immediateChildren;
                }
            }
        }

        private List<Comment> _immediateChildren;

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
