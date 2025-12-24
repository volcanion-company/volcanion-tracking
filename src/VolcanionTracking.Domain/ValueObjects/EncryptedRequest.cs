using VolcanionTracking.Domain.Common;

namespace VolcanionTracking.Domain.ValueObjects;

/// <summary>
/// Value object representing an encrypted request
/// </summary>
public class EncryptedRequest : ValueObject
{
    public string Data { get; private set; }
    public string RequestId { get; private set; }
    public string RequestTime { get; private set; }
    public string Partner { get; private set; }
    public string Sign { get; private set; }

    private EncryptedRequest(string data, string requestId, string requestTime, string partner, string sign)
    {
        Data = data;
        RequestId = requestId;
        RequestTime = requestTime;
        Partner = partner;
        Sign = sign;
    }

    public static Result<EncryptedRequest> Create(string data, string requestId, string requestTime, string partner, string sign)
    {
        if (string.IsNullOrWhiteSpace(data))
            return Result<EncryptedRequest>.Failure("Data is required");

        if (string.IsNullOrWhiteSpace(requestId))
            return Result<EncryptedRequest>.Failure("RequestId is required");

        if (!Guid.TryParse(requestId, out _))
            return Result<EncryptedRequest>.Failure("RequestId must be a valid GUID");

        if (string.IsNullOrWhiteSpace(requestTime))
            return Result<EncryptedRequest>.Failure("RequestTime is required");

        if (requestTime.Length != 14)
            return Result<EncryptedRequest>.Failure("RequestTime must be in format yyyyMMddHHmmss");

        if (string.IsNullOrWhiteSpace(partner))
            return Result<EncryptedRequest>.Failure("Partner code is required");

        if (string.IsNullOrWhiteSpace(sign))
            return Result<EncryptedRequest>.Failure("Signature is required");

        return Result<EncryptedRequest>.Success(new EncryptedRequest(data, requestId, requestTime, partner, sign));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Data;
        yield return RequestId;
        yield return RequestTime;
        yield return Partner;
        yield return Sign;
    }
}
