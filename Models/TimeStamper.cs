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
        public virtual DateTime Created { get; set; }
        public virtual DateTime Updated { get; set; }
        [NotMapped]
        public virtual string CreatedDisplay {
            get {
                return DisplayFormatDateTime(Created);
            }
        }
        [NotMapped]
        public virtual string UpdatedDisplay {
            get {
                return DisplayFormatDateTime(Updated);
            }
        }

        static TimeStamper()
        {
            Triggers<TimeStamper>.Inserting += entry => entry.Entity.Created = entry.Entity.Updated = DateTime.UtcNow;
            Triggers<TimeStamper>.Updating += entry => entry.Entity.Updated = DateTime.UtcNow;
        }

        public TimeStamper()
        {
            Created = DateTime.UtcNow;
            Updated = DateTime.UtcNow;
        }

        private string DisplayFormatDateTime(DateTime time)
        {
            if (time.Date == DateTime.Today)
                return time.ToLocalTime().ToString("t");
            else
                return time.ToLocalTime().ToString("d");
        }
    }
}
