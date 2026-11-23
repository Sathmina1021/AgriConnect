using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Farmer.Contracts;

namespace Farmer.Service.Data
{
    /// <summary>
    /// ADO.NET implementation. Every statement is parameterized (no string concatenation of user
    /// input) and talks only to the AgriConnect_Farmer_SOC database.
    /// </summary>
    public class FarmerRepository : IFarmerRepository
    {
        private readonly DbConnectionFactory _db;

        public FarmerRepository() : this(new DbConnectionFactory()) { }
        public FarmerRepository(DbConnectionFactory db) { _db = db; }

        // =====================================================================
        // Small reusable helpers
        // =====================================================================
        private static SqlCommand NewCommand(SqlConnection c, string sql, SqlTransaction tx = null)
        {
            var cmd = new SqlCommand(sql, c);
            cmd.Transaction = tx;
            return cmd;
        }

        private static void AddParam(SqlCommand cmd, string name, SqlDbType type, object value, int size = 0)
        {
            SqlParameter p = size > 0 ? cmd.Parameters.Add(name, type, size) : cmd.Parameters.Add(name, type);
            p.Value = value ?? DBNull.Value;
        }

        private static void AddDecimal(SqlCommand cmd, string name, decimal value, byte precision, byte scale)
        {
            var p = new SqlParameter(name, SqlDbType.Decimal) { Precision = precision, Scale = scale, Value = value };
            cmd.Parameters.Add(p);
        }

        private List<T> Query<T>(string sql, Action<SqlCommand> bind, Func<SqlDataReader, T> map)
        {
            var list = new List<T>();
            using (SqlConnection conn = _db.CreateOpenConnection())
            using (SqlCommand cmd = NewCommand(conn, sql))
            {
                if (bind != null) bind(cmd);
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read()) list.Add(map(r));
                }
            }
            return list;
        }

        private T QuerySingle<T>(string sql, Action<SqlCommand> bind, Func<SqlDataReader, T> map) where T : class
        {
            List<T> rows = Query(sql, bind, map);
            return rows.Count > 0 ? rows[0] : null;
        }

        private int NonQuery(string sql, Action<SqlCommand> bind)
        {
            using (SqlConnection conn = _db.CreateOpenConnection())
            using (SqlCommand cmd = NewCommand(conn, sql))
            {
                if (bind != null) bind(cmd);
                return cmd.ExecuteNonQuery();
            }
        }

        private static string Str(SqlDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? null : r.GetString(i);
        }

        // =====================================================================
        // Farmers
        // =====================================================================
        private const string FarmerColumns =
            "FarmerId, FullName, Email, Phone, District, [Address], RegisteredOn, PasswordHash, PasswordSalt, IsActive";

        private static FarmerAccount MapAccount(SqlDataReader r)
        {
            return new FarmerAccount
            {
                Profile = new FarmerDto
                {
                    FarmerId = r.GetInt32(r.GetOrdinal("FarmerId")),
                    FullName = Str(r, "FullName"),
                    Email = Str(r, "Email"),
                    Phone = Str(r, "Phone"),
                    District = Str(r, "District"),
                    Address = Str(r, "Address"),
                    RegisteredOn = r.GetDateTime(r.GetOrdinal("RegisteredOn"))
                },
                PasswordHash = Str(r, "PasswordHash"),
                PasswordSalt = Str(r, "PasswordSalt"),
                IsActive = r.GetBoolean(r.GetOrdinal("IsActive"))
            };
        }

        public bool EmailExists(string email)
        {
            using (SqlConnection conn = _db.CreateOpenConnection())
            using (SqlCommand cmd = NewCommand(conn, "SELECT COUNT(1) FROM dbo.Farmers WHERE Email = @Email"))
            {
                AddParam(cmd, "@Email", SqlDbType.NVarChar, email, 150);
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public int InsertFarmer(FarmerRegistration reg, string passwordHash, string passwordSalt)
        {
            const string sql =
                "INSERT INTO dbo.Farmers (FullName, Email, PasswordHash, PasswordSalt, Phone, District, [Address]) " +
                "VALUES (@FullName, @Email, @Hash, @Salt, @Phone, @District, @Address); " +
                "SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using (SqlConnection conn = _db.CreateOpenConnection())
            using (SqlCommand cmd = NewCommand(conn, sql))
            {
                AddParam(cmd, "@FullName", SqlDbType.NVarChar, reg.FullName, 100);
                AddParam(cmd, "@Email", SqlDbType.NVarChar, reg.Email, 150);
                AddParam(cmd, "@Hash", SqlDbType.VarChar, passwordHash, 64);
                AddParam(cmd, "@Salt", SqlDbType.VarChar, passwordSalt, 32);
                AddParam(cmd, "@Phone", SqlDbType.NVarChar, reg.Phone, 20);
                AddParam(cmd, "@District", SqlDbType.NVarChar, reg.District, 60);
                AddParam(cmd, "@Address", SqlDbType.NVarChar, reg.Address, 250);
                return (int)cmd.ExecuteScalar();
            }
        }

        public FarmerAccount GetAccountByEmail(string email)
        {
            return QuerySingle(
                "SELECT " + FarmerColumns + " FROM dbo.Farmers WHERE Email = @Email",
                cmd => AddParam(cmd, "@Email", SqlDbType.NVarChar, email, 150),
                MapAccount);
        }

        public FarmerDto GetFarmerById(int farmerId)
        {
            FarmerAccount a = QuerySingle(
                "SELECT " + FarmerColumns + " FROM dbo.Farmers WHERE FarmerId = @Id",
                cmd => AddParam(cmd, "@Id", SqlDbType.Int, farmerId),
                MapAccount);
            return a == null ? null : a.Profile;
        }

        // =====================================================================
        // Dashboard
        // =====================================================================
        public DashboardSummary GetDashboardCounts(int farmerId)
        {
            const string sql =
                "SELECT " +
                " (SELECT COUNT(*) FROM dbo.SupplyRequests WHERE FarmerId=@Id) AS TotalRequests," +
                " (SELECT COUNT(*) FROM dbo.SupplyRequests WHERE FarmerId=@Id AND [Status]='Pending') AS Pending," +
                " (SELECT COUNT(*) FROM dbo.SupplyRequests WHERE FarmerId=@Id AND [Status]='Approved') AS Approved," +
                " (SELECT COUNT(*) FROM dbo.SupplyRequests WHERE FarmerId=@Id AND [Status]='Rejected') AS Rejected," +
                " (SELECT COUNT(*) FROM dbo.ConfirmedOrders WHERE FarmerId=@Id AND OrderStatus<>'Cancelled') AS Orders," +
                " (SELECT ISNULL(SUM(TotalPrice),0) FROM dbo.ConfirmedOrders WHERE FarmerId=@Id AND OrderStatus<>'Cancelled') AS OrderValue," +
                " (SELECT COUNT(*) FROM dbo.CropListings WHERE FarmerId=@Id AND [Status]='Active') AS ActiveListings";

            List<DashboardSummary> rows = Query(sql,
                cmd => AddParam(cmd, "@Id", SqlDbType.Int, farmerId),
                r => new DashboardSummary
                {
                    FarmerId = farmerId,
                    TotalSupplyRequests = r.GetInt32(0),
                    PendingApprovals = r.GetInt32(1),
                    ApprovedReadyToConfirm = r.GetInt32(2),
                    RejectedRequests = r.GetInt32(3),
                    ConfirmedOrders = r.GetInt32(4),
                    TotalOrderValue = r.GetDecimal(5),
                    ActiveCropListings = r.GetInt32(6)
                });
            return rows[0];
        }

        // =====================================================================
        // Supply requests
        // =====================================================================
        private const string RequestColumns =
            "RequestId, FarmerId, ItemName, Quantity, DeliveryLocation, PreferredDate, [Status], OfficerId, " +
            "OfficerRemarks, SupplierNotified, OfficerNotified, CreatedOn, UpdatedOn";

        private static SupplyRequestDto MapRequest(SqlDataReader r)
        {
            int officerOrdinal = r.GetOrdinal("OfficerId");
            return new SupplyRequestDto
            {
                RequestId = r.GetInt32(r.GetOrdinal("RequestId")),
                FarmerId = r.GetInt32(r.GetOrdinal("FarmerId")),
                ItemName = Str(r, "ItemName"),
                Quantity = r.GetInt32(r.GetOrdinal("Quantity")),
                DeliveryLocation = Str(r, "DeliveryLocation"),
                PreferredDate = r.GetDateTime(r.GetOrdinal("PreferredDate")),
                Status = Str(r, "Status"),
                OfficerId = r.IsDBNull(officerOrdinal) ? (int?)null : r.GetInt32(officerOrdinal),
                OfficerRemarks = Str(r, "OfficerRemarks"),
                SupplierNotified = r.GetBoolean(r.GetOrdinal("SupplierNotified")),
                OfficerNotified = r.GetBoolean(r.GetOrdinal("OfficerNotified")),
                CreatedOn = r.GetDateTime(r.GetOrdinal("CreatedOn")),
                UpdatedOn = r.GetDateTime(r.GetOrdinal("UpdatedOn"))
            };
        }

        public int InsertSupplyRequest(SupplyRequestInput input)
        {
            const string sql =
                "INSERT INTO dbo.SupplyRequests (FarmerId, ItemName, Quantity, DeliveryLocation, PreferredDate) " +
                "VALUES (@FarmerId, @Item, @Qty, @Location, @Preferred); " +
                "SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using (SqlConnection conn = _db.CreateOpenConnection())
            using (SqlCommand cmd = NewCommand(conn, sql))
            {
                AddParam(cmd, "@FarmerId", SqlDbType.Int, input.FarmerId);
                AddParam(cmd, "@Item", SqlDbType.NVarChar, input.ItemName, 100);
                AddParam(cmd, "@Qty", SqlDbType.Int, input.Quantity);
                AddParam(cmd, "@Location", SqlDbType.NVarChar, input.DeliveryLocation, 200);
                AddParam(cmd, "@Preferred", SqlDbType.Date, input.PreferredDate.Date);
                return (int)cmd.ExecuteScalar();
            }
        }

        public SupplyRequestDto GetSupplyRequestById(int requestId)
        {
            return QuerySingle(
                "SELECT " + RequestColumns + " FROM dbo.SupplyRequests WHERE RequestId = @Id",
                cmd => AddParam(cmd, "@Id", SqlDbType.Int, requestId),
                MapRequest);
        }

        public List<SupplyRequestDto> GetSupplyRequestsByFarmer(int farmerId, string statusFilter, int? top)
        {
            string sql =
                "SELECT " + (top.HasValue ? "TOP (@Top) " : "") + RequestColumns +
                " FROM dbo.SupplyRequests WHERE FarmerId = @FarmerId" +
                (string.IsNullOrEmpty(statusFilter) ? "" : " AND [Status] = @Status") +   // fixed text only, value is a parameter
                " ORDER BY CreatedOn DESC, RequestId DESC";
            return Query(sql, cmd =>
            {
                AddParam(cmd, "@FarmerId", SqlDbType.Int, farmerId);
                if (top.HasValue) AddParam(cmd, "@Top", SqlDbType.Int, top.Value);
                if (!string.IsNullOrEmpty(statusFilter)) AddParam(cmd, "@Status", SqlDbType.VarChar, statusFilter, 20);
            }, MapRequest);
        }

        public void UpdateRequestNotificationFlags(int requestId, bool supplierNotified, bool officerNotified)
        {
            NonQuery("UPDATE dbo.SupplyRequests SET SupplierNotified=@S, OfficerNotified=@O, UpdatedOn=SYSUTCDATETIME() WHERE RequestId=@Id",
                cmd =>
                {
                    AddParam(cmd, "@S", SqlDbType.Bit, supplierNotified);
                    AddParam(cmd, "@O", SqlDbType.Bit, officerNotified);
                    AddParam(cmd, "@Id", SqlDbType.Int, requestId);
                });
        }

        public bool TryChangeRequestStatus(int requestId, string expectedStatus, string newStatus, int? officerId, string officerRemarks)
        {
            const string sql =
                "UPDATE dbo.SupplyRequests SET [Status]=@New, OfficerId=COALESCE(@Officer, OfficerId), " +
                "OfficerRemarks=COALESCE(@Remarks, OfficerRemarks), UpdatedOn=SYSUTCDATETIME() " +
                "WHERE RequestId=@Id AND [Status]=@Expected";
            int rows = NonQuery(sql, cmd =>
            {
                AddParam(cmd, "@New", SqlDbType.VarChar, newStatus, 20);
                AddParam(cmd, "@Expected", SqlDbType.VarChar, expectedStatus, 20);
                AddParam(cmd, "@Officer", SqlDbType.Int, officerId);
                AddParam(cmd, "@Remarks", SqlDbType.NVarChar, officerRemarks, 300);
                AddParam(cmd, "@Id", SqlDbType.Int, requestId);
            });
            return rows == 1;
        }

        // =====================================================================
        // Orders
        // =====================================================================
        private const string OrderSelect =
            "SELECT o.OrderId, o.RequestId, o.FarmerId, r.ItemName, o.SupplierId, o.SupplierName, o.QuotationId, o.Quantity, " +
            "o.UnitPrice, o.TotalPrice, o.EstimatedDeliveryDate, o.OrderStatus, o.SupplierNotified, o.OfficerNotified, o.ConfirmedOn " +
            "FROM dbo.ConfirmedOrders o INNER JOIN dbo.SupplyRequests r ON r.RequestId = o.RequestId ";

        private static ConfirmedOrderDto MapOrder(SqlDataReader r)
        {
            return new ConfirmedOrderDto
            {
                OrderId = r.GetInt32(r.GetOrdinal("OrderId")),
                RequestId = r.GetInt32(r.GetOrdinal("RequestId")),
                FarmerId = r.GetInt32(r.GetOrdinal("FarmerId")),
                ItemName = Str(r, "ItemName"),
                SupplierId = r.GetInt32(r.GetOrdinal("SupplierId")),
                SupplierName = Str(r, "SupplierName"),
                QuotationId = r.GetInt32(r.GetOrdinal("QuotationId")),
                Quantity = r.GetInt32(r.GetOrdinal("Quantity")),
                UnitPrice = r.GetDecimal(r.GetOrdinal("UnitPrice")),
                TotalPrice = r.GetDecimal(r.GetOrdinal("TotalPrice")),
                EstimatedDeliveryDate = r.GetDateTime(r.GetOrdinal("EstimatedDeliveryDate")),
                OrderStatus = Str(r, "OrderStatus"),
                SupplierNotified = r.GetBoolean(r.GetOrdinal("SupplierNotified")),
                OfficerNotified = r.GetBoolean(r.GetOrdinal("OfficerNotified")),
                ConfirmedOn = r.GetDateTime(r.GetOrdinal("ConfirmedOn"))
            };
        }

        public int CreateConfirmedOrder(ConfirmedOrderDto order)
        {
            using (SqlConnection conn = _db.CreateOpenConnection())
            using (SqlTransaction tx = conn.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    // 1) Guarded state change: only an Approved request may become Confirmed.
                    using (SqlCommand upd = NewCommand(conn,
                        "UPDATE dbo.SupplyRequests SET [Status]='Confirmed', UpdatedOn=SYSUTCDATETIME() " +
                        "WHERE RequestId=@Id AND FarmerId=@FarmerId AND [Status]='Approved'", tx))
                    {
                        AddParam(upd, "@Id", SqlDbType.Int, order.RequestId);
                        AddParam(upd, "@FarmerId", SqlDbType.Int, order.FarmerId);
                        if (upd.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("The supply request is no longer in the Approved state.");
                    }

                    // 2) Insert the order.
                    int orderId;
                    using (SqlCommand ins = NewCommand(conn,
                        "INSERT INTO dbo.ConfirmedOrders (RequestId, FarmerId, SupplierId, SupplierName, QuotationId, Quantity, " +
                        "UnitPrice, TotalPrice, EstimatedDeliveryDate, OrderStatus) " +
                        "VALUES (@RequestId, @FarmerId, @SupplierId, @SupplierName, @QuotationId, @Qty, @UnitPrice, @Total, @Eta, 'Confirmed'); " +
                        "SELECT CAST(SCOPE_IDENTITY() AS INT);", tx))
                    {
                        AddParam(ins, "@RequestId", SqlDbType.Int, order.RequestId);
                        AddParam(ins, "@FarmerId", SqlDbType.Int, order.FarmerId);
                        AddParam(ins, "@SupplierId", SqlDbType.Int, order.SupplierId);
                        AddParam(ins, "@SupplierName", SqlDbType.NVarChar, order.SupplierName, 100);
                        AddParam(ins, "@QuotationId", SqlDbType.Int, order.QuotationId);
                        AddParam(ins, "@Qty", SqlDbType.Int, order.Quantity);
                        AddDecimal(ins, "@UnitPrice", order.UnitPrice, 12, 2);
                        AddDecimal(ins, "@Total", order.TotalPrice, 14, 2);
                        AddParam(ins, "@Eta", SqlDbType.Date, order.EstimatedDeliveryDate.Date);
                        orderId = (int)ins.ExecuteScalar();
                    }

                    tx.Commit();
                    return orderId;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public ConfirmedOrderDto GetConfirmedOrderById(int orderId)
        {
            return QuerySingle(OrderSelect + "WHERE o.OrderId = @Id",
                cmd => AddParam(cmd, "@Id", SqlDbType.Int, orderId), MapOrder);
        }

        public List<ConfirmedOrderDto> GetConfirmedOrdersByFarmer(int farmerId)
        {
            return Query(OrderSelect + "WHERE o.FarmerId = @FarmerId ORDER BY o.ConfirmedOn DESC, o.OrderId DESC",
                cmd => AddParam(cmd, "@FarmerId", SqlDbType.Int, farmerId), MapOrder);
        }

        public void UpdateOrderNotificationFlags(int orderId, bool supplierNotified, bool officerNotified)
        {
            NonQuery("UPDATE dbo.ConfirmedOrders SET SupplierNotified=@S, OfficerNotified=@O WHERE OrderId=@Id",
                cmd =>
                {
                    AddParam(cmd, "@S", SqlDbType.Bit, supplierNotified);
                    AddParam(cmd, "@O", SqlDbType.Bit, officerNotified);
                    AddParam(cmd, "@Id", SqlDbType.Int, orderId);
                });
        }

        // =====================================================================
        // Crop listings
        // =====================================================================
        private const string ListingSelect =
            "SELECT c.ListingId, c.FarmerId, f.FullName, f.District, c.CropName, c.Quantity, c.Unit, c.UnitPrice, " +
            "c.AvailabilityDate, c.Notes, c.ContactInfo, c.[Status], c.CreatedOn, c.UpdatedOn " +
            "FROM dbo.CropListings c INNER JOIN dbo.Farmers f ON f.FarmerId = c.FarmerId ";

        private static CropListingDto MapListing(SqlDataReader r)
        {
            return new CropListingDto
            {
                ListingId = r.GetInt32(r.GetOrdinal("ListingId")),
                FarmerId = r.GetInt32(r.GetOrdinal("FarmerId")),
                FarmerName = Str(r, "FullName"),
                District = Str(r, "District"),
                CropName = Str(r, "CropName"),
                Quantity = r.GetDecimal(r.GetOrdinal("Quantity")),
                Unit = Str(r, "Unit"),
                UnitPrice = r.GetDecimal(r.GetOrdinal("UnitPrice")),
                AvailabilityDate = r.GetDateTime(r.GetOrdinal("AvailabilityDate")),
                Notes = Str(r, "Notes"),
                ContactInfo = Str(r, "ContactInfo"),
                Status = Str(r, "Status"),
                CreatedOn = r.GetDateTime(r.GetOrdinal("CreatedOn")),
                UpdatedOn = r.GetDateTime(r.GetOrdinal("UpdatedOn"))
            };
        }

        private static void BindListing(SqlCommand cmd, CropListingInput i)
        {
            AddParam(cmd, "@CropName", SqlDbType.NVarChar, i.CropName, 100);
            AddDecimal(cmd, "@Qty", i.Quantity, 10, 2);
            AddParam(cmd, "@Unit", SqlDbType.NVarChar, i.Unit, 20);
            AddDecimal(cmd, "@Price", i.UnitPrice, 12, 2);
            AddParam(cmd, "@Avail", SqlDbType.Date, i.AvailabilityDate.Date);
            AddParam(cmd, "@Notes", SqlDbType.NVarChar, i.Notes, 500);
            AddParam(cmd, "@Contact", SqlDbType.NVarChar, i.ContactInfo, 150);
        }

        public int InsertCropListing(CropListingInput input)
        {
            const string sql =
                "INSERT INTO dbo.CropListings (FarmerId, CropName, Quantity, Unit, UnitPrice, AvailabilityDate, Notes, ContactInfo) " +
                "VALUES (@FarmerId, @CropName, @Qty, @Unit, @Price, @Avail, @Notes, @Contact); " +
                "SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using (SqlConnection conn = _db.CreateOpenConnection())
            using (SqlCommand cmd = NewCommand(conn, sql))
            {
                AddParam(cmd, "@FarmerId", SqlDbType.Int, input.FarmerId);
                BindListing(cmd, input);
                return (int)cmd.ExecuteScalar();
            }
        }

        public bool UpdateCropListing(CropListingInput input)
        {
            const string sql =
                "UPDATE dbo.CropListings SET CropName=@CropName, Quantity=@Qty, Unit=@Unit, UnitPrice=@Price, " +
                "AvailabilityDate=@Avail, Notes=@Notes, ContactInfo=@Contact, UpdatedOn=SYSUTCDATETIME() " +
                "WHERE ListingId=@Id AND FarmerId=@FarmerId";
            return NonQuery(sql, cmd =>
            {
                AddParam(cmd, "@Id", SqlDbType.Int, input.ListingId);
                AddParam(cmd, "@FarmerId", SqlDbType.Int, input.FarmerId);
                BindListing(cmd, input);
            }) == 1;
        }

        public CropListingDto GetCropListingById(int listingId)
        {
            return QuerySingle(ListingSelect + "WHERE c.ListingId = @Id",
                cmd => AddParam(cmd, "@Id", SqlDbType.Int, listingId), MapListing);
        }

        public List<CropListingDto> GetCropListingsByFarmer(int farmerId, int? top)
        {
            string sql = ListingSelect.Replace("SELECT c.ListingId", "SELECT " + (top.HasValue ? "TOP (@Top) " : "") + "c.ListingId") +
                         "WHERE c.FarmerId = @FarmerId ORDER BY c.CreatedOn DESC, c.ListingId DESC";
            return Query(sql, cmd =>
            {
                AddParam(cmd, "@FarmerId", SqlDbType.Int, farmerId);
                if (top.HasValue) AddParam(cmd, "@Top", SqlDbType.Int, top.Value);
            }, MapListing);
        }

        public List<CropListingDto> SearchActiveListings(string searchText)
        {
            const string sql = "WHERE c.[Status] = 'Active' AND (@Search IS NULL OR c.CropName LIKE @Pattern OR f.District LIKE @Pattern) " +
                               "ORDER BY c.AvailabilityDate, c.CropName";
            string term = string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim();
            return Query(ListingSelect + sql, cmd =>
            {
                AddParam(cmd, "@Search", SqlDbType.NVarChar, term, 100);
                // Escape LIKE wildcards typed by the user, then wrap with %...%.
                string pattern = term == null ? null : "%" + term.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
                AddParam(cmd, "@Pattern", SqlDbType.NVarChar, pattern, 120);
            }, MapListing);
        }

        public bool SetListingStatus(int listingId, int farmerId, string status)
        {
            return NonQuery("UPDATE dbo.CropListings SET [Status]=@S, UpdatedOn=SYSUTCDATETIME() WHERE ListingId=@Id AND FarmerId=@FarmerId",
                cmd =>
                {
                    AddParam(cmd, "@S", SqlDbType.VarChar, status, 20);
                    AddParam(cmd, "@Id", SqlDbType.Int, listingId);
                    AddParam(cmd, "@FarmerId", SqlDbType.Int, farmerId);
                }) == 1;
        }

        public bool DeleteCropListing(int listingId, int farmerId)
        {
            return NonQuery("DELETE FROM dbo.CropListings WHERE ListingId=@Id AND FarmerId=@FarmerId",
                cmd =>
                {
                    AddParam(cmd, "@Id", SqlDbType.Int, listingId);
                    AddParam(cmd, "@FarmerId", SqlDbType.Int, farmerId);
                }) == 1;
        }
    }
}
