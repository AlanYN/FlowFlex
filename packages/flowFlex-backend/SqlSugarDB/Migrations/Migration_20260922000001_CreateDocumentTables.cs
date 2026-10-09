using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Create ff_document_snapshot and ff_document_operation_log tables for Univer document management
    /// </summary>
    public static class Migration_20260922000001_CreateDocumentTables
    {
        public static void Up(ISqlSugarClient db)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ff_document_snapshot (
                    id               BIGINT          NOT NULL PRIMARY KEY,
                    unit_id          VARCHAR(100)    NOT NULL,
                    title            VARCHAR(200),
                    doc_type         VARCHAR(20)     NOT NULL DEFAULT 'Sheet',
                    data_json        JSONB,
                    revision         INT             NOT NULL DEFAULT 1,
                    entity_type      VARCHAR(50),
                    entity_id        BIGINT,
                    tenant_id        VARCHAR(100)    NOT NULL DEFAULT '',
                    app_code         VARCHAR(100)    NOT NULL DEFAULT '',
                    is_valid         BOOLEAN         NOT NULL DEFAULT TRUE,
                    create_date      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    modify_date      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    create_by        VARCHAR(200),
                    modify_by        VARCHAR(200),
                    create_user_id   BIGINT          NOT NULL DEFAULT 0,
                    modify_user_id   BIGINT          NOT NULL DEFAULT 0
                );

                CREATE UNIQUE INDEX IF NOT EXISTS idx_ff_doc_snapshot_unit_id
                    ON ff_document_snapshot (unit_id, tenant_id, app_code)
                    WHERE is_valid = TRUE;

                CREATE INDEX IF NOT EXISTS idx_ff_doc_snapshot_entity
                    ON ff_document_snapshot (entity_type, entity_id, tenant_id, app_code)
                    WHERE is_valid = TRUE;

                CREATE INDEX IF NOT EXISTS idx_ff_doc_snapshot_list
                    ON ff_document_snapshot (tenant_id, app_code, modify_date DESC)
                    WHERE is_valid = TRUE;

                CREATE TABLE IF NOT EXISTS ff_document_operation_log (
                    id                   BIGINT          NOT NULL PRIMARY KEY,
                    unit_id              VARCHAR(100)    NOT NULL,
                    operator_user_id     VARCHAR(100),
                    operator_user_name   VARCHAR(100),
                    op_timestamp         BIGINT          NOT NULL DEFAULT 0,
                    mutation_id          VARCHAR(200),
                    params_json          JSONB,
                    revision             INT             NOT NULL DEFAULT 0,
                    tenant_id            VARCHAR(100)    NOT NULL DEFAULT '',
                    app_code             VARCHAR(100)    NOT NULL DEFAULT '',
                    is_valid             BOOLEAN         NOT NULL DEFAULT TRUE,
                    create_date          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    modify_date          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                    create_by            VARCHAR(200),
                    modify_by            VARCHAR(200),
                    create_user_id       BIGINT          NOT NULL DEFAULT 0,
                    modify_user_id       BIGINT          NOT NULL DEFAULT 0
                );

                CREATE INDEX IF NOT EXISTS idx_ff_doc_op_log_unit_id
                    ON ff_document_operation_log (unit_id, tenant_id, app_code, create_date DESC);
            ";

            db.Ado.ExecuteCommand(sql);
        }

        public static void Down(ISqlSugarClient db)
        {
            db.Ado.ExecuteCommand(@"
                DROP TABLE IF EXISTS ff_document_operation_log;
                DROP TABLE IF EXISTS ff_document_snapshot;
            ");
        }
    }
}
