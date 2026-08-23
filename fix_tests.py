import sys
import json

# 1. Update module-boundaries.json to fix 'national-catalog' vs 'catalog'
path_mb = r'B:\Xenova\AgropecuarIA\tasks\evidence\AGRO-FND-001\module-boundaries.json'
with open(path_mb, 'r', encoding='utf-8') as f:
    mb = json.load(f)

for m in mb['modules']:
    if m['id'] == 'national-catalog':
        m['id'] = 'catalog'

with open(path_mb, 'w', encoding='utf-8') as f:
    json.dump(mb, f, indent=2)

# 2. Update runtime-map.json
path_rt = r'B:\Xenova\AgropecuarIA\tasks\evidence\AGRO-FND-001\runtime-map.json'
with open(path_rt, 'r', encoding='utf-8') as f:
    rt = json.load(f)

for m in rt['modules']:
    if m['moduleId'] == 'national-catalog':
        m['moduleId'] = 'catalog'

for root in rt.get('compositionRoots', []):
    deps = root.get('allowedDependencies', [])
    if 'national-catalog' in deps:
        deps.remove('national-catalog')
        if 'catalog' not in deps:
            deps.append('catalog')

with open(path_rt, 'w', encoding='utf-8') as f:
    json.dump(rt, f, indent=2)

# 3. Update EvidenceFixture to copy catalog openapi and endpoints
path_ef = r'B:\Xenova\AgropecuarIA\tasks\evidence\AGRO-FND-001\fitness\tests\AgropecuarIA.ArchitectureFitness.Tests\AuthorizationSurfaceContractTests.cs'
with open(path_ef, 'r', encoding='utf-8') as f:
    ef = f.read()

if 'contracts/catalog.openapi.yaml' not in ef:
    ef = ef.replace(
        '"contracts/productive-core.openapi.yaml",',
        '"contracts/productive-core.openapi.yaml",\n            "contracts/catalog.openapi.yaml",'
    )
if 'apps/AgropecuarIA.Api/CatalogEndpoints.cs' not in ef:
    ef = ef.replace(
        '"src/AgropecuarIA.ProductiveCore/Delivery/ProductiveCoreEndpoints.cs",',
        '"src/AgropecuarIA.ProductiveCore/Delivery/ProductiveCoreEndpoints.cs",\n            "apps/AgropecuarIA.Api/CatalogEndpoints.cs",'
    )

with open(path_ef, 'w', encoding='utf-8') as f:
    f.write(ef)

