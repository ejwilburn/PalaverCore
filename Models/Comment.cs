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

namespace PalaverCore.Models
{
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
        public string Text { get { return _text; } set { _text = FilterComment(value); } }
        [Required]
        public TextFormat Format { get; set; }
        [NotMapped]
        public string DisplayText { get { return DisplayFilterComment(_text); } }
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

        private string _text;

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

        /// <summary>
        /// Apply temporary filtering for comment text display, such as modifying images so they're lazy loaded.
        /// </summary>
        /// <param name="commentText"></param>
        /// <returns></returns>
        private string DisplayFilterComment(string commentText)
        {
            if (String.IsNullOrWhiteSpace(commentText))
                return commentText;

            string output = EnableGifPlayOnHover(commentText);
            output = EnableLazyLoadingImages(output);
            output = EnableTwitterEmbedding(output);
            return output;
        }

        // Regexes for modifying images to lazy load.
        private static readonly Regex GIF_IMAGE_REGEX = new Regex(@"(<img [^>]*?)(?:\s+src=)([""'][^""'>]+[""'](?<=\.gif[""']))([^>]*?>)", RegexOptions.IgnoreCase);
        private static readonly string GIF_IMAGE_REPLACE = "$1 data-gifffer=$2 class=\"animated\"$3";

        /// <summary>
        /// Modifies any img tag with a gif src to add an animated class.
        /// </summary>
        /// <param name="commentText"></param>
        /// <returns></returns>
        private string EnableGifPlayOnHover(string commentText)
        {
            return GIF_IMAGE_REGEX.Replace(commentText, GIF_IMAGE_REPLACE);
        }

        // Regexes for modifying images to lazy load.
        private static readonly Regex IMAGE_TAG_REGEX = new Regex(@"(<img [^>]*?)\s+(src=[""'][^""'>]+[""'](?<!\.gif['""]))([^>]*?>)", RegexOptions.IgnoreCase);
        private static readonly string IMAGE_TAG_LAZY_LOAD_REPLACE = "$1 data-$2 class=\"b-lazy loading\"$3";

        private string EnableLazyLoadingImages(string commentText)
        {
            return IMAGE_TAG_REGEX.Replace(commentText, IMAGE_TAG_LAZY_LOAD_REPLACE);
        }

        // Regexes for modifying images to lazy load.
        // private static readonly Regex TWITTER_URL_REGEX = new Regex(@"(<a [^>]*?href=[""'](?:https?://|//)(?:www\.)?twitter\.com[^""'>]+[""'][^>]*?>)", RegexOptions.IgnoreCase);
        // private static readonly string TWITTER_URL_REPLACEMENT = "<blockquote class=\"twitter-tweet\">$1</blockquote>";
        // private static readonly Regex TWITTER_URL_REGEX = new Regex(@"<a [^>]*?href=[""'](?:https?://|//)(?:www\.)?twitter\.com/[^""'>/]+/status/(\d+)[^""'>]*[""'][^>]*?>.*</a>", RegexOptions.IgnoreCase);
        private static readonly Regex TWITTER_URL_REGEX = new Regex(@"(<a [^>]*?href=[""'](?:https?://|//)(?:www\.)?twitter\.com[^""'>]+[""'][^>]*?>.*?</a>)", RegexOptions.IgnoreCase);
        private static readonly string TWITTER_URL_REPLACEMENT = "<blockquote class=\"twitter-tweet\" data-theme=\"dark\">$1</blockquote>";

        private string EnableTwitterEmbedding(string commentText)
        {
            return TWITTER_URL_REGEX.Replace(commentText, TWITTER_URL_REPLACEMENT);
        }

        /// <summary>
        /// Apply any needed filters to comment text when modified, such as auto-linking of URLs.
        /// </summary>
        /// <param name="commentText">Comment's primary text.</param>
        /// <returns>Filtered comment string.</returns>
        private string FilterComment(string commentText)
        {
            if (String.IsNullOrWhiteSpace(commentText))
                return commentText;

            return Linkify(commentText);
        }

        // Find URLs within text outside of HTML tag properties.
        private static readonly Regex URL_REGEX_WITH_PROTOCOL = new Regex(@"(?<!(?:href=[""']?|src=['""]?|<a[^>]*>)[^.'""]*[\s]*)" +
            @"\b((?:https?://)(?:&amp;|[-A-Z0-9+&@#/%=~_|$?!:,.()])*[A-Z0-9+&@#/%=~_|$()])", RegexOptions.IgnoreCase);
        private static readonly Regex URL_REGEX_WITHOUT_PROTOCOL = new Regex(@"(?<!(?:href=[""']?|src=['""]?|<a[^>]*>)[^.'""]*[\s]*)" +
            @"\b((?:www\.)(?:&amp;|[-A-Z0-9+&@#/%=~_|$?!:,.()])*[A-Z0-9+&@#/%=~_|$()])", RegexOptions.IgnoreCase);
        private static readonly string URL_REPLACE_BASIC = "<a href=\"$1\" class=\"autolinked\" target=\"_blank\">$1</a>";
        private static readonly string URL_REPLACE_ADD_PROTOCOL = "<a href=\"http://$1\" class=\"autolinked\" target=\"_blank\">$1</a>";
        private static readonly Regex URL_ESCAPED_AMPERSAND = new Regex(@"(?<=href=""https?://[^/]+[^""]?)&amp;(?="" class=""autolinked"")", RegexOptions.IgnoreCase);
        private static readonly Regex TRAILING_WHITESPACE = new Regex(@"(?:&nbsp;|[ \t])+?(?=$|<br|</?p>|</?div>)", RegexOptions.IgnoreCase);

        /// <summary>
        /// Convert URLs in the text to links if they're not already a link.
        /// </summary>
        /// <param name="input">Text</param>
        /// <returns>The input string with links outside HTML tags formatted as &gt;A&lt; tags.</returns>
        private String Linkify(string input)
        {
            String output = TRAILING_WHITESPACE.Replace(input, "");
            output = URL_REGEX_WITH_PROTOCOL.Replace(output, URL_REPLACE_BASIC);
            output = URL_REGEX_WITHOUT_PROTOCOL.Replace(output, URL_REPLACE_ADD_PROTOCOL);
            output = URL_ESCAPED_AMPERSAND.Replace(output, "&");
            return output;
        }
    }
}
