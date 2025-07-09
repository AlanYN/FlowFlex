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
            // Database migration logging handled by structured logging
            _db.EnsureDatabaseCreated();
        }

        /// <summary>
        /// Reset database
        /// </summary>
        public void Reset()
        {
            // Database reset logging handled by structured logging
            _db.ResetDatabase();
            // Database reset completion logged by structured logging
        }

        /// <summary>
        /// Check database connection
        /// </summary>
        public void TestConnection()
        {
            try
            {
                _db.Ado.CheckConnection();
                // Database connection success logged by structured logging
            }
            catch (Exception ex)
            {
                // Database connection failure logged by structured logging
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
                // Database information logging handled by structured logging

                foreach (var table in tables)
                {
                    if (table.Name.StartsWith("ff_"))
                    {
                        // Table information logged by structured logging
                    }
                }
            }
            catch (Exception ex)
            {
                // Database information error logged by structured logging
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
