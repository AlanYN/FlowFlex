using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Create access tokens table migration
    /// Version: 2.0.0
    /// Created: 2025-01-01
    /// Description: Create access tokens table for JWT token management
    /// </summary>
    public class CreateAccessTokenTable_20250101000010
    {
        /// <summary>
        /// Execute migration - create access tokens table
        /// </summary>
        public static void Up(ISqlSugarClient db)
        {
            // Create access tokens table
            db.Ado.ExecuteCommand(@"
                CREATE TABLE IF NOT EXISTS ff_access_tokens (
                    id BIGINT NOT NULL PRIMARY KEY,
                    tenant_id VARCHAR(32) NOT NULL DEFAULT 'default',
                    app_code VARCHAR(32) NOT NULL DEFAULT 'DEFAULT',
                    jti VARCHAR(100) NOT NULL,
                    user_id BIGINT NOT NULL,
                    user_email VARCHAR(100) NOT NULL,
                    token_hash VARCHAR(500) NOT NULL,
                    issued_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    expires_at TIMESTAMPTZ NOT NULL,
                    is_active BOOLEAN DEFAULT TRUE,
                    token_type VARCHAR(20) DEFAULT 'login',
                    revoked_at TIMESTAMPTZ,
                    revoke_reason VARCHAR(50),
                    last_used_at TIMESTAMPTZ,
                    issued_ip VARCHAR(45),
                    user_agent VARCHAR(500),
                    is_valid BOOLEAN DEFAULT TRUE,
                    create_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    modify_date TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
                    create_by VARCHAR(50) DEFAULT 'SYSTEM',
                    modify_by VARCHAR(50) DEFAULT 'SYSTEM',
                    create_user_id BIGINT DEFAULT 0,
                    modify_user_id BIGINT DEFAULT 0
                );
                
                -- Create indexes for performance
                CREATE UNIQUE INDEX IF NOT EXISTS idx_access_tokens_jti ON ff_access_tokens(jti) WHERE is_valid = true;
                CREATE INDEX IF NOT EXISTS idx_access_tokens_user_id ON ff_access_tokens(user_id);
                CREATE INDEX IF NOT EXISTS idx_access_tokens_user_email ON ff_access_tokens(user_email);
                CREATE INDEX IF NOT EXISTS idx_access_tokens_expires_at ON ff_access_tokens(expires_at);
                CREATE INDEX IF NOT EXISTS idx_access_tokens_issued_at ON ff_access_tokens(issued_at);
                CREATE INDEX IF NOT EXISTS idx_access_tokens_is_active ON ff_access_tokens(is_active);
                CREATE INDEX IF NOT EXISTS idx_access_tokens_token_type ON ff_access_tokens(token_type);
                CREATE INDEX IF NOT EXISTS idx_access_tokens_tenant_id ON ff_access_tokens(tenant_id);
                CREATE INDEX IF NOT EXISTS idx_access_tokens_revoked_at ON ff_access_tokens(revoked_at);
                CREATE INDEX IF NOT EXISTS idx_access_tokens_last_used_at ON ff_access_tokens(last_used_at);
                
                -- Create composite indexes for common queries
                CREATE INDEX IF NOT EXISTS idx_access_tokens_user_active ON ff_access_tokens(user_id, is_active, expires_at) WHERE is_valid = true;
                CREATE INDEX IF NOT EXISTS idx_access_tokens_cleanup ON ff_access_tokens(expires_at, revoked_at) WHERE NOT is_active;
                
                -- Add comments for documentation
                COMMENT ON TABLE ff_access_tokens IS 'Store all JWT access tokens for validation and management';
                COMMENT ON COLUMN ff_access_tokens.jti IS 'JWT ID (jti claim) - unique identifier for the token';
                COMMENT ON COLUMN ff_access_tokens.user_id IS 'User ID who owns the token';
                COMMENT ON COLUMN ff_access_tokens.user_email IS 'User email for reference';
                COMMENT ON COLUMN ff_access_tokens.token_hash IS 'Hashed token value for security';
                COMMENT ON COLUMN ff_access_tokens.issued_at IS 'When the token was issued';
                COMMENT ON COLUMN ff_access_tokens.expires_at IS 'When the token expires';
                COMMENT ON COLUMN ff_access_tokens.is_active IS 'Whether the token is currently active';
                COMMENT ON COLUMN ff_access_tokens.token_type IS 'Type of token (login, refresh, portal-access)';
                COMMENT ON COLUMN ff_access_tokens.revoked_at IS 'When the token was revoked';
                COMMENT ON COLUMN ff_access_tokens.revoke_reason IS 'Reason for token revocation';
                COMMENT ON COLUMN ff_access_tokens.last_used_at IS 'Last time this token was used';
                COMMENT ON COLUMN ff_access_tokens.issued_ip IS 'IP address where token was issued';
                COMMENT ON COLUMN ff_access_tokens.user_agent IS 'User agent when token was issued';
            ");

            Console.WriteLine("Access tokens table created successfully");
        }

        /// <summary>
        /// Rollback migration - drop access tokens table
        /// </summary>
        public static void Down(ISqlSugarClient db)
        {
            // Drop table
            db.Ado.ExecuteCommand(@"
                DROP TABLE IF EXISTS ff_access_tokens;
            ");

            Console.WriteLine("Access tokens table dropped successfully");
        }
    }
} 