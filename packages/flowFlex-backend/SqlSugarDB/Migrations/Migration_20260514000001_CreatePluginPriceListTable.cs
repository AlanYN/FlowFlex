using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations;

public static class Migration_20260514000001_CreatePluginPriceListTable
{
    public static void Up(ISqlSugarClient db)
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS ff_plugin_price_lists (
                id BIGINT PRIMARY KEY,
                case_code VARCHAR(50) NOT NULL,
                customer_code VARCHAR(50),
                customer_name VARCHAR(200),
                price_list_type VARCHAR(50) DEFAULT 'Customer Specific',
                start_date VARCHAR(20),
                end_date VARCHAR(20),
                data JSONB NOT NULL DEFAULT '{}',
                status VARCHAR(20) DEFAULT 'draft',
                tenant_id VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                is_valid BOOLEAN DEFAULT TRUE,
                create_date TIMESTAMPTZ DEFAULT NOW(),
                modify_date TIMESTAMPTZ DEFAULT NOW(),
                create_by VARCHAR(50) DEFAULT 'SYSTEM',
                modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                create_user_id BIGINT DEFAULT 0,
                modify_user_id BIGINT DEFAULT 0
            );

            CREATE UNIQUE INDEX IF NOT EXISTS idx_plugin_price_list_case_code
                ON ff_plugin_price_lists(tenant_id, app_code, case_code)
                WHERE is_valid = TRUE;

            COMMENT ON TABLE ff_plugin_price_lists IS 'Plugin Price List - stores customer pricing data per onboarding case';
        ";

        db.Ado.ExecuteCommand(sql);
        Console.WriteLine("[Migration] Created ff_plugin_price_lists table");
    }

    public static void Down(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_plugin_price_lists");
        Console.WriteLine("[Migration] Dropped ff_plugin_price_lists table");
    }
}
