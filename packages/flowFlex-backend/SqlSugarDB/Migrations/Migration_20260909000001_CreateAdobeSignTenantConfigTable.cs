using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Create ff_adobe_sign_tenant_config table.
    /// Stores the AppCode → Adobe Sign account key mapping that controls which
    /// Adobe Sign account (Item / Unisco) is used for a given AppCode.
    /// This is a system-level config table — no app_code/tenant_id columns.
    /// </summary>
    public static class Migration_20260909000001_CreateAdobeSignTenantConfigTable
    {
        public static void Up(ISqlSugarClient db)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ff_adobe_sign_tenant_config (
                    id                   BIGINT          NOT NULL PRIMARY KEY,
                    configured_app_code  VARCHAR(100)    NOT NULL,
                    account_key          VARCHAR(50)     NOT NULL,
                    is_valid             BOOLEAN         NOT NULL DEFAULT TRUE,
                    create_date          TIMESTAMPTZ,
                    modify_date          TIMESTAMPTZ,
                    create_by            VARCHAR(200),
                    modify_by            VARCHAR(200),
                    create_user_id       BIGINT          NOT NULL DEFAULT 0,
                    modify_user_id       BIGINT          NOT NULL DEFAULT 0
                );

                CREATE UNIQUE INDEX IF NOT EXISTS idx_adobe_sign_tenant_config_app_code
                    ON ff_adobe_sign_tenant_config(configured_app_code)
                    WHERE is_valid = TRUE;

                COMMENT ON TABLE ff_adobe_sign_tenant_config
                    IS 'Maps AppCode to Adobe Sign account key (Item/Unisco). No record = use DefaultAccount from config.';
                COMMENT ON COLUMN ff_adobe_sign_tenant_config.configured_app_code
                    IS 'The X-App-Code header value being configured (e.g. item-wfe, unisco-wfe)';
                COMMENT ON COLUMN ff_adobe_sign_tenant_config.account_key
                    IS 'Adobe Sign account key — must match a key in appsettings.json AdobeSign:Accounts';
            ";

            db.Ado.ExecuteCommand(sql);
        }

        public static void Down(ISqlSugarClient db)
        {
            db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_adobe_sign_tenant_config;");
        }
    }
}
