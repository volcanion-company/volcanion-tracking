using VolcanionTracking.Domain.Common;

namespace VolcanionTracking.Domain.ValueObjects;

/// <summary>
/// Value object representing an API key for authentication
/// </summary>
public class ApiKey : ValueObject
{
    public string Value { get; private set; }

    private ApiKey(string value)
    {
        Value = value;
    }

    public static Result<ApiKey> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<ApiKey>.Failure("API key cannot be empty");

        if (value.Length < 32)
            return Result<ApiKey>.Failure("API key must be at least 32 characters");

        return Result<ApiKey>.Success(new ApiKey(value));
    }

    public static ApiKey Generate()
    {
        var key = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + 
                  Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        return new ApiKey(key.Replace("=", "").Replace("+", "").Replace("/", ""));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
