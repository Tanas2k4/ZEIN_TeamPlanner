namespace ZEIN_TeamPlanner.Models
{
    public class Message
    {
        public int MessageId { get; set; }
        public string Content { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public int GroupId { get; set; }
        public Group Group { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsPinned { get; set; } = false; // For pinned messages
        public string? AttachmentUrl { get; set; } // For file attachments
    }
}
