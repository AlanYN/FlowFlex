const filterPipelineColums = (columns, showPipeline) => {
	if (showPipeline) {
		return columns;
	}
	return columns.filter((item) => item.label != 'PIPELINE' && item.label != 'PIPELINE STAGE');
};

export { filterPipelineColums };
