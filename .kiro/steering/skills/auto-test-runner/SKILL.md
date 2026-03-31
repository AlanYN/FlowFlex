---
name: auto-test-runner
description: Execute test cases against a codebase and collect structured results. Use when test cases are ready and need to be run, when verifying a fix resolves a failing test, or when generating a test execution report.
inclusion: manual
---

# Auto Test Runner Skill

Execute test cases systematically and collect structured results for the QA coverage report.

## When to Use

- Test cases have been generated and are ready to execute
- Verifying that a bug fix resolves a previously failing test
- Generating the final test execution report for QA Spec

## Execution Workflow

### 1. Identify Test Runner

Detect the project's test framework from config files:

| File | Framework |
|------|-----------|
| `package.json` with `jest` / `vitest` / `mocha` | JavaScript/TypeScript |
| `pytest.ini` / `pyproject.toml` with `pytest` | Python |
| `pom.xml` / `build.gradle` | Java (JUnit) |
| `go.mod` | Go (testing) |

### 2. Run Tests

Execute with a single-run flag (no watch mode):

```bash
# Jest
npx jest --run --json --outputFile=test-results.json

# Vitest
npx vitest run --reporter=json --outputFile=test-results.json

# pytest
pytest --tb=short -q --json-report --json-report-file=test-results.json

# Go
go test ./... -v 2>&1 | tee test-results.txt
```

### 3. Map Results to Test Cases

For each test case in `qa/qa-spec.md`, find the corresponding test result and update:

| TC Status | Condition |
|-----------|-----------|
| `pass` | Test ran and assertions passed |
| `fail` | Test ran but one or more assertions failed |
| `skip` | Test was skipped or excluded |
| `pending` | No corresponding automated test found |

### 4. Output: Execution Results

Update the `executionResults` table in `qa/qa-spec.md`:

```markdown
## 执行结果（executionResults）

| 用例编号 | 用例标题 | 执行状态 | 执行时间 | 备注 |
|---------|---------|---------|---------|------|
| TC-001 | {标题} | pass | {时间} | - |
| TC-002 | {标题} | fail | {时间} | {失败原因摘要} |
| TC-003 | {标题} | skip | - | 需要手动执行 |
```

## Failure Analysis

When a test fails, capture:

1. **Failure message**: The exact assertion error or exception
2. **Stack trace**: First 5 lines are usually sufficient
3. **Root cause category**:
   - `implementation_bug` — code does not match spec
   - `test_setup` — preconditions not met
   - `environment` — missing dependency or config
   - `flaky` — intermittent failure, not deterministic

## Manual Test Cases

For test cases that cannot be automated (UI interactions, exploratory tests):

- Mark status as `pending` until manually executed
- Add note: `"手动执行所需"`
- After manual execution, update status to `pass` / `fail` with timestamp

## Execution Rules

1. Never run tests in watch mode — use single-run flags only
2. Always map results back to TC-xxx IDs in qa-spec.md
3. A `fail` result must include a `failureReason` — never leave it blank
4. If the test suite cannot be found or run, report the blocker and list all TCs as `pending`
5. Do not modify test files to make tests pass — report failures as-is
