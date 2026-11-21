using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SupplierWebService.Models;

namespace SupplierWebService.DAL
{
    public class DeliveryDAL
    {
        private const string Select =
            "SELECT d.Id, d.QuotationId, d.SupplyRequestId, d.SupplierId, r.ItemName, r.DeliveryLocation, d.Status, d.UpdatedAt " +
            "FROM Deliveries d JOIN SupplyRequests r ON r.RequestId = d.SupplyRequestId ";

        private static Delivery Map(SqlDataReader r)
        {
            return new Delivery
            {
                Id = (int)r["Id"],
                QuotationId = (int)r["QuotationId"],
                SupplyRequestId = (int)r["SupplyRequestId"],
                SupplierId = (int)r["SupplierId"],
                ItemName = (string)r["ItemName"],
                DeliveryLocation = (string)r["DeliveryLocation"],
                Status = (string)r["Status"],
                UpdatedAt = (DateTime)r["UpdatedAt"]
            };
        }

        public List<Delivery> GetBySupplier(int supplierId)
        {
            return Db.Query(Select + "WHERE d.SupplierId=@sid ORDER BY d.UpdatedAt DESC, d.Id DESC", Map, Db.P("@sid", supplierId));
        }

        public Delivery GetById(int id)
        {
            return Db.QueryOne(Select + "WHERE d.Id=@id", Map, Db.P("@id", id));
        }

        public void SetStatus(int id, string status)
        {
            Db.Execute("UPDATE Deliveries SET Status=@st, UpdatedAt=GETDATE() WHERE Id=@id", Db.P("@st", status), Db.P("@id", id));
        }
    }
}
