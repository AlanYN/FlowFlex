# 组件树 — Univer 集成

> 最后更新：2026-09-22

---

## 一、组件层级图

```
LAYOUT（现有布局组件）
├── views/document/list/index.vue          [DocumentList]
│   ├── views/document/list/DocumentCard.vue  [DocumentCard] × N
│   └── views/document/list/CreateDocumentDialog.vue  [CreateDocumentDialog]
│
└── views/document/editor/index.vue        [DocumentEditor]
    ├── （内联）EditorToolbar
    ├── components/UniverEditor/UniverSheet.vue  [UniverSheet]  ← docType=Sheet 时渲染
    ├── components/UniverEditor/UniverDoc.vue    [UniverDoc]    ← docType=Doc 时渲染
    └── components/UniverEditor/UniverSlide.vue  [UniverSlide]  ← docType=Slide 时渲染（P1）
```

---

## 二、组件详细规格

### DocumentList（`views/document/list/index.vue`）

**职责：** 文档列表页，搜索/筛选/排序/分页/新建入口

**依赖 Store：** `useDocumentListStore`  
**依赖 API：** `querySnapshots`、`deleteSnapshot`

**模板结构：**
```
<div class="document-list-page">
  <div class="page-header">  ← 标题 + 新建按钮
  <div class="toolbar">      ← 搜索 + 类型筛选 + 排序
  <div class="card-grid">    ← v-for DocumentCard
  <div class="empty-state">  ← v-if 无数据
  <el-pagination>            ← 分页组件
  <CreateDocumentDialog v-model="dialogVisible" @created="onCreated" />
```

---

### DocumentCard（`views/document/list/DocumentCard.vue`）

**职责：** 单个文档卡片，展示信息 + 操作菜单

**Props：**
```typescript
interface Props {
  id: string
  unitId: string
  title: string
  docType: 'Sheet' | 'Doc' | 'Slide'
  modifyDate: string
  createBy: string
}
```

**Emits：**
```typescript
defineEmits<{
  click: []
  rename: [newTitle: string]
  delete: []
}>()
```

**模板结构：**
```
<div class="doc-card" @click="emit('click')">
  <button class="card-menu-btn">⋯</button>         ← hover 可见
  <div class="card-icon">{{ typeIcon }}</div>
  <div class="card-title">{{ title }}</div>
  <div class="card-meta">时间 · 创建人</div>
  <el-dropdown>                                      ← 操作菜单
    <el-dropdown-item @click="emit('rename',...)">重命名</el-dropdown-item>
    <el-dropdown-item @click="confirmDelete">删除</el-dropdown-item>
  </el-dropdown>
```

---

### CreateDocumentDialog（`views/document/list/CreateDocumentDialog.vue`）

**职责：** 新建文档弹窗，填写名称 + 选择类型

**Props：** `modelValue: boolean`（v-model 控制显隐）  
**Emits：** `update:modelValue`、`created(unitId: string)`

**模板结构：**
```
<el-dialog v-model="visible" width="480px" title="新建文档">
  <el-input autofocus v-model="docName" />
  <div class="type-cards">
    <div v-for="type in types" :class="['type-card', { selected: docType === type.value }]">
  </div>
  <template #footer>
    <el-button @click="close">取消</el-button>
    <el-button type="primary" :loading="creating" @click="handleCreate">创 建</el-button>
  </template>
</el-dialog>
```

---

### DocumentEditor（`views/document/editor/index.vue`）

**职责：** 编辑器页，顶栏 + 根据 docType 动态渲染编辑器

**依赖 Store：** `useDocumentEditorStore`  
**依赖 Composable：** `useCollaboration`（在 onMounted 中注入）

**模板结构：**
```
<div class="editor-page">
  <!-- 顶栏 56px -->
  <div class="editor-toolbar">
    <span class="back-btn" @click="handleBack">← 返回</span>
    <span class="title-display" @click="startEdit">{{ store.title }}</span>
    <input class="title-input" v-show="editing" @blur="endEdit" />
    <div class="collaborators">
      <el-avatar v-for="c in store.collaborators" ... />
    </div>
    <el-button :loading="store.saving" @click="handleSave">保存</el-button>
  </div>
  <!-- 编辑器区域 -->
  <UniverSheet v-if="store.docType==='Sheet'" ref="editorRef"
    :height="`calc(100vh - 56px)`" @mutation="store.logMutation" @ready="onReady" />
  <UniverDoc v-else-if="store.docType==='Doc'" ref="editorRef" ... />
  <UniverSlide v-else-if="store.docType==='Slide'" ref="editorRef" ... />
</div>
```

---

### UniverSheet（`components/UniverEditor/UniverSheet.vue`）

**职责：** 封装 Univer Sheets 全功能编辑器

**Props：**
```typescript
interface Props {
  workbookData?: object    // IWorkbookData JSON（不放 ref，直接传原始对象）
  height?: string          // 默认 'calc(100vh - 56px)'
  readonly?: boolean       // 默认 false
}
```

**Emits：**
```typescript
defineEmits<{
  mutation: [info: { mutationId: string; params: unknown; timestamp: number }]
  ready: []
}>()
```

**defineExpose：**
```typescript
defineExpose({
  getWorkbookData: () => univerInstance?.univerAPI.getActiveWorkbook()?.save(),
  getInstance: () => univerInstance,
})
```

**关键实现（onMounted）：**
```typescript
univerInstance = createUniver({
  locale: LocaleType.ZH_CN,
  locales: { [LocaleType.ZH_CN]: sheetsZhCN },
  presets: [
    UniverSheetsCorePreset({ container: containerRef.value! }),
    UniverSheetsDrawingPreset(), UniverSheetsConditionalFormattingPreset(),
    UniverSheetsFilterPreset(), UniverSheetsHyperLinkPreset(),
    UniverSheetsDataValidationPreset(), UniverSheetsFindReplacePreset(),
    UniverSheetsNotePreset(), UniverSheetsSortPreset(),
    UniverSheetsTablePreset(), UniverSheetsThreadCommentPreset(),
  ],
})
univerInstance.univerAPI.createWorkbook(props.workbookData ?? {})
// 监听 MUTATION
const { ICommandService, CommandType } = await import('@univerjs/core')
commandService.onCommandExecuted((info) => {
  if (info.type !== CommandType.MUTATION) return
  emit('mutation', { mutationId: info.id, params: info.params, timestamp: Date.now() })
})
emit('ready')
```

---

### UniverDoc（`components/UniverEditor/UniverDoc.vue`）

结构与 UniverSheet 对应，区别：
- 使用 `UniverDocsCorePreset`、`UniverDocsDrawingPreset` 等 Docs 系列 Preset
- 额外注册 `UniverDocsLayoutWorkerPlugin`（Web Worker 排版）
- defineExpose 暴露 `getDocumentData()`

---

### UniverSlide（`components/UniverEditor/UniverSlide.vue`）

结构与 UniverSheet 对应，区别：
- 无 Preset，使用底层 `new Univer()` 实例直接 `registerPlugin(...)`
- P1 功能，优先级低于 Sheet 和 Doc

---

## 三、Composable 规格

### useCollaboration（`composables/useCollaboration.ts`）

```typescript
export function useCollaboration(
  univerAPI: FUniver,
  unitId: string,
  userId: string,
  options?: {
    wsUrl?: string            // 默认 wss://{location.host}/collab/{unitId}
    onJoin?: (userId: string, name: string) => void
    onLeave?: (userId: string) => void
  }
): {
  ws: ISocket
  sendCursor: (selection: string) => void
  disconnect: () => void
  baseRev: Ref<number>
  isConnected: Ref<boolean>
  collaborators: Ref<CollaboratorInfo[]>  // JOIN/LEAVE 消息驱动更新
}
```

---

## 四、Store 规格

### documentListStore（`stores/modules/documentList.ts`）

```typescript
// id: 'item-wfe-document-list'
interface DocumentListState {
  list: DocumentSnapshotOutputDto[]
  total: number
  page: number            // 默认 1
  pageSize: number        // 默认 20
  keyword: string
  docType: string         // '' | 'Sheet' | 'Doc' | 'Slide'
  sortBy: string          // 'modify_date'(默认) | 'create_date' | 'title'
  loading: boolean
}
actions: loadList() / deleteDocument(id) / renameDocument(id, title) / resetFilter()
```

### documentEditorStore（`stores/modules/documentEditor.ts`）

```typescript
// id: 'item-wfe-document-editor'
interface DocumentEditorState {
  unitId: string | null
  docType: 'Sheet' | 'Doc' | 'Slide'
  snapshotData: object | null   // ⚠️ 不放 ref，直接赋值
  title: string
  loading: boolean
  saving: boolean
  hasUnsaved: boolean
  collaborators: CollaboratorInfo[]
  currentRevision: number
  error: string | null
}
actions: loadDocument(unitId) / saveDocument(data) / logMutation(info) / setCollaborator / removeCollaborator
```
