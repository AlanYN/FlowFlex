using SqlSugar;
using System;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Create user invitations table migration
    /// Version: 2.0.0
    /// Created: 2025-01-01
    /// Description: Create user invitations table for portal access
    /// </summary>
    public class CreateUserInvitationsTable_20250101000008
    {
        /// <summary>
        /// Execute migration - create user invitations table
        /// </summary>
        public static void Up(ISqlSugarClient db)
        {
            // Create user invitations table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_user_invitations (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                    onboarding_id BIGINT NOT NULL,
                    email VARCHAR(200) NOT NULL,
                    invitation_token VARCHAR(100) NOT NULL,
                    status VARCHAR(20) DEFAULT 'Pending',
                    sent_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    token_expiry TIMESTAMPTZ NOT NULL,
                    last_access_date TIMESTAMPTZ,
                    user_id BIGINT,
                    send_count INTEGER DEFAULT 1,
                    invitation_url VARCHAR(500),
                    notes VARCHAR(1000),
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
                
                CREATE INDEX IF NOT EXISTS idx_user_invitations_tenant_id ON ff_user_invitations(tenant_id);
                CREATE INDEX IF NOT EXISTS idx_user_invitations_app_code ON ff_user_invitations(app_code);
                CREATE INDEX IF NOT EXISTS idx_user_invitations_onboarding_id ON ff_user_invitations(onboarding_id);
                CREATE INDEX IF NOT EXISTS idx_user_invitations_email ON ff_user_invitations(email);
                CREATE INDEX IF NOT EXISTS idx_user_invitations_token ON ff_user_invitations(invitation_token);
                CREATE INDEX IF NOT EXISTS idx_user_invitations_status ON ff_user_invitations(status);
                CREATE INDEX IF NOT EXISTS idx_user_invitations_token_expiry ON ff_user_invitations(token_expiry);
                CREATE UNIQUE INDEX IF NOT EXISTS idx_user_invitations_email_onboarding ON ff_user_invitations(email, onboarding_id) WHERE is_valid = TRUE;
                CREATE UNIQUE INDEX IF NOT EXISTS idx_user_invitations_token_unique ON ff_user_invitations(invitation_token) WHERE is_valid = TRUE;
            ");

            // Add foreign key constraints
            db.Ado.ExecuteCommand(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints 
                                  WHERE constraint_name = 'fk_user_invitations_onboarding') THEN
                        ALTER TABLE ff_user_invitations 
                        ADD CONSTRAINT fk_user_invitations_onboarding 
                        FOREIGN KEY (onboarding_id) REFERENCES ff_onboarding(id) ON DELETE CASCADE;
                    END IF;
                    
                    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints 
                                  WHERE constraint_name = 'fk_user_invitations_user') THEN
                        ALTER TABLE ff_user_invitations 
                        ADD CONSTRAINT fk_user_invitations_user 
                        FOREIGN KEY (user_id) REFERENCES ff_users(id) ON DELETE SET NULL;
                    END IF;
                END $$;
            ");
        }

        /// <summary>
        /// Rollback migration - drop user invitations table
        /// </summary>
        public static void Down(ISqlSugarClient db)
        {
            db.Ado.ExecuteCommand(@"
                DROP TABLE IF EXISTS ff_user_invitations;
            ");
        }
    }
}