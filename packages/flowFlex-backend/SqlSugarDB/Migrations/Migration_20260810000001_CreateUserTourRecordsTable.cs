using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations;

/// <summary>
/// Creates the ff_user_tour_records table.
/// Stores per-user, per-tour "seen" state so guided tours only show once per account,
/// across all modules (workflow, checklist, questionnaire, etc.).
///
/// tour_key convention:
///   workflow   — "workflow-list-tour", "workflow-detail-tour",
///                "workflow-condition-tour-{workflowId}", "workflow-stage-form-tour"
///   checklist  — "checklist-list-tour", "checklist-detail-tour-{checklistId}", ...
///   questionnaire — "questionnaire-list-tour", ...
///   (any future module follows the same pattern: "{module}-{context}-tour[-{id}]")
/// </summary>
public static class Migration_20260810000001_CreateUserTourRecordsTable
{
    public static void Up(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ff_user_tour_records (
                id            BIGINT          NOT NULL PRIMARY KEY,
                user_id       BIGINT          NOT NULL,
                tour_key      VARCHAR(200)    NOT NULL,
                seen_at       TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

                -- multi-tenancy
                tenant_id     VARCHAR(32)     NOT NULL DEFAULT 'default',
                app_code      VARCHAR(32)     NOT NULL DEFAULT 'default',

                -- soft delete (kept for consistency; tours are never actually deleted)
                is_valid      BOOLEAN         NOT NULL DEFAULT TRUE,

                -- audit
                create_date   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                modify_date   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                create_by     VARCHAR(50)     NOT NULL DEFAULT 'SYSTEM',
                modify_by     VARCHAR(50)     NOT NULL DEFAULT 'SYSTEM',
                create_user_id BIGINT         NOT NULL DEFAULT 0,
                modify_user_id BIGINT         NOT NULL DEFAULT 0
            );
        ");

        // Unique constraint: one record per (user, tour, tenant, app)
        db.Ado.ExecuteCommand(@"
            CREATE UNIQUE INDEX IF NOT EXISTS uq_user_tour_records_user_key
                ON ff_user_tour_records (user_id, tour_key, tenant_id, app_code)
                WHERE is_valid = TRUE;
        ");

        // Index for the primary lookup pattern: HasSeen(userId, tourKey)
        db.Ado.ExecuteCommand(@"
            CREATE INDEX IF NOT EXISTS idx_user_tour_records_user_id
                ON ff_user_tour_records (user_id, tenant_id, app_code);
        ");
    }

    public static void Down(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"DROP TABLE IF EXISTS ff_user_tour_records;");
    }
}
