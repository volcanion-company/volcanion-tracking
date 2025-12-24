using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace VolcanionTracking.API.Utilities;

/// <summary>
/// Helper class for partners to generate encrypted requests
/// This is for testing/demonstration purposes
/// </summary>
public static class EncryptionHelper
{
    /// <summary>
    /// Generate AES key (Base64 encoded 256-bit key)
    /// </summary>
    public static string GenerateAESKey()
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        return Convert.ToBase64String(aes.Key);
    }

    /// <summary>
    /// Generate RSA key pair (PEM format)
    /// </summary>
    public static (string PublicKey, string PrivateKey) GenerateRSAKeyPair()
    {
        using var rsa = RSA.Create(2048);
        var publicKey = rsa.ExportRSAPublicKeyPem();
        var privateKey = rsa.ExportRSAPrivateKeyPem();
        return (publicKey, privateKey);
    }

    /// <summary>
    /// Encrypt data with AES
    /// </summary>
    public static string EncryptAES(string plainData, string aesKeyBase64)
    {
        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(aesKeyBase64);
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

    /// <summary>
    /// Sign data with RSA private key
    /// </summary>
    public static string SignRSA(string data, string privateKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);

        var dataBytes = Encoding.UTF8.GetBytes(data);
        var signatureBytes = rsa.SignData(
            dataBytes,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return Convert.ToBase64String(signatureBytes);
    }

    /// <summary>
    /// Create encrypted request payload
    /// </summary>
    public static string CreateEncryptedRequest<T>(
        T data,
        string partnerCode,
        string aesKey,
        string rsaPrivateKey)
    {
        // 1. Serialize data to JSON
        var jsonData = JsonSerializer.Serialize(data);

        // 2. Generate request metadata
        var requestId = Guid.NewGuid().ToString();
        var requestTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        // 3. Encrypt data with AES
        var encryptedData = EncryptAES(jsonData, aesKey);

        // 4. Create signature
        var preSign = $"{jsonData}|{requestTime}|{requestId}|{partnerCode}";
        var signature = SignRSA(preSign, rsaPrivateKey);

        // 5. Create encrypted request object
        var encryptedRequest = new
        {
            data = encryptedData,
            requestId = requestId,
            requestTime = requestTime,
            partner = partnerCode,
            sign = signature
        };

        return JsonSerializer.Serialize(encryptedRequest);
    }

    /// <summary>
    /// Example usage for testing
    /// </summary>
    public static void ExampleUsage()
    {
        // Generate keys (do this once per partner)
        var aesKey = GenerateAESKey();
        var (publicKey, privateKey) = GenerateRSAKeyPair();

        Console.WriteLine("AES Key:");
        Console.WriteLine(aesKey);
        Console.WriteLine("\nRSA Public Key:");
        Console.WriteLine(publicKey);
        Console.WriteLine("\nRSA Private Key:");
        Console.WriteLine(privateKey);

        // Create a sample request
        var sampleData = new
        {
            apiKey = "sk_test123",
            eventName = "page_view",
            eventTimestamp = DateTime.UtcNow,
            userId = "user_123",
            anonymousId = "anon_456",
            eventProperties = "{\"page\": \"/home\"}"
        };

        var encryptedRequest = CreateEncryptedRequest(
            sampleData,
            "PARTNER001",
            aesKey,
            privateKey);

        Console.WriteLine("\nEncrypted Request:");
        Console.WriteLine(encryptedRequest);
    }
}
