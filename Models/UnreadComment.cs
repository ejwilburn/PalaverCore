namespace Palaver.Models
{
    public class UnreadComment
    {
        public int UserId { get; set; }
        public User User { get; set; }

        public int CommentId { get; set; }
        public Comment Comment { get; set; }
    }
}