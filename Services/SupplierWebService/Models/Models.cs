using System;
using System.Web.Services.Protocols;

namespace SupplierWebService.Models
{
    /// <summary>SOAP header carrying the API key (see "API Keys" in the Data Management and Security lecture).</summary>
    public class AuthHeader : SoapHeader
    {
        public string ApiKey { get; set; }
    }

    public class SupplierInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Region { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
    }

    public class InventoryItem
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public string ItemName { get; set; }
        public int StockQty { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class SupplyRequest
    {
        public int RequestId { get; set; }
        public string FarmerName { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public string DeliveryLocation { get; set; }
        public string Region { get; set; }
        public DateTime PreferredDate { get; set; }
        public string Status { get; set; }
    }

    public class Quotation
    {
        public int Id { get; set; }
        public int SupplyRequestId { get; set; }
        public int SupplierId { get; set; }
        public string ItemName { get; set; }        // filled by the service (from the request)
        public int Quantity { get; set; }           // filled by the service (from the request)
        public decimal UnitPrice { get; set; }
        public int StockAvailable { get; set; }
        public int DeliveryDays { get; set; }
        public decimal TotalPrice { get; set; }     // calculated by the service
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Delivery
    {
        public int Id { get; set; }
        public int QuotationId { get; set; }
        public int SupplyRequestId { get; set; }
        public int SupplierId { get; set; }
        public string ItemName { get; set; }
        public string DeliveryLocation { get; set; }
        public string Status { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class DashboardSummary
    {
        public int TotalInventoryItems { get; set; }
        public int LowStockItems { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public int OpenRequests { get; set; }
        public int PendingQuotations { get; set; }
        public int ApprovedQuotations { get; set; }
        public int AcceptedOrders { get; set; }
        public int ActiveDeliveries { get; set; }
        public int CompletedDeliveries { get; set; }
    }
}
