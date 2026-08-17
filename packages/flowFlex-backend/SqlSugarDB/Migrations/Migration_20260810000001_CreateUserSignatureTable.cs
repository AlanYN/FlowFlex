using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations;

/// <summary>
/// Migration to create ff_user_signature table.
/// Stores user electronic signatures as base64 PNG data.
/// No app_code or tenant_id columns — signatures are user-scoped and cross-tenant.
/// </summary>
public static class Migration_20260810000001_CreateUserSignatureTable
{
    public static void Up(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            CREATE TABLE IF NOT EXISTS ff_user_signature (
                id bigint PRIMARY KEY,
                user_id bigint NOT NULL,
                image_data TEXT NOT NULL,
                create_date timestamptz,
                modify_date timestamptz,
                create_by varchar(200),
                modify_by varchar(200),
                create_user_id bigint,
                modify_user_id bigint,
                is_valid bool NOT NULL DEFAULT true
            );
        ");

        Console.WriteLine("[Migration] Created ff_user_signature table");
    }

    public static void Down(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(@"
            DROP TABLE IF EXISTS ff_user_signature;
        ");

        Console.WriteLine("[Migration] Dropped ff_user_signature table");
    }
}
