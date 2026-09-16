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
        private readonly IWorkflowTriggerLogRepository _triggerLogRepository;

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
            UserContext userContext,
            IWorkflowTriggerLogRepository triggerLogRepository)
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
            _triggerLogRepository = triggerLogRepository;
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

            // Step 4: Persist agreement record — if DB write fails, recall the agreement to prevent orphans
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
            try
            {
                await _db.Insertable(agreement).ExecuteCommandAsync();
            }
            catch (Exception dbEx)
            {
                // DB write failed — recall the Agreement on Adobe Sign to prevent an orphan
                _logger.LogError(dbEx,
                    "[AdobeSign] DB insert failed after Agreement created on Adobe Sign. Recalling to prevent orphan. AdobeId={Id}",
                    adobeAgreementId);
                try
                {
                    var recallClient = CreateAdobeSignClient();
                    var recallPayload = JsonSerializer.Serialize(new { state = "CANCELLED", comment = "Auto-recalled: local DB write failed" });
                    await recallClient.PutAsync($"agreements/{adobeAgreementId}/state",
                        new StringContent(recallPayload, Encoding.UTF8, "application/json"));
                }
                catch (Exception recallEx)
                {
                    _logger.LogError(recallEx,
                        "[AdobeSign] Auto-recall also failed. Orphan Agreement exists on Adobe Sign. AdobeId={Id}",
                        adobeAgreementId);
                }
                throw new CRMException(ErrorCodeEnum.SystemError,
                    "Failed to save signing request. The Adobe Sign agreement has been automatically cancelled.");
            }

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

            // If still Awaiting, sync signer statuses from Adobe Sign in real time
            // so the Details modal always shows the latest per-person signing state
            if (agreement.Status == "Awaiting")
            {
                try { await SyncSignerStatusesAsync(agreement); }
                catch { /* non-critical — return cached data if sync fails */ }
                // Re-read from DB to pick up the updated signers JSON
                agreement = await _agreementRepository.GetByIdAsync(id) ?? agreement;
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

            // Step 1: Get participant IDs for the requested signer emails
            // Adobe Sign v6 /reminders requires participantIds, not email addresses
            var membersResponse = await client.GetAsync($"agreements/{agreement.AgreementId}/members");
            if (!membersResponse.IsSuccessStatusCode)
            {
                var membersError = await membersResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("[AdobeSign] GetMembers failed. AgreementId={Id}, Status={Status}, Body={Body}",
                    agreement.AgreementId, membersResponse.StatusCode, membersError);
                throw new CRMException(ErrorCodeEnum.SystemError,
                    $"Failed to get agreement members from Adobe Sign (HTTP {(int)membersResponse.StatusCode})");
            }

            var membersBody = await membersResponse.Content.ReadAsStringAsync();
            var participantIds = ExtractParticipantIds(membersBody, signerEmails);

            if (!participantIds.Any())
            {
                _logger.LogWarning("[AdobeSign] No matching participant IDs found for emails. AgreementId={Id}", agreement.AgreementId);
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    "None of the specified signer emails were found as active participants in this agreement.");
            }

            // Step 2: Send reminder with participant IDs
            // Adobe Sign v6: POST /reminders creates a new reminder with status ACTIVE
            var payload = new
            {
                recipientParticipantIds = participantIds,
                status = "ACTIVE",
                note = "Reminder: Please sign the document at your earliest convenience."
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
                agreement.AgreementId, participantIds.Count);
            return true;
        }

        /// <summary>
        /// Extracts participantIds from GET /members response for the given signer emails.
        /// Response format: { "participantSets": [{ "memberInfos": [{ "id": "...", "email": "..." }] }] }
        /// </summary>
        private static List<string> ExtractParticipantIds(string membersJson, List<string> signerEmails)
        {
            var ids = new List<string>();
            try
            {
                var node = JsonNode.Parse(membersJson);
                var sets = node?["participantSets"]?.AsArray();
                if (sets == null) return ids;

                var emailSet = new HashSet<string>(signerEmails, StringComparer.OrdinalIgnoreCase);
                foreach (var set in sets)
                {
                    var members = set?["memberInfos"]?.AsArray();
                    if (members == null) continue;
                    foreach (var member in members)
                    {
                        var email = member?["email"]?.GetValue<string>();
                        var pid   = member?["id"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(pid) && !string.IsNullOrEmpty(email) && emailSet.Contains(email))
                            ids.Add(pid);
                    }
                }
            }
            catch { /* return whatever was collected */ }
            return ids;
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
        //  GetPendingSignaturesAsync
        // ------------------------------------------------------------------ //

        /// <inheritdoc />
        public async Task<List<PendingSignatureDto>> GetPendingSignaturesAsync(long onboardingId)
        {
            var pendingAgreements = await _agreementRepository.GetPendingByOnboardingIdAsync(onboardingId);
            if (!pendingAgreements.Any()) return new List<PendingSignatureDto>();

            // Batch-load all source files and stages to avoid N+1
            var fileIds = pendingAgreements.Select(a => a.SourceFileId).Distinct().ToList();
            var stageIds = pendingAgreements.Select(a => a.StageId).Distinct().ToList();

            var files = await _db.Queryable<OnboardingFile>()
                .Where(f => fileIds.Contains(f.Id) && f.IsValid == true)
                .ToListAsync();

            var stages = await _db.Queryable<Stage>()
                .Where(s => stageIds.Contains(s.Id) && s.IsValid == true)
                .ToListAsync();

            var fileMap = files.ToDictionary(f => f.Id);
            var stageMap = stages.ToDictionary(s => s.Id);

            var result = new List<PendingSignatureDto>();
            foreach (var ag in pendingAgreements)
            {
                fileMap.TryGetValue(ag.SourceFileId, out var sourceFile);
                stageMap.TryGetValue(ag.StageId, out var stage);

                // Count signers who have not yet signed
                List<AdobeSignerDto> signers = new();
                if (!string.IsNullOrEmpty(ag.Signers))
                {
                    try
                    {
                        signers = JsonSerializer.Deserialize<List<AdobeSignerDto>>(ag.Signers,
                                      new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                                  ?? new();
                    }
                    catch { /* use empty list */ }
                }

                var pendingCount = signers.Count(s => s.SignedAt == null
                    && s.Status != "Declined" && s.Status != "Cancelled");

                result.Add(new PendingSignatureDto
                {
                    AgreementId       = ag.Id.ToString(),
                    FileName          = sourceFile?.OriginalFileName ?? "Unknown file",
                    StageName         = stage?.Name ?? "Unknown stage",
                    PendingSignerCount = pendingCount > 0 ? pendingCount : signers.Count,
                    TotalSignerCount  = signers.Count,
                    CreatedAt         = ag.CreateDate,
                });
            }

            return result;
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

                case "AGREEMENT_WORKFLOW_COMPLETED":
                    // Idempotent: skip if already marked Completed and documents already archived
                    if (agreement.Status == "Completed" && agreement.SignedFileId.HasValue)
                    {
                        _logger.LogInformation(
                            "[AdobeSign] Duplicate AGREEMENT_WORKFLOW_COMPLETED ignored — already archived. AdobeId={Id}",
                            adobeAgreementId);
                        break;
                    }
                    // Archive first — only mark Completed after documents are successfully saved
                    // This prevents the "Completed status but no file" inconsistency
                    // Also sync signer statuses so the Details modal shows correct per-person state
                    await SyncSignerStatusesAsync(agreement);
                    await ArchiveSignedDocumentsAsync(agreement);
                    break;

                case "AGREEMENT_ACTION_COMPLETED":
                    // A single signer completed — status stays Awaiting until all sign
                    // Idempotent: SyncSignerStatusesAsync overwrites in place, safe to re-run
                    await SyncSignerStatusesAsync(agreement);
                    break;

                case "AGREEMENT_REJECTED":
                case "AGREEMENT_ACTION_REJECTED":
                    if (agreement.Status == "Declined") break; // already processed
                    await UpdateStatusAsync(agreement, "Declined");
                    break;

                case "AGREEMENT_EXPIRED":
                    if (agreement.Status == "Expired") break;
                    await UpdateStatusAsync(agreement, "Expired");
                    break;

                case "AGREEMENT_RECALLED":
                    if (agreement.Status == "Cancelled") break;
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
            var token   = _configuration["AdobeSign:AccessToken"] ?? throw new InvalidOperationException("AdobeSign:AccessToken not configured");
            var baseUrl = (_configuration["AdobeSign:BaseUrl"] ?? "https://api.na2.adobesign.com/api/rest/v6").TrimEnd('/');

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(baseUrl + "/");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("User-Agent", "FlowFlex-AdobeSign/1.0");
            client.Timeout = TimeSpan.FromSeconds(60);
            return client;
        }

        /// <summary>
        /// POST /transientDocuments — upload a PDF and get a 24h-valid transient document ID
        /// </summary>
        private async Task<string> UploadTransientDocumentAsync(byte[] pdfBytes, string fileName)
        {
            var client = CreateAdobeSignClient();

            using var form = new MultipartFormDataContent();

            // Adobe Sign v6 /transientDocuments expects exactly one part named "File"
            // with the correct Content-Type. Extra fields (File-Name, Mime-Type) are not accepted.
            var fileContent = new ByteArrayContent(pdfBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            form.Add(fileContent, "File", fileName);

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
                // Each signer gets their own participant set with order=1 (all sent simultaneously).
                // Adobe Sign's "group" semantics mean any member in a set can sign on behalf of the group.
                // To require ALL signers to sign independently, each must be their own participant set.
                foreach (var signer in input.Signers)
                {
                    var role = signer.Role?.ToUpperInvariant() switch
                    {
                        "APPROVER" => "APPROVER",
                        "CC" => "CC",
                        _ => "SIGNER"
                    };
                    sets.Add(new
                    {
                        memberInfos = new[]
                        {
                            new { email = signer.Email, securityOption = new { authenticationMethod = "NONE" } }
                        },
                        order = 1,
                        role
                    });
                }
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
        /// Download signed PDF and Audit Trail from Adobe Sign, then save them to ff_onboarding_file.
        /// Works for both active and Force Completed cases — the signing flow continues regardless of case status.
        /// After archiving, also syncs the signed file to any downstream Cases that were triggered
        /// from the same source Case via the Workflow Trigger Graph.
        /// </summary>
        private async Task ArchiveSignedDocumentsAsync(AdobeSignAgreement agreement)
        {
            try
            {
                // Second-layer idempotency guard: if signed file already exists, skip download
                if (agreement.SignedFileId.HasValue)
                {
                    _logger.LogInformation(
                        "[AdobeSign] ArchiveSignedDocuments skipped — SignedFileId already set. AgreementId={Id}",
                        agreement.AgreementId);
                    return;
                }
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

                // Only mark Completed AFTER signed file is confirmed saved
                // This prevents the "Completed status but no signed file" inconsistency (#5)
                if (agreement.SignedFileId.HasValue)
                {
                    await UpdateStatusAsync(agreement, "Completed");
                    await SyncSignedDocumentToDownstreamCasesAsync(agreement);
                }
                else
                {
                    _logger.LogError(
                        "[AdobeSign] Signed PDF download failed — status NOT updated to Completed. AgreementId={Id}. Manual retry required.",
                        agreement.AgreementId);
                }

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
        /// After signing completes, copy the signed document to all downstream Cases
        /// that were created by the Workflow Trigger Graph from the same source Case.
        /// This ensures downstream cases always have the latest signed version.
        /// </summary>
        private async Task SyncSignedDocumentToDownstreamCasesAsync(AdobeSignAgreement agreement)
        {
            try
            {
                // Find all downstream Cases triggered from the source Case
                var triggerLogs = await _triggerLogRepository.GetBySourceOnboardingIdAsync(agreement.OnboardingId);
                var downstreamIds = triggerLogs
                    .Where(l => l.Status == "Triggered" && l.TargetOnboardingId.HasValue)
                    .Select(l => l.TargetOnboardingId!.Value)
                    .Distinct()
                    .ToList();

                if (!downstreamIds.Any())
                {
                    _logger.LogDebug(
                        "[AdobeSign] No downstream cases to sync for OnboardingId={Id}", agreement.OnboardingId);
                    return;
                }

                // Load the signed file record that was just saved
                var signedFile = await _onboardingFileRepository.GetByIdAsync(agreement.SignedFileId!.Value);
                if (signedFile == null)
                {
                    _logger.LogWarning("[AdobeSign] Signed file {Id} not found for downstream sync", agreement.SignedFileId);
                    return;
                }

                var now = DateTimeOffset.UtcNow;

                foreach (var downstreamId in downstreamIds)
                {
                    // Check if a copy for this downstream Case already exists (idempotent)
                    var existing = await _db.Queryable<OnboardingFile>()
                        .Where(f => f.OnboardingId == downstreamId
                                 && f.SourceFileId == signedFile.Id
                                 && f.IsValid == true)
                        .FirstAsync();

                    if (existing != null)
                    {
                        _logger.LogDebug(
                            "[AdobeSign] Signed file already synced to downstream Case {DownstreamId}, skipping",
                            downstreamId);
                        continue;
                    }

                    // Copy the signed file record to the downstream Case
                    // StageId is intentionally null — the downstream case may have different stages
                    var downstreamFile = new OnboardingFile
                    {
                        OnboardingId     = downstreamId,
                        StageId          = null,
                        AttachmentId     = 0,
                        OriginalFileName = signedFile.OriginalFileName,
                        StoredFileName   = signedFile.StoredFileName,
                        FileExtension    = signedFile.FileExtension,
                        FileSize         = signedFile.FileSize,
                        ContentType      = signedFile.ContentType,
                        Category         = signedFile.Category,
                        AccessUrl        = signedFile.AccessUrl,
                        StoragePath      = signedFile.StoragePath,
                        UploadedById     = signedFile.UploadedById,
                        UploadedDate     = now,
                        Status           = "Active",
                        Version          = 1,
                        Source           = "AdobeSign",
                        // SourceFileId links back to the signed file in the source Case for traceability
                        SourceFileId     = signedFile.Id,
                    };
                    downstreamFile.TenantId   = agreement.TenantId;
                    downstreamFile.AppCode    = agreement.AppCode;
                    downstreamFile.CreateDate = now;
                    downstreamFile.ModifyDate = now;
                    downstreamFile.IsValid    = true;

                    await _db.Insertable(downstreamFile).ExecuteCommandAsync();

                    _logger.LogInformation(
                        "[AdobeSign] Signed file synced to downstream Case {DownstreamId}. SignedFileId={FileId}",
                        downstreamId, signedFile.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[AdobeSign] Failed to sync signed documents to downstream cases. AgreementId={Id}",
                    agreement.AgreementId);
                // Non-critical — don't rethrow
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
                var formFile = new FormFileWrapper(fileBytes, fileName, contentType);
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

                // Use ExecuteReturnSnowflakeIdAsync to get the actual inserted ID back
                // and avoid duplicate key issues from rapid concurrent inserts
                var insertedId = await _db.Insertable(fileEntity).ExecuteReturnSnowflakeIdAsync();
                fileEntity.Id = insertedId;
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

                _logger.LogInformation("[AdobeSign] SyncSignerStatuses members response. AgreementId={Id}, Body={Body}",
                    agreement.AgreementId, body);
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
                            // completionDate is at the set level, not member level
                            var completionDate = set?["completionDate"]?.GetValue<string>()
                                ?? member?["completionDate"]?.GetValue<string>();

                            if (string.IsNullOrEmpty(email)) continue;

                            var signer = signers.FirstOrDefault(s =>
                                string.Equals(s.Email, email, StringComparison.OrdinalIgnoreCase));

                            if (signer != null)
                            {
                                // Map Adobe Sign member status to our internal status
                                // Adobe Sign member status: ACTIVE = waiting, WAITING_FOR_OTHERS = sequential wait
                                // The set-level status COMPLETED is more reliable for "has signed"
                                var setStatus = set?["status"]?.GetValue<string>();
                                var memberIsDone = string.Equals(setStatus, "COMPLETED", StringComparison.OrdinalIgnoreCase)
                                    || status?.ToUpperInvariant() is "SIGNED" or "APPROVED" or "ACCEPTED" or "FORM_FILLED" or "COMPLETED";

                                signer.Status = memberIsDone ? "Signed"
                                    : status?.ToUpperInvariant() switch
                                    {
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
        private readonly byte[] _bytes;
        private readonly string _fileName;
        private readonly string _contentType;

        public FormFileWrapper(byte[] bytes, string fileName, string contentType)
        {
            _bytes = bytes;
            _fileName = fileName;
            _contentType = contentType;
        }

        public string ContentType => _contentType;
        public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{_fileName}\"";
        public Microsoft.AspNetCore.Http.IHeaderDictionary Headers => new Microsoft.AspNetCore.Http.HeaderDictionary();
        public long Length => _bytes.LongLength;
        public string Name => "file";
        public string FileName => _fileName;

        // Each call returns a fresh MemoryStream so callers that dispose it don't affect others
        public void CopyTo(Stream target) => new MemoryStream(_bytes).CopyTo(target);
        public async Task CopyToAsync(Stream target, System.Threading.CancellationToken cancellationToken = default)
            => await new MemoryStream(_bytes).CopyToAsync(target, cancellationToken);
        public Stream OpenReadStream() => new MemoryStream(_bytes);
    }
}
