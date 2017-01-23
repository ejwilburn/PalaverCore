using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Palaver.Models;

namespace Palaver.Data
{
    public class PalaverDbContext : IdentityDbContext<User, Role, int>
    {
        public DbSet<Thread> Threads { get; set; }
        public DbSet<Comment> Comments { get; set; }

        public PalaverDbContext(DbContextOptions<PalaverDbContext> options)
            : base(options)
        {
        }

        public PalaverDbContext()
        {
        }

        /*
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(connectionString);
        }
        */

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.HasPostgresExtension("uuid-ossp");

            // Use singular table names.
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                // Only handle user-defined types, skipping shadow types.
                if (entityType.ClrType != null)
                    entityType.Relational().TableName = entityType.ClrType.Name;
            }

            builder.Entity<UnreadComment>()
                .HasKey(t => new { t.UserId, t.CommentId });

            builder.Entity<UnreadComment>()
                .HasOne(uc => uc.User)
                .WithMany(u => u.UnreadComments)
                .HasForeignKey(uc => uc.UserId);

            builder.Entity<UnreadComment>()
                .HasOne(uc => uc.Comment)
                .WithMany(c => c.UnreadComments)
                .HasForeignKey(uc => uc.CommentId);

            builder.Entity<Subscription>()
                .HasKey(t => new { t.UserId, t.ThreadId });

            builder.Entity<Subscription>()
                .HasOne(s => s.User)
                .WithMany(u => u.Subscriptions)
                .HasForeignKey(s => s.UserId);

            builder.Entity<Subscription>()
                .HasOne(s => s.Thread)
                .WithMany(t => t.Subscriptions)
                .HasForeignKey(s => s.ThreadId);

            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
    }
}
