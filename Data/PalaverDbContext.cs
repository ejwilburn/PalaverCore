using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using EntityFrameworkCore.Triggers;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Palaver.Models;

namespace Palaver.Data
{
    public class PalaverDbContext : IdentityDbContext<User, Role, int>
    {
        public DbSet<Palaver.Models.Thread> Threads { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<UnreadComment> UnreadComments { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<FavoriteThread> FavoriteThreads { get; set; }
        public DbSet<FavoriteComment> FavoriteComments { get; set; }

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
            base.OnModelCreating(builder);

            // Use singular table names.
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                // Only handle user-defined types, skipping shadow types.
                if (entityType.ClrType != null)
                    entityType.Relational().TableName = entityType.ClrType.Name;
            }

            // Shorten up identity table names.
            builder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityRoleClaim<int>", b => {
                    b.ToTable("RoleClaim");
            });
            builder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserClaim<int>", b => {
                    b.ToTable("UserClaim");
            });
            builder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserLogin<int>", b => {
                    b.ToTable("UserLogin");
            });
            builder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserRole<int>", b => {
                    b.ToTable("UserRole");
            });
            builder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserToken<int>", b => {
                    b.ToTable("UserToken");
            });

            builder.Entity<User>(u => {
                u.Property(props => props.Email).IsRequired(true);
            });

            /*
            builder.Entity<User>(u => {
                u.Property(props => props.Email).IsRequired(true);
                u.Property(props => props.CreatedTime).ForNpgsqlHasDefaultValueSql("timezone('UTC', now())");
            });

            builder.Entity<Thread>( t => {
                t.Property(props => props.CreatedTime).ForNpgsqlHasDefaultValueSql("timezone('UTC', now())");
                t.Property(props => props.LastUpdatedTime).ForNpgsqlHasDefaultValueSql("timezone('UTC', now())");
            });

            builder.Entity<Comment>( c => {
                c.Property(props => props.CreatedTime).ForNpgsqlHasDefaultValueSql("timezone('UTC', now())");
                c.Property(props => props.LastUpdatedTime).ForNpgsqlHasDefaultValueSql("timezone('UTC', now())");
            });
            */

            // Setup one to many relationship for Comment->Comment
            builder.Entity<Comment>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Comments)
                .HasForeignKey(c => c.ParentCommentId);

            // Setup many to many relationships for UnreadComment (User<->Comment)
            builder.Entity<UnreadComment>()
                .HasKey(uc => new { uc.UserId, uc.CommentId });
            builder.Entity<UnreadComment>()
                .HasOne(uc => uc.User)
                .WithMany(u => u.UnreadComments)
                .HasForeignKey(uc => uc.UserId);
            builder.Entity<UnreadComment>()
                .HasOne(uc => uc.Comment)
                .WithMany(c => c.UnreadComments)
                .HasForeignKey(uc => uc.CommentId);

            // Setup many to many relationships for Subscription (User<->Thread)
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

            // Setup many to many relationships for FavoriteThread (User<->Thread)
            builder.Entity<FavoriteThread>()
                .HasKey(ft => new { ft.UserId, ft.ThreadId });
            builder.Entity<FavoriteThread>()
                .HasOne(ft => ft.User)
                .WithMany(u => u.FavoriteThreads)
                .HasForeignKey(ft => ft.UserId);
            builder.Entity<FavoriteThread>()
                .HasOne(ft => ft.Thread)
                .WithMany(t => t.FavoriteThreads)
                .HasForeignKey(ft => ft.ThreadId);

            // Setup many to many relationships for FavoriteComment (User<->Comment)
            builder.Entity<FavoriteComment>()
                .HasKey(fc => new { fc.UserId, fc.CommentId });
            builder.Entity<FavoriteComment>()
                .HasOne(fc => fc.User)
                .WithMany(u => u.FavoriteComments)
                .HasForeignKey(fc => fc.UserId);
            builder.Entity<FavoriteComment>()
                .HasOne(fc => fc.Comment)
                .WithMany(t => t.FavoriteComments)
                .HasForeignKey(fc => fc.CommentId);
        }

        // Adding support for triggers.
        public override Int32 SaveChanges() {
            return this.SaveChangesWithTriggers(base.SaveChanges, acceptAllChangesOnSuccess: true);
        }

        public override Int32 SaveChanges(Boolean acceptAllChangesOnSuccess) {
            return this.SaveChangesWithTriggers(base.SaveChanges, acceptAllChangesOnSuccess);
        }

        public override Task<Int32> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken)) {
            return this.SaveChangesWithTriggersAsync(base.SaveChangesAsync, acceptAllChangesOnSuccess: true, cancellationToken: cancellationToken);
        }

        public override Task<Int32> SaveChangesAsync(Boolean acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken)) {
            return this.SaveChangesWithTriggersAsync(base.SaveChangesAsync, acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
