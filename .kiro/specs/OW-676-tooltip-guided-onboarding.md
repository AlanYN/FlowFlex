# OW-676 Tooltip Guided Onboarding — 技术方案

**Jira:** OW-676  
**优先级:** P1  
**负责人:** Zhenyan Wang  
**状态:** 设计完成，待开发

---

## 一、背景与目标

WFE 是多团队协作的工作流引擎，每个 Case 由多个 Stage 组成，分配给不同 Assignee。新用户通过邮件通知首次进入 Case 详情页时，不清楚自己需要做什么、怎么操作、如何推进。

**目标：** 为 Case 执行端提供上下文感知的 Tooltip 引导，首次自动触发，按页面布局逐步引导用户完成当前 Stage 的操作。

**Phase 1 范围：** 仅 Case 执行端（`portal.vue`）

---

## 二、设计原则

1. **上下文相关** — 只在用户需要操作的位置出现，不做全局产品介绍
2. **角色感知** — 只对当前 Stage 的 Assignee 触发
3. **一次性 + 可重播** — 首次进入自动触发，后续通过右下角 `?` 按钮重播
4. **渐进式** — 按页面从上到下分步引导，动态跳过当前 Stage 不含的 Component
5. **非阻断** — 用户可随时跳过或关闭
6. **可复用** — 组件封装为通用 Tour 系统，供其他模块（配置端、列表页等）后续复用

---

## 三、技术选型

### Tour 库：Driver.js

| 选项          | 大小      | 结论            |
| ------------- | --------- | --------------- |
| **Driver.js** | ~5kb gzip | ✅ 选用         |
| Shepherd.js   | ~34kb     | ❌ 太重         |
| Intro.js      | ~10kb     | ❌ 商业授权限制 |
| 自实现        | 0kb       | ❌ 维护成本高   |

**理由：** 轻量、框架无关、支持高亮遮罩 + 箭头 Tooltip，通过 Vite dynamic import 实现 lazy load，不影响首屏渲染。

### 持久化：localStorage（Phase 1）

- Key 格式：`ff_tour_done_{userId}_{onboardingId}_{stageId}`
- 每个 Stage 独立记录，Stage 切换后会检查新 Stage 是否已引导过
- Phase 2 可迁移至后端（新建 `ff_tour_guide_state` 表）

---

## 四、架构设计（可复用）

```
src/app/
├── composables/
│   └── useTourGuide.ts              # 核心 composable（通用）
│       ├── buildSteps(config)       # 根据配置构建步骤列表
│       ├── initTour(steps)          # 初始化 Driver.js 实例（lazy load）
│       ├── startTour()              # 启动 Tour
│       ├── checkShouldShow()        # 检查是否需要触发（localStorage）
│       └── markCompleted()          # 标记已完成
│
├── components/global/
│   └── TourGuide/
│       └── index.vue                # 通用 Tour 容器组件
│           ├── Props: tourKey, steps, autoStart, showFab
│           ├── FAB "?" 按钮（固定右下角）
│           └── 暴露 startTour() 方法供外部调用
│
└── views/onboard/sub-portal/
    └── portal.vue                   # 消费端，加 data-tour 锚点 + 引入 TourGuide
```

### TourGuide 组件接口设计

```typescript
// Props
interface TourGuideProps {
  tourKey: string; // 唯一标识，用于持久化 key（如 "case-portal"）
  steps: TourStep[]; // 步骤列表（外部传入，支持动态构建）
  autoStart?: boolean; // 是否自动启动（默认 true）
  showFab?: boolean; // 是否显示右下角 "?" 按钮（默认 true）
  fabTooltip?: string; // FAB hover 提示文字
  persistKey?: string; // localStorage key 后缀（不传则只用 tourKey）
}

// TourStep 类型
interface TourStep {
  element: string; // CSS 选择器，如 '[data-tour="case-title"]'
  title?: string; // 步骤标题
  description: string; // 步骤说明文字（支持 HTML）
  side?: "top" | "bottom" | "left" | "right";
  align?: "start" | "center" | "end";
}

// Expose（供父组件调用）
interface TourGuideExpose {
  startTour(): void; // 手动触发 Tour
  resetTour(): void; // 清除持久化状态并重新触发
}
```

### useTourGuide composable 接口

```typescript
function useTourGuide(options: {
    persistKey: string;        // localStorage key 的唯一标识部分
}) {
    // 返回
    return {
        isCompleted: Ref<boolean>;
        startTour(steps: TourStep[]): Promise<void>;
        markCompleted(): void;
        resetCompleted(): void;
    }
}
```

---

## 五、DOM 锚点规范（`data-tour` 属性）

在 `portal.vue` 各区域添加以下属性，作为 Driver.js 选择器：

| data-tour 值            | 对应区域                        |
| ----------------------- | ------------------------------- |
| `case-title`            | PageHeader 标题区               |
| `progress-bar`          | 右侧 OnboardingProgress 组件    |
| `stage-fields`          | Fields（静态字段表单）容器      |
| `stage-quick-link`      | QuickLink 容器                  |
| `stage-checklist-first` | 第一个 Checklist 容器           |
| `stage-checklist-task`  | Checklist 任务列表区域          |
| `stage-checklist-notes` | Checklist Notes/Attachment 区域 |
| `stage-checklist-other` | 后续 Checklist 容器（第2个起）  |
| `stage-questionnaire`   | Questionnaire 容器              |
| `stage-files`           | Files（文档）容器               |
| `complete-btn`          | Complete 按钮                   |

---

## 六、Tour 步骤动态构建逻辑

`portal.vue` 中根据 `sortedComponents` 动态生成 `TourStep[]`：

```
固定步骤 1: [data-tour="case-title"]      → Case 标题和状态说明
固定步骤 2: [data-tour="progress-bar"]   → 进度条说明

动态步骤（按 sortedComponents 顺序遍历）:
  key === 'fields'       → 1 步（填写信息字段）
  key === 'quickLink'    → 1 步（跳转相关链接）
  key === 'checklist'    →
    第 1 个: 5 步（标题 + 任务勾选 + Notes/Attachment + 整体说明）
    后续:    1 步（继续完成其余清单）
  key === 'questionnaires' → 4 步（标题 + 填写字段 + 提交按钮 + 整体说明）
  key === 'files'        → 1 步（上传文件说明）

固定最后步骤: [data-tour="complete-btn"] → 推进按钮说明
```

**触发条件（双重检查）：**

1. 当前用户是当前 Stage 的 Assignee
2. localStorage 中该 key 不存在（未完成过）

---

## 七、触发时机

```typescript
// portal.vue 中
watch(stageDataLoading, async (newVal) => {
  if (newVal === false) {
    await nextTick();
    // 等待 DOM 渲染完成
    setTimeout(() => {
      tourGuideRef.value?.startTour(); // TourGuide 组件内部检查是否需要触发
    }, 300);
  }
});
```

---

## 八、文件改动清单

### 新增文件

| 文件                                            | 说明                                                 |
| ----------------------------------------------- | ---------------------------------------------------- |
| `src/app/composables/useTourGuide.ts`           | 通用 Tour composable（持久化 + Driver.js lazy load） |
| `src/app/components/global/TourGuide/index.vue` | 通用 TourGuide 组件（FAB + 初始化）                  |

### 修改文件

| 文件                                    | 改动说明                                                         |
| --------------------------------------- | ---------------------------------------------------------------- |
| `packages/flowFlex-common/package.json` | 添加 `driver.js` 依赖                                            |
| `portal.vue`                            | 加 `data-tour` 属性、引入 TourGuide 组件、构建步骤、监听触发时机 |

### 不需要改动

- 后端（Phase 1 纯前端实现）
- 路由、Pinia store
- 其他页面组件

---

## 九、风险与缓解

| 风险                        | 原因                                            | 缓解方案                                                     |
| --------------------------- | ----------------------------------------------- | ------------------------------------------------------------ |
| DOM 时序问题                | `sortedComponents` 渲染后 DOM 才存在            | `nextTick` + `setTimeout(300ms)` 确保渲染完成                |
| 滚动容器冲突                | 内容在 `el-scrollbar` 内，非 window 滚动        | Driver.js `smoothScroll: true` + 必要时手动 `scrollIntoView` |
| Wujie 微前端沙箱            | `window` 对象被代理，localStorage 可能受影响    | 测试 `window.localStorage` 可访问性，降级用 sessionStorage   |
| Stage 切换重复触发          | 切换 Stage 后 `stageDataLoading` 再次变为 false | persistKey 包含 `stageId`，每个 Stage 独立判断               |
| Driver.js 遮罩 z-index 冲突 | Element Plus dialog/drawer z-index 较高         | 配置 Driver.js `overlayOpacity` + z-index，避免被遮挡        |

---

## 十、后续迭代规划

### Phase 2 扩展点（基于本次封装）

由于 `TourGuide` 和 `useTourGuide` 设计为通用组件，后续模块只需：

1. 在目标页面定义 `data-tour` 锚点
2. 构建对应的 `TourStep[]`
3. 引入 `TourGuide` 组件并传入 `steps` + `persistKey`

**规划扩展场景：**

- 配置端：Workflow 编辑页 Stage 配置引导
- 列表页：Onboarding List 筛选和操作引导
- 邮件通知补充：配合 Phase 2 后端方案统计引导完成率
- Admin 自定义文案：后端存储各 Workflow 的自定义 Tour 文案

---

## 十一、验收标准对照

| #   | 需求                                | 实现方式                                        |
| --- | ----------------------------------- | ----------------------------------------------- |
| 1   | Assignee 首次进入自动触发           | watch stageDataLoading + Assignee 校验          |
| 2   | Step-by-step 引导，带高亮遮罩       | Driver.js 原生支持                              |
| 3   | 可随时跳过                          | Driver.js 原生 `allowClose: true`               |
| 4   | 同一 scene 只触发一次               | localStorage persistKey                         |
| 5   | 页面有 "?" 按钮可重播               | TourGuide FAB 组件                              |
| 6   | Tooltip 显示当前 Stage 名等动态内容 | 步骤构建时从 `currentStageTitle` 插值           |
| 7   | 移动端适配                          | Driver.js 自适应位置 + TourGuide FAB 响应式定位 |
| 8   | 多语言支持                          | 步骤文案通过 `useI18n().t()` 提供               |
| 9   | 不影响首屏渲染                      | `import('driver.js')` dynamic import            |
