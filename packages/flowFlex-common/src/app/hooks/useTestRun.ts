import { ElMessageBox } from 'element-plus';
import type { TestResult } from '../apis/action';
import { testAction } from '../apis/action';

export interface TestRunOptions {
	actionId?: string;
	currentData: any;
	originalData: any;
	onSave: () => Promise<boolean>;
	silentSave?: boolean; // Whether to show save success message
}

export const useTestRun = () => {
	// Check if data has changed
	const hasDataChanged = (currentData: any, originalData: any): boolean => {
		if (!originalData) return false;

		// Deep compare data
		const currentStr = JSON.stringify(currentData);
		const originalStr = JSON.stringify(originalData);

		return currentStr !== originalStr;
	};

	const handleTestRun = async (options: TestRunOptions): Promise<boolean> => {
		const { actionId, currentData, originalData, onSave } = options;

		// If no Action ID, prompt to save first
		if (!actionId) {
			await ElMessageBox.alert(
				'Please save the action first before testing. The action needs to be created to get an Action ID for testing.',
				'Action Required',
				{
					confirmButtonText: 'OK',
					type: 'info',
				}
			);
			return false;
		}

		// Check if data has changed
		const hasChanges = hasDataChanged(currentData, originalData);

		if (hasChanges) {
			try {
				await ElMessageBox.confirm(
					'Data has been modified. Do you want to save and test?',
					'Save & Test',
					{
						confirmButtonText: 'Save & Test',
						cancelButtonText: 'Cancel',
						type: 'warning',
					}
				);

				// Execute save
				const saved = await onSave();
				if (!saved) {
					return false; // Save failed
				}
			} catch {
				// User cancelled
				return false;
			}
		}

		return true;
	};

	// Handle component test events
	const handleComponentTest = async (
		result: TestResult | null,
		options: TestRunOptions
	): Promise<boolean> => {
		// If result is null, it's a pre-check
		if (result === null) {
			const canTest = await handleTestRun(options);
			return canTest;
		}

		// If result is not null, it's a test result
		console.log('Test result received:', result);
		return true;
	};

	// Execute test method
	const executeTest = async (actionId: string, actionType: string): Promise<string> => {
		try {
			const response = await testAction(actionId);

			if (response.code === '200' && response.success) {
				const result = response.data as TestResult;
				return formatTestResult(result, actionType);
			} else {
				return 'Test run failed: ' + (response.msg || 'Unknown error');
			}
		} catch (error) {
			console.error('Test run failed:', error);
			return 'Test run failed: ' + error;
		}
	};

	// Format test result
	const formatTestResult = (result: TestResult, actionType: string): string => {
		let output = '';

		switch (actionType) {
			case 'Python Script':
				output = result.stdout || 'No output';
				if (result.executionTime) {
					output += `\n\nExecution Time: ${result.executionTime}s`;
				}
				if (result.memoryUsage) {
					output += `\nMemory Usage: ${result.memoryUsage} bytes`;
				}
				break;

			case 'HTTP API':
				output = `Status Code: ${result.statusCode || 'N/A'}\n`;
				output += `Response Body:\n${result.responseBody || 'No response body'}\n`;
				if (result.responseHeaders) {
					output += `\nResponse Headers:\n${JSON.stringify(
						result.responseHeaders,
						null,
						2
					)}`;
				}
				break;

			case 'Send Email':
				output = `Email Sent: ${result.emailSent ? 'Yes' : 'No'}\n`;
				if (result.recipients && result.recipients.length > 0) {
					output += `Recipients: ${result.recipients.join(', ')}\n`;
				}
				output += `Message: ${result.message || 'No message'}`;
				break;

			default:
				// Generic handling, display all available data
				output = JSON.stringify(result, null, 2);
				break;
		}

		return output;
	};

	return {
		handleTestRun,
		handleComponentTest,
		executeTest,
	};
};
