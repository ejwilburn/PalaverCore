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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EntityFrameworkCore.Triggers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PalaverCore.Data;
using PalaverCore.Services;

namespace PalaverCore.Models;

public class Comment : TimeStamper
{
    public enum TextFormat
    {
        HTML = 0,
        Markdown = 1
    }

    [Key]
    public int Id { get; set; }
    [Required]
    public string Text { get; set; }
    [Required]
    public TextFormat Format { get; set; }
    [Required]
    public int UserId { get; set; }
    [Required]
    public User User { get; set; }
    [Required]
    public int ThreadId { get; set; }
    [Required]
    public Thread Thread { get; set; }
    public int? ParentCommentId { get; set; } = null;
    public Comment Parent { get; set; } = null;
    [NotMapped]
    public bool IsUnread { get; set; } = false;
    [NotMapped]
    public bool IsAuthor { get; set; } = false;

    public List<Comment> Comments { get; set; }
    public List<UnreadComment> UnreadComments { get; set; }
    public List<FavoriteComment> FavoriteComments { get; set; }

    public Comment()
    {
        Triggers<Comment>.Inserting += entry => entry.Entity.Thread.Updated = DateTime.UtcNow;
        Triggers<Comment>.Updating += entry => entry.Entity.Thread.Updated = DateTime.UtcNow;
    }

    public static async Task<Comment> CreateComment(string text, TextFormat format, Thread thread, int? parentId, User user, PalaverDbContext db)
    {
        Comment newComment = new Comment {
            Text = text,
            Format = format,
            ThreadId = thread.Id,
            Thread = thread,
            UserId = user.Id,
            User = user,
            ParentCommentId = parentId,
            IsAuthor = true
        };

        if (thread.Comments == null)
            thread.Comments = new List<Comment>();
        thread.Comments.Add(newComment);

        if (parentId != null)
        {
            newComment.Parent = await db.Comments.Where(c => c.Id == parentId)
                .Include(c => c.Comments)
                .Include(c => c.Thread)
                .Include(c => c.User)
                .Include(c => c.UnreadComments)
                .SingleOrDefaultAsync();
            if (newComment.Parent != null)
            {
                if (newComment.Parent.Comments == null)
                    newComment.Parent.Comments = new List<Comment>();
                newComment.Parent.Comments.Add(newComment);
            }
        }

        return newComment;
    }
}