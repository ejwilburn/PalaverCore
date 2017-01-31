/*
Copyright 2017, Marcus McKinnon, E.J. Wilburn, Kevin Williams
This program is distributed under the terms of the GNU General Public License.

This file is part of Palaver.

Palaver is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 2 of the License, or
(at your option) any later version.

Palaver is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Palaver.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using EntityFrameworkCore.Triggers;
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

		public async Task<List<Palaver.Models.Thread>> GetThreadsListAsync(int userId)
		{
            List<Palaver.Models.Thread> threads = await Threads.OrderByDescending(t => t.Updated).ToListAsync();

			// Get unread counts for each thread for the current user.
            var countTotals = await UnreadComments.Where(uc => uc.UserId == userId)
                .Join(Comments, uc => uc.CommentId, c => c.Id, (uc, c) => new { UnreadComment = uc, Comment = c })
                .GroupBy(ucAndc => ucAndc.Comment.ThreadId)
                .Select(g => new { ThreadId = g.Key, Count = g.Count() }).ToListAsync();

            // Unread counts
            foreach (var count in countTotals)
            {
                threads.Find(t => t.Id == (int)count.ThreadId).UnreadCount = (int)count.Count;
            }

            // Subscribed or not
            foreach (Subscription sub in await Subscriptions.Where(s => s.UserId == userId).ToListAsync())
            {
                sub.Thread.IsSubscribed = true;
            }

            // Favorites
            foreach (FavoriteThread fav in await FavoriteThreads.Where(ft => ft.UserId == userId).ToListAsync())
            {
                fav.Thread.IsFavorite = true;
            }

			return threads;
		}

		public async Task<Palaver.Models.Thread> GetThreadAsync(int threadId, int userId)
		{
            Palaver.Models.Thread thread;
			//List<Comment> threadComments = Comments.Include("User").Include("Comments").Where(x => x.SubjectId == subjectId).OrderBy(x=> x.CreatedTime).ToList();
            // Palaver.Models.Thread thread = await Threads.Where(t => t.Id == threadId)
            //     .Include(t => t.User)
            //     .Include(t => t.Comments)
            //         .ThenInclude(c => c.User)
            //     .Include(t => t.Comments)
            //         .ThenInclude(c => c.Comments)
            //     .Include(t => t.Comments)
            //         .ThenInclude(c => c.UnreadComments.Where(uc => uc.UserId == userId))
            //     .Include(t => t.Comments)
            //         .ThenInclude(c => c.FavoriteComments.Where(fc => fc.UserId == userId))
            //     .SingleAsync();

            // Includes can't be ordered, so to ge the comments back in order of creation date the comments are loaded directly
            // and include the Thread, rather than the other way around.
            List<Comment> comments = await Comments.Where(c => c.ThreadId == threadId)
                .Include(c => c.Thread)
                    .ThenInclude(t => t.User)
                .Include(c => c.Thread)
                    .ThenInclude(t => t.FavoriteThreads)
                .Include(c => c.Thread)
                    .ThenInclude(t => t.Subscriptions)
                .Include(c => c.User)
                .Include(c => c.Parent)
                .Include(c => c.Comments)
                .Include(c => c.FavoriteComments)
                .OrderBy(c => c.Created).ToListAsync();

            // If there are comments, get the thread from there, otherwise load the thread and return it.
            if (comments.Count > 0)
            {
                thread = comments[0].Thread;

                // Sort comment.comments by creation date and mark favorites.
                foreach (Comment comment in comments)
                {
                    comment.IsFavorite = comment.FavoriteComments.Exists(fc => fc.UserId == userId);
                    comment.Comments = comment.Comments.OrderBy(c => c.Created).ToList();
                }

                // Get unread counts for threads.
                List<int> unreadIds = await UnreadComments.Where(uc => uc.UserId == userId && uc.Comment.ThreadId == threadId)
                    .Select(uc => uc.CommentId)
                    .ToListAsync();
                foreach (Comment comment in comments.FindAll(c => unreadIds.Contains(c.Id)))
                {
                    comment.IsUnread = true;
                }
            }
            else
            {
                thread = await Threads.Where(t => t.Id == threadId)
                    .Include(t => t.User)
                    .Include(t => t.FavoriteThreads)
                    .Include(t => t.Subscriptions)
                    .SingleOrDefaultAsync();
            }

            thread.IsFavorite = thread.FavoriteThreads.Exists(ft => ft.UserId == userId);
            thread.IsSubscribed = thread.Subscriptions.Exists(s => s.UserId == userId);

			return thread;
		}

		public async Task<Palaver.Models.Comment> GetCommentAsync(int id, int userId)
		{
            Comment comment = await Comments.Where(c => c.Id == id)
                .Include(c => c.Thread)
                    .ThenInclude(t => t.Comments)
                        .ThenInclude(c => c.FavoriteComments)
                .Include(c => c.Thread)
                    .ThenInclude(t => t.Subscriptions)
                .Include(c => c.User)
                .Include(c => c.Parent)
                .Include(c => c.Comments)
                .Include(c => c.FavoriteComments)
                .OrderBy(c => c.Created).SingleAsync();

            if (comment != null)
            {
                // Sort comment.comments by creation date and mark favorites.
                foreach (Comment curComment in comment.Thread.Comments)
                {
                    curComment.IsFavorite = curComment.FavoriteComments.Exists(fc => fc.UserId == userId);
                    curComment.Comments = curComment.Comments.OrderBy(c => c.Created).ToList();
                }

                // Get unread flag for comments.
                List<int> unreadIds = await UnreadComments.Where(uc => uc.UserId == userId && uc.Comment.ThreadId == comment.ThreadId)
                    .Select(uc => uc.CommentId)
                    .ToListAsync();
                foreach (Comment curComment in comment.Thread.Comments.FindAll(c => unreadIds.Contains(c.Id)))
                {
                    curComment.IsUnread = true;
                }
            }

			return comment;
		}

        public async Task<Palaver.Models.Thread> CreateThreadAsync(string title, int userId)
        {
            List<User> allUsers = await Users.ToListAsync();
            Palaver.Models.Thread newThread = Palaver.Models.Thread.CreateThread(title, userId, this);

            // Subscribe everyone to all threads by default.
            foreach (User user in allUsers)
            {
                Subscriptions.Add(new Subscription { Thread = newThread, User = user} );
            }

            Threads.Add(newThread);
            await SaveChangesAsync();
            return newThread;
        }

        public async Task<Comment> CreateCommentAsync(string text, int threadId, int? parentId, int userId)
        {
            Palaver.Models.Thread thread = await Threads.Where(t => t.Id == threadId).Include(t => t.Subscriptions).SingleAsync();
            Comment newComment = Comment.CreateComment(text, thread, parentId, userId, this);
            Comments.Add(newComment);
            await SaveChangesAsync();
            return newComment;
        }

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
