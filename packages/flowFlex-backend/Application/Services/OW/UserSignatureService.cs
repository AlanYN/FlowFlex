using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.UserSignature;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Service for managing user electronic signatures (OW-703).
    /// Signatures are not tenant-isolated — they follow the user across all tenants.
    /// </summary>
    public class UserSignatureService : IUserSignatureService, IScopedService
    {
        private const int MaxSignaturesPerUser = 7;
        private const long MaxImageBytes = 500 * 1024; // 500 KB

        private readonly IUserSignatureRepository _userSignatureRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserSignatureService> _logger;
        private readonly UserContext _userContext;
        private readonly IOperatorContextService _operatorContextService;

        public UserSignatureService(
            IUserSignatureRepository userSignatureRepository,
            IMapper mapper,
            ILogger<UserSignatureService> logger,
            UserContext userContext,
            IOperatorContextService operatorContextService)
        {
            _userSignatureRepository = userSignatureRepository;
            _mapper = mapper;
            _logger = logger;
            _userContext = userContext;
            _operatorContextService = operatorContextService;
        }

        /// <inheritdoc />
        public async Task<List<ProfileSignatureOutputDto>> GetByCurrentUserAsync()
        {
            var userId = GetCurrentUserIdOrThrow();
            var signatures = await _userSignatureRepository.GetByUserIdAsync(userId);
            return _mapper.Map<List<ProfileSignatureOutputDto>>(signatures);
        }

        /// <inheritdoc />
        public async Task<ProfileSignatureOutputDto> CreateAsync(CreateSignatureInputDto input)
        {
            var userId = GetCurrentUserIdOrThrow();

            // Requirement 4.1 / 4.2: enforce 7-signature cap
            var existing = await _userSignatureRepository.GetByUserIdAsync(userId);
            if (existing.Count >= MaxSignaturesPerUser)
            {
                throw new CRMException(
                    ErrorCodeEnum.OperationNotAllowed,
                    $"已达签名上限（{MaxSignaturesPerUser}个），请删除后再添加");
            }

            // Requirement 6.5: reject if decoded image exceeds 500 KB
            ValidateImageSize(input.ImageBase64);

            var entity = new UserSignature
            {
                UserId = userId,
                ImageData = input.ImageBase64,
            };
            entity.InitCreateInfo(_userContext);

            var inserted = await _userSignatureRepository.InsertAsync(entity);
            if (!inserted)
            {
                throw new CRMException(ErrorCodeEnum.SystemError, "Failed to save signature");
            }

            _logger.LogInformation("User {UserId} created signature {SignatureId}", userId, entity.Id);

            return _mapper.Map<ProfileSignatureOutputDto>(entity);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(long signatureId)
        {
            var userId = GetCurrentUserIdOrThrow();

            var signature = await _userSignatureRepository.GetByIdAsync(signatureId);

            if (signature == null || !signature.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Signature not found");
            }

            // Requirement 7.4: 403 if the signature belongs to a different user
            if (signature.UserId != userId)
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "Access denied: signature does not belong to the current user");
            }

            // Requirement 6.4 / 5.1: soft delete — set is_valid = false
            signature.IsValid = false;
            signature.InitModifyInfo(_userContext);

            await _userSignatureRepository.UpdateAsync(signature);

            _logger.LogInformation("User {UserId} deleted signature {SignatureId}", userId, signatureId);
        }

        #region Private helpers

        /// <summary>
        /// Returns the current user's ID, throwing <see cref="CRMException"/> if the user is not authenticated.
        /// </summary>
        private long GetCurrentUserIdOrThrow()
        {
            if (string.IsNullOrEmpty(_userContext?.UserId) ||
                !long.TryParse(_userContext.UserId, out var userId) ||
                userId <= 0)
            {
                throw new CRMException(ErrorCodeEnum.AuthenticationFail, "User not authenticated");
            }

            return userId;
        }

        /// <summary>
        /// Validates that the base64-decoded image does not exceed 500 KB.
        /// Throws <see cref="CRMException"/> when the limit is exceeded.
        /// </summary>
        private static void ValidateImageSize(string imageBase64)
        {
            if (string.IsNullOrEmpty(imageBase64))
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "ImageBase64 is required");
            }

            // Strip optional data-URI prefix (e.g. "data:image/png;base64,")
            var base64Data = imageBase64;
            var commaIndex = imageBase64.IndexOf(',');
            if (commaIndex >= 0)
            {
                base64Data = imageBase64[(commaIndex + 1)..];
            }

            // Fast approximate size check before full decode (each Base64 char ≈ 0.75 bytes)
            var approximateBytes = (long)(base64Data.Length * 0.75);
            if (approximateBytes > MaxImageBytes + 512) // small tolerance
            {
                throw new CRMException(ErrorCodeEnum.UploadFileTooLarge, "签名图片大小超过 500KB 限制");
            }

            // Exact check via actual byte count after decode
            try
            {
                var bytes = Convert.FromBase64String(base64Data);
                if (bytes.Length > MaxImageBytes)
                {
                    throw new CRMException(ErrorCodeEnum.UploadFileTooLarge, "签名图片大小超过 500KB 限制");
                }
            }
            catch (FormatException)
            {
                throw new CRMException(ErrorCodeEnum.DataFormatInvalid, "ImageBase64 is not valid base64-encoded data");
            }
        }

        #endregion
    }
}
