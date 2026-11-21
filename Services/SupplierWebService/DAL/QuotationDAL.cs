using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SupplierWebService.Models;

namespace SupplierWebService.DAL
{
    public class QuotationDAL
    {
        private const string Select =
            "SELECT q.Id, q.SupplyRequestId, q.SupplierId, r.ItemName, r.Quantity, q.UnitPrice, q.StockAvailable, " +
            "q.DeliveryDays, q.TotalPrice, q.Status, q.CreatedAt " +
            "FROM Quotations q JOIN SupplyRequests r ON r.RequestId = q.SupplyRequestId ";

        private static Quotation Map(SqlDataReader r)
        {
            return new Quotation
            {
                Id = (int)r["Id"],
                SupplyRequestId = (int)r["SupplyRequestId"],
                SupplierId = (int)r["SupplierId"],
                ItemName = (string)r["ItemName"],
                Quantity = (int)r["Quantity"],
                UnitPrice = (decimal)r["UnitPrice"],
                StockAvailable = (int)r["StockAvailable"],
                DeliveryDays = (int)r["DeliveryDays"],
                TotalPrice = (decimal)r["TotalPrice"],
                Status = (string)r["Status"],
                CreatedAt = (DateTime)r["CreatedAt"]
            };
        }

        public List<Quotation> GetBySupplier(int supplierId)
        {
            return Db.Query(Select + "WHERE q.SupplierId=@sid ORDER BY q.CreatedAt DESC, q.Id DESC", Map, Db.P("@sid", supplierId));
        }

        public Quotation GetById(int id)
        {
            return Db.QueryOne(Select + "WHERE q.Id=@id", Map, Db.P("@id", id));
        }

        public bool Exists(int requestId, int supplierId)
        {
            int n = (int)Db.Scalar("SELECT COUNT(*) FROM Quotations WHERE SupplyRequestId=@rid AND SupplierId=@sid",
                Db.P("@rid", requestId), Db.P("@sid", supplierId));
            return n > 0;
        }

        public int Insert(Quotation q)
        {
            object id = Db.Scalar(
                "INSERT INTO Quotations (SupplyRequestId, SupplierId, UnitPrice, StockAvailable, DeliveryDays, TotalPrice, Status) " +
                "VALUES (@rid, @sid, @price, @stock, @days, @total, N'Submitted'); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                Db.P("@rid", q.SupplyRequestId), Db.P("@sid", q.SupplierId), Db.P("@price", q.UnitPrice),
                Db.P("@stock", q.StockAvailable), Db.P("@days", q.DeliveryDays), Db.P("@total", q.TotalPrice));
            return (int)id;
        }

        public void SetStatus(int id, string status)
        {
            Db.Execute("UPDATE Quotations SET Status=@st WHERE Id=@id", Db.P("@st", status), Db.P("@id", id));
        }

        /// <summary>
        /// Farmer confirmed this quotation: mark it Accepted, close the request, reduce stock and create the delivery.
        /// All four steps run in one transaction so the data never ends up half-updated.
        /// </summary>
        public void Accept(Quotation q)
        {
            using (var con = Db.Open())
            using (var tx = con.BeginTransaction())
            {
                try
                {
                    Run(con, tx, "UPDATE Quotations SET Status=N'Accepted' WHERE Id=@id", Db.P("@id", q.Id));
                    Run(con, tx, "UPDATE SupplyRequests SET Status=N'Closed' WHERE RequestId=@rid", Db.P("@rid", q.SupplyRequestId));
                    Run(con, tx,
                        "UPDATE InventoryItems SET StockQty = StockQty - @qty, UpdatedAt = GETDATE() " +
                        "WHERE SupplierId=@sid AND ItemName=@item AND StockQty >= @qty",
                        Db.P("@qty", q.Quantity), Db.P("@sid", q.SupplierId), Db.P("@item", q.ItemName));
                    Run(con, tx,
                        "INSERT INTO Deliveries (QuotationId, SupplyRequestId, SupplierId, Status) VALUES (@qid, @rid, @sid, N'Pending')",
                        Db.P("@qid", q.Id), Db.P("@rid", q.SupplyRequestId), Db.P("@sid", q.SupplierId));
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        private static void Run(SqlConnection con, SqlTransaction tx, string sql, params SqlParameter[] ps)
        {
            using (var cmd = new SqlCommand(sql, con, tx))
            {
                cmd.Parameters.AddRange(ps);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
