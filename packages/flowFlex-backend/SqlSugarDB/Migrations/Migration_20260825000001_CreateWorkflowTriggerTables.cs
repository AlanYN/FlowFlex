using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations;

/// <summary>
/// Creates ff_workflow_trigger_graph and ff_workflow_trigger_connection tables
/// for the Workflow Trigger Graph feature (OW-723).
/// </summary>
public static class Migration_20260825000001_CreateWorkflowTriggerTables
{
    public static void Up(ISqlSugarClient db)
    {
        var sql = @"
            -- Trigger Graph: one per workflow (source/owner)
            CREATE TABLE IF NOT EXISTS ff_workflow_trigger_graph (
                id                  BIGINT          PRIMARY KEY,
                workflow_id         BIGINT          NOT NULL,
                name                VARCHAR(200)    NOT NULL DEFAULT '',
                canvas_layout       JSONB           NOT NULL DEFAULT '{}',
                canvas_workflow_ids JSONB           NOT NULL DEFAULT '[]',
                tenant_id           VARCHAR(32)     NOT NULL DEFAULT 'DEFAULT',
                app_code            VARCHAR(32)     NOT NULL DEFAULT 'DEFAULT',
                is_valid            BOOLEAN         NOT NULL DEFAULT TRUE,
                create_date         TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                modify_date         TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                create_by           VARCHAR(50)     NOT NULL DEFAULT 'SYSTEM',
                modify_by           VARCHAR(50)     NOT NULL DEFAULT 'SYSTEM',
                create_user_id      BIGINT          NOT NULL DEFAULT 0,
                modify_user_id      BIGINT          NOT NULL DEFAULT 0,

                CONSTRAINT fk_trigger_graph_workflow FOREIGN KEY (workflow_id) REFERENCES ff_workflow(id)
            );

            CREATE INDEX IF NOT EXISTS idx_trigger_graph_workflow  ON ff_workflow_trigger_graph(workflow_id);
            CREATE INDEX IF NOT EXISTS idx_trigger_graph_tenant    ON ff_workflow_trigger_graph(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_trigger_graph_app_code  ON ff_workflow_trigger_graph(app_code);

            COMMENT ON TABLE ff_workflow_trigger_graph IS
                'Canvas graph for a workflow trigger editor — holds layout and which workflows are on canvas';

            -- Trigger Connection: directed edge between two workflows
            CREATE TABLE IF NOT EXISTS ff_workflow_trigger_connection (
                id                  BIGINT          PRIMARY KEY,
                graph_id            BIGINT          NOT NULL,
                source_workflow_id  BIGINT          NOT NULL,
                target_workflow_id  BIGINT          NOT NULL,
                rule_name           VARCHAR(200)    NOT NULL DEFAULT '',
                condition_summary   VARCHAR(500)    NOT NULL DEFAULT '',
                config_json         JSONB           NOT NULL DEFAULT '{}',
                is_enabled          BOOLEAN         NOT NULL DEFAULT TRUE,
                execution_order     INT             NOT NULL DEFAULT 0,
                tenant_id           VARCHAR(32)     NOT NULL DEFAULT 'DEFAULT',
                app_code            VARCHAR(32)     NOT NULL DEFAULT 'DEFAULT',
                is_valid            BOOLEAN         NOT NULL DEFAULT TRUE,
                create_date         TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                modify_date         TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                create_by           VARCHAR(50)     NOT NULL DEFAULT 'SYSTEM',
                modify_by           VARCHAR(50)     NOT NULL DEFAULT 'SYSTEM',
                create_user_id      BIGINT          NOT NULL DEFAULT 0,
                modify_user_id      BIGINT          NOT NULL DEFAULT 0,

                CONSTRAINT fk_trigger_conn_graph  FOREIGN KEY (graph_id)           REFERENCES ff_workflow_trigger_graph(id),
                CONSTRAINT fk_trigger_conn_source FOREIGN KEY (source_workflow_id) REFERENCES ff_workflow(id),
                CONSTRAINT fk_trigger_conn_target FOREIGN KEY (target_workflow_id) REFERENCES ff_workflow(id)
            );

            CREATE INDEX IF NOT EXISTS idx_trigger_conn_graph   ON ff_workflow_trigger_connection(graph_id);
            CREATE INDEX IF NOT EXISTS idx_trigger_conn_source  ON ff_workflow_trigger_connection(source_workflow_id);
            CREATE INDEX IF NOT EXISTS idx_trigger_conn_target  ON ff_workflow_trigger_connection(target_workflow_id);
            CREATE INDEX IF NOT EXISTS idx_trigger_conn_tenant  ON ff_workflow_trigger_connection(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_trigger_conn_appcode ON ff_workflow_trigger_connection(app_code);

            COMMENT ON TABLE ff_workflow_trigger_connection IS
                'Directed edge in a trigger graph: conditions + data-mapping config between two workflows';
        ";

        db.Ado.ExecuteCommand(sql);
        Console.WriteLine("[Migration] Created ff_workflow_trigger_graph and ff_workflow_trigger_connection tables");
    }

    public static void Down(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            DROP TABLE IF EXISTS ff_workflow_trigger_connection;
            DROP TABLE IF EXISTS ff_workflow_trigger_graph;
        ");
        Console.WriteLine("[Migration] Dropped ff_workflow_trigger_graph and ff_workflow_trigger_connection tables");
    }
}
