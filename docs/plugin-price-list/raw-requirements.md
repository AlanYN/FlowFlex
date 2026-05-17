# 原始需求存档

## 2026-05-14 — OW-629: UF Online Rate Sheet Setup - Price List Plugin

**来源**：JIRA OW-629，创建者 Amanda Li，指派给 Kai Li  
**Sprint**：OW.2026.04/24-05/14

### 原始需求摘要

将 Price List 前端原型作为插件部署到 WFE，增加后端存储和权限管理，实现一个 Case 对应一份 Price List 数据的持久化方案。

**业务背景**：
- Sales 目前手打 Excel rate sheet 上传到 WFE Stage 1，Billing team 经常理解不一致
- Sales 签约的定价方案，很多设置 BNP 系统不支持，后续 setup 时才发现做不了
- 缺乏标准化管控，没有系统强制约束

**解决方案**：
- 开发 Price List 配置页面，作为 WFE 插件嵌入 Billing Setup Stage
- Sales 只能从系统支持的 342 个标准 Billing Item 中选择
- 生成的 Price List 数据结构化存储，未来可直接调用 BNP API 自动 setup billing

**交付物**：
- [x] 前端原型（Vue 3，Amanda 已完成）
- [x] 打包好的 dist 文件
- [x] 后端需求文档
- [ ] 后端 API 开发
- [ ] 权限对接
- [ ] 部署到 WFE

**关键决策（与用户确认）**：
1. 认证方式：复用 WFE 现有 JWT，同域名 cookie 自动携带
2. 数据库：在 FlowFlex 现有 PostgreSQL 加表，表名前缀 `ff_plugin_`
3. 一个 Case = 一份 Price List
4. Submit 后只读，撤回功能本期不做但预留口子
5. 部署方式：方案 A — dist 文件放 wwwroot/plugins/price-list/
