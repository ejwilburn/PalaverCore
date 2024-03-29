/*
Copyright 2021, E.J. Wilburn, Marcus McKinnon, Kevin Williams
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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using EntityFrameworkCore.Triggers;
using PalaverCore.Models;
using Npgsql;
using static PalaverCore.Models.Comment;

namespace PalaverCore.Data;

public class PalaverDbContext : IdentityDbContext<User, Role, int>
{
    public DbSet<Models.Thread> Threads { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<UnreadComment> UnreadComments { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<FavoriteThread> FavoriteThreads { get; set; }
    public DbSet<FavoriteComment> FavoriteComments { get; set; }

    public PalaverDbContext(DbContextOptions<PalaverDbContext> options)
        : base(options)
    {
    }

    public async Task<List<Models.Thread>> GetPagedThreadsListAsync(int userId, int startIndex = 0, int maxResults = 10)
    {
        List<Models.Thread> threads = await Threads.OrderByDescending(t => t.Updated)
            .Include(t => t.User)
            .Skip(startIndex)
            .Take(maxResults)
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync();

        // Get unread counts for each thread for the current user.
        var threadIds = threads.Select(t => t.Id);
        var countTotals = await Comments.Where(c => threadIds.Contains(c.ThreadId))
            .Join(UnreadComments.Where(uc => uc.UserId == userId), c => c.Id, uc => uc.CommentId, (c, uc) => new { Comment = c, UnreadComment = uc })
            .GroupBy(cuc => cuc.Comment.ThreadId)
            .Select(g => new { ThreadId = g.Key, Count = g.Count() })
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync();

        // Unread counts
        foreach (var count in countTotals)
        {
            threads.Find(t => t.Id == (int)count.ThreadId).UnreadCount = (int)count.Count;
        }

        return threads;
    }

    public async Task<Models.Thread> GetThreadAsync(int threadId, int userId)
    {
        Models.Thread thread;

        // Includes can't be ordered, so to ge the comments back in order of creation date the comments are loaded directly
        // and include the Thread, rather than the other way around.
        List<Comment> comments = await Comments.Where(c => c.ThreadId == threadId)
            .Include(c => c.Thread)
                .ThenInclude(t => t.User)
            .Include(c => c.User)
            //.Include(c => c.Parent)
            .Include(c => c.Comments)
            .OrderBy(c => c.Created)
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync();

        // If there are comments, get the thread from there, otherwise load the thread and return it.
        if (comments.Count > 0)
        {
            thread = comments[0].Thread;

            // Sort comment.comments by creation date and set IsAuthor
            foreach (Comment comment in comments)
            {
                comment.Comments = comment.Comments.OrderBy(c => c.Created).ToList();
                comment.IsAuthor = comment.UserId == userId;
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
                .AsNoTrackingWithIdentityResolution()
                .SingleOrDefaultAsync();
        }

        return thread;
    }

    public async Task<Models.Comment> GetCommentAsync(int id, int userId)
    {
        Comment comment = await Comments.Where(c => c.Id == id)
            .Include(c => c.Thread)
                .ThenInclude(t => t.Comments)
            .Include(c => c.User)
            //.Include(c => c.Parent)
            .Include(c => c.Comments)
            .OrderBy(c => c.Created).SingleAsync();

        if (comment != null)
        {
            // Sort comment.comments by creation date and set isAuthor flag.
            foreach (Comment curComment in comment.Thread.Comments)
            {
                curComment.Comments = curComment.Comments.OrderBy(c => c.Created).ToList();
                curComment.IsAuthor = curComment.UserId == userId;
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

    public async Task<Models.Thread> CreateThreadAsync(string title, int userId)
    {
        List<User> allUsers = await Users.ToListAsync();
        User currUser = Users.Find(userId);
        Models.Thread newThread = Models.Thread.CreateThread(title, currUser, this);

        // Subscribe everyone to all threads by default.
        foreach (User user in allUsers)
        {
            Subscriptions.Add(new Subscription { Thread = newThread, User = user} );
        }

        Threads.Add(newThread);
        await SaveChangesAsync();
        return newThread;
    }

    public async Task<Comment> CreateCommentAsync(string text, TextFormat format, int threadId, int? parentId, User user)
    {
        // If the comment has a parent, make sure the comment's thread id is the same as the parent's.
        int useThreadId = threadId;
        if (parentId != null)
        {
            Comment parent = await Comments.Where(c => c.Id == parentId).SingleOrDefaultAsync();
            if (parent != null)
                useThreadId = parent.ThreadId;
        }
        Models.Thread thread = await Threads.Where(t => t.Id == useThreadId).Include(t => t.Subscriptions).SingleAsync();
        Comment newComment = await Comment.CreateComment(text, format, thread, parentId, user, this);
        Comments.Add(newComment);

        // Add unread comments for subscribed users other than the current user.
        foreach (Subscription sub in thread.Subscriptions)
        {
            if (sub.UserId != user.Id)
            {
                UnreadComments.Add(new UnreadComment {
                    UserId = sub.UserId,
                    Comment = newComment
                });
            }
        }

        await SaveChangesAsync();
        return newComment;
    }

    public async Task MarkCommentReadByUser(int threadId, int commentId, int userId)
    {
        Comment comment = Comments.Local.FirstOrDefault(c => c.Id == commentId);
        if (comment != null && comment.IsUnread == true)
            comment.IsUnread = false;

        UnreadComment uc = await UnreadComments.FindAsync(userId, commentId);
        if (uc != null)
        {
            UnreadComments.Remove(uc);
            await SaveChangesAsync();

            Models.Thread thread = Threads.Local.FirstOrDefault(t => t.Id == threadId);
            if (thread != null && thread.UnreadCount > 0)
                thread.UnreadCount--;
        }

    }

    public async Task<List<Comment>> Search(string searchText) {
        return await Comments.FromSqlRaw("SELECT * FROM search_comments({0}) LIMIT 5", new NpgsqlParameter("@searchText", searchText))
            .Include(c => c.User)
            .Include(c => c.Thread)
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync();
    }

    public async Task<User> NewUserAsync(string name, string email, bool emailConfirmed = false)
    {
        User newUser = new User { UserName = name, Email = email, EmailConfirmed = emailConfirmed };
        newUser.Subscriptions = new List<Subscription>();
        List<Models.Thread> threads = await Threads.ToListAsync();

        foreach (Models.Thread thread in threads)
        {
            Subscription sub = new Subscription { User = newUser, Thread = thread };
            newUser.Subscriptions.Add(sub);
            Subscriptions.Add(sub);
        }

        return newUser;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Use singular and lower case table names and lower case column names.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            // Only handle user-defined types, skipping shadow types.
            if (entityType.ClrType == null)
                continue;

            entityType.SetTableName(entityType.ClrType.Name.ToLower());
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(property.Name.ToLower());
            }
        }

        // Shorten up identity table names.
        builder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b => {
                b.ToTable("roleclaim");
        });
        builder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b => {
                b.ToTable("userclaim");
        });
        builder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b => {
                b.ToTable("userlogin");
        });
        builder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<int>", b => {
                b.ToTable("userrole");
        });
        builder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b => {
                b.ToTable("usertoken");
        });

        builder.Entity<User>(u => {
            u.Property(props => props.Email).IsRequired(true);
            u.Property(props => props.NotificationEnabled).HasDefaultValueSql("true");
            u.HasIndex(props => props.UserName).IsUnique(false);
            u.HasIndex(props => props.Email).IsUnique(false);
        });

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
