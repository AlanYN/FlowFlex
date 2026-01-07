# Implementation Plan: Dashboard API

## Overview

本实现计划将仪表盘 API 功能分解为可执行的编码任务。实现采用 C# / .NET 8.0，遵循现有 FlowFlex 后端架构。

## Tasks

- [x] 1. 创建 Dashboard DTOs 和数据模型
  - [x] 1.1 创建请求 DTOs (DashboardQueryDto, TaskQueryDto)
    - 在 `Application.Contracts/Dtos/OW/Dashboard/` 目录下创建
    - 包含模块选择、分页、筛选参数
    - _Requirements: 1.10, 3.3, 3.6_
  - [x] 1.2 创建响应 DTOs (DashboardDto, DashboardStatisticsDto, StatisticItemDto)
    - 包含统计值、差值、趋势方向
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9_
  - [x] 1.3 创建 StageDistributionDto
    - 包含阶段 ID、名称、案例数、排序
    - _Requirements: 2.1, 2.2, 2.3, 2.4_
  - [x] 1.4 创建 DashboardTaskDto
    - 包含任务信息、优先级、截止日期、逾期标记
    - _Requirements: 3.1, 3.2, 3.4, 3.5_
  - [x] 1.5 创建 MessageSummaryDto 和 DashboardMessageDto
    - 包含消息列表和未读数量
    - _Requirements: 4.1, 4.2, 4.3, 4.4_
  - [x] 1.6 创建 AchievementDto
    - 包含成就标题、描述、完成日期、团队
    - _Requirements: 5.1, 5.2_
  - [x] 1.7 创建 DeadlineDto
    - 包含截止日期、紧急程度分类
    - _Requirements: 6.1, 6.2, 6.4_

- [x] 2. 创建 Dashboard Service 接口和实现
  - [x] 2.1 创建 IDashboardService 接口
    - 在 `Application.Contracts/IServices/OW/` 目录下创建
    - 定义所有仪表盘方法签名
    - _Requirements: 1.1-1.10, 2.1-2.4, 3.1-3.6, 4.1-4.5, 5.1-5.4, 6.1-6.5_
  - [x] 2.2 创建 DashboardService 类框架
    - 在 `Application/Services/OW/` 目录下创建
    - 注入所需的 Repository 依赖
    - _Requirements: 7.1, 7.2_
  - [x] 2.3 实现 GetStatisticsAsync 方法
    - 计算活跃案例数、本月完成数、逾期任务数、平均完成时间
    - 计算与上月的差值和趋势方向
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9_
  - [x] 2.4 编写 GetStatisticsAsync 单元测试
    - **Property 1: Statistics Month-over-Month Comparison Accuracy**
    - **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9**
  - [x] 2.5 实现 GetStageDistributionAsync 方法
    - 按阶段统计案例数量
    - 按阶段顺序排序
    - _Requirements: 2.1, 2.2, 2.3, 2.4_
  - [x] 2.6 编写 GetStageDistributionAsync 单元测试
    - **Property 2: Stage Distribution Ordering and Completeness**
    - **Validates: Requirements 2.1, 2.2, 2.3, 2.4**
  - [x] 2.7 实现 GetTasksAsync 方法
    - 分页查询待办任务
    - 支持分类筛选
    - 按优先级和截止日期排序
    - 标记逾期任务
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_
  - [x] 2.8 编写 GetTasksAsync 单元测试
    - **Property 3: Task List Filtering and Ordering**
    - **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6**

- [x] 3. Checkpoint - 确保统计和任务功能测试通过
  - 所有测试通过 (14/14)

- [x] 4. 实现消息、成就和截止日期功能
  - [x] 4.1 实现 GetMessageSummaryAsync 方法
    - 调用现有 MessageService 获取消息
    - 计算未读数量
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_
  - [x] 4.2 编写 GetMessageSummaryAsync 单元测试
    - **Property 4: Message Summary Completeness**
    - **Validates: Requirements 4.1, 4.2, 4.3, 4.4, 4.5**
  - [x] 4.3 实现 GetAchievementsAsync 方法
    - 查询最近完成的案例里程碑
    - 按完成日期降序排序
    - 支持团队筛选
    - _Requirements: 5.1, 5.2, 5.3, 5.4_
  - [x] 4.4 编写 GetAchievementsAsync 单元测试
    - **Property 5: Achievement List Ordering**
    - **Validates: Requirements 5.1, 5.2, 5.3, 5.4**
  - [x] 4.5 实现 GetDeadlinesAsync 方法
    - 查询指定天数内的截止日期
    - 按截止日期升序排序
    - 分类紧急程度
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_
  - [x] 4.6 编写 GetDeadlinesAsync 单元测试
    - **Property 6: Deadline Categorization and Ordering**
    - **Validates: Requirements 6.1, 6.2, 6.3, 6.4, 6.5**

- [x] 5. 实现聚合端点和权限控制
  - [x] 5.1 实现 GetDashboardAsync 聚合方法
    - 根据 Modules 参数选择性加载数据
    - 并行调用各模块方法
    - _Requirements: 8.4_
  - [x] 5.2 实现权限过滤逻辑
    - 应用租户隔离
    - 应用用户权限过滤
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  - [x] 5.3 编写权限过滤单元测试
    - **Property 7: Permission and Tenant Isolation**
    - **Validates: Requirements 7.1, 7.2, 7.3, 7.4**

- [x] 6. 创建 Dashboard Controller
  - [x] 6.1 创建 DashboardController 类
    - 在 `WebApi/Controllers/OW/` 目录下创建
    - 添加 WFEAuthorize 权限控制
    - _Requirements: 7.1_
  - [x] 6.2 实现聚合端点 GET /api/ow/dashboard/v1
    - 支持模块选择性加载
    - _Requirements: 8.4_
  - [x] 6.3 实现各模块独立端点
    - GET /api/ow/dashboard/v1/statistics
    - GET /api/ow/dashboard/v1/stage-distribution
    - GET /api/ow/dashboard/v1/tasks
    - GET /api/ow/dashboard/v1/messages
    - GET /api/ow/dashboard/v1/achievements
    - GET /api/ow/dashboard/v1/deadlines
    - _Requirements: 1.1-6.5_

- [x] 7. Checkpoint - 确保所有 API 端点测试通过
  - 所有单元测试通过 (14/14)
  - 所有 API 端点测试通过 (7/7 基础端点 + 7/7 带参数测试)
  - 测试环境: http://localhost:5173/api
  - 测试时间: 2025-12-22

- [ ] 8. 添加缓存支持
  - [ ] 8.1 为统计数据添加 Redis 缓存
    - 配置缓存 TTL
    - 实现缓存失效策略
    - _Requirements: 8.2, 8.3_
  - [ ] 8.2 编写缓存行为单元测试
    - 测试缓存命中
    - 测试缓存失效
    - _Requirements: 8.2_

- [ ] 9. 添加 AutoMapper 配置
  - [ ] 9.1 创建 DashboardProfile 映射配置
    - 配置实体到 DTO 的映射
    - _Requirements: 1.1-6.5_

- [x] 10. Final Checkpoint - 确保所有测试通过
  - 所有单元测试通过 (14/14)
  - [x] 10.1 修复 Statistics API 返回 0 的 bug
    - 问题: team 过滤条件 `(team == null || o.CurrentTeam == team)` 被 ORM 错误翻译
    - 解决: 使用 `var filterByTeam = !string.IsNullOrEmpty(team)` 预先计算布尔值
    - 修复方法: GetActiveCasesCountAsync, GetActiveCasesCountAtDateAsync, GetCompletedCasesCountAsync, GetOverdueTasksCountAsync, GetOverdueTasksCountAtDateAsync, GetAverageCompletionTimeAsync
    - 修复时间: 2025-12-23

- [x] 11. 编写 API 接口文档
  - [x] 11.1 创建 Dashboard API 接口文档
    - 在 `Docs/` 目录下创建 `Dashboard-API.md`
    - 包含所有端点的详细说明
    - 包含请求/响应示例
    - 更新时间: 2025-12-23
    - _Requirements: 1.1-8.4_
  - [x] 11.2 创建 HTTP 测试文件
    - 在 `scripts/` 目录下创建 `test-dashboard-api.http`
    - 包含所有端点的测试请求 (17 个测试用例)
    - 更新时间: 2025-12-23
    - _Requirements: 1.1-8.4_

## Notes

- 所有测试任务都是必需的，确保完整的测试覆盖
- 每个任务都引用了具体的需求条款以确保可追溯性
- Checkpoint 任务用于增量验证
- 单元测试验证具体示例和边界情况
- 接口文档任务确保 API 有完整的文档说明
