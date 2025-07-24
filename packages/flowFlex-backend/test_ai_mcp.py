#!/usr/bin/env python3
"""
FlowFlex AI & MCP Service Test Script
测试AI工作流生成和MCP服务功能
"""

import requests
import json
import time
import sys
from typing import Dict, Any, Optional

class FlowFlexAIMCPTester:
    def __init__(self, base_url: str = "http://localhost:8080", token: Optional[str] = None):
        self.base_url = base_url.rstrip('/')
        self.headers = {
            'Content-Type': 'application/json'
        }
        if token:
            self.headers['Authorization'] = f'Bearer {token}'
    
    def test_ai_service_status(self) -> bool:
        """测试AI服务状态"""
        print("🧪 Testing AI Service Status...")
        try:
            response = requests.get(
                f"{self.base_url}/api/ai/workflows/v1/status",
                headers=self.headers
            )
            
            if response.status_code == 200:
                data = response.json()
                print(f"✅ AI Service Status: {data}")
                return data.get('success', False)
            else:
                print(f"❌ AI Service Status Failed: {response.status_code} - {response.text}")
                return False
                
        except Exception as e:
            print(f"❌ AI Service Status Error: {str(e)}")
            return False
    
    def test_mcp_service_status(self) -> bool:
        """测试MCP服务状态"""
        print("🧪 Testing MCP Service Status...")
        try:
            response = requests.get(
                f"{self.base_url}/api/mcp/v1/status",
                headers=self.headers
            )
            
            if response.status_code == 200:
                data = response.json()
                print(f"✅ MCP Service Status: {data}")
                return data.get('success', False)
            else:
                print(f"❌ MCP Service Status Failed: {response.status_code} - {response.text}")
                return False
                
        except Exception as e:
            print(f"❌ MCP Service Status Error: {str(e)}")
            return False
    
    def test_ai_workflow_generation(self) -> bool:
        """测试AI工作流生成"""
        print("🧪 Testing AI Workflow Generation...")
        
        test_input = {
            "description": "我需要一个员工入职流程，包括文档收集、IT设备分配、培训安排和试用期评估",
            "industry": "Technology",
            "processType": "Onboarding",
            "includeApprovals": True,
            "includeNotifications": True,
            "context": "科技公司新员工入职流程",
            "requirements": [
                "包含HR文档收集",
                "IT设备申请和分配",
                "安全培训",
                "试用期评估"
            ]
        }
        
        try:
            response = requests.post(
                f"{self.base_url}/api/ai/workflows/v1/generate",
                headers=self.headers,
                json=test_input
            )
            
            if response.status_code == 200:
                data = response.json()
                if data.get('success'):
                    result = data.get('data', {})
                    print(f"✅ AI Workflow Generated:")
                    print(f"   - Name: {result.get('generatedWorkflow', {}).get('name', 'N/A')}")
                    print(f"   - Stages: {len(result.get('stages', []))}")
                    print(f"   - Confidence: {result.get('confidenceScore', 0) * 100:.1f}%")
                    return True
                else:
                    print(f"❌ AI Generation Failed: {data.get('message', 'Unknown error')}")
                    return False
            else:
                print(f"❌ AI Generation Request Failed: {response.status_code} - {response.text}")
                return False
                
        except Exception as e:
            print(f"❌ AI Generation Error: {str(e)}")
            return False
    
    def test_mcp_context_storage(self) -> bool:
        """测试MCP上下文存储"""
        print("🧪 Testing MCP Context Storage...")
        
        context_id = f"test_context_{int(time.time())}"
        test_context = {
            "contextId": context_id,
            "content": "测试上下文：用户询问关于员工入职流程的创建",
            "metadata": {
                "type": "test",
                "timestamp": time.time(),
                "user": "test_user"
            }
        }
        
        try:
            # 存储上下文
            response = requests.post(
                f"{self.base_url}/api/mcp/v1/contexts",
                headers=self.headers,
                json=test_context
            )
            
            if response.status_code == 200:
                print(f"✅ Context Stored: {context_id}")
                
                # 检索上下文
                time.sleep(0.5)  # 等待存储完成
                get_response = requests.get(
                    f"{self.base_url}/api/mcp/v1/contexts/{context_id}",
                    headers=self.headers
                )
                
                if get_response.status_code == 200:
                    retrieved_data = get_response.json()
                    if retrieved_data.get('success'):
                        print(f"✅ Context Retrieved Successfully")
                        return True
                    else:
                        print(f"❌ Context Retrieval Failed: {retrieved_data.get('message', 'Unknown error')}")
                        return False
                else:
                    print(f"❌ Context Retrieval Request Failed: {get_response.status_code}")
                    return False
            else:
                print(f"❌ Context Storage Failed: {response.status_code} - {response.text}")
                return False
                
        except Exception as e:
            print(f"❌ MCP Context Storage Error: {str(e)}")
            return False
    
    def test_mcp_workflow_generation(self) -> bool:
        """测试MCP工作流生成（带上下文记忆）"""
        print("🧪 Testing MCP Workflow Generation with Memory...")
        
        test_request = {
            "description": "创建一个客户服务流程，包括问题接收、分类、处理和反馈",
            "industry": "Service",
            "processType": "Customer Service",
            "includeApprovals": False,
            "includeNotifications": True,
            "userId": "test_user_001",
            "sessionId": f"session_{int(time.time())}"
        }
        
        try:
            response = requests.post(
                f"{self.base_url}/api/mcp/v1/tools/generate-workflow",
                headers=self.headers,
                json=test_request
            )
            
            if response.status_code == 200:
                data = response.json()
                if data.get('success'):
                    result = data.get('data', {})
                    print(f"✅ MCP Workflow Generated:")
                    print(f"   - Name: {result.get('generatedWorkflow', {}).get('name', 'N/A')}")
                    print(f"   - Stages: {len(result.get('stages', []))}")
                    print(f"   - With Context Memory: Yes")
                    return True
                else:
                    print(f"❌ MCP Generation Failed: {data.get('message', 'Unknown error')}")
                    return False
            else:
                print(f"❌ MCP Generation Request Failed: {response.status_code} - {response.text}")
                return False
                
        except Exception as e:
            print(f"❌ MCP Generation Error: {str(e)}")
            return False
    
    def test_context_search(self) -> bool:
        """测试上下文搜索"""
        print("🧪 Testing Context Search...")
        
        try:
            response = requests.get(
                f"{self.base_url}/api/mcp/v1/contexts/search",
                headers=self.headers,
                params={"query": "员工入职", "limit": 5}
            )
            
            if response.status_code == 200:
                data = response.json()
                if data.get('success'):
                    results = data.get('data', [])
                    print(f"✅ Context Search Results: {len(results)} found")
                    for i, result in enumerate(results[:3]):  # 显示前3个结果
                        print(f"   {i+1}. {result.get('contextId', 'N/A')} (Score: {result.get('relevanceScore', 0):.2f})")
                    return True
                else:
                    print(f"❌ Context Search Failed: {data.get('message', 'Unknown error')}")
                    return False
            else:
                print(f"❌ Context Search Request Failed: {response.status_code} - {response.text}")
                return False
                
        except Exception as e:
            print(f"❌ Context Search Error: {str(e)}")
            return False
    
    def test_knowledge_graph(self) -> bool:
        """测试知识图谱功能"""
        print("🧪 Testing Knowledge Graph...")
        
        # 创建实体
        entity_data = {
            "type": "workflow",
            "name": "测试工作流",
            "properties": {
                "industry": "Technology",
                "stages": 3,
                "created_by": "test_user"
            },
            "tags": ["test", "workflow", "technology"]
        }
        
        try:
            response = requests.post(
                f"{self.base_url}/api/mcp/v1/entities",
                headers=self.headers,
                json=entity_data
            )
            
            if response.status_code == 200:
                data = response.json()
                if data.get('success'):
                    entity_id = data.get('data')
                    print(f"✅ Entity Created: {entity_id}")
                    
                    # 查询图谱
                    time.sleep(0.5)
                    query_response = requests.post(
                        f"{self.base_url}/api/mcp/v1/graph/query",
                        headers=self.headers,
                        json={"query": "测试工作流"}
                    )
                    
                    if query_response.status_code == 200:
                        query_data = query_response.json()
                        if query_data.get('success'):
                            result = query_data.get('data', {})
                            entities = result.get('entities', [])
                            print(f"✅ Graph Query Results: {len(entities)} entities found")
                            return True
                        else:
                            print(f"❌ Graph Query Failed: {query_data.get('message', 'Unknown error')}")
                            return False
                    else:
                        print(f"❌ Graph Query Request Failed: {query_response.status_code}")
                        return False
                else:
                    print(f"❌ Entity Creation Failed: {data.get('message', 'Unknown error')}")
                    return False
            else:
                print(f"❌ Entity Creation Request Failed: {response.status_code} - {response.text}")
                return False
                
        except Exception as e:
            print(f"❌ Knowledge Graph Error: {str(e)}")
            return False
    
    def run_all_tests(self) -> Dict[str, bool]:
        """运行所有测试"""
        print("🚀 Starting FlowFlex AI & MCP Service Tests\n")
        
        tests = {
            "AI Service Status": self.test_ai_service_status,
            "MCP Service Status": self.test_mcp_service_status,
            "AI Workflow Generation": self.test_ai_workflow_generation,
            "MCP Context Storage": self.test_mcp_context_storage,
            "MCP Workflow Generation": self.test_mcp_workflow_generation,
            "Context Search": self.test_context_search,
            "Knowledge Graph": self.test_knowledge_graph
        }
        
        results = {}
        passed = 0
        total = len(tests)
        
        for test_name, test_func in tests.items():
            print(f"\n{'='*50}")
            try:
                result = test_func()
                results[test_name] = result
                if result:
                    passed += 1
            except Exception as e:
                print(f"❌ {test_name} - Unexpected Error: {str(e)}")
                results[test_name] = False
            
            time.sleep(1)  # 测试间隔
        
        print(f"\n{'='*50}")
        print(f"📊 Test Results Summary:")
        print(f"   Total Tests: {total}")
        print(f"   Passed: {passed}")
        print(f"   Failed: {total - passed}")
        print(f"   Success Rate: {passed/total*100:.1f}%")
        
        print(f"\n📋 Detailed Results:")
        for test_name, result in results.items():
            status = "✅ PASS" if result else "❌ FAIL"
            print(f"   {status} - {test_name}")
        
        return results

def main():
    """主函数"""
    import argparse
    
    parser = argparse.ArgumentParser(description='FlowFlex AI & MCP Service Tester')
    parser.add_argument('--url', default='http://localhost:8080', help='Base URL of FlowFlex API')
    parser.add_argument('--token', help='Authorization token (optional)')
    parser.add_argument('--test', choices=[
        'ai-status', 'mcp-status', 'ai-gen', 'mcp-context', 
        'mcp-gen', 'search', 'graph', 'all'
    ], default='all', help='Specific test to run')
    
    args = parser.parse_args()
    
    tester = FlowFlexAIMCPTester(args.url, args.token)
    
    if args.test == 'all':
        results = tester.run_all_tests()
        # 如果有失败的测试，返回非零退出码
        failed_count = sum(1 for result in results.values() if not result)
        sys.exit(failed_count)
    else:
        # 运行单个测试
        test_methods = {
            'ai-status': tester.test_ai_service_status,
            'mcp-status': tester.test_mcp_service_status,
            'ai-gen': tester.test_ai_workflow_generation,
            'mcp-context': tester.test_mcp_context_storage,
            'mcp-gen': tester.test_mcp_workflow_generation,
            'search': tester.test_context_search,
            'graph': tester.test_knowledge_graph
        }
        
        test_func = test_methods.get(args.test)
        if test_func:
            result = test_func()
            sys.exit(0 if result else 1)
        else:
            print(f"Unknown test: {args.test}")
            sys.exit(1)

if __name__ == "__main__":
    main() 