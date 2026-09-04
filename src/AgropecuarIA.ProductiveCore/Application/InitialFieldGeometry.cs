using System.Text;
using System.Text.Json;

namespace AgropecuarIA.ProductiveCore.Application;

public sealed record ValidatedFieldGeometry(
    string BoundaryGeoJson, byte[] Ewkb, decimal CalculatedAreaHectares,
    double CentroidLatitude, double CentroidLongitude);

public sealed record InitialFieldGeometrySnapshot(
    Guid Id, Guid OrganizationId, Guid FieldId, Guid ActorUserId, Guid SessionId,
    decimal DeclaredAreaHectares, ValidatedFieldGeometry Geometry, long Revision,
    DateTimeOffset ConfiguredAtUtc, Guid JournalEntryId, Guid OutboxMessageId);

/// <summary>Cheap transport checks before invoking the authoritative PostGIS topology validator.</summary>
public static class InitialFieldGeometryInput
{
    public const int MaximumUtf8Bytes = 1024 * 1024;
    public const int MaximumPositions = 10000;

    public static void Validate(string geoJson, decimal declaredAreaHectares)
    {
        if (string.IsNullOrWhiteSpace(geoJson) || declaredAreaHectares <= 0 ||
            declaredAreaHectares > 99999999999999.9999m || decimal.Round(declaredAreaHectares, 4) != declaredAreaHectares)
        {
            throw ProductiveCoreErrors.InvalidGeometry();
        }

        if (Encoding.UTF8.GetByteCount(geoJson) > MaximumUtf8Bytes)
        {
            throw ProductiveCoreErrors.GeometryTooLarge();
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(geoJson, new JsonDocumentOptions { MaxDepth = 10 });
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                !root.TryGetProperty("type", out JsonElement type) || type.ValueKind != JsonValueKind.String ||
                !root.TryGetProperty("coordinates", out JsonElement coordinates))
            {
                throw ProductiveCoreErrors.InvalidGeometry();
            }

            int positions = 0;
            switch (type.GetString())
            {
                case "Polygon":
                    ValidatePolygon(coordinates, ref positions);
                    break;
                case "MultiPolygon":
                    RequireNonemptyArray(coordinates);
                    foreach (JsonElement polygon in coordinates.EnumerateArray())
                    {
                        ValidatePolygon(polygon, ref positions);
                    }

                    break;
                default:
                    throw ProductiveCoreErrors.InvalidGeometry();
            }
        }
        catch (JsonException)
        {
            throw ProductiveCoreErrors.InvalidGeometry();
        }
    }

    private static void ValidatePolygon(JsonElement polygon, ref int count)
    {
        RequireNonemptyArray(polygon);
        foreach (JsonElement ring in polygon.EnumerateArray())
        {
            if (ring.ValueKind != JsonValueKind.Array || ring.GetArrayLength() < 4)
            {
                throw ProductiveCoreErrors.InvalidGeometry();
            }

            (double Longitude, double Latitude) first = default;
            (double Longitude, double Latitude) last = default;
            bool isFirst = true;
            foreach (JsonElement position in ring.EnumerateArray())
            {
                if (++count > MaximumPositions)
                {
                    throw ProductiveCoreErrors.GeometryTooLarge();
                }

                if (position.ValueKind != JsonValueKind.Array || position.GetArrayLength() != 2 ||
                    position[0].ValueKind != JsonValueKind.Number || position[1].ValueKind != JsonValueKind.Number ||
                    !position[0].TryGetDouble(out double longitude) || !position[1].TryGetDouble(out double latitude) ||
                    !double.IsFinite(longitude) || !double.IsFinite(latitude) ||
                    longitude is < -180 or > 180 || latitude is < -90 or > 90)
                {
                    throw ProductiveCoreErrors.InvalidGeometry();
                }

                last = (longitude, latitude);
                if (isFirst)
                {
                    first = last;
                    isFirst = false;
                }
            }

            if (first != last)
            {
                throw ProductiveCoreErrors.InvalidGeometry();
            }
        }
    }

    private static void RequireNonemptyArray(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array || element.GetArrayLength() == 0)
        {
            throw ProductiveCoreErrors.InvalidGeometry();
        }
    }
}
