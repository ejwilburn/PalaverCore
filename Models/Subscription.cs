namespace Palaver.Models
{
    public class Subscription
    {
        public int UserId { get; set; }
        public User User { get; set; }

        public int ThreadId { get; set; }
        public Thread Thread { get; set; }
    }
}