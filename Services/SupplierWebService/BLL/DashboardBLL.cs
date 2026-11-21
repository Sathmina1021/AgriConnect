using SupplierWebService.DAL;
using SupplierWebService.Models;

namespace SupplierWebService.BLL
{
    public class DashboardBLL
    {
        public DashboardSummary GetSummary(int supplierId)
        {
            if (new SupplierDAL().GetById(supplierId) == null) throw new BusinessException("Supplier not found.");
            return new DashboardDAL().Get(supplierId, InventoryBLL.LowStockLimit);
        }
    }
}
