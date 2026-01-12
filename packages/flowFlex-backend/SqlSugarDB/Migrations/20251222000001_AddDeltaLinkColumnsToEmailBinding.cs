using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Migration to add Delta Link columns to email_bindings table
    /// These columns store Microsoft Graph Delta Query links for efficient incremental sync
    /// </summary>
    public class _20251222000001_AddDeltaLinkColumnsToEmailBinding
    {
        /// <summary>
        /// Apply migration - add delta link columns
        /// </summary>
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[Migration] Adding Delta Link columns to ff_email_bindings...");

                // Add delta_link_inbox column
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_email_bindings 
                    ADD COLUMN IF NOT EXISTS delta_link_inbox TEXT;
                ");

                // Add delta_link_sent column
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_email_bindings 
                    ADD COLUMN IF NOT EXISTS delta_link_sent TEXT;
                ");

                // Add delta_link_deleted column
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_email_bindings 
                    ADD COLUMN IF NOT EXISTS delta_link_deleted TEXT;
                ");

                Console.WriteLine("[Migration] Delta Link columns added successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error adding Delta Link columns: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Rollback migration - remove delta link columns
        /// </summary>
        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[Migration] Removing Delta Link columns from ff_email_bindings...");

                db.Ado.ExecuteCommand("ALTER TABLE ff_email_bindings DROP COLUMN IF EXISTS delta_link_inbox;");
                db.Ado.ExecuteCommand("ALTER TABLE ff_email_bindings DROP COLUMN IF EXISTS delta_link_sent;");
                db.Ado.ExecuteCommand("ALTER TABLE ff_email_bindings DROP COLUMN IF EXISTS delta_link_deleted;");

                Console.WriteLine("[Migration] Delta Link columns removed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error removing Delta Link columns: {ex.Message}");
                throw;
            }
        }
    }
}
