# FlowFlex MCP服务集成指南

## 概述

本文档描述了如何将FlowFlex后端服务包装成MCP (Memory, Context, Processing) 服务，为AI提供上下文记忆、知识图谱和智能生成功能。

## MCP服务架构

### 1. 核心组件

```
FlowFlex MCP Service
├── Memory Layer (记忆层)
│   ├── Context Storage (上下文存储)
│   ├── Knowledge Graph (知识图谱)
│   └── Semantic Search (语义搜索)
├── AI Integration Layer (AI集成层)
│   ├── ZhipuAI Provider (智谱AI提供商)
│   ├── Workflow Generation (工作流生成)
│   ├── Questionnaire Generation (问卷生成)
│   └── Checklist Generation (检查清单生成)
└── API Layer (API层)
    ├── RESTful APIs (REST接口)
    ├── Streaming APIs (流式接口)
    └── WebSocket Support (WebSocket支持)
```

### 2. 数据流

```
AI Agent → MCP API → Context Memory → AI Service → Generated Content → Response
     ↑                    ↓
     └── Knowledge Graph ←┘
```

## 配置说明

### 1. AI服务配置

在 `appsettings.json` 中配置AI服务：

```json
{
  "AI": {
    "Provider": "ZhipuAI",
    "ZhipuAI": {
      "ApiKey": "6706ab1ca566494aafc1fbcfaf4facce.6K41ihqtV6ccHIbi",
      "BaseUrl": "https://open.bigmodel.cn/api/paas/v4",
      "Model": "glm-4",
      "MaxTokens": 8192,
      "Temperature": 0.7,
      "EnableStreaming": true
    },
    "Features": {
      "WorkflowGeneration": true,
      "QuestionnaireGeneration": true,
      "ChecklistGeneration": true,
      "RealTimePreview": true,
      "ContextMemory": true
    }
  }
}
```

### 2. MCP服务配置

```json
{
  "MCP": {
    "EnableMCP": true,
    "Services": {
      "Memory": {
        "Provider": "InMemory",
        "MaxEntities": 10000,
        "EnablePersistence": true
      },
      "Context": {
        "MaxContextLength": 8192,
        "EnableSemanticSearch": true,
        "EmbeddingModel": "text-embedding-ada-002"
      }
    }
  }
}
```

## API接口文档

### 1. 上下文管理接口

#### 存储上下文
```http
POST /api/mcp/v1/contexts
Content-Type: application/json
Authorization: Bearer {token}

{
  "contextId": "workflow_gen_20241220_001",
  "content": "用户需要一个员工入职流程...",
  "metadata": {
    "type": "workflow_generation",
    "user_id": "user123",
    "timestamp": "2024-12-20T10:00:00Z"
  }
}
```

#### 搜索上下文
```http
GET /api/mcp/v1/contexts/search?query=员工入职&limit=10
Authorization: Bearer {token}
```

### 2. AI工具接口

#### 智能工作流生成
```http
POST /api/mcp/v1/tools/generate-workflow
Content-Type: application/json
Authorization: Bearer {token}

{
  "description": "我需要一个员工入职流程",
  "context": "科技公司，包含IT设备分配",
  "industry": "Technology",
  "processType": "Onboarding",
  "includeApprovals": true,
  "userId": "user123",
  "sessionId": "session456"
}
```

#### 实时流式生成
```http
POST /api/ai/workflows/v1/generate/stream
Content-Type: application/json
Authorization: Bearer {token}

{
  "description": "创建客户服务流程",
  "industry": "Retail"
}
```

### 3. 知识图谱接口

#### 创建实体
```http
POST /api/mcp/v1/entities
Content-Type: application/json

{
  "type": "workflow",
  "name": "员工入职流程",
  "properties": {
    "industry": "Technology",
    "stages": 5,
    "duration": "7天"
  },
  "tags": ["onboarding", "hr", "technology"]
}
```

#### 查询图谱
```http
POST /api/mcp/v1/graph/query
Content-Type: application/json

{
  "query": "入职流程"
}
```

## 使用示例

### 1. AI Agent调用示例

```python
import requests
import json

class FlowFlexMCPClient:
    def __init__(self, base_url, token):
        self.base_url = base_url
        self.headers = {
            'Authorization': f'Bearer {token}',
            'Content-Type': 'application/json'
        }
    
    def generate_workflow_with_memory(self, description, user_id=None):
        """使用MCP服务生成工作流（带上下文记忆）"""
        payload = {
            "description": description,
            "userId": user_id,
            "sessionId": f"session_{int(time.time())}"
        }
        
        response = requests.post(
            f"{self.base_url}/mcp/v1/tools/generate-workflow",
            headers=self.headers,
            json=payload
        )
        
        return response.json()
    
    def store_context(self, context_id, content, metadata=None):
        """存储上下文信息"""
        payload = {
            "contextId": context_id,
            "content": content,
            "metadata": metadata or {}
        }
        
        response = requests.post(
            f"{self.base_url}/mcp/v1/contexts",
            headers=self.headers,
            json=payload
        )
        
        return response.json()

# 使用示例
client = FlowFlexMCPClient("https://api.flowflex.com", "your-token")

# 生成工作流
result = client.generate_workflow_with_memory(
    "我需要一个新员工培训流程，包含安全培训和技能培训",
    user_id="hr_manager_001"
)

print(f"生成结果: {result}")
```

### 2. 前端集成示例

```javascript
// AI工作流生成组件使用
import { generateAIWorkflow, mcpGenerateWorkflow } from '@/apis/ai/workflow';

// 普通AI生成
const result = await generateAIWorkflow({
  description: "创建项目管理流程",
  industry: "Technology",
  processType: "Project Management"
});

// 带MCP记忆的生成
const mcpResult = await mcpGenerateWorkflow({
  description: "创建项目管理流程", 
  userId: "user123",
  sessionId: "session456"
});
```

## 部署说明

### 1. Docker部署

创建 `docker-compose.mcp.yml`:

```yaml
version: '3.8'

services:
  flowflex-mcp:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: flowflex-mcp-service
    ports:
      - "8080:8080"
      - "8081:8081"  # MCP专用端口
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - AI__Provider=ZhipuAI
      - AI__ZhipuAI__ApiKey=6706ab1ca566494aafc1fbcfaf4facce.6K41ihqtV6ccHIbi
      - MCP__EnableMCP=true
    volumes:
      - mcp_data:/app/data
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/mcp/v1/status"]
      interval: 30s
      timeout: 10s
      retries: 3

volumes:
  mcp_data:
```

### 2. 服务注册

确保在 `Program.cs` 中正确注册服务：

```csharp
// 注册AI和MCP服务
builder.Services.AddOptions<AIOptions>()
    .Bind(builder.Configuration.GetSection("AI"));

builder.Services.AddOptions<MCPOptions>()
    .Bind(builder.Configuration.GetSection("MCP"));

// 服务将通过IScopedService接口自动注册
```

## 监控和日志

### 1. 健康检查

```http
GET /api/mcp/v1/status
```

响应示例：
```json
{
  "success": true,
  "data": {
    "isAvailable": true,
    "version": "1.0.0",
    "features": [
      "Context Storage",
      "Knowledge Graph", 
      "AI Tool Integration"
    ],
    "memoryProvider": "InMemory",
    "lastHealthCheck": "2024-12-20T10:00:00Z"
  }
}
```

### 2. 日志监控

MCP服务会记录以下类型的日志：

- **Context Operations**: 上下文存储和检索操作
- **AI Generation**: AI生成请求和结果
- **Knowledge Graph**: 实体和关系操作
- **Performance**: 性能指标和响应时间

## 安全考虑

### 1. 认证授权

- 所有MCP API都需要JWT认证
- 支持基于角色的访问控制
- API密钥管理和轮换

### 2. 数据保护

- 上下文数据加密存储
- 敏感信息脱敏
- 数据保留策略

### 3. 速率限制

```json
{
  "RateLimit": {
    "AI": {
      "RequestsPerMinute": 60,
      "RequestsPerHour": 1000
    },
    "MCP": {
      "RequestsPerMinute": 120,
      "RequestsPerHour": 2000
    }
  }
}
```

## 故障排除

### 1. 常见问题

**AI服务不可用**
- 检查API密钥是否正确
- 验证网络连接
- 查看智谱AI服务状态

**上下文搜索不准确**
- 调整搜索算法参数
- 增加上下文数据量
- 优化语义匹配逻辑

**性能问题**
- 启用缓存机制
- 优化数据库查询
- 调整并发处理参数

### 2. 调试命令

```bash
# 检查服务状态
curl -H "Authorization: Bearer $TOKEN" \
     https://api.flowflex.com/mcp/v1/status

# 测试AI生成
curl -X POST -H "Content-Type: application/json" \
     -H "Authorization: Bearer $TOKEN" \
     -d '{"description":"测试工作流"}' \
     https://api.flowflex.com/ai/workflows/v1/generate
```

## 扩展功能

### 1. 自定义AI提供商

实现 `IAIProvider` 接口以支持其他AI服务：

```csharp
public interface IAIProvider
{
    Task<string> GenerateAsync(string prompt);
    IAsyncEnumerable<string> StreamGenerateAsync(string prompt);
}
```

### 2. 插件系统

支持通过插件扩展MCP功能：

```csharp
public interface IMCPPlugin
{
    string Name { get; }
    Task<object> ExecuteAsync(string command, object parameters);
}
```

## 版本更新

### v1.0.0 (当前版本)
- 基础MCP服务实现
- 智谱AI集成
- 上下文记忆功能
- 知识图谱支持

### 路线图
- v1.1.0: 增强语义搜索
- v1.2.0: 多模态支持
- v1.3.0: 分布式部署支持

---

## 联系支持

如有问题或建议，请联系：
- 技术支持: tech-support@flowflex.com
- 文档反馈: docs@flowflex.com
- GitHub Issues: https://github.com/flowflex/flowflex/issues 