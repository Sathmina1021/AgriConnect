using System;
using System.Runtime.Serialization;

namespace Farmer.Contracts
{
    /// <summary>Used for both create (ListingId = 0) and update.</summary>
    [DataContract(Namespace = FarmerNamespaces.Data)]
    public class CropListingInput
    {
        [DataMember] public int ListingId { get; set; }
        [DataMember] public int FarmerId { get; set; }
        [DataMember] public string CropName { get; set; }
        [DataMember] public decimal Quantity { get; set; }
        [DataMember] public string Unit { get; set; }
        [DataMember] public decimal UnitPrice { get; set; }
        [DataMember] public DateTime AvailabilityDate { get; set; }
        [DataMember] public string Notes { get; set; }
        [DataMember] public string ContactInfo { get; set; }
    }

    [DataContract(Namespace = FarmerNamespaces.Data)]
    public class CropListingDto
    {
        [DataMember] public int ListingId { get; set; }
        [DataMember] public int FarmerId { get; set; }
        [DataMember] public string FarmerName { get; set; }
        [DataMember] public string District { get; set; }
        [DataMember] public string CropName { get; set; }
        [DataMember] public decimal Quantity { get; set; }
        [DataMember] public string Unit { get; set; }
        [DataMember] public decimal UnitPrice { get; set; }
        [DataMember] public DateTime AvailabilityDate { get; set; }
        [DataMember] public string Notes { get; set; }
        [DataMember] public string ContactInfo { get; set; }
        [DataMember] public string Status { get; set; }
        [DataMember] public DateTime CreatedOn { get; set; }
        [DataMember] public DateTime UpdatedOn { get; set; }
    }
}
