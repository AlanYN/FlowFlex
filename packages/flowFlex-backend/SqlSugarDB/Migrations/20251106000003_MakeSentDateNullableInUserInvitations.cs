using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Make sent_date nullable in user invitations table
    /// Version: 2.0.0
    /// Created: 2025-11-06
    /// Description: Change sent_date to nullable so it can be empty until invitation is actually sent
    /// </summary>
    public class MakeSentDateNullableInUserInvitations_20251106000003
    {
        /// <summary>
        /// Execute migration - make sent_date nullable
        /// </summary>
        public static void Up(ISqlSugarClient db)
        {
            // Remove default value and make sent_date nullable
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_user_invitations 
                ALTER COLUMN sent_date DROP DEFAULT,
                ALTER COLUMN sent_date DROP NOT NULL;
            ");

            // Update existing records with default timestamp to NULL if they haven't been sent
            // (Optional: only if you want to clear existing auto-generated timestamps)
            db.Ado.ExecuteCommand(@"
                UPDATE ff_user_invitations 
                SET sent_date = NULL 
                WHERE send_count = 0 
                  AND status = 'Pending'
                  AND notes LIKE '%Auto-created default invitation%';
            ");
        }

        /// <summary>
        /// Rollback migration - make sent_date NOT NULL with default
        /// </summary>
        public static void Down(ISqlSugarClient db)
        {
            // Set default value for NULL records before making it NOT NULL
            db.Ado.ExecuteCommand(@"
                UPDATE ff_user_invitations 
                SET sent_date = CURRENT_TIMESTAMP 
                WHERE sent_date IS NULL;
            ");

            // Make sent_date NOT NULL and add default
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_user_invitations 
                ALTER COLUMN sent_date SET DEFAULT CURRENT_TIMESTAMP,
                ALTER COLUMN sent_date SET NOT NULL;
            ");
        }
    }
}

