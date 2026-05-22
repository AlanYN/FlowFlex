# Raw Requirements: LSO Parcel CRM & IAM Integration Actions

## 原始需求 (2026-04-09)

在 WFE 的 LSO Parcel 公司下创建 Action，当 Case 选中 "LSO Customer Onboarding" Workflow 时，点击 Case Complete 需要触发以下 Action：

### Action 1: CRM 创建 Customer
- 在 CRM 创建一个 Customer 并自动 Approved
- Case 字段和 CRM 字段对应关系：
  - Company Name → Customer Name
  - Tax ID → Tax ID
  - Company Street/Unit/City/State/Postal Code → Corporation Address + Primary Address
  - Contact First Name + Last Name → Contact Name
  - Contact Email → Contact Email
  - Contact Phone → Contact Phone
  - Sales Name/Email → Account Holders > Sales
  - Bill To Name/Street/Unit/City/State/Postal Code → Billto 类型地址
  - Payment Terms → Financial > Net Term

### Action 2: IAM 创建 User
- 在 IAM 的 LSO 租户下创建一个 User
- User 类型为 External
- Tenant = 当前 WFE Case 所在的 Company
- User 需要关联上 CRM 创建的相应 Customer Account
- User Tags = Admin
- IAM Username = WFE Field 的 Username
- IAM First Name, Last Name = WFE Field 的 First Name 和 Last Name
- IAM Password = WFE 的 Password

### 补充说明
- 需要新建一个 workflow，在 stage 调用 action
- 需要新建一个 case
- 需要添加两个 action，关联到 integration/1994239810054787072

### Lina Gao 评论 (03/24/26)
IAM 创建 External 类型 User 时，页面上可以配置 Customer 关联。调用接口时需要传 Customer 关联参数，但需确认 IAM 接口是否支持，不支持则需 IAM 团队添加。
