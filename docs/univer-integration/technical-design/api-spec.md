# API 规范 — Univer 集成

> 最后更新：2026-09-22  
> 认证：Bearer Token（JWT）  
> 响应格式：`SuccessResponse<T>` 统一包装，前端 Axios 拦截器自动 unwrap

---

## 一、文档快照 API（DocumentSnapshotController）

**路由前缀：** `ow/document-snapshots/v1`  
**权限：** `[WFEAuthorize(PermissionConsts.Document.*)]`

---

### GET `ow/document-snapshots/v1/:unitId` — 加载文档快照

**权限：** `DOCUMENT:READ`

**路径参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| unitId | string | Univer 文档唯一 ID |

**响应：** `SuccessResponse<DocumentSnapshotOutputDto>`

```json
{
  "id": "1234567890",
  "unitId": "550e8400-e29b-41d4-a716-446655440000",
  "title": "销售数据分析表",
  "docType": "Sheet",
  "dataJson": "{\"id\":\"workbook-01\",\"name\":\"...\"}",
  "revision": 6,
  "createBy": "张三",
  "createDate": "2026-09-20T10:00:00Z",
  "modifyDate": "2026-09-22T14:30:00Z"
}
```

**错误码：**

| 错误码 | 说明 | 前端处理 |
|--------|------|---------|
| 404 | 文档不存在或无权限 | ElMessage.error("文档不存在") |

---

### POST `ow/document-snapshots/v1` — 创建文档

**权限：** `DOCUMENT:CREATE`

**请求体：** `DocumentSnapshotInputDto`

```json
{
  "title": "新文档",
  "docType": "Sheet",
  "dataJson": "{}",
  "entityType": null,
  "entityId": null
}
```

**响应：** `SuccessResponse<string>` — 新文档的 `unitId`（UUID）

---

### POST `ow/document-snapshots/v1/:unitId/save` — 保存/更新快照（Upsert）

**权限：** `DOCUMENT:UPDATE`

**路径参数：** `unitId`

**请求体：**

```json
{
  "title": "销售数据分析表",
  "docType": "Sheet",
  "dataJson": "{\"id\":\"workbook-01\",\"name\":\"...\",\"rev\":7}"
}
```

**响应：** `SuccessResponse<bool>`

---

### DELETE `ow/document-snapshots/v1/:id` — 删除文档（软删除）

**权限：** `DOCUMENT:DELETE`

**路径参数：** `id`（数字 ID，非 unitId）

**响应：** `SuccessResponse<bool>`

---

### POST `ow/document-snapshots/v1/query` — 分页查询文档列表

**权限：** `DOCUMENT:READ`

**请求体：** `DocumentSnapshotQueryRequest`

```json
{
  "keyword": "销售",
  "docType": "Sheet",
  "sortBy": "modify_date",
  "sortOrder": "desc",
  "pageIndex": 1,
  "pageSize": 20
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| keyword | string | — | 标题模糊搜索 |
| docType | string | — | Sheet / Doc / Slide，空则全部 |
| sortBy | string | — | modify_date（默认）/ create_date / title |
| sortOrder | string | — | desc（默认）/ asc |
| pageIndex | int | ✅ | 页码，从 1 开始 |
| pageSize | int | ✅ | 每页条数，默认 20，最大 100 |

**响应：** `SuccessResponse<PagedResult<DocumentSnapshotOutputDto>>`

---

### GET `ow/document-snapshots/v1/by-entity/:entityType/:entityId` — 按业务实体查询

**权限：** `DOCUMENT:READ`

**路径参数：** entityType（如 "Onboarding"）、entityId

**响应：** `SuccessResponse<List<DocumentSnapshotOutputDto>>`

---

## 二、操作日志 API（DocumentOperationLogController）

**路由前缀：** `ow/document-operation-logs/v1`

---

### POST `ow/document-operation-logs/v1` — 上报操作日志

**权限：** `DOCUMENT:UPDATE`

**请求体：** `DocumentOperationLogInputDto`

```json
{
  "unitId": "550e8400-...",
  "userId": "user-001",
  "userName": "张三",
  "timestamp": 1727012345678,
  "mutationId": "sheet.mutation.set-range-values",
  "paramsJson": "{\"range\":{...},\"cellValue\":{...}}",
  "revision": 6
}
```

**响应：** `SuccessResponse<string>` — 日志 ID

---

### GET `ow/document-operation-logs/v1/:unitId` — 查询操作历史

**权限：** `DOCUMENT:READ`

**查询参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| from | datetime? | 开始时间（UTC） |
| to | datetime? | 结束时间（UTC） |
| userId | string? | 过滤指定用户 |
| pageIndex | int | 默认 1 |
| pageSize | int | 默认 50 |

**响应：** `SuccessResponse<PagedResult<DocumentOperationLogOutputDto>>`

---

### GET `ow/document-operation-logs/v1/:unitId/revision` — 获取当前版本号

**权限：** `DOCUMENT:READ`

**响应：** `SuccessResponse<int>` — 当前 revision

---

## 三、协同编辑 WebSocket（`/ws/collab/{unitId}`）

**实现文件：** `WebApi/Hubs/CollabWebSocketEndpoint.cs`、`WebApi/Hubs/CollabRoomRegistry.cs`
**前端实现：** `src/app/composables/useCollaboration.ts`

> 浏览器无法直接完成 SignalR 握手（需要 `@microsoft/signalr` 客户端），因此实际生效的是**裸 WebSocket** 端点：
> `WebApi/Hubs/CollabHub.cs`（SignalR）与 `/collab/{unitId}` 保留为方案 A 的遗留实现，Web 端不使用。

**端点：** `wss://{host}/ws/collab/{unitId}?userId={id}&userName={name}&access_token={jwt}`
**开发代理：** `vite.config.ts` 中 `/ws` → `VITE_PROXY_URL`（`ws: true`），前端只需连 `ws://localhost:5173/ws/collab/...`
**认证：** `access_token` 可选（JWT Bearer）；当前 userId/userName 由 query 传入，后续应从 Token 解析（见 `CollabWebSocketEndpoint.cs` 内 TODO）
**房间状态：** 单实例内存态（成员名单 / presence / revision），多副本部署需替换为 Redis 或粘性会话

### 客户端 → 服务端消息

| cmd | 载荷 | 说明 |
|-----|------|------|
| `join` | — | 身份已随 query 携带，服务端忽略（幂等） |
| `presence` | `presence: { sheetId, a1, text, editing }` | 上报光标 / 选区 / 正在编辑的内容 |
| `ingest` | `cs: { unitId, userId, baseRev, mutations: [{ id, data }] }` | 提交编辑 Changeset（乐观锁） |
| `ping` | — | 心跳（前端每 25s） |
| `leave` | — | 主动离开房间 |

### 服务端 → 客户端消息

| eventID | 说明 | 数据 |
|---------|------|------|
| `init` | 加入成功后返回房间快照 | `{ unitId, revision, self, members: [{ userId, name, color, presence }] }` |
| `join` | 有人加入房间 | `{ member: { userId, name, color, presence } }` |
| `leave` | 某人的连接全部断开 | `{ memberID }` |
| `presence` | 他人光标 / 编辑状态 | `{ memberID, presence: { sheetId, a1, text, editing } }` |
| `new_cs` | 广播他人 Changeset | `{ cs: { revision, mutations } }` |
| `cs_ack` | 确认我的 Changeset | `{ cs: { revision, mutations } }` |
| `cs_rej` | 版本冲突，拒绝 | `{ cs: { revision（服务端当前版本）, mutations } }`，客户端采用服务端 revision 后重试（最多 5 次） |
| `pong` | 心跳响应 | `{}` |

### presence 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| sheetId | string? | 当前工作表 id（Sheets） |
| a1 | string? | 当前选区 A1 记法，如 `B3`、`B3:D5`；Docs 为空 |
| text | string? | 选中内容或正在输入的内容预览（截断 60 字符） |
| editing | bool | 是否处于单元格编辑态 |

### 颜色

10 色调色板按 `userId` 哈希取模，前后端算法一致（`useCollaboration.ts#colorForUser` ↔ `CollabRoomRegistry.ColorFor`），
同一用户在两端拿到的颜色一定相同。

---

## 四、统一响应格式

```json
{
  "code": "200",
  "data": {},
  "message": "Success"
}
```

前端 Axios `transformResponseHook` 自动 unwrap `data` 字段，业务代码直接拿到数据。
