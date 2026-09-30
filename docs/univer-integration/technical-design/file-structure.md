# 文件结构 — Univer 集成

> 最后更新：2026-09-22  
> 仅列出本次新增文件，现有文件用 `(现有，需修改)` 标注

---

## 前端（packages/flowFlex-common/src/app/）

```
src/app/
│
├── components/
│   └── UniverEditor/                           ← 🆕 新增目录
│       ├── UniverSheet.vue                     ← 🆕 Sheet 编辑器组件
│       ├── UniverDoc.vue                       ← 🆕 Doc 编辑器组件
│       ├── UniverSlide.vue                     ← 🆕 Slide 编辑器组件（P1）
│       └── index.ts                            ← 🆕 统一导出
│
├── composables/
│   └── useCollaboration.ts                     ← 🆕 协同 Composable
│
├── apis/
│   └── ow/
│       ├── documentSnapshot.ts                 ← 🆕 快照 API
│       └── documentOperationLog.ts             ← 🆕 操作日志 API
│
├── stores/
│   └── modules/
│       ├── documentList.ts                     ← 🆕 列表页 Store
│       └── documentEditor.ts                   ← 🆕 编辑器页 Store
│
├── views/
│   └── document/                               ← 🆕 新增目录
│       ├── list/
│       │   ├── index.vue                       ← 🆕 文档列表页
│       │   ├── DocumentCard.vue                ← 🆕 文档卡片组件
│       │   └── CreateDocumentDialog.vue        ← 🆕 新建文档弹窗
│       └── editor/
│           └── index.vue                       ← 🆕 文档编辑器页
│
└── router/
    └── routers/
        └── modules/
            └── document.ts                     ← 🆕 /documents 路由模块

── 需修改的现有文件 ──

tailwind.config.ts                              ← ✏️ 添加 important: '#app'
vite.config.ts                                  ← ✏️ 添加 optimizeDeps.include
```

---

## 后端（packages/flowFlex-backend/）

```
packages/flowFlex-backend/
│
├── Domain/
│   ├── Entities/OW/
│   │   ├── DocumentSnapshot.cs                 ← 🆕 快照实体
│   │   └── DocumentOperationLog.cs             ← 🆕 操作日志实体
│   └── Repository/OW/
│       ├── IDocumentSnapshotRepository.cs      ← 🆕 Repository 接口
│       └── IDocumentOperationLogRepository.cs  ← 🆕 Repository 接口
│
├── SqlSugarDB/
│   ├── Repositories/OW/
│   │   ├── DocumentSnapshotRepository.cs       ← 🆕 Repository 实现
│   │   └── DocumentOperationLogRepository.cs   ← 🆕 Repository 实现
│   └── Migrations/
│       └── Migration_20260922000001_CreateDocumentTables.cs  ← 🆕 建表 Migration
│
├── Application.Contracts/
│   ├── Dtos/OW/DocumentSnapshot/
│   │   ├── DocumentSnapshotInputDto.cs         ← 🆕
│   │   ├── DocumentSnapshotOutputDto.cs        ← 🆕
│   │   ├── DocumentSnapshotQueryRequest.cs     ← 🆕
│   │   ├── DocumentOperationLogInputDto.cs     ← 🆕
│   │   └── DocumentOperationLogOutputDto.cs    ← 🆕
│   └── IServices/OW/
│       ├── IDocumentSnapshotService.cs         ← 🆕
│       └── IDocumentOperationLogService.cs     ← 🆕
│
├── Application/
│   ├── Services/OW/
│   │   ├── DocumentSnapshotService.cs          ← 🆕
│   │   └── DocumentOperationLogService.cs      ← 🆕
│   └── Maps/
│       └── DocumentSnapshotMapProfile.cs       ← 🆕 AutoMapper Profile
│
├── WebApi/
│   ├── Controllers/OW/
│   │   ├── DocumentSnapshotController.cs       ← 🆕
│   │   └── DocumentOperationLogController.cs   ← 🆕
│   └── Hubs/                                   ← 🆕 新增目录
│       └── CollabHub.cs                        ← 🆕 SignalR 协同 Hub
│
└── Tests/FlowFlex.Tests/OW/
    ├── DocumentSnapshotServiceTests.cs         ← 🆕 单元测试
    └── DocumentOperationLogServiceTests.cs     ← 🆕 单元测试

── 需修改的现有文件 ──

SqlSugarDB/Migrations/MigrationManager.cs      ← ✏️ 末尾追加 Migration 注册
WebApi/Program.cs                              ← ✏️ AddSignalR() + MapHub<CollabHub>
Domain/Shared/Const/PermissionConsts.cs        ← ✏️ 新增 Document 权限常量类
```

---

## 新增文件汇总

| 类型 | 文件数 |
|------|--------|
| 前端新增 | 12 个 |
| 后端新增 | 18 个 |
| 需修改现有文件 | 5 个 |
| **合计** | **35 个** |
