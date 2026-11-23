using Farmer.Contracts;

namespace Farmer.Service.Data
{
    /// <summary>Internal-only model (never serialized to clients): profile plus password material.</summary>
    public class FarmerAccount
    {
        public FarmerDto Profile { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public bool IsActive { get; set; }
    }
}
