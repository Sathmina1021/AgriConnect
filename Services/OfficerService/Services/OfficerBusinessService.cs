using AgriConnect.Shared.Clients;
using AgriConnect.Shared.Models;
using OfficerService.Data;

namespace OfficerService.Services;

public class OfficerBusinessService
{
    private readonly OfficerRepository _repo;
    private readonly ServiceClientHelper _client;

    public OfficerBusinessService(OfficerRepository repo, ServiceClientHelper client)
    {
        _repo = repo;
        _client = client;
    }

    public object GetHealth() => new
    {
        status = "Running",
        database = "Services/OfficerService/Data/officer_db.json",
        verifications = _repo.GetVerifications().Count,
        advisories = _repo.GetAdvisories().Count
    };

    public async Task<object> GetWorkflowsAsync()
    {
        var requests = await _client.GetAsync<List<SupplyRequest>>($"{ServiceEndpoints.Farmer}/api/farmer/internal/requests")
                       ?? new List<SupplyRequest>();
        var quotes = await _client.GetAsync<List<Quotation>>($"{ServiceEndpoints.Supplier}/api/supplier/internal/quotations")
                     ?? new List<Quotation>();

        return new
        {
            requests = requests.OrderByDescending(r => r.Id).Select(r => new
            {
                r.Id,
                r.FarmerName,
                r.ItemName,
                r.Quantity,
                r.Unit,
                r.DeliveryLocation,
                r.Status,
                r.OfficerNote
            }),
            quotes = quotes.Select(q => new
            {
                q.Id,
                q.SupplyRequestId,
                q.SupplierName,
                q.UnitPrice,
                q.TotalAmount,
                q.Status
            })
        };
    }

    public List<Verification> GetVerifications() => _repo.GetVerifications();

    public async Task<bool> VerifyRequestAsync(VerifyRequest req, string fallbackOfficerName)
    {
        var updated = await _client.PutAsync(
            $"{ServiceEndpoints.Farmer}/api/farmer/internal/requests/{req.SupplyRequestId}/status",
            new SupplyStatusUpdate(req.Decision, req.Remarks));

        if (!updated) return false;

        var officerName = string.IsNullOrWhiteSpace(req.OfficerName) ? fallbackOfficerName : req.OfficerName;
        var itemName = string.IsNullOrWhiteSpace(req.ItemName) ? $"Request #{req.SupplyRequestId}" : req.ItemName;

        _repo.AddVerification(req.SupplyRequestId, itemName, officerName, req.Decision, req.Remarks);
        await _client.LogAuditAsync("OfficerService", "Verification",
            $"{officerName} marked request #{req.SupplyRequestId} as {req.Decision}.");
        return true;
    }

    public List<Advisory> GetAdvisories() => _repo.GetAdvisories();

    public async Task<Advisory> PublishAdvisoryAsync(AdvisoryCreateRequest req, string fallbackOfficerName)
    {
        var officerName = string.IsNullOrWhiteSpace(req.OfficerName) ? fallbackOfficerName : req.OfficerName;
        var advisory = _repo.AddAdvisory(req, officerName);
        await _client.LogAuditAsync("OfficerService", "Advisory", $"{advisory.OfficerName} published advisory '{advisory.Title}'.");
        return advisory;
    }

    public async Task<bool> DeleteAdvisoryAsync(int id)
    {
        var deleted = _repo.DeleteAdvisory(id);
        if (deleted is null) return false;
        await _client.LogAuditAsync("OfficerService", "AdvisoryDeleted", $"Advisory #{id} ('{deleted.Title}') deleted.");
        return true;
    }
}
