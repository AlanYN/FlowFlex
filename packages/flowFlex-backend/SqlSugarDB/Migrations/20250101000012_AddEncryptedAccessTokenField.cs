using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add encrypted access token field to user invitations table
    /// Version: 2.0.1
    /// Created: 2025-01-01
    /// Description: Add encrypted access token field for secure portal access links
    /// </summary>
    public class AddEncryptedAccessTokenField_20250101000012
    {
        /// <summary>
        /// Execute migration - add encrypted access token field
        /// </summary>
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                // Add encrypted access token field
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_user_invitations 
                    ADD COLUMN encrypted_access_token VARCHAR(500);
                ");

                // Create index for better performance
                db.Ado.ExecuteCommand(@"
                    CREATE INDEX IF NOT EXISTS idx_user_invitations_encrypted_token 
                    ON ff_user_invitations(encrypted_access_token);
                ");

                Console.WriteLine("✓ Added encrypted_access_token field to ff_user_invitations table");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error adding encrypted_access_token field: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Rollback migration - remove encrypted access token field
        /// </summary>
        public static void Down(ISqlSugarClient db)
        {
            try
            {
                // Drop index first
                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_user_invitations_encrypted_token;
                ");

                // Remove encrypted access token field
                db.Ado.ExecuteCommand(@"
                    ALTER TABLE ff_user_invitations 
                    DROP COLUMN IF EXISTS encrypted_access_token;
                ");

                Console.WriteLine("✓ Removed encrypted_access_token field from ff_user_invitations table");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error removing encrypted_access_token field: {ex.Message}");
                throw;
            }
        }
    }
}