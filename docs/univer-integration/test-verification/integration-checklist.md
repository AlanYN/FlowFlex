# 联调检查清单 — Univer 集成

> 生成日期：2026-09-22  
> 单元测试状态：✅ 14/14 通过（TV-BE-01 ~ TV-BE-02）

---

## TV-STYLE-01：Tailwind 样式回归（必须手工验证）

修改了 `tailwind.config.ts`（`important` 从数组追加了 `'#app'`），需验证以下页面样式无变化：

- [ ] 工作流列表页 `/onboard/onboardWorkflow`
- [ ] 问卷管理页 `/onboard/questionnaire`
- [ ] 清单页 `/onboard/checklist`
- [ ] 任意包含 `el-dropdown` 的页面（确认浮层 z-index 正常）
- [ ] 任意包含 `el-select` 的页面（确认下拉弹出正常）
- [ ] 任意包含 `el-dialog` 的页面（确认弹窗显示正常）

---

## TV-STYLE-02：文档模块样式验证

- [ ] `/documents/list` 文档列表页卡片网格布局正常（4列/3列/2列响应式）
- [ ] 空状态展示正确（插图 + 文案 + 新建按钮）
- [ ] 卡片 hover 时背景变灰 + 操作按钮出现
- [ ] `/documents/:unitId/edit` 顶栏高度 56px，编辑器区占满剩余高度
- [ ] Univer Sheet 编辑器内部 toolbar 不被 Tailwind 样式影响
- [ ] Univer Doc 编辑器内部样式不被影响

---

## TV-API-01~03：后端 API 端点验证

> 可用 Swagger UI（`/swagger`）或 Postman 验证

- [ ] `GET ow/document-snapshots/v1/:unitId` — 返回文档 + revision
- [ ] `GET ow/document-snapshots/v1/not-exist` — 返回 404
- [ ] `POST ow/document-snapshots/v1` — 创建空白文档，返回 unitId
- [ ] `POST ow/document-snapshots/v1/:unitId/save`（首次）— INSERT，返回 true
- [ ] `POST ow/document-snapshots/v1/:unitId/save`（二次）— UPDATE，返回 true
- [ ] `DELETE ow/document-snapshots/v1/:id` — 软删除，DB is_valid = FALSE
- [ ] `POST ow/document-snapshots/v1/query` — 含 keyword + docType 过滤，total 正确
- [ ] `POST ow/document-operation-logs/v1` — 记录日志，返回 ID
- [ ] `GET ow/document-operation-logs/v1/:unitId` — 返回分页日志
- [ ] `GET ow/document-operation-logs/v1/:unitId/revision` — 返回正确版本号
- [ ] 任意端点无 token — 返回 401

---

## TV-HUB-01：SignalR 协同验证

- [ ] 浏览器 A 打开 `/documents/:unitId/edit`，观察 WebSocket 连接建立（DevTools Network）
- [ ] 浏览器 B 打开同一文档，观察 A 的顶栏头像中出现 B 的头像
- [ ] A 编辑单元格，B 的编辑器 1 秒内同步变更
- [ ] 模拟并发（双标签同时编辑），确认 cs_rej 触发并最终收敛

---

## TV-FE-01：CreateDocumentDialog 验证

- [ ] 名称为空点确认 → 显示错误提示，弹窗不关闭
- [ ] 填写名称 + 选 Doc 类型点创建 → 创建成功跳转编辑器
- [ ] 弹窗打开时默认选中 Sheet 类型
- [ ] 取消按钮关闭弹窗并清空表单

---

## TV-FE-02~04：编辑器页功能验证

- [ ] Ctrl+S 触发保存，按钮短暂显示「✓ 已保存」
- [ ] 标题点击后可行内编辑，失焦保存；空标题自动还原
- [ ] 有未保存变更时点击返回 → 弹出三选确认框
- [ ] 「保存并离开」→ 保存后导航；「不保存离开」→ 直接导航；「取消」→ 留在编辑器

---

## TV-ISS-01：z-index 浮层检查（ISS-01）

- [ ] `el-dropdown` 菜单在文档列表页正常弹出（DocumentCard 右键菜单）
- [ ] `el-dialog`（新建文档弹窗、ElMessageBox）正常显示在最前层
- [ ] `el-tooltip`（协同头像 tooltip）正常显示

---

## TV-ISS-02：Web Worker MIME 验证（ISS-02，Staging 环境）

- [ ] 在 Staging 环境打开 Doc 编辑器，DevTools Network 查看 Worker 文件返回 `application/javascript`
- [ ] 无 `ERR_INVALID_MIME_TYPE` 等 Worker 相关错误

---

## 数据库验证（启动后自动执行）

- [ ] 应用启动后 `ff_document_snapshot` 表已创建
- [ ] 应用启动后 `ff_document_operation_log` 表已创建
- [ ] 复合唯一索引 `idx_ff_doc_snapshot_unit_id` 已创建

```sql
-- 验证 SQL
SELECT table_name, index_name
FROM pg_indexes
WHERE table_name IN ('ff_document_snapshot', 'ff_document_operation_log')
ORDER BY table_name, index_name;
```
