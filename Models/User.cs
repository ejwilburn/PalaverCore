using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Palaver.Models
{
    public class User : IdentityUser<int>
    {
        public ICollection<Thread> Threads { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<Subscription> Subscriptions { get; set; }
        public ICollection<UnreadComment> UnreadComments { get; set; }
    }
}
