/**
 * Lookup configuration for a single field
 */
export interface LookupConfig {
	/** API endpoint to fetch lookup options */
	endpoint: string;
	/** JSONPath to extract display value from each option item */
	displayPath: string;
	/** JSONPath to extract actual value from each option item */
	valuePath: string;
	/** Optional JSONPath to locate the options array in the response */
	responsePath?: string;
	/** Optional custom headers for the lookup request */
	headers?: Record<string, string>;
	/** Optional override integration ID (uses default if null) */
	integrationId?: string | null;
}

/**
 * Field mapping item extended with lookup configuration
 */
export interface FieldMappingItemWithLookup {
	/** External API field name */
	externalFieldName: string;
	/** WFE internal field ID */
	wfeFieldId: string;
	/** Field type */
	fieldType: number;
	/** Sync direction */
	syncDirection: number;
	/** Whether lookup is enabled for this field */
	lookupEnabled?: boolean;
	/** Lookup configuration */
	lookup?: LookupConfig;
}

/**
 * Request DTO for previewing lookup options
 */
export interface LookupPreviewRequest {
	/** Integration ID to use for authentication */
	integrationId: string;
	/** API endpoint to fetch lookup options */
	endpoint: string;
	/** JSONPath to extract display value */
	displayPath: string;
	/** JSONPath to extract actual value */
	valuePath: string;
	/** Optional JSONPath to locate the options array */
	responsePath?: string;
	/** Optional custom headers */
	headers?: Record<string, string>;
}

/**
 * Response DTO for lookup preview
 */
export interface LookupPreviewResponse {
	/** Whether the lookup was successful */
	success: boolean;
	/** Preview options (limited to first 10) */
	options: OptionItem[];
	/** Total count of available options */
	totalCount: number;
	/** Error message when lookup fails */
	error?: string;
}

/**
 * Single option item with display text and value
 */
export interface OptionItem {
	/** Display text shown to the user */
	display: string;
	/** Actual value stored when option is selected */
	value: string;
}

/**
 * Mapping configuration model for saving
 */
export interface MappingConfigModel {
	/** List of field mapping items with lookup */
	fieldMappings?: FieldMappingItemWithLookup[];
	/** Global lookup configuration */
	lookupConfig?: LookupGlobalConfig;
}

/**
 * Global lookup configuration
 */
export interface LookupGlobalConfig {
	/** Request timeout in seconds (default: 10) */
	timeoutSeconds?: number;
	/** Maximum options per field (default: 200) */
	maxOptionsPerField?: number;
}
