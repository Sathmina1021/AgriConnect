using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SupplierWebService.Models;

namespace SupplierWebService.DAL
{
    public class RequestDAL
    {
        private const string Cols = "RequestId, FarmerName, ItemName, Quantity, DeliveryLocation, Region, PreferredDate, Status";

        private static SupplyRequest Map(SqlDataReader r)
        {
            return new SupplyRequest
            {
                RequestId = (int)r["RequestId"],
                FarmerName = (string)r["FarmerName"],
                ItemName = (string)r["ItemName"],
                Quantity = (int)r["Quantity"],
                DeliveryLocation = (string)r["DeliveryLocation"],
                Region = (string)r["Region"],
                PreferredDate = (DateTime)r["PreferredDate"],
                Status = (string)r["Status"]
            };
        }

        /// <summary>Open requests in the supplier's region that this supplier has not quoted yet.</summary>
        public List<SupplyRequest> GetOpenForSupplier(int supplierId)
        {
            return Db.Query(
                "SELECT " + Cols + " FROM SupplyRequests r " +
                "WHERE r.Status='Open' " +
                "AND r.Region = (SELECT Region FROM Suppliers WHERE Id=@sid) " +
                "AND NOT EXISTS (SELECT 1 FROM Quotations q WHERE q.SupplyRequestId=r.RequestId AND q.SupplierId=@sid) " +
                "ORDER BY r.PreferredDate",
                Map, Db.P("@sid", supplierId));
        }

        public SupplyRequest GetById(int requestId)
        {
            return Db.QueryOne("SELECT " + Cols + " FROM SupplyRequests WHERE RequestId=@id", Map, Db.P("@id", requestId));
        }

        public void Insert(SupplyRequest r)
        {
            Db.Execute(
                "INSERT INTO SupplyRequests (RequestId, FarmerName, ItemName, Quantity, DeliveryLocation, Region, PreferredDate, Status) " +
                "VALUES (@id, @farmer, @item, @qty, @loc, @region, @date, N'Open')",
                Db.P("@id", r.RequestId), Db.P("@farmer", r.FarmerName), Db.P("@item", r.ItemName), Db.P("@qty", r.Quantity),
                Db.P("@loc", r.DeliveryLocation), Db.P("@region", r.Region), Db.P("@date", r.PreferredDate));
        }
    }
}
