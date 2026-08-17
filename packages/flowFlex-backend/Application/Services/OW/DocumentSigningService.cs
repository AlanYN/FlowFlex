using System.IO;
using System.Security.Cryptography;
using AutoMapper;
using FlowFlex.Application.Contracts;
using FlowFlex.Application.Contracts.Dtos.OW.DocumentSigning;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Implementation of <see cref="IDocumentSigningService"/> (OW-704).
    ///
    /// Signing pipeline (Requirements 15.1–15.7, 17.1):
    ///   1. Validate source file exists and is not already signed (400 otherwise).
    ///   2. Read signed-PDF bytes from dto.File; compute SHA-256 independently.
    ///   3. Upload signed PDF to blob storage.
    ///   4. In a DB transaction: insert new OnboardingFile (is_signed=true) + ChangeLog.
    ///   5. On transaction failure: best-effort delete the uploaded blob, log orphan warning, return 500.
    /// </summary>
    public class DocumentSigningService : IDocumentSigningService, IScopedService
    {
        private readonly IOnboardingFileRepository _onboardingFileRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IOperationChangeLogService _operationChangeLogService;
        private readonly IOperatorContextService _operatorContextService;
        private readonly ISqlSugarClient _db;
        private readonly IMapper _mapper;
        private readonly ILogger<DocumentSigningService> _logger;
        private readonly UserContext _userContext;

        public DocumentSigningService(
            IOnboardingFileRepository onboardingFileRepository,
            IFileStorageService fileStorageService,
            IOperationChangeLogService operationChangeLogService,
            IOperatorContextService operatorContextService,
            ISqlSugarClient db,
            IMapper mapper,
            ILogger<DocumentSigningService> logger,
            UserContext userContext)
        {
            _onboardingFileRepository = onboardingFileRepository;
            _fileStorageService = fileStorageService;
            _operationChangeLogService = operationChangeLogService;
            _operatorContextService = operatorContextService;
            _db = db;
            _mapper = mapper;
            _logger = logger;
            _userContext = userContext;
        }

        // ------------------------------------------------------------------ //
        //  SignDocumentAsync
        // ------------------------------------------------------------------ //

        /// <inheritdoc />
        public async Task<SignDocumentOutputDto> SignDocumentAsync(long fileId, SignDocumentInputDto dto)
        {
            // Step 1: Validate source file exists and has not been signed yet
            var sourceFile = await _onboardingFileRepository.GetByIdAsync(fileId);
            if (sourceFile == null || !sourceFile.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, $"File {fileId} not found");
            }

            if (sourceFile.IsSigned)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"File {fileId} has already been signed");
            }

            // Step 2: Read bytes from the uploaded signed PDF and compute SHA-256
            byte[] signedBytes;
            using (var ms = new MemoryStream())
            {
                await dto.File.CopyToAsync(ms);
                signedBytes = ms.ToArray();
            }

            var fileHash = ComputeSha256(signedBytes);

            // Determine sign time — accept ISO-8601 from dto, fall back to UtcNow
            DateTimeOffset signTime = DateTimeOffset.UtcNow;
            if (!string.IsNullOrWhiteSpace(dto.SignedAt) &&
                DateTimeOffset.TryParse(dto.SignedAt, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var parsedTime))
            {
                signTime = parsedTime;
            }

            // Build signed file name
            var signedFileName = BuildSignedFileName(
                sourceFile.OriginalFileName,
                dto.SignerName,
                signTime.UtcDateTime);

            // Step 3: Upload the signed PDF to blob storage
            var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            var storageResult = await _fileStorageService.SaveFileAsync(dto.File, "signed-documents", tenantId);

            if (!storageResult.Success)
            {
                throw new CRMException(
                    ErrorCodeEnum.SystemError,
                    $"Failed to upload signed document: {storageResult.ErrorMessage}");
            }

            string uploadedFilePath = storageResult.FilePath;
            string accessUrl = storageResult.AccessUrl;

            // Step 4: DB transaction — insert OnboardingFile record + ChangeLog
            OnboardingFile signedFileEntity;
            try
            {
                _db.Ado.BeginTran();

                signedFileEntity = new OnboardingFile
                {
                    OnboardingId = sourceFile.OnboardingId,
                    StageId = sourceFile.StageId,
                    // AttachmentId is not applicable here (file stored via IFileStorageService);
                    // use 0 as a sentinel — same pattern as external-import files in this project.
                    AttachmentId = 0,
                    OriginalFileName = signedFileName,
                    StoredFileName = storageResult.FileName ?? signedFileName,
                    FileExtension = ".pdf",
                    FileSize = signedBytes.LongLength,
                    ContentType = "application/pdf",
                    Category = sourceFile.Category ?? "Document",
                    Description = sourceFile.Description,
                    IsRequired = sourceFile.IsRequired,
                    Tags = sourceFile.Tags,
                    AccessUrl = accessUrl,
                    StoragePath = uploadedFilePath,
                    UploadedById = _userContext?.UserId,
                    UploadedByName = _operatorContextService.GetOperatorDisplayName(),
                    UploadedDate = DateTimeOffset.UtcNow,
                    Status = "Active",
                    Version = 1,
                    SortOrder = sourceFile.SortOrder,
                    // Signing-specific fields
                    IsSigned = true,
                    SourceFileId = fileId,
                    FileHash = fileHash,
                    SignerName = dto.SignerName,
                    SignTime = signTime,
                };

                signedFileEntity.InitCreateInfo(_userContext);

                var insertResult = await _db.Insertable(signedFileEntity).ExecuteCommandAsync();
                if (insertResult <= 0)
                {
                    _db.Ado.RollbackTran();
                    throw new CRMException(ErrorCodeEnum.SystemError, "Failed to persist signed file record");
                }

                // Write ChangeLog entry (best-effort — failure rolls back the transaction)
                await _operationChangeLogService.LogOperationAsync(
                    operationType: OperationTypeEnum.FileUpload,
                    businessModule: BusinessModuleEnum.File,
                    businessId: signedFileEntity.Id,
                    onboardingId: sourceFile.OnboardingId,
                    stageId: sourceFile.StageId,
                    operationTitle: "Document Signed",
                    operationDescription: $"File '{signedFileName}' signed by '{dto.SignerName}' at {signTime:O}",
                    afterData: System.Text.Json.JsonSerializer.Serialize(new
                    {
                        SignedFileId = signedFileEntity.Id,
                        SourceFileId = fileId,
                        FileName = signedFileName,
                        FileHash = fileHash,
                        SignerName = dto.SignerName,
                        SignTime = signTime,
                    }));

                _db.Ado.CommitTran();
            }
            catch (CRMException)
            {
                // CRMException already rolled back above or not yet inside tran — just rethrow
                try { _db.Ado.RollbackTran(); } catch { /* ignore secondary failure */ }
                await TryDeleteOrphanedBlobAsync(uploadedFilePath);
                throw;
            }
            catch (Exception ex)
            {
                try { _db.Ado.RollbackTran(); } catch { /* ignore secondary failure */ }

                // Best-effort cleanup of uploaded blob (Requirement 15.5)
                await TryDeleteOrphanedBlobAsync(uploadedFilePath);

                _logger.LogError(ex,
                    "Transaction failed while signing file {FileId}. Blob at {FilePath} may be orphaned.",
                    fileId, uploadedFilePath);

                throw new CRMException(ErrorCodeEnum.SystemError,
                    $"Document signing failed during database commit: {ex.Message}");
            }

            // Build download URL
            var downloadUrl = await _fileStorageService.GetFileUrlAsync(uploadedFilePath);
            if (string.IsNullOrEmpty(downloadUrl))
            {
                downloadUrl = accessUrl;
            }

            return new SignDocumentOutputDto
            {
                SignedFileId = signedFileEntity.Id,
                DownloadUrl = downloadUrl,
                FileName = signedFileName,
                FileHash = fileHash,
            };
        }

        // ------------------------------------------------------------------ //
        //  ComputeSha256
        // ------------------------------------------------------------------ //

        /// <inheritdoc />
        public string ComputeSha256(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                // SHA-256 of empty input is well-defined; compute it correctly.
                data = Array.Empty<byte>();
            }

            var hashBytes = SHA256.HashData(data);
            return BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
        }

        // ------------------------------------------------------------------ //
        //  BuildSignedFileName
        // ------------------------------------------------------------------ //

        /// <inheritdoc />
        public string BuildSignedFileName(string originalName, string signerName, DateTime date)
        {
            // Strip extension (handles names with multiple dots correctly)
            var nameWithoutExt = Path.GetFileNameWithoutExtension(originalName ?? "document");
            var datePart = date.ToString("MMddyyyy");
            return $"{nameWithoutExt}_signed_{signerName}_{datePart}.pdf";
        }

        // ------------------------------------------------------------------ //
        //  Private helpers
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Attempt to delete a previously uploaded blob file.
        /// Logs a warning if deletion fails so ops can clean up manually.
        /// </summary>
        private async Task TryDeleteOrphanedBlobAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            try
            {
                var deleted = await _fileStorageService.DeleteFileAsync(filePath);
                if (!deleted)
                {
                    _logger.LogWarning(
                        "Orphaned blob could not be deleted after transaction failure. FilePath={FilePath}",
                        filePath);
                }
            }
            catch (Exception deleteEx)
            {
                _logger.LogWarning(deleteEx,
                    "Exception while deleting orphaned blob after transaction failure. FilePath={FilePath}",
                    filePath);
            }
        }
    }
}
