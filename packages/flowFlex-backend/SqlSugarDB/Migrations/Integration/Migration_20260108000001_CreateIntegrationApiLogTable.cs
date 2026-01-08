using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations.Integration;

/// <summary>
/// Migration to create ff_integration_api_log table for tracking external API calls
/// </summary>
public static class Migration_20260108000001_CreateIntegrationApiLogTable
{
    public static void Up(ISqlSugarClient db)
    {
        Console.WriteLine("Starting migration: Create ff_integration_api_log table");

        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ff_integration_api_log (
                id BIGINT NOT NULL PRIMARY KEY,
                tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                integration_id BIGINT NOT NULL,
                system_id VARCHAR(100),
                endpoint VARCHAR(200) NOT NULL,
                http_method VARCHAR(10) NOT NULL DEFAULT 'GET',
                started_at TIMESTAMPTZ NOT NULL,
                completed_at TIMESTAMPTZ,
                duration_ms BIGINT,
                status_code INT NOT NULL DEFAULT 200,
                is_success BOOLEAN NOT NULL DEFAULT true,
                error_message VARCHAR(2000),
                request_params JSONB,
                caller_user_id BIGINT,
                caller_ip VARCHAR(50),
                create_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                modify_date TIMESTAMPTZ,
                create_by VARCHAR(100),
                modify_by VARCHAR(100),
                create_user_id BIGINT DEFAULT 0,
                modify_user_id BIGINT DEFAULT 0,
                is_valid BOOLEAN NOT NULL DEFAULT true
            );

            -- Create indexes for common queries
            CREATE INDEX IF NOT EXISTS idx_integration_api_log_tenant_id ON ff_integration_api_log(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_integration_api_log_integration_id ON ff_integration_api_log(integration_id);
            CREATE INDEX IF NOT EXISTS idx_integration_api_log_create_date ON ff_integration_api_log(create_date);
            CREATE INDEX IF NOT EXISTS idx_integration_api_log_integration_date ON ff_integration_api_log(integration_id, create_date);

            -- Add comment
            COMMENT ON TABLE ff_integration_api_log IS 'Integration API call logs for statistics';
        ");

        Console.WriteLine("✓ Created ff_integration_api_log table");
    }

    public static void Down(ISqlSugarClient db)
    {
        Console.WriteLine("Rolling back migration: Drop ff_integration_api_log table");

        db.Ado.ExecuteCommand(@"
            DROP TABLE IF EXISTS ff_integration_api_log;
        ");

        Console.WriteLine("✓ Dropped ff_integration_api_log table");
    }
}
