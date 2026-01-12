/**
 * Test setup for AI File Analyzer utilities
 *
 * This file configures the test environment for property-based testing
 * using fast-check library.
 */

import fc from 'fast-check';

// Configure fast-check defaults for property tests
// Run at least 100 iterations per property test
fc.configureGlobal({
	numRuns: 100,
	verbose: false,
});

// Mock browser APIs that may not be available in jsdom
if (typeof window !== 'undefined') {
	// Mock URL.createObjectURL and URL.revokeObjectURL
	if (!window.URL.createObjectURL) {
		window.URL.createObjectURL = jest.fn(() => 'blob:mock-url');
	}
	if (!window.URL.revokeObjectURL) {
		window.URL.revokeObjectURL = jest.fn();
	}
}

// Export fast-check for convenience
export { fc };
