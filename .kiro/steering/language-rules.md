# Language Rules

## Communication Language
- Use **Chinese (中文)** for all conversations and explanations with the user
- Documentation files (like API docs) should be written in Chinese

## Code Language
- Use **English** for all code-related content:
  - Variable names
  - Function names
  - Class names
  - Code comments
  - Commit messages
  - Error messages in code

## Examples

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
