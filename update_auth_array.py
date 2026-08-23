import json
import os

path = r'B:\Xenova\AgropecuarIA\tasks\evidence\AGRO-SEC-002\authorization-surface-register.json'
with open(path, 'r', encoding='utf-8') as f:
    data = json.load(f)

# Add GET /api/catalog/diff
data.append({
    "id": "catalog.diff.read",
    "method": "GET",
    "path": "/api/catalog/diff",
    "boundary": "public-platform",
    "resource": "catalog-diff",
    "action": "read",
    "authentication": "anonymous",
    "actorSource": "none",
    "tenantSource": "none",
    "applicationAuthorization": "none",
    "storageBoundary": "catalog-platform",
    "neutralErrors": "none",
    "owner": "Catalog",
    "tests": ["tests/AgropecuarIA.Catalog.Tests/CatalogDiffApplicationServiceTests.cs#DiffIsGenerated"]
})

# Add POST /api/catalog/ingest
data.append({
    "id": "catalog.ingest.create",
    "method": "POST",
    "path": "/api/catalog/ingest",
    "boundary": "public-platform",
    "resource": "catalog-ingest",
    "action": "create",
    "authentication": "anonymous",
    "actorSource": "none",
    "tenantSource": "none",
    "applicationAuthorization": "none",
    "storageBoundary": "catalog-platform",
    "neutralErrors": "none",
    "owner": "Catalog",
    "tests": ["tests/AgropecuarIA.Catalog.Tests/CatalogIngestionApplicationServiceTests.cs#IngestionIsIdempotent"]
})

# Add POST /api/organizations/{organizationId}/fields/{fieldId}/archive
data.append({
    "id": "productive-core.field.archive",
    "method": "POST",
    "path": "/api/organizations/{organizationId}/fields/{fieldId}/archive",
    "boundary": "tenant",
    "resource": "field",
    "action": "archive",
    "authentication": "session-cookie+csrf+idempotency-key",
    "actorSource": "server-session-claims-revalidated",
    "tenantSource": "server-derived-route-revalidated",
    "applicationAuthorization": "active field membership",
    "storageBoundary": "force-rls:actor+tenant",
    "neutralErrors": "foreign and missing tenant are neutral",
    "owner": "ProductiveCore",
    "tests": ["tests/AgropecuarIA.ProductiveCore.Tests/ProductiveCoreArchiveApplicationServiceTests.cs#ArchiveIsIdempotent"]
})

with open(path, 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=2)

