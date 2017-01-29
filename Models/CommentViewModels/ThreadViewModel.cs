using System;
using System.ComponentModel.DataAnnotations;

namespace Palaver.Models.CommentViewModels
{
    public class ThreadViewModel
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Text { get; set; }
        [Required]
        public int? ParentCommentId { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public string UserName { get; set; }
        [Required]
        public bool IsUnread { get; set; }
        [Required]
        public bool IsFavorite { get; set; }
        [Required]
        public DateTime Created { get; set; }
    }
}
