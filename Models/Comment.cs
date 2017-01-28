using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Palaver.Data;

namespace Palaver.Models
{
    public class Comment : TimeStamper
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Text { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public User User { get; set; }
        [Required]
        public int ThreadId { get; set; }
        [Required]
        public Thread Thread { get; set; }
        public int? ParentCommentId { get; set; }
        public Comment Parent { get; set; }
        [NotMapped]
        public bool IsUnread { get; set; }

        public List<Comment> Comments { get; set; }
        public List<UnreadComment> UnreadComments { get; set; }
        public List<FavoriteComment> FavoriteComments { get; set; }

        public Comment()
        {
            this.Parent = null;
            this.IsUnread = false;
        }

        public static async Task<Comment> CreateAsync(string text, int threadId, int? parentId, int userId, PalaverDbContext db)
        {
            Comment newComment = new Comment {
                Text = text,
                ThreadId = threadId,
                UserId = userId,
                ParentCommentId = parentId,
                IsUnread = false
            };

            List<Subscription> subs = await db.Subscriptions.Where(s => s.ThreadId == threadId && s.UserId != userId).ToListAsync();

            foreach (Subscription sub in subs)
            {
                db.UnreadComments.Add( new UnreadComment { Comment = newComment, User = sub.User });
            }

            /*
            //User currentUser = _dbContext.Users.Find( (new User { UserId = CodeFirstSecurity.GetUserId(Context.User.Identity.Name) }).UserId );
            User currentUser = _dbContext.Users.Find( CodeFirstSecurity.GetUserId(Context.User.Identity.Name) );
            Comment comment = new Comment(replyText, currentUser);
            Comment parentComment = _dbContext.Comments.Include("Comments").First(pc => pc.CommentId == parentId);

            if (parentComment.SubjectId != null)
                comment.SubjectId = parentComment.SubjectId;
            else
                comment.SubjectId = parentComment.CommentId;

            comment.ParentCommentId = parentComment.CommentId;

            Comment thread = _dbContext.Comments.Find(comment.SubjectId);
            thread.LastUpdatedTime = DateTime.UtcNow;

            parentComment.Comments.Add(comment);

            foreach (User uu in _dbContext.Users)
            {
                if (uu.UserId != currentUser.UserId)
                    _dbContext.UnreadItems.Add(new UnreadItem { User = uu, Comment = comment });
            }
            */

            return newComment;
        }
    }
}
