using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using EntityFrameworkCore.Triggers;

namespace Palaver.Models
{
    public class User : IdentityUser<int>
    {
        public DateTime Created { get; set; }
        public ICollection<Thread> Threads { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<FavoriteThread> FavoriteThreads { get; set; }
        public ICollection<FavoriteComment> FavoriteComments { get; set; }
        public ICollection<Subscription> Subscriptions { get; set; }
        public ICollection<UnreadComment> UnreadComments { get; set; }

        static User()
        {
            Triggers<User>.Inserting += entry => entry.Entity.Created = DateTime.UtcNow;
        }
    }
}
