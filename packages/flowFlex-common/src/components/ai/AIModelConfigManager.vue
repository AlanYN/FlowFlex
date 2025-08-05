<template>
  <div class="ai-model-config-manager">
    <!-- Header -->
    <div class="config-header">
      <div class="header-info">
        <h3>AI Model Configuration</h3>
        <p>Manage your AI model settings and test connections</p>
      </div>
      <div class="header-actions">
        <el-button type="primary" @click="showAddConfigDialog = true">
          <el-icon class="mr-1"><Plus /></el-icon>
          Add New Model
        </el-button>
      </div>
    </div>

    <!-- Current Models List -->
    <div class="models-list">
      <el-table 
        :data="modelConfigs" 
        v-loading="loading"
        stripe
        style="width: 100%"
      >
        <el-table-column prop="provider" label="Provider" width="120">
          <template #default="{ row }">
            <el-tag :type="getProviderType(row.provider)">
              {{ row.provider }}
            </el-tag>
          </template>
        </el-table-column>
        
        <el-table-column prop="modelName" label="Model" width="150" />
        
        <el-table-column prop="baseUrl" label="API URL" width="200" show-overflow-tooltip />
        
        <el-table-column label="Status" width="100">
          <template #default="{ row }">
            <el-tag :type="row.isAvailable ? 'success' : 'danger'" size="small">
              {{ row.isAvailable ? 'Online' : 'Offline' }}
            </el-tag>
          </template>
        </el-table-column>
        
        <el-table-column label="Default" width="80">
          <template #default="{ row }">
            <el-icon v-if="row.isDefault" class="text-green-500">
              <Check />
            </el-icon>
          </template>
        </el-table-column>
        
        <el-table-column prop="lastCheckTime" label="Last Check" width="150">
          <template #default="{ row }">
            {{ formatDateTime(row.lastCheckTime) }}
          </template>
        </el-table-column>
        
        <el-table-column label="Actions" width="200">
          <template #default="{ row }">
            <div class="action-buttons">
              <el-button size="small" @click="testConnection(row)" :loading="testingId === row.id">
                <el-icon><Connection /></el-icon>
              </el-button>
              <el-button size="small" @click="editConfig(row)">
                <el-icon><Edit /></el-icon>
              </el-button>
              <el-button 
                size="small" 
                type="success" 
                @click="setDefault(row.id)" 
                :disabled="row.isDefault"
              >
                <el-icon><Star /></el-icon>
              </el-button>
              <el-button 
                size="small" 
                type="danger" 
                @click="deleteConfig(row.id)"
                :disabled="row.isDefault"
              >
                <el-icon><Delete /></el-icon>
              </el-button>
            </div>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <!-- Add/Edit Config Dialog -->
    <el-dialog
      v-model="showAddConfigDialog"
      :title="editingConfig ? 'Edit AI Model' : 'Add New AI Model'"
      width="600px"
      :close-on-click-modal="false"
    >
      <el-form 
        ref="configFormRef"
        :model="configForm" 
        :rules="configRules"
        label-width="120px"
      >
        <el-form-item label="Provider" prop="provider">
          <el-select 
            v-model="configForm.provider" 
            placeholder="Select AI Provider"
            @change="onProviderChange"
          >
            <el-option 
              v-for="provider in availableProviders"
              :key="provider.name"
              :label="provider.displayName"
              :value="provider.name"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="Model Name" prop="modelName">
          <el-input 
            v-model="configForm.modelName" 
            placeholder="e.g., gpt-4, glm-4, claude-3"
          />
        </el-form-item>

        <el-form-item label="API Key" prop="apiKey">
          <el-input 
            v-model="configForm.apiKey" 
            type="password" 
            placeholder="Enter your API key"
            show-password
          />
        </el-form-item>

        <el-form-item label="Base URL" prop="baseUrl">
          <el-input 
            v-model="configForm.baseUrl" 
            placeholder="API base URL (optional for default providers)"
          />
        </el-form-item>

        <el-form-item label="API Version" prop="apiVersion">
          <el-input 
            v-model="configForm.apiVersion" 
            placeholder="API version (if required)"
          />
        </el-form-item>

        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="Temperature" prop="temperature">
              <el-slider 
                v-model="configForm.temperature" 
                :min="0" 
                :max="2" 
                :step="0.1" 
                show-input
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="Max Tokens" prop="maxTokens">
              <el-input-number 
                v-model="configForm.maxTokens" 
                :min="100" 
                :max="32000"
                :step="100"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="Enable Streaming">
          <el-switch v-model="configForm.enableStreaming" />
        </el-form-item>

        <el-form-item label="Set as Default">
          <el-switch v-model="configForm.isDefault" />
        </el-form-item>

        <el-form-item label="Remarks">
          <el-input 
            v-model="configForm.remarks" 
            type="textarea" 
            :rows="3"
            placeholder="Optional notes about this configuration"
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <div class="dialog-footer">
          <el-button @click="cancelConfig">Cancel</el-button>
          <el-button type="primary" @click="saveConfig" :loading="saving">
            {{ editingConfig ? 'Update' : 'Add' }} Model
          </el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue';
import { ElMessage, ElMessageBox, type FormInstance } from 'element-plus';
import { 
  Plus, 
  Check, 
  Edit, 
  Delete, 
  Star, 
  Connection 
} from '@element-plus/icons-vue';

// API imports will be created
import { 
  getUserAIModels, 
  createAIModel, 
  updateAIModel, 
  deleteAIModel, 
  setDefaultAIModel,
  testAIModelConnection,
  getAIProviders 
} from '@/apis/ai/config';

// Types
interface AIModelConfig {
  id: number;
  provider: string;
  modelName: string;
  apiKey: string;
  baseUrl: string;
  apiVersion: string;
  temperature: number;
  maxTokens: number;
  enableStreaming: boolean;
  isDefault: boolean;
  isAvailable: boolean;
  lastCheckTime: string;
  remarks: string;
}

interface AIProvider {
  name: string;
  displayName: string;
  defaultBaseUrl: string;
  requiresApiVersion: boolean;
  defaultModels: string[];
}

// Emits
const emit = defineEmits<{
  configUpdated: []
}>();

// Reactive data
const loading = ref(false);
const saving = ref(false);
const testingId = ref<number | null>(null);
const modelConfigs = ref<AIModelConfig[]>([]);
const availableProviders = ref<AIProvider[]>([]);
const showAddConfigDialog = ref(false);
const editingConfig = ref<AIModelConfig | null>(null);
const configFormRef = ref<FormInstance | null>(null);

const configForm = reactive({
  provider: '',
  modelName: '',
  apiKey: '',
  baseUrl: '',
  apiVersion: '',
  temperature: 0.7,
  maxTokens: 4096,
  enableStreaming: true,
  isDefault: false,
  remarks: ''
});

const configRules = {
  provider: [
    { required: true, message: 'Please select a provider', trigger: 'change' }
  ],
  modelName: [
    { required: true, message: 'Please enter model name', trigger: 'blur' }
  ],
  apiKey: [
    { required: true, message: 'Please enter API key', trigger: 'blur' }
  ]
};

// Methods
const loadProviders = async () => {
  try {
    const response = await getAIProviders();
    if (response.success) {
      availableProviders.value = response.data;
    }
  } catch (error) {
    console.error('Failed to load providers:', error);
  }
};

const loadConfigs = async () => {
  loading.value = true;
  try {
    const response = await getUserAIModels();
    if (response.success) {
      modelConfigs.value = response.data;
    }
  } catch (error) {
    console.error('Failed to load configs:', error);
    ElMessage.error('Failed to load AI model configurations');
  } finally {
    loading.value = false;
  }
};

const onProviderChange = () => {
  const provider = availableProviders.value.find(p => p.name === configForm.provider);
  if (provider) {
    configForm.baseUrl = provider.defaultBaseUrl;
    if (provider.defaultModels.length > 0) {
      configForm.modelName = provider.defaultModels[0];
    }
  }
};

const testConnection = async (config: AIModelConfig) => {
  testingId.value = config.id;
  try {
    const response = await testAIModelConnection(config.id);
    if (response.success) {
      const result = response.data;
      if (result.isAvailable) {
        ElMessage.success('Connection test successful!');
      } else {
        ElMessage.error(`Connection failed: ${result.errorMessage}`);
      }
      // Refresh the list to update status
      await loadConfigs();
    }
  } catch (error) {
    console.error('Connection test error:', error);
    ElMessage.error('Connection test failed');
  } finally {
    testingId.value = null;
  }
};

const editConfig = (config: AIModelConfig) => {
  editingConfig.value = config;
  Object.assign(configForm, {
    provider: config.provider,
    modelName: config.modelName,
    apiKey: config.apiKey,
    baseUrl: config.baseUrl,
    apiVersion: config.apiVersion,
    temperature: config.temperature,
    maxTokens: config.maxTokens,
    enableStreaming: config.enableStreaming,
    isDefault: config.isDefault,
    remarks: config.remarks
  });
  showAddConfigDialog.value = true;
};

const setDefault = async (configId: number) => {
  try {
    const response = await setDefaultAIModel(configId);
    if (response.success) {
      ElMessage.success('Default model updated successfully!');
      await loadConfigs();
      emit('configUpdated');
    }
  } catch (error) {
    console.error('Set default error:', error);
    ElMessage.error('Failed to set default model');
  }
};

const deleteConfig = async (configId: number) => {
  try {
    await ElMessageBox.confirm(
      'This will permanently delete the AI model configuration. Continue?',
      'Warning',
      {
        confirmButtonText: 'Delete',
        cancelButtonText: 'Cancel',
        type: 'warning'
      }
    );

    const response = await deleteAIModel(configId);
    if (response.success) {
      ElMessage.success('Configuration deleted successfully!');
      await loadConfigs();
      emit('configUpdated');
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('Delete error:', error);
      ElMessage.error('Failed to delete configuration');
    }
  }
};

const saveConfig = async () => {
  if (!configFormRef.value) return;
  
  try {
    await configFormRef.value.validate();
  } catch {
    return;
  }

  saving.value = true;
  try {
    let response;
    if (editingConfig.value) {
      response = await updateAIModel(editingConfig.value.id, configForm);
    } else {
      response = await createAIModel(configForm);
    }

    if (response.success) {
      ElMessage.success(
        editingConfig.value ? 'Configuration updated successfully!' : 'Configuration added successfully!'
      );
      showAddConfigDialog.value = false;
      await loadConfigs();
      emit('configUpdated');
    }
  } catch (error) {
    console.error('Save error:', error);
    ElMessage.error('Failed to save configuration');
  } finally {
    saving.value = false;
  }
};

const cancelConfig = () => {
  showAddConfigDialog.value = false;
  editingConfig.value = null;
  configFormRef.value?.resetFields();
};

const getProviderType = (provider: string) => {
  const types = {
    'OpenAI': 'success',
    'ZhipuAI': 'primary',
    'Claude': 'warning',
    'DeepSeek': 'info'
  };
  return types[provider] || 'default';
};

const formatDateTime = (dateTime: string) => {
  if (!dateTime) return 'Never';
  return new Date(dateTime).toLocaleString();
};

// Lifecycle
onMounted(async () => {
  await Promise.all([loadProviders(), loadConfigs()]);
});
</script>

<style scoped>
.ai-model-config-manager {
  padding: 20px;
}

.config-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid #e5e7eb;
}

.header-info h3 {
  margin: 0 0 4px 0;
  font-size: 18px;
  font-weight: 600;
  color: #374151;
}

.header-info p {
  margin: 0;
  color: #6b7280;
  font-size: 14px;
}

.models-list {
  background: white;
  border-radius: 8px;
  overflow: hidden;
}

.action-buttons {
  display: flex;
  gap: 4px;
}

.action-buttons .el-button {
  padding: 4px 8px;
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

.el-form-item {
  margin-bottom: 18px;
}

.el-slider {
  margin-right: 16px;
}
</style> 