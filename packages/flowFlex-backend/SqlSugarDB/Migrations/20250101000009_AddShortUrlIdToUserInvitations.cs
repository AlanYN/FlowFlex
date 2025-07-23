using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Add short URL ID to user invitations table migration
    /// Version: 2.0.1
    /// Created: 2025-01-01
    /// Description: Add short_url_id column for compact invitation links
    /// </summary>
    public class AddShortUrlIdToUserInvitations_20250101000009
    {
        /// <summary>
        /// Execute migration - add short_url_id column
        /// </summary>
        public static void Up(ISqlSugarClient db)
        {
            // Add short_url_id column to user invitations table
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_user_invitations 
                ADD COLUMN short_url_id VARCHAR(32) NULL;
                
                CREATE INDEX IF NOT EXISTS idx_user_invitations_short_url_id 
                ON ff_user_invitations(short_url_id);
            ");
        }

        /// <summary>
        /// Rollback migration - remove short_url_id column
        /// </summary>
        public static void Down(ISqlSugarClient db)
        {
            // Remove index and column
            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_user_invitations_short_url_id;
                ALTER TABLE ff_user_invitations DROP COLUMN short_url_id;
            ");
        }
    }
} 