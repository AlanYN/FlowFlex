# Code Conventions

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
