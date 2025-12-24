namespace VolcanionTracking.Application.Common.Interfaces;

/// <summary>
/// Service to decrypt and verify encrypted requests
/// </summary>
public interface IRequestDecryptionService
{
    /// <summary>
    /// Decrypt and verify encrypted request
    /// Returns the decrypted JSON data if successful
    /// </summary>
    Task<(bool IsValid, string? DecryptedData, string? Error)> DecryptAndVerifyAsync(
        string encryptedData,
        string requestId,
        string requestTime,
        string partnerCode,
        string signature,
        CancellationToken cancellationToken = default);
}
