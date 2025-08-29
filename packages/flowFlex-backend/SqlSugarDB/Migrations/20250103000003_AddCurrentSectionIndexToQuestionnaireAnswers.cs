using System;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// 添加 current_section_index 字段到 ff_questionnaire_answers 表
    /// 
    /// 此迁移为问卷答案表添加当前章节索引字段，用于跟踪用户在多章节问卷中的填写进度
    /// 
    /// 好处：
    /// - 支持多章节问卷的进度保存和恢复
    /// - 提高用户体验，允许用户中断后继续填写
    /// - 为前端提供当前章节的位置信息
    /// </summary>
    public class AddCurrentSectionIndexToQuestionnaireAnswers_20250103000003
    {
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Starting AddCurrentSectionIndexToQuestionnaireAnswers migration...");

                // 检查列是否已存在
                var columnExists = db.Ado.GetDataTable(@"
                    SELECT column_name 
                    FROM information_schema.columns 
                    WHERE table_name = 'ff_questionnaire_answers' 
                    AND column_name = 'current_section_index'
                ").Rows.Count > 0;

                if (!columnExists)
                {
                    Console.WriteLine("Adding current_section_index column to ff_questionnaire_answers table...");

                    // 添加列
                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_questionnaire_answers 
                        ADD COLUMN current_section_index INT NOT NULL DEFAULT 0;
                    ");

                    Console.WriteLine("Column current_section_index added successfully.");

                    // 为新列创建索引以提高查询性能
                    Console.WriteLine("Creating index on current_section_index...");

                    var indexExists = db.Ado.GetDataTable(@"
                        SELECT indexname 
                        FROM pg_indexes 
                        WHERE tablename = 'ff_questionnaire_answers' 
                        AND indexname = 'idx_ff_questionnaire_answers_current_section_index'
                    ").Rows.Count > 0;

                    if (!indexExists)
                    {
                        db.Ado.ExecuteCommand(@"
                            CREATE INDEX idx_ff_questionnaire_answers_current_section_index 
                            ON ff_questionnaire_answers (current_section_index);
                        ");
                        Console.WriteLine("Index created successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Index already exists, skipping creation.");
                    }
                }
                else
                {
                    Console.WriteLine("current_section_index column already exists, skipping addition.");
                }

                // 记录完成
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine($"Migration AddCurrentSectionIndexToQuestionnaireAnswers_20250103000003 completed at {timestamp}");
                Console.WriteLine("Benefits: Improved multi-section questionnaire progress tracking and user experience.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddCurrentSectionIndexToQuestionnaireAnswers migration: {ex.Message}");
                throw;
            }
        }

        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("Rolling back AddCurrentSectionIndexToQuestionnaireAnswers migration...");

                // 先删除索引
                Console.WriteLine("Dropping index on current_section_index...");

                db.Ado.ExecuteCommand(@"
                    DROP INDEX IF EXISTS idx_ff_questionnaire_answers_current_section_index;
                ");

                // 检查列是否存在
                var columnExists = db.Ado.GetDataTable(@"
                    SELECT column_name 
                    FROM information_schema.columns 
                    WHERE table_name = 'ff_questionnaire_answers' 
                    AND column_name = 'current_section_index'
                ").Rows.Count > 0;

                if (columnExists)
                {
                    // 删除列
                    Console.WriteLine("Removing current_section_index column...");

                    db.Ado.ExecuteCommand(@"
                        ALTER TABLE ff_questionnaire_answers 
                        DROP COLUMN current_section_index;
                    ");

                    Console.WriteLine("Column current_section_index removed successfully.");
                }
                else
                {
                    Console.WriteLine("current_section_index column does not exist, skipping removal.");
                }

                Console.WriteLine("AddCurrentSectionIndexToQuestionnaireAnswers migration rollback completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rolling back AddCurrentSectionIndexToQuestionnaireAnswers migration: {ex.Message}");
                throw;
            }
        }
    }
}