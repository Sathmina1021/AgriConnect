using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace SupplierWebService.DAL
{
    /// <summary>Small ADO.NET helper (SqlConnection / SqlCommand / SqlParameter / SqlDataReader) used by every DAL class.</summary>
    internal static class Db
    {
        public static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["SupplierDb"].ConnectionString; }
        }

        public static SqlConnection Open()
        {
            var con = new SqlConnection(ConnectionString);
            con.Open();
            return con;
        }

        public static SqlParameter P(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        public static List<T> Query<T>(string sql, Func<SqlDataReader, T> map, params SqlParameter[] ps)
        {
            var list = new List<T>();
            using (var con = Open())
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddRange(ps);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) list.Add(map(r));
                }
            }
            return list;
        }

        public static T QueryOne<T>(string sql, Func<SqlDataReader, T> map, params SqlParameter[] ps) where T : class
        {
            var list = Query(sql, map, ps);
            return list.Count == 0 ? null : list[0];
        }

        public static object Scalar(string sql, params SqlParameter[] ps)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddRange(ps);
                return cmd.ExecuteScalar();
            }
        }

        public static int Execute(string sql, params SqlParameter[] ps)
        {
            using (var con = Open())
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddRange(ps);
                return cmd.ExecuteNonQuery();
            }
        }
    }
}
