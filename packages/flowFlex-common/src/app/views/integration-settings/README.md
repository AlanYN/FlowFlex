# Integration Settings 模块

## 📦 已完成的开发工作

### ✅ P0 (MVP) 功能已全部实现

本模块已按照需求文档完成了所有 P0 优先级的功能开发。

---

## 📁 文件结构

```
src/app/views/integration-settings/
├── index.vue                           # 主页面
├── types.ts                            # TypeScript 类型定义
├── README.md                           # 本文档
└── components/
    ├── integration-card.vue            # 集成卡片组件（折叠卡）
    ├── connection-auth.vue             # 连接认证组件
    ├── inbound-settings.vue            # 入站设置组件
    ├── outbound-settings.vue           # 出站设置组件
    └── actions-list.vue                # 动作列表组件

src/app/apis/integration/
└── index.ts                            # API 接口定义
```

---

## 🎯 已实现功能

### 1. 主页面 (index.vue)
- ✅ 添加新集成（支持多种系统类型）
- ✅ 集成列表展示
- ✅ 空状态处理
- ✅ 加载状态
- ✅ 错误处理

**支持的系统类型**:
- Salesforce
- HubSpot
- Zoho CRM
- Microsoft Dynamics
- Custom Integration

### 2. 集成卡片 (integration-card.vue)
- ✅ 可展开/折叠的卡片设计
- ✅ 显示系统图标、名称、状态
- ✅ 显示已配置实体数量
- ✅ Tab 导航（4个标签页）
- ✅ 保存配置功能
- ✅ 删除集成功能
- ✅ 取消编辑功能

### 3. 连接认证 (connection-auth.vue)
- ✅ 系统名称配置
- ✅ Endpoint URL 配置
- ✅ 多种认证方式支持：
  - API Key
  - Basic Auth
  - Bearer Token
  - OAuth 2.0 (占位符，待实现)
- ✅ 动态凭证字段
- ✅ 表单验证
- ✅ 测试连接功能

### 4. 入站设置 (inbound-settings.vue)
包含 3 个子标签页：

#### 4.1 Entity Mapping (实体映射)
- ✅ 表格展示
- ✅ CRM Entity (只读)
- ✅ WFE Entity (下拉选择)
- ✅ Workflows (多选)
- ✅ 添加/删除映射

#### 4.2 Field Mapping (字段映射)
- ✅ 表格展示
- ✅ CRM Field (只读)
- ✅ WFE Field (支持选择或创建新字段)
- ✅ Type (自动识别)
- ✅ Sync Direction (View Only / Editable)
- ✅ Workflows (多选)
- ✅ 添加/删除字段映射
- ✅ Editable 字段自动提示

#### 4.3 Attachment Sharing (附件共享)
- ✅ 表格展示
- ✅ Module 配置
- ✅ Workflows 多选
- ✅ 添加/删除附件共享配置

### 5. 出站设置 (outbound-settings.vue)
包含 3 个子标签页：

#### 5.1 Master Data to Share (主数据共享)
- ✅ 复选框列表
- ✅ 支持选择多种数据类型：
  - Cases
  - Customers
  - Leads
  - Contacts
  - Opportunities

#### 5.2 Fields to Share (字段共享)
- ✅ 左右双列布局
- ✅ Available Fields (可用字段)
  - Basic Fields 分组
  - Dynamic Fields 分组
  - 搜索功能
- ✅ Fields to Share (共享字段)
  - 拖拽排序 (使用 vuedraggable)
  - 点击 >> 添加
  - 移除字段
  - 显示字段数量
- ✅ 空状态提示

#### 5.3 Attachments to Share (附件共享)
- ✅ Workflows 多选
- ✅ 信息提示

### 6. 动作列表 (actions-list.vue)
- ✅ 只读表格展示
- ✅ Action ID
- ✅ Action Name (可点击跳转)
- ✅ Type
- ✅ Status (带颜色标签)
- ✅ Workflows
- ✅ 加载状态
- ✅ 空状态处理

---

## 🔌 API 接口

所有 API 接口已在 `@/apis/integration/index.ts` 中定义：

```typescript
// 基础 CRUD
getIntegrations()              // 获取集成列表
createIntegration(data)        // 创建新集成
getIntegration(id)             // 获取单个集成详情
updateIntegration(id, data)    // 更新集成配置
deleteIntegration(id)          // 删除集成

// 连接测试
testConnection(id)             // 测试连接

// 字段映射
getFieldMappings(id)           // 获取字段映射
createFieldMapping(id, data)   // 创建字段映射
updateFieldMapping(id, fieldId, data)  // 更新字段映射
deleteFieldMapping(id, fieldId)        // 删除字段映射

// 动作
getActions(id)                 // 获取动作列表
```

---

## 🎨 样式规范

### CSS 变量使用
所有颜色都使用 CSS 全局变量，支持主题切换：

```css
var(--color-primary)      /* 主色调 */
var(--bg-primary)         /* 主背景色 */
var(--bg-secondary)       /* 次级背景色 */
var(--bg-input)           /* 输入框背景 */
var(--text-primary)       /* 主文本色 */
var(--text-secondary)     /* 次要文本色 */
var(--border-color)       /* 边框色 */
var(--success-color)      /* 成功状态色 */
var(--error-color)        /* 错误状态色 */
var(--bg-hover)           /* 悬停背景色 */
```

### 暗黑模式支持
- ✅ 所有组件已适配暗黑模式
- ✅ Element Plus 组件主题适配
- ✅ 自定义组件颜色适配

---

## 📝 使用方法

### 1. 路由配置

在路由文件中添加：

```typescript
{
  path: '/integration-settings',
  name: 'IntegrationSettings',
  component: () => import('@/app/views/integration-settings/index.vue'),
  meta: {
    title: 'Integration Settings',
    requiresAuth: true,
  },
}
```

### 2. 导航菜单

在侧边栏菜单中添加入口：

```vue
<el-menu-item index="/integration-settings">
  <el-icon><Connection /></el-icon>
  <span>Integration Settings</span>
</el-menu-item>
```

### 3. 依赖安装

确保已安装以下依赖：

```bash
npm install vuedraggable@next
# 或
pnpm add vuedraggable@next
```

---

## 🔧 配置说明

### 1. API 端点配置

在 `src/app/apis/integration/index.ts` 中修改 API 前缀：

```typescript
const API_PREFIX = '/api/integrations';  // 根据实际后端 API 调整
```

### 2. 工作流选项

目前使用模拟数据，实际使用时需要从 API 获取：

```typescript
// 在各组件中替换为实际 API 调用
const workflowOptions = ref<IWorkflowOption[]>([]);

async function loadWorkflows() {
  // 调用实际 API
  workflowOptions.value = await getWorkflows();
}
```

### 3. WFE 实体选项

同样需要从 API 获取：

```typescript
const wfeEntityOptions = ref<IWfeEntityOption[]>([]);

async function loadWfeEntities() {
  wfeEntityOptions.value = await getWfeEntities();
}
```

---

## 🚀 后续开发建议

### P1 优先级（完整版）
- [ ] OAuth 2.0 认证实现
- [ ] 实时数据同步监控
- [ ] 更多系统类型支持
- [ ] 批量操作功能

### P2 优先级（扩展功能）
- [ ] 批量字段映射
- [ ] 导入导出配置
- [ ] 高级数据规则
- [ ] 告警系统
- [ ] 同步日志查看

---

## 🐛 已知问题

1. **模拟数据**: 目前使用模拟数据，需要连接实际后端 API
2. **vuedraggable**: 需要确保安装了 `vuedraggable@next` 版本（Vue 3 兼容）
3. **路由跳转**: Actions 列表中的跳转需要配置实际的 Actions 详情页路由

---

## 📚 相关文档

- [需求文档](../../../Docs/IntegrationSettings_Frontend_Summary.md)
- [Element Plus 文档](https://element-plus.org/)
- [Vue 3 文档](https://vuejs.org/)
- [TypeScript 文档](https://www.typescriptlang.org/)

---

## 👥 开发团队

如有问题或建议，请联系前端开发团队。

**最后更新**: 2025-11-18

