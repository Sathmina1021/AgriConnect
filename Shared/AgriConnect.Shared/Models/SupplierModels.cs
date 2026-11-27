namespace AgriConnect.Shared.Models;

public class InventoryItem
{
    public int Id { get; set; }
    public string SupplierName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Category { get; set; } = "";
    public int AvailableQty { get; set; }
    public string Unit { get; set; } = "";
    public double UnitPrice { get; set; }
    public string Warehouse { get; set; } = "";
}

public class Quotation
{
    public int Id { get; set; }
    public int SupplyRequestId { get; set; }
    public string ItemName { get; set; } = "";
    public string SupplierName { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public double UnitPrice { get; set; }
    public int OfferedQty { get; set; }
    public int DeliveryDays { get; set; }
    public string Remarks { get; set; } = "";
    public double TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";
}

public class Delivery
{
    public int Id { get; set; }
    public int SupplyRequestId { get; set; }
    public string FarmerName { get; set; } = "";
    public string SupplierName { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string Quantity { get; set; } = "";
    public string DeliveryLocation { get; set; } = "";
    public string Status { get; set; } = "Preparing";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public record InventoryCreateRequest(string SupplierName, string ProductName, string Category, int AvailableQty, string Unit, double UnitPrice, string Warehouse);
public record InventoryUpdateRequest(int AvailableQty, double UnitPrice);
public record QuotationCreateRequest(int SupplyRequestId, string ItemName, string SupplierName, string ContactPhone, double UnitPrice, int OfferedQty, int DeliveryDays, string Remarks);
public record DeliveryStatusRequest(string Status);
public record ConfirmQuotationInternalRequest(int SupplyRequestId, int QuotationId, string FarmerName, string SupplierName, string ItemName, string Quantity, string DeliveryLocation);
