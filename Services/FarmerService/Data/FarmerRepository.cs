using AgriConnect.Shared.Models;
using AgriConnect.Shared.Storage;

namespace FarmerService.Data;

public record FarmerDbSnapshot(
    List<FarmerProfile> Farmers,
    List<SupplyRequest> SupplyRequests,
    List<CropListing> Crops);

public class FarmerRepository
{
    private readonly JsonFileStore<FarmerDbSnapshot> _store;
    private readonly object _sync = new();

    private List<FarmerProfile> _farmers;
    private List<SupplyRequest> _requests;
    private List<CropListing> _crops;

    public FarmerRepository(string dbPath)
    {
        _store = new JsonFileStore<FarmerDbSnapshot>(dbPath);
        var data = _store.LoadOrCreate(SeedData);
        _farmers = data.Farmers ?? new();
        _requests = data.SupplyRequests ?? new();
        _crops = data.Crops ?? new();
    }

    private static FarmerDbSnapshot SeedData() => new(
        new List<FarmerProfile>
        {
            new() { Id = 1, FullName = "Saman Perera", FarmName = "Perera Family Farm", District = "Homagama", Phone = "0771234567", Acres = 2.5 },
            new() { Id = 2, FullName = "Kamal Silva", FarmName = "Silva Agro Estate", District = "Godagama", Phone = "0772345678", Acres = 1.8 },
            new() { Id = 3, FullName = "Nimal Bandara", FarmName = "Bandara Paddy Fields", District = "Anuradhapura", Phone = "0773456789", Acres = 4.0 }
        },
        new List<SupplyRequest>
        {
            new() { Id = 1, FarmerId = 1, FarmerName = "Saman Perera", ItemName = "Urea Fertilizer", Category = "Fertilizer", Quantity = 10, Unit = "Bags", DeliveryLocation = "Homagama", PreferredDate = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd"), Status = "Approved", OfficerNote = "Approved for agricultural use." },
            new() { Id = 2, FarmerId = 2, FarmerName = "Kamal Silva", ItemName = "Hybrid Paddy Seeds", Category = "Seeds", Quantity = 5, Unit = "Bags", DeliveryLocation = "Godagama", PreferredDate = DateTime.Today.AddDays(5).ToString("yyyy-MM-dd"), Status = "Pending", OfficerNote = null }
        },
        new List<CropListing>
        {
            new() { Id = 1, FarmerId = 1, FarmerName = "Saman Perera", ContactPhone = "0771234567", CropName = "Red Nadu Rice", QuantityKg = 500, PricePerKg = 210, Location = "Homagama", Status = "Available" },
            new() { Id = 2, FarmerId = 3, FarmerName = "Nimal Bandara", ContactPhone = "0773456789", CropName = "Organic Tomatoes", QuantityKg = 120, PricePerKg = 340, Location = "Anuradhapura", Status = "Available" }
        });

    private void Persist() => _store.Save(new FarmerDbSnapshot(_farmers, _requests, _crops));

    public List<FarmerProfile> GetFarmers() { lock (_sync) return _farmers.ToList(); }

    public FarmerProfile? GetFarmerById(int id) { lock (_sync) return _farmers.FirstOrDefault(f => f.Id == id); }

    public FarmerProfile AddFarmer(FarmerRegisterRequest req)
    {
        lock (_sync)
        {
            var nextId = _farmers.Count > 0 ? _farmers.Max(x => x.Id) + 1 : 1;
            var profile = new FarmerProfile
            {
                Id = nextId,
                FullName = req.FullName.Trim(),
                FarmName = req.FarmName.Trim(),
                District = req.District.Trim(),
                Phone = req.Phone.Trim(),
                Acres = req.Acres
            };
            _farmers.Add(profile);
            Persist();
            return profile;
        }
    }

    public List<SupplyRequest> GetSupplyRequests() { lock (_sync) return _requests.OrderByDescending(r => r.Id).ToList(); }

    public SupplyRequest? GetSupplyRequestById(int id) { lock (_sync) return _requests.FirstOrDefault(r => r.Id == id); }

    public SupplyRequest AddSupplyRequest(SupplyRequestCreate req)
    {
        lock (_sync)
        {
            var farmer = _farmers.FirstOrDefault(f => f.Id == req.FarmerId);
            var nextId = _requests.Count > 0 ? _requests.Max(x => x.Id) + 1 : 1;
            var item = new SupplyRequest
            {
                Id = nextId,
                FarmerId = req.FarmerId,
                FarmerName = farmer?.FullName ?? "Unknown Farmer",
                ItemName = req.ItemName.Trim(),
                Category = req.Category,
                Quantity = req.Quantity,
                Unit = req.Unit,
                DeliveryLocation = req.DeliveryLocation.Trim(),
                PreferredDate = req.PreferredDate,
                Status = "Pending"
            };
            _requests.Add(item);
            Persist();
            return item;
        }
    }

    public SupplyRequest? UpdateSupplyStatus(int id, string status, string? officerNote)
    {
        lock (_sync)
        {
            var item = _requests.FirstOrDefault(r => r.Id == id);
            if (item is null) return null;
            item.Status = status;
            if (officerNote is not null) item.OfficerNote = officerNote;
            Persist();
            return item;
        }
    }

    public List<CropListing> GetCrops() { lock (_sync) return _crops.OrderByDescending(c => c.Id).ToList(); }

    public CropListing AddCrop(CropCreateRequest req)
    {
        lock (_sync)
        {
            var farmer = _farmers.FirstOrDefault(f => f.Id == req.FarmerId);
            var nextId = _crops.Count > 0 ? _crops.Max(x => x.Id) + 1 : 1;
            var crop = new CropListing
            {
                Id = nextId,
                FarmerId = req.FarmerId,
                FarmerName = farmer?.FullName ?? "Unknown Farmer",
                ContactPhone = farmer?.Phone ?? "-",
                CropName = req.CropName.Trim(),
                QuantityKg = req.QuantityKg,
                PricePerKg = req.PricePerKg,
                Location = req.Location.Trim(),
                Status = "Available"
            };
            _crops.Add(crop);
            Persist();
            return crop;
        }
    }

    public CropListing? MarkCropSold(int id)
    {
        lock (_sync)
        {
            var crop = _crops.FirstOrDefault(c => c.Id == id);
            if (crop is null) return null;
            crop.Status = "Sold";
            Persist();
            return crop;
        }
    }

    public CropListing? DeleteCrop(int id)
    {
        lock (_sync)
        {
            var crop = _crops.FirstOrDefault(c => c.Id == id);
            if (crop is null) return null;
            _crops.Remove(crop);
            Persist();
            return crop;
        }
    }
}
