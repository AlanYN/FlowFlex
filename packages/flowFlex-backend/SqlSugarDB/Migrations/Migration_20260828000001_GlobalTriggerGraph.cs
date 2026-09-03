using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// OW-730: Make trigger graph global (one per tenant+appCode instead of one per workflow).
    ///
    /// Strategy:
    ///   1. Drop the unique constraint on workflow_id (was implicitly unique via usage).
    ///   2. Allow workflow_id = 0 to represent the global graph.
    ///   3. For each tenant+appCode that has existing per-workflow graphs, pick the one with the
    ///      most connections (or the earliest one) as the "winner" and set its workflow_id = 0.
    ///      Soft-delete the remaining per-workflow graphs after copying their connections to the
    ///      winner graph.
    ///
    /// Post-migration invariant: every tenant+appCode has at most ONE graph with workflow_id = 0.
    /// </summary>
    public static class Migration_20260828000001_GlobalTriggerGraph
    {
        public static void Up(ISqlSugarClient db)
        {
            // 1. Remove the FK constraint that enforces workflow_id → ff_workflow (allows 0)
            db.Ado.ExecuteCommand(@"
                ALTER TABLE ff_workflow_trigger_graph
                    DROP CONSTRAINT IF EXISTS fk_trigger_graph_workflow;
            ");

            // 2. For each (tenant_id, app_code) group, pick the graph with the most connections
            //    (or the earliest id) and promote it to the global graph (workflow_id = 0).
            //    Move all connections from the other graphs to the winner, then soft-delete them.
            db.Ado.ExecuteCommand(@"
                DO $$
                DECLARE
                    r RECORD;
                    winner_id BIGINT;
                BEGIN
                    -- Iterate over every distinct (tenant_id, app_code) pair that has graphs
                    FOR r IN
                        SELECT DISTINCT tenant_id, app_code
                        FROM ff_workflow_trigger_graph
                        WHERE is_valid = true AND workflow_id <> 0
                    LOOP
                        -- Pick the graph with the most associated connections (ties broken by earliest id)
                        SELECT g.id INTO winner_id
                        FROM ff_workflow_trigger_graph g
                        LEFT JOIN (
                            SELECT graph_id, COUNT(*) AS cnt
                            FROM ff_workflow_trigger_connection
                            WHERE is_valid = true
                            GROUP BY graph_id
                        ) c ON c.graph_id = g.id
                        WHERE g.tenant_id = r.tenant_id
                          AND g.app_code  = r.app_code
                          AND g.is_valid  = true
                          AND g.workflow_id <> 0
                        ORDER BY COALESCE(c.cnt, 0) DESC, g.id ASC
                        LIMIT 1;

                        IF winner_id IS NULL THEN
                            CONTINUE;
                        END IF;

                        -- Re-point all connections from OTHER graphs in this tenant to the winner
                        UPDATE ff_workflow_trigger_connection
                        SET graph_id = winner_id
                        WHERE graph_id IN (
                            SELECT id FROM ff_workflow_trigger_graph
                            WHERE tenant_id  = r.tenant_id
                              AND app_code   = r.app_code
                              AND is_valid   = true
                              AND workflow_id <> 0
                              AND id         <> winner_id
                        );

                        -- Soft-delete the non-winner graphs
                        UPDATE ff_workflow_trigger_graph
                        SET is_valid = false
                        WHERE tenant_id  = r.tenant_id
                          AND app_code   = r.app_code
                          AND is_valid   = true
                          AND workflow_id <> 0
                          AND id         <> winner_id;

                        -- Promote winner to global (workflow_id = 0)
                        UPDATE ff_workflow_trigger_graph
                        SET workflow_id = 0
                        WHERE id = winner_id;
                    END LOOP;
                END $$;
            ");

            // 3. Create index for the new global-graph lookup pattern
            db.Ado.ExecuteCommand(@"
                CREATE INDEX IF NOT EXISTS idx_trigger_graph_global
                    ON ff_workflow_trigger_graph(tenant_id, app_code, workflow_id)
                    WHERE is_valid = true;
            ");
        }

        public static void Down(ISqlSugarClient db)
        {
            db.Ado.ExecuteCommand(@"
                DROP INDEX IF EXISTS idx_trigger_graph_global;
                -- Re-add FK (best-effort; may fail if data has workflow_id=0)
                ALTER TABLE ff_workflow_trigger_graph
                    ADD CONSTRAINT fk_trigger_graph_workflow
                    FOREIGN KEY (workflow_id) REFERENCES ff_workflow(id);
            ");
        }
    }
}
