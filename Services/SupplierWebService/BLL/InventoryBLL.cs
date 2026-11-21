using System.Collections.Generic;
using SupplierWebService.DAL;
using SupplierWebService.Models;

namespace SupplierWebService.BLL
{
    public class InventoryBLL
    {
        public const int LowStockLimit = 25;

        private readonly InventoryDAL _dal = new InventoryDAL();
        private readonly SupplierDAL _suppliers = new SupplierDAL();

        public List<InventoryItem> GetInventory(int supplierId)
        {
            return _dal.GetBySupplier(supplierId);
        }

        public int AddItem(InventoryItem item)
        {
            if (item == null) throw new BusinessException("Item is required.");
            if (_suppliers.GetById(item.SupplierId) == null) throw new BusinessException("Supplier not found.");
            Validate(item);
            if (_dal.NameExists(item.SupplierId, item.ItemName.Trim(), 0))
                throw new BusinessException("This item already exists in your inventory. Update its stock instead.");
            item.ItemName = item.ItemName.Trim();
            return _dal.Add(item);
        }

        public void UpdateItem(InventoryItem item)
        {
            if (item == null) throw new BusinessException("Item is required.");
            var existing = _dal.GetById(item.Id);
            if (existing == null) throw new BusinessException("Inventory item not found.");
            Validate(item);
            if (_dal.NameExists(existing.SupplierId, item.ItemName.Trim(), item.Id))
                throw new BusinessException("Another item with this name already exists.");
            item.ItemName = item.ItemName.Trim();
            _dal.Update(item);
        }

        public void DeleteItem(int id)
        {
            if (_dal.GetById(id) == null) throw new BusinessException("Inventory item not found.");
            _dal.Delete(id);
        }

        private static void Validate(InventoryItem i)
        {
            if (string.IsNullOrWhiteSpace(i.ItemName)) throw new BusinessException("Item name is required.");
            if (i.ItemName.Trim().Length > 100) throw new BusinessException("Item name is too long (max 100 characters).");
            if (i.StockQty < 0) throw new BusinessException("Stock quantity cannot be negative.");
            if (i.UnitPrice <= 0) throw new BusinessException("Unit price must be greater than 0.");
        }
    }
}
