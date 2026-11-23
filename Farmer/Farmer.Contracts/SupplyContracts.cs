using System;
using System.Runtime.Serialization;

namespace Farmer.Contracts
{
    [DataContract(Namespace = FarmerNamespaces.Data)]
    public class SupplyRequestInput
    {
        [DataMember] public int FarmerId { get; set; }
        [DataMember] public string ItemName { get; set; }
        [DataMember] public int Quantity { get; set; }
        [DataMember] public string DeliveryLocation { get; set; }
        [DataMember] public DateTime PreferredDate { get; set; }
    }

    [DataContract(Namespace = FarmerNamespaces.Data)]
    public class SupplyRequestDto
    {
        [DataMember] public int RequestId { get; set; }
        [DataMember] public int FarmerId { get; set; }
        [DataMember] public string ItemName { get; set; }
        [DataMember] public int Quantity { get; set; }
        [DataMember] public string DeliveryLocation { get; set; }
        [DataMember] public DateTime PreferredDate { get; set; }
        [DataMember] public string Status { get; set; }
        [DataMember] public int? OfficerId { get; set; }
        [DataMember] public string OfficerRemarks { get; set; }
        [DataMember] public bool SupplierNotified { get; set; }
        [DataMember] public bool OfficerNotified { get; set; }
        [DataMember] public DateTime CreatedOn { get; set; }
        [DataMember] public DateTime UpdatedOn { get; set; }
    }

    /// <summary>Sent BY the Agriculture Officer service to the Farmer service (callback style).</summary>
    [DataContract(Namespace = FarmerNamespaces.Data)]
    public class OfficerDecisionInput
    {
        [DataMember] public int RequestId { get; set; }
        [DataMember] public int OfficerId { get; set; }
        [DataMember] public bool Approved { get; set; }
        [DataMember] public string Remarks { get; set; }
        /// <summary>Shared integration key agreed between the team's services.</summary>
        [DataMember] public string IntegrationKey { get; set; }
    }
}
