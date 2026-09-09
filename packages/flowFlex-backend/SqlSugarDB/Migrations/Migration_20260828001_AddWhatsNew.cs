using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations;

public static class Migration_20260828001_AddWhatsNew
{
    public static void Up(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ff_whats_new (
                id              BIGINT          NOT NULL PRIMARY KEY,
                title           VARCHAR(100)    NOT NULL,
                summary         VARCHAR(200)    NOT NULL,
                content         TEXT            NOT NULL,
                category        VARCHAR(50)     NOT NULL,
                status          INT             NOT NULL DEFAULT 0,
                publish_time    TIMESTAMPTZ,
                scheduled_time  TIMESTAMPTZ,
                create_date     TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                modify_date     TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
                create_by       VARCHAR(100)    NOT NULL DEFAULT '',
                modify_by       VARCHAR(100)    NOT NULL DEFAULT '',
                create_user_id  BIGINT          NOT NULL DEFAULT 0,
                modify_user_id  BIGINT          NOT NULL DEFAULT 0,
                is_valid        BOOLEAN         NOT NULL DEFAULT TRUE
            );

            CREATE INDEX IF NOT EXISTS idx_ff_whats_new_status_publish_time
                ON ff_whats_new (status, publish_time DESC)
                WHERE is_valid = TRUE;

            CREATE TABLE IF NOT EXISTS ff_whats_new_read_status (
                id              BIGINT          NOT NULL PRIMARY KEY,
                whats_new_id    BIGINT          NOT NULL,
                user_id         BIGINT          NOT NULL,
                read_time       TIMESTAMPTZ     NOT NULL DEFAULT NOW()
            );

            CREATE UNIQUE INDEX IF NOT EXISTS uidx_ff_whats_new_read_status_unique
                ON ff_whats_new_read_status (whats_new_id, user_id);

            CREATE INDEX IF NOT EXISTS idx_ff_whats_new_read_status_user
                ON ff_whats_new_read_status (user_id);

            CREATE INDEX IF NOT EXISTS idx_ff_whats_new_read_status_whats_new
                ON ff_whats_new_read_status (whats_new_id);
        ");

        Console.WriteLine("[Migration] Created ff_whats_new and ff_whats_new_read_status tables");
    }

    public static void Down(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            DROP TABLE IF EXISTS ff_whats_new_read_status;
            DROP TABLE IF EXISTS ff_whats_new;
        ");

        Console.WriteLine("[Migration] Dropped ff_whats_new and ff_whats_new_read_status tables");
    }
}
