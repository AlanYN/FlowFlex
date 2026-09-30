# Univer 集成指南

> 适用技术栈：Vue3 + Vite + Tailwind CSS + Element Plus（前端）/ C#（后端）  
> Univer 版本：1.0.0-rc.0 | 协议：Apache License 2.0（免费商业使用）

---

## 一、项目概览

Univer 是一个开源的类 Office 在线文档编辑框架，支持三种文档类型：

| 类型       | 说明               | 核心包                         |
| ---------- | ------------------ | ------------------------------ |
| **Sheets** | 类 Excel 电子表格  | `@univerjs/preset-sheets-core` |
| **Docs**   | 类 Word 富文本文档 | `@univerjs/preset-docs-core`   |
| **Slides** | 类 PPT 演示文稿    | 底层插件组合（尚无 preset）    |

### Sheets 支持的功能模块

- 公式引擎（含 Web Worker 异步计算）
- 条件格式、数据验证、筛选/排序
- 查找替换、超链接、绘图/图片
- 数字格式、批注/评论、便签
- 表格、十字线高亮

### Docs 支持的功能模块

- 富文本编辑、超链接、绘图
- 目录、查找替换、批注/评论

---

## 二、本地开发环境启动

> 当前仓库使用 pnpm 12.5.1（由 Corepack 管理），在 Windows PowerShell 下存在 `.ps1` 脚本缺失的 bug。

**临时解决方案（直接调用 pnpm 的 .mjs 入口）：**

```powershell
# 安装依赖（在项目根目录执行）
node "C:\Users\<用户名>\AppData\Local\pnpm\.tools\pnpm\12.5.1\node_modules\pnpm\bin\pnpm.mjs" install

# 启动开发服务器（访问 http://localhost:5173）
node "C:\Users\<用户名>\AppData\Local\pnpm\.tools\pnpm\12.5.1\node_modules\pnpm\bin\pnpm.mjs" --filter univer-examples dev
```

**根本解决方案（重装 pnpm，修复 PowerShell 脚本）：**

```powershell
Invoke-WebRequest https://get.pnpm.io/install.ps1 -UseBasicParsing | Invoke-Expression
# 重启终端后直接使用 pnpm dev
```

### Windows 路径兼容性修复（vite.config.ts）

已修复两处 Windows 路径问题：

| 位置                     | 问题                                                                                                | 修复方式                                     |
| ------------------------ | --------------------------------------------------------------------------------------------------- | -------------------------------------------- |
| `VIRTUAL_LOCALE_MODULES` | `fileURLToPath()` 生成 `C:\...` 路径，Vite 虚拟模块 re-export 时报 `ERR_UNSUPPORTED_ESM_URL_SCHEME` | 改用 `new URL(...).href` 保持 `file://` 格式 |
| `spawn()` 脚本参数       | 脚本路径传成了 `file://` URL，Node 将其当相对路径拼接到 cwd                                         | 改用 `fileURLToPath()` 转回文件系统路径      |

**规则总结：**

- `--import` 参数 → 必须是 `file://` URL
- `spawn()` 的脚本路径参数 → 必须是文件系统路径（`C:\...`）
- Vite/Rollup 的模块 re-export specifier → 必须是 `file://` URL

---

## 三、前端集成到 Vue 项目

### 3.1 包的使用方式：npm 包 vs 本地源码包

**方式一：使用 npm 发布包（推荐）**

适合绝大多数情况。直接从 npm 安装，无需关心 Univer 源码：

```bash
npm install @univerjs/presets @univerjs/preset-sheets-core
# ... 其余包同理
```

当前仓库版本 `1.0.0-rc.0` 已同步发布到 npm，包名与 `packages/` 目录下的包名完全一致。

**方式二：使用当前仓库本地源码包**

适合需要修改 Univer 源码、调试内部逻辑的情况。需要在你自己项目的 `package.json` 中用绝对路径或 `file:` 协议引用，并先构建 Univer：

```bash
# 1. 先在 Univer 仓库构建所有包
cd c:\Users\zhenyan.wang\项目文件\univer
pnpm build

# 2. 在你的项目里用本地路径引用
```

```json
// 你的项目 package.json
{
  "dependencies": {
    "@univerjs/presets": "file:../univer/node_modules/@univerjs/presets",
    "@univerjs/preset-sheets-core": "file:../univer/presets/packages/preset-sheets-core"
  }
}
```

> **结论**：日常集成直接用 npm 包即可，不需要本地源码包。只有当你需要修改 Univer 内部逻辑时才考虑本地包。

---

### 3.2 安装依赖（npm 方式）

以下是当前 Univer 示例项目所有功能对应的完整依赖：

```bash
# ---- 基础框架（必须）----
npm install @univerjs/presets

# ---- Sheets（电子表格）全功能 ----
npm install @univerjs/preset-sheets-core                    # 核心：编辑/公式/UI
npm install @univerjs/preset-sheets-drawing                 # 绘图/图片
npm install @univerjs/preset-sheets-conditional-formatting  # 条件格式
npm install @univerjs/preset-sheets-filter                  # 筛选
npm install @univerjs/preset-sheets-hyper-link              # 超链接
npm install @univerjs/preset-sheets-data-validation         # 数据验证
npm install @univerjs/preset-sheets-find-replace            # 查找替换
npm install @univerjs/preset-sheets-note                    # 便签
npm install @univerjs/preset-sheets-sort                    # 排序
npm install @univerjs/preset-sheets-table                   # 表格
npm install @univerjs/preset-sheets-thread-comment          # 批注/评论

# ---- Docs（富文本文档）全功能 ----
npm install @univerjs/preset-docs-core                      # 核心：编辑/排版/UI
npm install @univerjs/preset-docs-drawing                   # 绘图/图片
npm install @univerjs/preset-docs-hyper-link                # 超链接
npm install @univerjs/preset-docs-thread-comment            # 批注/评论

# ---- Slides（演示文稿）---- 尚无 preset，使用底层包
npm install @univerjs/slides @univerjs/slides-ui
npm install @univerjs/docs @univerjs/docs-ui
npm install @univerjs/drawing
npm install @univerjs/engine-render
npm install @univerjs/ui

# ---- 协同编辑（可选）----
npm install @univerjs/network                               # WebSocket 服务
```

---

### 3.3 Tailwind CSS 隔离配置（重要）

Tailwind 的 base/reset 样式会破坏 Univer 内部 UI，**必须配置前缀或 important 选择器**：

```js
// tailwind.config.js
export default {
  content: ["./src/**/*.{vue,js,ts}"],
  prefix: "tw-", // 所有 Tailwind 类改为 tw-flex、tw-text-sm 等
  // 或者：
  // important: '#app',
};
```

---

### 3.4 Vite 配置

```ts
// vite.config.ts
import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
  plugins: [vue()],
  optimizeDeps: {
    include: [
      "@univerjs/presets",
      "@univerjs/preset-sheets-core",
      "@univerjs/preset-docs-core",
    ],
  },
});
```

---

### 3.5 电子表格组件封装（全功能版）

```vue
<!-- components/UniverSheet.vue -->
<template>
  <div ref="containerRef" :style="{ width: '100%', height }"></div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from "vue";
import { createUniver, LocaleType } from "@univerjs/presets";
import { UniverSheetsCorePreset } from "@univerjs/preset-sheets-core";
import { UniverSheetsDrawingPreset } from "@univerjs/preset-sheets-drawing";
import { UniverSheetsConditionalFormattingPreset } from "@univerjs/preset-sheets-conditional-formatting";
import { UniverSheetsFilterPreset } from "@univerjs/preset-sheets-filter";
import { UniverSheetsHyperLinkPreset } from "@univerjs/preset-sheets-hyper-link";
import { UniverSheetsDataValidationPreset } from "@univerjs/preset-sheets-data-validation";
import { UniverSheetsFindReplacePreset } from "@univerjs/preset-sheets-find-replace";
import { UniverSheetsNotePreset } from "@univerjs/preset-sheets-note";
import { UniverSheetsSortPreset } from "@univerjs/preset-sheets-sort";
import { UniverSheetsTablePreset } from "@univerjs/preset-sheets-table";
import { UniverSheetsThreadCommentPreset } from "@univerjs/preset-sheets-thread-comment";
import sheetsZhCN from "@univerjs/preset-sheets-core/locales/zh-CN";

const props = defineProps<{
  workbookData?: object; // 从后端获取的 IWorkbookData JSON
  height?: string;
}>();

const emit = defineEmits<{
  mutation: [
    info: {
      userId: string;
      timestamp: number;
      mutationId: string;
      params: unknown;
    },
  ];
}>();

const containerRef = ref<HTMLDivElement>();
let univerInstance: ReturnType<typeof createUniver> | null = null;

onMounted(async () => {
  univerInstance = createUniver({
    locale: LocaleType.ZH_CN,
    locales: { [LocaleType.ZH_CN]: sheetsZhCN },
    presets: [
      UniverSheetsCorePreset({ container: containerRef.value! }),
      UniverSheetsDrawingPreset(),
      UniverSheetsConditionalFormattingPreset(),
      UniverSheetsFilterPreset(),
      UniverSheetsHyperLinkPreset(),
      UniverSheetsDataValidationPreset(),
      UniverSheetsFindReplacePreset(),
      UniverSheetsNotePreset(),
      UniverSheetsSortPreset(),
      UniverSheetsTablePreset(),
      UniverSheetsThreadCommentPreset(),
    ],
  });

  const { univerAPI, univer } = univerInstance;
  univerAPI.createWorkbook(props.workbookData ?? {});

  // 操作日志监听
  const { ICommandService, CommandType } = await import("@univerjs/core");
  const commandService = univer.__getInjector().get(ICommandService);
  commandService.onCommandExecuted((info) => {
    if (info.type !== CommandType.MUTATION) return;
    emit("mutation", {
      userId: "", // 替换为你的用户 ID
      timestamp: Date.now(),
      mutationId: info.id,
      params: info.params,
    });
  });
});

onBeforeUnmount(() => {
  univerInstance?.univer.dispose();
  univerInstance = null;
});

defineExpose({
  getWorkbookData: () => univerInstance?.univerAPI.getActiveWorkbook()?.save(),
});
</script>
```

---

### 3.6 文档组件封装（全功能版）

```vue
<!-- components/UniverDoc.vue -->
<template>
  <div ref="containerRef" :style="{ width: '100%', height }"></div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from "vue";
import {
  createUniver,
  LocaleType,
  UniverInstanceType,
} from "@univerjs/presets";
import { UniverDocsCorePreset } from "@univerjs/preset-docs-core";
import { UniverDocsDrawingPreset } from "@univerjs/preset-docs-drawing";
import { UniverDocsHyperLinkPreset } from "@univerjs/preset-docs-hyper-link";
import { UniverDocsThreadCommentPreset } from "@univerjs/preset-docs-thread-comment";
import { UniverDocsLayoutWorkerPlugin } from "@univerjs/docs";
import docsZhCN from "@univerjs/preset-docs-core/locales/zh-CN";

const props = defineProps<{
  documentData?: object; // 从后端获取的 IDocumentData JSON
  height?: string;
}>();

const emit = defineEmits<{
  mutation: [
    info: {
      userId: string;
      timestamp: number;
      mutationId: string;
      params: unknown;
    },
  ];
}>();

const containerRef = ref<HTMLDivElement>();
let univerInstance: ReturnType<typeof createUniver> | null = null;

onMounted(async () => {
  univerInstance = createUniver({
    locale: LocaleType.ZH_CN,
    locales: { [LocaleType.ZH_CN]: docsZhCN },
    presets: [
      UniverDocsCorePreset({ container: containerRef.value!, toc: true }),
      UniverDocsDrawingPreset(),
      UniverDocsHyperLinkPreset(),
      UniverDocsThreadCommentPreset(),
    ],
  });

  const { univerAPI, univer } = univerInstance;

  // 开启 Web Worker 排版（提升大文档性能）
  univer.registerPlugin(UniverDocsLayoutWorkerPlugin, {
    workerFactory: () =>
      new Worker(new URL("@univerjs/docs/worker", import.meta.url), {
        type: "module",
      }),
  });

  univer.createUnit(UniverInstanceType.UNIVER_DOC, props.documentData ?? {});

  // 操作日志监听
  const { ICommandService, CommandType } = await import("@univerjs/core");
  const commandService = univer.__getInjector().get(ICommandService);
  commandService.onCommandExecuted((info) => {
    if (info.type !== CommandType.MUTATION) return;
    emit("mutation", {
      userId: "", // 替换为你的用户 ID
      timestamp: Date.now(),
      mutationId: info.id,
      params: info.params,
    });
  });
});

onBeforeUnmount(() => {
  univerInstance?.univer.dispose();
  univerInstance = null;
});

defineExpose({
  getDocumentData: () => univerInstance?.univerAPI.getActiveDocument()?.save(),
});
</script>
```

---

### 3.7 演示文稿组件封装（Slides）

> Slides 目前没有 preset 封装，需要使用底层插件 API：

```vue
<!-- components/UniverSlide.vue -->
<template>
  <div ref="containerRef" :style="{ width: '100%', height }"></div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from "vue";
import { Univer, UniverInstanceType, LocaleType } from "@univerjs/core";
import { FUniver } from "@univerjs/core/facade";
import { UniverRenderEnginePlugin } from "@univerjs/engine-render";
import { UniverUIPlugin } from "@univerjs/ui";
import { UniverDocsPlugin } from "@univerjs/docs";
import { UniverDocsUIPlugin } from "@univerjs/docs-ui";
import { UniverDrawingPlugin } from "@univerjs/drawing";
import { UniverSlidesPlugin } from "@univerjs/slides";
import { UniverSlidesUIPlugin } from "@univerjs/slides-ui";
import "@univerjs/ui/facade";

const props = defineProps<{
  slideData?: object;
  height?: string;
}>();

const containerRef = ref<HTMLDivElement>();
let univer: Univer | null = null;

onMounted(() => {
  univer = new Univer({ locale: LocaleType.ZH_CN });

  univer.registerPlugin(UniverRenderEnginePlugin);
  univer.registerPlugin(UniverUIPlugin, { container: containerRef.value! });
  univer.registerPlugin(UniverDocsPlugin);
  univer.registerPlugin(UniverDocsUIPlugin);
  univer.registerPlugin(UniverDrawingPlugin);
  univer.registerPlugin(UniverSlidesPlugin);
  univer.registerPlugin(UniverSlidesUIPlugin);

  univer.createUnit(UniverInstanceType.UNIVER_SLIDE, props.slideData ?? {});
});

onBeforeUnmount(() => {
  univer?.dispose();
  univer = null;
});
</script>
```

---

### 3.8 在页面中使用

```vue
<template>
  <el-card>
    <template #header>
      <span>文档编辑</span>
      <el-button type="primary" @click="save">保存</el-button>
    </template>
    <UniverSheet
      ref="sheetRef"
      :workbook-data="workbookData"
      height="600px"
      @mutation="onMutation"
    />
  </el-card>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import UniverSheet from "@/components/UniverSheet.vue";

const sheetRef = ref();
const workbookData = ref();
const documentId = "your-document-id"; // 替换为实际文档 ID

onMounted(async () => {
  // 从 C# 后端加载
  const res = await fetch(`/api/workbook/${documentId}`);
  workbookData.value = await res.json();
});

async function save() {
  const data = sheetRef.value?.getWorkbookData();
  await fetch(`/api/workbook/${documentId}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  });
  ElMessage.success("保存成功");
}

async function onMutation(info: {
  userId: string;
  timestamp: number;
  mutationId: string;
  params: unknown;
}) {
  await fetch("/api/operation-log", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ...info, documentId }),
  });
}
</script>
```

---

## 四、C# 后端实现

### 4.1 数据模型

```csharp
// 文档快照（存储完整 JSON）
public class WorkbookSnapshot
{
    public Guid Id { get; set; }
    public string UnitId { get; set; }       // Univer 文档 ID
    public string Title { get; set; }
    public string DataJson { get; set; }     // IWorkbookData JSON，直接存字符串
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; }
}

// 操作日志（记录每次编辑行为）
public class OperationLog
{
    public Guid Id { get; set; }
    public string UnitId { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public long Timestamp { get; set; }
    public string MutationId { get; set; }   // 操作类型 ID
    public string ParamsJson { get; set; }   // 操作参数（含修改的单元格/内容）
    public DateTime CreatedAt { get; set; }
}
```

### 4.2 API 接口

```csharp
[ApiController]
[Route("api/workbook")]
public class WorkbookController : ControllerBase
{
    // 加载文档
    [HttpGet("{unitId}")]
    public async Task<IActionResult> Get(string unitId)
    {
        var snapshot = await _repo.GetByUnitIdAsync(unitId);
        if (snapshot == null) return NotFound();
        return Content(snapshot.DataJson, "application/json");
    }

    // 保存文档
    [HttpPost("{unitId}")]
    public async Task<IActionResult> Save(string unitId, [FromBody] JsonElement data)
    {
        await _repo.UpsertAsync(unitId, data.GetRawText());
        return Ok();
    }
}

[ApiController]
[Route("api/operation-log")]
public class OperationLogController : ControllerBase
{
    // 记录操作日志
    [HttpPost]
    public async Task<IActionResult> Log([FromBody] OperationLogDto dto)
    {
        await _repo.AddAsync(new OperationLog {
            UnitId = dto.DocumentId,
            UserId = dto.UserId,
            MutationId = dto.MutationId,
            ParamsJson = JsonSerializer.Serialize(dto.Params),
            Timestamp = dto.Timestamp,
            CreatedAt = DateTime.UtcNow,
        });
        return Ok();
    }

    // 查询操作历史（谁/什么时候/改了什么）
    [HttpGet("{unitId}")]
    public async Task<IActionResult> GetLogs(
        string unitId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? userId)
    {
        var logs = await _repo.QueryAsync(unitId, from, to, userId);
        return Ok(logs);
    }
}
```

### 4.3 常见 MutationId 含义

| MutationId                          | 含义            | params 关键字段                      |
| ----------------------------------- | --------------- | ------------------------------------ |
| `sheet.mutation.set-range-values`   | 修改单元格值    | `cellValue`（新值）、`range`（位置） |
| `sheet.mutation.insert-row`         | 插入行          | `range`（插入位置）                  |
| `sheet.mutation.remove-row`         | 删除行          | `range`                              |
| `sheet.mutation.insert-col`         | 插入列          | `range`                              |
| `sheet.mutation.set-worksheet-name` | 修改 Sheet 名称 | `name`                               |
| `doc.mutation.retain-delete-apply`  | 文档文本变更    | `actions`（CRDT 格式）               |

---

## 五、整体数据流

```
用户打开页面
  └─ GET /api/workbook/{unitId}          ← 电子表格
  └─ GET /api/document/{unitId}          ← 文档
      └─ C# 返回对应格式的 JSON
          └─ univerAPI.createWorkbook()  /  univer.createUnit()  →  渲染编辑器

用户编辑内容
  └─ onCommandExecuted 触发（CommandType.MUTATION）
      └─ POST /api/operation-log
          └─ C# 写入 OperationLog 表（时间/用户/操作类型/参数）

用户点击保存
  └─ getWorkbookData() / getDocumentData()  →  获取完整当前数据
      └─ POST /api/workbook/{unitId} / POST /api/document/{unitId}
          └─ C# 更新对应快照表
```

### 三种数据格式对应的 C# 存储字段

| 文档类型 | 前端获取方法                            | 数据格式             | 建议字段类型    |
| -------- | --------------------------------------- | -------------------- | --------------- |
| Sheets   | `univerAPI.getActiveWorkbook()?.save()` | `IWorkbookData` JSON | `nvarchar(max)` |
| Docs     | `univerAPI.getActiveDocument()?.save()` | `IDocumentData` JSON | `nvarchar(max)` |
| Slides   | 暂无 Facade API，可序列化 Unit          | JSON                 | `nvarchar(max)` |

---

## 六、注意事项

| 事项                                      | 说明                                                                                  |
| ----------------------------------------- | ------------------------------------------------------------------------------------- |
| **不要用 reactive/ref 包裹 workbookData** | 数据量大，放入 Vue 响应式系统会严重影响性能，保存时调用 `.save()` 即可                |
| **容器必须有明确高度**                    | Univer 渲染依赖容器尺寸，不能是 `height: auto`                                        |
| **C# 存储格式**                           | 直接存 JSON 字符串到 `nvarchar(max)`，不需要解析，格式由前端版本管理                  |
| **多人协同**                              | 开源版本没有内置客户端插件，需要自行实现（见第七章）；商业版 Univer Server 提供此能力 |
| **Worker 支持**                           | 公式计算运行在 Web Worker，需要服务器响应正确的 MIME 类型（`application/javascript`） |
| **许可证**                                | Apache 2.0，商业使用免费，分发时保留 LICENSE 文件即可                                 |

---

## 七、多人实时协同编辑

### 7.1 当前项目已支持的部分

| 部分                 | 所在位置                                       | 说明                                                                                                                  |
| -------------------- | ---------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| **WebSocket 连接层** | `packages/network` → npm `@univerjs/network`   | `ISocketService` + `WebSocketService`，封装原生 WebSocket 为 RxJS Observable，通过 `univerAPI.createSocket(url)` 调用 |
| **协议类型定义**     | `packages/protocol` → npm `@univerjs/protocol` | `IChangeset`、`ICollaMsg`（含所有消息类型）、`ICombService`、`IUpdateCursor` 全部定义好                               |
| **fromCollab 标志**  | `packages/core` → npm `@univerjs/core`         | 命令执行选项，传入后自动跳过权限检查、跳过本地 UI 副作用、正确处理远端光标排版                                        |
| **协同模式开关**     | `presets` → npm `@univerjs/presets`            | `createUniver({ collaboration: true })` 自动禁用本地 undo/redo、权限服务，为协同让路                                  |
| **光标气泡渲染组件** | `packages/docs-ui` → npm `@univerjs/docs-ui`   | `TextBubbleShape` 已实现，可渲染带用户名的彩色光标标签                                                                |
| **官方接线示例**     | `packages/network/src/facade/f-univer.ts`      | 代码注释里有完整的 WebSocket ↔ Univer 协同示例                                                                        |

**尚未实现（需要自己做）：**

- 没有开箱即用的协同客户端插件
- 没有 OT（操作变换）/ 冲突解决逻辑
- Sheets 没有远端光标渲染
- 没有 Join/Leave 房间管理
- 没有快照版本加载逻辑

---

### 7.2 前端实现（Vue3）

```typescript
// composables/useCollaboration.ts
import { CommandType } from "@univerjs/core";
import type { FUniver } from "@univerjs/presets";

export function useCollaboration(
  univerAPI: FUniver,
  unitId: string,
  userId: string,
) {
  // 1. 建立 WebSocket 连接（需要先 import '@univerjs/network' 让 createSocket 可用）
  const ws = univerAPI.createSocket(`wss://your-server/collab/${unitId}`);
  let baseRev = 0; // 本地已确认的最新版本号

  // 2. 连接成功后加入房间
  ws.open$.subscribe(() => {
    ws.send(JSON.stringify({ cmd: "JOIN", unitId, userId }));
  });

  // 3. 接收服务器消息并应用到本地
  ws.message$.subscribe((event) => {
    const msg = JSON.parse(event.data);

    switch (msg.eventID) {
      // 收到其他人的操作 → 应用到本地
      case "new_cs": {
        const { mutations, revision } = msg.cs;
        for (const mutation of mutations) {
          univerAPI.executeCommand(
            mutation.id,
            JSON.parse(mutation.data),
            { fromCollab: true }, // ← 关键标志，跳过权限检查、不触发本地副作用
          );
        }
        baseRev = revision;
        break;
      }

      // 服务器确认我的操作 → 更新本地版本号
      case "cs_ack": {
        baseRev = msg.cs.revision;
        break;
      }

      // 服务器拒绝我的操作（并发冲突）→ 重新提交
      case "cs_rej": {
        ws.send(JSON.stringify({ cmd: "RETRY", cs: msg.cs }));
        break;
      }

      // 其他用户的光标位置
      case "update_cursor": {
        // renderRemoteCursor(msg.memberID, msg.selection) — 需自行实现
        break;
      }

      case "join":
        console.log(`${msg.name} 加入了协同`);
        break;
      case "leave":
        console.log(`${msg.name} 离开了协同`);
        break;
    }
  });

  // 4. 本地操作发送给服务器
  univerAPI.addEvent(
    univerAPI.Event.CommandExecuted,
    ({ id, type, params, options }) => {
      // 只同步本地产生的 Mutation，跳过来自协同的和仅本地的
      if (
        type !== CommandType.MUTATION ||
        options?.fromCollab ||
        options?.onlyLocal
      )
        return;

      const changeset = {
        unitID: unitId,
        userID: userId,
        baseRev,
        mutations: [{ id, data: JSON.stringify(params) }],
      };
      ws.send(JSON.stringify({ cmd: "INGEST", cs: changeset }));
    },
  );

  return { ws };
}
```

在 Vue 组件里使用（需额外 import network 包激活 createSocket）：

```vue
<script setup lang="ts">
import { onMounted, ref } from "vue";
import { createUniver, LocaleType } from "@univerjs/presets";
import { UniverSheetsCorePreset } from "@univerjs/preset-sheets-core";
import "@univerjs/network/facade"; // 激活 univerAPI.createSocket
import { useCollaboration } from "@/composables/useCollaboration";
import { useUserStore } from "@/stores/user";

const containerRef = ref<HTMLDivElement>();
const userStore = useUserStore();
const documentId = "your-document-id";

onMounted(async () => {
  const { univerAPI } = createUniver({
    collaboration: true, // 禁用本地 undo/权限服务，为协同让路
    locale: LocaleType.ZH_CN,
    presets: [UniverSheetsCorePreset({ container: containerRef.value! })],
  });

  const workbookData = await fetch(`/api/workbook/${documentId}`).then((r) =>
    r.json(),
  );
  univerAPI.createWorkbook(workbookData);

  // 接入协同
  useCollaboration(univerAPI, documentId, userStore.currentUser.id);
});
</script>
```

---

### 7.3 C# 后端实现（SignalR）

```csharp
// Models/ChangesetDto.cs
public class ChangesetDto
{
    public string UnitId { get; set; }
    public string UserId { get; set; }
    public int BaseRev { get; set; }
    public int Revision { get; set; }
    public List<MutationDto> Mutations { get; set; }
}

public class MutationDto
{
    public string Id { get; set; }     // mutation 命令 ID
    public string Data { get; set; }   // JSON 序列化的 params
}
```

```csharp
// Hubs/CollabHub.cs
public class CollabHub : Hub
{
    private readonly IChangesetRepository _repo;

    public CollabHub(IChangesetRepository repo) => _repo = repo;

    // 用户加入文档房间
    public async Task Join(string unitId, string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, unitId);

        await Clients.OthersInGroup(unitId).SendAsync("collab_msg", new {
            eventID = "join",
            memberID = Context.ConnectionId,
            name = userId
        });
    }

    // 用户断开连接
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // 需额外存储 connectionId → unitId 的映射来通知同组成员
        await base.OnDisconnectedAsync(exception);
    }

    // 接收并广播 Changeset（核心逻辑）
    public async Task Ingest(ChangesetDto cs)
    {
        var currentRev = await _repo.GetRevision(cs.UnitId);

        // 并发冲突检测（乐观锁）
        if (cs.BaseRev != currentRev)
        {
            await Clients.Caller.SendAsync("collab_msg", new {
                eventID = "cs_rej", cs
            });
            return;
        }

        // 分配新版本号并持久化
        cs.Revision = currentRev + 1;
        await _repo.SaveChangeset(cs);

        // 确认给提交方
        await Clients.Caller.SendAsync("collab_msg", new {
            eventID = "cs_ack", cs
        });

        // 广播给同组其他人
        await Clients.OthersInGroup(cs.UnitId).SendAsync("collab_msg", new {
            eventID = "new_cs", cs
        });
    }

    // 广播光标位置
    public async Task UpdateCursor(string unitId, string selection)
    {
        await Clients.OthersInGroup(unitId).SendAsync("collab_msg", new {
            eventID = "update_cursor",
            unitID = unitId,
            memberID = Context.ConnectionId,
            selection
        });
    }
}
```

```csharp
// Program.cs
builder.Services.AddSignalR();
app.MapHub<CollabHub>("/collab/{unitId}");
```

---

### 7.4 整体数据流

```
用户 A 编辑单元格
  → CommandExecuted 触发 (type=MUTATION, fromCollab=false)
  → ws.send({ cmd:'INGEST', cs:{ baseRev:5, mutations:[...] } })
      → C# SignalR Hub 收到
          → 校验 baseRev == currentRev（乐观锁）
          → 写入 changeset 日志表（版本 6）
          → cs_ack → 用户A（更新 baseRev=6）
          → new_cs → 用户B、C

用户 B 收到 new_cs
  → executeCommand(mutation.id, params, { fromCollab: true })
      → 跳过权限检查
      → 不触发本地 UI 副作用
      → 正常渲染变更
```

---

### 7.5 工作量评估

| 工作项                 | 难度       | 说明                                 |
| ---------------------- | ---------- | ------------------------------------ |
| WebSocket 接线（前端） | ⭐⭐       | 官方注释里已有骨架，照着扩展即可     |
| SignalR Hub（C#）      | ⭐⭐       | 标准 SignalR 群组广播模式            |
| 简单乐观锁冲突检测     | ⭐⭐       | 用版本号做冲突检测，拒绝后客户端重试 |
| 完整 OT 操作变换       | ⭐⭐⭐⭐⭐ | Google Docs 级别，一般业务场景可不做 |
| 光标共享（Docs）       | ⭐⭐⭐     | TextBubbleShape 已有，需要接线       |
| 光标共享（Sheets）     | ⭐⭐⭐⭐   | 需要自己做选区叠加层渲染             |

> **建议**：对于大多数业务场景，简单乐观锁 + 冲突时重试就够用了，不需要实现完整 OT 变换。

---

## 八、保存数据格式详解

### 8.1 Sheets 格式（`IWorkbookData`）

调用 `univerAPI.getActiveWorkbook()?.save()` 返回的完整结构：

```json
{
  "id": "workbook-01",
  "name": "我的表格",
  "appVersion": "1.0.0-rc.0",
  "locale": "zh-CN",
  "rev": 6,
  "sheetOrder": ["sheet1", "sheet2"],
  "styles": {
    "header": {
      "bl": 1,
      "bg": { "rgb": "#2563EB" },
      "cl": { "rgb": "#FFFFFF" },
      "ht": 2,
      "vt": 2
    }
  },
  "sheets": {
    "sheet1": {
      "id": "sheet1",
      "name": "Sheet1",
      "tabColor": "#2563EB",
      "hidden": 0,
      "rowCount": 40,
      "columnCount": 18,
      "zoomRatio": 1,
      "defaultColumnWidth": 96,
      "defaultRowHeight": 24,
      "freeze": { "xSplit": 0, "ySplit": 3, "startColumn": 0, "startRow": 3 },
      "mergeData": [
        { "startRow": 0, "endRow": 0, "startColumn": 0, "endColumn": 7 }
      ],
      "rowData": {
        "0": { "h": 42 },
        "29": { "h": 24, "hd": 1 }
      },
      "columnData": {
        "0": { "w": 135 },
        "10": { "w": 96, "hd": 1 }
      },
      "cellData": {
        "0": {
          "0": { "v": "标题文字", "s": "header" }
        },
        "4": {
          "0": { "v": "普通文字" },
          "1": { "v": 123.45, "t": 2, "s": "money" },
          "2": { "f": "=SUM(B5:B10)", "s": "money" },
          "3": { "v": true, "t": 4 }
        }
      }
    }
  },
  "resources": [
    {
      "name": "SHEET_DATA_VALIDATION_PLUGIN",
      "data": "{\"sheet1\":[{\"uid\":\"rule-1\",\"type\":\"list\",...}]}"
    },
    {
      "name": "SHEET_CONDITIONAL_FORMATTING_PLUGIN",
      "data": "{\"sheet1\":[...]}"
    },
    {
      "name": "SHEET_FILTER_PLUGIN",
      "data": "{\"sheet1\":{\"ref\":{...},\"filterColumns\":[...]}}"
    },
    {
      "name": "SHEET_UNIVER_THREAD_COMMENT_PLUGIN",
      "data": "{\"sheet1\":[{\"id\":\"thread-1\",\"ref\":\"B5\",\"text\":{...}}]}"
    }
  ]
}
```

#### 单元格 `ICellData` 字段说明

| 字段 | 类型                      | 含义                                                               |
| ---- | ------------------------- | ------------------------------------------------------------------ |
| `v`  | string / number / boolean | 单元格显示值                                                       |
| `f`  | string                    | 公式（如 `=SUM(A1:A10)`）                                          |
| `t`  | number                    | 值类型：1=字符串, 2=数字, 3=强制字符串, 4=布尔, 5=强制数字, 6=null |
| `s`  | string                    | 样式 ID，对应根级 `styles` 中的 key                                |
| `p`  | IDocumentData             | 富文本单元格（内嵌完整文档结构，用于超链接/混合格式）              |

#### 样式 `IStyleData` 常用字段

| 字段 | 含义       | 示例值                                               |
| ---- | ---------- | ---------------------------------------------------- |
| `bl` | 粗体       | `1`                                                  |
| `it` | 斜体       | `1`                                                  |
| `fs` | 字号（pt） | `12`                                                 |
| `cl` | 字体颜色   | `{ "rgb": "#FF0000" }`                               |
| `bg` | 背景色     | `{ "rgb": "#FFFF00" }`                               |
| `ht` | 水平对齐   | `1=左, 2=居中, 3=右`                                 |
| `vt` | 垂直对齐   | `1=顶, 2=居中, 3=底`                                 |
| `tb` | 文字换行   | `1=溢出, 2=自动换行, 3=截断`                         |
| `bd` | 边框       | `{ "t": {...}, "b": {...}, "l": {...}, "r": {...} }` |

---

### 8.2 Docs 格式（`IDocumentData`）

调用 `univerAPI.getActiveDocument()?.save()` 返回的完整结构：

```json
{
  "id": "doc-01",
  "title": "我的文档",
  "locale": "zh-CN",
  "rev": 3,
  "documentStyle": {
    "pageSize": { "width": 595, "height": 842 },
    "marginTop": 72,
    "marginBottom": 72,
    "marginLeft": 90,
    "marginRight": 90
  },
  "body": {
    "dataStream": "第一段文字\r第二段文字\n",
    "textRuns": [
      {
        "st": 0,
        "ed": 5,
        "ts": { "bl": 1, "fs": 16, "cl": { "rgb": "#1d4ed8" } }
      }
    ],
    "paragraphs": [
      {
        "startIndex": 9,
        "paragraphId": "p-1",
        "paragraphStyle": { "horizontalAlign": 2, "lineSpacing": 1.5 }
      }
    ],
    "sectionBreaks": [{ "startIndex": 18, "sectionId": "s-1" }],
    "customRanges": [
      {
        "startIndex": 0,
        "endIndex": 4,
        "rangeId": "link-1",
        "rangeType": 1,
        "properties": { "url": "https://example.com" }
      }
    ]
  },
  "drawings": {
    "img-1": {
      "drawingId": "img-1",
      "layoutType": 0,
      "source": "base64...",
      "...": "..."
    }
  },
  "resources": [
    {
      "name": "SHEET_UNIVER_THREAD_COMMENT_PLUGIN",
      "data": "{\"default_doc\":[{\"id\":\"c-1\",\"ref\":\"检查段落\",\"text\":{...}}]}"
    }
  ]
}
```

#### `body.dataStream` 控制字符说明

`dataStream` 是一个包含特殊控制字符的字符串，所有位置索引（`textRuns`、`paragraphs`、`sectionBreaks` 里的 `st`/`ed`/`startIndex`）都是相对它的字节偏移：

| 字符 | Unicode  | 含义                             |
| ---- | -------- | -------------------------------- |
| `\r` | `U+000D` | 段落结束符                       |
| `\n` | `U+000A` | 文档结束符（永远是最后一个字符） |
| `\f` | `U+000C` | 分页符                           |
| `\v` | `U+000B` | 分节符                           |
| `\b` | `U+0008` | 内嵌对象占位符（图片/绘图）      |

---

### 8.3 `resources` 扩展插件数据

所有功能插件（条件格式、数据验证、筛选、批注等）把自己的数据 JSON 序列化后存入 `resources` 数组，不影响核心数据结构：

| `name` 字段                           | 对应插件      |
| ------------------------------------- | ------------- |
| `SHEET_DATA_VALIDATION_PLUGIN`        | 数据验证规则  |
| `SHEET_CONDITIONAL_FORMATTING_PLUGIN` | 条件格式规则  |
| `SHEET_FILTER_PLUGIN`                 | 筛选配置      |
| `SHEET_NOTE_PLUGIN`                   | 便签内容      |
| `SHEET_UNIVER_THREAD_COMMENT_PLUGIN`  | 批注/评论线程 |

---

### 8.4 关键结论

| 结论                          | 说明                                                                                                        |
| ----------------------------- | ----------------------------------------------------------------------------------------------------------- |
| **格式是标准 JSON**           | 可直接用 `JSON.stringify` / `JSON.parse` 处理，C# 存 `nvarchar(max)` 即可                                   |
| **`styles` 是共享样式池**     | 单元格只存样式 ID，Univer 自动去重，避免大量重复数据                                                        |
| **`rev` 字段是版本号**        | 协同编辑时用于冲突检测，从 1 开始递增                                                                       |
| **不要手动修改 `dataStream`** | Docs 的 dataStream 是位置敏感的，所有元数据用 offset 索引，直接修改容易破坏数据，应始终通过 Univer API 操作 |
| **插件数据在 `resources` 里** | 条件格式、数据验证等功能的数据独立存放，加载时 Univer 自动分发给对应插件处理                                |

---

## 九、Agent / AI 自动化支持

Univer 对 Agent 友好不是单一特性，而是整个架构设计的结果。

### 9.1 架构设计原因

| 设计决策                   | 对 Agent 的意义                                                 |
| -------------------------- | --------------------------------------------------------------- |
| Command / Mutation 分层    | 所有操作确定性、可逆，Agent 操作自动支持撤销                    |
| Facade API 语义化命名      | Agent 可读 `bold`/`color`/`fontSize`，而非内部的 `bl`/`cl`/`fs` |
| Action Recorder / Replayer | 人工录制操作 → Agent 在任意文档上回放                           |
| 可取消的 Before 事件       | Agent 可以充当守门人，在操作执行前拦截并阻止                    |
| 统一 CommandService        | `executeCommand(id, params)` 就能驱动编辑器所有功能             |

---

### 9.2 核心 API

#### 程序化驱动编辑器

```typescript
// 所有操作通过统一入口
univerAPI.executeCommand("sheet.command.set-range-values", {
  value: { v: "Hello" },
  range: { startRow: 0, startColumn: 0, endRow: 0, endColumn: 0 },
});

// 撤销 / 重做
univerAPI.undo();
univerAPI.redo();
```

#### Sheets 程序化 API（FRange）

```typescript
const sheet = univerAPI.getActiveWorkbook().getActiveSheet();

// 写数据
sheet.getRange("B2:D4").setValues([
  [1, 2, 3],
  [4, 5, 6],
  [7, 8, 9],
]);

// 写公式
sheet.getRange("E2").setFormula("=SUM(B2:D4)");

// 设置样式
sheet
  .getRange("A1")
  .setFontWeight("bold")
  .setFontColor("#FF0000")
  .setBackgroundColor("#FFFF00");

// 读数据
const values = sheet.getRange("B2:D4").getValues();
const formula = sheet.getRange("E2").getFormula();

// 插入/删除行列
sheet.insertRows(2, 3); // 在第2行插入3行
sheet.deleteColumns(5, 1); // 删除第5列
```

#### Docs 程序化 API（FDocument）

```typescript
const doc = univerAPI.getActiveDocument();

// 插入文本
doc.insertText(0, "标题文字");

// 构建富文本（Agent 友好的语义化 API）
const richText = univerAPI
  .newRichText()
  .paragraph()
  .span("重要内容", { bold: true, fontSize: 16, color: "#1d4ed8" })
  .paragraph()
  .text("普通文字")
  .link("查看文档", "https://docs.univer.ai");

// 查找并修改段落样式
const para = doc.findParagraphByText("重要内容");
para?.getTextRange().setTextStyle({ bold: true, fontSize: 18 });
```

---

### 9.3 事件系统——观察与拦截

#### 观察变更

```typescript
// 监听单元格值变化
univerAPI.addEvent(univerAPI.Event.SheetValueChanged, (e) => {
  console.log("单元格变化", e.cellValue, e.row, e.column);
});

// 监听选区变化
univerAPI.addEvent(univerAPI.Event.SelectionChanged, (e) => {
  console.log("选区变化", e.selection);
});

// 监听所有命令执行
univerAPI.addEvent(
  univerAPI.Event.CommandExecuted,
  ({ id, type, params, options }) => {
    console.log("命令执行", id, params);
  },
);
```

#### 拦截并阻止操作（守门人模式）

```typescript
// 在操作执行前拦截——Agent 可以充当权限守卫
univerAPI.addEvent(univerAPI.Event.BeforeCommandExecute, (e) => {
  const riskyCommands = [
    "sheet.command.delete-sheet",
    "sheet.command.clear-selection-all",
  ];
  if (riskyCommands.includes(e.id)) {
    e.cancel = true; // 阻止执行
    console.warn("操作被 Agent 阻止:", e.id);
  }
});

// 拦截撤销操作
univerAPI.addEvent(univerAPI.Event.BeforeUndo, (e) => {
  if (!canUndo(currentUser)) {
    e.cancel = true;
  }
});
```

#### 可用事件一览

| 事件                         | 说明           | 可取消 |
| ---------------------------- | -------------- | ------ |
| `CommandExecuted`            | 任意命令执行后 | 否     |
| `BeforeCommandExecute`       | 任意命令执行前 | **是** |
| `SheetValueChanged`          | 单元格值变化   | 否     |
| `SelectionChanged`           | 选区变化       | 否     |
| `SheetEditStarted`           | 开始编辑单元格 | 否     |
| `SheetEditEnded`             | 结束编辑单元格 | 否     |
| `BeforeSheetEditEnd`         | 结束编辑前     | **是** |
| `ClipboardPasted`            | 粘贴完成       | 否     |
| `BeforeClipboardPaste`       | 粘贴前         | **是** |
| `WorkbookCreated`            | 工作簿创建     | 否     |
| `SheetCreated`               | Sheet 创建     | 否     |
| `BeforeSheetDelete`          | Sheet 删除前   | **是** |
| `SheetRangeFiltered`         | 筛选应用       | 否     |
| `SheetDataValidationChanged` | 数据验证变化   | 否     |
| `Undo` / `Redo`              | 撤销/重做执行  | 否     |
| `BeforeUndo` / `BeforeRedo`  | 撤销/重做前    | **是** |
| `LifeCycleChanged`           | 生命周期变化   | 否     |

---

### 9.4 操作录制与回放

`action-recorder` 包提供了完整的录制/回放能力：**先录制一次人工操作，然后让 Agent 在任意文档上重复执行**。

```typescript
import { UniverActionRecorderPlugin } from "@univerjs/action-recorder";

// 注册录制插件
univer.registerPlugin(UniverActionRecorderPlugin);

// 获取服务
const recorderService = univer.__getInjector().get(ActionRecorderService);
const replayService = univer.__getInjector().get(ActionReplayService);

// 开始录制用户操作
recorderService.startRecording();

// 用户做了一系列操作...
// 停止并下载为 recorded-commands.json
recorderService.completeRecording();
```

```typescript
// Agent 回放（三种模式）
import { ReplayMode } from "@univerjs/action-recorder";

// DEFAULT: 在当前聚焦的文档上回放
await replayService.replayCommands(commandsArray);

// NAME: 按 Sheet 名称匹配目标（跨工作簿回放）
await replayService.replayCommands(commandsArray, { mode: ReplayMode.NAME });

// ACTIVE: 始终在当前激活的 Sheet 上执行
await replayService.replayCommands(commandsArray, { mode: ReplayMode.ACTIVE });

// 带延迟回放（用于演示效果）
await replayService.replayCommandsWithDelay(commandsArray);

// 从 JSON 文件回放
await replayService.replayLocalJSON(ReplayMode.NAME);
```

录制结果是标准 JSON 数组，可以保存到数据库让 Agent 按需调用：

```json
[
  { "id": "sheet.command.set-range-values", "params": { "range": {...}, "value": {...} } },
  { "id": "sheet.command.set-style", "params": { "range": {...}, "style": {...} } },
  { "id": "sheet.operation.set-selections", "params": { "selections": [...] } }
]
```

---

### 9.5 结合 LLM 的典型集成模式

```typescript
// Agent 工具函数示例：让 LLM 通过工具调用操作表格
const univerTools = {
  // 工具1：读取单元格区域
  readRange: (a1Notation: string) => {
    return univerAPI
      .getActiveWorkbook()
      .getActiveSheet()
      .getRange(a1Notation)
      .getValues();
  },

  // 工具2：写入单元格值
  writeRange: (a1Notation: string, values: unknown[][]) => {
    univerAPI
      .getActiveWorkbook()
      .getActiveSheet()
      .getRange(a1Notation)
      .setValues(values);
  },

  // 工具3：写入公式
  writeFormula: (a1Notation: string, formula: string) => {
    univerAPI
      .getActiveWorkbook()
      .getActiveSheet()
      .getRange(a1Notation)
      .setFormula(formula);
  },

  // 工具4：撤销上一步操作
  undo: () => univerAPI.undo(),

  // 工具5：获取当前工作簿快照（传给 LLM 作为上下文）
  getSnapshot: () => univerAPI.getActiveWorkbook()?.save(),

  // 工具6：回放录制的操作脚本
  replayScript: (commands: ICommandInfo[]) =>
    replayService.replayCommands(commands, { mode: ReplayMode.NAME }),
};
```

> **提示**：向 LLM 传入 `univerAPI.Event` 里的事件名称列表和 `FRange` / `FDocument` 的方法签名，可以让 LLM 自行生成正确的 `executeCommand` 调用或直接调用 Facade API 方法，无需额外训练。

---

## 十、协同编辑落地现状（与第七节的差异）

> 第七节是最初的设计方案，本节记录**实际落地的实现**；两者在传输层、在线状态渲染上有差异。

### 10.1 传输层：裸 WebSocket（不是 SignalR）

浏览器无法直接完成 SignalR 握手（需要 `@microsoft/signalr` 客户端），因此前端改为使用**原生 WebSocket**，服务端提供裸 WS 端点：

| 项 | 值 |
| --- | --- |
| 前端 | `src/app/composables/useCollaboration.ts`（原生 `WebSocket`，未新增任何依赖） |
| 后端 | `WebApi/Hubs/CollabWebSocketEndpoint.cs`、`WebApi/Hubs/CollabRoomRegistry.cs` |
| 端点 | `wss://{host}/ws/collab/{unitId}?userId=&userName=&access_token=` |
| 开发代理 | `vite.config.ts` 中 `/ws` → `VITE_PROXY_URL`（`ws: true`），前端连 `ws://localhost:5173/ws/collab/...` |
| 房间状态 | 单实例内存（rooms / 成员名单 / presence / revision），多副本需换 Redis |

`WebApi/Hubs/CollabHub.cs`（SignalR）与 `app.MapHub<CollabHub>("/collab/{unitId}")` 仍在代码中，但 Web 端不使用，
属于第七节方案 A 的遗留实现。完整消息协议（`join` / `presence` / `ingest` / `ping` / `leave` ↔
`init` / `join` / `leave` / `presence` / `new_cs` / `cs_ack` / `cs_rej` / `pong`）见
`docs/univer-integration/technical-design/api-spec.md` 第三节。

### 10.2 “谁在哪儿编辑什么”是如何显示的

1. **本地上报**（`UniverSheet.vue`）：监听 `SelectionChanged`（选区变化）、`SheetEditStarted`（进入编辑）、
   `SheetEditChanging`（输入中，250ms 先发后补的节流，见 10.7）、`BeforeSheetEditEnd`、`SheetEditEnded`、`Scroll`、`SheetSkeletonChanged`，
   把 `{ sheetId, a1, text, editing }` 作为 `presence` 发出；打开文档时先上报一次初始光标。
2. **服务端广播**：`CollabRoomRegistry.PresenceAsync` 只转发给**其他人**，同时把 presence 记进成员快照，
   所以**后加入的人**在 `init` 里就能立刻看到已有光标，不必等对方再动一次。
3. **远端渲染**：
   - 选区高亮：`sheet.highlightRanges([range], { stroke: color, strokeWidth: 2, fill: rgba(color, .1) })`，
     编辑中填充加深（.2）；
   - 姓名标签：`.univer-collab-tag` DOM 图层叠在画布上，仅当对方在**同一张工作表**时显示。
     `FRange.getCellRect()` 给的是**场景坐标**（不含滚动量、不含画布原点），所以按 Univer 内部同样的公式换算：
     `屏幕坐标 = (单元格坐标 − viewportScroll) × 画布缩放 + 画布原点`；`viewportScroll` 取自
     `FWorksheet.onScroll()`（初值用 `getScrollState()` 补一次），画布缩放 = `getZoom()` ×
     （canvas 实际宽度 / `canvas.style.width`），切换工作表时重新订阅；
   - 工具栏：头像 + `N 人在线`，悬浮气泡逐人显示 `正在编辑 B3：内容` / `位于 B3` / `在文档中`；
   - 颜色：10 色调色板按 `userId` 哈希取模，前后端算法一致；
   - Docs：Univer 未提供公开的“文本范围 → 屏幕坐标”接口，因此 Docs 只上报并显示“正在编辑”状态，不绘制光标框。
4. **断线重连**：指数退避重连（最长 10s），重连后补发最后一次 presence；离线期间的本地 mutation 会缓存，
   重连后作为 Changeset 发出。

### 10.3 页面高度 / 双滚动条

`src/app/views/document/editor/index.vue` 不再使用写死的 `calc(100vh - 56px)`：

- 挂载时测量 `rootRef.getBoundingClientRect().top`，页面高度 = `window.innerHeight - top`，
  并在 `resize` 与文档加载完成后重新测量 → 编辑区正好占满外壳留出的空间，外层 `el-scrollbar` 不再出现滚动条；
- 进入页面时把外层 `el-scrollbar__wrap` 的 `scrollTop` 归零，避免切路由后带着旧滚动位置出现“幽灵滚动条”；
- 编辑区使用 `flex-1 min-h-0 overflow-hidden`，编辑器统一传 `height="100%"`，
  页面本身不再滚动，表格的滚动条只出现在表格内部；
- 另外用 `ResizeObserver` 监听外层滚动容器，侧边栏折叠、布局变化后会重新测量。

### 10.4 其它已修复的细节

| 问题 | 说明 |
| --- | --- |
| 重命名会清空文档内容 | 列表页改名与编辑器标题改名只提交 `{ title }`；后端 `SaveAsync` 对 `dataJson` 做 `?? existing` 合并，不会再被 `{}` 覆盖 |
| 光标标签常驻定时器 | 姓名标签的跟随定时器只在“确实有远端光标”时运行，无人在线时不再每 120ms 轮询 |
| 本地临时 mutation 被广播 | 与 Univer 官方协同示例一致，`options.onlyLocal` 的 mutation 不再上网 |
| 切换工作表后残留高亮 | `SheetSkeletonChanged` 时重新计算远端高亮，切表即清理上一张表的标记 |
| 远端选区高亮完全不显示 | `sheet.highlightRanges(...)` 的 `primary` 必须是**完整**的 `ISelectionCell`：传 `{ row, col }` 这种残缺对象时 Univer 会算出 `NaN` 坐标，整个高亮都不会绘制。现在传 `null`（Univer 自己的“无主单元格”状态），远端光标有边框 + 淡填充，也不会带上自动填充手柄 |
| 滚动后姓名标签位置漂移 | 标签是 DOM 元素、`getCellRect()` 却是场景坐标，原先只做 `rect − layerRect`，滚动/缩放后位置全错；现在按 10.2 的公式换算（viewportScroll + zoom + 画布原点） |
| `pnpm dev` / `pnpm build` 报 `Rollup failed to resolve import "@univerjs/..."` | `UniverSlide.vue` 直接 import 的 7 个深层包原先是 preset 的传递依赖，pnpm 不会把它们提升到 `node_modules/@univerjs/`；现已在 `package.json` 显式声明，见 10.6 |

| 远端选区仍然完全不显示 / 两端内容不一致 / 日志接口被刷爆 | 三个根因（随机 id、params 延迟序列化、挂载前丢 changeset）与修复见 10.7；界面语言改为英文见 10.8 |

### 10.5 自查清单（两人同时编辑）

1. 用**两个不同账号**（同一租户）分别登录，打开同一个文档；
2. 双方都应看到对方头像出现在工具栏，`N online` 数字正确；
3. A 点击任意单元格 → B 侧该单元格出现 A 颜色的边框 + 淡填充 + 姓名标签（B 需停留在同一张工作表）；
4. A 双击单元格输入内容 → B 侧标签实时显示 `A: 正在输入的内容`，悬浮头像气泡文案同步变化；
5. 两人依次修改不同单元格 → 内容互相同步（Changeset 乐观锁，版本冲突自动重试）；
6. 关掉一个标签页 → 另一方头像消失；
7. 页面只剩一个滚动条（表格内部），窗口缩放后表格自适应可用高度。
8. 远端改动按单元格对齐：A 在 E8 输入文字 → B 的 E8 出现同样的内容（而不是别的单元格、也不是旧值）；
9. 编辑一个单元格时，`/api/ow/document-operation-logs/v1` 只在停止输入约 1.5s 后调用一次；
10. 界面文案全英文：右键菜单、工具栏、条件格式 / 表格面板、协同气泡与远端姓名标签。

---

### 10.6 依赖声明与构建

`UniverSlide.vue` 直接 import 了 7 个“深层”包：`@univerjs/engine-render`、`@univerjs/ui`、
`@univerjs/docs`、`@univerjs/docs-ui`、`@univerjs/drawing`、`@univerjs/slides`、`@univerjs/slides-ui`。
pnpm 默认不提升传递依赖，而这些包原先只作为 preset 的传递依赖存在（`@univerjs/slides` /
`@univerjs/slides-ui` 甚至不在依赖图里），所以 `pnpm dev` / `pnpm build` 会在 `UniverSlide.vue`
处报 `Rollup failed to resolve import ...`。

现已在 `package.json` 的 `dependencies` 中显式声明这 7 个包 + `@univerjs/core`（版本均为 `0.25.1`），
`pnpm.overrides` 仍锁定全部 `@univerjs/*` 的版本，`pnpm-lock.yaml` 已同步更新。

> ⚠️ 拉取本次改动后**必须执行一次 `pnpm install`**，否则 `node_modules/@univerjs/` 下缺少这些包，
> 开发服务器与构建都会失败：
>
> ```bash
> cd packages/flowFlex-common
> pnpm install
> pnpm build:development   # 或 pnpm dev
> ```

---

### 10.7 为什么“两端数据不统一 / 看不到对方选区 / 日志接口被打爆”

#### 根因：每个标签页的 Univer id 都是随机的

`univerAPI.createWorkbook(snapshot)` 不会给快照补 id。读 `@univerjs/core` 的源码可以确认：

| 位置 | 行为 |
| --- | --- |
| `getEmptySnapshot()` | 返回 `{ id: '', sheetOrder: [], sheets: {} }` |
| `Workbook` 构造函数 | `if (snapshot.id == null \|\| id.length === 0) snapshot.id = generateRandomId(6)` |
| `_parseWorksheetSnapshots()` | `sheets` 为空时执行 `sheets[generateRandomId()] = { id }` |
| `Worksheet` 构造函数 | `this._sheetId = this.snapshot.id ?? generateRandomId(6)` |

后端 `DocumentSnapshotService.CreateAsync` 给新文档写的是 `DataJson = "{}"`，
也就是说**没有保存过的文档，A、B 两个浏览器各自生成了完全不同的 `unitId` 与 `subUnitId`**。
而协同协议恰恰把 id 放在 payload 里：

- mutation 参数 `{ unitId, subUnitId, cellValue }`：对方收到一个自己不存在的工作表 id，
  Univer 找不到目标，整条变更被丢弃 → “A 编辑的内容到不了 B 的表格里”；
- presence `{ sheetId, a1 }`：`user.sheetId` 永远不等于 `activeSheetId`，`applyHighlights()` 直接跳过 →
  “看不到对方选中的单元格”。

**修复**：新增 `src/app/components/UniverEditor/univerIds.ts`，从路由里的文档 `unitId` 派生出
稳定的 workbook id，并给默认工作表补一个稳定 id；快照里已经有 id 的（保存过的文档）沿用原 id。
`UniverSheet.vue` / `UniverDoc.vue` / `UniverSlide.vue` 在 `createWorkbook` / `createUnit`
之前统一做一次归一化（`normalizeWorkbookSnapshot` / `normalizeUnitSnapshot`）。

#### 另外三个导致“不同步”的细节

| 问题 | 修复 |
| --- | --- |
| changeset 引用的是 Univer 自己的 `params` 对象，而它最多延迟 400ms 才发出，期间对象可能已被改写 | `CommandExecuted` 回调里立刻序列化成 `data` 字段，`useCollaboration.queueMutation` 只搬运字符串 |
| 编辑器是 `defineAsyncComponent`，协同会话却可能在它挂载前就建立，早到的 changeset 被静默丢弃 | `editor/index.vue` 的 `applyRemoteChanges` 先入队，编辑器 `@ready` 之后统一应用 |
| 本地 changeset 用的是 debounce（每次按键都重置 80ms 定时器），连续输入时一个字符都发不出去 | 改成 throttle + 400ms 上限，并在重连后立刻补发离线期间缓存的 changeset |

#### 远端变更的兼容与可观测性

`UniverSheet.applyRemoteMutations` 现在会先把发送方的 id 改写成自己的：`unitId` 一律改成本地
workbook id；`subUnitId` 在本地区不存在时**跳过该条变更并计数**，而不是把内容写到任意一张工作表上。
结束时若出现 `failed` / `skipped`，会打一条汇总 warn，便于排查。

#### 编辑日志（`/ow/document-operation-logs/v1`）不再被刷爆

- 原因：每次按键都是一条 mutation，过去一条 mutation 记一条日志，一个单元格就能产生上百条记录；
  每 800ms 只能发 20 条，队列永远排不空，接口自然一直在被调用。
- 现在：`documentEditor.ts` 按 `mutationId` + 目标单元格合并（`mutationTarget()` 从序列化后的
  `cellValue` 矩阵里取出首个 `row:col`），一整段输入只保留一条“最新状态”记录，停止输入 1.5s 后才发出；
  同一批用 `Promise.allSettled` 并发发送，卸载 / 离开页面时 `flushPendingLogs()` 兜底。
- 效果：编辑一个单元格约等于 1 次请求（过去是几十到上百次）。

### 10.8 界面语言：全部改为英文

#### Univer 自身 UI（菜单、工具栏、右键菜单、面板、对话框）

Univer 的文案都走 `createUniver({ locale })` 提供的 LocaleService，三处入口均已切换：

| 文件 | 改动 |
| --- | --- |
| `src/app/components/UniverEditor/UniverSheet.vue` | `@univerjs/preset-sheets-core/locales/zh-CN` → `.../en-US`；`LocaleType.ZH_CN` → `LocaleType.EN_US` |
| `src/app/components/UniverEditor/UniverDoc.vue` | `@univerjs/preset-docs-core/locales/zh-CN` → `.../en-US`；`LocaleType.EN_US` |
| `src/app/components/UniverEditor/UniverSlide.vue` | `new Univer({ locale: LocaleType.EN_US })`（Slides 没有 preset 语言包，用内置默认） |
| `src/shims/univer.d.ts` | 语言包模块声明由 `zh-CN` 改为 `en-US` |

#### 本项目自己的文案

| 位置 | 现在显示 |
| --- | --- |
| 工具栏协同状态 | `Collaboration offline` / `N online` |
| 协作者悬浮气泡 | `Online collaborators`，逐人显示 `Editing B3: 内容` / `At B3: 内容` / `Viewing the document` |
| 远端光标姓名标签 | `姓名: 内容`（原来是全角冒号） |
| 保存提示 | `Saved` |
| 文档列表页 / 新建对话框 | 原本就是英文 |

#### Univer 里两处写死的中文

`@univerjs/sheets-conditional-formatting-ui`（条件格式的“单元格图标”下拉）与
`@univerjs/sheets-table-ui`（表格条件输入框的 placeholder）把中文**硬编码**在组件里、不走语言包，
因此切换 locale 不起作用。`vite.config.ts` 新增了一个小插件 `univer-localisation-patch`：

- `transform`（`enforce: 'pre'`）在打包阶段改写这两个字面量；
- 同一个改写在 `optimizeDeps.esbuildOptions.plugins` 里再做一遍，因为开发服务器提供的是**预打包依赖**。

替换表见 `UNIVER_TEXT_PATCHES`：`无单元格图标` → `No cell icon`，`请输入` → `Please enter`。

> 其余残留的中文字符只来自 `@univerjs/engine-formula` / `sheets-numfmt` 的
> **数字格式与中文日历数据**（中文数字、星期 / 月份 / 时辰名、`2020年1月1日` 这类日期模式样本），
> 属于数据而不是界面文案，只有用户主动套用中文数字格式时才会用到。

#### 验证方式

```bash
pnpm build:development
grep -c 无单元格图标 dist/js/UniverSheet.*.js   # 期望 0
grep -c 请输入     dist/js/UniverSheet.*.js   # 期望 0
grep -c "No cell icon" dist/js/UniverSheet.*.js  # 期望 1
```
