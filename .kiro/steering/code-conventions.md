# Code Conventions

## TenantId and AppCode Source Convention

For `/api/ow/` endpoints, the CRUD operations must get TenantId and AppCode from HTTP headers:

- **TenantId**: Get from `X-Tenant-Id` header
- **AppCode**: Get from `X-App-Code` header

### Implementation

The `UserContext` is automatically populated from HTTP headers in `ServiceCollectionExtensions.cs`:

```csharp
// Priority: headers > JWT claims > AppContext > defaults
var tenantId = tenantIdHeader ?? tenantIdClaim?.Value ?? appContext?.TenantId ?? "default";
var appCode = appCodeHeader ?? appCodeClaim?.Value ?? appContext?.AppCode ?? "default";
```

### Usage in Services

Services should use `UserContext` to get TenantId and AppCode:

```csharp
// Good ✅
entity.TenantId = _userContext.TenantId ?? "default";
entity.AppCode = _userContext.AppCode ?? "default";

// Bad ❌ - Hardcoding values
entity.TenantId = "some-tenant";
entity.AppCode = "some-app";
```

### Query Filtering

Always filter queries by TenantId for tenant isolation:

```csharp
// Good ✅
var entities = await _db.Queryable<Entity>()
    .Where(e => e.TenantId == _userContext.TenantId)
    .ToListAsync();

// Bad ❌ - No tenant filtering
var entities = await _db.Queryable<Entity>()
    .ToListAsync();
```

---

## AppCode Naming Convention

- **NEVER** use uppercase `"DEFAULT"` for AppCode values in code
- **ALWAYS** use lowercase `"default"` for AppCode values
- This applies to:
  - Entity default values
  - Constants
  - String literals
  - Configuration values

### Examples

#### Good ✅
```csharp
public string AppCode { get; set; } = "default";

if (appCode == "default")
{
    // ...
}

const string DEFAULT_APP_CODE = "default";
```

#### Bad ❌
```csharp
public string AppCode { get; set; } = "DEFAULT";

if (appCode == "DEFAULT")
{
    // ...
}

const string DEFAULT_APP_CODE = "DEFAULT";
```

### Reason
- Consistency with database values
- Avoid case-sensitivity issues in queries
- Maintain uniform naming convention across the codebase

---

## Migration File Naming Convention

Database migration files in `SqlSugarDB/Migrations` folder must follow a consistent naming pattern.

### Naming Format

```
Migration_{YYYYMMDDHHMMSS}_{DescriptiveName}.cs
```

Or for special cases (initial create, seed data, etc.):

```
{DescriptiveName}_{YYYYMMDDHHMMSS}.cs
```

### Rules

1. **Timestamp Format**: Use `YYYYMMDDHHMMSS` format (e.g., `20260108000002`)
   - Year (4 digits) + Month (2 digits) + Day (2 digits) + Hour (2 digits) + Minute (2 digits) + Second (2 digits)
   - For most migrations, use `000001`, `000002`, etc. for the time portion within the same day

2. **Descriptive Name**: Use PascalCase with clear description of what the migration does
   - Good: `AddIsExternalImportToOnboardingFile`, `CreateIntegrationTables`, `AddCaseCodeToOnboarding`
   - Bad: `Update1`, `Fix`, `Changes`

3. **Class Name**: Must match the file name (without `.cs` extension)
   - File: `Migration_20260108000002_AddIsExternalImportToOnboardingFile.cs`
   - Class: `Migration_20260108000002_AddIsExternalImportToOnboardingFile`

4. **Subfolder Organization**: Domain-specific migrations can be placed in subfolders
   - Example: `SqlSugarDB/Migrations/Integration/Migration_20260108000001_CreateIntegrationApiLogTable.cs`

### Examples

#### Good ✅
```
Migration_20260108000001_CreateIntegrationApiLogTable.cs
Migration_20260108000002_AddIsExternalImportToOnboardingFile.cs
Migration_20251124000001_CreateIntegrationTables.cs
Migration_20251105000001_AddCaseCodeToOnboarding.cs
```

#### Bad ❌
```
Migration1.cs
AddNewColumn.cs
20260108_Update.cs
migration_20260108000001_createtable.cs  // lowercase
```

### Registration in MigrationManager

All migrations must be registered in `MigrationManager.cs`:

```csharp
var migrations = new[]
{
    // ... existing migrations
    ("20260108000002_AddIsExternalImportToOnboardingFile", (Action)(() => Migration_20260108000002_AddIsExternalImportToOnboardingFile.Up(_db)))
};
```

- The migration ID string should match the timestamp and name portion of the file
- Migrations are executed in the order they appear in the array
- Keep migrations in chronological order
