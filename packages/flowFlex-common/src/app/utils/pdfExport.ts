import { ElMessage } from 'element-plus';

/**
 * Detect if string contains CJK characters
 */
const containsCJK = (text: string): boolean => {
	if (!text) return false;
	// CJK Unified Ideographs + Extensions + common symbols used with CJK
	return /[\u3000-\u303F\u3400-\u4DBF\u4E00-\u9FFF\uF900-\uFAFF]/.test(text);
};

interface DrawTextOptions {
	fontSizePt?: number; // PDF font size in pt
	color?: string; // CSS color string like '#000000'
	fontWeight?: 'normal' | 'bold';
	fontFamilyOverride?: string; // Optional font family override
	maxWidthMm?: number; // Maximum width for text wrapping
	lineSpacingMm?: number; // Line spacing for multi-line text
}

/**
 * Draw text with CJK support. Falls back to pdf.text when no CJK is present.
 * For CJK, it renders to a canvas using system/web fonts, then embeds as an image.
 */
const drawTextWithCJK = async (
	pdf: any,
	text: string,
	xMm: number,
	yMm: number,
	opts: DrawTextOptions = {}
): Promise<void> => {
	const fontSizePt = opts.fontSizePt ?? 12;
	const color = opts.color ?? '#000000';
	const fontWeight = opts.fontWeight ?? 'normal';
	const fontFamily =
		opts.fontFamilyOverride ||
		"Arial, 'Microsoft YaHei', 'PingFang SC', 'Hiragino Sans GB', 'Noto Sans CJK', 'WenQuanYi Micro Hei', 'Heiti SC', 'Source Han Sans SC', sans-serif";

	// If text does NOT contain CJK, use native text to preserve existing behavior exactly
	if (!containsCJK(text)) {
		pdf.text(text, xMm, yMm);
		return;
	}

	// Convert pt->px for canvas rendering. 1pt = 1.3333px at 96 DPI
	const ptToPx = (pt: number) => (pt * 96) / 72;
	const pxToMm = (px: number) => (px * 25.4) / 96; // 96 DPI assumption

	const fontSizePx = Math.max(10, ptToPx(fontSizePt));

	// Create a temporary canvas for exact text rasterization
	const canvas = document.createElement('canvas');
	const ctx = canvas.getContext('2d');
	if (!ctx) {
		// Fallback if canvas is unavailable
		pdf.text(text, xMm, yMm);
		return;
	}

	ctx.font = `${fontWeight} ${fontSizePx}px ${fontFamily}`;
	const metrics = ctx.measureText(text);
	const textWidthPx = Math.ceil(metrics.width) + 2; // slight padding
	const lineHeightPx = Math.ceil(fontSizePx * 1.25);

	canvas.width = Math.max(1, textWidthPx);
	canvas.height = Math.max(1, lineHeightPx);

	// High-DPI scaling for sharper text
	const scale = window.devicePixelRatio || 1;
	if (scale !== 1) {
		canvas.width = Math.ceil(canvas.width * scale);
		canvas.height = Math.ceil(canvas.height * scale);
		ctx.scale(scale, scale);
	}

	// Re-apply font after scaling
	ctx.font = `${fontWeight} ${fontSizePx}px ${fontFamily}`;
	ctx.textBaseline = 'alphabetic';
	ctx.fillStyle = color;

	// Clear and render
	ctx.clearRect(0, 0, canvas.width, canvas.height);
	ctx.fillText(text, 0, fontSizePx);

	const dataUrl = canvas.toDataURL('image/png');

	// Convert canvas size to mm for PDF placement
	const widthMm = pxToMm(textWidthPx);
	const heightMm = pxToMm(lineHeightPx);

	// Adjust y so that the image baseline aligns approximately with jsPDF text baseline
	const baselineAdjustMm = pxToMm(lineHeightPx - fontSizePx); // approx descender space
	const yTopMm = yMm - (heightMm - baselineAdjustMm);

	pdf.addImage(dataUrl, 'PNG', xMm, yTopMm, widthMm, heightMm, undefined, 'FAST');
};

/**
 * Break text into lines that fit within maxWidth
 */
const breakTextIntoLines = (
	ctx: CanvasRenderingContext2D,
	text: string,
	maxWidthPx: number
): string[] => {
	const words = text.split(' ');
	const lines: string[] = [];
	let currentLine = '';

	for (const word of words) {
		const testLine = currentLine ? `${currentLine} ${word}` : word;
		const metrics = ctx.measureText(testLine);

		if (metrics.width <= maxWidthPx) {
			currentLine = testLine;
		} else {
			if (currentLine) {
				lines.push(currentLine);
				currentLine = word;
			} else {
				// Single word is too long, break it character by character
				const chars = word.split('');
				let charLine = '';
				for (const char of chars) {
					const testCharLine = charLine + char;
					if (ctx.measureText(testCharLine).width <= maxWidthPx) {
						charLine = testCharLine;
					} else {
						if (charLine) lines.push(charLine);
						charLine = char;
					}
				}
				if (charLine) currentLine = charLine;
			}
		}
	}

	if (currentLine) {
		lines.push(currentLine);
	}

	return lines.length > 0 ? lines : [text];
};

/**
 * Draw text with CJK support and automatic line wrapping
 */
const drawTextWithCJKWrapped = async (
	pdf: any,
	text: string,
	xMm: number,
	yMm: number,
	opts: DrawTextOptions = {}
): Promise<number> => {
	// Returns the height used in mm
	const fontSizePt = opts.fontSizePt ?? 12;
	const fontWeight = opts.fontWeight ?? 'normal';
	const maxWidthMm = opts.maxWidthMm;
	const lineSpacingMm = opts.lineSpacingMm ?? 1.2 * (fontSizePt * 0.352778); // Default line spacing
	const fontFamily =
		opts.fontFamilyOverride ||
		"Arial, 'Microsoft YaHei', 'PingFang SC', 'Hiragino Sans GB', 'Noto Sans CJK', 'WenQuanYi Micro Hei', 'Heiti SC', 'Source Han Sans SC', sans-serif";

	// If no max width specified or text doesn't contain CJK and is short, use simple rendering
	if (!maxWidthMm || (!containsCJK(text) && text.length < 50)) {
		if (!containsCJK(text)) {
			pdf.text(text, xMm, yMm);
		} else {
			await drawTextWithCJK(pdf, text, xMm, yMm, opts);
		}
		return lineSpacingMm;
	}

	// Convert measurements
	const ptToPx = (pt: number) => (pt * 96) / 72;
	const mmToPx = (mm: number) => (mm * 96) / 25.4;

	const fontSizePx = Math.max(10, ptToPx(fontSizePt));
	const maxWidthPx = mmToPx(maxWidthMm);

	// Create canvas for text measurement
	const canvas = document.createElement('canvas');
	const ctx = canvas.getContext('2d');
	if (!ctx) {
		pdf.text(text, xMm, yMm);
		return lineSpacingMm;
	}

	ctx.font = `${fontWeight} ${fontSizePx}px ${fontFamily}`;

	// Break text into lines
	const lines = breakTextIntoLines(ctx, text, maxWidthPx);

	// Draw each line
	let currentY = yMm;
	for (let i = 0; i < lines.length; i++) {
		const line = lines[i];
		if (containsCJK(line)) {
			await drawTextWithCJK(pdf, line, xMm, currentY, opts);
		} else {
			pdf.text(line, xMm, currentY);
		}
		if (i < lines.length - 1) {
			currentY += lineSpacingMm;
		}
	}

	return lines.length * lineSpacingMm;
};

/**
 * Calculate the height needed for text with wrapping
 */
const calculateTextHeight = async (
	text: string,
	maxWidthMm: number,
	fontSizePt: number
): Promise<number> => {
	const fontFamily =
		"Arial, 'Microsoft YaHei', 'PingFang SC', 'Hiragino Sans GB', 'Noto Sans CJK', 'WenQuanYi Micro Hei', 'Heiti SC', 'Source Han Sans SC', sans-serif";
	const lineSpacingMm = 1.2 * (fontSizePt * 0.352778); // Default line spacing

	// Convert measurements
	const ptToPx = (pt: number) => (pt * 96) / 72;
	const mmToPx = (mm: number) => (mm * 96) / 25.4;

	const fontSizePx = Math.max(10, ptToPx(fontSizePt));
	const maxWidthPx = mmToPx(maxWidthMm);

	// Create canvas for text measurement
	const canvas = document.createElement('canvas');
	const ctx = canvas.getContext('2d');
	if (!ctx) {
		return lineSpacingMm;
	}

	ctx.font = `normal ${fontSizePx}px ${fontFamily}`;

	// Break text into lines and count them
	const lines = breakTextIntoLines(ctx, text, maxWidthPx);

	return lines.length * lineSpacingMm;
};

/**
 * Sanitize filename but keep CJK characters. Removes Windows illegal characters.
 */
export const sanitizeFilenameKeepCJK = (raw: string | undefined | null): string => {
	const base = (raw ?? 'checklist')
		.replace(/[\\/:*?"<>|]/g, '')
		.replace(/\s+/g, ' ')
		.trim()
		.replace(/[. ]+$/g, '');
	return base || 'checklist';
};

/**
 * PDF导出配置接口
 */
export interface PdfExportConfig {
	headerTitle?: string;
	headerSubtitle?: string;
	showHeader?: boolean;
	showAssignments?: boolean;
	showTaskTable?: boolean;
	filename?: string;
}

/**
 * 检查清单数据接口
 */
export interface ChecklistData {
	id?: string;
	name: string;
	description?: string;
	team?: string;
	assignments?: Array<{
		workflowId: string;
		stageId: string;
	}>;
	tasks?: Array<{
		id?: string;
		name: string;
		description?: string;
		order?: number;
		orderIndex?: number;
	}>;
}

/**
 * 工作流和阶段数据接口
 */
export interface WorkflowData {
	workflows: Array<{ id: string; name: string }>;
	stages: Array<{ id: string; name: string; workflowId?: string }>;
}

/**
 * 导出检查清单为PDF
 */
export const exportChecklistToPdf = async (
	checklist: ChecklistData,
	workflowData: WorkflowData,
	config: PdfExportConfig = {}
): Promise<void> => {
	const defaultConfig: PdfExportConfig = {
		headerTitle: 'UNIS',
		headerSubtitle: 'Warehousing Solutions',
		showHeader: true,
		showAssignments: true,
		showTaskTable: true,
		filename: `${sanitizeFilenameKeepCJK(checklist.name)}.pdf`,
		...config,
	};

	try {
		// 尝试使用jsPDF生成PDF
		await exportPdfWithJsPDF(checklist, workflowData, defaultConfig);
	} catch (jsPdfError) {
		console.error('jsPDF export failed:', jsPdfError);
		try {
			// 降级到文本文件导出
			await exportAsTextFile(checklist, workflowData, defaultConfig);
		} catch (textError) {
			console.error('Text export failed:', textError);
			try {
				// 最后的降级方案：打印
				await exportWithPrint(checklist, workflowData, defaultConfig);
			} catch (printError) {
				console.error('Print export failed:', printError);
				throw new Error('All export methods failed');
			}
		}
	}
};

/**
 * 使用jsPDF生成PDF
 */
const exportPdfWithJsPDF = async (
	checklist: ChecklistData,
	workflowData: WorkflowData,
	config: PdfExportConfig
): Promise<void> => {
	// 动态导入jsPDF库
	const jsPDFModule = await import('jspdf');

	// 尝试不同的导入方式
	let jsPDF: any;
	if ((jsPDFModule as any).jsPDF) {
		jsPDF = (jsPDFModule as any).jsPDF;
	} else if (jsPDFModule.default && (jsPDFModule.default as any).jsPDF) {
		jsPDF = (jsPDFModule.default as any).jsPDF;
	} else if (jsPDFModule.default) {
		jsPDF = jsPDFModule.default;
	} else {
		throw new Error('无法找到jsPDF构造函数');
	}

	// 创建PDF实例
	const pdf = new jsPDF({
		orientation: 'portrait',
		unit: 'mm',
		format: 'a4',
	});

	let y = 20;
	const margin = 20;
	const pageWidth = 210; // A4宽度

	// 添加头部（如果配置启用）
	if (config.showHeader) {
		pdf.setFillColor(52, 71, 103);
		pdf.rect(0, 0, pageWidth, 30, 'F');

		pdf.setTextColor(255, 255, 255);
		pdf.setFontSize(20);
		if (config.headerTitle) {
			await drawTextWithCJK(pdf, String(config.headerTitle), margin, 20, {
				fontSizePt: 20,
				color: '#FFFFFF',
				fontWeight: 'bold',
			});
		}
		pdf.setFontSize(16);
		if (config.headerSubtitle) {
			await drawTextWithCJK(pdf, String(config.headerSubtitle), margin + 60, 20, {
				fontSizePt: 16,
				color: '#FFFFFF',
			});
		}

		pdf.setTextColor(0, 0, 0);
		y = 45;
	}

	// 添加清单名称作为主标题（支持换行）
	pdf.setFontSize(18);
	const checklistName = String(checklist.name || 'Untitled');
	const titleMaxWidth = pageWidth - 2 * margin; // 标题可用宽度
	const titleHeight = await drawTextWithCJKWrapped(pdf, checklistName, margin, y, {
		fontSizePt: 18,
		color: '#000000',
		fontWeight: 'bold',
		maxWidthMm: titleMaxWidth,
		lineSpacingMm: 6,
	});
	y += Math.max(15, titleHeight + 5); // 动态调整间距

	// 添加基本信息
	pdf.setFontSize(12);
	const infoMaxWidth = pageWidth - 2 * margin; // 信息可用宽度

	if (checklist.description) {
		const description = String(checklist.description);
		const descHeight = await drawTextWithCJKWrapped(
			pdf,
			`Description: ${description}`,
			margin,
			y,
			{
				fontSizePt: 12,
				color: '#000000',
				maxWidthMm: infoMaxWidth,
				lineSpacingMm: 4,
			}
		);
		y += Math.max(8, descHeight + 2);
	}

	if (checklist.team) {
		const team = String(checklist.team);
		const teamHeight = await drawTextWithCJKWrapped(pdf, `Team: ${team}`, margin, y, {
			fontSizePt: 12,
			color: '#000000',
			maxWidthMm: infoMaxWidth,
			lineSpacingMm: 4,
		});
		y += Math.max(8, teamHeight + 2);
	}

	// 处理assignments信息（如果配置启用）
	if (config.showAssignments && checklist.assignments?.length) {
		const assignmentsText = getAssignmentsText(checklist.assignments, workflowData);
		pdf.text('Assignments:', margin, y);
		y += 6;

		if (assignmentsText && assignmentsText !== 'No assignments specified') {
			const assignmentLines = assignmentsText.split(', ');
			for (const line of assignmentLines) {
				const cleanLine = line.replace(/→/g, ' -> ');
				const assignHeight = await drawTextWithCJKWrapped(
					pdf,
					`  ${cleanLine}`,
					margin + 5,
					y,
					{
						fontSizePt: 12,
						color: '#000000',
						maxWidthMm: infoMaxWidth - 5,
						lineSpacingMm: 4,
					}
				);
				y += Math.max(5, assignHeight + 1);
			}
		} else {
			pdf.text('  No assignments specified', margin + 5, y);
			y += 5;
		}
		y += 10;
	}

	// 创建任务表格（如果配置启用）
	if (config.showTaskTable) {
		const tasks = checklist.tasks || [];
		await addTasksTable(pdf, tasks, margin, y, pageWidth);
	}

	// 保存PDF（使用 Blob + a[download]，确保中文文件名）
	const pdfBlob: Blob = pdf.output('blob');
	const pdfUrl = URL.createObjectURL(pdfBlob);
	const a = document.createElement('a');
	a.href = pdfUrl;
	a.download = config.filename!;
	a.style.display = 'none';
	document.body.appendChild(a);
	a.click();
	setTimeout(() => {
		document.body.removeChild(a);
		URL.revokeObjectURL(pdfUrl);
	}, 100);
	ElMessage.success('PDF exported successfully');
};

/**
 * 添加任务表格到PDF
 */
const addTasksTable = async (
	pdf: any,
	tasks: ChecklistData['tasks'],
	margin: number,
	startY: number,
	pageWidth: number
): Promise<void> => {
	let y = startY;
	const taskColumnWidth = pageWidth - 2 * margin - 15; // Available width for task text
	const minRowHeight = 8; // Minimum row height

	const drawTableHeader = (yPos: number) => {
		pdf.setFillColor(52, 71, 103);
		pdf.rect(margin, yPos, pageWidth - 2 * margin, 8, 'F');
		pdf.setTextColor(255, 255, 255);
		pdf.setFontSize(12);
		pdf.text('Task', margin + 20, yPos + 5.5);
		pdf.setDrawColor(255, 255, 255);
		pdf.setLineWidth(0.1);
		pdf.line(margin + 15, yPos, margin + 15, yPos + 8);
		return yPos + 8;
	};

	if (tasks && tasks.length > 0) {
		// 表格头部
		y = drawTableHeader(y);
		pdf.setTextColor(0, 0, 0);
		pdf.setFontSize(11);

		// 添加任务行
		for (let index = 0; index < tasks.length; index++) {
			const task = tasks[index];
			const taskName = String(task.name || `Task ${index + 1}`);

			// 计算文本高度
			const textHeight = await calculateTextHeight(taskName, taskColumnWidth, 12);
			const rowHeight = Math.max(minRowHeight, textHeight + 2); // Add padding

			// 检查是否需要新页面
			if (y + rowHeight > 280) {
				pdf.addPage();
				y = 20;
				y = drawTableHeader(y);
				pdf.setTextColor(0, 0, 0);
				pdf.setFontSize(11);
			}

			// 绘制表格行背景（交替颜色）
			if (index % 2 === 1) {
				pdf.setFillColor(245, 247, 250);
				pdf.rect(margin, y, pageWidth - 2 * margin, rowHeight, 'F');
			}

			// 绘制表格边框
			pdf.setDrawColor(209, 213, 219);
			pdf.setLineWidth(0.1);
			pdf.rect(margin, y, pageWidth - 2 * margin, rowHeight, 'S');
			pdf.line(margin + 15, y, margin + 15, y + rowHeight);

			// 添加序号
			pdf.setTextColor(0, 0, 0);
			pdf.setFontSize(12);
			pdf.text(`${index + 1}`, margin + 6, y + rowHeight / 2 + 2);

			// 添加任务名称（支持换行）
			await drawTextWithCJKWrapped(pdf, taskName, margin + 20, y + 4, {
				fontSizePt: 12,
				color: '#000000',
				maxWidthMm: taskColumnWidth,
				lineSpacingMm: 4,
			});

			y += rowHeight;
		}
	} else {
		// 空状态表格
		pdf.setFillColor(52, 71, 103);
		pdf.rect(margin, y, pageWidth - 2 * margin, 8, 'F');

		pdf.setTextColor(255, 255, 255);
		pdf.setFontSize(12);
		pdf.text('Task', margin + 20, y + 5.5);

		pdf.setDrawColor(255, 255, 255);
		pdf.setLineWidth(0.1);
		pdf.line(margin + 15, y, margin + 15, y + 8);

		y += 8;

		pdf.setDrawColor(209, 213, 219);
		pdf.setLineWidth(0.1);
		pdf.rect(margin, y, pageWidth - 2 * margin, 8, 'S');
		pdf.line(margin + 15, y, margin + 15, y + 8);

		pdf.setTextColor(156, 163, 175);
		pdf.setFontSize(11);
		pdf.text('No tasks available', margin + 20, y + 5.5);
	}
};

/**
 * 降级到文本文件导出
 */
const exportAsTextFile = async (
	checklist: ChecklistData,
	workflowData: WorkflowData,
	config: PdfExportConfig
): Promise<void> => {
	let content = 'UNIS Checklist Export\n\n';
	content += `Name: ${checklist.name || 'Untitled'}\n`;
	content += `Description: ${checklist.description || 'No description'}\n`;
	content += `Team: ${checklist.team || 'No team'}\n`;

	if (config.showAssignments && checklist.assignments?.length) {
		content += `Assignments: ${getAssignmentsText(checklist.assignments, workflowData)}\n`;
	}
	content += '\nTasks:\n';

	const tasks = checklist.tasks || [];
	if (tasks.length > 0) {
		tasks.forEach((task, index) => {
			content += `${index + 1}. ${task.name || `Task ${index + 1}`}\n`;
		});
	} else {
		content += 'No tasks available\n';
	}

	// 创建文本文件
	const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
	const url = URL.createObjectURL(blob);

	const link = document.createElement('a');
	link.href = url;
	link.download = config.filename?.replace('.pdf', '.txt') || 'checklist.txt';
	link.style.display = 'none';

	document.body.appendChild(link);
	link.click();

	setTimeout(() => {
		document.body.removeChild(link);
		URL.revokeObjectURL(url);
	}, 100);

	ElMessage.info('PDF generation failed, exported as text file instead');
};

/**
 * 打印方案（最后的降级）
 */
const exportWithPrint = async (
	checklist: ChecklistData,
	workflowData: WorkflowData,
	config: PdfExportConfig
): Promise<void> => {
	const printWindow = window.open('', '_blank');
	if (!printWindow) {
		throw new Error('Unable to open print window. Please check popup settings.');
	}

	const printContent = createPrintContent(checklist, workflowData, config);
	printWindow.document.write(printContent);
	printWindow.document.close();

	printWindow.onload = () => {
		setTimeout(() => {
			printWindow.print();
			printWindow.close();
		}, 500);
	};

	ElMessage.info('Print dialog opened. You can save as PDF from the print dialog.');
};

/**
 * 创建打印内容
 */
const createPrintContent = (
	checklist: ChecklistData,
	workflowData: WorkflowData,
	config: PdfExportConfig
): string => {
	const tasks = checklist.tasks || [];
	const assignmentsText =
		config.showAssignments && checklist.assignments?.length
			? getAssignmentsText(checklist.assignments, workflowData)
			: '';

	const tasksHtml =
		tasks.length > 0
			? tasks
					.map(
						(task, index) => `
			<tr>
				<td class="task-cell">${index + 1}</td>
				<td class="task-cell">${task.name || `Task ${index + 1}`}</td>
			</tr>
		`
					)
					.join('')
			: `
			<tr>
				<td class="task-cell" colspan="2" style="text-align: center; color: #9ca3af; font-style: italic;">
					No tasks available
				</td>
			</tr>
		`;

	return `
		<!DOCTYPE html>
		<html>
		<head>
			<meta charset="utf-8">
			<title>${checklist.name} - Checklist</title>
			<style>
				@page {
					size: A4;
					margin: 0;
				}
				
				* {
					margin: 0;
					padding: 0;
					box-sizing: border-box;
				}
				
				body {
					font-family: Arial, sans-serif;
					background: white;
					color: #333;
					line-height: 1.4;
				}
				
				.pdf-container {
					width: 210mm;
					min-height: 297mm;
					padding: 15mm;
					background: white;
				}
				
				.header {
					background: #3b4d66;
					color: white;
					padding: 15px 20px;
					margin: -15mm -15mm 20px -15mm;
					display: flex;
					justify-content: space-between;
					align-items: center;
				}
				
				.header-left {
					font-size: 24px;
					font-weight: bold;
				}
				
				.header-right {
					font-size: 18px;
				}
				
				.title {
					font-size: 24px;
					color: #1f2937;
					margin: 0 0 20px 0;
					font-weight: bold;
				}
				
				.info-section {
					margin-bottom: 25px;
				}
				
				.info-item {
					margin: 6px 0;
					font-size: 14px;
					color: #374151;
				}
				
				.info-label {
					font-weight: bold;
				}
				
				.tasks-table {
					width: 100%;
					border-collapse: collapse;
					margin-top: 15px;
					border: 1px solid #e5e7eb;
				}
				
				.table-header {
					background: #3b4d66;
					color: white;
				}

				.header-cell {
					padding: 10px 8px;
					text-align: left;
					font-size: 14px;
					font-weight: bold;
				}

				.header-cell:first-child {
					width: 50px;
				}

				.task-cell {
					padding: 8px;
					border-bottom: 1px solid #e5e7eb;
					font-size: 12px;
					color: #374151;
				}

				@media print {
					body {
						-webkit-print-color-adjust: exact;
						print-color-adjust: exact;
					}

					.pdf-container {
						margin: 0;
						padding: 15mm;
					}
				}
			</style>
		</head>
		<body>
			<div class="pdf-container">
				${
					config.showHeader
						? `
				<div class="header">
					<div class="header-left">${config.headerTitle || ''}</div>
					<div class="header-right">${config.headerSubtitle || ''}</div>
				</div>
				`
						: ''
				}

				<h1 class="title">${checklist.name}</h1>

				<div class="info-section">
					${
						checklist.description
							? `
					<div class="info-item">
						<span class="info-label">Description:</span> ${checklist.description}
					</div>
					`
							: ''
					}
					${
						checklist.team
							? `
					<div class="info-item">
						<span class="info-label">Team:</span> ${checklist.team}
					</div>
					`
							: ''
					}
					${
						assignmentsText
							? `
					<div class="info-item">
						<span class="info-label">Assignments:</span> ${assignmentsText}
					</div>
					`
							: ''
					}
				</div>

				${
					config.showTaskTable
						? `
				<table class="tasks-table">
					<thead class="table-header">
						<tr>
							<th class="header-cell" style="width: 50px;">No.</th>
							<th class="header-cell">Task</th>
						</tr>
					</thead>
					<tbody>
						${tasksHtml}
					</tbody>
				</table>
				`
						: ''
				}
			</div>
		</body>
		</html>
	`;
};

/**
 * 获取assignments文本
 */
const getAssignmentsText = (
	assignments: ChecklistData['assignments'],
	workflowData: WorkflowData
): string => {
	if (!assignments || assignments.length === 0) {
		return 'No assignments specified';
	}

	const assignmentTexts = assignments.map((assignment) => {
		const workflowName = getWorkflowNameById(assignment.workflowId, workflowData.workflows);
		const stageName = getStageNameById(assignment.stageId, workflowData.stages);
		return `${workflowName} → ${stageName}`;
	});

	return assignmentTexts.join(', ');
};

/**
 * 根据ID获取工作流名称
 */
const getWorkflowNameById = (workflowId: string, workflows: WorkflowData['workflows']): string => {
	if (!workflowId) return '';
	const workflow = workflows.find((w) => w.id.toString() === workflowId.toString());
	return workflow ? workflow.name : '--';
};

/**
 * 根据ID获取阶段名称
 */
const getStageNameById = (stageId: string, stages: WorkflowData['stages']): string => {
	if (!stageId) return '';
	const stage = stages.find((s) => s.id.toString() === stageId.toString());
	return stage ? stage.name : '--';
};
