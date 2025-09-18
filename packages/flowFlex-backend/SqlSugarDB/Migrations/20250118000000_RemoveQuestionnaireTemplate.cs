using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Remove template functionality from Questionnaire entity
    /// </summary>
    public class _20250118000000_RemoveQuestionnaireTemplate
    {
        public static void Up(ISqlSugarClient db)
        {
            // Remove template-related columns from ff_questionnaire table
            try
            {
                // Drop columns if they exist
                var sql = @"
                    DO $$
                    BEGIN
                        -- Remove is_template column
                        IF EXISTS (SELECT 1 FROM information_schema.columns 
                                  WHERE table_name='ff_questionnaire' AND column_name='is_template') THEN
                            ALTER TABLE ff_questionnaire DROP COLUMN is_template;
                        END IF;
                        
                        -- Remove template_id column
                        IF EXISTS (SELECT 1 FROM information_schema.columns 
                                  WHERE table_name='ff_questionnaire' AND column_name='template_id') THEN
                            ALTER TABLE ff_questionnaire DROP COLUMN template_id;
                        END IF;
                        
                        -- Remove type column (Template/Instance)
                        IF EXISTS (SELECT 1 FROM information_schema.columns 
                                  WHERE table_name='ff_questionnaire' AND column_name='type') THEN
                            ALTER TABLE ff_questionnaire DROP COLUMN type;
                        END IF;
                    END $$;
                ";

                db.Ado.ExecuteCommand(sql);

                Console.WriteLine("Successfully removed template-related columns from ff_questionnaire table");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to remove some columns: {ex.Message}");
                // Continue migration even if some columns don't exist
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            // Add back template-related columns if rollback is needed
            try
            {
                var sql = @"
                    DO $$
                    BEGIN
                        -- Add back is_template column
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                      WHERE table_name='ff_questionnaire' AND column_name='is_template') THEN
                            ALTER TABLE ff_questionnaire ADD COLUMN is_template BOOLEAN NOT NULL DEFAULT true;
                        END IF;
                        
                        -- Add back template_id column
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                      WHERE table_name='ff_questionnaire' AND column_name='template_id') THEN
                            ALTER TABLE ff_questionnaire ADD COLUMN template_id BIGINT NULL;
                        END IF;
                        
                        -- Add back type column
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                      WHERE table_name='ff_questionnaire' AND column_name='type') THEN
                            ALTER TABLE ff_questionnaire ADD COLUMN type VARCHAR(20) NOT NULL DEFAULT 'Template';
                        END IF;
                    END $$;
                ";

                db.Ado.ExecuteCommand(sql);

                Console.WriteLine("Successfully restored template-related columns to ff_questionnaire table");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during rollback: {ex.Message}");
                throw;
            }
        }
    }
}