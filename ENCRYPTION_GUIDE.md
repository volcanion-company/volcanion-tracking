# Encryption Guide for Volcanion Tracking

## Overview

All requests to the Volcanion Tracking API must be encrypted using a combination of AES and RSA encryption. This ensures data confidentiality and integrity.

## Encrypted Request Format

Every request must follow this format:

```json
{
  "data": "AES-encrypted-json",
  "requestId": "guid-unique-per-request",
  "requestTime": "yyyyMMddHHmmss",
  "partner": "partner-code",
  "sign": "RSA-signature"
}
```

### Fields Description

- **data**: Your actual request payload (JSON) encrypted with AES using your partner's AES key
- **requestId**: A unique GUID for each request (prevents replay attacks)
- **requestTime**: Current timestamp in format `yyyyMMddHHmmss` (requests older than 5 minutes are rejected)
- **partner**: Your partner code (Partner.Code in the system)
- **sign**: RSA signature to verify request integrity

### Signature Generation

The signature is created as follows:

```csharp
// 1. Create pre-sign string
var preSign = $"{jsonRawData}|{requestTime}|{requestId}|{partnerCode}";

// 2. Sign with RSA private key (SHA256 + PKCS1 padding)
var signature = SignRSA(preSign, rsaPrivateKey);
```

## Step-by-Step Guide

### 1. Generate Encryption Keys

Use the utility endpoints to generate keys for your partner:

```bash
# Generate all keys at once
curl https://localhost:5001/api/encryptionutility/generate-all-keys
```

Response:
```json
{
  "aesKey": "base64-encoded-256-bit-key",
  "rsaPublicKey": "-----BEGIN RSA PUBLIC KEY-----\n...\n-----END RSA PUBLIC KEY-----",
  "rsaPrivateKey": "-----BEGIN RSA PRIVATE KEY-----\n...\n-----END RSA PRIVATE KEY-----",
  "generatedAt": "2025-12-24T10:30:00Z"
}
```

**Important**: Store these keys securely. The RSA private key must never be shared.

### 2. Register Partner with Keys

Create a partner with the generated keys:

```bash
curl -X POST https://localhost:5001/api/partners \
  -H "Content-Type: application/json" \
  -d '{
    "code": "PARTNER001",
    "name": "Test Partner",
    "email": "partner@example.com",
    "aesKey": "your-aes-key",
    "rsaPublicKey": "your-rsa-public-key",
    "rsaPrivateKey": "your-rsa-private-key"
  }'
```

### 3. Create Encrypted Request

#### C# Example

```csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public class VolcanionClient
{
    private readonly string _partnerCode;
    private readonly string _aesKey;
    private readonly string _rsaPrivateKey;

    public VolcanionClient(string partnerCode, string aesKey, string rsaPrivateKey)
    {
        _partnerCode = partnerCode;
        _aesKey = aesKey;
        _rsaPrivateKey = rsaPrivateKey;
    }

    public string CreateEncryptedRequest<T>(T data)
    {
        // 1. Serialize data to JSON
        var jsonData = JsonSerializer.Serialize(data);

        // 2. Generate metadata
        var requestId = Guid.NewGuid().ToString();
        var requestTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        // 3. Encrypt data with AES
        var encryptedData = EncryptAES(jsonData, _aesKey);

        // 4. Create signature
        var preSign = $"{jsonData}|{requestTime}|{requestId}|{_partnerCode}";
        var signature = SignRSA(preSign, _rsaPrivateKey);

        // 5. Create encrypted request
        var encryptedRequest = new
        {
            data = encryptedData,
            requestId = requestId,
            requestTime = requestTime,
            partner = _partnerCode,
            sign = signature
        };

        return JsonSerializer.Serialize(encryptedRequest);
    }

    private string EncryptAES(string plainData, string aesKeyBase64)
    {
        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(aesKeyBase64);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        
        msEncrypt.Write(aes.IV, 0, aes.IV.Length);

        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainData);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    private string SignRSA(string data, string privateKeyPem)
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
}

// Usage
var client = new VolcanionClient("PARTNER001", aesKey, rsaPrivateKey);

var eventData = new
{
    apiKey = "sk_test123",
    eventName = "page_view",
    eventTimestamp = DateTime.UtcNow,
    userId = "user_123",
    anonymousId = "anon_456",
    eventProperties = "{\"page\": \"/home\"}"
};

var encryptedRequest = client.CreateEncryptedRequest(eventData);

// Send to API
using var httpClient = new HttpClient();
var content = new StringContent(encryptedRequest, Encoding.UTF8, "application/json");
var response = await httpClient.PostAsync("https://api.volcanion.com/api/events/ingest", content);
```

#### Python Example

```python
import base64
import json
from datetime import datetime
from uuid import uuid4
from Crypto.Cipher import AES
from Crypto.PublicKey import RSA
from Crypto.Signature import pkcs1_15
from Crypto.Hash import SHA256
from Crypto.Util.Padding import pad
import requests

class VolcanionClient:
    def __init__(self, partner_code, aes_key, rsa_private_key):
        self.partner_code = partner_code
        self.aes_key = base64.b64decode(aes_key)
        self.rsa_key = RSA.import_key(rsa_private_key)
    
    def encrypt_aes(self, plain_data):
        cipher = AES.new(self.aes_key, AES.MODE_CBC)
        ct_bytes = cipher.encrypt(pad(plain_data.encode('utf-8'), AES.block_size))
        iv = cipher.iv
        return base64.b64encode(iv + ct_bytes).decode('utf-8')
    
    def sign_rsa(self, data):
        h = SHA256.new(data.encode('utf-8'))
        signature = pkcs1_15.new(self.rsa_key).sign(h)
        return base64.b64encode(signature).decode('utf-8')
    
    def create_encrypted_request(self, data):
        # 1. Serialize data
        json_data = json.dumps(data)
        
        # 2. Generate metadata
        request_id = str(uuid4())
        request_time = datetime.utcnow().strftime('%Y%m%d%H%M%S')
        
        # 3. Encrypt data
        encrypted_data = self.encrypt_aes(json_data)
        
        # 4. Create signature
        pre_sign = f"{json_data}|{request_time}|{request_id}|{self.partner_code}"
        signature = self.sign_rsa(pre_sign)
        
        # 5. Create encrypted request
        return {
            "data": encrypted_data,
            "requestId": request_id,
            "requestTime": request_time,
            "partner": self.partner_code,
            "sign": signature
        }
    
    def ingest_event(self, event_data):
        encrypted_request = self.create_encrypted_request(event_data)
        response = requests.post(
            'https://api.volcanion.com/api/events/ingest',
            json=encrypted_request
        )
        return response.json()

# Usage
client = VolcanionClient('PARTNER001', aes_key, rsa_private_key)

event_data = {
    'apiKey': 'sk_test123',
    'eventName': 'page_view',
    'eventTimestamp': datetime.utcnow().isoformat(),
    'userId': 'user_123',
    'anonymousId': 'anon_456',
    'eventProperties': json.dumps({'page': '/home'})
}

result = client.ingest_event(event_data)
print(result)
```

### 4. Test Encryption

Use the test endpoint to verify your encryption:

```bash
curl -X POST https://localhost:5001/api/encryptionutility/test-encryption \
  -H "Content-Type: application/json" \
  -d '{
    "data": {
      "apiKey": "sk_test123",
      "eventName": "page_view",
      "eventTimestamp": "2025-12-24T10:30:00Z",
      "userId": "user_123",
      "anonymousId": "anon_456",
      "eventProperties": "{\"page\": \"/home\"}"
    },
    "partnerCode": "PARTNER001",
    "aesKey": "your-aes-key",
    "rsaPrivateKey": "your-rsa-private-key"
  }'
```

## Security Considerations

### Request Validation

The system performs the following validations:

1. **Time Validation**: Request must be within 5 minutes of server time
2. **Replay Protection**: RequestId is cached for 10 minutes to prevent replay attacks
3. **Signature Verification**: RSA signature must be valid
4. **Partner Validation**: Partner must exist and be active
5. **Decryption**: AES decryption must succeed

### Best Practices

1. **Key Storage**: Store keys securely (use environment variables, key vaults)
2. **Key Rotation**: Rotate keys periodically (update via Partner management API)
3. **Request Uniqueness**: Always generate unique RequestId (GUID)
4. **Time Sync**: Ensure your server time is synchronized (NTP)
5. **HTTPS Only**: Always use HTTPS in production
6. **Private Key Security**: Never expose RSA private key

## Error Responses

### 400 Bad Request
```json
{
  "error": "Invalid request format",
  "message": "Request must be in encrypted format"
}
```

### 401 Unauthorized
```json
{
  "error": "Request verification failed",
  "message": "Invalid signature - data may have been tampered",
  "requestId": "guid-here"
}
```

Common causes:
- Invalid AES key (wrong key or corrupted)
- Invalid RSA signature (wrong private key or data modified)
- Request time too old (>5 minutes)
- Duplicate requestId (replay attack)
- Partner not found or inactive

## Endpoints Affected

All `POST`, `PUT`, `PATCH` endpoints require encrypted requests:

- `/api/partners` (Create partner - special case, may need unencrypted)
- `/api/partnersystems`
- `/api/events/ingest`

Excluded endpoints (no encryption required):
- `/health`
- `/metrics`
- `/scalar/v1`
- `/openapi/v1.json`
- All `GET` endpoints

## Support

For integration support, contact: support@volcanion.com

## Example Complete Flow

```bash
# 1. Generate keys
curl https://localhost:5001/api/encryptionutility/generate-all-keys

# 2. Create partner (unencrypted - first time setup)
curl -X POST https://localhost:5001/api/partners \
  -H "Content-Type: application/json" \
  -d '{...keys...}'

# 3. Create partner system (get API key)
# ... create encrypted request ...

# 4. Ingest events (encrypted)
# ... create encrypted request ...
```

## Key Rotation

To rotate encryption keys:

```bash
# Generate new keys
curl https://localhost:5001/api/encryptionutility/generate-all-keys

# Update partner keys
# ... create encrypted request to update partner ...
```

---

**Remember**: All requests are logged. Never send sensitive data in plain text.
