using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations;

/// <summary>
/// Migration to add audit fields to ff_data_value table
/// </summary>
public static class AddAuditFieldsToDataValue_20251226000002
{
    public static void Up(ISqlSugarClient db)
    {
        // Add missing audit columns to ff_data_value table
        var addColumns = @"
            DO $$
            BEGIN
                -- Add create_date column if not exists
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'ff_data_value' AND column_name = 'create_date') THEN
                    ALTER TABLE ff_data_value ADD COLUMN create_date TIMESTAMPTZ;
                END IF;

                -- Add create_by column if not exists
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'ff_data_value' AND column_name = 'create_by') THEN
                    ALTER TABLE ff_data_value ADD COLUMN create_by VARCHAR(100);
                END IF;

                -- Add create_user_id column if not exists
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'ff_data_value' AND column_name = 'create_user_id') THEN
                    ALTER TABLE ff_data_value ADD COLUMN create_user_id BIGINT;
                END IF;

                -- Add modify_date column if not exists
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'ff_data_value' AND column_name = 'modify_date') THEN
                    ALTER TABLE ff_data_value ADD COLUMN modify_date TIMESTAMPTZ;
                END IF;

                -- Add modify_by column if not exists
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'ff_data_value' AND column_name = 'modify_by') THEN
                    ALTER TABLE ff_data_value ADD COLUMN modify_by VARCHAR(100);
                END IF;

                -- Add modify_user_id column if not exists
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'ff_data_value' AND column_name = 'modify_user_id') THEN
                    ALTER TABLE ff_data_value ADD COLUMN modify_user_id BIGINT;
                END IF;
            END $$;
        ";
        db.Ado.ExecuteCommand(addColumns);
    }

    public static void Down(ISqlSugarClient db)
    {
        // Remove audit columns from ff_data_value table
        var removeColumns = @"
            ALTER TABLE ff_data_value DROP COLUMN IF EXISTS create_date;
            ALTER TABLE ff_data_value DROP COLUMN IF EXISTS create_by;
            ALTER TABLE ff_data_value DROP COLUMN IF EXISTS create_user_id;
            ALTER TABLE ff_data_value DROP COLUMN IF EXISTS modify_date;
            ALTER TABLE ff_data_value DROP COLUMN IF EXISTS modify_by;
            ALTER TABLE ff_data_value DROP COLUMN IF EXISTS modify_user_id;
        ";
        db.Ado.ExecuteCommand(removeColumns);
    }
}
