using System;
using System.Runtime.Serialization;

namespace Farmer.Contracts
{
    /// <summary>A supplier's offer for one supply request (data owned by the Supplier service).</summary>
    [DataContract(Namespace = FarmerNamespaces.Data)]
    public class QuotationDto
    {
        [DataMember] public int QuotationId { get; set; }
        [DataMember] public int RequestId { get; set; }
        [DataMember] public int SupplierId { get; set; }
        [DataMember] public string SupplierName { get; set; }
        [DataMember] public decimal UnitPrice { get; set; }
        [DataMember] public decimal TotalPrice { get; set; }
        [DataMember] public int StockQuantity { get; set; }
        [DataMember] public int DeliveryDays { get; set; }
        [DataMember] public DateTime EstimatedDeliveryDate { get; set; }
        [DataMember] public string Notes { get; set; }
        /// <summary>Computed by the Farmer service: supplier stock covers the requested quantity.</summary>
        [DataMember] public bool MeetsStock { get; set; }
        /// <summary>Computed by the Farmer service: delivery estimate is on/before the preferred date.</summary>
        [DataMember] public bool MeetsPreferredDate { get; set; }
        /// <summary>True when the data came from the built-in placeholder (Supplier service not wired up).</summary>
        [DataMember] public bool IsPlaceholder { get; set; }
    }

    [DataContract(Namespace = FarmerNamespaces.Data)]
    public class OrderConfirmationInput
    {
        [DataMember] public int FarmerId { get; set; }
        [DataMember] public int RequestId { get; set; }
        [DataMember] public int QuotationId { get; set; }
    }

    [DataContract(Namespace = FarmerNamespaces.Data)]
    public class ConfirmedOrderDto
    {
        [DataMember] public int OrderId { get; set; }
        [DataMember] public int RequestId { get; set; }
        [DataMember] public int FarmerId { get; set; }
        [DataMember] public string ItemName { get; set; }
        [DataMember] public int SupplierId { get; set; }
        [DataMember] public string SupplierName { get; set; }
        [DataMember] public int QuotationId { get; set; }
        [DataMember] public int Quantity { get; set; }
        [DataMember] public decimal UnitPrice { get; set; }
        [DataMember] public decimal TotalPrice { get; set; }
        [DataMember] public DateTime EstimatedDeliveryDate { get; set; }
        [DataMember] public string OrderStatus { get; set; }
        [DataMember] public bool SupplierNotified { get; set; }
        [DataMember] public bool OfficerNotified { get; set; }
        [DataMember] public DateTime ConfirmedOn { get; set; }
    }
}
