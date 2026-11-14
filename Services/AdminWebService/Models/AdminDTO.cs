namespace AdminWebService.Models
{
    public class UserDTO
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string District { get; set; }
        public bool IsActive { get; set; }
    }

    public class AuditLogDTO
    {
        public int LogId { get; set; }
        public string ServiceName { get; set; }
        public string ActionName { get; set; }
        public string Details { get; set; }
        public string LogTime { get; set; }
    }
}
