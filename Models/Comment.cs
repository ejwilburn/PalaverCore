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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.Triggers;
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
        [NotMapped]
        public bool IsFavorite { get; set; }

        public List<Comment> Comments { get; set; }
        public List<UnreadComment> UnreadComments { get; set; }
        public List<FavoriteComment> FavoriteComments { get; set; }

        public Comment()
        {
            this.Parent = null;
            this.IsUnread = false;
            Triggers<Comment>.Inserting += entry => entry.Entity.Thread.Updated = DateTime.UtcNow;
            Triggers<Comment>.Updating += entry => entry.Entity.Thread.Updated = DateTime.UtcNow;
        }

        public static Comment CreateComment(string text, Thread thread, int? parentId, User user, PalaverDbContext db)
        {
            Comment newComment = new Comment {
                Text = text,
                Thread = thread,
                User = user,
                ParentCommentId = parentId,
                IsUnread = true
            };

            return newComment;
        }
    }
}
