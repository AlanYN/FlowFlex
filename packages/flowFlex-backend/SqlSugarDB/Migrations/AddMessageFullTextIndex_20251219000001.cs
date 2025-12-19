using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Migration to add full-text search index for messages table
    /// This improves search performance significantly compared to LIKE queries
    /// </summary>
    public class AddMessageFullTextIndex_20251219000001
    {
        /// <summary>
        /// Apply migration - create GIN index for full-text search
        /// </summary>
        public static void Up(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[Migration] Creating full-text search index for messages...");

                // Create GIN index for full-text search on subject and body columns
                // Using 'simple' configuration for basic tokenization (works well for mixed language content)
                var createIndexSql = @"
                    CREATE INDEX IF NOT EXISTS idx_ff_messages_fulltext 
                    ON ff_messages 
                    USING GIN (to_tsvector('simple', coalesce(subject, '') || ' ' || coalesce(body, '')));
                ";

                db.Ado.ExecuteCommand(createIndexSql);
                Console.WriteLine("[Migration] Full-text search index created successfully");

                // Also create index on sender_email for faster filtering
                var createSenderIndexSql = @"
                    CREATE INDEX IF NOT EXISTS idx_ff_messages_sender_email 
                    ON ff_messages (sender_email);
                ";

                db.Ado.ExecuteCommand(createSenderIndexSql);
                Console.WriteLine("[Migration] Sender email index created successfully");

                // Create composite index for common query patterns
                var createCompositeIndexSql = @"
                    CREATE INDEX IF NOT EXISTS idx_ff_messages_owner_folder_received 
                    ON ff_messages (owner_id, folder, received_date DESC)
                    WHERE is_valid = true;
                ";

                db.Ado.ExecuteCommand(createCompositeIndexSql);
                Console.WriteLine("[Migration] Composite index for owner/folder/received_date created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error creating full-text search index: {ex.Message}");
                // Don't throw - index creation failure should not block application startup
            }
        }

        /// <summary>
        /// Rollback migration - drop the indexes
        /// </summary>
        public static void Down(ISqlSugarClient db)
        {
            try
            {
                Console.WriteLine("[Migration] Dropping full-text search indexes...");

                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ff_messages_fulltext;");
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ff_messages_sender_email;");
                db.Ado.ExecuteCommand("DROP INDEX IF EXISTS idx_ff_messages_owner_folder_received;");

                Console.WriteLine("[Migration] Full-text search indexes dropped successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Migration] Error dropping indexes: {ex.Message}");
                throw;
            }
        }
    }
}
