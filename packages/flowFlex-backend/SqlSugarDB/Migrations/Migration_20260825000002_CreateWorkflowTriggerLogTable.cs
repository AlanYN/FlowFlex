using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations;

public static class Migration_20260825000002_CreateWorkflowTriggerLogTable
{
    public static void Up(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ff_workflow_trigger_log (
                id                    BIGINT          PRIMARY KEY,
                connection_id         BIGINT          NOT NULL,
                source_workflow_id    BIGINT          NOT NULL,
                target_workflow_id    BIGINT          NOT NULL,
                source_onboarding_id  BIGINT          NOT NULL,
                target_onboarding_id  BIGINT,
                status                VARCHAR(20)     NOT NULL DEFAULT 'Pending',
                reason                VARCHAR(1000)   NOT NULL DEFAULT '',
                completion_type       VARCHAR(30)     NOT NULL DEFAULT 'Completed',
                conditions_snapshot   JSONB           NOT NULL DEFAULT '[]',
                mappings_snapshot     JSONB           NOT NULL DEFAULT '[]',
                tenant_id             VARCHAR(32)     NOT NULL DEFAULT 'DEFAULT',
                app_code              VARCHAR(32)     NOT NULL DEFAULT 'DEFAULT',
                is_valid              BOOLEAN         NOT NULL DEFAULT TRUE,
                create_date           TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                modify_date           TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                create_by             VARCHAR(50)     NOT NULL DEFAULT 'SYSTEM',
                modify_by             VARCHAR(50)     NOT NULL DEFAULT 'SYSTEM',
                create_user_id        BIGINT          NOT NULL DEFAULT 0,
                modify_user_id        BIGINT          NOT NULL DEFAULT 0
            );

            CREATE INDEX IF NOT EXISTS idx_trig_log_source_ob   ON ff_workflow_trigger_log(source_onboarding_id);
            CREATE INDEX IF NOT EXISTS idx_trig_log_connection   ON ff_workflow_trigger_log(connection_id);
            CREATE INDEX IF NOT EXISTS idx_trig_log_tenant       ON ff_workflow_trigger_log(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_trig_log_status       ON ff_workflow_trigger_log(status);
        ");
        Console.WriteLine("[Migration] Created ff_workflow_trigger_log table");
    }

    public static void Down(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_workflow_trigger_log;");
        Console.WriteLine("[Migration] Dropped ff_workflow_trigger_log table");
    }
}
