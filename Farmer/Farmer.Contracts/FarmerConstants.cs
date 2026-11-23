namespace Farmer.Contracts
{
    /// <summary>XML namespaces used by the Farmer SOAP service (keeps WSDL stable and unique).</summary>
    public static class FarmerNamespaces
    {
        public const string Service = "http://agriconnect.lk/farmer/2026";
        public const string Data = "http://agriconnect.lk/farmer/2026/data";
    }

    /// <summary>Supply-request workflow: Pending -> Approved/Rejected -> Confirmed (or Cancelled).</summary>
    public static class RequestStatus
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Confirmed = "Confirmed";
        public const string Cancelled = "Cancelled";
    }

    public static class OrderStatus
    {
        public const string Confirmed = "Confirmed";
        public const string Dispatched = "Dispatched";
        public const string Delivered = "Delivered";
        public const string Cancelled = "Cancelled";
    }

    public static class ListingStatus
    {
        public const string Active = "Active";
        public const string Closed = "Closed";
    }
}
