using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Palaver.Data;

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
        public int UserId { get; set; }
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

        public Thread()
        {
            this.IsSticky = false;
            this.UnreadCount = 0;
        }

        public static async Task<Thread> CreateAsync(string newTitle, int userId, PalaverDbContext db)
        {
            List<User> allUsers = await db.Users.ToListAsync();

            Thread newThread = new Thread {
                Title = newTitle,
                User = db.Users.Find(userId),
                IsSticky = false,
                UnreadCount = 0
            };

            db.Threads.Add(newThread);

            foreach (User user in db.Users.ToList())
            {
                db.Subscriptions.Add(new Subscription { Thread = newThread, User = user} );
            }

            return newThread;
        }
    }
}
