using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts;
using FlowFlex.Application.Contracts.Dtos.OW.AdobeSign;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Adobe Sign e-signature service implementation (OW-731).
    ///
    /// Integrates with Adobe Sign REST API v6 to:
    ///   - Upload PDFs as transient documents
    ///   - Create multi-party signing agreements
    ///   - Handle Webhook callbacks to update status and archive completed files
    ///
    /// API credentials are read from appsettings.json "AdobeSign" section (placeholders until real keys arrive).
    /// </summary>
    public class AdobeSignService : IAdobeSignService
    {
        private readonly IAdobeSignAgreementRepository _agreementRepository;
        private readonly IOnboardingFileRepository _onboardingFileRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IAttachmentService _attachmentService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ISqlSugarClient _db;
        private readonly ILogger<AdobeSignService> _logger;
        private readonly UserContext _userContext;

        // Config section key
        private const string ConfigSection = "AdobeSign";

        public AdobeSignService(
            IAdobeSignAgreementRepository agreementRepository,
            IOnboardingFileRepository onboardingFileRepository,
            IFileStorageService fileStorageService,
            IAttachmentService attachmentService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ISqlSugarClient db,
            ILogger<AdobeSignService> logger,
            UserContext userContext)
        {
            _agreementRepository = agreementRepository;
            _onboardingFileRepository = onboardingFileRepository;
            _fileStorageService = fileStorageService;
            _attachmentService = attachmentService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _db = db;
            _logger = logger;
            _userContext = userContext;
        }

        // ------------------------------------------------------------------ //
        //  RequestSignatureAsync
        // ------------------------------------------------------------------ //

        /// <inheritdoc />
        public async Task<AdobeSignAgreementOutputDto> RequestSignatureAsync(RequestAdobeSignInputDto input)
        {
            // Validate source file exists
            var sourceFile = await _onboardingFileRepository.GetByIdAsync(input.SourceFileId);
            if (sourceFile == null || !sourceFile.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, $"File {input.SourceFileId} not found");
            }

            // Check no active agreement already exists for this file
            var existing = await _agreementRepository.GetBySourceFileIdAsync(input.SourceFileId);
            if (existing != null && existing.Status == "Awaiting")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"An active signing request already exists for file {input.SourceFileId}");
            }

            // Step 1: Read PDF bytes using the same pattern as OnboardingFileService.DownloadFileAsync
            // - AttachmentId == 0: special files (e.g. signed docs), use StoragePath/AccessUrl directly
            // - AttachmentId != 0: normal uploaded files, use IAttachmentService to get the correct relative path
            byte[] pdfBytes;
            if (sourceFile.AttachmentId == 0)
            {
                var filePath = sourceFile.StoragePath ?? sourceFile.AccessUrl;
                if (string.IsNullOrEmpty(filePath))
                    throw new CRMException(ErrorCodeEnum.DataNotFound,
                        $"File {input.SourceFileId} has no storage path or access URL");

                var (stream, _, _) = await _fileStorageService.GetFileAsync(filePath);
                using var ms0 = new MemoryStream();
                try { await stream.CopyToAsync(ms0); }
                finally { await stream.DisposeAsync(); }
                pdfBytes = ms0.ToArray();
            }
            else
            {
                var (attachStream, _) = await _attachmentService.GetAttachmentAsync(sourceFile.AttachmentId);
                using var ms1 = new MemoryStream();
                try { await attachStream.CopyToAsync(ms1); }
                finally { await attachStream.DisposeAsync(); }
                pdfBytes = ms1.ToArray();
            }

            // Step 2: Upload to Adobe Sign as transient document
            var transientDocumentId = await UploadTransientDocumentAsync(pdfBytes, sourceFile.OriginalFileName);

            // Step 3: Build Agreement payload and POST to Adobe Sign
            var adobeAgreementId = await CreateAgreementAsync(transientDocumentId, input, sourceFile.OriginalFileName);

            // Step 4: Persist agreement record
            var signersJson = JsonSerializer.Serialize(input.Signers);
            var agreement = new AdobeSignAgreement
            {
                AgreementId = adobeAgreementId,
                OnboardingId = input.OnboardingId,
                StageId = input.StageId,
                SourceFileId = input.SourceFileId,
                Status = "Awaiting",
                Signers = signersJson,
                SigningOrder = input.SigningOrder,
                ExpirationDays = input.ExpirationDays,
                Message = input.Message,
                RequestedBy = long.TryParse(_userContext?.UserId, out var uid) ? uid : 0,
            };
            agreement.InitCreateInfo(_userContext);
            await _db.Insertable(agreement).ExecuteCommandAsync();

            _logger.LogInformation(
                "[AdobeSign] Agreement created. WFE ID={AgreementDbId}, Adobe ID={AdobeId}, File={FileId}",
                agreement.Id, adobeAgreementId, input.SourceFileId);

            return MapToOutputDto(agreement);
        }

        // ------------------------------------------------------------------ //
        //  GetAgreementAsync
        // ------------------------------------------------------------------ //

        /// <inheritdoc />
        public async Task<AdobeSignAgreementOutputDto> GetAgreementAsync(long id)
        {
            var agreement = await _agreementRepository.GetByIdAsync(id);
            if (agreement == null || !agreement.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, $"Agreement {id} not found");
            }

            return MapToOutputDto(agreement);
        }

        // ------------------------------------------------------------------ //
        //  GetAgreementByFileIdAsync
        // ------------------------------------------------------------------ //

        /// <inheritdoc />
        public async Task<AdobeSignAgreementOutputDto?> GetAgreementByFileIdAsync(long sourceFileId)
        {
            var agreement = await _agreementRepository.GetBySourceFileIdAsync(sourceFileId);
            return agreement == null ? null : MapToOutputDto(agreement);
        }

        // ------------------------------------------------------------------ //
        //  SendReminderAsync
        // ------------------------------------------------------------------ //

        /// <inheritdoc />
        public async Task<bool> SendReminderAsync(long id, List<string> signerEmails)
        {
            var agreement = await _agreementRepository.GetByIdAsync(id);
            if (agreement == null || !agreement.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, $"Agreement {id} not found");
            }

            if (agreement.Status != "Awaiting")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Cannot send reminder for agreement with status '{agreement.Status}'");
            }

            var client = CreateAdobeSignClient();
            var payload = new
            {
                agreementId = agreement.AgreementId,
                recipientEmailList = signerEmails,
                message = "Reminder: Please sign the document at your earliest convenience."
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"agreements/{agreement.AgreementId}/reminders", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("[AdobeSign] SendReminder failed. AgreementId={Id}, Status={Status}, Body={Body}",
                    agreement.AgreementId, response.StatusCode, error);
                throw new CRMException(ErrorCodeEnum.SystemError,
                    $"Failed to send reminder via Adobe Sign (HTTP {(int)response.StatusCode})");
            }

            _logger.LogInformation("[AdobeSign] Reminder sent. AgreementId={Id}, Recipients={Count}",
                agreement.AgreementId, signerEmails.Count);
            return true;
        }

        // ------------------------------------------------------------------ //
        //  RecallAgreementAsync
        // ------------------------------------------------------------------ //

        /// <inheritdoc />
        public async Task<bool> RecallAgreementAsync(long id)
        {
            var agreement = await _agreementRepository.GetByIdAsync(id);
            if (agreement == null || !agreement.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, $"Agreement {id} not found");
            }

            if (agreement.Status != "Awaiting")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"Cannot recall agreement with status '{agreement.Status}'");
            }

            var client = CreateAdobeSignClient();
            var payload = new { state = "CANCELLED", comment = "Recalled by WFE user" };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"agreements/{agreement.AgreementId}/state", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("[AdobeSign] RecallAgreement failed. AgreementId={Id}, Status={Status}, Body={Body}",
                    agreement.AgreementId, response.StatusCode, error);
                throw new CRMException(ErrorCodeEnum.SystemError,
                    $"Failed to recall agreement via Adobe Sign (HTTP {(int)response.StatusCode})");
            }

            // Update local status
            agreement.Status = "Cancelled";
            agreement.ModifyDate = DateTimeOffset.UtcNow;
            await _db.Updateable(agreement)
                .UpdateColumns(a => new { a.Status, a.ModifyDate })
                .ExecuteCommandAsync();

            _logger.LogInformation("[AdobeSign] Agreement recalled. WFE ID={Id}", id);
            return true;
        }

        // ------------------------------------------------------------------ //
        //  HandleWebhookAsync
        // ------------------------------------------------------------------ //

        /// <inheritdoc />
        public async Task HandleWebhookAsync(string eventType, string adobeAgreementId)
        {
            _logger.LogInformation("[AdobeSign] Webhook received. Event={Event}, AdobeId={Id}",
                eventType, adobeAgreementId);

            var agreement = await _agreementRepository.GetByAgreementIdAsync(adobeAgreementId);
            if (agreement == null)
            {
                _logger.LogWarning("[AdobeSign] Webhook for unknown agreement. AdobeId={Id}", adobeAgreementId);
                return;
            }

            switch (eventType)
            {
                case "AGREEMENT_CREATED":
                    await UpdateStatusAsync(agreement, "Awaiting");
                    break;

                case "AGREEMENT_ACTION_COMPLETED":
                    // A single signer completed — status stays Awaiting until all sign
                    await SyncSignerStatusesAsync(agreement);
                    break;

                case "AGREEMENT_WORKFLOW_COMPLETED":
                    await UpdateStatusAsync(agreement, "Completed");
                    await ArchiveSignedDocumentsAsync(agreement);
                    break;

                case "AGREEMENT_REJECTED":
                case "AGREEMENT_ACTION_REJECTED":
                    await UpdateStatusAsync(agreement, "Declined");
                    break;

                case "AGREEMENT_EXPIRED":
                    await UpdateStatusAsync(agreement, "Expired");
                    break;

                case "AGREEMENT_RECALLED":
                    await UpdateStatusAsync(agreement, "Cancelled");
                    break;

                default:
                    _logger.LogInformation("[AdobeSign] Unhandled webhook event type: {Event}", eventType);
                    break;
            }
        }

        // ------------------------------------------------------------------ //
        //  Private helpers — Adobe Sign API calls
        // ------------------------------------------------------------------ //

        private HttpClient CreateAdobeSignClient()
        {
            var client = _httpClientFactory.CreateClient("AdobeSign");
            return client;
        }

        /// <summary>
        /// POST /transientDocuments — upload a PDF and get a 24h-valid transient document ID
        /// </summary>
        private async Task<string> UploadTransientDocumentAsync(byte[] pdfBytes, string fileName)
        {
            var client = CreateAdobeSignClient();

            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(pdfBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            form.Add(fileContent, "File", fileName);
            form.Add(new StringContent("agreement"), "File-Name");
            form.Add(new StringContent("APPLICATION_PDF"), "Mime-Type");

            var response = await client.PostAsync("transientDocuments", form);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[AdobeSign] UploadTransientDocument failed. Status={Status}, Body={Body}",
                    response.StatusCode, responseBody);
                throw new CRMException(ErrorCodeEnum.SystemError,
                    $"Failed to upload document to Adobe Sign: {response.StatusCode}");
            }

            var doc = JsonNode.Parse(responseBody);
            var transientDocumentId = doc?["transientDocumentId"]?.GetValue<string>();

            if (string.IsNullOrEmpty(transientDocumentId))
            {
                throw new CRMException(ErrorCodeEnum.SystemError,
                    "Adobe Sign returned empty transientDocumentId");
            }

            return transientDocumentId;
        }

        /// <summary>
        /// POST /agreements — create the signing agreement and return the Adobe Agreement ID
        /// </summary>
        private async Task<string> CreateAgreementAsync(
            string transientDocumentId,
            RequestAdobeSignInputDto input,
            string documentName)
        {
            var client = CreateAdobeSignClient();

            // Build participant sets based on signing order
            var participantSets = BuildParticipantSets(input);

            var payload = new
            {
                fileInfos = new[]
                {
                    new { transientDocumentId }
                },
                name = documentName,
                participantSetsInfo = participantSets,
                signatureType = "ESIGN",
                state = "IN_PROCESS",
                daysUntilSigningDeadline = input.ExpirationDays,
                message = input.Message ?? string.Empty,
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("agreements", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[AdobeSign] CreateAgreement failed. Status={Status}, Body={Body}",
                    response.StatusCode, responseBody);
                throw new CRMException(ErrorCodeEnum.SystemError,
                    $"Failed to create Adobe Sign agreement: {response.StatusCode}");
            }

            var agreementNode = JsonNode.Parse(responseBody);
            var agreementId = agreementNode?["id"]?.GetValue<string>();

            if (string.IsNullOrEmpty(agreementId))
            {
                throw new CRMException(ErrorCodeEnum.SystemError,
                    "Adobe Sign returned empty agreement ID");
            }

            return agreementId;
        }

        private static List<object> BuildParticipantSets(RequestAdobeSignInputDto input)
        {
            var sets = new List<object>();

            if (input.SigningOrder == "Parallel")
            {
                // All signers in a single participant set (order=1)
                var members = new List<object>();
                foreach (var signer in input.Signers)
                {
                    members.Add(new { email = signer.Email, securityOption = new { authenticationMethod = "NONE" } });
                }
                sets.Add(new
                {
                    memberInfos = members,
                    order = 1,
                    role = "SIGNER"
                });
            }
            else
            {
                // Sequential — each signer gets their own participant set with incrementing order
                var grouped = new Dictionary<int, List<AdobeSignerDto>>();
                foreach (var signer in input.Signers)
                {
                    if (!grouped.ContainsKey(signer.Order))
                        grouped[signer.Order] = new List<AdobeSignerDto>();
                    grouped[signer.Order].Add(signer);
                }

                foreach (var (order, signers) in grouped)
                {
                    var members = new List<object>();
                    foreach (var s in signers)
                    {
                        members.Add(new { email = s.Email, securityOption = new { authenticationMethod = "NONE" } });
                    }
                    var role = signers[0].Role?.ToUpperInvariant() switch
                    {
                        "APPROVER" => "APPROVER",
                        "CC" => "CC",
                        _ => "SIGNER"
                    };
                    sets.Add(new { memberInfos = members, order, role });
                }
            }

            return sets;
        }

        /// <summary>
        /// Download signed PDF and Audit Trail from Adobe Sign, then save them to ff_onboarding_file
        /// </summary>
        private async Task ArchiveSignedDocumentsAsync(AdobeSignAgreement agreement)
        {
            try
            {
                var client = CreateAdobeSignClient();

                // Get source file info for naming
                var sourceFile = await _onboardingFileRepository.GetByIdAsync(agreement.SourceFileId);
                var baseName = sourceFile != null
                    ? Path.GetFileNameWithoutExtension(sourceFile.OriginalFileName)
                    : "document";
                var datePart = DateTimeOffset.UtcNow.ToString("yyyyMMdd");

                // Download signed PDF
                var signedPdfResponse = await client.GetAsync($"agreements/{agreement.AgreementId}/combinedDocument");
                if (signedPdfResponse.IsSuccessStatusCode)
                {
                    var signedBytes = await signedPdfResponse.Content.ReadAsByteArrayAsync();
                    var signedFileName = $"{baseName}_Signed_{datePart}.pdf";
                    var signedFileId = await SaveArchivedFileAsync(agreement, signedBytes, signedFileName, "application/pdf");

                    if (signedFileId.HasValue)
                    {
                        agreement.SignedFileId = signedFileId;
                    }
                }
                else
                {
                    _logger.LogWarning("[AdobeSign] Failed to download signed PDF. AgreementId={Id}, Status={Status}",
                        agreement.AgreementId, signedPdfResponse.StatusCode);
                }

                // Download Audit Trail
                var auditResponse = await client.GetAsync($"agreements/{agreement.AgreementId}/auditTrail");
                if (auditResponse.IsSuccessStatusCode)
                {
                    var auditBytes = await auditResponse.Content.ReadAsByteArrayAsync();
                    var auditFileName = $"{baseName}_Audit_Trail_{datePart}.pdf";
                    var auditFileId = await SaveArchivedFileAsync(agreement, auditBytes, auditFileName, "application/pdf");

                    if (auditFileId.HasValue)
                    {
                        agreement.AuditTrailFileId = auditFileId;
                    }
                }
                else
                {
                    _logger.LogWarning("[AdobeSign] Failed to download Audit Trail. AgreementId={Id}, Status={Status}",
                        agreement.AgreementId, auditResponse.StatusCode);
                }

                // Update agreement record with file IDs and completion time
                agreement.CompletedDate = DateTimeOffset.UtcNow;
                agreement.ModifyDate = DateTimeOffset.UtcNow;
                await _db.Updateable(agreement)
                    .UpdateColumns(a => new { a.SignedFileId, a.AuditTrailFileId, a.CompletedDate, a.ModifyDate })
                    .ExecuteCommandAsync();

                _logger.LogInformation(
                    "[AdobeSign] Documents archived. AgreementId={Id}, SignedFile={SignedId}, AuditFile={AuditId}",
                    agreement.AgreementId, agreement.SignedFileId, agreement.AuditTrailFileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[AdobeSign] Error archiving signed documents. AgreementId={Id}", agreement.AgreementId);
                // Don't rethrow — Webhook must return 200 regardless
            }
        }

        /// <summary>
        /// Save downloaded file bytes to blob storage and insert a new ff_onboarding_file record
        /// </summary>
        private async Task<long?> SaveArchivedFileAsync(
            AdobeSignAgreement agreement,
            byte[] fileBytes,
            string fileName,
            string contentType)
        {
            try
            {
                using var stream = new MemoryStream(fileBytes);
                var formFile = new FormFileWrapper(stream, fileName, contentType);
                var tenantId = agreement.TenantId ?? string.Empty;
                var storageResult = await _fileStorageService.SaveFileAsync(formFile, "adobe-sign-completed", tenantId);

                if (!storageResult.Success)
                {
                    _logger.LogWarning("[AdobeSign] SaveArchivedFile failed. FileName={Name}, Error={Error}",
                        fileName, storageResult.ErrorMessage);
                    return null;
                }

                var sourceFile = await _onboardingFileRepository.GetByIdAsync(agreement.SourceFileId);

                var fileEntity = new OnboardingFile
                {
                    OnboardingId = agreement.OnboardingId,
                    StageId = agreement.StageId,
                    AttachmentId = 0,
                    OriginalFileName = fileName,
                    StoredFileName = storageResult.FileName ?? fileName,
                    FileExtension = ".pdf",
                    FileSize = fileBytes.LongLength,
                    ContentType = contentType,
                    Category = sourceFile?.Category ?? "Document",
                    AccessUrl = storageResult.AccessUrl,
                    StoragePath = storageResult.FilePath,
                    UploadedById = agreement.RequestedBy.ToString(),
                    UploadedDate = DateTimeOffset.UtcNow,
                    Status = "Active",
                    Version = 1,
                    Source = "AdobeSign",
                };
                fileEntity.TenantId = agreement.TenantId;
                fileEntity.AppCode = agreement.AppCode;
                fileEntity.CreateDate = DateTimeOffset.UtcNow;
                fileEntity.ModifyDate = DateTimeOffset.UtcNow;
                fileEntity.IsValid = true;

                await _db.Insertable(fileEntity).ExecuteCommandAsync();
                return fileEntity.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdobeSign] Exception saving archived file. FileName={Name}", fileName);
                return null;
            }
        }

        /// <summary>
        /// Sync individual signer statuses from Adobe Sign API.
        /// Only updates Status/SignedAt on existing signer records — never overwrites
        /// the original Email/Name/Role/Order data that was set when the agreement was created.
        /// </summary>
        private async Task SyncSignerStatusesAsync(AdobeSignAgreement agreement)
        {
            try
            {
                var client = CreateAdobeSignClient();
                var response = await client.GetAsync($"agreements/{agreement.AgreementId}/members");

                if (!response.IsSuccessStatusCode) return;

                var body = await response.Content.ReadAsStringAsync();

                // Parse existing signers — these contain the original Email/Name/Role/Order
                List<AdobeSignerDto> signers = new();
                if (!string.IsNullOrEmpty(agreement.Signers))
                {
                    try
                    {
                        signers = JsonSerializer.Deserialize<List<AdobeSignerDto>>(agreement.Signers,
                                      new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                                  ?? new List<AdobeSignerDto>();
                    }
                    catch { /* leave empty */ }
                }

                // Parse Adobe Sign /members response:
                // { "participantSets": [{ "memberInfos": [{ "email": "...", "status": "...", "completionDate": "..." }] }] }
                var membersNode = JsonNode.Parse(body);
                var participantSets = membersNode?["participantSets"]?.AsArray();
                if (participantSets != null)
                {
                    foreach (var set in participantSets)
                    {
                        var memberInfos = set?["memberInfos"]?.AsArray();
                        if (memberInfos == null) continue;
                        foreach (var member in memberInfos)
                        {
                            var email = member?["email"]?.GetValue<string>();
                            var status = member?["status"]?.GetValue<string>();
                            var completionDate = member?["completionDate"]?.GetValue<string>();

                            if (string.IsNullOrEmpty(email)) continue;

                            var signer = signers.FirstOrDefault(s =>
                                string.Equals(s.Email, email, StringComparison.OrdinalIgnoreCase));

                            if (signer != null)
                            {
                                // Map Adobe Sign status to our internal status
                                signer.Status = status?.ToUpperInvariant() switch
                                {
                                    "SIGNED" or "APPROVED" or "ACCEPTED" or "FORM_FILLED" => "Signed",
                                    "DECLINED" or "REJECTED" => "Declined",
                                    "CANCELLED" => "Cancelled",
                                    _ => "Awaiting"
                                };

                                if (!string.IsNullOrEmpty(completionDate) &&
                                    DateTimeOffset.TryParse(completionDate, out var dt))
                                {
                                    signer.SignedAt = dt;
                                }
                            }
                        }
                    }
                }

                // Persist updated signers (original Email/Name/Role/Order preserved)
                agreement.Signers = JsonSerializer.Serialize(signers);
                agreement.ModifyDate = DateTimeOffset.UtcNow;
                await _db.Updateable(agreement)
                    .UpdateColumns(a => new { a.Signers, a.ModifyDate })
                    .ExecuteCommandAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[AdobeSign] SyncSignerStatuses failed. AgreementId={Id}", agreement.AgreementId);
            }
        }

        private async Task UpdateStatusAsync(AdobeSignAgreement agreement, string status)
        {
            agreement.Status = status;
            agreement.ModifyDate = DateTimeOffset.UtcNow;
            await _db.Updateable(agreement)
                .UpdateColumns(a => new { a.Status, a.ModifyDate })
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Download file bytes from a URL (blob storage or direct URL)
        /// </summary>
        private async Task<byte[]> DownloadFileBytesAsync(string urlOrPath)
        {
            // If it's a relative local path, read from disk via storage service
            if (!urlOrPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                using var fileStream = (await _fileStorageService.GetFileAsync(urlOrPath)).stream;
                if (fileStream == null)
                    throw new CRMException(ErrorCodeEnum.DataNotFound, $"Could not read file at path: {urlOrPath}");

                using var ms = new MemoryStream();
                await fileStream.CopyToAsync(ms);
                return ms.ToArray();
            }

            // Otherwise fetch via HTTP
            using var httpClient = _httpClientFactory.CreateClient();
            var bytes = await httpClient.GetByteArrayAsync(urlOrPath);
            return bytes;
        }

        // ------------------------------------------------------------------ //
        //  Mapping
        // ------------------------------------------------------------------ //

        private static AdobeSignAgreementOutputDto MapToOutputDto(AdobeSignAgreement agreement)
        {
            List<AdobeSignerDto> signers = new();
            if (!string.IsNullOrEmpty(agreement.Signers))
            {
                try
                {
                    signers = JsonSerializer.Deserialize<List<AdobeSignerDto>>(agreement.Signers,
                                  new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                              ?? new List<AdobeSignerDto>();
                }
                catch
                {
                    // Ignore deserialization errors — return empty list
                }
            }

            return new AdobeSignAgreementOutputDto
            {
                Id = agreement.Id.ToString(),
                AgreementId = agreement.AgreementId,
                OnboardingId = agreement.OnboardingId,
                StageId = agreement.StageId,
                SourceFileId = agreement.SourceFileId,
                SignedFileId = agreement.SignedFileId,
                AuditTrailFileId = agreement.AuditTrailFileId,
                Status = agreement.Status,
                Signers = signers,
                SigningOrder = agreement.SigningOrder,
                ExpirationDays = agreement.ExpirationDays,
                Message = agreement.Message,
                RequestedByName = agreement.CreateBy,
                CreateDate = agreement.CreateDate,
                CompletedDate = agreement.CompletedDate,
            };
        }
    }

    /// <summary>
    /// Minimal IFormFile wrapper for saving in-memory byte arrays via IFileStorageService
    /// </summary>
    internal sealed class FormFileWrapper : Microsoft.AspNetCore.Http.IFormFile
    {
        private readonly Stream _stream;
        private readonly string _fileName;
        private readonly string _contentType;

        public FormFileWrapper(Stream stream, string fileName, string contentType)
        {
            _stream = stream;
            _fileName = fileName;
            _contentType = contentType;
        }

        public string ContentType => _contentType;
        public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{_fileName}\"";
        public Microsoft.AspNetCore.Http.IHeaderDictionary Headers => new Microsoft.AspNetCore.Http.HeaderDictionary();
        public long Length => _stream.Length;
        public string Name => "file";
        public string FileName => _fileName;

        public void CopyTo(Stream target) => _stream.CopyTo(target);
        public async Task CopyToAsync(Stream target, System.Threading.CancellationToken cancellationToken = default)
            => await _stream.CopyToAsync(target, cancellationToken);
        public Stream OpenReadStream() { _stream.Position = 0; return _stream; }
    }
}
