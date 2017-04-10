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
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using EntityFrameworkCore.Triggers;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Palaver.Models
{
    public class User : IdentityUser<int>
    {
        [Required]
        public bool NotificationEnabled { get; set; } = true;
        public DateTime Created { get; set; }
        public List<Thread> Threads { get; set; }
        public List<Comment> Comments { get; set; }
        public List<FavoriteThread> FavoriteThreads { get; set; }
        public List<FavoriteComment> FavoriteComments { get; set; }
        public List<Subscription> Subscriptions { get; set; }
        public List<UnreadComment> UnreadComments { get; set; }

        [NotMapped]
        public string EmailHash {
            get {
                if (!String.IsNullOrWhiteSpace(Email))
                    return MD5Hash(Email.Trim().ToLower());
                else
                    return "";
            }
        }

        static User()
        {
            Triggers<User>.Inserting += entry => entry.Entity.Created = DateTime.UtcNow;
        }

        private static string MD5Hash(string input)
        {
            var md5 = MD5.Create();
            var result = md5.ComputeHash(Encoding.ASCII.GetBytes(input));
            return BitConverter.ToString(result).Replace("-","").ToLower();
        }
    }
}
