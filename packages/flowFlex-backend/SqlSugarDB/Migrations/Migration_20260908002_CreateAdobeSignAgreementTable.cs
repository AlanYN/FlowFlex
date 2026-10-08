using SqlSugar;

namespace FlowFlex.SqlSugarDB.Migrations
{
    /// <summary>
    /// Create ff_adobe_sign_agreement table for Adobe Sign integration (OW-731)
    /// </summary>
    public static class Migration_20260908002_CreateAdobeSignAgreementTable
    {
        public static void Up(ISqlSugarClient db)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS ff_adobe_sign_agreement (
                    id                   BIGINT          NOT NULL PRIMARY KEY,
                    agreement_id         VARCHAR(200)    NOT NULL,
                    onboarding_id        BIGINT          NOT NULL,
                    stage_id             BIGINT          NOT NULL,
                    source_file_id       BIGINT          NOT NULL,
                    signed_file_id       BIGINT,
                    audit_trail_file_id  BIGINT,
                    status               VARCHAR(50)     NOT NULL DEFAULT 'Awaiting',
                    signers              JSONB,
                    signing_order        VARCHAR(20)     NOT NULL DEFAULT 'Sequential',
                    expiration_days      INT             NOT NULL DEFAULT 30,
                    message              VARCHAR(1000),
                    requested_by         BIGINT          NOT NULL,
                    completed_date       TIMESTAMPTZ,
                    create_date          TIMESTAMPTZ,
                    modify_date          TIMESTAMPTZ,
                    create_by            VARCHAR(200),
                    modify_by            VARCHAR(200),
                    create_user_id       BIGINT,
                    modify_user_id       BIGINT,
                    is_valid             BOOLEAN         NOT NULL DEFAULT TRUE,
                    app_code             VARCHAR(100),
                    tenant_id            VARCHAR(100)
                );

                CREATE INDEX IF NOT EXISTS idx_adobe_sign_agreement_source_file_id
                    ON ff_adobe_sign_agreement(source_file_id);

                CREATE INDEX IF NOT EXISTS idx_adobe_sign_agreement_onboarding_id
                    ON ff_adobe_sign_agreement(onboarding_id);

                CREATE INDEX IF NOT EXISTS idx_adobe_sign_agreement_agreement_id
                    ON ff_adobe_sign_agreement(agreement_id);

                CREATE INDEX IF NOT EXISTS idx_adobe_sign_agreement_app_code_tenant
                    ON ff_adobe_sign_agreement(app_code, tenant_id);

                COMMENT ON TABLE ff_adobe_sign_agreement IS 'Adobe Sign e-signature agreements (OW-731)';
                COMMENT ON COLUMN ff_adobe_sign_agreement.agreement_id IS 'Agreement ID returned by Adobe Sign API';
                COMMENT ON COLUMN ff_adobe_sign_agreement.source_file_id IS 'Original PDF file ID in ff_onboarding_file';
                COMMENT ON COLUMN ff_adobe_sign_agreement.signed_file_id IS 'Completed signed PDF saved back to ff_onboarding_file';
                COMMENT ON COLUMN ff_adobe_sign_agreement.audit_trail_file_id IS 'Audit Trail PDF saved to ff_onboarding_file';
                COMMENT ON COLUMN ff_adobe_sign_agreement.signers IS 'JSON array of signers with status and timestamps';
                COMMENT ON COLUMN ff_adobe_sign_agreement.signing_order IS 'Sequential or Parallel';
            ";

            db.Ado.ExecuteCommand(sql);
        }

        public static void Down(ISqlSugarClient db)
        {
            var sql = @"DROP TABLE IF EXISTS ff_adobe_sign_agreement;";
            db.Ado.ExecuteCommand(sql);
        }
    }
}
