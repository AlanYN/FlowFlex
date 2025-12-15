using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// 创建 Message Center 相关表
    /// - ff_messages: 统一消息存储表（Internal、Email、Portal）
    /// - ff_message_attachments: 消息附件表
    /// </summary>
    public class _20251215000001_CreateMessageCenterTables
    {
        public static void Up(ISqlSugarClient db)
        {
            Console.WriteLine("[Migration] Creating Message Center tables...");

            // 创建 ff_messages 表
            var createMessagesTableSql = @"
                CREATE TABLE IF NOT EXISTS ff_messages (
                    id BIGINT PRIMARY KEY,
                    tenant_id VARCHAR(50) NOT NULL,
                    app_code VARCHAR(50),
                    is_valid BOOLEAN NOT NULL DEFAULT TRUE,
                    
                    -- 消息内容
                    subject VARCHAR(500) NOT NULL DEFAULT '',
                    body TEXT NOT NULL DEFAULT '',
                    body_preview VARCHAR(500) DEFAULT '',
                    
                    -- 消息分类
                    message_type VARCHAR(20) NOT NULL DEFAULT 'Internal',
                    folder VARCHAR(20) NOT NULL DEFAULT 'Inbox',
                    labels JSONB DEFAULT '[]'::jsonb,
                    
                    -- 发送者信息
                    sender_id BIGINT,
                    sender_name VARCHAR(100) DEFAULT '',
                    sender_email VARCHAR(200) DEFAULT '',
                    
                    -- 收件人（JSON 数组）
                    recipients JSONB DEFAULT '[]'::jsonb,
                    cc_recipients JSONB DEFAULT '[]'::jsonb,
                    bcc_recipients JSONB DEFAULT '[]'::jsonb,
                    
                    -- 关联实体
                    related_entity_type VARCHAR(50),
                    related_entity_id BIGINT,
                    related_entity_code VARCHAR(50),
                    
                    -- 消息状态
                    is_read BOOLEAN NOT NULL DEFAULT FALSE,
                    is_starred BOOLEAN NOT NULL DEFAULT FALSE,
                    is_draft BOOLEAN NOT NULL DEFAULT FALSE,
                    has_attachments BOOLEAN NOT NULL DEFAULT FALSE,
                    
                    -- 时间戳
                    sent_date TIMESTAMPTZ,
                    received_date TIMESTAMPTZ,
                    
                    -- 会话线程
                    parent_message_id BIGINT,
                    conversation_id VARCHAR(100),
                    
                    -- Portal 集成
                    portal_id BIGINT,
                    
                    -- Outlook 集成
                    external_message_id VARCHAR(200),
                    
                    -- 所有权
                    owner_id BIGINT NOT NULL,
                    original_folder VARCHAR(20),
                    
                    -- 审计字段
                    create_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(100),
                    modify_by VARCHAR(100),
                    create_user_id BIGINT,
                    modify_user_id BIGINT
                );
            ";
            db.Ado.ExecuteCommand(createMessagesTableSql);
            Console.WriteLine("[Migration] Created ff_messages table");

            // 添加表注释
            var addMessagesCommentsSql = @"
                COMMENT ON TABLE ff_messages IS '统一消息存储表（Internal、Email、Portal）';
                COMMENT ON COLUMN ff_messages.message_type IS '消息类型: Internal, Email, Portal';
                COMMENT ON COLUMN ff_messages.folder IS '文件夹: Inbox, Sent, Starred, Archive, Trash';
                COMMENT ON COLUMN ff_messages.labels IS '标签 JSON 数组: Internal, External, Important, Portal';
                COMMENT ON COLUMN ff_messages.owner_id IS '消息所有者用户 ID';
                COMMENT ON COLUMN ff_messages.external_message_id IS 'Outlook 集成的外部消息 ID';
                COMMENT ON COLUMN ff_messages.original_folder IS '移动到垃圾箱前的原始文件夹，用于恢复';
            ";
            db.Ado.ExecuteCommand(addMessagesCommentsSql);

            // 创建索引
            var createMessagesIndexesSql = @"
                CREATE INDEX IF NOT EXISTS idx_ff_messages_tenant_id ON ff_messages(tenant_id);
                CREATE INDEX IF NOT EXISTS idx_ff_messages_owner_id ON ff_messages(owner_id);
                CREATE INDEX IF NOT EXISTS idx_ff_messages_folder ON ff_messages(folder);
                CREATE INDEX IF NOT EXISTS idx_ff_messages_message_type ON ff_messages(message_type);
                CREATE INDEX IF NOT EXISTS idx_ff_messages_received_date ON ff_messages(received_date DESC);
                CREATE INDEX IF NOT EXISTS idx_ff_messages_is_read ON ff_messages(is_read);
                CREATE INDEX IF NOT EXISTS idx_ff_messages_is_starred ON ff_messages(is_starred);
                CREATE INDEX IF NOT EXISTS idx_ff_messages_related_entity ON ff_messages(related_entity_type, related_entity_id);
                CREATE INDEX IF NOT EXISTS idx_ff_messages_conversation_id ON ff_messages(conversation_id);
                CREATE INDEX IF NOT EXISTS idx_ff_messages_external_message_id ON ff_messages(external_message_id);
                CREATE INDEX IF NOT EXISTS idx_ff_messages_owner_folder ON ff_messages(owner_id, folder, is_valid);
                CREATE INDEX IF NOT EXISTS idx_ff_messages_owner_received ON ff_messages(owner_id, received_date DESC) WHERE is_valid = TRUE;
            ";
            db.Ado.ExecuteCommand(createMessagesIndexesSql);
            Console.WriteLine("[Migration] Created indexes for ff_messages");

            // 创建 ff_message_attachments 表
            var createAttachmentsTableSql = @"
                CREATE TABLE IF NOT EXISTS ff_message_attachments (
                    id BIGINT PRIMARY KEY,
                    tenant_id VARCHAR(50) NOT NULL,
                    app_code VARCHAR(50),
                    is_valid BOOLEAN NOT NULL DEFAULT TRUE,
                    
                    -- 消息引用
                    message_id BIGINT NOT NULL DEFAULT 0,
                    
                    -- 文件信息
                    file_name VARCHAR(255) NOT NULL DEFAULT '',
                    file_size BIGINT NOT NULL DEFAULT 0,
                    content_type VARCHAR(100) NOT NULL DEFAULT 'application/octet-stream',
                    storage_path VARCHAR(500) NOT NULL DEFAULT '',
                    
                    -- Outlook 集成
                    external_attachment_id VARCHAR(200),
                    
                    -- 内联附件支持
                    content_id VARCHAR(100),
                    is_inline BOOLEAN NOT NULL DEFAULT FALSE,
                    
                    -- 审计字段
                    create_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(100),
                    modify_by VARCHAR(100),
                    create_user_id BIGINT,
                    modify_user_id BIGINT
                );
            ";
            db.Ado.ExecuteCommand(createAttachmentsTableSql);
            Console.WriteLine("[Migration] Created ff_message_attachments table");

            // 添加表注释
            var addAttachmentsCommentsSql = @"
                COMMENT ON TABLE ff_message_attachments IS '消息附件存储表';
                COMMENT ON COLUMN ff_message_attachments.message_id IS '父消息 ID，0 表示临时附件';
                COMMENT ON COLUMN ff_message_attachments.storage_path IS 'Blob 存储路径';
                COMMENT ON COLUMN ff_message_attachments.external_attachment_id IS 'Outlook 集成的外部附件 ID';
                COMMENT ON COLUMN ff_message_attachments.content_id IS 'HTML 正文中内联图片的 Content ID';
            ";
            db.Ado.ExecuteCommand(addAttachmentsCommentsSql);

            // 创建索引
            var createAttachmentsIndexesSql = @"
                CREATE INDEX IF NOT EXISTS idx_ff_message_attachments_tenant_id ON ff_message_attachments(tenant_id);
                CREATE INDEX IF NOT EXISTS idx_ff_message_attachments_message_id ON ff_message_attachments(message_id);
                CREATE INDEX IF NOT EXISTS idx_ff_message_attachments_external_id ON ff_message_attachments(external_attachment_id);
                CREATE INDEX IF NOT EXISTS idx_ff_message_attachments_unassociated ON ff_message_attachments(message_id, create_date) WHERE message_id = 0;
            ";
            db.Ado.ExecuteCommand(createAttachmentsIndexesSql);
            Console.WriteLine("[Migration] Created indexes for ff_message_attachments");

            Console.WriteLine("[Migration] Message Center tables created successfully");
        }

        public static void Down(ISqlSugarClient db)
        {
            Console.WriteLine("[Migration] Dropping Message Center tables...");

            db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_message_attachments CASCADE;");
            db.Ado.ExecuteCommand("DROP TABLE IF EXISTS ff_messages CASCADE;");

            Console.WriteLine("[Migration] Message Center tables dropped successfully");
        }
    }
}
