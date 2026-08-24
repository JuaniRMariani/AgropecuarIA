namespace AgropecuarIA.ProductiveCore.Domain;

public static class ProductionCycleStatuses
{
    public const string Active = "active";
    public const string Closed = "closed";
    public const string Canceled = "canceled";

    public static bool IsValid(string status) =>
        status is Active or Closed or Canceled;
}

public static class ProductionOrigins
{
    public const string Manual = "manual";
    public const string Importacion = "importacion";
    public const string Dispositivo = "dispositivo";
    public const string Calculo = "calculo";
    public const string IA = "ia";

    public static bool IsValid(string origin) =>
        origin is Manual or Importacion or Dispositivo or Calculo or IA;
}

public sealed class ProductionCycle
{
    private ProductionCycle() { }

    public ProductionCycle(
        Guid id,
        Guid organizationId,
        Guid managementUnitId,
        string catalogCode,
        string catalogDisplayName,
        string purpose,
        string system,
        string supportLevel,
        DateTimeOffset startDateUtc,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));
        if (managementUnitId == Guid.Empty)
            throw new ArgumentException("ManagementUnitId is required.", nameof(managementUnitId));

        ArgumentException.ThrowIfNullOrWhiteSpace(catalogCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogDisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(system);

        Id = id;
        OrganizationId = organizationId;
        ManagementUnitId = managementUnitId;
        CatalogCode = catalogCode.Trim().ToUpperInvariant();
        CatalogDisplayName = catalogDisplayName.Trim();
        Purpose = purpose.Trim();
        System = system.Trim();
        SupportLevel = string.IsNullOrWhiteSpace(supportLevel) ? "FLUJO_GENERICO" : supportLevel.Trim().ToUpperInvariant();
        Status = ProductionCycleStatuses.Active;
        StartDateUtc = startDateUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid ManagementUnitId { get; private set; }
    public string CatalogCode { get; private set; } = string.Empty;
    public string CatalogDisplayName { get; private set; } = string.Empty;
    public string Purpose { get; private set; } = string.Empty;
    public string System { get; private set; } = string.Empty;
    public string SupportLevel { get; private set; } = "FLUJO_GENERICO";
    public string Status { get; private set; } = ProductionCycleStatuses.Active;
    public DateTimeOffset StartDateUtc { get; private set; }
    public DateTimeOffset? EndDateUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void Close(DateTimeOffset endDateUtc)
    {
        if (Status != ProductionCycleStatuses.Active)
            throw new InvalidOperationException($"Cannot close cycle in status '{Status}'.");

        if (endDateUtc < StartDateUtc)
            throw new ArgumentException("End date cannot be earlier than start date.", nameof(endDateUtc));

        Status = ProductionCycleStatuses.Closed;
        EndDateUtc = endDateUtc;
    }

    public void Cancel()
    {
        if (Status != ProductionCycleStatuses.Active)
            throw new InvalidOperationException($"Cannot cancel cycle in status '{Status}'.");

        Status = ProductionCycleStatuses.Canceled;
    }
}

public sealed class ProductionEvent
{
    private ProductionEvent() { }

    public ProductionEvent(
        Guid id,
        Guid organizationId,
        Guid productionCycleId,
        string eventType,
        DateTimeOffset effectiveDateUtc,
        DateTimeOffset recordedAtUtc,
        decimal? quantity,
        string? unit,
        string? notes,
        string origin)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));
        if (productionCycleId == Guid.Empty)
            throw new ArgumentException("ProductionCycleId is required.", nameof(productionCycleId));

        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        string effectiveOrigin = string.IsNullOrWhiteSpace(origin) ? ProductionOrigins.Manual : origin.Trim().ToLowerInvariant();
        if (!ProductionOrigins.IsValid(effectiveOrigin))
            throw new ArgumentException($"Invalid origin: {origin}", nameof(origin));

        if (quantity.HasValue && string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Unit is required when quantity is specified.", nameof(unit));

        Id = id;
        OrganizationId = organizationId;
        ProductionCycleId = productionCycleId;
        EventType = eventType.Trim();
        EffectiveDateUtc = effectiveDateUtc;
        RecordedAtUtc = recordedAtUtc;
        Quantity = quantity;
        Unit = unit?.Trim();
        Notes = notes?.Trim();
        Origin = effectiveOrigin;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid ProductionCycleId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public DateTimeOffset EffectiveDateUtc { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }
    public decimal? Quantity { get; private set; }
    public string? Unit { get; private set; }
    public string? Notes { get; private set; }
    public string Origin { get; private set; } = ProductionOrigins.Manual;
}
