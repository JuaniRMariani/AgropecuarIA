using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace AgropecuarIA.IdentitySpike.Api.Recovery;

internal sealed class RecoveryRequestService(TimeProvider timeProvider)
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);
    private const int MaximumTrackedIdentifiers = 1024;
    private readonly object _sync = new();
    private readonly Dictionary<string, List<DateTimeOffset>> _requests = [];

    internal RecoveryDecision Accept(string? email)
    {
        var normalized = Normalize(email);
        if (normalized is null)
        {
            return new(true, "invalid_request");
        }

        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        var now = timeProvider.GetUtcNow();

        lock (_sync)
        {
            foreach (var existingKey in _requests.Keys.ToArray())
            {
                var existingTimestamps = _requests[existingKey];
                existingTimestamps.RemoveAll(timestamp => timestamp <= now - Window);
                if (existingTimestamps.Count == 0)
                {
                    _requests.Remove(existingKey);
                }
            }

            if (!_requests.TryGetValue(key, out var timestamps))
            {
                if (_requests.Count >= MaximumTrackedIdentifiers)
                {
                    return new(true, "global_rate_limited");
                }

                timestamps = [];
                _requests[key] = timestamps;
            }

            if (timestamps.Count >= 3)
            {
                return new(true, "rate_limited");
            }

            timestamps.Add(now);
            return new(true, null);
        }
    }

    private static string? Normalize(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254 || !MailAddress.TryCreate(email, out var parsed))
        {
            return null;
        }

        return parsed.Address.Trim().ToUpperInvariant();
    }
}

internal sealed record RecoveryDecision(bool Accepted, string? InternalReasonCode);
