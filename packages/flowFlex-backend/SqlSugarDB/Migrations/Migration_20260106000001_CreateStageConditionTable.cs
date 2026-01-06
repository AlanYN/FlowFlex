using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations;

/// <summary>
/// Migration to create ff_stage_condition table for Stage Condition feature
/// </summary>
public static class Migration_20260106000001_CreateStageConditionTable
{
    public static void Up(ISqlSugarClient db)
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS ff_stage_condition (
                id BIGINT PRIMARY KEY,
                stage_id BIGINT NOT NULL,
                workflow_id BIGINT NOT NULL,
                name VARCHAR(100) NOT NULL,
                description VARCHAR(500),
                rules_json JSONB NOT NULL,
                actions_json JSONB NOT NULL,
                fallback_stage_id BIGINT,
                is_active BOOLEAN DEFAULT TRUE,
                status VARCHAR(20) DEFAULT 'Valid',
                tenant_id VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                is_valid BOOLEAN DEFAULT TRUE,
                create_date TIMESTAMPTZ DEFAULT NOW(),
                modify_date TIMESTAMPTZ DEFAULT NOW(),
                create_by VARCHAR(50) DEFAULT 'SYSTEM',
                modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                create_user_id BIGINT DEFAULT 0,
                modify_user_id BIGINT DEFAULT 0,
                
                CONSTRAINT fk_stage_condition_stage FOREIGN KEY (stage_id) REFERENCES ff_stage(id),
                CONSTRAINT fk_stage_condition_workflow FOREIGN KEY (workflow_id) REFERENCES ff_workflow(id),
                CONSTRAINT fk_stage_condition_fallback FOREIGN KEY (fallback_stage_id) REFERENCES ff_stage(id),
                CONSTRAINT uq_stage_condition UNIQUE (stage_id, tenant_id)
            );

            CREATE INDEX IF NOT EXISTS idx_stage_condition_stage ON ff_stage_condition(stage_id);
            CREATE INDEX IF NOT EXISTS idx_stage_condition_workflow ON ff_stage_condition(workflow_id);
            CREATE INDEX IF NOT EXISTS idx_stage_condition_tenant ON ff_stage_condition(tenant_id);

            COMMENT ON TABLE ff_stage_condition IS 'Stage condition rules and actions for workflow automation';
        ";

        db.Ado.ExecuteCommand(sql);

        Console.WriteLine("[Migration] Created ff_stage_condition table");
    }

    public static void Down(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_stage_condition");
        Console.WriteLine("[Migration] Dropped ff_stage_condition table");
    }
}
