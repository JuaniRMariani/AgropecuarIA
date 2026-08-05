using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace AgropecuarIA.GisWeatherSpike;

public enum CapMessageType
{
    Alert,
    Update,
    Cancel,
}

public enum CapStatus
{
    Actual,
    Exercise,
    System,
    Test,
    Draft,
}

public enum CapScope
{
    Public,
    Restricted,
    Private,
}

public enum CapLifecycleState
{
    Active,
    Updated,
    Cancelled,
    Expired,
}

public sealed record CapArea(string Description, IReadOnlyList<IReadOnlyList<GeoPosition>> Polygons);

public sealed record CapAlert(
    string Sender,
    string Identifier,
    DateTimeOffset Sent,
    CapMessageType MessageType,
    CapStatus Status,
    CapScope Scope,
    DateTimeOffset Effective,
    DateTimeOffset Expires,
    IReadOnlyList<string> References,
    IReadOnlyList<CapArea> Areas,
    CapLifecycleState LifecycleState,
    string SourceHash,
    IReadOnlyList<string> Limitations)
{
    public const string ContractVersion = "1.0";
}

public sealed class CapParser
{
    public const int MaximumPayloadBytes = 2 * 1024 * 1024;
    private static readonly XNamespace CapNamespace = "urn:oasis:names:tc:emergency:cap:1.2";

    public static ProviderParseResult<CapAlert> Parse(Stream payload, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(payload);

        using var activity = WeatherDiagnostics.ActivitySource.StartActivity("weather.cap.parse", ActivityKind.Internal);
        activity?.SetTag("weather.provider", "smn-cap");
        var started = Stopwatch.GetTimestamp();

        try
        {
            var bytes = ReadLimited(payload, MaximumPayloadBytes);
            using var content = new MemoryStream(bytes, writable: false);
            using var reader = XmlReader.Create(content, CreateSecureSettings());
            var document = XDocument.Load(reader, LoadOptions.None);

            if (document.Root?.Name != CapNamespace + "alert")
            {
                return Fail(new ProviderError(ProviderErrorCode.SchemaInvalid, "CAP root must be alert in the CAP 1.2 namespace."), activity);
            }

            var root = document.Root;
            var sender = RequiredText(root, "sender");
            var identifier = RequiredText(root, "identifier");
            var sent = RequiredTimestamp(root, "sent");
            var status = RequiredEnum<CapStatus>(root, "status");
            var messageType = RequiredEnum<CapMessageType>(root, "msgType");
            var scope = RequiredEnum<CapScope>(root, "scope");
            var references = ParseReferences(OptionalText(root, "references"));
            var info = root.Elements(CapNamespace + "info").FirstOrDefault()
                ?? throw new CapSchemaException("CAP alert must contain at least one info element.");

            var expires = RequiredTimestamp(info, "expires");
            var effective = OptionalTimestamp(info, "effective") ??
                            OptionalTimestamp(info, "onset") ??
                            sent;
            if (expires <= effective)
            {
                throw new CapSchemaException("CAP expires must be later than effective/onset.");
            }

            var eventName = OptionalText(info, "event");
            var limitations = new List<string>();
            var areas = new List<CapArea>();
            foreach (var areaElement in info.Elements(CapNamespace + "area"))
            {
                var description = OptionalText(areaElement, "areaDesc");
                if (string.IsNullOrWhiteSpace(description))
                {
                    description = string.IsNullOrWhiteSpace(eventName)
                        ? "Área sin descripción en fuente CAP"
                        : $"Área sin descripción en fuente CAP ({eventName})";
                    limitations.Add("SMN CAP supplied an empty areaDesc; the display label is explicitly marked as source-missing.");
                }

                var polygons = areaElement.Elements(CapNamespace + "polygon")
                    .Select(element => ParsePolygon(element.Value))
                    .ToArray();
                if (polygons.Length == 0)
                {
                    throw new CapSchemaException("Every CAP area must contain at least one polygon for spatial matching.");
                }

                areas.Add(new CapArea(description, ImmutableLists.Create(polygons)));
            }

            if (areas.Count == 0)
            {
                throw new CapSchemaException("CAP alert must contain at least one area.");
            }

            var lifecycleState = messageType switch
            {
                CapMessageType.Cancel => CapLifecycleState.Cancelled,
                _ when expires <= now => CapLifecycleState.Expired,
                CapMessageType.Update => CapLifecycleState.Updated,
                _ => CapLifecycleState.Active,
            };

            var alert = new CapAlert(
                sender,
                identifier,
                sent,
                messageType,
                status,
                scope,
                effective,
                expires,
                references,
                ImmutableLists.Create(areas),
                lifecycleState,
                Convert.ToHexStringLower(SHA256.HashData(bytes)),
                ImmutableLists.Create(limitations));

            WeatherDiagnostics.ParsedValues.Add(
                1,
                new KeyValuePair<string, object?>("weather.provider", "smn-cap"));
            activity?.SetTag("cap.message_type", messageType.ToString());
            activity?.SetTag("cap.lifecycle_state", lifecycleState.ToString());
            activity?.SetStatus(ActivityStatusCode.Ok);
            return ProviderParseResult.Success(alert);
        }
        catch (CapPayloadLimitException)
        {
            return Fail(
                new ProviderError(ProviderErrorCode.PayloadTooLarge, "CAP payload exceeded the 2 MiB safety limit."), activity);
        }
        catch (CapSchemaException exception)
        {
            return Fail(new ProviderError(ProviderErrorCode.SchemaInvalid, exception.Message), activity);
        }
        catch (XmlException exception)
        {
            return Fail(new ProviderError(ProviderErrorCode.SchemaInvalid, $"CAP XML is invalid at line {exception.LineNumber}."), activity);
        }
        finally
        {
            WeatherDiagnostics.ParseDuration.Record(
                Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                new KeyValuePair<string, object?>("weather.provider", "smn-cap"));
        }
    }

    private static XmlReaderSettings CreateSecureSettings() => new()
    {
        Async = false,
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersInDocument = MaximumPayloadBytes,
        MaxCharactersFromEntities = 0,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true,
    };

    private static byte[] ReadLimited(Stream payload, int maximumBytes)
    {
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        var total = 0;
        while (true)
        {
            var read = payload.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            total = checked(total + read);
            if (total > maximumBytes)
            {
                throw new CapPayloadLimitException();
            }

            output.Write(buffer, 0, read);
        }

        return output.ToArray();
    }

    private static string RequiredText(XElement parent, string localName) =>
        OptionalText(parent, localName) is { Length: > 0 } value
            ? value
            : throw new CapSchemaException($"CAP {localName} is required.");

    private static string? OptionalText(XElement parent, string localName)
    {
        var value = parent.Element(CapNamespace + localName)?.Value.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static DateTimeOffset RequiredTimestamp(XElement parent, string localName) =>
        OptionalTimestamp(parent, localName)
        ?? throw new CapSchemaException($"CAP {localName} must be an ISO 8601 timestamp with offset.");

    private static DateTimeOffset? OptionalTimestamp(XElement parent, string localName)
    {
        var value = OptionalText(parent, localName);
        if (value is null)
        {
            return null;
        }

        if (!HasExplicitOffset(value) || !DateTimeOffset.TryParseExact(
            value,
            ["yyyy-MM-dd'T'HH:mm:ssK", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var timestamp))
        {
            throw new CapSchemaException($"CAP {localName} must be an ISO 8601 timestamp with offset.");
        }

        return timestamp;
    }

    private static TEnum RequiredEnum<TEnum>(XElement parent, string localName)
        where TEnum : struct, Enum
    {
        var value = RequiredText(parent, localName);
        return Enum.TryParse<TEnum>(value, ignoreCase: false, out var parsed)
            ? parsed
            : throw new CapSchemaException($"CAP {localName} value is unsupported.");
    }

    private static IReadOnlyList<string> ParseReferences(string? value)
    {
        if (value is null)
        {
            return Array.Empty<string>();
        }

        var references = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (references.Distinct(StringComparer.Ordinal).Count() != references.Length)
        {
            throw new CapSchemaException("CAP references cannot contain duplicates.");
        }

        foreach (var reference in references)
        {
            if (!TryParseReference(reference, out _))
            {
                throw new CapSchemaException("CAP references must use sender,identifier,sent format.");
            }
        }

        return ImmutableLists.Create(references);
    }

    private static IReadOnlyList<GeoPosition> ParsePolygon(string value)
    {
        var tokens = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var positions = new List<GeoPosition>(tokens.Length);
        foreach (var token in tokens)
        {
            var components = token.Split(',', StringSplitOptions.TrimEntries);
            if (components.Length != 2 ||
                !double.TryParse(components[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
                !double.TryParse(components[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude) ||
                !double.IsFinite(latitude) ||
                !double.IsFinite(longitude))
            {
                throw new CapSchemaException("CAP polygon coordinates must use latitude,longitude decimal pairs.");
            }

            try
            {
                positions.Add(new GeoPosition(longitude, latitude));
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new CapSchemaException("CAP polygon contains an out-of-range coordinate.");
            }
        }

        if (positions.Count < 4 || positions[0] != positions[^1])
        {
            throw new CapSchemaException("CAP polygon must be a closed ring with at least four positions.");
        }

        return ImmutableLists.Create(positions);
    }

    internal static bool TryParseReference(string value, out CapReference reference)
    {
        reference = default;
        var lastComma = value.LastIndexOf(',');
        if (lastComma <= 0 || lastComma == value.Length - 1)
        {
            return false;
        }

        var beforeSent = value[..lastComma];
        var firstComma = beforeSent.IndexOf(',');
        var sentValue = value[(lastComma + 1)..];
        if (firstComma <= 0 || firstComma == beforeSent.Length - 1 ||
            !HasExplicitOffset(sentValue) ||
            !DateTimeOffset.TryParseExact(
                sentValue,
                ["yyyy-MM-dd'T'HH:mm:ssK", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var sent))
        {
            return false;
        }

        reference = new CapReference(beforeSent[..firstComma], beforeSent[(firstComma + 1)..], sent);
        return true;
    }

    private static bool HasExplicitOffset(string value)
    {
        if (value.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (value.Length < 6 || value[^3] != ':' || (value[^6] != '+' && value[^6] != '-'))
        {
            return false;
        }

        return char.IsDigit(value[^5]) &&
               char.IsDigit(value[^4]) &&
               char.IsDigit(value[^2]) &&
               char.IsDigit(value[^1]);
    }

    private static ProviderParseResult<CapAlert> Fail(ProviderError error, Activity? activity)
    {
        WeatherDiagnostics.ParseErrors.Add(
            1,
            new KeyValuePair<string, object?>("weather.provider", "smn-cap"),
            new KeyValuePair<string, object?>("error.type", error.Code.ToString()));
        activity?.SetTag("error.type", error.Code.ToString());
        activity?.SetStatus(ActivityStatusCode.Error, error.SafeMessage);
        return ProviderParseResult.Failure<CapAlert>(error);
    }

    internal readonly record struct CapReference(string Sender, string Identifier, DateTimeOffset Sent);

    private sealed class CapSchemaException(string message) : Exception(message);

    private sealed class CapPayloadLimitException : Exception;
}

public sealed class CapLifecycleTracker
{
    private readonly Dictionary<(string Sender, string Identifier, DateTimeOffset Sent), CapAlert> _history = new();
    private readonly Dictionary<
        (string Sender, string Identifier, DateTimeOffset Sent),
        (string Sender, string Identifier, DateTimeOffset Sent)> _rootByIdentity = new();
    private readonly Dictionary<
        (string Sender, string Identifier, DateTimeOffset Sent),
        CapAlert> _latestByRoot = new();

    public ProviderParseResult<CapAlert> Apply(CapAlert alert)
    {
        ArgumentNullException.ThrowIfNull(alert);

        if (alert.MessageType == CapMessageType.Alert)
        {
            var key = (alert.Sender, alert.Identifier, alert.Sent);
            if (_history.ContainsKey(key))
            {
                return OutOfOrder("CAP message identity already exists in append-only history.");
            }

            _history[key] = alert;
            _rootByIdentity[key] = key;
            _latestByRoot[key] = alert;
            return ProviderParseResult.Success(alert);
        }

        if (alert.References.Count == 0)
        {
            return OutOfOrder("CAP Update/Cancel must reference an existing message.");
        }

        var referencedRoots = new HashSet<(string Sender, string Identifier, DateTimeOffset Sent)>();
        foreach (var referenceText in alert.References)
        {
            if (!CapParser.TryParseReference(referenceText, out var reference))
            {
                return OutOfOrder("CAP reference format is invalid.");
            }

            if (!string.Equals(alert.Sender, reference.Sender, StringComparison.Ordinal))
            {
                return OutOfOrder("CAP Update/Cancel sender must match the referenced sender.");
            }

            var candidateKey = (reference.Sender, reference.Identifier, reference.Sent);
            if (!_history.ContainsKey(candidateKey) || !_rootByIdentity.TryGetValue(candidateKey, out var rootKey))
            {
                return OutOfOrder("CAP Update/Cancel references an unknown message.");
            }

            referencedRoots.Add(rootKey);
        }

        if (referencedRoots.Count != 1)
        {
            return OutOfOrder("CAP Update/Cancel cannot merge multiple lifecycle chains.");
        }

        var root = referencedRoots.Single();
        var latest = _latestByRoot[root];
        if (latest.LifecycleState == CapLifecycleState.Cancelled)
        {
            return OutOfOrder("A cancelled CAP lifecycle is terminal.");
        }

        if (alert.Sent <= latest.Sent)
        {
            return OutOfOrder("CAP Update/Cancel must be newer than the latest message in its lifecycle.");
        }

        var newIdentity = (alert.Sender, alert.Identifier, alert.Sent);
        if (_history.ContainsKey(newIdentity))
        {
            return OutOfOrder("CAP message identity already exists in append-only history.");
        }

        _history[newIdentity] = alert;
        _rootByIdentity[newIdentity] = root;
        _latestByRoot[root] = alert;
        return ProviderParseResult.Success(alert);
    }

    private static ProviderParseResult<CapAlert> OutOfOrder(string message) =>
        ProviderParseResult.Failure<CapAlert>(new ProviderError(ProviderErrorCode.OutOfOrder, message));
}
