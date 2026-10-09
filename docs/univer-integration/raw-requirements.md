# 原始需求存档 — univer-integration

> 本文件追加写入，保留所有历史原始需求，不修改已有记录。

---

## [2026-09-22] 初始需求

**来源：** 用户交互 + docs/INTEGRATION_GUIDE.md + docs/UNIVER_INTEGRATION_PLAN.md

**原始描述：**

将 Univer（开源类 Office 在线文档编辑框架，Apache 2.0 协议）集成到 FlowFlex 项目。

核心需求：
1. 支持三种文档类型：Sheets（类 Excel）、Docs（类 Word）、Slides（类 PPT，P1）
2. 使用独立的「文档管理」菜单，路径 `/documents`
3. 列表页展示所有文档，支持搜索、类型筛选、排序、分页
4. 新建文档：弹窗选类型 + 填写标题，创建后跳转编辑器
5. 编辑器页全屏展示 Univer，顶栏含标题编辑 + 保存按钮 + 协同头像
6. 文档快照存储到后端（PostgreSQL JSONB），按 tenant 隔离
7. 每次编辑操作异步记录操作日志
8. 多人实时协同编辑（SignalR + 乐观锁，简单版本，不做完整 OT）
9. 不破坏现有 FlowFlex 功能

**补充约束：**
- 技术栈：Vue 3 + .NET 8（现有技术栈，不引入新框架）
- Tailwind 与 Univer 样式冲突需用 `important: '#app'` 处理
- `workbookData` 不放入 Vue 响应式系统（性能约束）
