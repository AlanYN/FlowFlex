# Requirements Document

## Introduction

本文档定义了 FlowFlex 系统仪表盘（Dashboard）功能的需求规范。仪表盘为用户提供系统核心业务数据的可视化概览，包括案例统计、待办任务、消息中心、近期成就和即将到期的截止日期等模块。

## Glossary

- **Dashboard**: 仪表盘，系统首页的数据概览界面
- **Case**: 案例/Onboarding，系统中的核心业务实体
- **Stage**: 阶段，案例流程中的各个步骤
- **ChecklistTask**: 待办任务，分配给用户的具体工作项
- **Achievement**: 成就/里程碑，案例完成的重要节点
- **Message**: 消息，包括内部消息、客户邮件和 Portal 消息
- **Deadline**: 截止日期，任务或里程碑的到期时间
- **Team**: 团队，用户所属的工作组

## Requirements

### Requirement 1: 案例统计概览

**User Story:** As a user, I want to see an overview of case statistics with month-over-month comparison, so that I can quickly understand the current workload, progress, and trends.

#### Acceptance Criteria

1. THE Dashboard_API SHALL return the total count of active cases for the current user
2. THE Dashboard_API SHALL return the count of cases completed this month
3. THE Dashboard_API SHALL return the count of overdue tasks
4. THE Dashboard_API SHALL return the average completion time in days
5. WHEN returning active cases count, THE Dashboard_API SHALL include the difference compared to last month (e.g., +2 or -3)
6. WHEN returning completed this month count, THE Dashboard_API SHALL include the difference compared to last month (e.g., +3)
7. WHEN returning overdue tasks count, THE Dashboard_API SHALL include the difference compared to last month (e.g., -2)
8. WHEN returning average completion time, THE Dashboard_API SHALL include the difference in days compared to last month (e.g., -5 days)
9. THE Dashboard_API SHALL indicate whether each trend is positive (improvement) or negative (decline) with a direction indicator
10. THE Dashboard_API SHALL support filtering statistics by team when the user has team-level access

### Requirement 2: 案例阶段分布

**User Story:** As a user, I want to see the distribution of cases by stage, so that I can understand where cases are in the workflow.

#### Acceptance Criteria

1. THE Dashboard_API SHALL return a list of stages with their respective case counts
2. WHEN returning stage distribution, THE Dashboard_API SHALL order stages by their sequence in the workflow
3. THE Dashboard_API SHALL include the stage name and case count for each stage
4. THE Dashboard_API SHALL calculate the overall progress percentage based on completed stages

### Requirement 3: 待办任务列表

**User Story:** As a user, I want to see my pending tasks, so that I can prioritize my work effectively.

#### Acceptance Criteria

1. THE Dashboard_API SHALL return a paginated list of pending tasks assigned to the current user
2. WHEN returning tasks, THE Dashboard_API SHALL include task title, priority, due date, and associated case information
3. THE Dashboard_API SHALL support filtering tasks by category (All, Sales, Account, Other)
4. THE Dashboard_API SHALL order tasks by priority (High > Medium > Low) and then by due date
5. WHEN a task is overdue, THE Dashboard_API SHALL mark it with an overdue indicator
6. THE Dashboard_API SHALL return the total count of tasks for pagination

### Requirement 4: 消息中心摘要

**User Story:** As a user, I want to see recent messages, so that I can stay informed about communications.

#### Acceptance Criteria

1. THE Dashboard_API SHALL return a list of recent messages (limited to configurable count, default 5)
2. WHEN returning messages, THE Dashboard_API SHALL include sender name, subject preview, timestamp, and message type labels
3. THE Dashboard_API SHALL indicate unread messages with a visual marker
4. THE Dashboard_API SHALL return the total count of unread messages
5. THE Dashboard_API SHALL support filtering messages by type (Internal, Customer, Portal)

### Requirement 5: 近期成就

**User Story:** As a user, I want to see recent achievements and milestones, so that I can track team accomplishments.

#### Acceptance Criteria

1. THE Dashboard_API SHALL return a list of recent achievements (limited to configurable count, default 5)
2. WHEN returning achievements, THE Dashboard_API SHALL include achievement title, description, completion date, and associated team
3. THE Dashboard_API SHALL order achievements by completion date in descending order
4. THE Dashboard_API SHALL support filtering achievements by team

### Requirement 6: 即将到期的截止日期

**User Story:** As a user, I want to see upcoming deadlines, so that I can plan my work accordingly.

#### Acceptance Criteria

1. THE Dashboard_API SHALL return a list of upcoming deadlines within a configurable time range (default 7 days)
2. WHEN returning deadlines, THE Dashboard_API SHALL include task/milestone name, due date, and associated case
3. THE Dashboard_API SHALL order deadlines by due date in ascending order
4. THE Dashboard_API SHALL categorize deadlines by urgency (overdue, due today, due tomorrow, due this week)
5. THE Dashboard_API SHALL support filtering deadlines by team or assignee

### Requirement 7: 数据权限和租户隔离

**User Story:** As a system administrator, I want dashboard data to respect user permissions and tenant isolation, so that data security is maintained.

#### Acceptance Criteria

1. THE Dashboard_API SHALL only return data that the current user has permission to view
2. THE Dashboard_API SHALL apply tenant isolation to all queries
3. WHEN a user has team-level access, THE Dashboard_API SHALL aggregate data across their accessible teams
4. IF a user lacks permission to view certain data, THEN THE Dashboard_API SHALL exclude that data from the response

### Requirement 8: API 性能和缓存

**User Story:** As a user, I want the dashboard to load quickly, so that I can access information efficiently.

#### Acceptance Criteria

1. THE Dashboard_API SHALL respond within 500ms for typical requests
2. THE Dashboard_API SHALL support caching of aggregated statistics with configurable TTL
3. WHEN cache is stale, THE Dashboard_API SHALL refresh data in the background
4. THE Dashboard_API SHALL support partial data loading for progressive UI rendering
