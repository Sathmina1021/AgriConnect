namespace AgriConnect.Shared.Models;

public class FarmerProfile
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string FarmName { get; set; } = "";
    public string District { get; set; } = "";
    public string Phone { get; set; } = "";
    public double Acres { get; set; }
}

public class SupplyRequest
{
    public int Id { get; set; }
    public int FarmerId { get; set; }
    public string FarmerName { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string Category { get; set; } = "";
    public int Quantity { get; set; }
    public string Unit { get; set; } = "";
    public string DeliveryLocation { get; set; } = "";
    public string PreferredDate { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string? OfficerNote { get; set; }
}

public class CropListing
{
    public int Id { get; set; }
    public int FarmerId { get; set; }
    public string FarmerName { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public string CropName { get; set; } = "";
    public double QuantityKg { get; set; }
    public double PricePerKg { get; set; }
    public string Location { get; set; } = "";
    public string Status { get; set; } = "Available";
}

public record FarmerRegisterRequest(string FullName, string FarmName, string District, string Phone, double Acres);
public record SupplyRequestCreate(int FarmerId, string ItemName, string Category, int Quantity, string Unit, string DeliveryLocation, string PreferredDate);
public record SupplyStatusUpdate(string Status, string? OfficerNote);
public record ConfirmOrderRequest(int QuotationId, string SupplierName, double TotalAmount);
public record CropCreateRequest(int FarmerId, string CropName, double QuantityKg, double PricePerKg, string Location);
