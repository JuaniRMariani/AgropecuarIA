using AgropecuarIA.ProductiveCore.Domain;

namespace AgropecuarIA.ProductiveCore.Application;

public sealed class ProductiveCoreGeometryApplicationService(
    IProductiveCoreUnitOfWorkFactory unitOfWorkFactory, TimeProvider timeProvider)
{
    public async Task<ConfiguredFieldGeometryResult> ConfigureGeometryAsync(
        ConfigureFieldGeometryCommand command, ProductiveRequestContext requestContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        RequireScope(command.OrganizationId, requestContext);
        try
        {
            await using IProductiveCoreUnitOfWork work = await unitOfWorkFactory.BeginAsync(
                ProductiveTransactionMode.SerializableWrite, cancellationToken);
            if (await work.AuthorizeOwnerAsync(requestContext, cancellationToken) is null)
            {
                throw ProductiveCoreErrors.FieldNotAvailable();
            }

            ManagementUnit field = await work.GetManagementUnitForUpdateAsync(command.OrganizationId, command.FieldId, cancellationToken)
                ?? throw ProductiveCoreErrors.FieldNotAvailable();
            if (field.Version != command.ExpectedVersion)
            {
                throw ProductiveCoreErrors.FieldVersionStale();
            }

            if (field.Status != ManagementUnitStatuses.Draft || field.SpatialStatus != ManagementUnitSpatialStatuses.NotConfigured)
            {
                throw ProductiveCoreErrors.GeometryAlreadyConfigured();
            }

            InitialFieldGeometryInput.Validate(command.BoundaryGeoJson, command.DeclaredAreaHectares);
            ValidatedFieldGeometry geometry = await work.ValidateInitialGeometryAsync(command.BoundaryGeoJson, cancellationToken);
            DateTimeOffset now = timeProvider.GetUtcNow();
            now = new DateTimeOffset(now.Ticks - (now.Ticks % 10), TimeSpan.Zero);
            field.ConfigureSpatialGeometry(geometry.BoundaryGeoJson, command.DeclaredAreaHectares,
                geometry.CalculatedAreaHectares, geometry.CentroidLatitude, geometry.CentroidLongitude,
                null, null, command.ExpectedVersion, Guid.NewGuid());
            Guid snapshotId = Guid.NewGuid();
            ProductiveJournalEntry journal = ProductiveJournalEntry.CreateManagementUnitGeometryConfigured(
                Guid.NewGuid(), command.OrganizationId, requestContext.ActorUserId, requestContext.SessionId,
                requestContext.CorrelationId, now);
            ProductiveOutboxMessage outbox = ProductiveOutboxMessage.CreateManagementUnitGeometryConfigured(
                Guid.NewGuid(), requestContext.CorrelationId,
                new ManagementUnitGeometryConfiguredIntegrationEventPayload(command.OrganizationId, field.Id,
                    snapshotId, field.Revision, field.SpatialStatus, command.DeclaredAreaHectares,
                    geometry.CalculatedAreaHectares, now, "postgis-geography-spheroid"));
            var snapshot = new InitialFieldGeometrySnapshot(snapshotId, command.OrganizationId, field.Id,
                requestContext.ActorUserId, requestContext.SessionId, command.DeclaredAreaHectares, geometry,
                field.Revision, now, journal.Id, outbox.Id);
            await work.AddInitialGeometryAsync(snapshot, journal, outbox, cancellationToken);
            await work.SaveChangesAsync(cancellationToken);
            await work.CommitAsync(cancellationToken);
            return ToResult(field, snapshot);
        }
        catch (Exception exception) when (exception is ProductiveSerializationRaceException or ProductiveStaleVersionException or ManagementUnitVersionConflictException)
        {
            throw ProductiveCoreErrors.FieldVersionStale();
        }
        catch (Exception exception) when (IsUnavailable(exception))
        {
            throw ProductiveCoreErrors.GeometryUnavailable();
        }
    }

    public async Task<ConfiguredFieldGeometryResult> GetGeometryAsync(
        Guid organizationId, Guid fieldId, ProductiveRequestContext requestContext, CancellationToken cancellationToken)
    {
        RequireScope(organizationId, requestContext);
        try
        {
            await using IProductiveCoreUnitOfWork work = await unitOfWorkFactory.BeginAsync(ProductiveTransactionMode.Read, cancellationToken);
            if (await work.AuthorizeOwnerAsync(requestContext, cancellationToken) is null)
            {
                throw ProductiveCoreErrors.FieldNotAvailable();
            }

            InitialFieldGeometrySnapshot snapshot = await work.GetInitialGeometryAsync(organizationId, fieldId, cancellationToken)
                ?? throw ProductiveCoreErrors.FieldNotAvailable();
            // Read the immutable snapshot first: a concurrent initial commit cannot be paired with a pre-configuration field.
            ManagementUnit field = await work.GetManagementUnitAsync(organizationId, fieldId, cancellationToken)
                ?? throw ProductiveCoreErrors.FieldNotAvailable();
            await work.CommitAsync(cancellationToken);
            return ToResult(field, snapshot);
        }
        catch (Exception exception) when (IsUnavailable(exception))
        {
            throw ProductiveCoreErrors.GeometryUnavailable();
        }
    }

    private static ConfiguredFieldGeometryResult ToResult(ManagementUnit field, InitialFieldGeometrySnapshot snapshot) =>
        new(field.Id, field.OrganizationId, field.DisplayName, field.UnitType, field.Status, field.SpatialStatus,
            snapshot.DeclaredAreaHectares, snapshot.Geometry.CalculatedAreaHectares, snapshot.Geometry.CentroidLatitude,
            snapshot.Geometry.CentroidLongitude, snapshot.Geometry.BoundaryGeoJson, null, null,
            field.CreatedAtUtc, field.Revision, field.Version, snapshot.Id, snapshot.ConfiguredAtUtc);

    private static void RequireScope(Guid organizationId, ProductiveRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (organizationId == Guid.Empty || context.OrganizationId != organizationId)
        {
            throw ProductiveCoreErrors.FieldNotAvailable();
        }
    }

    private static bool IsUnavailable(Exception exception) =>
        exception is ProductivePersistenceUnavailableException or ProductiveCommitOutcomeUnknownException or TimeoutException;
}
