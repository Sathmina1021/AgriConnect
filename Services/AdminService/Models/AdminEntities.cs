using System.ComponentModel.DataAnnotations;

namespace AdminService.Models
{
    public class PlatformUser
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string FullName { get; set; } = string.Empty;
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Role { get; set; } = "Farmer"; // Farmer | Supplier | Officer | Admin
        public string Region { get; set; } = "Western";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AuditLog
    {
        [Key]
        public int Id { get; set; }
        public string SourceService { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
