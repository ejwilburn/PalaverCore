using System;
using EntityFrameworkCore.Triggers;

namespace Palaver.Models
{
    public abstract class TimeStamper
    {
        public virtual DateTime Created { get; set; }
        public virtual DateTime Updated { get; set; }

        static TimeStamper()
        {
            Triggers<TimeStamper>.Inserting += entry => entry.Entity.Created = entry.Entity.Updated = DateTime.UtcNow;
            Triggers<TimeStamper>.Updating += entry => entry.Entity.Updated = DateTime.UtcNow;
        }
    }
}
