-- Dynamic Data Tables Migration
-- Date: 2025-12-25
-- Description: Create tables for dynamic field functionality

-- Business Data Table (main table for dynamic data)
CREATE TABLE IF NOT EXISTS ff_business_data (
    id              BIGINT PRIMARY KEY,
    module_id       INT NOT NULL,
    internal_data   JSONB,
    tenant_id       VARCHAR(50),
    app_code        VARCHAR(50),
    is_valid        BOOLEAN DEFAULT TRUE,
    create_date     TIMESTAMPTZ,
    create_by       VARCHAR(100),
    create_user_id  BIGINT,
    modify_date     TIMESTAMPTZ,
    modify_by       VARCHAR(100),
    modify_user_id  BIGINT
);

-- Data Value Table (EAV pattern for storing dynamic field values)
CREATE TABLE IF NOT EXISTS ff_data_value (
    id                  BIGINT PRIMARY KEY,
    module_id           INT NOT NULL,
    tenant_id           VARCHAR(50),
    app_code            VARCHAR(50),
    business_id         BIGINT NOT NULL,
    field_id            BIGINT NOT NULL,
    field_name          VARCHAR(100),
    data_type           INT,
    long_value          BIGINT,
    int_value           INT,
    double_value        DOUBLE PRECISION,
    text_value          TEXT,
    varchar100_value    VARCHAR(100),
    varchar500_value    VARCHAR(500),
    varchar_value       VARCHAR(5000),
    bool_value          BOOLEAN,
    date_time_value     TIMESTAMPTZ,
    string_list_value   JSONB,
    is_valid            BOOLEAN DEFAULT TRUE
);

-- Define Field Table (field definitions)
CREATE TABLE IF NOT EXISTS ff_define_field (
    id                  BIGINT PRIMARY KEY,
    module_id           INT NOT NULL,
    display_name        VARCHAR(200),
    field_name          VARCHAR(100) NOT NULL,
    description         TEXT,
    data_type           INT NOT NULL,
    source_type         INT,
    source_name         VARCHAR(100),
    format_id           BIGINT,
    validate_id         BIGINT,
    ref_field_id        BIGINT,
    is_system_define    BOOLEAN DEFAULT FALSE,
    is_static           BOOLEAN DEFAULT FALSE,
    is_display_field    BOOLEAN DEFAULT FALSE,
    is_must_use         BOOLEAN DEFAULT FALSE,
    is_required         BOOLEAN DEFAULT FALSE,
    is_table_must_show  BOOLEAN DEFAULT FALSE,
    is_hidden           BOOLEAN DEFAULT FALSE,
    is_computed         BOOLEAN DEFAULT FALSE,
    allow_edit          BOOLEAN DEFAULT TRUE,
    allow_edit_item     BOOLEAN DEFAULT TRUE,
    sort                INT DEFAULT 0,
    additional_info     JSONB,
    tenant_id           VARCHAR(50),
    app_code            VARCHAR(50),
    is_valid            BOOLEAN DEFAULT TRUE,
    create_date         TIMESTAMPTZ,
    create_by           VARCHAR(100),
    create_user_id      BIGINT,
    modify_date         TIMESTAMPTZ,
    modify_by           VARCHAR(100),
    modify_user_id      BIGINT
);

-- Field Group Table
CREATE TABLE IF NOT EXISTS ff_field_group (
    id                  BIGINT PRIMARY KEY,
    module_id           INT NOT NULL,
    group_name          VARCHAR(200),
    sort                INT DEFAULT 0,
    is_system_define    BOOLEAN DEFAULT FALSE,
    is_default          BOOLEAN DEFAULT FALSE,
    fields              BIGINT[],
    tenant_id           VARCHAR(50),
    app_code            VARCHAR(50),
    is_valid            BOOLEAN DEFAULT TRUE,
    create_date         TIMESTAMPTZ,
    create_by           VARCHAR(100),
    create_user_id      BIGINT,
    modify_date         TIMESTAMPTZ,
    modify_by           VARCHAR(100),
    modify_user_id      BIGINT
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_ff_business_data_module_tenant ON ff_business_data(module_id, tenant_id, app_code);
CREATE INDEX IF NOT EXISTS idx_ff_business_data_is_valid ON ff_business_data(is_valid);

CREATE INDEX IF NOT EXISTS idx_ff_data_value_business_id ON ff_data_value(business_id);
CREATE INDEX IF NOT EXISTS idx_ff_data_value_field_name ON ff_data_value(field_name);
CREATE INDEX IF NOT EXISTS idx_ff_data_value_module_tenant ON ff_data_value(module_id, tenant_id, app_code);
CREATE INDEX IF NOT EXISTS idx_ff_data_value_is_valid ON ff_data_value(is_valid);

CREATE INDEX IF NOT EXISTS idx_ff_define_field_module ON ff_define_field(module_id);
CREATE INDEX IF NOT EXISTS idx_ff_define_field_module_tenant ON ff_define_field(module_id, tenant_id, app_code);
CREATE INDEX IF NOT EXISTS idx_ff_define_field_field_name ON ff_define_field(field_name);
CREATE INDEX IF NOT EXISTS idx_ff_define_field_is_valid ON ff_define_field(is_valid);

CREATE INDEX IF NOT EXISTS idx_ff_field_group_module ON ff_field_group(module_id);
CREATE INDEX IF NOT EXISTS idx_ff_field_group_module_tenant ON ff_field_group(module_id, tenant_id, app_code);
CREATE INDEX IF NOT EXISTS idx_ff_field_group_is_valid ON ff_field_group(is_valid);

-- Comments
COMMENT ON TABLE ff_business_data IS 'Business data main table for dynamic fields';
COMMENT ON TABLE ff_data_value IS 'Data value table using EAV pattern';
COMMENT ON TABLE ff_define_field IS 'Field definition table';
COMMENT ON TABLE ff_field_group IS 'Field group table';
