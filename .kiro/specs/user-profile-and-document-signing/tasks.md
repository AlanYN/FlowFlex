# Implementation Plan: User Profile & Document Signing (OW-703 + OW-704)

## Overview

分两大功能模块交付：

- **OW-703**：用户个人中心 + 签名管理（Profile 页、签名 CRUD API、ff_user_signature 表）
- **OW-704**：在线 PDF 文档签署（Documents 组件改造、全屏签署弹窗、pdf-lib 合成、后端接收落库）

依赖链：`Migration（DB）` → `Backend API` → `Frontend`。前后端可在 Mock 阶段并行，完整联调需 API 就绪。

---

## Tasks

### 1. 后端基础设施（Migration、Entity、Repository）

- [x] 1.1 创建 `Migration_20260810000001_CreateUserSignatureTable`
  - 在 `SqlSugarDB/Migrations/` 下新建文件，继承规范命名格式
  - SQL：`CREATE TABLE IF NOT EXISTS ff_user_signature (id bigint PRIMARY KEY, user_id bigint NOT NULL, image_data TEXT NOT NULL, create_date timestamptz, modify_date timestamptz, create_by varchar(200), modify_by varchar(200), create_user_id bigint, modify_user_id bigint, is_valid bool NOT NULL DEFAULT true)`
  - 在 `MigrationManager.cs` 的 `migrations` 数组末尾注册此 Migration
  - _Requirements: 6.1_

- [x] 1.2 创建 `Migration_20260810000002_AddSigningFieldsToOnboardingFile`
  - SQL：为 `ff_onboarding_file` 表新增 `is_signed bool NOT NULL DEFAULT false`、`source_file_id bigint`、`file_hash varchar(64)`、`signer_name varchar(200)`、`sign_time timestamptz`
  - 全部字段使用 `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` 保证幂等
  - 在 `MigrationManager.cs` 末尾注册此 Migration
  - _Requirements: 15.4_

- [x] 1.3 新建 `UserSignature` Entity 及 Repository
  - `Domain/Entities/OW/UserSignature.cs`：继承 `EntityBaseCreateInfo`（**不**继承 `OwEntityBase`，无 app_code/tenant_id 字段），添加 `[SugarTable("ff_user_signature")]`
  - `Domain/Repository/OW/IUserSignatureRepository.cs`：继承 `IBaseRepository<UserSignature>`，声明 `GetByUserIdAsync(long userId)`
  - `SqlSugarDB/Repositories/OW/UserSignatureRepository.cs`：继承 `BaseRepository<UserSignature>`，实现 `GetByUserIdAsync`，内部使用 `.Filter(null, true)` 绕过多租户全局过滤，仅按 `user_id` 筛选（`is_valid = true`）
  - _Requirements: 6.1, 6.2, 6.3_

- [x] 1.4 更新 `OnboardingFile` Entity 新增签署字段
  - 在现有 `OnboardingFile.cs` 中添加 `IsSigned`、`SourceFileId`、`FileHash`、`SignerName`、`SignTime` 属性，附对应 `[SugarColumn]` 注解
  - _Requirements: 15.4_

---

### 2. 后端 OW-703：签名管理 API

- [x] 2.1 新建签名 DTOs
  - `Application.Contracts/Dtos/OW/UserSignature/ProfileSignatureOutputDto.cs`：包含 `Id`（LongToStringConverter）、`ImageBase64`、`CreatedDate`
  - `Application.Contracts/Dtos/OW/UserSignature/CreateSignatureInputDto.cs`：包含 `ImageBase64`（Required）
  - _Requirements: 7.5_

- [x] 2.2 新建 `IUserSignatureService` 及 `UserSignatureService`
  - `Application.Contracts/IServices/OW/IUserSignatureService.cs`：声明 `GetByCurrentUserAsync()`、`CreateAsync(CreateSignatureInputDto)`、`DeleteAsync(long signatureId)`
  - `Application/Services/OW/UserSignatureService.cs`：实现 `IScopedService`
    - `GetByCurrentUserAsync`：调用 `IUserSignatureRepository.GetByUserIdAsync(currentUserId)` 并映射为 `ProfileSignatureOutputDto`
    - `CreateAsync`：先检查 `count >= 7` 则抛 `CRMException`；再验证 base64 解码后 ≤ 500KB 否则抛 `CRMException`；写库
    - `DeleteAsync`：验证签名归属当前用户，否则抛 403 `CRMException`；软删除（`is_valid = false`）
  - _Requirements: 4.1, 4.2, 5.1, 6.3, 6.4, 6.5, 7.1, 7.2, 7.3, 7.4_

- [x] 2.3 新建 `UserSignatureMapProfile` 及注册
  - `Application/Maps/UserSignatureMapProfile.cs`：定义 `UserSignature` → `ProfileSignatureOutputDto` 的 AutoMapper 映射（`ImageData` → `ImageBase64`，`CreateDate` → `CreatedDate`）
  - 在 `Program.cs` AutoMapper 注册处添加此 Profile
  - _Requirements: 7.5_

- [x] 2.4 新建 `ProfileController`
  - `WebApi/Controllers/OW/ProfileController.cs`，路由前缀 `ow/profile/v1`
  - `GET signatures` → `IUserSignatureService.GetByCurrentUserAsync()`
  - `POST signatures` → `IUserSignatureService.CreateAsync(dto)`
  - `DELETE signatures/{signatureId}` → `IUserSignatureService.DeleteAsync(signatureId)`
  - 全部返回 `Success<T>(data)` 包装
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ]\* 2.5 后端单元测试：UserSignatureService
  - `CreateAsync` — 第 7 个签名允许创建（count 6→7），第 8 个被拒绝（count 7→8）
  - `CreateAsync` — base64 解码 > 500KB 时抛异常
  - `DeleteAsync` — 软删除后 `is_valid = false`，行仍存在
  - `DeleteAsync` — 跨用户删除返回 403
  - `GetByCurrentUserAsync` — 不含 app_code/tenant_id 条件（验证 Property 4）
  - 标注：`Feature: user-profile-and-document-signing, Property 2: 签名上限不变式`
  - 标注：`Feature: user-profile-and-document-signing, Property 3: 软删除不变式`
  - 标注：`Feature: user-profile-and-document-signing, Property 4: 用户签名数据隔离`
  - 标注：`Feature: user-profile-and-document-signing, Property 5: 跨用户删除权限拒绝`
  - _Requirements: 4.1, 4.2, 5.1, 6.3, 6.4, 6.5, 7.4_

---

### 3. 后端 OW-704：签署 API

- [x] 3.1 新建签署 DTOs
  - `Application.Contracts/Dtos/OW/DocumentSigning/SignDocumentInputDto.cs`：`IFormFile File`（Required）、`string SignerName`（Required）、`string SignedAt`（Required，ISO 8601 UTC）
  - `Application.Contracts/Dtos/OW/DocumentSigning/SignDocumentOutputDto.cs`：`SignedFileId`（LongToStringConverter）、`DownloadUrl`、`FileName`、`FileHash`
  - _Requirements: 14.4, 15.7_

- [x] 3.2 新建 `IDocumentSigningService` 及 `DocumentSigningService`
  - `Application.Contracts/IServices/OW/IDocumentSigningService.cs`：声明 `SignDocumentAsync(long fileId, SignDocumentInputDto dto)`、`ComputeSha256(byte[] data)` 、`BuildSignedFileName(string originalName, string signerName, DateTime date)`
  - `Application/Services/OW/DocumentSigningService.cs`：实现 `IScopedService`
    - `SignDocumentAsync`：
      1. 查验 `fileId` 存在且 `is_signed = false`，否则抛 400
      2. 读取文件字节流，调 `ComputeSha256` 独立计算哈希（不信任前端）
      3. 调现有 BlobProvider 上传已签署文件
      4. 开启 DB 事务：插入新 `OnboardingFile`（`is_signed=true`，`source_file_id=fileId`，`file_hash`，`signer_name`，`sign_time`）+ 写 Change_History
      5. 事务失败则 best-effort 删除 Blob，记录孤立日志，返回 500
    - `ComputeSha256`：使用 `System.Security.Cryptography.SHA256` 计算 64 位 hex
    - `BuildSignedFileName`：按规则 `{original}_已签署_{signer}_{MMDDYYYY}.pdf` 拼装
  - _Requirements: 14.4, 15.1, 15.2, 15.3, 15.4, 15.5, 15.6, 15.7, 17.1_

- [x] 3.3 新建 `DocumentSigningController`
  - `WebApi/Controllers/OW/DocumentSigningController.cs`，路由 `ow/files/{fileId}/sign`
  - `POST {fileId}/sign`：接收 `multipart/form-data`，调 `IDocumentSigningService.SignDocumentAsync`
  - 返回 `Success<SignDocumentOutputDto>(data)`
  - _Requirements: 14.4, 15.1_

- [ ]\* 3.4 后端单元测试：DocumentSigningService
  - `ComputeSha256` — 相同字节输入两次返回相同的 64 位 hex 字符串（FsCheck property，100 次随机）
  - `ComputeSha256` — 输出严格匹配正则 `^[0-9a-f]{64}$`
  - `BuildSignedFileName` — 返回值匹配正则 `^.+_已签署_.+_\d{8}\.pdf$`（FsCheck property，100 次随机）
  - `SignDocumentAsync` — `is_signed=true` 的文件返回 400
  - `SignDocumentAsync` — 事务失败时不留孤立记录
  - 标注：`Feature: user-profile-and-document-signing, Property 9: SHA-256 哈希确定性`
  - 标注：`Feature: user-profile-and-document-signing, Property 10: 已签署文件命名格式`
  - _Requirements: 15.1, 15.2, 15.5, 17.1_

---

### 4. 前端基础设施：依赖安装、工具函数、路由、API 模块

- [x] 4.1 安装前端新依赖
  - `pnpm add pdf-lib vue-signature-pad` （在 `packages/flowFlex-common/` 下执行）
  - 确认 `pdf-lib` 与 `vue-signature-pad` 均可通过 TypeScript 类型正确引用
  - _Requirements: 14.2, 3.1_

- [x] 4.2 新建坐标与边界工具函数
  - `src/app/views/onboard/onboardingList/components/signing/utils/coordinateUtils.ts`
    - 导出 `toPdfJsPt(canvasPx: number, scale: number): number`（`canvasPx / scale`）
    - 导出 `toPdfLibY(pdfJsY: number, pageHeight: number, elementHeight: number): number`（`pageHeight - pdfJsY - elementHeight`）
    - 导出 `toPdfLibCoords(x, y, w, h, scale, pageHeight)` 综合转换函数
  - `src/app/views/onboard/onboardingList/components/signing/utils/clampUtils.ts`
    - 导出 `clampPosition(x, y, w, h, pageW, pageH): { x, y }`（确保元素完整在页面内）
    - 导出 `clampSize(w, h, type: 'signature' | 'date'): { w, h }`（签名最小 50×20，日期最小 60×16）
  - _Requirements: 13.2, 13.3, 13.5, 13.6, 14.3_

- [ ]\* 4.3 工具函数属性测试（fast-check）
  - `coordinateUtils.test.ts`：`toPdfLibCoords` round-trip property（100 次随机），验证 `pageHeight - pdfLibY - elementHeight ≈ pdfJsY`
  - `clampUtils.test.ts`：`clampPosition` 不变式（`0 ≤ x ≤ pageW - w` 且 `0 ≤ y ≤ pageH - h`）（100 次随机）
  - `clampUtils.test.ts`：`clampSize` 不变式（签名 `w≥50, h≥20`；日期 `w≥60, h≥16`）（100 次随机）
  - 标注：`Feature: user-profile-and-document-signing, Property 1: 坐标换算可逆性`
  - 标注：`Feature: user-profile-and-document-signing, Property 7: 元素位置边界 clamp 不变式`
  - 标注：`Feature: user-profile-and-document-signing, Property 8: 元素尺寸 clamp 不变式`
  - _Requirements: 14.3, 13.2, 13.3, 13.5, 13.6_

- [x] 4.4 新建 Profile 路由模块
  - `src/app/router/routers/modules/profile.ts`：按 design.md 配置 `hidden: true`、`code: ''`（无需权限码）、`path: '/profile'`
  - 在路由入口文件中引入此模块
  - _Requirements: 1.2, 1.3, 1.4_

- [x] 4.5 新建 Profile API 模块
  - `src/app/apis/ow/profile.ts`
  - `getSignatures()` → `GET /ow/profile/v1/signatures`
  - `createSignature(imageBase64: string)` → `POST /ow/profile/v1/signatures`
  - `deleteSignature(signatureId: string)` → `DELETE /ow/profile/v1/signatures/{signatureId}`
  - 定义 `SignatureItem { id: string, imageBase64: string, createdDate: string }` 类型
  - _Requirements: 7.1, 7.2, 7.3, 7.5_

- [x] 4.6 新建 DocumentSigning API 模块
  - `src/app/apis/ow/documentSigning.ts`
  - `signDocument(fileId: string, formData: FormData)` → `POST /ow/files/{fileId}/sign`（multipart/form-data）
  - 定义 `SignDocumentResponse { signedFileId, downloadUrl, fileName, fileHash }` 类型
  - _Requirements: 14.4_

---

### 5. 前端 OW-703：Profile 页面

- [x] 5.1 改造 `userLayout.vue`：添加 My Profile 入口
  - 在 `<el-popover>` 内容区找到 Log Out 按钮，在其前面插入"My Profile"行
  - `goToProfile()` 调用 `router.push('/profile')`，点击后关闭 popover
  - _Requirements: 1.1, 1.2_

- [x] 5.2 新建 `DrawTab.vue`（可复用画板组件）
  - `src/app/views/onboard/onboardingList/components/signing/DrawTab.vue`（signing 目录，后续复用）
  - Props：`mode: 'profile' | 'signing'`
  - 内嵌 `vue-signature-pad`（画布 300×150px，白色背景）
  - 提供 Clear 按钮（清空画布）
  - `mode === 'profile'` 时显示"Save"按钮，点击后 emit `save(imageBase64)`
  - `mode === 'signing'` 时显示"Use Signature"按钮，点击后 emit `use(imageBase64)`
  - _Requirements: 3.1, 3.2, 3.3, 11.5, 11.6, 11.7, 11.8_

- [x] 5.3 新建 `AddSignatureDialog.vue`（Profile 页用）
  - `src/app/views/profile/components/AddSignatureDialog.vue`
  - `v-model:visible`，mode 固定为 `'profile'`
  - Tab 1 "Draw"：嵌入 `DrawTab.vue`（mode='profile'），监听 `save` 事件调 `createSignature` API，成功后 emit `saved`，关闭弹窗
  - Tab 2 "Upload"：`<el-upload>` 限制 `accept="image/png,image/jpeg"`，`beforeUpload` 验证文件类型和大小（≤ 500KB），显示预览（simulated PDF background）
  - 上传成功后调 `createSignature` API，emit `saved`
  - 展示提示文字："推荐上传透明背景的 PNG，白色背景图片会在 PDF 上显示为白色方块"
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7_

- [x] 5.4 新建 Profile 页面 `index.vue`
  - `src/app/views/profile/index.vue`
  - 页面加载时调 `getSignatures()` 获取签名列表
  - 签名卡片网格：每张卡显示 base64 PNG 预览 + 删除图标按钮
  - 删除按钮：调 `deleteSignature(id)`，成功后刷新列表
  - 空态：引导文案 + "Add Signature"按钮
  - `count >= 7` 时"Add Signature"按钮 disabled，附 ElTooltip 显示 "已达签名上限（7个），请删除后再添加"
  - 点击"Add Signature"打开 `AddSignatureDialog.vue`，监听 `saved` 事件后刷新列表
  - _Requirements: 1.3, 1.4, 2.1, 2.2, 2.3, 4.1, 4.2, 4.3, 5.1, 5.2_

- [ ]\* 5.5 前端单元测试：Profile 页面
  - `count < 7` 时"Add Signature"按钮可点击
  - `count === 7` 时按钮 disabled，tooltip 文字正确
  - 空态（`signatures.length === 0`）时渲染引导文案
  - 删除后列表更新（mock API）
  - `previewFile.vue`：`allowSign=true + type=pdf + isSigned=false` → 显示 Sign Document 按钮（4 种组合全覆盖）
  - 标注：`Feature: user-profile-and-document-signing, Property 6: allowSign 条件渲染不变式`
  - _Requirements: 2.2, 4.3, 5.2, 8.1, 8.2, 8.3_

---

### 6. 前端 OW-703：previewFile.vue 改造

- [x] 6.1 改造 `previewFile.vue`：新增 `allowSign`、`isSigned`、`fileId` props
  - 在 defineProps 中追加三个 prop（默认 `false`/`null`）
  - 在 defineEmits 中追加 `signDocument`
  - 工具栏添加条件渲染：`v-if="allowSign && type === 'pdf' && !isSigned"`，显示 primary 按钮"Sign Document"
  - `handleSignDocument`：先 `emit('closeOffice')`，再 `emit('signDocument', { fileId, fileUrl })`
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_

---

### 7. 前端 OW-704：签署弹窗基础组件

- [x] 7.1 新建 `PdfViewer.vue`
  - `src/app/views/onboard/onboardingList/components/signing/PdfViewer.vue`
  - Props：`pdfDoc: PDFDocumentProxy | null`、`pageNumber: number`、`scale: number`（50–200，百分比）
  - 在 `<canvas>` 上调用 `page.getViewport({ scale: scale/100 })`，每次 scale 变化重新渲染（**不使用 CSS transform**）
  - canvas 宽高随 viewport 同步更新
  - 渲染完成后 emit `rendered({ width, height })` 供 SigningOverlay 同步尺寸
  - 加载失败（如加密 PDF）emit `loadFailed(error)`
  - _Requirements: 9.1, 9.5, 9.6_

- [x] 7.2 新建 `SigningOverlay.vue`
  - `src/app/views/onboard/onboardingList/components/signing/SigningOverlay.vue`
  - Props：`elements: PlacedElement[]`、`canvasWidth: number`、`canvasHeight: number`、`scale: number`
  - 绝对定位覆盖在 canvas 上，`pointer-events: none`（container），每个元素 `pointer-events: auto`
  - 选中态显示 3 个 handle（左上角移动-紫色圆点、右上角删除-红色×、右下角缩放-黑色圆点）
  - 移动：`pointerdown` 调 `setPointerCapture`，计算偏移量，实时更新坐标（调 `clampPosition`），`pointerup` 调 `releasePointerCapture`
  - 缩放：同理用 Pointer Events API，实时更新尺寸（调 `clampSize`）
  - emit：`elementMoved(id, { x, y })`、`elementResized(id, { w, h })`、`elementDeleted(id)`
  - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5, 13.6_

- [x] 7.3 新建 `PageThumbnails.vue`
  - `src/app/views/onboard/onboardingList/components/signing/PageThumbnails.vue`
  - 左侧 180px 滚动列表，每项为独立 `<canvas>`
  - 通过 `IntersectionObserver` 懒加载：进入视口时调 `page.getViewport({ scale: 0.15 })` 渲染缩略图
  - 当前页高亮（border 或 bg 样式）
  - 点击 emit `pageChanged(pageIndex)`
  - _Requirements: 9.2, 9.3_

- [x] 7.4 新建 `SigningToolbar.vue`
  - `src/app/views/onboard/onboardingList/components/signing/SigningToolbar.vue`
  - Props：`mode: 'edit' | 'signed'`、`hasElements: boolean`、`isSaving: boolean`、`fileName: string`
  - `edit` 模式：Add Signature、Add Date、Confirm Signature（`disabled` 当 `!hasElements`）、× 关闭
  - `signed` 模式：Download、Print、绿色 Signed badge（文件名旁）、× 关闭
  - 全部按钮 emit 对应事件：`addSignature`、`addDate`、`confirmSignature`、`close`、`download`、`print`
  - _Requirements: 10.1, 10.2, 10.3, 16.1_

---

### 8. 前端 OW-704：AddSignatureDialog（签署流程）

- [x] 8.1 新建 `FromProfileTab.vue`
  - `src/app/views/onboard/onboardingList/components/signing/FromProfileTab.vue`
  - 组件加载时调 `getSignatures()` 获取用户签名列表
  - 有签名时：显示网格卡片 + 提示文字 "Signatures from [username]'s profile. Click one to place it."
  - 点击签名卡片：emit `signatureSelected(imageBase64)`
  - 无签名时：空态引导（去 My Profile 或切到 Draw tab 链接）
  - _Requirements: 11.2, 11.3, 11.4_

- [x] 8.2 新建 `AddSignatureDialog.vue`（签署流程版）
  - `src/app/views/onboard/onboardingList/components/signing/AddSignatureDialog.vue`
  - `v-model:visible`，两个 Tab：From Profile（嵌入 `FromProfileTab.vue`）和 Draw（嵌入 `DrawTab.vue`，mode='signing'）
  - 监听 `FromProfileTab` 的 `signatureSelected` → emit `signatureSelected(imageBase64)`，关闭弹窗
  - 监听 `DrawTab` 的 `use` → emit `signatureSelected(imageBase64)`，关闭弹窗
  - 说明：Draw tab 中绘制的签名**不存库**（不调 createSignature API）
  - _Requirements: 11.1, 11.5, 11.6, 11.7, 11.8_

---

### 9. 前端 OW-704：pdf-lib 合成工具

- [x] 9.1 新建 `pdfSynthesis.ts`
  - `src/app/views/onboard/onboardingList/components/signing/utils/pdfSynthesis.ts`
  - 导出 `async function synthesizePdf(pdfUrl: string, elements: Map<number, PlacedElement[]>): Promise<ArrayBuffer>`
  - 内部流程：
    1. `fetch(pdfUrl)` → `ArrayBuffer` → `PDFDocument.load(arrayBuffer)`
    2. 遍历 `elements` Map，对每个页面：
       - 取 `pdfDoc.getPage(pageIndex)` 的 `getSize()` 获得 `pageHeight`
       - 按 `toPdfLibCoords` 换算坐标（pdfJsPt → pdf-lib 坐标系，原点左下）
       - `type === 'signature'`：base64 → ArrayBuffer → `pdfDoc.embedPng(...)` → `page.drawImage(...)`
       - `type === 'date'`：`page.drawText(dateText, { x, y, size })` 使用 Helvetica 字体
    3. 返回 `pdfDoc.save()` 的 `ArrayBuffer`
  - _Requirements: 14.2, 14.3, 18.2_

---

### 10. 前端 OW-704：DocumentSigningDialog 主组件

- [x] 10.1 新建 `DocumentSigningDialog.vue`
  - `src/app/views/onboard/onboardingList/components/signing/DocumentSigningDialog.vue`
  - Props：`visible: boolean`、`fileId: string|number`、`fileUrl: string`、`fileName: string`
  - 全屏弹窗（`width: 100%`，`height: 100%`）
  - 布局：左侧 `PageThumbnails`（180px）、中间 `PdfViewer`+`SigningOverlay` 叠层、右上 `SigningToolbar`
  - 内部状态管理（`mode`、`pdfDoc`、`currentPage`、`totalPages`、`scale`、`elements: Map<number, PlacedElement[]>`、`isSaving`、`signedFileData`）
  - 弹窗打开时：调 `loadPdfJs()`（复用项目封装），加载 PDF，进入 `edit` 模式
  - 加载失败时：显示 "该 PDF 已加密，无法在线签署"，不进入编辑模式
  - 翻页时：`elements` Map 保留所有页的数据（不清空其他页）
  - 显示"Page X / Y"页码
  - 缩放范围：50–200%
  - emit `refreshDocuments` 给父组件（签署成功后刷新文件列表）
  - _Requirements: 9.1, 9.2, 9.4, 9.5, 9.6, 9.7, 10.1, 18.1_

- [x] 10.2 实现 Add Signature 流程
  - 工具栏 `addSignature` 事件 → 打开 `AddSignatureDialog`（signing 版）
  - 监听 `signatureSelected(imageBase64)` → 在当前页 `elements` Map 追加新 `PlacedElement`：`type='signature'`、`x/y` 居中、初始尺寸 150×60 pt、id = `uuid()`
  - 超过 10 个签名元素时禁用 Add Signature 并 `ElMessage.warning`
  - _Requirements: 11.3, 12.4_

- [x] 10.3 实现 Add Date 流程
  - 工具栏 `addDate` 事件 → 直接在当前页 `elements` Map 追加新 `PlacedElement`：`type='date'`、`x/y` 居中、初始尺寸 100×20 pt、`dateText` 格式 `MM/DD/YYYY`（今天日期）
  - 超过 10 个日期元素时禁用 Add Date 并 `ElMessage.warning`
  - _Requirements: 12.1, 12.2, 12.3_

- [x] 10.4 实现 SigningOverlay 元素交互联动
  - 监听 `SigningOverlay` 的 `elementMoved`、`elementResized`、`elementDeleted` 事件
  - 对应更新 `elements` Map 中对应页的元素状态（immutable 更新，触发响应式）
  - 关闭（×）时：有元素则 `ElMessageBox.confirm`，确认后清空 elements 并关闭；取消则留在编辑态
  - _Requirements: 10.4, 10.5, 13.1, 13.4_

- [x] 10.5 实现 Confirm Signature 流程（pdf-lib 合成 + 上传）
  - 工具栏 `confirmSignature` 事件 → `ElMessageBox.confirm`（警告不可修改）
  - 用户确认后：
    1. `isSaving = true`
    2. 调 `synthesizePdf(fileUrl, elements)` 得到 `ArrayBuffer`
    3. 构建 `FormData`（File 对象 + signerName + signedAt ISO 8601 UTC）
    4. 调 `signDocument(fileId, formData)`
    5. 成功：`mode = 'signed'`，保存 `signedFileData`，emit `refreshDocuments`
    6. 失败：`isSaving = false`，`ElMessage.error`，**保留编辑模式和所有已放置元素**
  - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5, 14.6_

- [x] 10.6 实现已签署只读模式
  - `mode === 'signed'` 时 `SigningToolbar` 显示 Download、Print、绿色 Signed badge
  - Download：`window.open(signedFileData.downloadUrl)` 或构造 `<a download>`
  - Print：`window.open(url)` 后 `.print()`
  - _Requirements: 16.1, 16.2, 16.3_

---

### 11. 前端 OW-704：Documents.vue 改造

- [x] 11.1 改造 `Documents.vue`：集成签署入口与状态展示
  - 打开 `previewFile` 时传入 `:allow-sign="true"`、`:is-signed="row.isSigned"`、`:file-id="row.id"`
  - 监听 `@sign-document="handleSignDocument"` 事件，响应函数打开 `DocumentSigningDialog.vue`
  - 文件列表新增列/tag：
    - 已签署文件（`isSigned=true`）：绿色 "Signed" badge、signer name、sign time（格式化显示）
    - 已签署文件：隐藏 Delete 按钮
  - 监听 `DocumentSigningDialog` 的 `refreshDocuments` 事件，触发文件列表刷新
  - _Requirements: 8.1, 8.2, 16.4, 16.5, 16.6_

---

### 12. 集成测试与端到端验证

- [ ] 12.1 联调检查点（OW-703）
  - 用真实用户账号验证：My Profile 入口可见 → 导航到 `/profile` → 新增签名（Draw + Upload 两种方式）→ 签名卡片正常展示 → 删除签名后列表更新
  - 验证 7 个上限：第 7 个可添加，尝试第 8 个提示正确且按钮 disabled
  - 确认 API 响应 `GET /ow/profile/v1/signatures` 不包含其他用户的签名
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 12.2 联调检查点（OW-704）
  - 在 Documents 组件选择 PDF 文件 → 预览弹窗显示"Sign Document"按钮 → 点击进入全屏签署弹窗
  - 添加签名元素（From Profile Tab）和日期元素 → 拖拽/缩放/删除交互正常
  - 跨页放置元素后翻回验证元素保留
  - 点击 Confirm Signature → 确认弹窗 → 合成上传 → 进入已签署只读模式
  - Documents 列表刷新后显示绿色 Signed badge、签署人、时间，Delete 按钮消失
  - 已签署 PDF 不显示"Sign Document"按钮
  - Ensure all tests pass, ask the user if questions arise.

- [ ]\* 12.3 前端集成测试：DocumentSigningDialog 核心流程
  - 跨页元素保留（Property 11）：翻页后返回，`elements Map` 中原页元素不丢失
  - Confirm 失败时保留编辑模式（模拟 API 500）
  - `pdfSynthesis.ts`：多页元素嵌入位置正确（mock pdf-lib）
  - 标注：`Feature: user-profile-and-document-signing, Property 11: 跨页元素完整性`
  - _Requirements: 14.6, 18.1, 18.2_

---

## Notes

- 任务标注 `*` 为可选（测试类），可跳过以加快 MVP 交付，后续补充
- Migration 必须在应用启动时自动执行，无需手动运行任何命令
- `UserSignatureRepository` 的 `.Filter(null, true)` 是绕过多租户的关键，不能省略
- `previewFile.vue` 的 `allowSign` 默认 `false`，确保其他所有调用方不受影响
- pdf-lib 坐标原点在**左下角**，PDF.js 坐标原点在**左上角**，坐标换算必须经过 `coordinateUtils.ts`
- `DrawTab.vue` 设计为可复用：Profile 页面和签署弹窗均可使用，通过 `mode` prop 区分行为
- 后端 `DocumentSigningService.SignDocumentAsync` 的原子性：DB 事务失败后须 best-effort 清理 Blob

---

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "4.1"] },
    { "id": 1, "tasks": ["1.3", "1.4", "4.2", "4.4"] },
    { "id": 2, "tasks": ["2.1", "3.1", "4.3", "4.5", "4.6"] },
    { "id": 3, "tasks": ["2.2", "3.2", "5.1", "5.2"] },
    {
      "id": 4,
      "tasks": ["2.3", "2.4", "3.3", "5.3", "6.1", "7.1", "7.2", "7.3", "7.4"]
    },
    { "id": 5, "tasks": ["2.5", "3.4", "5.4", "8.1", "9.1"] },
    { "id": 6, "tasks": ["5.5", "8.2", "10.1", "11.1"] },
    { "id": 7, "tasks": ["10.2", "10.3", "10.4"] },
    { "id": 8, "tasks": ["10.5", "10.6"] },
    { "id": 9, "tasks": ["12.1", "12.2"] },
    { "id": 10, "tasks": ["12.3"] }
  ]
}
```
