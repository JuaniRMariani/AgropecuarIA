using AgropecuarIA.ProductiveCore.Application;
using AgropecuarIA.ProductiveCore.Domain;
using Npgsql;

namespace AgropecuarIA.ProductiveCore.Infrastructure;

internal sealed partial class PostgresProductiveCoreUnitOfWork
{
    public async Task<ValidatedFieldGeometry> ValidateInitialGeometryAsync(string geoJson, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = new NpgsqlCommand("""
                WITH input AS MATERIALIZED (SELECT public.ST_GeomFromGeoJSON(@json) AS g),
                valid AS MATERIALIZED (
                    SELECT public.ST_Multi(g) AS g FROM input
                    WHERE public.ST_SRID(g) = 4326 AND public.ST_NDims(g) = 2
                      AND public.GeometryType(g) IN ('POLYGON', 'MULTIPOLYGON')
                      AND NOT public.ST_IsEmpty(g) AND public.ST_IsValid(g, 0)
                      AND public.ST_NPoints(g) BETWEEN 4 AND 10000
                      AND public.ST_XMin(public.Box3D(g)) >= -180 AND public.ST_XMax(public.Box3D(g)) <= 180
                      AND public.ST_YMin(public.Box3D(g)) >= -90 AND public.ST_YMax(public.Box3D(g)) <= 90),
                facts AS MATERIALIZED (
                    SELECT g, round((public.ST_Area(g::public.geography, true) / 10000)::numeric, 4) AS area,
                        public.ST_Centroid(g::public.geography, true)::public.geometry AS centroid FROM valid)
                SELECT public.ST_AsGeoJSON(g, 17, 0), public.ST_AsEWKB(g), area,
                    public.ST_Y(centroid), public.ST_X(centroid)
                FROM facts WHERE area > 0 AND area <= 99999999999999.9999
                    AND public.ST_AsEWKB(public.ST_GeomFromGeoJSON(public.ST_AsGeoJSON(g, 17, 0))) = public.ST_AsEWKB(g)
                """, await GetOpenConnectionAsync(cancellationToken), GetTransaction());
            command.Parameters.AddWithValue("json", geoJson);
            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw ProductiveCoreErrors.InvalidGeometry();
            }

            return new ValidatedFieldGeometry(reader.GetString(0), reader.GetFieldValue<byte[]>(1), reader.GetDecimal(2),
                reader.GetDouble(3), reader.GetDouble(4));
        }
        catch (PostgresException exception) when (exception.SqlState is "XX000" or "22023" or "22P02")
        {
            throw ProductiveCoreErrors.InvalidGeometry();
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw Unavailable("validate geometry; pre-provision PostGIS in the database", exception);
        }
    }

    public async Task AddInitialGeometryAsync(InitialFieldGeometrySnapshot snapshot,
        ProductiveJournalEntry journalEntry, ProductiveOutboxMessage outboxMessage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(journalEntry);
        ArgumentNullException.ThrowIfNull(outboxMessage);
        try
        {
            await using var command = new NpgsqlCommand("""
                INSERT INTO productive_core.management_unit_geometry_versions
                    ("Id", "OrganizationId", "ManagementUnitId", "ActorUserId", "SessionId", "Boundary",
                     "DeclaredAreaHectares", "Revision", "ConfiguredAtUtc", "JournalEntryId", "OutboxMessageId")
                VALUES (@id, @organization, @field, @actor, @session, public.ST_GeomFromEWKB(@ewkb),
                        @declared, @revision, @now, @journal, @outbox)
                """, await GetOpenConnectionAsync(cancellationToken), GetTransaction());
            command.Parameters.AddWithValue("id", snapshot.Id);
            command.Parameters.AddWithValue("organization", snapshot.OrganizationId);
            command.Parameters.AddWithValue("field", snapshot.FieldId);
            command.Parameters.AddWithValue("actor", snapshot.ActorUserId);
            command.Parameters.AddWithValue("session", snapshot.SessionId);
            command.Parameters.AddWithValue("ewkb", snapshot.Geometry.Ewkb);
            command.Parameters.AddWithValue("declared", snapshot.DeclaredAreaHectares);
            command.Parameters.AddWithValue("revision", snapshot.Revision);
            command.Parameters.AddWithValue("now", snapshot.ConfiguredAtUtc);
            command.Parameters.AddWithValue("journal", snapshot.JournalEntryId);
            command.Parameters.AddWithValue("outbox", snapshot.OutboxMessageId);
            await command.ExecuteNonQueryAsync(cancellationToken);
            dbContext.ProductiveJournalEntries.Add(journalEntry);
            dbContext.ProductiveOutboxMessages.Add(outboxMessage);
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw Unavailable("persist immutable initial geometry", exception);
        }
    }

    public async Task<InitialFieldGeometrySnapshot?> GetInitialGeometryAsync(Guid organizationId, Guid fieldId, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = new NpgsqlCommand("""
                SELECT "Id", "ActorUserId", "SessionId", "DeclaredAreaHectares", "Revision", "ConfiguredAtUtc",
                    "JournalEntryId", "OutboxMessageId", public.ST_AsGeoJSON("Boundary", 17, 0),
                    public.ST_AsEWKB("Boundary"), "CalculatedAreaHectares", "CentroidLatitude", "CentroidLongitude"
                FROM productive_core.management_unit_geometry_versions
                WHERE "OrganizationId" = @organization AND "ManagementUnitId" = @field
                """, await GetOpenConnectionAsync(cancellationToken), GetTransaction());
            command.Parameters.AddWithValue("organization", organizationId);
            command.Parameters.AddWithValue("field", fieldId);
            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new InitialFieldGeometrySnapshot(reader.GetGuid(0), organizationId, fieldId, reader.GetGuid(1),
                reader.GetGuid(2), reader.GetDecimal(3),
                new ValidatedFieldGeometry(reader.GetString(8), reader.GetFieldValue<byte[]>(9), reader.GetDecimal(10),
                    reader.GetDouble(11), reader.GetDouble(12)), reader.GetInt64(4), reader.GetFieldValue<DateTimeOffset>(5),
                reader.GetGuid(6), reader.GetGuid(7));
        }
        catch (Exception exception) when (IsPersistenceFailure(exception))
        {
            throw Unavailable("read immutable initial geometry", exception);
        }
    }
}
