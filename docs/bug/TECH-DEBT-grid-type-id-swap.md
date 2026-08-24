# 技术债：Grid Question Type ID 命名混乱

## 背景

来源：JIRA OW-682（P0 Bug，已通过临时 fix 解决展示问题）

## Bug 引入记录

| 字段 | 内容 |
|------|------|
| 引入 commit | `a4c93c48` |
| 作者 | 王振炎 \<zhenyan.wang@item.com\> |
| 时间 | 2025-07-07 |
| commit message | `feat: commit common` |

该 commit 首次定义了这两个类型，`checkbox_grid` 的 `description` 就已写作 `'Single choice in a grid'`，但 `name` 却叫 `'Checkbox grid'`，说明创建时对两者语义已经混淆。

后续 commit `c5348641`（同一作者，2025-08-12，`feat: add question type shor_grid`）只是更换了图标，未修正命名问题，Bug 延续至今。

当前代码中，`multiple_choice_grid` 和 `checkbox_grid` 这两个 id 的**语义与实际行为是反的**：

| id | 实际渲染控件 | 实际行为 | 正确语义 |
|----|-------------|---------|---------|
| `multiple_choice_grid` | `el-checkbox-group` | 每行可多选 | 应该是 Checkbox Grid |
| `checkbox_grid` | `el-radio` | 每行只能单选 | 应该是 Multiple Choice Grid |

参照 Google Forms 的标准定义：
- **Multiple Choice Grid** = 每行只能选一个 → radio button（单选）
- **Checkbox Grid** = 每行可以选多个 → checkbox（多选）

## 临时 Fix（已做）

在 `createQuestion.vue` 里把两个类型的 `name` 字段对调，让用户看到的名称与实际行为一致。
这只是打补丁，没有修正底层 id 命名混乱的问题，代码可读性仍然差。

## 正确做法：对调 id 字符串本身

将所有代码中的 `multiple_choice_grid` 和 `checkbox_grid` 对调，使 id 的字面含义与行为一致。

### ⚠️ 必须先处理数据库存量数据

数据库中 questionnaire 配置（JSONB 列）里存有 `question.type` 字段，值为这两个字符串。
**对调 id 前必须先写 Migration SQL 更新存量数据**，否则所有历史问卷的 grid 题目类型会指向错误行为。

```sql
-- 伪代码，需根据实际 JSONB 结构调整
-- 先把 multiple_choice_grid 改为临时值，再把 checkbox_grid 改为目标值，最后把临时值改为最终值
UPDATE ff_questionnaire
SET config = jsonb_replace_all(config, 'multiple_choice_grid', '__tmp__');

UPDATE ff_questionnaire
SET config = jsonb_replace_all(config, 'checkbox_grid', 'multiple_choice_grid');

UPDATE ff_questionnaire
SET config = jsonb_replace_all(config, '__tmp__', 'checkbox_grid');
```

### 需要修改的前端文件

以下文件有**行为差异**的分支逻辑，必须跟着 id 对调一起改：

#### 1. `src/app/views/onboard/questionnaire/createQuestion.vue`
- 行 ~500–508：枚举定义数组，id 和 name 重新对齐（撤销临时 fix，直接改 id）

#### 2. `src/app/views/onboard/questionnaire/components/PreviewContent.vue`
- 行 ~409–972：v-else-if 渲染分支（`multiple_choice_grid` → checkbox 表格，`checkbox_grid` → radio 表格）
- 行 ~1564–1595：switch-case 初始化（`multiple_choice_grid` → `[]`，`checkbox_grid` → `null`）
- 行 ~1985–2007：必填校验分支（数组检查 vs 字符串检查）

#### 3. `src/app/views/onboard/onboardingList/components/dynamicForm.vue`
- 行 ~208–256、~761–836：v-else-if 渲染分支
- 行 ~1352–1361：答案反序列化（多选 → 数组，单选 → 字符串）
- 行 ~1540–1546、~2507–2517：表单数据初始化
- 行 ~1984–2022：必填校验分支
- 行 ~2193–2197：构造提交答案数组

#### 4. `src/app/views/onboard/sub-portal/portal.vue`
- 行 ~815–852：switch-case 必填校验（数组检查 vs 字符串检查）

#### 5. `src/app/views/onboard/onboardingList/components/QuestionnaireDetails.vue`
- 行 ~253、~632–640：校验分支

#### 6. `src/app/views/onboard/overview/customer-overview.vue`
- 行 ~2682–2688：`isCheckboxGridType` / `isMultipleChoiceGridType` 两个辅助函数的函数名与逻辑需同步对调（或重命名避免混乱）

#### 7. `src/app/views/onboard/questionnaire/components/GridEditor.vue`
- 行 ~180：`checkbox_grid` 专属的 "require one response per row" 配置项，需跟随 id 调整

### 需要修改的后端文件

以下后端文件两种 grid 类型合并处理，只需更新字符串字面量：

- `Application/Services/OW/ChangeLog/QuestionnaireAnswerParser.cs`（行 ~294）
- `Application/Services/OW/ComponentDataService.cs`（行 ~230）
- `Application/Services/OW/ChangeLog/BaseOperationLogService.cs`（行 ~4026）

### 只需更新字符串常量的其他前端文件

两种类型统一处理，无行为差异，改字符串即可：

- `src/app/utils/ruleUtils.ts`（行 ~91）— gridTypes 数组
- `src/app/enums/conditionEnum.ts`（行 ~43、~179）— 映射对象 key
- `src/app/views/onboard/questionnaire/components/QuestionEditor.vue`（行 ~483）— needsGrid 数组
- `src/app/views/onboard/workflow/components/condition/ConditionRuleForm.vue`（行 ~863）— grid 类型判断数组
- `src/app/apis/ow/change-log.ts`（行 ~240）— 两种类型合并 case

## 执行顺序建议

1. **查库确认**存量 questionnaire 数据中 `multiple_choice_grid` / `checkbox_grid` 的数量分布
2. **写 Migration SQL** 更新存量数据（需在发布前上线，或与代码同步部署）
3. **改前端**：按上方清单逐文件修改，建议用全局替换辅助，人工核查每处分支逻辑
4. **改后端**：更新三处字符串字面量
5. **撤销临时 fix**：`createQuestion.vue` 的 name 字段恢复与 id 一致（id 已对调后名字自然正确）
6. **全量回归测试**：重点测试 questionnaire 创建、填写、提交、历史记录查看四个场景
