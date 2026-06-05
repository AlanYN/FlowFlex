# CONTEXT.md 格式

## 结构

```md
# {上下文名称}

{一两句话描述这个上下文是什么以及为什么存在。}

## 术语

**Order**：
{一两句话描述该术语}
_避免使用_：Purchase、transaction

**Invoice**：
交付后发送给客户的付款请求。
_避免使用_：Bill、payment request

**Customer**：
下订单的个人或组织。
_避免使用_：Client、buyer、account
```

## 规则

- **要有主见。** 当同一概念存在多个词时，选择最好的一个，将其他的列为应避免的别名。
- **明确标记冲突。** 如果一个术语被模糊使用，在"已标记的歧义"中指出并给出明确的解决方案。
- **保持定义简洁。** 最多一两句话。定义它*是*什么，而不是它做什么。
- **展示关系。** 使用粗体术语名称，在明显时表达基数。
- **只包含特定于本项目上下文的术语。** 通用编程概念（超时、错误类型、工具模式）不属于这里，即使项目大量使用它们。在添加术语之前问：这是这个上下文独有的概念，还是通用编程概念？只有前者属于这里。
- **在自然聚类出现时按子标题分组术语。** 如果所有术语属于单一内聚领域，平面列表就可以。
- **写一个示例对话。** 开发者和领域专家之间的对话，展示术语如何自然交互，并澄清相关概念之间的边界。

## 单上下文 vs 多上下文仓库

**单上下文（大多数仓库）：** 仓库根目录一个 `CONTEXT.md`。

**多上下文：** 仓库根目录的 `CONTEXT-MAP.md` 列出上下文、它们的位置以及它们之间的关系：

```md
# 上下文映射

## 上下文

- [Ordering](./src/ordering/CONTEXT.md) — 接收和跟踪客户订单
- [Billing](./src/billing/CONTEXT.md) — 生成发票和处理付款
- [Fulfillment](./src/fulfillment/CONTEXT.md) — 管理仓库拣货和发货

## 关系

- **Ordering → Fulfillment**：Ordering 发出 `OrderPlaced` 事件；Fulfillment 消费它们来开始拣货
- **Fulfillment → Billing**：Fulfillment 发出 `ShipmentDispatched` 事件；Billing 消费它们来生成发票
- **Ordering ↔ Billing**：共享 `CustomerId` 和 `Money` 类型
```

技能推断适用哪种结构：

- 如果 `CONTEXT-MAP.md` 存在，读取它来找到上下文
- 如果只有根目录的 `CONTEXT.md` 存在，单上下文
- 如果都不存在，在第一个术语被解决时延迟创建根目录的 `CONTEXT.md`

当存在多个上下文时，推断当前话题与哪个相关。如果不清楚，就问。
