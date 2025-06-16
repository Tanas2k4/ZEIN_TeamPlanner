namespace ZEIN_TeamPlanner.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string Action { get; set; } // e.g., "CreatedGroup"
        public string EntityType { get; set; } // e.g., "Group"
        public int? EntityId { get; set; }
        public string Details { get; set; } // JSON or string
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
