using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations;

/// <summary>
/// Migration to create phone_number_prefixes table in master schema
/// </summary>
public static class Migration_20251231000001_CreatePhoneNumberPrefixesTable
{
    public static void Up(ISqlSugarClient db)
    {
        // Create master schema if not exists
        db.Ado.ExecuteCommand("CREATE SCHEMA IF NOT EXISTS master");

        // Create phone_number_prefixes table
        var sql = @"
            CREATE TABLE IF NOT EXISTS master.phone_number_prefixes (
                id BIGINT PRIMARY KEY,
                country_name VARCHAR(100),
                country_code VARCHAR(100),
                dialing_code VARCHAR(100),
                description VARCHAR(100)
            );

            COMMENT ON TABLE master.phone_number_prefixes IS 'Phone number prefixes for countries/regions';
            COMMENT ON COLUMN master.phone_number_prefixes.id IS 'Primary key ID';
            COMMENT ON COLUMN master.phone_number_prefixes.country_name IS 'Country/Region name';
            COMMENT ON COLUMN master.phone_number_prefixes.country_code IS 'Country/Region code (e.g., CN, US)';
            COMMENT ON COLUMN master.phone_number_prefixes.dialing_code IS 'Dialing code (e.g., 86, 1)';
            COMMENT ON COLUMN master.phone_number_prefixes.description IS 'Description/Remarks';
        ";

        db.Ado.ExecuteCommand(sql);

        Console.WriteLine("[Migration] Created master.phone_number_prefixes table");
    }

    public static void Down(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand("DROP TABLE IF EXISTS master.phone_number_prefixes");
        Console.WriteLine("[Migration] Dropped master.phone_number_prefixes table");
    }
}
