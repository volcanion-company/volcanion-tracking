namespace VolcanionTracking.Application.Common.Interfaces;

/// <summary>
/// Encryption service for AES and RSA operations
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Decrypt AES encrypted data
    /// </summary>
    string DecryptAES(string encryptedData, string aesKey);

    /// <summary>
    /// Encrypt data with AES
    /// </summary>
    string EncryptAES(string plainData, string aesKey);

    /// <summary>
    /// Verify RSA signature
    /// </summary>
    bool VerifyRSASignature(string data, string signature, string publicKey);

    /// <summary>
    /// Sign data with RSA private key
    /// </summary>
    string SignRSA(string data, string privateKey);

    /// <summary>
    /// Decrypt RSA encrypted data with private key
    /// </summary>
    string DecryptRSA(string encryptedData, string privateKey);
}
