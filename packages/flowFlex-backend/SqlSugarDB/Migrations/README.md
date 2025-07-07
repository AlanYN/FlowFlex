# FlowFlex Database Migrations

This directory contains the database migration files for the FlowFlex project, used to initialize and manage the database structure.

## Migration Files Overview

### 1. Initial Migration
- **File**: `20250101000000_InitialCreate.cs`
- **Description**: Creates core table structures including users, workflows, stages, checklists, questionnaires, onboarding management, and other main tables
- **Included Tables**:
  - `ff_users` - Users table
  - `ff_workflow` - Workflows table
  - `ff_stage` - Stages table
  - `ff_checklist` - Checklists table
  - `ff_checklist_task` - Checklist tasks table
  - `ff_checklist_task_completion` - Checklist task completion table
  - `ff_questionnaire` - Questionnaires table
  - `ff_questionnaire_section` - Questionnaire sections table
  - `ff_questionnaire_answers` - Questionnaire answers table
  - `ff_onboarding` - Onboarding management table

### 2. Extended Tables Migration
- **File**: `20250101000001_CreateRemainingTables.cs`
- **Description**: Creates remaining auxiliary tables and version control tables
- **Included Tables**:
  - `ff_onboarding_file` - Onboarding files table
  - `ff_operation_change_log` - Operation change log table
  - `ff_internal_notes` - Internal notes table
  - `ff_stage_completion_log` - Stage completion log table
  - `ff_static_field_values` - Static field values table
  - `ff_workflow_version` - Workflow version table
  - `ff_stage_version` - Stage version table



## Usage

### 1. Automatic Initialization
Database migrations are automatically executed when the project starts, no manual operation required.

### 2. Manual Migration Execution

```csharp
// Manual execution in code
var migrationManager = new MigrationManager(db);
await migrationManager.RunMigrationsAsync();
```

### 3. Using Extension Methods

```csharp
// Initialize database
await serviceProvider.InitializeDatabaseAsync();

// Or execute directly on SqlSugar client
await db.EnsureDatabaseCreatedAsync();
```

### 4. Database Reset

```csharp
// Reset database (drop all tables and recreate)
await migrationManager.RollbackAllMigrationsAsync();
await migrationManager.RunMigrationsAsync();
```

## Database Structure Features

### 1. Common Fields
All tables include the following common fields:
- `id` - Primary key ID (BIGSERIAL)
- `tenant_id` - Tenant ID (VARCHAR(32))
- `is_valid` - Is valid (BOOLEAN)
- `create_date` - Create date (TIMESTAMPTZ)
- `modify_date` - Modify date (TIMESTAMPTZ)
- `create_by` - Created by (VARCHAR(50))
- `modify_by` - Modified by (VARCHAR(50))
- `create_user_id` - Creator user ID (BIGINT)
- `modify_user_id` - Modifier user ID (BIGINT)

### 2. Automatic Time Updates
All tables are configured with triggers to automatically update the `modify_date` field when records are updated.

### 3. Foreign Key Constraints
Complete foreign key constraint relationships are established to ensure data integrity.

### 4. Index Optimization
Indexes are created for commonly queried fields to improve query performance.

## Data Management

The migration system creates the database structure without any initial sample data. All data should be created through the application's proper business flows:

- **Users**: Register through the application's user registration system
- **Workflows**: Create through the workflow management interface
- **Checklists**: Design through the checklist builder
- **Questionnaires**: Build through the questionnaire designer
- **Onboarding Records**: Create through the onboarding process

## Important Notes

1. **Database Version**: Supports PostgreSQL 15+
2. **Character Encoding**: Uses UTF-8 encoding
3. **Timezone**: All time fields use UTC timezone
4. **Naming Convention**: All table names use `ff_` prefix
5. **Migration History**: Migration execution history is recorded in the `__migration_history` table

## Troubleshooting

### 1. Migration Failure
- Check database connection string
- Confirm database user permissions
- View detailed error information

### 2. Repeat Execution
- Migration system automatically checks executed migrations
- Will not repeat executed migrations

### 3. Rollback Operation
- Can use `MigrationManager.RollbackMigrations()` to roll back
- Note: Rollback will delete all data

## Version Information

- **Version**: 2.0.0
- **Creation Date**: 2025-01-01
- **Compatibility**: .NET 8.0, SqlSugar 5.1.4+, PostgreSQL 15+ 