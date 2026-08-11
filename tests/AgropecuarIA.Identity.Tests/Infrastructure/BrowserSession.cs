using System.Net.Http.Json;
using System.Text.Json;

namespace AgropecuarIA.Identity.Tests.Infrastructure;

internal sealed class BrowserSession(
    HttpClient client,
    IReadOnlyDictionary<string, string>? initialCookies = null) : IDisposable
{
    private readonly Dictionary<string, string> _cookies = new(StringComparer.Ordinal);
    private readonly List<string> _setCookieHeaders = [];
    private bool _initialCookiesApplied;

    public void InitializeCookies()
    {
        if (_initialCookiesApplied)
        {
            return;
        }

        _initialCookiesApplied = true;
        if (initialCookies is not null)
        {
            foreach (var cookie in initialCookies)
            {
                _cookies.TryAdd(cookie.Key, cookie.Value);
            }
        }
    }

    public IReadOnlyDictionary<string, string> Cookies => _cookies;

    public IReadOnlyList<string> SetCookieHeaders => _setCookieHeaders;

    public async Task<HttpResponseMessage> GetAsync(
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        return await SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostAsync<TBody>(
        string requestUri,
        TBody body,
        string? antiforgeryToken = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(body),
        };
        if (antiforgeryToken is not null)
        {
            request.Headers.Add("X-CSRF-TOKEN", antiforgeryToken);
        }

        return await SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostWithoutBodyAsync(
        string requestUri,
        string antiforgeryToken,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Add("X-CSRF-TOKEN", antiforgeryToken);
        return await SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostWithIdempotencyKeyAsync<TBody>(
        string requestUri,
        TBody body,
        string? antiforgeryToken,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(body),
        };
        if (antiforgeryToken is not null)
        {
            request.Headers.Add("X-CSRF-TOKEN", antiforgeryToken);
        }

        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return await SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostWithIfMatchAsync<TBody>(
        string requestUri,
        TBody body,
        string? antiforgeryToken,
        string? ifMatch,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(body),
        };
        if (antiforgeryToken is not null)
        {
            request.Headers.Add("X-CSRF-TOKEN", antiforgeryToken);
        }

        if (ifMatch is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        }

        return await SendAsync(request, cancellationToken);
    }

    public async Task<string> GetAntiforgeryTokenAsync(CancellationToken cancellationToken = default)
    {
        using var response = await GetAsync("/api/identity/antiforgery", cancellationToken);
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);
        return json.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("The antiforgery token was null.");
    }

    public void Dispose()
    {
        client.Dispose();
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        InitializeCookies();

        if (_cookies.Count > 0)
        {
            request.Headers.Add(
                "Cookie",
                string.Join("; ", _cookies.Select(cookie => $"{cookie.Key}={cookie.Value}")));
        }

        var response = await client.SendAsync(request, cancellationToken);
        CaptureCookies(response);
        return response;
    }

    private void CaptureCookies(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return;
        }

        foreach (var value in values)
        {
            _setCookieHeaders.Add(value);
            var nameValue = value.Split(';', 2, StringSplitOptions.TrimEntries)[0];
            var separatorIndex = nameValue.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                continue;
            }

            var name = nameValue[..separatorIndex];
            var cookieValue = nameValue[(separatorIndex + 1)..];
            if (cookieValue.Length == 0)
            {
                _cookies.Remove(name);
            }
            else
            {
                _cookies[name] = cookieValue;
            }
        }
    }
}
