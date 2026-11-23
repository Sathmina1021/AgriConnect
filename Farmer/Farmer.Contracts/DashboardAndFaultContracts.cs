using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Farmer.Contracts
{
    [DataContract(Namespace = FarmerNamespaces.Data)]
    public class DashboardSummary
    {
        [DataMember] public int FarmerId { get; set; }
        [DataMember] public string FarmerName { get; set; }
        [DataMember] public int TotalSupplyRequests { get; set; }
        [DataMember] public int PendingApprovals { get; set; }
        [DataMember] public int ApprovedReadyToConfirm { get; set; }
        [DataMember] public int RejectedRequests { get; set; }
        [DataMember] public int ConfirmedOrders { get; set; }
        [DataMember] public decimal TotalOrderValue { get; set; }
        [DataMember] public int ActiveCropListings { get; set; }
        [DataMember] public List<SupplyRequestDto> RecentRequests { get; set; }
        [DataMember] public List<CropListingDto> RecentListings { get; set; }
    }

    /// <summary>
    /// Business/validation error detail carried inside a SOAP fault, so clients never see raw exceptions.
    /// ErrorCode examples: ValidationError, NotFound, WorkflowViolation, Unauthorized, DatabaseError, UnexpectedError.
    /// </summary>
    [DataContract(Namespace = FarmerNamespaces.Data)]
    public class ServiceFault
    {
        [DataMember] public string ErrorCode { get; set; }
        [DataMember] public string Message { get; set; }
        [DataMember] public List<string> ValidationErrors { get; set; }
    }
}
