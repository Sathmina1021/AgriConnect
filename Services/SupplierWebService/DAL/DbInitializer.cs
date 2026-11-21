using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SupplierWebService.DAL
{
    /// <summary>
    /// Creates the AgriSupplierDb database, tables and demo data on first run,
    /// so the service works without running any SQL by hand.
    /// </summary>
    public static class DbInitializer
    {
        private static readonly object Gate = new object();
        private static bool _done;

        public static string LastError { get; private set; }

        public static bool EnsureCreated()
        {
            lock (Gate)
            {
                if (_done) return true;
                try
                {
                    var b = new SqlConnectionStringBuilder(Db.ConnectionString);
                    string dbName = b.InitialCatalog;
                    if (!Regex.IsMatch(dbName ?? "", "^[A-Za-z0-9_]+$"))
                        throw new InvalidOperationException("Database name in the connection string must contain only letters, digits and _");

                    // 1) create the database (connect to master first)
                    b.InitialCatalog = "master";
                    using (var con = new SqlConnection(b.ConnectionString))
                    {
                        con.Open();
                        using (var cmd = new SqlCommand("IF DB_ID(@n) IS NULL EXEC('CREATE DATABASE [' + @n + ']')", con))
                        {
                            cmd.Parameters.AddWithValue("@n", dbName);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // 2) run the embedded schema script (batches separated by GO)
                    string script = ReadSchema();
                    var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                    using (var con = Db.Open())
                    {
                        foreach (var batch in batches.Where(x => !string.IsNullOrWhiteSpace(x)))
                        {
                            using (var cmd = new SqlCommand(batch, con)) cmd.ExecuteNonQuery();
                        }
                    }
                    LastError = null;
                    _done = true;
                    return true;
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                    return false;
                }
            }
        }

        private static string ReadSchema()
        {
            var asm = Assembly.GetExecutingAssembly();
            string name = asm.GetManifestResourceNames().First(n => n.EndsWith("Schema.sql", StringComparison.OrdinalIgnoreCase));
            using (var s = asm.GetManifestResourceStream(name))
            using (var r = new StreamReader(s))
                return r.ReadToEnd();
        }
    }
}
