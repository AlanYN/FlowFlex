# Requirements Document

## Introduction

本 spec 合并 OW-703 与 OW-704 两张票，实现**用户个人中心 + 多签名管理**以及**在线 PDF 文档签署**两个功能。

OW-703 建立签名数据基础：用户可在个人中心预设多个电子签名（手写或上传），签名跟人走、不做租户隔离。OW-704 在此之上实现业务闭环：用户在 Stage 的 Documents 组件里预览 PDF 时，可拖拽放置签名和日期元素，由前端 pdf-lib 合成已签署 PDF，再由后端接收、计算 SHA-256 哈希并落库存储。

两者是前置依赖关系：OW-703 的签名列表 API 是 OW-704 "From Profile" Tab 的数据来源。

**技术栈**：前端 Vue 3.5 + TypeScript + Element Plus，后端 .NET 8 + ASP.NET Core + SqlSugar ORM + PostgreSQL。

---

## Glossary

- **Profile_Page**：位于 `/profile` 路由的用户个人中心页面，通过顶部导航右上角用户下拉菜单访问
- **Signature**：用户预设的电子签名，以 base64 PNG 格式存储在 `ff_user_signature` 表，与用户绑定、不含租户字段
- **Signature_Canvas**：手写签名的 HTML Canvas 区域，基于 `vue-signature-pad` 库实现
- **Documents_Component**：Stage 中的 Documents 类型组件，文件路径为 `Documents.vue`，本次为签署入口的主要改造点
- **PreviewFile_Dialog**：通用文件预览弹窗组件（`previewFile.vue`），新增 `allowSign` prop 后可显示"Sign Document"按钮
- **Signing_Dialog**：全屏文档签署弹窗（`DocumentSigningDialog.vue`），包含 PDF 渲染、签名元素叠加层和工具栏三态
- **Signing_Overlay**：叠加在 PDF canvas 上方的透明交互层，承载所有可拖拽的签名/日期元素
- **Signature_Element**：放置在 PDF 页面上的签名图片元素，支持移动、缩放、删除
- **Date_Element**：放置在 PDF 页面上的日期文字元素，内容自动填充当天日期（MM/DD/YYYY），支持移动、缩放、删除
- **PDF_Viewer**：基于 PDF.js 渲染 PDF 页面的 canvas 组件
- **AddSignature_Dialog**：点击"Add Signature"时弹出的弹窗，含 From Profile 和 Draw 两个 Tab
- **Signed_File**：已经过签署确认、由后端存储并记录 SHA-256 哈希的 PDF 文件，`is_signed = true`
- **Source_File**：被签署前的原始 PDF 文件，签署后不被修改
- **File_Hash**：后端收到已签署 PDF 后自行计算的 SHA-256 哈希值（64 位 hex），用于防篡改检测
- **Blob_Store**：项目现有的对象存储服务（Aliyun OSS 或 AWS S3），文件通过现有上传逻辑存储
- **Change_History**：Case 的变更历史记录，签署操作须写入此记录

---

## Requirements

### Requirement 1：Profile 页面入口

**User Story：** As a user, I want to access my personal profile from the top navigation, so that I can manage my signatures without navigating through the sidebar.

#### Acceptance Criteria

1. WHEN a user opens the top-right user dropdown menu in the navigation bar, THE Profile_Page entry SHALL be displayed between the user's name/email row and the "Log Out" button
2. WHEN a user clicks the "My Profile" entry in the dropdown menu, THE System SHALL navigate the user to `/profile`
3. THE Profile_Page SHALL be accessible at the URL `/profile` without requiring a permission code assignment
4. THE Profile_Page SHALL NOT appear in the sidebar navigation menu

---

### Requirement 2：签名列表展示

**User Story：** As a user, I want to see all my saved signatures on the Profile page, so that I can review and manage them.

#### Acceptance Criteria

1. WHEN a user visits the Profile_Page, THE System SHALL display all of the user's saved Signatures as preview cards in a list
2. WHEN a user has no saved Signatures, THE System SHALL display an empty state with guidance to add a new signature
3. WHEN a user has one or more Signatures, THE System SHALL display each Signature's preview image in its card

---

### Requirement 3：新增签名

**User Story：** As a user, I want to add new signatures by drawing or uploading an image, so that I can have multiple signatures available for document signing.

#### Acceptance Criteria

1. WHEN a user clicks the "Add Signature" button on the Profile_Page, THE System SHALL open the signature creation dialog with a Draw tab and an Upload tab
2. WHEN a user draws on the Signature_Canvas and clicks "Save", THE System SHALL save the drawn signature as a base64 PNG to the user's Signature list
3. WHEN a user clicks "Clear" on the Signature_Canvas, THE Signature_Canvas SHALL be cleared and ready for a new drawing
4. WHEN a user uploads a PNG or JPG file of size ≤ 500KB via the Upload tab, THE System SHALL display a preview of the uploaded image on a simulated PDF background before saving
5. WHEN a user uploads a file larger than 500KB, THE System SHALL reject the upload and display an error message
6. WHEN a user uploads a file that is not PNG or JPG, THE System SHALL reject the upload and display an error message
7. WHERE the upload feature is available, THE System SHALL display the guidance text: "推荐上传透明背景的 PNG，白色背景图片会在 PDF 上显示为白色方块"

---

### Requirement 4：签名数量上限

**User Story：** As a user, I want the system to limit the number of signatures I can save, so that the signature list remains manageable.

#### Acceptance Criteria

1. THE System SHALL allow a user to save a maximum of 7 Signatures
2. WHEN a user already has 7 saved Signatures and attempts to add another, THE System SHALL display the message "已达签名上限（7个），请删除后再添加" and prevent the addition
3. IF the "Add Signature" button is visible when the user has 7 Signatures, THEN THE System SHALL disable the button

---

### Requirement 5：删除签名

**User Story：** As a user, I want to delete any of my saved signatures, so that I can keep my signature list up to date.

#### Acceptance Criteria

1. WHEN a user clicks the delete action on a Signature card, THE System SHALL remove that Signature from the user's list permanently
2. WHEN a Signature is deleted, THE System SHALL update the displayed list immediately to reflect the removal

---

### Requirement 6：签名数据存储（后端）

**User Story：** As a system, I want to store user signatures in a dedicated table without tenant isolation, so that a user's signatures are accessible across all tenants they belong to.

#### Acceptance Criteria

1. THE System SHALL store each Signature in the `ff_user_signature` table with fields: `id`, `user_id`, `image_data` (base64 PNG), `create_date`, `modify_date`, `create_by`, `modify_by`, `is_valid`
2. THE `ff_user_signature` table SHALL NOT contain `app_code` or `tenant_id` fields
3. WHEN querying Signatures for the current user, THE System SHALL filter only by `user_id`, without applying the multi-tenant global filter
4. WHEN a Signature is deleted, THE System SHALL set `is_valid = false` rather than physically deleting the row
5. WHEN a new Signature is submitted with base64 data that decodes to more than 500KB, THE System SHALL reject the request and return an error

---

### Requirement 7：签名管理 API

**User Story：** As the frontend, I want REST APIs for signature CRUD operations, so that the Profile page and Document Signing feature can retrieve and manage signatures.

#### Acceptance Criteria

1. THE System SHALL expose `GET /ow/profile/signatures` to return all valid Signatures of the currently authenticated user
2. THE System SHALL expose `POST /ow/profile/signatures` to accept a `{ imageBase64: string }` body and create a new Signature for the current user
3. THE System SHALL expose `DELETE /ow/profile/signatures/{signatureId}` to soft-delete the specified Signature belonging to the current user
4. WHEN a `DELETE` request targets a Signature that does not belong to the current user, THE System SHALL return a 403 error
5. THE `GET /ow/profile/signatures` response SHALL follow the standard `Success<T>` envelope format with a list of `{ id, imageBase64, createdDate }` objects

---

### Requirement 8：PDF 预览与 Sign Document 入口

**User Story：** As a user, I want to see a "Sign Document" button when previewing an original PDF, so that I can initiate the signing flow directly from the preview.

#### Acceptance Criteria

1. WHEN the Documents_Component opens PreviewFile_Dialog for an original (unsigned) PDF file with `allowSign = true`, THE PreviewFile_Dialog SHALL display a "Sign Document" button in the toolbar
2. WHEN the Documents_Component opens PreviewFile_Dialog for a Signed_File, THE PreviewFile_Dialog SHALL NOT display the "Sign Document" button
3. WHEN the Documents_Component opens PreviewFile_Dialog for a non-PDF file, THE PreviewFile_Dialog SHALL NOT display the "Sign Document" button
4. WHEN a user clicks the "Sign Document" button in PreviewFile_Dialog, THE System SHALL close PreviewFile_Dialog and open the Signing_Dialog for that file
5. THE `allowSign` prop on PreviewFile_Dialog SHALL default to `false`, ensuring all other references to PreviewFile_Dialog are unaffected

---

### Requirement 9：PDF 渲染与页面导航

**User Story：** As a user, I want to navigate through PDF pages and zoom in/out within the signing dialog, so that I can precisely position signature and date elements.

#### Acceptance Criteria

1. WHEN the Signing_Dialog opens, THE PDF_Viewer SHALL render the first page of the PDF using PDF.js at the default scale of 100%
2. THE Signing_Dialog SHALL display page thumbnails in the left sidebar with lazy loading via IntersectionObserver
3. WHEN a user clicks a thumbnail in the sidebar, THE PDF_Viewer SHALL navigate to and render that page
4. WHEN a user clicks the previous or next page button, THE PDF_Viewer SHALL navigate to the adjacent page and display "Page X / Y" in the toolbar
5. WHEN a user changes the zoom level between 50% and 200%, THE PDF_Viewer SHALL re-render the canvas using the new viewport scale rather than applying a CSS transform
6. WHEN PDF.js fails to load a PDF (e.g., encrypted file), THE System SHALL immediately display the message "该 PDF 已加密，无法在线签署" and SHALL NOT enter signing edit mode
7. WHEN a PDF exceeds 20MB or 100 pages, THE System SHALL display a loading notice without blocking the user

---

### Requirement 10：签署编辑模式与工具栏

**User Story：** As a user, I want a clear set of tools in signing edit mode, so that I can add, position, and confirm signature and date elements.

#### Acceptance Criteria

1. WHEN a user enters signing edit mode, THE Signing_Dialog toolbar SHALL display: "Add Signature", "Add Date", "Confirm Signature" (primary), and "×" (close)
2. WHEN no Signature_Elements or Date_Elements are placed on any page, THE "Confirm Signature" button SHALL be disabled
3. WHEN at least one element is placed, THE "Confirm Signature" button SHALL be enabled
4. WHEN a user clicks "×" (close) while at least one element is placed, THE System SHALL display a confirmation dialog asking the user to confirm discarding unsaved changes
5. WHEN a user confirms the discard dialog, THE Signing_Dialog SHALL close and all placed elements SHALL be discarded without persistence

---

### Requirement 11：Add Signature 弹窗

**User Story：** As a user, I want to pick a saved signature or draw a new one when adding a signature element, so that I can place my preferred signature on the document.

#### Acceptance Criteria

1. WHEN a user clicks "Add Signature" in signing edit mode, THE AddSignature_Dialog SHALL open with two tabs: "From Profile" and "Draw"
2. WHEN the "From Profile" tab is active and the user has saved Signatures, THE AddSignature_Dialog SHALL display all Signatures as clickable cards with image previews and the prompt text "Signatures from [username]'s profile. Click one to place it."
3. WHEN a user clicks a Signature card in the "From Profile" tab, THE AddSignature_Dialog SHALL close and a new Signature_Element using that signature image SHALL appear centered on the current PDF page at an initial size of 150×60 pt
4. WHEN the "From Profile" tab is active and the user has no saved Signatures, THE AddSignature_Dialog SHALL display guidance to visit My Profile or switch to the Draw tab
5. WHEN the "Draw" tab is active, THE AddSignature_Dialog SHALL display a Signature_Canvas with the prompt "Draw your signature below.", a "Clear" button, and a "Use Signature" button
6. WHEN a user draws on the Signature_Canvas and clicks "Use Signature", THE AddSignature_Dialog SHALL close and a new Signature_Element using the drawn signature SHALL appear centered on the current PDF page
7. WHEN a user draws on the Signature_Canvas and clicks "Clear", THE Signature_Canvas SHALL be cleared without closing the dialog
8. THE System SHALL NOT automatically save a signature drawn in the Draw tab to the user's Profile Signature list

---

### Requirement 12：Add Date 元素

**User Story：** As a user, I want to add a date element to the PDF with a single click, so that I can quickly stamp the signing date without any additional dialog.

#### Acceptance Criteria

1. WHEN a user clicks "Add Date" in signing edit mode, THE System SHALL place a new Date_Element centered on the current PDF page at an initial size of 100×20 pt without opening any dialog
2. THE Date_Element content SHALL be automatically set to the current date in MM/DD/YYYY format
3. THE System SHALL allow a maximum of 10 Date_Elements to be placed across all pages in a single signing session
4. THE System SHALL allow a maximum of 10 Signature_Elements to be placed across all pages in a single signing session

---

### Requirement 13：签名/日期元素交互

**User Story：** As a user, I want to move, resize, and delete signature and date elements on the PDF, so that I can precisely position them where required.

#### Acceptance Criteria

1. WHEN a Signature_Element or Date_Element is selected, THE Signing_Overlay SHALL display a move handle at the top-left corner, a delete button at the top-right corner, and a resize handle at the bottom-right corner
2. WHEN a user drags the move handle of an element, THE System SHALL move the element following the pointer using the Pointer Events API, clamping the element's position to stay within the PDF page boundaries
3. WHEN a user drags the resize handle of an element, THE System SHALL resize the element following the pointer using the Pointer Events API, with a minimum size of 50×20 pt for Signature_Elements and 60×16 pt for Date_Elements
4. WHEN a user clicks the delete button of an element, THE System SHALL remove that element from the Signing_Overlay
5. WHEN a user drags an element to a position that would exceed the PDF page boundary, THE System SHALL clamp the element's position so it remains fully within the page
6. WHEN a user attempts to resize an element below its minimum dimensions, THE System SHALL clamp the size to the minimum value

---

### Requirement 14：签署确认与 PDF 合成

**User Story：** As a user, I want to confirm my signature placement and have the system generate a signed PDF, so that the signed document can be stored and shared.

#### Acceptance Criteria

1. WHEN a user clicks "Confirm Signature" (with at least one element placed), THE System SHALL display a confirmation dialog explaining that the document cannot be modified after signing
2. WHEN a user confirms in the confirmation dialog, THE System SHALL use pdf-lib to embed all placed Signature_Elements and Date_Elements into the PDF in the browser, producing a signed PDF ArrayBuffer
3. WHEN embedding Signature_Elements, THE System SHALL convert canvas pixel coordinates to PDF.js points and then convert to pdf-lib coordinates (origin at bottom-left) using the formula: `pdfLibY = pageHeight - pdfJsY - elementHeight`
4. WHEN the PDF synthesis completes, THE System SHALL POST the signed PDF file and signing metadata (`signerName`, `signedAt` in ISO 8601 UTC) to `POST /ow/files/{fileId}/sign` as `multipart/form-data`
5. WHEN the backend returns a success response, THE System SHALL transition the Signing_Dialog to the signed read-only mode
6. IF the PDF synthesis or the backend upload fails, THEN THE System SHALL retain the signing edit mode with all placed elements intact and display an error message allowing the user to retry

---

### Requirement 15：后端签署处理

**User Story：** As the system, I want to receive the signed PDF, compute its hash, and store it securely, so that document integrity can be verified later.

#### Acceptance Criteria

1. WHEN the backend receives a `POST /ow/files/{fileId}/sign` request, THE System SHALL verify that the specified file exists and that `is_signed = false`; IF the file is already signed, THE System SHALL reject the request with an error
2. WHEN the backend accepts the signed PDF, THE System SHALL compute the SHA-256 hash of the received file independently, without trusting any hash value provided by the frontend
3. WHEN the hash is computed, THE System SHALL upload the signed PDF to the Blob_Store using the existing upload logic
4. WHEN the file is stored, THE System SHALL write a signing record containing: `source_file_id`, `file_hash` (SHA-256 hex), `signer_name`, `sign_time` (UTC), `is_signed = true`
5. IF any step in the signing pipeline (hash computation, upload, or database write) fails, THEN THE System SHALL roll back the operation atomically, leaving no orphaned files or partial records
6. WHEN a signing record is created, THE System SHALL append an entry to the Case Change_History for audit purposes
7. THE System SHALL return a response containing `signedFileId`, `downloadUrl`, `fileName`, and `fileHash`

---

### Requirement 16：已签署状态与文件列表

**User Story：** As a user, I want to see clearly which documents are signed and be able to download or print them, so that I can access and share the finalized documents.

#### Acceptance Criteria

1. WHEN the Signing_Dialog enters signed read-only mode, THE toolbar SHALL display "Download" and "Print" buttons, the signing action buttons (Add Signature, Add Date, Confirm Signature) SHALL be hidden, and a green "Signed" badge SHALL appear next to the file name
2. WHEN a user clicks "Download" in signed read-only mode, THE System SHALL initiate a browser download of the Signed_File
3. WHEN a user clicks "Print" in signed read-only mode, THE System SHALL trigger the browser print dialog for the Signed_File
4. WHEN the Documents_Component displays the file list, THE Signed_File entry SHALL show a green "Signed" badge, the signer's name, and the signing timestamp
5. WHEN the Documents_Component displays the file list, THE Signed_File entry SHALL NOT display a "Delete" button
6. WHEN a Source_File is deleted from the Documents_Component, THE associated Signed_Files SHALL remain accessible (soft association, no foreign key constraint)

---

### Requirement 17：签署后文件命名

**User Story：** As a user, I want the signed PDF to have a recognizable file name, so that I can easily identify it among other documents.

#### Acceptance Criteria

1. WHEN a Signed_File is created, THE System SHALL name it following the pattern `{original_file_name}_已签署_{signer_name}_{date}.pdf`, where `{date}` is in the format MMDDYYYY

---

### Requirement 18：跨页签署支持

**User Story：** As a user, I want to place signature and date elements on multiple pages of the same PDF, so that I can sign documents requiring signatures on more than one page.

#### Acceptance Criteria

1. WHEN a user navigates to a different page in signing edit mode, THE Signing_Overlay SHALL preserve all elements placed on previously visited pages
2. WHEN pdf-lib synthesizes the final PDF, THE System SHALL embed all elements placed on all pages into their corresponding pages in the output file

---
