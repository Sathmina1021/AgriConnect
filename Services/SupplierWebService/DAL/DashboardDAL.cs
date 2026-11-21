using SupplierWebService.Models;

namespace SupplierWebService.DAL
{
    public class DashboardDAL
    {
        public DashboardSummary Get(int supplierId, int lowStockLimit)
        {
            var s = new DashboardSummary();

            s.TotalInventoryItems = (int)Db.Scalar("SELECT COUNT(*) FROM InventoryItems WHERE SupplierId=@sid", Db.P("@sid", supplierId));
            s.LowStockItems = (int)Db.Scalar("SELECT COUNT(*) FROM InventoryItems WHERE SupplierId=@sid AND StockQty < @lim",
                Db.P("@sid", supplierId), Db.P("@lim", lowStockLimit));
            s.TotalInventoryValue = (decimal)Db.Scalar("SELECT ISNULL(SUM(StockQty * UnitPrice), 0) FROM InventoryItems WHERE SupplierId=@sid",
                Db.P("@sid", supplierId));
            s.OpenRequests = (int)Db.Scalar(
                "SELECT COUNT(*) FROM SupplyRequests r WHERE r.Status='Open' " +
                "AND r.Region = (SELECT Region FROM Suppliers WHERE Id=@sid) " +
                "AND NOT EXISTS (SELECT 1 FROM Quotations q WHERE q.SupplyRequestId=r.RequestId AND q.SupplierId=@sid)",
                Db.P("@sid", supplierId));
            s.PendingQuotations = Count("Quotations", "Submitted", supplierId);
            s.ApprovedQuotations = Count("Quotations", "Approved", supplierId);
            s.AcceptedOrders = Count("Quotations", "Accepted", supplierId);
            s.ActiveDeliveries = (int)Db.Scalar("SELECT COUNT(*) FROM Deliveries WHERE SupplierId=@sid AND Status<>'Delivered'", Db.P("@sid", supplierId));
            s.CompletedDeliveries = Count("Deliveries", "Delivered", supplierId);
            return s;
        }

        private static int Count(string table, string status, int supplierId)
        {
            return (int)Db.Scalar("SELECT COUNT(*) FROM " + table + " WHERE SupplierId=@sid AND Status=@st",
                Db.P("@sid", supplierId), Db.P("@st", status));
        }
    }
}
