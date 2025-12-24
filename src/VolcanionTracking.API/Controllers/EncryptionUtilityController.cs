using Microsoft.AspNetCore.Mvc;
using VolcanionTracking.API.Utilities;

namespace VolcanionTracking.API.Controllers;

/// <summary>
/// Utility controller for encryption key generation and testing
/// Should be disabled in production or protected with admin authentication
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EncryptionUtilityController : ControllerBase
{
    private readonly ILogger<EncryptionUtilityController> _logger;

    public EncryptionUtilityController(ILogger<EncryptionUtilityController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generate AES key for a new partner
    /// </summary>
    [HttpGet("generate-aes-key")]
    [ProducesResponseType(typeof(KeyGenerationResult), StatusCodes.Status200OK)]
    public IActionResult GenerateAESKey()
    {
        var aesKey = EncryptionHelper.GenerateAESKey();
        
        return Ok(new KeyGenerationResult
        {
            Type = "AES",
            Key = aesKey,
            KeySize = "256-bit",
            Format = "Base64"
        });
    }

    /// <summary>
    /// Generate RSA key pair for a new partner
    /// </summary>
    [HttpGet("generate-rsa-keypair")]
    [ProducesResponseType(typeof(RSAKeyPairResult), StatusCodes.Status200OK)]
    public IActionResult GenerateRSAKeyPair()
    {
        var (publicKey, privateKey) = EncryptionHelper.GenerateRSAKeyPair();
        
        return Ok(new RSAKeyPairResult
        {
            PublicKey = publicKey,
            PrivateKey = privateKey,
            KeySize = "2048-bit",
            Format = "PEM"
        });
    }

    /// <summary>
    /// Generate all keys needed for a new partner
    /// </summary>
    [HttpGet("generate-all-keys")]
    [ProducesResponseType(typeof(AllKeysResult), StatusCodes.Status200OK)]
    public IActionResult GenerateAllKeys()
    {
        var aesKey = EncryptionHelper.GenerateAESKey();
        var (publicKey, privateKey) = EncryptionHelper.GenerateRSAKeyPair();
        
        return Ok(new AllKeysResult
        {
            AESKey = aesKey,
            RSAPublicKey = publicKey,
            RSAPrivateKey = privateKey,
            GeneratedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test encryption with provided keys
    /// </summary>
    [HttpPost("test-encryption")]
    [ProducesResponseType(typeof(EncryptionTestResult), StatusCodes.Status200OK)]
    public IActionResult TestEncryption([FromBody] EncryptionTestRequest request)
    {
        try
        {
            var encryptedRequest = EncryptionHelper.CreateEncryptedRequest(
                request.Data,
                request.PartnerCode,
                request.AESKey,
                request.RSAPrivateKey);

            return Ok(new EncryptionTestResult
            {
                Success = true,
                EncryptedRequest = encryptedRequest,
                Message = "Encryption successful. Use this encrypted request to call the API."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during encryption test");
            return Ok(new EncryptionTestResult
            {
                Success = false,
                Message = $"Encryption failed: {ex.Message}"
            });
        }
    }
}

public class KeyGenerationResult
{
    public string Type { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string KeySize { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
}

public class RSAKeyPairResult
{
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string KeySize { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
}

public class AllKeysResult
{
    public string AESKey { get; set; } = string.Empty;
    public string RSAPublicKey { get; set; } = string.Empty;
    public string RSAPrivateKey { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}

public class EncryptionTestRequest
{
    public object Data { get; set; } = new();
    public string PartnerCode { get; set; } = string.Empty;
    public string AESKey { get; set; } = string.Empty;
    public string RSAPrivateKey { get; set; } = string.Empty;
}

public class EncryptionTestResult
{
    public bool Success { get; set; }
    public string? EncryptedRequest { get; set; }
    public string Message { get; set; } = string.Empty;
}
