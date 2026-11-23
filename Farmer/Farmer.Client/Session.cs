using Farmer.Client.FarmerServiceReference;

namespace Farmer.Client
{
    /// <summary>Holds the logged-in farmer for the lifetime of the application window.</summary>
    internal static class Session
    {
        public static FarmerDto CurrentFarmer { get; set; }
        public static int FarmerId { get { return CurrentFarmer == null ? 0 : CurrentFarmer.FarmerId; } }
    }
}
