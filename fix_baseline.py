import sys
import json

# Fix module-boundaries.json
path_mb = r'B:\Xenova\AgropecuarIA\tasks\evidence\AGRO-FND-001\module-boundaries.json'
with open(path_mb, 'r', encoding='utf-8') as f:
    mb = json.load(f)

for m in mb['modules']:
    if m['id'] == 'national-catalog':
        m['projectPath'] = 'src/AgropecuarIA.Catalog/AgropecuarIA.Catalog.csproj'
        # ensure databaseSchema is 'catalog'
        m['databaseSchema'] = 'catalog'

# Remove 'catalog'
mb['modules'] = [m for m in mb['modules'] if m['id'] != 'catalog']

with open(path_mb, 'w', encoding='utf-8') as f:
    json.dump(mb, f, indent=2)

# Fix runtime-map.json
path_rt = r'B:\Xenova\AgropecuarIA\tasks\evidence\AGRO-FND-001\runtime-map.json'
with open(path_rt, 'r', encoding='utf-8') as f:
    rt = json.load(f)

# Update national-catalog
found_nc = False
for m in rt['modules']:
    if m['moduleId'] == 'national-catalog':
        m['projectPath'] = 'src/AgropecuarIA.Catalog/AgropecuarIA.Catalog.csproj'
        m['databaseSchema'] = 'catalog'
        found_nc = True

# Remove catalog
rt['modules'] = [m for m in rt['modules'] if m['moduleId'] != 'catalog']

# Fix composition roots
for root in rt.get('compositionRoots', []):
    deps = root.get('allowedDependencies', [])
    if 'catalog' in deps:
        deps.remove('catalog')
        if 'national-catalog' not in deps:
            deps.append('national-catalog')

with open(path_rt, 'w', encoding='utf-8') as f:
    json.dump(rt, f, indent=2)

# Convert to LF
for path in [path_mb, path_rt]:
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read().replace('\r\n', '\n')
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
