using AgriConnect.Shared.Clients;
using AgriConnect.Shared.Models;
using FarmerService.Data;

namespace FarmerService.Services;

public class FarmerBusinessService
{
    private readonly FarmerRepository _repo;
    private readonly ServiceClientHelper _client;

    public FarmerBusinessService(FarmerRepository repo, ServiceClientHelper client)
    {
        _repo = repo;
        _client = client;
    }

    public object GetHealth() => new
    {
        status = "Running",
        database = "Services/FarmerService/Data/farmer_db.json",
        farmers = _repo.GetFarmers().Count,
        requests = _repo.GetSupplyRequests().Count,
        crops = _repo.GetCrops().Count
    };

    public List<FarmerProfile> GetProfiles() => _repo.GetFarmers();

    public async Task<FarmerProfile> RegisterFarmerAsync(FarmerRegisterRequest req)
    {
        var profile = _repo.AddFarmer(req);
        await _client.LogAuditAsync("FarmerService", "Register", $"New farmer profile created: {profile.FullName} ({profile.District}).");
        return profile;
    }

    public async Task<List<object>> GetSupplyRequestsWithQuoteCountsAsync()
    {
        var requests = _repo.GetSupplyRequests();
        var quotes = await _client.GetAsync<List<Quotation>>($"{ServiceEndpoints.Supplier}/api/supplier/internal/quotations")
                     ?? new List<Quotation>();

        return requests.Select(r => (object)new
        {
            r.Id,
            r.FarmerName,
            r.ItemName,
            r.Category,
            r.Quantity,
            r.Unit,
            r.DeliveryLocation,
            r.PreferredDate,
            r.Status,
            r.OfficerNote,
            quoteCount = quotes.Count(q => q.SupplyRequestId == r.Id)
        }).ToList();
    }

    public async Task<SupplyRequest> CreateSupplyRequestAsync(SupplyRequestCreate req)
    {
        var item = _repo.AddSupplyRequest(req);
        await _client.LogAuditAsync("FarmerService", "SupplyRequest", $"{item.FarmerName} requested {item.Quantity} {item.Unit} of {item.ItemName}.");
        return item;
    }

    public async Task<List<Quotation>> GetQuotationsForRequestAsync(int requestId)
    {
        return await _client.GetAsync<List<Quotation>>($"{ServiceEndpoints.Supplier}/api/supplier/internal/quotations/{requestId}")
               ?? new List<Quotation>();
    }

    public async Task<(bool Ok, int StatusCode, string? Message)> ConfirmOrderAsync(int requestId, ConfirmOrderRequest req)
    {
        var existing = _repo.GetSupplyRequestById(requestId);
        if (existing is null) return (false, 404, "Supply request not found.");
        if (existing.Status != "Approved") return (false, 400, "Only Approved supply requests can be confirmed.");

        var updated = _repo.UpdateSupplyStatus(requestId, "Confirmed", existing.OfficerNote);
        if (updated is null) return (false, 404, "Supply request not found.");

        await _client.PostAsync($"{ServiceEndpoints.Supplier}/api/supplier/internal/confirm",
            new ConfirmQuotationInternalRequest(
                requestId,
                req.QuotationId,
                updated.FarmerName,
                req.SupplierName,
                updated.ItemName,
                $"{updated.Quantity} {updated.Unit}",
                updated.DeliveryLocation));

        await _client.LogAuditAsync("FarmerService", "ConfirmOrder",
            $"{updated.FarmerName} confirmed order #{requestId} with {req.SupplierName} (Rs. {req.TotalAmount}).");

        return (true, 200, null);
    }

    public async Task<List<Delivery>> GetDeliveriesAsync()
    {
        return await _client.GetAsync<List<Delivery>>($"{ServiceEndpoints.Supplier}/api/supplier/internal/deliveries")
               ?? new List<Delivery>();
    }

    public async Task<List<Advisory>> GetAdvisoriesAsync()
    {
        return await _client.GetAsync<List<Advisory>>($"{ServiceEndpoints.Officer}/api/officer/internal/advisories")
               ?? new List<Advisory>();
    }

    public List<CropListing> GetCrops() => _repo.GetCrops();

    public async Task<CropListing> PublishCropAsync(CropCreateRequest req)
    {
        var crop = _repo.AddCrop(req);
        await _client.LogAuditAsync("FarmerService", "CropListing", $"{crop.FarmerName} published {crop.QuantityKg}kg of {crop.CropName}.");
        return crop;
    }

    public async Task<bool> MarkCropSoldAsync(int id)
    {
        var crop = _repo.MarkCropSold(id);
        if (crop is null) return false;
        await _client.LogAuditAsync("FarmerService", "CropSold", $"Crop listing #{id} ({crop.CropName}) marked as sold.");
        return true;
    }

    public async Task<bool> DeleteCropAsync(int id)
    {
        var crop = _repo.DeleteCrop(id);
        if (crop is null) return false;
        await _client.LogAuditAsync("FarmerService", "CropDeleted", $"Crop listing #{id} ({crop.CropName}) removed.");
        return true;
    }

    public List<SupplyRequest> GetRawSupplyRequests() => _repo.GetSupplyRequests();

    public SupplyRequest? UpdateSupplyStatusInternal(int id, SupplyStatusUpdate update) =>
        _repo.UpdateSupplyStatus(id, update.Status, update.OfficerNote);

    public FarmerProfile AddProfileInternal(FarmerRegisterRequest req) => _repo.AddFarmer(req);
}
