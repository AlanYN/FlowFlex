using System.Text.Json;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Domain.Shared.Enums.Action;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using FlowFlex.Application.Contracts;
using Microsoft.AspNetCore.Http;

namespace FlowFlex.Application.Services.Action.Executors
{
    /// <summary>
    /// HTTP API action executor - makes HTTP API calls
    /// </summary>
    public class HttpApiActionExecutor : IActionExecutor
    {
        private readonly ILogger<HttpApiActionExecutor> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IFileStorageService _fileStorageService;
        private readonly JsonSerializerOptions _jsonOptions;

        public HttpApiActionExecutor(
            ILogger<HttpApiActionExecutor> logger,
            IHttpClientFactory httpClientFactory,
            IFileStorageService fileStorageService)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _fileStorageService = fileStorageService;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public ActionTypeEnum ActionType => ActionTypeEnum.HttpApi;

        public async Task<object> ExecuteAsync(string config, object triggerContext)
        {
            _logger.LogInformation("Executing HTTP API action with config: {Config}", config);

            try
            {
                var configData = ParseConfig(config);
                if (configData == null)
                {
                    return CreateErrorResult("Invalid configuration format");
                }

                if (string.IsNullOrWhiteSpace(configData.Url))
                {
                    return CreateErrorResult("URL is required");
                }

                var result = await ExecuteHttpRequestAsync(configData, triggerContext);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing HTTP API action");
                return CreateErrorResult($"Execution failed: {ex.Message}");
            }
        }

        private HttpApiConfigDto? ParseConfig(string config)
        {
            try
            {
                return JsonSerializer.Deserialize<HttpApiConfigDto>(config, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse HTTP API configuration");
                return null;
            }
        }

        private async Task<object> ExecuteHttpRequestAsync(HttpApiConfigDto config, object triggerContext)
        {
            using var client = _httpClientFactory.CreateClient("HttpApiExecutor");

            // Set timeout based on configuration
            if (config.Timeout > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(config.Timeout);
            }

            // Replace placeholders in URL
            var processedUrl = ReplacePlaceholders(config.Url, triggerContext);
            var request = new HttpRequestMessage(GetHttpMethod(config.Method), processedUrl);

            // Separate headers into request headers and content headers
            var requestHeaders = new Dictionary<string, string>();
            var contentHeaders = new Dictionary<string, string>();

            foreach (var header in config.Headers)
            {
                var key = header.Key?.Trim();
                var value = header.Value?.Trim();
                
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                    continue;

                var processedValue = ReplacePlaceholders(value, triggerContext);

                // Content headers should be added to HttpContent, not HttpRequestMessage
                if (IsContentHeader(key))
                {
                    contentHeaders[key] = processedValue;
                }
                else
                {
                    requestHeaders[key] = processedValue;
                }
            }

            // Add request headers
            foreach (var header in requestHeaders)
            {
                if (!request.Headers.Contains(header.Key))
                {
                    try
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Failed to add request header {HeaderName}: {Error}", header.Key, ex.Message);
                    }
                }
            }

            // Add body with placeholder replacement
            if (!string.IsNullOrEmpty(config.Body))
            {
                var processedBody = ReplacePlaceholders(config.Body, triggerContext);
                
                // Determine content type from config or default to application/json
                var contentType = contentHeaders.GetValueOrDefault("Content-Type", "application/json");
                request.Content = new StringContent(processedBody, System.Text.Encoding.UTF8, contentType);
                
                // Remove Content-Type from contentHeaders since it's already set in StringContent
                contentHeaders.Remove("Content-Type");
                
                // Add remaining content headers
                foreach (var header in contentHeaders)
                {
                    try
                    {
                        request.Content.Headers.Add(header.Key, header.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Failed to add content header {HeaderName}: {Error}", header.Key, ex.Message);
                    }
                }
            }
            else if (contentHeaders.Count > 0)
            {
                // If there are content headers but no body, create empty content
                request.Content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "text/plain");
                
                foreach (var header in contentHeaders)
                {
                    try
                    {
                        request.Content.Headers.Add(header.Key, header.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Failed to add content header {HeaderName}: {Error}", header.Key, ex.Message);
                    }
                }
            }

            try
            {
                var response = await client.SendAsync(request);

                _logger.LogInformation(
                    "HTTP API request completed: {Method} {Url} - Status: {StatusCode}",
                    config.Method, processedUrl, (int)response.StatusCode);

                // Check if auto download is enabled or if content should be auto-downloaded
                var shouldDownload = config.AutoDownloadFile || ShouldAutoDownloadContent(response);
                
                if (shouldDownload && response.IsSuccessStatusCode)
                {
                    return await HandleFileDownloadAsync(response, config, processedUrl);
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return CreateSuccessResult(response, content);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex,
                    "HTTP API request failed: {Method} {Url}",
                    config.Method, processedUrl);

                return CreateErrorResult($"HTTP request failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Replace placeholders like {{ stageId }} with values from triggerContext
        /// </summary>
        /// <param name="input">Input string containing placeholders</param>
        /// <param name="triggerContext">Context object containing values</param>
        /// <returns>String with placeholders replaced</returns>
        private string ReplacePlaceholders(string input, object triggerContext)
        {
            if (string.IsNullOrEmpty(input) || triggerContext == null)
                return input;

            try
            {
                // Use regex to find all placeholders like {{stageId}}
                var placeholderPattern = @"\{\{(\w+)\}\}";
                var result = Regex.Replace(input, placeholderPattern, match =>
                {
                    var placeholderName = match.Groups[1].Value.Trim();
                    var value = ExtractValueFromContext(triggerContext, placeholderName);
                    return value?.ToString() ?? match.Value; // Return original placeholder if value not found
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to replace placeholders in string: {Input}", input);
                return input;
            }
        }

        /// <summary>
        /// Extract value from triggerContext by property name
        /// </summary>
        /// <param name="triggerContext">Context object</param>
        /// <param name="propertyName">Property name to extract</param>
        /// <returns>Property value or null if not found</returns>
        private object? ExtractValueFromContext(object triggerContext, string propertyName)
        {
            if (triggerContext == null)
                return null;

            try
            {
                object parsedContext = triggerContext;
                if (triggerContext is string jsonString && !string.IsNullOrWhiteSpace(jsonString))
                {
                    string trimmed = jsonString.Trim();
                    if ((trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
                        (trimmed.StartsWith("[") && trimmed.EndsWith("]")))
                    {
                        try
                        {
                            parsedContext = JToken.Parse(jsonString);
                        }
                        catch (JsonException)
                        {
                            // If parsing fails, continue with original triggerContext
                            parsedContext = triggerContext;
                        }
                    }
                }

                // Handle JToken/JObject
                if (parsedContext is JToken jToken)
                {
                    var token = jToken[propertyName];
                    if (token != null && token.Type != JTokenType.Null)
                    {
                        return token.ToObject<object>();
                    }
                    return null;
                }

                // Handle JObject
                if (parsedContext is JObject jObject)
                {
                    var token = jObject[propertyName];
                    if (token != null && token.Type != JTokenType.Null)
                    {
                        return token.ToObject<object>();
                    }
                    return null;
                }

                // Handle IDictionary
                if (parsedContext is System.Collections.IDictionary dict)
                {
                    if (dict.Contains(propertyName))
                    {
                        return dict[propertyName];
                    }
                    return null;
                }

                // Handle generic Dictionary
                if (parsedContext is IDictionary<string, object> genericDict)
                {
                    if (genericDict.TryGetValue(propertyName, out var value))
                    {
                        return value;
                    }
                    return null;
                }

                // Handle object properties via reflection
                var property = parsedContext.GetType().GetProperty(propertyName);
                if (property != null)
                {
                    return property.GetValue(parsedContext);
                }

                // Try case-insensitive property search
                property = parsedContext.GetType().GetProperties()
                    .FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
                if (property != null)
                {
                    return property.GetValue(parsedContext);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to extract property '{PropertyName}' from triggerContext", propertyName);
                return null;
            }
        }

        private static HttpMethod GetHttpMethod(string method)
        {
            return method.ToUpper() switch
            {
                "GET" => HttpMethod.Get,
                "POST" => HttpMethod.Post,
                "PUT" => HttpMethod.Put,
                "DELETE" => HttpMethod.Delete,
                "PATCH" => HttpMethod.Patch,
                _ => HttpMethod.Get
            };
        }

        private object CreateSuccessResult(HttpResponseMessage response, string content)
        {
            // 检查内容是否为二进制数据或包含不可打印字符
            var processedContent = ProcessResponseContent(content, response.Content.Headers.ContentType?.MediaType);
            
            return new
            {
                success = response.IsSuccessStatusCode,
                statusCode = (int)response.StatusCode,
                response = processedContent,
                headers = response.Headers.ToDictionary(h => h.Key, h => h.Value),
                timestamp = DateTimeOffset.UtcNow
            };
        }

        /// <summary>
        /// 处理响应内容，过滤二进制数据或不可打印字符
        /// </summary>
        private string ProcessResponseContent(string content, string? contentType)
        {
            try
            {
                // 如果内容为空，直接返回
                if (string.IsNullOrEmpty(content))
                {
                    return content;
                }

                // 检查是否为二进制内容类型
                if (!string.IsNullOrEmpty(contentType))
                {
                    var lowerContentType = contentType.ToLower();
                    if (lowerContentType.StartsWith("image/") || 
                        lowerContentType.StartsWith("video/") || 
                        lowerContentType.StartsWith("audio/") ||
                        lowerContentType.Contains("octet-stream"))
                    {
                        return $"[Binary content: {contentType}, size: {content.Length} bytes]";
                    }
                }

                // 检查内容是否包含大量不可打印字符（可能是二进制数据）
                int unprintableCount = 0;
                int totalChars = Math.Min(content.Length, 1000); // 只检查前1000个字符

                for (int i = 0; i < totalChars; i++)
                {
                    char c = content[i];
                    // 检查是否为控制字符或不可打印字符（排除常见的换行符等）
                    if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t')
                    {
                        unprintableCount++;
                    }
                }

                // 如果不可打印字符超过10%，认为是二进制内容
                if ((double)unprintableCount / totalChars > 0.1)
                {
                    return $"[Binary content detected, size: {content.Length} bytes]";
                }

                // 如果内容过长（超过10KB），截断以避免数据库性能问题
                if (content.Length > 10240)
                {
                    return content.Substring(0, 10240) + $"... [Truncated, total size: {content.Length} bytes]";
                }

                return content;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing response content");
                return $"[Error processing content: {ex.Message}]";
            }
        }

        private object CreateErrorResult(string message)
        {
            return new
            {
                success = false,
                error = message,
                timestamp = DateTimeOffset.UtcNow
            };
        }

        #region File Download Methods

        /// <summary>
        /// Determine if content should be automatically downloaded based on content type and size
        /// </summary>
        private bool ShouldAutoDownloadContent(HttpResponseMessage response)
        {
            try
            {
                var contentType = response.Content.Headers.ContentType?.MediaType?.ToLower();
                var contentLength = response.Content.Headers.ContentLength;

                // Auto-download binary content types
                if (!string.IsNullOrEmpty(contentType))
                {
                    var binaryContentTypes = new[]
                    {
                        "application/pdf",
                        "application/zip",
                        "application/x-rar-compressed",
                        "application/vnd.ms-excel",
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "application/msword",
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                        "application/vnd.ms-powerpoint",
                        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                        "application/octet-stream",
                        "image/jpeg",
                        "image/png",
                        "image/gif",
                        "image/bmp",
                        "image/tiff",
                        "audio/mpeg",
                        "audio/wav",
                        "video/mp4",
                        "video/avi",
                        "video/mov"
                    };

                    if (binaryContentTypes.Contains(contentType))
                    {
                        _logger.LogInformation("Auto-downloading binary content: ContentType={ContentType}, Size={Size}", 
                            contentType, contentLength?.ToString() ?? "unknown");
                        return true;
                    }
                }

                // Auto-download large content (likely binary)
                if (contentLength.HasValue && contentLength.Value > 1048576) // > 1MB
                {
                    _logger.LogInformation("Auto-downloading large content: Size={Size} bytes", contentLength.Value);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error determining if content should be auto-downloaded");
                return false;
            }
        }

        /// <summary>
        /// Handle file download from HTTP response
        /// </summary>
        private async Task<object> HandleFileDownloadAsync(HttpResponseMessage response, HttpApiConfigDto config, string requestUrl)
        {
            try
            {
                // Check content length
                var contentLength = response.Content.Headers.ContentLength;
                var maxDownloadSize = config.MaxDownloadSize > 0 ? config.MaxDownloadSize : 104857600; // Default 100MB
                if (contentLength.HasValue && contentLength.Value > maxDownloadSize)
                {
                    _logger.LogWarning("File size {Size} bytes exceeds maximum download size {MaxSize} bytes", 
                        contentLength.Value, maxDownloadSize);
                    
                    return CreateErrorResult($"File size {contentLength.Value} bytes exceeds maximum download size {maxDownloadSize} bytes");
                }

                // Get content type
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                
                // Determine file name
                var fileName = DetermineFileName(response, config, requestUrl);
                
                _logger.LogInformation("Starting file download: {FileName}, ContentType: {ContentType}, Size: {Size}", 
                    fileName, contentType, contentLength?.ToString() ?? "unknown");

                // Read response content as byte array to avoid stream disposal issues
                var responseBytes = await response.Content.ReadAsByteArrayAsync();
                var totalBytesRead = responseBytes.Length;
                
                // Check size limit
                if (totalBytesRead > maxDownloadSize)
                {
                    _logger.LogWarning("Download exceeded maximum size limit: {Size} bytes", totalBytesRead);
                    return CreateErrorResult($"Download exceeded maximum size limit {maxDownloadSize} bytes");
                }

                // Save file using IFileStorageService
                var fileCategory = string.IsNullOrWhiteSpace(config.FileCategory) ? "HttpApiDownload" : config.FileCategory;
                var fileResult = await SaveDownloadedFileAsync(responseBytes, fileName, contentType, fileCategory);
                
                if (!fileResult.Success)
                {
                    _logger.LogError("Failed to save downloaded file: {Error}", fileResult.ErrorMessage);
                    return CreateErrorResult($"Failed to save file: {fileResult.ErrorMessage}");
                }

                _logger.LogInformation("File downloaded and saved successfully: {FilePath}, AccessUrl: {AccessUrl}", 
                    fileResult.FilePath, fileResult.AccessUrl);

                // Return success result with file information
                return CreateFileDownloadSuccessResult(response, fileResult, totalBytesRead, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file download");
                return CreateErrorResult($"File download failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Determine file name from response headers or config
        /// </summary>
        private string DetermineFileName(HttpResponseMessage response, HttpApiConfigDto config, string requestUrl)
        {
            // Use custom file name if provided
            if (!string.IsNullOrWhiteSpace(config.CustomFileName))
            {
                return config.CustomFileName;
            }

            // Try to get file name from Content-Disposition header
            var contentDisposition = response.Content.Headers.ContentDisposition;
            if (contentDisposition?.FileName != null)
            {
                var headerFileName = contentDisposition.FileName.Trim('"');
                if (!string.IsNullOrWhiteSpace(headerFileName))
                {
                    return headerFileName;
                }
            }

            // Try to extract file name from URL
            try
            {
                var uri = new Uri(requestUrl);
                var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (pathSegments.Length > 0)
                {
                    var lastSegment = pathSegments.Last();
                    if (lastSegment.Contains('.'))
                    {
                        return lastSegment;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to extract file name from URL: {Url}", requestUrl);
            }

            // Generate file name based on content type
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var extension = GetFileExtensionFromContentType(contentType);
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");
            
            return $"download_{timestamp}{extension}";
        }

        /// <summary>
        /// Get file extension from content type
        /// </summary>
        private string GetFileExtensionFromContentType(string contentType)
        {
            return contentType.ToLower() switch
            {
                "application/pdf" => ".pdf",
                "application/json" => ".json",
                "application/xml" => ".xml",
                "text/plain" => ".txt",
                "text/html" => ".html",
                "text/css" => ".css",
                "text/javascript" => ".js",
                "application/javascript" => ".js",
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/svg+xml" => ".svg",
                "application/zip" => ".zip",
                "application/x-rar-compressed" => ".rar",
                "application/vnd.ms-excel" => ".xls",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
                "application/msword" => ".doc",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                _ => ".bin"
            };
        }

        /// <summary>
        /// Save downloaded file using IFileStorageService
        /// </summary>
        private async Task<FileStorageResult> SaveDownloadedFileAsync(byte[] fileBytes, string fileName, string contentType, string category)
        {
            try
            {
                // Create IFormFile from byte array
                var formFile = new FormFileFromBytes(fileBytes, fileName, contentType);
                
                // Save using file storage service
                return await _fileStorageService.SaveFileAsync(formFile, category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving downloaded file: {FileName}", fileName);
                return new FileStorageResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Create success result for file download
        /// </summary>
        private object CreateFileDownloadSuccessResult(HttpResponseMessage response, FileStorageResult fileResult, long downloadedBytes, string contentType)
        {
            return new
            {
                success = response.IsSuccessStatusCode,
                statusCode = (int)response.StatusCode,
                fileDownload = new
                {
                    downloaded = true,
                    fileName = fileResult.FileName,
                    originalFileName = fileResult.OriginalFileName,
                    filePath = fileResult.FilePath,
                    accessUrl = fileResult.AccessUrl,
                    fileSize = downloadedBytes,
                    contentType = contentType,
                    fileHash = fileResult.FileHash
                },
                response = $"[File downloaded and saved: {fileResult.FileName}, size: {downloadedBytes} bytes]",
                headers = response.Headers.ToDictionary(h => h.Key, h => h.Value),
                timestamp = DateTimeOffset.UtcNow
            };
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// IFormFile implementation from byte array for file storage service
        /// </summary>
        private class FormFileFromBytes : IFormFile
        {
            private readonly byte[] _fileBytes;
            private readonly string _fileName;
            private readonly string _contentType;

            public FormFileFromBytes(byte[] fileBytes, string fileName, string contentType)
            {
                _fileBytes = fileBytes ?? throw new ArgumentNullException(nameof(fileBytes));
                _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
                _contentType = contentType ?? "application/octet-stream";
            }

            public string ContentType => _contentType;
            public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{_fileName}\"";
            public IHeaderDictionary Headers => new HeaderDictionary();
            public long Length => _fileBytes.Length;
            public string Name => "file";
            public string FileName => _fileName;

            public void CopyTo(Stream target)
            {
                target.Write(_fileBytes, 0, _fileBytes.Length);
            }

            public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
            {
                await target.WriteAsync(_fileBytes, 0, _fileBytes.Length, cancellationToken);
            }

            public Stream OpenReadStream()
            {
                return new MemoryStream(_fileBytes, false);
            }
        }

        /// <summary>
        /// Determines if a header should be added to HttpContent instead of HttpRequestMessage
        /// </summary>
        private static bool IsContentHeader(string headerName)
        {
            if (string.IsNullOrEmpty(headerName))
                return false;

            var name = headerName.Trim();
            
            // Content headers that belong to HttpContent
            return name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Content-Encoding", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Content-Language", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Content-Location", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Content-MD5", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Content-Range", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Content-Disposition", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Expires", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Last-Modified", StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}