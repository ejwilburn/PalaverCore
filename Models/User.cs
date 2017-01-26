using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using EntityFrameworkCore.Triggers;

namespace Palaver.Models
{
    public class User : IdentityUser<int>
    {
        public DateTime Created { get; set; }
        public List<Thread> Threads { get; set; }
        public List<Comment> Comments { get; set; }
        public List<FavoriteThread> FavoriteThreads { get; set; }
        public List<FavoriteComment> FavoriteComments { get; set; }
        public List<Subscription> Subscriptions { get; set; }
        public List<UnreadComment> UnreadComments { get; set; }

        static User()
        {
            Triggers<User>.Inserting += entry => entry.Entity.Created = DateTime.UtcNow;
        }
    }
}
