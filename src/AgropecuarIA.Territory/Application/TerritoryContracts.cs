using Microsoft.AspNetCore.Http;

namespace AgropecuarIA.Territory.Application;

public static class TerritoryReferenceStatuses
{
    public const string Fresh = "fresh";
    public const string Stale = "stale";
    public const string Unavailable = "unavailable";
}

public sealed record TerritorySearchResponse(
    string Status,
    TerritorySourceResponse Source,
    IReadOnlyList<TerritoryUnitResponse> Items);

public sealed record TerritoryResolveResponse(
    string Status,
    TerritorySourceResponse? Source,
    TerritoryUnitResponse? Unit,
    TerritoryFallbackResponse Fallback);

public sealed record TerritorySourceResponse(
    string Provider,
    string Version,
    DateTimeOffset CapturedAtUtc);

public sealed record TerritoryUnitResponse(
    string OfficialCode,
    string Name,
    string Level,
    string? ParentCode,
    string? ParentName,
    string HierarchyLabel);

public sealed record TerritoryFallbackResponse(bool SearchAvailable);

public sealed class TerritoryOperationException : Exception
{
    public TerritoryOperationException(
        string code,
        string title,
        int statusCode,
        bool retryable = false)
        : base(title)
    {
        Code = code;
        Title = title;
        StatusCode = statusCode;
        Retryable = retryable;
    }

    public string Code { get; }

    public string Title { get; }

    public int StatusCode { get; }

    public bool Retryable { get; }
}

public static class TerritoryErrors
{
    public static TerritoryOperationException InvalidSearchQuery() => new(
        "territory.invalid_search_query",
        "Search query must contain between 2 and 80 characters.",
        StatusCodes.Status400BadRequest);

    public static TerritoryOperationException InvalidLevel() => new(
        "territory.invalid_level",
        "Territory level is invalid.",
        StatusCodes.Status400BadRequest);

    public static TerritoryOperationException InvalidParentCode() => new(
        "territory.invalid_parent_code",
        "Parent code is invalid.",
        StatusCodes.Status400BadRequest);

    public static TerritoryOperationException InvalidLimit() => new(
        "territory.invalid_limit",
        "Search limit must be between 1 and 20.",
        StatusCodes.Status400BadRequest);

    public static TerritoryOperationException InvalidCoordinates() => new(
        "territory.invalid_coordinates",
        "Coordinates must be finite WGS84 values inside the supported continental Argentina bounds.",
        StatusCodes.Status400BadRequest);

    public static TerritoryOperationException ReferenceUnavailable() => new(
        "territory.reference_unavailable",
        "The official territory reference is temporarily unavailable.",
        StatusCodes.Status503ServiceUnavailable,
        retryable: true);
}

public sealed class TerritoryProviderException : Exception
{
    public TerritoryProviderException(string message)
        : base(message)
    {
    }

    public TerritoryProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
