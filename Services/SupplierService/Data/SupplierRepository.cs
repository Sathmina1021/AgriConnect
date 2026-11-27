using AgriConnect.Shared.Models;
using AgriConnect.Shared.Storage;

namespace SupplierService.Data;

public record SupplierDbSnapshot(
    List<InventoryItem> Inventory,
    List<Quotation> Quotations,
    List<Delivery> Deliveries);

public class SupplierRepository
{
    private readonly JsonFileStore<SupplierDbSnapshot> _store;
    private readonly object _sync = new();

    private List<InventoryItem> _inventory;
    private List<Quotation> _quotations;
    private List<Delivery> _deliveries;

    public SupplierRepository(string dbPath)
    {
        _store = new JsonFileStore<SupplierDbSnapshot>(dbPath);
        var data = _store.LoadOrCreate(SeedData);
        _inventory = data.Inventory ?? new();
        _quotations = data.Quotations ?? new();
        _deliveries = data.Deliveries ?? new();
    }

    private static SupplierDbSnapshot SeedData() => new(
        new List<InventoryItem>
        {
            new() { Id = 1, SupplierName = "Lanka Agro Supplies", ProductName = "Urea Fertilizer", Category = "Fertilizer", AvailableQty = 400, Unit = "Bags", UnitPrice = 850, Warehouse = "Kurunegala" },
            new() { Id = 2, SupplierName = "GreenField Distributors", ProductName = "Hybrid Paddy Seeds", Category = "Seeds", AvailableQty = 150, Unit = "Bags", UnitPrice = 1200, Warehouse = "Anuradhapura" }
        },
        new List<Quotation>
        {
            new() { Id = 1, SupplyRequestId = 1, ItemName = "Urea Fertilizer", SupplierName = "Lanka Agro Supplies", ContactPhone = "0372229010", UnitPrice = 850, OfferedQty = 10, DeliveryDays = 2, Remarks = "Available in stock", TotalAmount = 8500, Status = "Pending" },
            new() { Id = 2, SupplyRequestId = 1, ItemName = "Urea Fertilizer", SupplierName = "GreenField Distributors", ContactPhone = "0372229044", UnitPrice = 820, OfferedQty = 10, DeliveryDays = 1, Remarks = "Fast delivery available", TotalAmount = 8200, Status = "Pending" }
        },
        new List<Delivery>());

    private void Persist() => _store.Save(new SupplierDbSnapshot(_inventory, _quotations, _deliveries));

    public List<InventoryItem> GetInventory() { lock (_sync) return _inventory.OrderByDescending(i => i.Id).ToList(); }

    public InventoryItem AddInventory(InventoryCreateRequest req)
    {
        lock (_sync)
        {
            var nextId = _inventory.Count > 0 ? _inventory.Max(x => x.Id) + 1 : 1;
            var item = new InventoryItem
            {
                Id = nextId,
                SupplierName = req.SupplierName.Trim(),
                ProductName = req.ProductName.Trim(),
                Category = req.Category,
                AvailableQty = req.AvailableQty,
                Unit = req.Unit,
                UnitPrice = req.UnitPrice,
                Warehouse = req.Warehouse.Trim()
            };
            _inventory.Add(item);
            Persist();
            return item;
        }
    }

    public InventoryItem? UpdateInventory(int id, InventoryUpdateRequest req)
    {
        lock (_sync)
        {
            var item = _inventory.FirstOrDefault(x => x.Id == id);
            if (item is null) return null;
            item.AvailableQty = req.AvailableQty;
            item.UnitPrice = req.UnitPrice;
            Persist();
            return item;
        }
    }

    public InventoryItem? DeleteInventory(int id)
    {
        lock (_sync)
        {
            var item = _inventory.FirstOrDefault(x => x.Id == id);
            if (item is null) return null;
            _inventory.Remove(item);
            Persist();
            return item;
        }
    }

    public List<Quotation> GetQuotations() { lock (_sync) return _quotations.OrderByDescending(q => q.Id).ToList(); }

    public List<Quotation> GetQuotationsByRequest(int requestId)
    {
        lock (_sync) return _quotations.Where(q => q.SupplyRequestId == requestId).ToList();
    }

    public Quotation AddQuotation(QuotationCreateRequest req, string resolvedItemName)
    {
        lock (_sync)
        {
            var nextId = _quotations.Count > 0 ? _quotations.Max(x => x.Id) + 1 : 1;
            var q = new Quotation
            {
                Id = nextId,
                SupplyRequestId = req.SupplyRequestId,
                ItemName = resolvedItemName,
                SupplierName = req.SupplierName.Trim(),
                ContactPhone = req.ContactPhone.Trim(),
                UnitPrice = req.UnitPrice,
                OfferedQty = req.OfferedQty,
                DeliveryDays = req.DeliveryDays,
                Remarks = req.Remarks.Trim(),
                TotalAmount = req.UnitPrice * req.OfferedQty,
                Status = "Pending"
            };
            _quotations.Add(q);
            Persist();
            return q;
        }
    }

    public Delivery ConfirmQuotationAndCreateDelivery(ConfirmQuotationInternalRequest req)
    {
        lock (_sync)
        {
            foreach (var q in _quotations.Where(q => q.SupplyRequestId == req.SupplyRequestId))
                q.Status = q.Id == req.QuotationId ? "Confirmed" : "NotSelected";

            var nextId = _deliveries.Count > 0 ? _deliveries.Max(x => x.Id) + 1 : 1;
            var delivery = new Delivery
            {
                Id = nextId,
                SupplyRequestId = req.SupplyRequestId,
                FarmerName = req.FarmerName,
                SupplierName = req.SupplierName,
                ItemName = req.ItemName,
                Quantity = req.Quantity,
                DeliveryLocation = req.DeliveryLocation,
                Status = "Preparing",
                UpdatedAt = DateTime.Now
            };
            _deliveries.Add(delivery);
            Persist();
            return delivery;
        }
    }

    public List<Delivery> GetDeliveries() { lock (_sync) return _deliveries.OrderByDescending(d => d.Id).ToList(); }

    public Delivery? UpdateDeliveryStatus(int id, string status)
    {
        lock (_sync)
        {
            var d = _deliveries.FirstOrDefault(x => x.Id == id);
            if (d is null) return null;
            d.Status = status;
            d.UpdatedAt = DateTime.Now;
            Persist();
            return d;
        }
    }
}
