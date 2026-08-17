# OW-704 — Document Signing（文档签署）

> **优先级**: P1  
> **Sprint**: OW.2026.08/07–08/20  
> **状态**: New  
> **指派**: Kai Li  
> **创建**: Amanda Li  
> **前置依赖**: OW-703（可并行开发，From Profile Tab 先 mock，OW-703 签名 API 完成后替换）

---

## 1. 需求摘要

在 Stage 的 **Documents 类型 Component** 中，允许用户对 PDF 附件进行在线电子签署：在 PDF 页面上自由拖拽放置电子签名和日期，生成已签署的新文档供下载和打印，**原始 PDF 不被修改**。

---

## 2. 核心业务价值

| 场景     | 当前痛点                  | 解决方案                     |
| -------- | ------------------------- | ---------------------------- |
| 合同签署 | 打印 → 手签 → 扫描 → 上传 | 在线签署，直接生成已签署 PDF |
| 交付验收 | 纸质签收单难以归档        | 电子签名，自动关联 Case      |
| 审批签字 | 异地签字需快递            | 随时随地在线签署             |

---

## 3. 架构方案（ADR-0002 确认）

**前端合成 + 后端存储哈希**

```
前端（浏览器）                              后端
───────────────────────────────────────────────────────
1. PDF.js 渲染 PDF 预览
2. 用户拖拽放置签名/日期元素
3. pdf-lib 将签名/日期合成进 PDF
4. 生成已合成的 PDF ArrayBuffer
5. 下载供用户本地保存（Download/Print）
6. POST 已合成文件 + 签署元数据
                            ──────────────────────────>
                            7. 后端接收文件
                            8. 后端自行计算 SHA-256 哈希
                            9. 上传文件到 Blob Store
                            10. 写签署记录
                                (file_hash, source_file_id,
                                 signer, sign_time, is_signed)
```

**为什么不用后端合成：** 法律效力要求不高，前端合成省掉 PDFsharp 和 Dockerfile 字体处理，降低复杂度。详见 ADR-0002。

**为什么后端还要存哈希：** 后端收到文件后自行计算 SHA-256，不信任前端传来的任何哈希值。用途：当前防篡改检测，未来为用户真伪判别留基础。

---

## 4. 界面交互流程（来自原型图）

### 4.1 整体状态机

```
【普通预览模式】
  顶部：文件名 | ← 翻页 → | 页码 | 缩放 | Sign Document（紫色）| ×
  左侧：页面缩略图导航（懒加载）
  中央：PDF 只读预览
        ↓ 点击「Sign Document」（仅原始 PDF 显示，已签署文件无此按钮）
        ↓ PDF.js 加载失败（加密 PDF）→ 立即提示，不进入签署模式

【签署编辑模式】
  顶部：文件名 | ← 翻页 → | 页码 | 缩放 | Add Signature | Add Date | Confirm Signature（紫色）| ×
  左侧：页面缩略图导航
  中央：PDF 预览 + 可拖拽的签名/日期元素叠加层
        ↓ 点击 × 且有已放置元素 → 弹「确认放弃」提示
        ↓ Confirm 按钮在无元素时 disabled
        ↓ 点击「Confirm Signature」且有元素 → 确认弹窗 → 用户确认
        ↓ 前端 pdf-lib 合成 → POST 后端 → 成功

【已签署只读模式】
  顶部：文件名 + 绿色「Signed」图标 | ← 翻页 → | 页码 | 缩放 | Download | Print | ×
  左侧：页面缩略图导航
  中央：已签署 PDF 只读预览
```

### 4.2 签名/日期元素视觉交互（来自原型图）

选中态元素：

- **左上角紫色圆点**：移动 handle
- **右上角红色 × 圆点**：删除按钮
- **右下角黑色圆点**：唯一 resize handle
- **蓝紫色虚线边框**：选中态
- 未选中元素也显示虚线边框，无 handle

### 4.3 Add Signature 弹窗

**Tab 1：From Profile**

- 调用 `GET /ow/profile/signatures` 获取用户全部已保存签名
- 每个签名显示为可点击卡片，展示签名图片预览
- 提示文字：「Signatures from [用户名]'s profile. Click one to place it.」
- 点击卡片 → 弹窗关闭 → 签名出现在当前页中央（初始 150×60pt），可拖拽
- 无签名时：提示前往 My Profile 设置，或切换到 Draw Tab

**Tab 2：Draw**

- 提示文字：「Draw your signature below.」
- 手写画布（Canvas，Pointer Events API，支持鼠标 + 触屏）
- 底部：「Clear」（清空）+ 「Use Signature」（确认）
- 点「Use Signature」→ 弹窗关闭 → 手写签名出现在当前页中央
- **现场手写的签名不自动保存到 Profile**

### 4.4 Add Date 行为

- 点击「Add Date」**不弹窗**，日期元素直接出现在当前页中央（初始 100×20pt）
- 内容自动填充当天日期，格式 MM/DD/YYYY

### 4.5 Confirm Signature 流程

1. 无元素时 Confirm 按钮 **disabled**
2. 点击后弹确认弹窗，说明签署后不可修改
3. 用户确认 → **前端 pdf-lib 合成 PDF**
4. 合成完成 → POST 到后端（multipart/form-data）
5. 后端计算哈希、存储、写记录
6. 成功 → 进入已签署只读模式，工具栏变为 Download + Print，文件名旁绿色 Signed 图标
7. **失败** → 保持签署编辑模式，元素保留，显示错误提示，可重试

---

## 5. 边界规则

| 规则         | 说明                                                        |
| ------------ | ----------------------------------------------------------- |
| 签署权限     | MVP 所有能访问该 Stage 的用户都可签署                       |
| 多次签署     | 原始 PDF 可被多人各自独立签署；已签署文件不能再次签署       |
| 中途退出     | 有元素时点 × 弹「确认放弃」提示；关掉即清空，不做草稿持久化 |
| 空签署       | 无元素时 Confirm 按钮 disabled                              |
| 元素数量     | 签名最多 10 个，日期最多 10 个                              |
| 元素初始尺寸 | 签名 150×60pt，日期 100×20pt                                |
| 元素最小尺寸 | 签名 50×20pt，日期 60×16pt，自动 clamp                      |
| 边界约束     | 元素不能拖出 PDF 页面范围                                   |
| 加密 PDF     | PDF.js 加载失败立即提示，不进入签署模式                     |
| 大文件提示   | PDF > 20MB 或 > 100 页时提示加载较慢，不阻止                |
| 已签署文件   | 不显示 Sign Document 按钮，不显示删除按钮                   |
| 删除原始文件 | 不影响已签署文件，软关联                                    |
| 缩放方式     | 调整 viewport.scale 重新渲染 canvas（非 CSS transform）     |
| 哈希计算     | 后端收到文件后自行计算 SHA-256，不信任前端传值              |

---

## 6. 验收标准

### Part 1：PDF 预览与导航

| #   | 需求描述   | 验收标准                                                             |
| --- | ---------- | -------------------------------------------------------------------- |
| 1   | 打开 PDF   | 全屏弹窗打开，顶部显示文件名，原始 PDF 右上角有「Sign Document」按钮 |
| 2   | 左侧缩略图 | 各页缩略图懒加载，点击跳转对应页                                     |
| 3   | 翻页       | 上一页/下一页按钮，显示「第 X 页 / 共 Y 页」                         |
| 4   | 缩放       | 50%–200%，按钮或滚轮控制，默认 100%，缩放触发 canvas 重绘            |
| 5   | 加密检测   | PDF.js 加载失败立即提示「该 PDF 已加密，无法在线签署」               |
| 6   | 大文件提示 | > 20MB 或 > 100 页时显示加载提示                                     |

### Part 2：进入签署模式

| #   | 需求描述 | 验收标准                                                             |
| --- | -------- | -------------------------------------------------------------------- |
| 7   | 入口按钮 | 原始 PDF 右上角「Sign Document」紫色按钮                             |
| 8   | 模式切换 | 点击后工具栏变为 Add Signature \| Add Date \| Confirm Signature \| × |

### Part 3：Add Signature 弹窗

| #   | 需求描述      | 验收标准                                      |
| --- | ------------- | --------------------------------------------- |
| 9   | 弹窗结构      | 含 From Profile 和 Draw 两个 Tab              |
| 10  | From Profile  | 显示全部已保存签名卡片，点击选用              |
| 11  | 无签名引导    | Profile 无签名时提示前往设置或切换 Draw Tab   |
| 12  | Draw Tab      | 手写画布，Pointer Events API，支持鼠标 + 触屏 |
| 13  | Clear         | 清空画布                                      |
| 14  | Use Signature | 签名放置到当前页中央，不保存到 Profile        |

### Part 4：签名/日期元素操作

| #   | 需求描述 | 验收标准                               |
| --- | -------- | -------------------------------------- |
| 15  | 移动     | 左上角紫色圆点拖拽移动                 |
| 16  | 删除     | 右上角红色 × 删除                      |
| 17  | Resize   | 右下角黑色圆点调整大小                 |
| 18  | 最小尺寸 | 签名 50×20pt，日期 60×16pt，自动 clamp |
| 19  | 边界约束 | 不能拖出页面范围                       |
| 20  | Add Date | 点击直接出现在当前页，不弹窗           |
| 21  | 日期格式 | MM/DD/YYYY，自动填充当天               |
| 22  | 跨页放置 | 可在不同页面分别放置                   |
| 23  | 数量上限 | 签名最多 10 个，日期最多 10 个         |

### Part 5：签署确认与生成

| #   | 需求描述   | 验收标准                                 |
| --- | ---------- | ---------------------------------------- |
| 24  | 禁止空签署 | 无元素时 Confirm 按钮 disabled           |
| 25  | 确认弹窗   | 弹窗说明签署后不可修改                   |
| 26  | 前端合成   | 确认后 pdf-lib 在浏览器完成 PDF 合成     |
| 27  | 上传后端   | 合成后 POST 文件给后端存储               |
| 28  | 文件命名   | `原文件名_已签署_签署人_日期.pdf`        |
| 29  | 原件保留   | 原始 PDF 不被修改                        |
| 30  | 失败处理   | 失败时保持签署编辑模式，元素保留，可重试 |

### Part 6：已签署状态

| #   | 需求描述    | 验收标准                                           |
| --- | ----------- | -------------------------------------------------- |
| 31  | 工具栏变化  | 变为 Download + Print，签署按钮组消失              |
| 32  | Signed 图标 | 文件名旁绿色「Signed」图标                         |
| 33  | Download    | 可下载已签署 PDF                                   |
| 34  | Print       | 触发浏览器打印                                     |
| 35  | 文件列表    | 已签署条目显示绿色 Signed 标识 + 签署人 + 签署时间 |
| 36  | 不可再签    | 已签署文件不显示 Sign Document 和删除按钮          |
| 37  | 退出提示    | 有元素时点 × 弹「确认放弃」提示                    |

### Part 7：审计与安全

| #   | 需求描述       | 验收标准                                           |
| --- | -------------- | -------------------------------------------------- |
| 38  | Change History | 签署操作记入 Case Change History                   |
| 39  | 服务端校验     | 后端校验 fileId 未被签署过，已签署拒绝再次签署请求 |
| 40  | 身份记录       | 记录签署人用户名、签署时间（UTC）                  |
| 41  | 哈希存储       | 后端自行计算已签署文件 SHA-256 并存入数据库        |

---

## 7. MVP 不包含

- Word / Excel 文件签署
- 添加文字批注
- 多人在同一文档签署
- 签署顺序控制
- 第三方签章集成（DocuSign）
- 数字证书签名（PKI）
- 批量文档签署
- 骑缝章 / 水印
- 用户真伪判别入口（哈希对比功能，基础已预留）

---

## 8. 后端 API 设计

### 签署接口

```
POST /ow/files/{fileId}/sign
Content-Type: multipart/form-data
```

**请求体：**

| 字段         | 类型   | 说明                               |
| ------------ | ------ | ---------------------------------- |
| `file`       | File   | 前端已合成的 PDF 文件              |
| `signerName` | string | 签署人姓名                         |
| `signedAt`   | string | 签署时间（ISO 8601 UTC，前端记录） |

**后端处理：**

1. 校验 fileId 存在且 `is_signed = false`，否则拒绝
2. 接收文件
3. 自行计算 SHA-256 哈希
4. 上传到 Blob Store
5. 写签署记录
6. 原子性：任何步骤失败，不写数据库，不留垃圾文件

**返回：**

```json
{
  "code": "200",
  "data": {
    "signedFileId": "123456789",
    "downloadUrl": "/ow/files/123456789/download",
    "fileName": "contract_已签署_Kai Li_08112026.pdf",
    "fileHash": "a3f5c8d2..."
  }
}
```

---

## 9. 前端技术方案

### 9.1 组件结构

```
DocumentSigningDialog.vue              // 签署主弹窗（全屏）
├── SigningToolbar.vue                 // 顶部工具栏（三种模式状态切换）
├── PageThumbnails.vue                 // 左侧缩略图（IntersectionObserver 懒加载）
├── PdfViewer.vue                      // PDF.js canvas 渲染
│   └── SigningOverlay.vue             // 签名/日期拖拽元素叠加层
└── AddSignatureDialog.vue             // Add Signature 弹窗
    ├── FromProfileTab.vue             // From Profile Tab
    └── DrawTab.vue                    // Draw Tab（vue-signature-pad）
```

新建位置：`src/app/views/onboard/onboardingList/components/signing/`

### 9.2 PDF 渲染（PDF.js）

复用 `src/app/components/ai/utils/pdfDetector.ts` 中的 `loadPdfJs()`：

```typescript
import { loadPdfJs } from "@/components/ai/utils/pdfDetector";

const pdfjsLib = await loadPdfJs();
const pdf = await pdfjsLib.getDocument({ data: arrayBuffer }).promise;
const page = await pdf.getPage(pageNumber);
const viewport = page.getViewport({ scale: currentScale });
await page.render({ canvasContext: ctx, viewport }).promise;
// 缩放时：更新 currentScale → 重新 getViewport + render，不用 CSS transform
```

### 9.3 PDF 合成（pdf-lib）

```bash
pnpm add pdf-lib
```

```typescript
import { PDFDocument } from "pdf-lib";

const pdfDoc = await PDFDocument.load(originalArrayBuffer);
const page = pdfDoc.getPage(pageIndex); // 0-indexed
const { height: pageHeight } = page.getSize();

// 嵌入签名图片
const pngImage = await pdfDoc.embedPng(signatureBase64);
page.drawImage(pngImage, {
  x: element.pdfX,
  y: pageHeight - element.pdfY - element.pdfHeight, // pdf-lib 坐标原点在左下角，需转换
  width: element.pdfWidth,
  height: element.pdfHeight,
});

// 绘制日期文字
page.drawText(element.content, {
  x: element.pdfX,
  y: pageHeight - element.pdfY - element.pdfHeight,
  size: 12,
});

const signedPdfBytes = await pdfDoc.save();
```

> ⚠️ **坐标系注意**：pdf-lib 坐标原点在**左下角**，PDF.js 在**左上角**。换算：`pdfLibY = pageHeight - pdfJsY - elementHeight`

### 9.4 坐标换算（完整流程）

```
canvas 坐标（px）
  ÷ viewport.scale
= PDF.js 坐标（pt，左上角原点）
  转换 Y 轴：pdfLibY = pageHeight - pdfJsY - elementHeight
= pdf-lib 坐标（pt，左下角原点）
```

```typescript
const toSigningCoords = (
  canvasX: number,
  canvasY: number,
  elementW: number,
  elementH: number,
  viewport: any,
  pageHeightPt: number,
) => {
  const pdfJsX = canvasX / viewport.scale;
  const pdfJsY = canvasY / viewport.scale;
  return {
    x: pdfJsX,
    y: pageHeightPt - pdfJsY - elementH / viewport.scale, // pdf-lib 左下角原点
    width: elementW / viewport.scale,
    height: elementH / viewport.scale,
  };
};
```

### 9.5 签名拖拽层

- Pointer Events API（`pointerdown/pointermove/pointerup`）统一鼠标和触屏
- 移动：左上角 handle，clamp 到页面范围
- Resize：右下角 handle，clamp 到最小/最大值
- 元素不能拖出页面边界

### 9.6 新增依赖

```bash
pnpm add pdf-lib          # 前端 PDF 合成
pnpm add vue-signature-pad # Draw Tab 手写画布（与 OW-703 共用）
```

---

## 10. 后端技术方案

### 不再需要

- ~~PDFsharp NuGet 包~~
- ~~Dockerfile 字体处理~~

### 需要处理

- 接收 multipart/form-data 文件上传
- SHA-256 哈希计算：

```csharp
using var sha256 = SHA256.Create();
var hashBytes = sha256.ComputeHash(fileStream);
var fileHash = Convert.ToHexString(hashBytes).ToLower();
```

- 上传到 Blob Store（复用现有逻辑）
- 签署记录写库（含 `file_hash`、`source_file_id`、`is_signed = true`）

---

## 11. 数据库字段补充

已签署文件记录需要新增字段：

| 字段             | 类型         | 说明                              |
| ---------------- | ------------ | --------------------------------- |
| `is_signed`      | bool         | 是否为已签署文件                  |
| `source_file_id` | bigint       | 原始文件 ID（软关联，无外键约束） |
| `file_hash`      | varchar(64)  | SHA-256 哈希，后端自行计算        |
| `signer_name`    | varchar(200) | 签署人姓名                        |
| `sign_time`      | timestamp    | 签署时间（UTC）                   |

---

## 12. 工时拆解

| 模块                                                                    | 工时          |
| ----------------------------------------------------------------------- | ------------- |
| 前端 - DocumentSigningDialog（PDF.js 渲染 + 缩略图懒加载 + 工具栏三态） | 2 天          |
| 前端 - 签名/日期拖拽层（Pointer Events + resize + clamp + 边界约束）    | 1.5 天        |
| 前端 - AddSignatureDialog（From Profile + Draw Tab）                    | 0.5 天        |
| 前端 - pdf-lib 合成 + 坐标换算 + Confirm 流程                           | 1 天          |
| 前端 - 已签署状态 + Download/Print + 退出提示                           | 0.5 天        |
| 后端 - 签署 API（接收文件 + SHA-256 + Blob 上传 + 写库）                | 0.75 天       |
| 后端 - Change History + is_signed 校验                                  | 0.25 天       |
| 测试                                                                    | 1 天          |
| **合计**                                                                | **~7.5 人天** |

---

## 13. 后续增强路线图

| 版本     | 功能                              | 优先级 |
| -------- | --------------------------------- | ------ |
| V1 (MVP) | PDF 签署 + 拖拽定位 + 缩放 + 日期 | P0     |
| V1.1     | 用户真伪判别入口（哈希对比）      | P1     |
| V1.2     | Word/Excel 转 PDF 后签署          | P1     |
| V2       | 添加文字批注                      | P2     |
| V2.1     | 多人签署                          | P2     |
| V3       | 第三方签章集成                    | P3     |

---

## 14. 原型参考

https://guidepost-three.vercel.app/

---

## 15. 相关 ADR

- [ADR-0001](./adr/0001-user-signature-no-tenant-isolation.md)：用户签名表不做租户隔离
- [ADR-0002](./adr/0002-legal-validity-pending-discussion.md)：前端合成方案 + 后端哈希存储

---

## 16. 项目背景信息（开发依据）

### 现有 PDF 相关代码

| 文件路径                                                        | 用途           | 说明                                                                                                            |
| --------------------------------------------------------------- | -------------- | --------------------------------------------------------------------------------------------------------------- |
| `src/app/components/previewFile/previewFile.vue`                | 文件预览弹窗   | **需改造**：新增 `allowSign` prop，PDF 文件在 `allowSign=true` 时显示「Sign Document」按钮；其他 3 处引用零改动 |
| `src/app/components/ai/utils/pdfDetector.ts`                    | AI PDF 检测    | 含完整 PDF.js CDN 加载逻辑，**直接复用 `loadPdfJs()`**                                                          |
| `src/app/views/onboard/onboardingList/components/Documents.vue` | Documents 组件 | 本次改造主体                                                                                                    |

### Documents.vue 改造点

**签署入口：方案 B（previewFile.vue 内置 Sign Document 按钮）**

与 BA 原型一致，Sign Document 按钮在 PDF 预览弹窗顶部工具栏内。

**实现方式：** 给 `previewFile.vue` 新增 `allowSign` prop（默认 false），只有 `Documents.vue` 传 `:allow-sign="true"`，其他 3 个引用点零改动。

**`previewFile.vue` 改造：**

- 新增 prop：`allowSign: { type: Boolean, default: false }`
- 新增 emit：`signDocument`
- 当 `allowSign = true` 且 `type = 'pdf'` 时，顶部工具栏显示「Sign Document」紫色按钮
- 点击「Sign Document」→ emit `signDocument` → `Documents.vue` 关闭预览 → 打开 `DocumentSigningDialog`

**`previewFile.vue` 的 4 个引用点影响范围：**

| 引用位置                         | 传 allowSign          | 影响                                  |
| -------------------------------- | --------------------- | ------------------------------------- |
| `Documents.vue`                  | ✅ 传 `true`          | PDF 预览弹窗出现「Sign Document」按钮 |
| `dynamicForm.vue`                | ❌ 不传（默认 false） | 零改动                                |
| `dynamicFieldRenderer/index.vue` | ❌ 不传（默认 false） | 零改动                                |
| `FileResultRenderer.vue`         | ❌ 不传（默认 false） | 零改动                                |

**`Documents.vue` 改造：**

- 点击 Preview → 现有逻辑不变，已签署 PDF 传 `:allow-sign="false"`，原始 PDF 传 `:allow-sign="true"`
- 监听 `@sign-document` 事件 → 关闭预览弹窗 → 打开 `DocumentSigningDialog`，传入当前文件 Blob 和文件信息
- 签署成功后调用 `refreshDocumentsSilently()` 刷新列表

**文件状态与按钮显示：**

| 文件状态           | Preview | Sign Document（弹窗内） | Delete |
| ------------------ | ------- | ----------------------- | ------ |
| 原始 PDF（未签署） | ✅      | ✅                      | ✅     |
| 已签署 PDF         | ✅      | ❌                      | ❌     |
| 非 PDF 文件        | ✅      | ❌                      | ✅     |

> 待 BA 确认：原型中签署入口在预览弹窗内，实现上因 iframe 沙箱限制无法复现，改为文件列表双入口方案。详见下方 BA 沟通记录。

### 已安装相关依赖

```json
{
  "@vueuse/core": "^10.11.1",
  "vuedraggable": "^4.1.0"
}
```

**需新增：**

```bash
pnpm add pdf-lib           # 前端 PDF 合成
pnpm add vue-signature-pad # Draw Tab 手写画布
```

### 后端规范

- Controller：`WebApi/Controllers/OW/DocumentSigningController.cs`，前缀 `ow/`
- 文件存储：复用现有 Blob Store 上传逻辑
- Change History：参考现有 `BaseOperationLogService`
- 多租户：已签署文件记录带 `AppCode` + `TenantId`（与签名表不同，签名表无多租户字段）
