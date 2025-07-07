using FlowFlex.SqlSugarDB.Extensions;
using FlowFlex.SqlSugarDB.Migrations;
using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Tools
{
    /// <summary>
    /// Database management tool
    /// </summary>
    public class DatabaseTool
    {
        private readonly ISqlSugarClient _db;

        public DatabaseTool(string connectionString)
        {
            _db = new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = connectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true
            });
        }

        /// <summary>
        /// Run migration
        /// </summary>
        public void Migrate()
        {
            Console.WriteLine("Starting database migration...");
            _db.EnsureDatabaseCreated();
        }

        /// <summary>
        /// Reset database
        /// </summary>
        public void Reset()
        {
            Console.WriteLine("Starting database reset...");
            _db.ResetDatabase();
            Console.WriteLine("Database reset completed!");
        }

        /// <summary>
        /// Check database connection
        /// </summary>
        public void TestConnection()
        {
            try
            {
                _db.Ado.CheckConnection();
                Console.WriteLine("Database connection successful!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database connection failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Show database information
        /// </summary>
        public void ShowDatabaseInfo()
        {
            try
            {
                var tables = _db.DbMaintenance.GetTableInfoList(false);
                Console.WriteLine($"Database contains {tables.Count} tables:");
                
                foreach (var table in tables)
                {
                    if (table.Name.StartsWith("ff_"))
                    {
                        Console.WriteLine($"  - {table.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get database information: {ex.Message}");
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            _db?.Dispose();
        }
    }
} 
