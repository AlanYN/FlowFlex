# Requirements Document

## Introduction

本文档定义了 AI Workflow Generator 组件中图片识别和 OCR 功能的需求。当前系统支持 TXT、PDF、DOCX、XLSX、CSV、MD、JSON 格式的文件，但存在以下限制：
1. PDF 处理使用 pdf.js 库只能提取文本层内容，无法处理扫描版 PDF 或 PDF 中嵌入的图片
2. 不支持直接上传图片文件（JPG、PNG 等）进行分析

本功能旨在：
1. 增强 PDF 处理能力，支持 OCR 识别扫描版 PDF 和 PDF 中的图片内容
2. 新增图片文件（JPG、PNG、GIF、BMP、WebP）的直接上传和分析支持
3. 提供 Tesseract.js 本地 OCR 和 Vision API 两种图像识别方案

## Glossary

- **PDF**: Portable Document Format，便携式文档格式
- **OCR**: Optical Character Recognition，光学字符识别，将图片中的文字转换为可编辑文本的技术
- **pdf.js**: Mozilla 开发的 JavaScript PDF 渲染库
- **Tesseract.js**: 基于 Tesseract OCR 引擎的 JavaScript 库，支持浏览器端 OCR，免费开源无需 API Key
- **AIFileAnalyzer**: 系统中负责文件分析的 Vue 组件
- **Scanned PDF**: 扫描版 PDF，内容以图片形式存储而非文本层
- **Image-based PDF**: 包含嵌入图片的 PDF 文档
- **Vision API**: 具有图像理解能力的 AI 模型 API（如 GPT-4o Vision、Claude 3 Vision），复用现有 AI 模型配置
- **Image File**: 图片文件，包括 JPG、JPEG、PNG、GIF、BMP、WebP 等常见格式

## Requirements

### Requirement 1

**User Story:** As a user, I want to upload image files directly (JPG, PNG, GIF, BMP, WebP), so that the AI can analyze the visual content and extract information for workflow generation.

#### Acceptance Criteria

1. WHEN a user uploads an image file THEN the AIFileAnalyzer component SHALL accept JPG, JPEG, PNG, GIF, BMP, and WebP formats
2. WHEN an image file is uploaded THEN the system SHALL display a preview of the image in the processing dialog
3. WHEN processing an image file THEN the system SHALL offer two analysis options: Tesseract.js OCR and Vision API
4. WHEN Tesseract.js OCR is selected THEN the system SHALL extract text from the image locally without requiring API calls
5. WHEN Vision API is selected and the configured AI model supports vision THEN the system SHALL send the image to the AI for comprehensive analysis

### Requirement 2

**User Story:** As a user, I want to upload scanned PDFs or PDFs containing images, so that the AI can analyze the visual content and generate workflows based on the extracted information.

#### Acceptance Criteria

1. WHEN a user uploads a PDF file THEN the AIFileAnalyzer component SHALL detect whether the PDF contains extractable text or is image-based
2. WHEN the PDF is detected as image-based or contains minimal text THEN the system SHALL automatically trigger OCR processing
3. WHEN OCR processing is triggered THEN the system SHALL extract images from each PDF page for text recognition
4. WHEN images are extracted from PDF pages THEN the system SHALL perform OCR using Tesseract.js to convert images to text
5. WHEN OCR completes successfully THEN the system SHALL combine the extracted text with any existing text layer content

### Requirement 3

**User Story:** As a user, I want to see the progress of image and PDF processing, so that I understand what the system is doing and how long it might take.

#### Acceptance Criteria

1. WHEN OCR processing begins THEN the system SHALL display a progress indicator showing the current processing stage
2. WHEN processing multiple PDF pages THEN the system SHALL display the current page number and total page count
3. WHEN OCR processing encounters an error THEN the system SHALL display a clear error message describing the issue
4. WHEN OCR processing completes THEN the system SHALL display a summary of extracted content including character count and confidence level

### Requirement 4

**User Story:** As a user, I want the system to handle mixed PDFs (containing both text and images), so that all content is properly extracted regardless of format.

#### Acceptance Criteria

1. WHEN a PDF contains both text layers and embedded images THEN the system SHALL extract content from both sources
2. WHEN merging text and OCR content THEN the system SHALL maintain the logical reading order of the document
3. WHEN duplicate content exists between text layer and OCR results THEN the system SHALL deduplicate the content intelligently

### Requirement 5

**User Story:** As a user, I want the option to use Vision AI for complex image analysis, so that diagrams, flowcharts, and handwritten content can be better understood.

#### Acceptance Criteria

1. WHEN the AI model supports vision capabilities THEN the system SHALL offer an option to use Vision API for image analysis
2. WHEN Vision API is selected THEN the system SHALL send PDF page images or uploaded images directly to the Vision-capable AI model
3. WHEN Vision API analysis completes THEN the system SHALL combine the AI interpretation with any text content
4. WHEN Vision API is unavailable or fails THEN the system SHALL fall back to Tesseract.js OCR

### Requirement 6

**User Story:** As a developer, I want the OCR functionality to be modular and configurable, so that it can be easily maintained and extended.

#### Acceptance Criteria

1. WHEN implementing OCR functionality THEN the system SHALL encapsulate OCR logic in a separate utility module
2. WHEN configuring OCR THEN the system SHALL support language selection for better recognition accuracy
3. WHEN OCR processing is resource-intensive THEN the system SHALL implement web worker-based processing to avoid blocking the UI
4. WHEN the OCR library loads THEN the system SHALL use lazy loading to minimize initial bundle size

### Requirement 7

**User Story:** As a user, I want the system to handle large files efficiently, so that processing does not cause browser performance issues.

#### Acceptance Criteria

1. WHEN processing large PDF files THEN the system SHALL implement page-by-page processing to manage memory usage
2. WHEN a PDF exceeds 50 pages THEN the system SHALL warn the user about potential processing time
3. WHEN processing is taking too long THEN the system SHALL provide an option to cancel the operation
4. WHEN memory usage becomes critical THEN the system SHALL release processed page resources before continuing

### Requirement 8

**User Story:** As a user, I want to preview the extracted content before sending to AI, so that I can verify the OCR accuracy and make corrections if needed.

#### Acceptance Criteria

1. WHEN OCR extraction completes THEN the system SHALL display a preview of the extracted text
2. WHEN displaying the preview THEN the system SHALL indicate which portions came from OCR versus text layer
3. WHEN the user identifies OCR errors THEN the system SHALL allow manual editing of the extracted text before AI analysis

### Requirement 9

**User Story:** As a user, I want the file upload tooltip to reflect all supported file types, so that I know what files I can upload.

#### Acceptance Criteria

1. WHEN the user hovers over the file upload button THEN the tooltip SHALL display "Supported: TXT, PDF, DOCX, XLSX, CSV, MD, JSON, JPG, PNG, GIF, BMP, WebP"
2. WHEN the user attempts to upload an unsupported file type THEN the system SHALL display a clear error message listing supported formats
