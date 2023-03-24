using Dinosaur.SqlServerToMySql.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace Dinosaur.SqlServerToMySql
{
    internal static class Settings
    {
        private static IConfiguration _configuration;
        private static string _sourceConnectionString;
        private static string _mySqlConnectionString;
        private static DbmsType _dbms;

        public static void Initialize()
        {
            if (_configuration != null)
                return;

            _configuration = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json")
                        .Build();

            _sourceConnectionString = _configuration.GetSection("Source:ConnectionString").Value;

            string dbms = _configuration.GetSection("Source:DBMS").Value;
            if (Enum.TryParse(dbms, true, out DbmsType type))
            {
                _dbms = type;
            }

            if (_dbms == DbmsType.SqlServer)
            {
                var sqlConn = new SqlConnection(_sourceConnectionString);
                try
                {
                    sqlConn.Open();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(_sourceConnectionString);
                    throw;
                }
                finally
                {
                    sqlConn.Dispose();
                }
            }

            _mySqlConnectionString = _configuration.GetSection("MySql:ConnectionString").Value;

            var mysqlConn = new MySqlConnection(_mySqlConnectionString);
            try
            {
                mysqlConn.Open();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(_mySqlConnectionString);
                throw;
            }
            finally
            {
                mysqlConn.Dispose();
            }
        }

        public static IConfiguration Configuration => _configuration;

        public static string SourceConnectionString => _sourceConnectionString;

        public static DbmsType Dbms => _dbms;

        public static string MySqlConnectionString => _mySqlConnectionString;

    }
}
