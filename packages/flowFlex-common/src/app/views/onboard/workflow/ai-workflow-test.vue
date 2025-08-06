<template>
  <div class="ai-workflow-test">
    <h1>AI工作流测试页面</h1>
    <p>如果您能看到这个页面，说明路由配置正确。</p>
    <el-button type="primary" @click="goBack">返回</el-button>
  </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';

const router = useRouter();

const goBack = () => {
  router.push('/onboard/onboardWorkflow');
};
</script>

<style scoped>
.ai-workflow-test {
  padding: 20px;
  text-align: center;
}
</style> 