using AdminService.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Data
{
    public class AdminContext : DbContext
    {
        public AdminContext(DbContextOptions<AdminContext> options) : base(options) { }

        public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
        public DbSet<SystemLog> SystemLogs => Set<SystemLog>();
    }
}
