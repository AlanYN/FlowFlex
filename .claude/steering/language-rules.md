---
inclusion: always
---

# Language Rules

## Communication Language
- Use **Chinese (中文)** for all conversations and explanations with the user
- Documentation files (specs, docs, design documents) should be written in Chinese

## Code Language
- Use **English** for all code-related content:
  - Variable names, function names, class names
  - Code comments (including XML doc comments and JSDoc)
  - Commit messages
  - Error messages in code
  - Interface and type names
  - Enum values and constants
  - File and folder names (in source code)

## Backend (C#) Examples

### Good ✅
```csharp
/// <summary>
/// Get user email from Microsoft Graph API
/// </summary>
private async Task<string> GetUserEmailAsync(string accessToken)
{
    // Call Graph API to get user profile
    var response = await _httpClient.GetAsync("/me");
    return response.Email;
}
```

### Bad ❌
```csharp
/// <summary>
/// 从 Microsoft Graph API 获取用户邮箱
/// </summary>
private async Task<string> 获取用户邮箱(string 访问令牌)
{
    // 调用 Graph API 获取用户信息
    var response = await _httpClient.GetAsync("/me");
    return response.Email;
}
```

## Frontend (Vue 3 + TypeScript) Examples

### Good ✅
```typescript
/**
 * Fetch onboarding list with pagination
 * @param params - Query parameters
 */
export function getOnboardingList(params: OnboardingQueryParams) {
  return defHttp.get<PagedResult<OnboardingModel>>({ url: Api().onboarding, params })
}
```

```vue
<script setup lang="ts">
// Filter onboarding records by status
const filteredList = computed(() =>
  list.value.filter((item) => item.status === selectedStatus.value)
)
</script>
```

### Bad ❌
```typescript
/**
 * 获取入职列表（分页）
 */
export function 获取入职列表(参数: 入职查询参数) {
  return defHttp.get({ url: Api().onboarding, params: 参数 })
}
```
