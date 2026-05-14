# Requirements: Action Field Lookup — 测试覆盖范围

## 测试目标

验证 Action Field Lookup 功能的正确性、容错性和兼容性，确保：

1. Lookup 配置能正确保存和回显
2. 运行时能正确拉取第三方选项列表
3. 降级处理正常工作
4. 现有功能不受影响

---

## 测试覆盖范围

### 后端测试

| 模块                   | 覆盖内容                                                 |
| ---------------------- | -------------------------------------------------------- |
| IntegrationHttpClient  | 4 种认证方式 + additionalHeaders 合并 + 超时 + 日志记录  |
| FieldLookupService     | MappingConfig 解析 + JSONPath 提取 + 并行执行 + 降级处理 |
| ActionExecutionService | 执行链路集成 + lookup 结果存储 + 无 lookup 时零影响      |
| ActionController       | Preview API + MappingConfig 保存 API                     |

### 前端测试

| 模块               | 覆盖内容                                         |
| ------------------ | ------------------------------------------------ |
| LookupConfigPanel  | 配置输入 + Test 按钮 + 预览结果 + Custom Headers |
| ActionConfigDialog | Lookup 列 + expand 展开/收起 + 保存/回显         |

---

## 质量目标

| 指标               | 目标                                                |
| ------------------ | --------------------------------------------------- |
| 后端单元测试覆盖率 | ≥ 80%（IntegrationHttpClient + FieldLookupService） |
| API 集成测试       | Preview + MappingConfig 两个 endpoint 全覆盖        |
| 降级场景覆盖       | 超时、网络错误、非 2xx、JSON 解析失败               |
| 兼容性验证         | 无 lookup 配置时行为不变                            |
