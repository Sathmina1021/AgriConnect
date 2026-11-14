namespace AdminService.Models
{
    public class UserAccount
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "Farmer";
        public string District { get; set; } = "";
        public bool Active { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    public class SystemLog
    {
        public int Id { get; set; }
        public string ServiceName { get; set; } = "";
        public string Action { get; set; } = "";
        public string Details { get; set; } = "";
        public DateTime LogTime { get; set; } = DateTime.Now;
    }
}
