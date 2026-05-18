# File Structure: Plugin Price List

## 后端新增文件

```
packages/flowFlex-backend/
├── Domain/
│   ├── Entities/OW/
│   │   └── PluginPriceList.cs                    ← 实体定义
│   └── Repository/OW/
│       └── IPluginPriceListRepository.cs         ← 仓储接口
├── Application.Contracts/
│   ├── Dtos/OW/PluginPriceList/
│   │   ├── PluginPriceListInputDto.cs            ← 保存请求 DTO
│   │   └── PluginPriceListOutputDto.cs           ← 响应 DTO
│   └── IServices/OW/
│       └── IPluginPriceListService.cs            ← 服务接口
├── Application/
│   ├── Services/OW/
│   │   └── PluginPriceListService.cs             ← 服务实现（含权限检查）
│   └── Maps/
│       └── PluginPriceListMapProfile.cs          ← AutoMapper 映射
├── SqlSugarDB/
│   ├── Implements/OW/
│   │   └── PluginPriceListRepository.cs          ← 仓储实现
│   └── Migrations/
│       └── Migration_20260514000001_CreatePluginPriceListTable.cs  ← 建表迁移
└── WebApi/
    └── Controllers/OW/
        └── PluginPriceListController.cs          ← API 控制器

packages/flowFlex-common/
└── public/plugins/price-list/                    ← 前端静态文件（由 Nginx serve）
    ├── index.html
    └── assets/
        ├── index-{hash}.js
        └── index-{hash}.css
```

## 前端改动文件

```
amanda自己开发的前端项目/price-list-page/
├── vite.config.js                                ← 修改 base path
└── src/
    └── RateSheetBuilder.vue                      ← API 对接 + 只读模式
```

## 需要修改的现有文件

```
packages/flowFlex-backend/
├── SqlSugarDB/Migrations/MigrationManager.cs     ← 注册新 Migration
└── Application/Maps/ (Program.cs 中注册)         ← 注册新 MapProfile
```

## 各文件职责

| 文件 | 职责 |
|------|------|
| `PluginPriceList.cs` | 定义数据库实体，继承 OwEntityBase |
| `IPluginPriceListRepository.cs` | 定义 GetByCaseCodeAsync 等数据访问方法 |
| `PluginPriceListRepository.cs` | SqlSugar 实现，含租户过滤 |
| `Migration_...CreatePluginPriceListTable.cs` | DDL：建表 + 唯一索引 |
| `PluginPriceListInputDto.cs` | POST 请求体验证 |
| `PluginPriceListOutputDto.cs` | GET 响应体（含 permission 字段） |
| `IPluginPriceListService.cs` | 定义 GetAsync/SaveAsync/SubmitAsync |
| `PluginPriceListService.cs` | 业务逻辑：权限检查 + CRUD + 状态流转 |
| `PluginPriceListMapProfile.cs` | Entity ↔ DTO 映射规则 |
| `PluginPriceListController.cs` | 3 个 API Action，路由 + 认证 + 响应 |
