using System;
using System.Configuration;
using System.Data.SqlClient;

namespace Farmer.Service.Data
{
    /// <summary>
    /// Creates open connections to AgriConnect_Farmer_SOC. The connection string comes from
    /// App.config (name "FarmerDb"), so nothing machine-specific is hard-coded in source.
    /// </summary>
    public sealed class DbConnectionFactory
    {
        private readonly string _connectionString;

        public DbConnectionFactory()
            : this(ReadConnectionString())
        {
        }

        public DbConnectionFactory(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string is empty.", "connectionString");
            _connectionString = connectionString;
        }

        public SqlConnection CreateOpenConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        private static string ReadConnectionString()
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["FarmerDb"];
            if (settings == null)
                throw new ConfigurationErrorsException("Connection string 'FarmerDb' is missing in App.config.");
            return settings.ConnectionString;
        }
    }
}
