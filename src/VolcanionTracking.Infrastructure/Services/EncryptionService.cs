using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using VolcanionTracking.Application.Common.Interfaces;

namespace VolcanionTracking.Infrastructure.Services;

/// <summary>
/// Implementation of encryption service for AES and RSA operations
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly ILogger<EncryptionService> _logger;

    public EncryptionService(ILogger<EncryptionService> logger)
    {
        _logger = logger;
    }

    public string DecryptAES(string encryptedData, string aesKey)
    {
        try
        {
            var fullCipher = Convert.FromBase64String(encryptedData);

            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(aesKey);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Extract IV (first 16 bytes)
            var iv = new byte[16];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;

            // Extract cipher text (remaining bytes)
            var cipherText = new byte[fullCipher.Length - iv.Length];
            Array.Copy(fullCipher, iv.Length, cipherText, 0, cipherText.Length);

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipherText);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting AES data");
            throw new InvalidOperationException("Failed to decrypt AES data", ex);
        }
    }

    public string EncryptAES(string plainData, string aesKey)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(aesKey);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            
            // Write IV first
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);

            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainData);
            }

            return Convert.ToBase64String(msEncrypt.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting AES data");
            throw new InvalidOperationException("Failed to encrypt AES data", ex);
        }
    }

    public bool VerifyRSASignature(string data, string signature, string publicKey)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKey);

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = Convert.FromBase64String(signature);

            return rsa.VerifyData(
                dataBytes,
                signatureBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying RSA signature");
            return false;
        }
    }

    public string SignRSA(string data, string privateKey)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKey);

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = rsa.SignData(
                dataBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            return Convert.ToBase64String(signatureBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing RSA data");
            throw new InvalidOperationException("Failed to sign RSA data", ex);
        }
    }

    public string DecryptRSA(string encryptedData, string privateKey)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKey);

            var encryptedBytes = Convert.FromBase64String(encryptedData);
            var decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.Pkcs1);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting RSA data");
            throw new InvalidOperationException("Failed to decrypt RSA data", ex);
        }
    }
}
