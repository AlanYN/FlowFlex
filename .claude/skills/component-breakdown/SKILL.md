---
inclusion: manual
---

# Skill: component-breakdown — 组件拆分

## 职责

基于交互设计的线框图和组件清单，将 UI 拆分为可复用的原子组件树，明确每个组件的职责、Props、状态和依赖关系。

---

## 输入

- `specs/{project-name}_{YYYY-MM-DD}/interaction-design/design.md`（线框图 + 组件清单）
- `docs/{project-name}_{YYYY-MM-DD}/interaction-design/context.md`（交互约定）

---

## 执行步骤

### 步骤 1：识别组件层级

从线框图中提取所有 UI 元素，按 Atomic Design 分层：
- **Atoms**：Button、Input、Icon、Badge 等不可再拆分的基础元素
- **Molecules**：由 Atoms 组合的功能单元（如 SearchBar = Input + Button）
- **Organisms**：由 Molecules 组合的页面区块（如 Header、ProductCard）
- **Templates**：页面布局骨架
- **Pages**：完整页面，对应 US

### 步骤 2：定义组件接口

每个组件必须明确：
- Props 类型定义（TypeScript interface）
- 内部状态（useState 列表）
- 触发的事件（onXxx 回调）
- 依赖的子组件

### 步骤 3：识别可复用组件

标记跨页面复用的组件，避免重复实现。

### 步骤 4：生成组件树

用缩进树形结构表示组件嵌套关系。

---

## 输出格式

写入 `specs/{project-name}_{YYYY-MM-DD}/technical-design/component-tree.md`：

```markdown
# Component Tree

## 组件层级总览

### Atoms
| 组件名 | 文件路径 | Props | 状态 | 复用次数 |
|--------|---------|-------|------|---------|
| Button | `src/components/atoms/Button.tsx` | variant, size, disabled, onClick | - | {N} |

### Molecules
| 组件名 | 文件路径 | 包含 Atoms | Props | 状态 |
|--------|---------|-----------|-------|------|

### Organisms
| 组件名 | 文件路径 | 包含 Molecules | 关联 US |
|--------|---------|--------------|---------|

### Pages
| 页面名 | 文件路径 | 关联 US | 路由 |
|--------|---------|---------|------|

## 组件树（嵌套结构）

### {页面名}（US-{NNN}）
```
Page
├── Header (Organism)
│   ├── Logo (Atom)
│   └── NavBar (Molecule)
│       └── NavItem (Atom) × N
└── MainContent (Organism)
    └── {ComponentName} (Molecule)
```

## Props 接口定义

### {ComponentName}
```typescript
interface {ComponentName}Props {
  // props 定义
}
```

## 可复用组件清单
| 组件名 | 复用场景 | 注意事项 |
|--------|---------|---------|
```

---

## 澄清检查点（COMPAMB）

以下情况标记为 COMPAMB，需用户确认：
- 组件职责边界不清晰
- 状态应放在父组件还是子组件
- 是否需要全局状态管理（Redux/Zustand）
- 组件是否需要懒加载
