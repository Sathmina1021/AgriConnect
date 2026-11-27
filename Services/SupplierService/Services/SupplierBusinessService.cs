using AgriConnect.Shared.Clients;
using AgriConnect.Shared.Models;
using SupplierService.Data;

namespace SupplierService.Services;

public class SupplierBusinessService
{
    private readonly SupplierRepository _repo;
    private readonly ServiceClientHelper _client;

    public SupplierBusinessService(SupplierRepository repo, ServiceClientHelper client)
    {
        _repo = repo;
        _client = client;
    }

    public object GetHealth() => new
    {
        status = "Running",
        database = "Services/SupplierService/Data/supplier_db.json",
        inventory = _repo.GetInventory().Count,
        quotations = _repo.GetQuotations().Count,
        deliveries = _repo.GetDeliveries().Count
    };

    public List<InventoryItem> GetInventory() => _repo.GetInventory();

    public async Task<InventoryItem> AddInventoryAsync(InventoryCreateRequest req)
    {
        var item = _repo.AddInventory(req);
        await _client.LogAuditAsync("SupplierService", "StockAdded", $"{item.SupplierName} added {item.AvailableQty} {item.Unit} of {item.ProductName}.");
        return item;
    }

    public async Task<bool> UpdateInventoryAsync(int id, InventoryUpdateRequest req)
    {
        var item = _repo.UpdateInventory(id, req);
        if (item is null) return false;
        await _client.LogAuditAsync("SupplierService", "StockUpdated", $"{item.ProductName} stock updated to {item.AvailableQty} {item.Unit} @ Rs.{item.UnitPrice}.");
        return true;
    }

    public async Task<bool> DeleteInventoryAsync(int id)
    {
        var item = _repo.DeleteInventory(id);
        if (item is null) return false;
        await _client.LogAuditAsync("SupplierService", "StockDeleted", $"Inventory item #{id} ({item.ProductName}) removed.");
        return true;
    }

    public async Task<List<object>> GetOpenFarmerRequestsAsync()
    {
        var requests = await _client.GetAsync<List<SupplyRequest>>($"{ServiceEndpoints.Farmer}/api/farmer/internal/requests")
                       ?? new List<SupplyRequest>();
        return requests
            .Where(r => r.Status != "Confirmed" && r.Status != "Rejected")
            .OrderByDescending(r => r.Id)
            .Select(r => (object)new { r.Id, r.FarmerName, r.ItemName, r.Quantity, r.Unit, r.DeliveryLocation, r.Status })
            .ToList();
    }

    public List<Quotation> GetQuotations() => _repo.GetQuotations();
    public List<Quotation> GetQuotationsByRequest(int requestId) => _repo.GetQuotationsByRequest(requestId);

    public async Task<(bool Ok, string? Error, Quotation? Quotation)> SubmitQuotationAsync(QuotationCreateRequest req)
    {
        var requests = await _client.GetAsync<List<SupplyRequest>>($"{ServiceEndpoints.Farmer}/api/farmer/internal/requests")
                       ?? new List<SupplyRequest>();
        var target = requests.FirstOrDefault(r => r.Id == req.SupplyRequestId);
        if (target is null) return (false, "Selected supply request does not exist.", null);

        var itemName = string.IsNullOrWhiteSpace(req.ItemName) ? target.ItemName : req.ItemName;
        var created = _repo.AddQuotation(req, itemName);
        await _client.LogAuditAsync("SupplierService", "Quotation", $"{created.SupplierName} quoted Rs.{created.TotalAmount} for request #{created.SupplyRequestId}.");
        return (true, null, created);
    }

    public List<Delivery> GetDeliveries() => _repo.GetDeliveries();

    public async Task<bool> UpdateDeliveryStatusAsync(int id, string status)
    {
        var delivery = _repo.UpdateDeliveryStatus(id, status);
        if (delivery is null) return false;
        await _client.LogAuditAsync("SupplierService", "DeliveryUpdate", $"Delivery #{id} status changed to {status}.");
        return true;
    }

    public async Task<List<CropListing>> GetAvailableCropsAsync()
    {
        var crops = await _client.GetAsync<List<CropListing>>($"{ServiceEndpoints.Farmer}/api/farmer/internal/crops")
                    ?? new List<CropListing>();
        return crops.Where(c => c.Status == "Available").OrderByDescending(c => c.Id).ToList();
    }

    public Delivery ConfirmQuotationInternal(ConfirmQuotationInternalRequest req) =>
        _repo.ConfirmQuotationAndCreateDelivery(req);
}
