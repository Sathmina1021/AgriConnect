using System.Collections.Generic;
using System.Data.SqlClient;
using SupplierWebService.Models;

namespace SupplierWebService.DAL
{
    public class SupplierDAL
    {
        private static SupplierInfo Map(SqlDataReader r)
        {
            return new SupplierInfo
            {
                Id = (int)r["Id"],
                Name = (string)r["Name"],
                Region = (string)r["Region"],
                Phone = r["Phone"] as string,
                Email = r["Email"] as string
            };
        }

        public List<SupplierInfo> GetAll()
        {
            return Db.Query("SELECT Id, Name, Region, Phone, Email FROM Suppliers ORDER BY Name", Map);
        }

        public SupplierInfo GetById(int id)
        {
            return Db.QueryOne("SELECT Id, Name, Region, Phone, Email FROM Suppliers WHERE Id=@id", Map, Db.P("@id", id));
        }
    }
}
