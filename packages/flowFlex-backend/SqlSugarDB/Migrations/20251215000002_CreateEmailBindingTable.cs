using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations;

/// <summary>
/// Migration: Create Email Binding Table for Outlook Integration
/// </summary>
public class Migration_20251215000002_CreateEmailBindingTable
{
    public void Up(ISqlSugarClient db)
    {
        Console.WriteLine("[Migration] Creating Email Binding table...");

        // Create ff_email_bindings table
        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS ff_email_bindings (
                id BIGINT PRIMARY KEY,
                tenant_id VARCHAR(50) NOT NULL,
                app_code VARCHAR(50),
                is_valid BOOLEAN NOT NULL DEFAULT TRUE,
                
                -- User binding info
                user_id BIGINT NOT NULL,
                email VARCHAR(255) NOT NULL DEFAULT '',
                provider VARCHAR(50) NOT NULL DEFAULT 'Outlook',
                
                -- OAuth tokens
                access_token TEXT,
                refresh_token TEXT,
                token_expire_time TIMESTAMPTZ,
                
                -- Sync settings
                last_sync_time TIMESTAMPTZ,
                sync_status VARCHAR(20) NOT NULL DEFAULT 'Active',
                last_sync_error VARCHAR(500),
                auto_sync_enabled BOOLEAN NOT NULL DEFAULT TRUE,
                sync_interval_minutes INT NOT NULL DEFAULT 15,
                
                -- Audit fields
                create_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                modify_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                create_by VARCHAR(100),
                modify_by VARCHAR(100),
                create_user_id BIGINT,
                modify_user_id BIGINT
            );
        ";
        db.Ado.ExecuteCommand(createTableSql);
        Console.WriteLine("[Migration] Created ff_email_bindings table");

        // Create indexes
        var createIndexesSql = @"
            CREATE INDEX IF NOT EXISTS idx_email_bindings_tenant_id ON ff_email_bindings(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_email_bindings_user_id ON ff_email_bindings(user_id);
            CREATE INDEX IF NOT EXISTS idx_email_bindings_user_provider ON ff_email_bindings(user_id, provider);
            CREATE INDEX IF NOT EXISTS idx_email_bindings_sync_status ON ff_email_bindings(sync_status) WHERE is_valid = TRUE;
        ";
        db.Ado.ExecuteCommand(createIndexesSql);
        Console.WriteLine("[Migration] Created indexes for ff_email_bindings");

        // Add comments
        var addCommentsSql = @"
            COMMENT ON TABLE ff_email_bindings IS 'Email account bindings for Outlook integration';
            COMMENT ON COLUMN ff_email_bindings.user_id IS 'User ID who owns this binding';
            COMMENT ON COLUMN ff_email_bindings.email IS 'Bound email address';
            COMMENT ON COLUMN ff_email_bindings.provider IS 'Email provider (Outlook, Gmail, etc.)';
            COMMENT ON COLUMN ff_email_bindings.access_token IS 'OAuth access token';
            COMMENT ON COLUMN ff_email_bindings.refresh_token IS 'OAuth refresh token';
            COMMENT ON COLUMN ff_email_bindings.token_expire_time IS 'Token expiration time';
            COMMENT ON COLUMN ff_email_bindings.last_sync_time IS 'Last successful sync time';
            COMMENT ON COLUMN ff_email_bindings.sync_status IS 'Sync status: Active, Error, Disabled';
            COMMENT ON COLUMN ff_email_bindings.auto_sync_enabled IS 'Whether auto sync is enabled';
            COMMENT ON COLUMN ff_email_bindings.sync_interval_minutes IS 'Sync interval in minutes';
        ";
        db.Ado.ExecuteCommand(addCommentsSql);

        Console.WriteLine("[Migration] Email Binding table created successfully");
    }

    public static void Down(ISqlSugarClient db)
    {
        Console.WriteLine("[Migration] Dropping Email Binding table...");
        db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_email_bindings CASCADE;");
        Console.WriteLine("[Migration] Email Binding table dropped successfully");
    }
}
