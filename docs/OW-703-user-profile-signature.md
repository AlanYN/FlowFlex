# OW-703 — User Profile with Signature Management

> **优先级**: P1  
> **Sprint**: OW.2026.08/07–08/20  
> **状态**: New  
> **指派**: Kai Li  
> **创建**: Amanda Li  
> **相关票**: OW-704（本票为 OW-704 的前置依赖）

---

## 1. 需求摘要

新增 **User Profile（个人中心）** 页面入口，MVP 阶段仅包含**签名管理**功能。用户可在此预设**多个**电子签名，供 OW-704 Document Signing 签署 PDF 时选择使用。

---

## 2. 与 OW-704 的关系

User Profile 中保存的签名是「签名源」。Document Signing（OW-704）签署文档时，从 Add Signature 弹窗的 From Profile Tab 中展示用户所有已保存签名，用户点选其中一个放置到 PDF 上：

- **有预设签名** → From Profile Tab 显示全部签名列表，点击选用
- **无预设签名** → 提示用户前往 Profile 设置，或切换到 Draw Tab 现场手写

---

## 3. MVP 功能范围

| 功能                                  | 阶段                    |
| ------------------------------------- | ----------------------- |
| 签名管理（手写 / 上传）               | MVP ✅                  |
| 多签名管理（保存多个签名，上限 7 个） | MVP ✅                  |
| 头像上传                              | P2（暂不做）            |
| 修改姓名                              | P2 / 已有 IDM（暂不做） |
| 修改密码                              | P2 / 已有 IDM（暂不做） |

---

## 4. 验收标准

### 4.1 页面入口

| #   | 需求描述 | 验收标准                                                                                                  |
| --- | -------- | --------------------------------------------------------------------------------------------------------- |
| 1   | 入口位置 | 顶部导航栏右上角用户头像下拉菜单（PROFILE 弹窗）中，在用户名/邮箱下方、Log Out 上方增加「My Profile」入口 |
| 2   | 页面 URL | `/profile`                                                                                                |
| 3   | 侧边栏   | 不出现在侧边栏菜单（不分配权限 code，仅通过用户下拉菜单访问）                                             |

### 4.2 签名管理（支持多签名）

| #   | 需求描述     | 验收标准                                                                    |
| --- | ------------ | --------------------------------------------------------------------------- |
| 4   | 显示签名列表 | 已保存的所有签名以卡片列表形式展示预览图；未设置时显示空状态引导            |
| 5   | 新增签名入口 | 提供「Add Signature」按钮，可继续添加新签名（手写或上传）                   |
| 6   | 手写签名     | 点击手写 Tab 打开 Canvas 画布，支持鼠标 + 触屏                              |
| 7   | 清除重签     | 手写时可清除画布重新签名                                                    |
| 8   | 上传签名图片 | 支持上传 PNG/JPG，文件大小 ≤ 500KB                                          |
| 9   | 上传提示     | 提示文字：「推荐上传透明背景的 PNG，白色背景图片会在 PDF 上显示为白色方块」 |
| 10  | 上传预览     | 上传后在模拟 PDF 背景色的容器中展示签名实际效果，让用户确认                 |
| 11  | 签名预览     | 保存后在签名列表卡片中显示签名图片预览                                      |
| 12  | 保存签名     | 确认后将签名追加到用户签名列表                                              |
| 13  | 数量上限     | 最多保存 7 个签名；超出上限时提示「已达签名上限（7个），请删除后再添加」    |
| 14  | 删除签名     | 可删除列表中任意已保存的签名                                                |
| 15  | 使用说明     | 页面显示提示：「签署文档时可从这里选择签名」                                |

---

## 5. 后端 API 设计

| 方法   | 路径                                   | 说明                                |
| ------ | -------------------------------------- | ----------------------------------- |
| GET    | `/ow/profile/signatures`               | 获取当前用户的所有签名列表          |
| POST   | `/ow/profile/signatures`               | 新增一个签名（base64 PNG，≤ 500KB） |
| DELETE | `/ow/profile/signatures/{signatureId}` | 删除指定签名                        |

**GET 返回示例：**

```json
{
  "code": "200",
  "data": [
    {
      "id": "111111111111111",
      "imageBase64": "data:image/png;base64,...",
      "createdDate": "2026-08-11T10:00:00Z"
    },
    {
      "id": "222222222222222",
      "imageBase64": "data:image/png;base64,...",
      "createdDate": "2026-08-10T09:00:00Z"
    }
  ]
}
```

**POST 请求体：**

```json
{
  "imageBase64": "data:image/png;base64,..."
}
```

**POST 校验规则：**

- base64 解码后 ≤ 500KB，否则返回错误
- 当前用户签名数量 < 7，否则返回「已达签名上限」错误

---

## 6. 数据库设计

### 表：`ff_user_signature`

```sql
CREATE TABLE ff_user_signature (
    id          BIGINT PRIMARY KEY,           -- Snowflake ID
    user_id     VARCHAR(100) NOT NULL,        -- 用户 ID
    image_data  TEXT NOT NULL,               -- base64 PNG
    create_date TIMESTAMP NOT NULL,
    modify_date TIMESTAMP NOT NULL,
    create_by   VARCHAR(100),
    modify_by   VARCHAR(100),
    is_valid    BOOLEAN NOT NULL DEFAULT TRUE  -- 软删除
);

CREATE INDEX idx_ff_user_signature_user_id ON ff_user_signature(user_id);
```

> ⚠️ **重要**：此表**不含** `app_code` 和 `tenant_id` 字段，是项目中唯一不做租户隔离的业务表。签名跟人走，同一用户在不同租户下登录共享同一批签名。详见 ADR-0001。

---

## 7. 前端技术方案

### 入口改造（`userLayout.vue`）

文件位置：`src/app/components/navbarCompanents/userLayout.vue`

在用户名/邮箱标签下方、Log Out 按钮上方，新增：

```vue
<div class="flex items-center my-3 cursor-pointer" @click="goToProfile">
  <el-button text :icon="UserIcon">My Profile</el-button>
</div>
```

> 注意：`userLayout.vue` 里 `avatar` 字段目前永远是空字符串（`const avatar = ref('')`），头像图片分支是预留代码，MVP 不做头像上传，不需要处理。

### 签名画布组件

**推荐：`vue-signature-pad`**（底层为 `signature_pad`）

```bash
pnpm add vue-signature-pad
```

### Canvas 规格

- 尺寸：400 × 150 px
- 背景：透明（`rgba(0,0,0,0)`）
- 导出格式：PNG，base64

---

## 8. 工时拆解

| 模块                                                  | 工时           |
| ----------------------------------------------------- | -------------- |
| 前端 - Profile 页面入口（userLayout.vue 改造 + 路由） | 0.5 天         |
| 前端 - 签名管理组件（多签名列表 + 新增弹窗 + 删除）   | 2 天           |
| 后端 - Migration（新建 ff_user_signature 表）         | 0.25 天        |
| 后端 - 签名 API（列表 / 新增 / 删除）                 | 0.5 天         |
| 测试                                                  | 0.5 天         |
| **合计**                                              | **~3.75 人天** |

---

## 9. 后续增强（P2，本期不做）

| 功能                 | 优先级 |
| -------------------- | ------ |
| 头像上传             | P2     |
| 签名样式（字体签名） | P3     |

---

## 10. 原型参考

https://guidepost-three.vercel.app/

---

## 11. 相关 ADR

- [ADR-0001](./adr/0001-user-signature-no-tenant-isolation.md)：用户签名表不做租户隔离

---

## 12. 项目背景信息（开发依据）

### 项目技术栈

- 前端：Vue 3.5 + Vite 5 + TypeScript + Element Plus + Pinia + Tailwind CSS
- 后端：.NET 8 + ASP.NET Core + SqlSugar ORM
- 包管理：pnpm

### 现有代码关键信息

- 用户头像下拉菜单：`src/app/components/navbarCompanents/userLayout.vue`
- 路由注册位置：`src/app/router/routers/modules/`
- `/profile` 不分配权限 code，不出现在侧边栏，仅通过 userLayout.vue 入口访问
- 如需 store：`src/app/stores/modules/profile.ts`，ID 前缀 `item-wfe-app-`
- 项目已有 `cropperjs` 可用于图片裁剪（上传签名图片场景可复用）
- `vue-signature-pad` 需新增安装：`pnpm add vue-signature-pad`

### 后端代码规范

- Controller：`WebApi/Controllers/OW/ProfileController.cs`，路由前缀 `ow/`
- Service 接口：`Application.Contracts/IServices/OW/IProfileService.cs`
- Service 实现：`Application/Services/OW/ProfileService.cs`，实现 `IScopedService`
- Migration 文件：`SqlSugarDB/Migrations/Migration_{日期}_AddUserSignatureTable.cs`
- 必须在 `MigrationManager.cs` 的 `migrations` 数组末尾注册
- Migration 自动执行：应用启动时自动检查并执行，dev/staging/preview 均为自动部署自动触发

### API 格式约定

- 返回格式：`Success<T>(data)` 包装
- 错误：`throw new CRMException(ErrorCodeEnum, message)`
- **签名表不加多租户字段，查询不用 `.Filter(null, true)`**
