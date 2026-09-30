# 业务流程图 — Univer 集成

> 阶段：requirements-analysis  
> 最后更新：2026-09-22

---

## 一、文档创建与编辑主流程

```mermaid
flowchart TD
    A([用户进入文档管理]) --> B[/documents/list 列表页]
    B --> C{有目标文档？}
    C -->|是| D[点击文档卡片]
    C -->|否| E[点击「+ 新建文档」]
    E --> F[弹出 CreateDocumentDialog]
    F --> G[填写标题 + 选择类型]
    G --> H{提交}
    H -->|名称为空| I[提示错误，不关闭弹窗]
    I --> G
    H -->|信息完整| J[POST /ow/document-snapshots/v1 创建空白快照]
    J --> K[跳转 /documents/:unitId/edit]
    D --> K
    K --> L{根据 docType 渲染编辑器}
    L -->|Sheet| M[UniverSheet.vue]
    L -->|Doc| N[UniverDoc.vue]
    L -->|Slide| O[UniverSlide.vue]
    M & N & O --> P[GET /ow/document-snapshots/v1/:unitId 加载数据]
    P --> Q[编辑器就绪，用户开始编辑]
```

---

## 二、保存流程

```mermaid
flowchart TD
    A([用户触发保存]) -->|点击按钮 或 Ctrl+S| B[getWorkbookData / getDocumentData]
    B --> C[POST /ow/document-snapshots/v1/:unitId/save Upsert]
    C -->|成功| D[保存成功提示]
    C -->|失败| E[保存失败提示 + 重试建议]
    A2([用户修改标题]) -->|行内编辑失焦| F[PATCH title 字段]
    A3([用户离开页面]) --> G{有未保存变更？}
    G -->|是| H[弹出离开确认弹窗]
    G -->|否| I[直接离开]
    H -->|保存后离开| B
    H -->|不保存离开| I
```

---

## 三、操作日志记录流程

```mermaid
flowchart TD
    A([用户在编辑器执行操作]) --> B[Univer onCommandExecuted 触发]
    B --> C{CommandType === MUTATION?}
    C -->|否| D[忽略，不上报]
    C -->|是| E{fromCollab === true?}
    E -->|是| D
    E -->|否| F[异步 POST /ow/document-operation-logs/v1]
    F --> G[后端写入 ff_document_operation_log]
```

---

## 四、多人协同编辑流程

```mermaid
sequenceDiagram
    participant A as 用户A（浏览器）
    participant S as SignalR Hub
    participant B as 用户B（浏览器）

    A->>S: JOIN { unitId, userId }
    B->>S: JOIN { unitId, userId }
    S->>B: join { memberID: A, name: A.name }
    S->>A: join { memberID: B, name: B.name }

    A->>A: 编辑单元格（MUTATION）
    A->>S: INGEST { cs: { baseRev: 5, mutations: [...] } }
    S->>S: 乐观锁校验 baseRev == currentRev?

    alt 校验通过
        S->>S: revision = 6，持久化 changeset
        S-->>A: cs_ack { revision: 6 }
        S-->>B: new_cs { mutations: [...], revision: 6 }
        B->>B: executeCommand(fromCollab: true)，渲染变更
    else 校验失败（并发冲突）
        S-->>A: cs_rej
        A->>A: 自动重试（更新 baseRev 后重发）
    end

    A->>S: DISCONNECT
    S->>B: leave { memberID: A }
```

---

## 五、列表页操作流程

```mermaid
flowchart TD
    A([进入 /documents/list]) --> B[GET 分页查询，page=1]
    B --> C[渲染文档卡片列表]
    C --> D{用户操作}
    D -->|输入搜索词| E[debounce 后重新查询，page=1]
    D -->|切换类型筛选| E
    D -->|切换排序| E
    D -->|翻页| F[查询对应页]
    D -->|点击卡片| G[跳转编辑器]
    D -->|点击重命名| H[行内编辑标题，失焦保存]
    D -->|点击删除| I[二次确认弹窗]
    I -->|确认| J[DELETE 软删除，刷新列表]
    I -->|取消| C
    E & F --> C
```
