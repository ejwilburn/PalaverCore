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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Palaver.Data;

namespace Palaver.Models
{
    public class Thread : TimeStamper
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        public bool IsSticky { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public User User { get; set; }

        public List<Comment> Comments { get; set; }
        public List<Subscription> Subscriptions { get; set; }
        public List<FavoriteThread> FavoriteThreads { get; set; }

        [NotMapped]
        public bool IsSubscribed { get; set; }
        [NotMapped]
        public bool IsFavorite { get; set; }
		[NotMapped]
		public int UnreadCount { get; set; }
        [NotMapped]
        public List<Comment> ImmediateChildren {
            get {
                if (_immediateChildren != null)
                    return _immediateChildren;
                else
                {
                    _immediateChildren = new List<Comment>();
                    if (Comments != null && Comments.Count > 0)
                    {
                        _immediateChildren = Comments.FindAll(c => !c.ParentCommentId.HasValue);
                    }
                    return _immediateChildren;
                }
            }
        }

        private List<Comment> _immediateChildren;

        public Thread()
        {
            this.IsSticky = false;
            this.UnreadCount = 0;
        }

        public static Thread CreateThread(string newTitle, User user, PalaverDbContext db)
        {
            Thread newThread = new Thread {
                Title = newTitle,
                UserId = user.Id,
                User = user,
                IsSticky = false,
                IsSubscribed = true,
                UnreadCount = 0
            };

            return newThread;
        }
    }
}
