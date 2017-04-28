/*
Copyright 2017, E.J. Wilburn, Marcus McKinnon, Kevin Williams
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
using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.Triggers;

namespace Palaver.Models
{
    public abstract class TimeStamper
    {
        [Column(TypeName="timestamptz")]
        public virtual DateTime Created { get; set; }
        [Column(TypeName="timestamptz")]
        public virtual DateTime Updated { get; set; }
        [NotMapped]
        public virtual string CreatedDisplay {
            get {
                return DisplayFormatLocalDateTime(Created);
            }
        }
        [NotMapped]
        public virtual string CreatedIsoTime {
            get {
                return Iso8601FormatDateTime(Created);
            }
        }
        [NotMapped]
        public virtual string UpdatedDisplay {
            get {
                return DisplayFormatLocalDateTime(Updated);
            }
        }
        [NotMapped]
        public virtual string UpdatedIsoTime {
            get {
                return Iso8601FormatDateTime(Updated);
            }
        }

        static TimeStamper()
        {
            Triggers<TimeStamper>.Updating += entry => entry.Entity.Updated = DateTime.UtcNow;
        }

        public TimeStamper()
        {
            Created = DateTime.UtcNow;
            Updated = DateTime.UtcNow;
        }

        /// <summary>
        /// Return a short-format local date and time string from the provided DateTime.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns>Short local date and time string.</returns>
        private string DisplayFormatLocalDateTime(DateTime dateTime)
        {
            return dateTime.ToLocalTime().ToString("g");
        }

        /// <summary>
        /// Return an UTC Iso8601 date/time string from the given DateTime
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns>UTC Iso8601 date/time string.</returns>
        private string Iso8601FormatDateTime(DateTime dateTime)
        {
            return dateTime.ToString("o");
        }
    }
}
