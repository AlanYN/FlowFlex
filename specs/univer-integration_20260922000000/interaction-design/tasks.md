# Tasks — 交互设计任务清单

> 阶段：interaction-design  
> 模块：univer-integration  
> 创建日期：2026-09-22

---

## 交互设计任务

- [x] ID-01：读取需求分析 context.md，确认页面列表
- [x] ID-02：制定文档列表页交互规范（IR-L-01 ~ IR-L-12）
- [x] ID-03：制定新建文档弹窗交互规范（IR-C-01 ~ IR-C-08）
- [x] ID-04：制定编辑器顶栏交互规范（IR-E-01 ~ IR-E-10）
- [x] ID-05：定义 Design Tokens（颜色/字体/圆角/间距）
- [x] ID-06：绘制三个核心组件线框图（DocumentCard / CreateDocumentDialog / EditorToolbar）
- [x] ID-07：绘制三张页面级线框图（列表页 / 编辑器页 / 空状态）
- [x] ID-08：整理关键交互约定（6条）

---

## 待技术方案阶段决策的约定

| ID | 约定项 | 建议 |
|----|--------|------|
| TC-01 | DocumentCard 组件放入 `components/UniverEditor/` 还是 `views/document/list/`？ | 建议放 `views/document/list/DocumentCard.vue`，因为仅列表页使用 |
| TC-02 | `EditorToolbar` 是否抽成全局组件？ | 建议内联在编辑器页，不抽全局 |
| TC-03 | 协同头像数据如何实时同步？ | 由 `useCollaboration` composable 维护 `collaborators` 列表，通过 JOIN/LEAVE 消息更新 |
