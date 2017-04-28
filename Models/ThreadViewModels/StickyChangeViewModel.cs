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

using System.ComponentModel.DataAnnotations;

namespace Palaver.Models.ThreadViewModels
{
    public class StickyChangeViewModel
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public bool IsSticky { get; set; }
        [Required]
        public string UpdatedDisplay { get; set; }
        [Required]
        public string UpdatedIsoTime { get; set; }
    }
}
