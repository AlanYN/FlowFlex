# Flowchart: Action Field Lookup — 用户操作流程图

## 1. Lookup 配置操作流程

```mermaid
flowchart TD
    A[打开 Action 编辑弹窗] --> B[展开 Field Mapping 区域]
    B --> C[查看字段映射表格]
    C --> D{需要配置 Lookup？}
    D -->|否| E[直接保存 Action]
    D -->|是| F[打开目标行的 Lookup 开关]
    F --> G[配置面板展开]
    G --> H[填写 Options Source]
    H --> I[填写 Display Field]
    I --> J[填写 Value Field]
    J --> K{需要 Response Path？}
    K -->|是| L[填写 Response Path]
    K -->|否| M{需要 Custom Headers？}
    L --> M
    M -->|是| N[展开 Custom Headers]
    N --> O[添加 Key-Value 对]
    O --> P{需要更多 Headers？}
    P -->|是| O
    P -->|否| Q{验证配置？}
    M -->|否| Q
    Q -->|是| R[点击 Test 按钮]
    R --> S{预览成功？}
    S -->|是| T[查看选项列表表格]
    S -->|否| U[查看错误信息]
    U --> V[修正配置]
    V --> R
    T --> W[保存 Action]
    Q -->|否| W
    W --> X{验证通过？}
    X -->|是| Y[保存成功，弹窗关闭]
    X -->|否| Z[高亮必填字段]
    Z --> H
```

## 2. 交互状态流转

```mermaid
stateDiagram-v2
    [*] --> SwitchOff: 初始状态

    SwitchOff --> PanelExpanded: 打开 Lookup 开关
    PanelExpanded --> Editing: 用户开始填写
    Editing --> TestReady: 必填字段已填完
    TestReady --> Testing: 点击 Test
    Testing --> PreviewSuccess: API 成功
    Testing --> PreviewError: API 失败
    PreviewSuccess --> TestReady: 可再次 Test
    PreviewError --> Editing: 修正配置

    PanelExpanded --> SwitchOff: 关闭开关（数据保留）
    Editing --> SwitchOff: 关闭开关（数据保留）
    TestReady --> SwitchOff: 关闭开关（数据保留）

    TestReady --> Saving: 点击保存
    Saving --> Saved: 验证通过
    Saving --> ValidationError: 必填未填
    ValidationError --> Editing: 补充字段
```

## 3. Custom Headers 折叠交互

```mermaid
flowchart TD
    A[Lookup 面板展开] --> B[Custom Headers 区域默认收起]
    B --> C{用户点击 ▸ Custom Headers？}
    C -->|是| D[展开 Headers 编辑区域]
    D --> E[显示已有 Header 行]
    E --> F{操作？}
    F -->|添加| G[点击 + Add Header]
    G --> H[新增空行 Key | Value | ✕]
    H --> F
    F -->|删除| I[点击 ✕ 按钮]
    I --> J[移除该行]
    J --> F
    F -->|编辑| K[修改 Key 或 Value]
    K --> F
    F -->|收起| L[点击 ▾ Custom Headers]
    L --> B
    C -->|否| M[保持收起状态]
```
