using System;
using System.ComponentModel.DataAnnotations;

namespace Palaver.Models.CommentViewModels
{
    public class CreateResultViewModel
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Text { get; set; }
        [Required]
        public int ThreadId { get; set; }
        public int? ParentCommentId { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public string UserName { get; set; }
        [Required]
        public DateTime Created { get; set; }
    }
}
