using Microsoft.Extensions.Logging;
using VolcanionTracking.Application.Common.Interfaces;

namespace VolcanionTracking.Infrastructure.Services;

/// <summary>
/// Service to decrypt and verify encrypted requests from partners
/// </summary>
public class RequestDecryptionService : IRequestDecryptionService
{
    private readonly IPartnerRepository _partnerRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<RequestDecryptionService> _logger;

    public RequestDecryptionService(
        IPartnerRepository partnerRepository,
        IEncryptionService encryptionService,
        ICacheService cacheService,
        ILogger<RequestDecryptionService> logger)
    {
        _partnerRepository = partnerRepository;
        _encryptionService = encryptionService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<(bool IsValid, string? DecryptedData, string? Error)> DecryptAndVerifyAsync(
        string encryptedData,
        string requestId,
        string requestTime,
        string partnerCode,
        string signature,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validate request time (within 5 minutes)
            if (!DateTime.TryParseExact(
                requestTime,
                "yyyyMMddHHmmss",
                null,
                System.Globalization.DateTimeStyles.None,
                out var requestDateTime))
            {
                return (false, null, "Invalid request time format");
            }

            var timeDiff = Math.Abs((DateTime.UtcNow - requestDateTime).TotalMinutes);
            if (timeDiff > 5)
            {
                return (false, null, $"Request time is too old or in future: {timeDiff} minutes");
            }

            // 2. Check for duplicate requestId (replay attack prevention)
            var requestIdKey = $"request_id:{requestId}";
            var isDuplicateStr = await _cacheService.GetAsync<string>(requestIdKey, cancellationToken);
            if (isDuplicateStr != null)
            {
                return (false, null, "Duplicate request ID - possible replay attack");
            }

            // 3. Get partner by code (with caching)
            var cacheKey = $"partner:code:{partnerCode}";
            var partnerIdStr = await _cacheService.GetAsync<string>(cacheKey, cancellationToken);

            Domain.Aggregates.PartnerAggregate.Partner? partner = null;
            if (partnerIdStr != null && Guid.TryParse(partnerIdStr, out var partnerId))
            {
                partner = await _partnerRepository.GetByIdAsync(partnerId, cancellationToken);
            }

            if (partner == null)
            {
                // Try to find partner by code
                var partners = await _partnerRepository.GetAllAsync(cancellationToken);
                partner = partners.FirstOrDefault(p => p.Code == partnerCode);

                if (partner == null)
                {
                    return (false, null, $"Partner not found with code: {partnerCode}");
                }

                // Cache partner ID
                await _cacheService.SetAsync(cacheKey, partner.Id.ToString(), TimeSpan.FromHours(1), cancellationToken);
            }

            if (!partner.IsActive)
            {
                return (false, null, "Partner is not active");
            }

            // 4. Decrypt AES data first to get raw JSON
            string decryptedData;
            try
            {
                decryptedData = _encryptionService.DecryptAES(encryptedData, partner.AESKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt AES data for partner: {PartnerCode}", partnerCode);
                return (false, null, "Failed to decrypt data - invalid AES key or corrupted data");
            }

            // 5. Verify RSA signature
            // preSign format: {jsonRawData}|{requestTime}|{requestId}|{partner}
            var preSign = $"{decryptedData}|{requestTime}|{requestId}|{partnerCode}";
            
            bool isSignatureValid;
            try
            {
                isSignatureValid = _encryptionService.VerifyRSASignature(
                    preSign,
                    signature,
                    partner.RSAPublicKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify RSA signature for partner: {PartnerCode}", partnerCode);
                return (false, null, "Failed to verify signature - invalid RSA key");
            }

            if (!isSignatureValid)
            {
                _logger.LogWarning(
                    "Invalid signature for partner: {PartnerCode}, RequestId: {RequestId}",
                    partnerCode,
                    requestId);
                return (false, null, "Invalid signature - data may have been tampered");
            }

            // 6. Store requestId in cache to prevent replay (keep for 10 minutes)
            await _cacheService.SetAsync(
                requestIdKey,
                "processed",
                TimeSpan.FromMinutes(10),
                cancellationToken);

            _logger.LogInformation(
                "Successfully decrypted and verified request from partner: {PartnerCode}, RequestId: {RequestId}",
                partnerCode,
                requestId);

            return (true, decryptedData, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error during request decryption for partner: {PartnerCode}, RequestId: {RequestId}",
                partnerCode,
                requestId);
            return (false, null, $"Internal error: {ex.Message}");
        }
    }
}
