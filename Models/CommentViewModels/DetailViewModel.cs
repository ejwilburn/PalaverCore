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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PalaverCore.Models.CommentViewModels;

public class DetailViewModel
{
    [Required]
    public int Id { get; set; }
    [Required]
    public string DisplayText { get; set; }
    [Required]
    public int ThreadId { get; set; }
    public int? ParentCommentId { get; set; }
    [Required]
    public int UserId { get; set; }
    [Required]
    public bool IsAuthor { get; set; }
    [Required]
    public string UserName { get; set; }
    [Required]
    public string EmailHash { get; set; }
    [Required]
    public bool IsUnread { get; set; }
    [Required]
    public string CreatedDisplay { get; set; }
    [Required]
    public string CreatedIsoTime { get; set; }
    [Required]
    public string Url { get; set; }
    public IEnumerable<DetailViewModel> Comments { get; set; }
}
