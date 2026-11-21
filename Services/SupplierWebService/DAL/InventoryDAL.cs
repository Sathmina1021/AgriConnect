using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SupplierWebService.Models;

namespace SupplierWebService.DAL
{
    public class InventoryDAL
    {
        private const string Cols = "Id, SupplierId, ItemName, StockQty, UnitPrice, UpdatedAt";

        private static InventoryItem Map(SqlDataReader r)
        {
            return new InventoryItem
            {
                Id = (int)r["Id"],
                SupplierId = (int)r["SupplierId"],
                ItemName = (string)r["ItemName"],
                StockQty = (int)r["StockQty"],
                UnitPrice = (decimal)r["UnitPrice"],
                UpdatedAt = (DateTime)r["UpdatedAt"]
            };
        }

        public List<InventoryItem> GetBySupplier(int supplierId)
        {
            return Db.Query("SELECT " + Cols + " FROM InventoryItems WHERE SupplierId=@sid ORDER BY ItemName", Map, Db.P("@sid", supplierId));
        }

        public InventoryItem GetById(int id)
        {
            return Db.QueryOne("SELECT " + Cols + " FROM InventoryItems WHERE Id=@id", Map, Db.P("@id", id));
        }

        public int Add(InventoryItem i)
        {
            object id = Db.Scalar(
                "INSERT INTO InventoryItems (SupplierId, ItemName, StockQty, UnitPrice) VALUES (@sid, @name, @qty, @price); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                Db.P("@sid", i.SupplierId), Db.P("@name", i.ItemName), Db.P("@qty", i.StockQty), Db.P("@price", i.UnitPrice));
            return (int)id;
        }

        public void Update(InventoryItem i)
        {
            Db.Execute(
                "UPDATE InventoryItems SET ItemName=@name, StockQty=@qty, UnitPrice=@price, UpdatedAt=GETDATE() WHERE Id=@id",
                Db.P("@name", i.ItemName), Db.P("@qty", i.StockQty), Db.P("@price", i.UnitPrice), Db.P("@id", i.Id));
        }

        public void Delete(int id)
        {
            Db.Execute("DELETE FROM InventoryItems WHERE Id=@id", Db.P("@id", id));
        }

        public bool NameExists(int supplierId, string name, int ignoreId)
        {
            int n = (int)Db.Scalar("SELECT COUNT(*) FROM InventoryItems WHERE SupplierId=@sid AND ItemName=@name AND Id<>@id",
                Db.P("@sid", supplierId), Db.P("@name", name), Db.P("@id", ignoreId));
            return n > 0;
        }
    }
}
