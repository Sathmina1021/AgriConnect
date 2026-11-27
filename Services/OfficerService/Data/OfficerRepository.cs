using AgriConnect.Shared.Models;
using AgriConnect.Shared.Storage;

namespace OfficerService.Data;

public record OfficerDbSnapshot(
    List<Verification> Verifications,
    List<Advisory> Advisories);

public class OfficerRepository
{
    private readonly JsonFileStore<OfficerDbSnapshot> _store;
    private readonly object _sync = new();

    private List<Verification> _verifications;
    private List<Advisory> _advisories;

    public OfficerRepository(string dbPath)
    {
        _store = new JsonFileStore<OfficerDbSnapshot>(dbPath);
        var data = _store.LoadOrCreate(SeedData);
        _verifications = data.Verifications ?? new();
        _advisories = data.Advisories ?? new();
    }

    private static OfficerDbSnapshot SeedData() => new(
        new List<Verification>
        {
            new() { Id = 1, SupplyRequestId = 1, ItemName = "Urea Fertilizer", OfficerName = "Dr. Chaminda Silva", Decision = "Approved", Remarks = "Approved for agricultural use.", VerifiedAt = DateTime.Now }
        },
        new List<Advisory>
        {
            new() { Id = 1, Title = "Monsoon Paddy Care", District = "All Districts", CropType = "Paddy", Message = "Ensure proper field drainage before the upcoming monsoon season to prevent waterlogging.", OfficerName = "Dr. Chaminda Silva", PublishedAt = DateTime.Now }
        });

    private void Persist() => _store.Save(new OfficerDbSnapshot(_verifications, _advisories));

    public List<Verification> GetVerifications() { lock (_sync) return _verifications.OrderByDescending(v => v.Id).ToList(); }

    public Verification AddVerification(int supplyRequestId, string itemName, string officerName, string decision, string remarks)
    {
        lock (_sync)
        {
            var nextId = _verifications.Count > 0 ? _verifications.Max(x => x.Id) + 1 : 1;
            var record = new Verification
            {
                Id = nextId,
                SupplyRequestId = supplyRequestId,
                ItemName = itemName,
                OfficerName = officerName,
                Decision = decision,
                Remarks = remarks,
                VerifiedAt = DateTime.Now
            };
            _verifications.Add(record);
            Persist();
            return record;
        }
    }

    public List<Advisory> GetAdvisories() { lock (_sync) return _advisories.OrderByDescending(a => a.Id).ToList(); }

    public Advisory AddAdvisory(AdvisoryCreateRequest req, string officerName)
    {
        lock (_sync)
        {
            var nextId = _advisories.Count > 0 ? _advisories.Max(x => x.Id) + 1 : 1;
            var advisory = new Advisory
            {
                Id = nextId,
                Title = req.Title.Trim(),
                District = req.District.Trim(),
                CropType = req.CropType.Trim(),
                Message = req.Message.Trim(),
                OfficerName = officerName,
                PublishedAt = DateTime.Now
            };
            _advisories.Add(advisory);
            Persist();
            return advisory;
        }
    }

    public Advisory? DeleteAdvisory(int id)
    {
        lock (_sync)
        {
            var advisory = _advisories.FirstOrDefault(x => x.Id == id);
            if (advisory is null) return null;
            _advisories.Remove(advisory);
            Persist();
            return advisory;
        }
    }
}
