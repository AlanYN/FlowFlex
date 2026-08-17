# ADR-0001：用户签名表不做租户隔离

**日期**: 2026-08-11  
**状态**: 已采纳  
**相关需求**: OW-703 User Profile with Signature Management

---

## 背景

项目为多租户架构，所有标准业务 entity 继承多租户基类，SqlSugar 全局过滤器自动按 `app_code` + `tenant_id` 隔离数据。

用户签名（`ff_user_signature`）是用户的个人数据，需要跟着人走——同一个用户在不同租户下登录，应该能看到同一批签名。

## 决定

`ff_user_signature` 表的 entity **不继承多租户基类**，不添加 `app_code` 和 `tenant_id` 字段，只按 `user_id` 隔离。

## 理由

- 签名是个人资产，不属于任何特定租户的业务数据
- 不继承多租户基类，SqlSugar 全局过滤器不会自动生效，无需在查询时用 `.Filter(null, true)` 绕过，逻辑更干净
- 避免以后维护者误以为租户隔离是遗漏而「修复」

## 替代方案

继承多租户基类，查询时用 `.Filter(null, true)` 绕过全局过滤器。被否决，因为依赖开发者记得每次都加 `.Filter(null, true)`，容易遗漏导致签名跨租户丢失，且无法从 entity 定义上看出意图。

## 后果

- `ff_user_signature` 是项目中**唯一不带多租户字段的业务表**，是刻意设计，非疏漏
- 如果未来业务要求签名按租户隔离，需要加字段 + Migration + 修改查询逻辑
