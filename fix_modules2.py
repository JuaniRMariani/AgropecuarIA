import sys
import json

path = r'B:\Xenova\AgropecuarIA\tasks\evidence\AGRO-FND-001\module-boundaries.json'
with open(path, 'r', encoding='utf-8') as f:
    data = json.load(f)

# Find 'national-catalog' and update projectPath
for m in data['modules']:
    if m['id'] == 'national-catalog':
        m['projectPath'] = 'src/AgropecuarIA.Catalog/AgropecuarIA.Catalog.csproj'
        break

# Remove 'catalog'
data['modules'] = [m for m in data['modules'] if m['id'] != 'catalog']

with open(path, 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=2)

path_runtime = r'B:\Xenova\AgropecuarIA\tasks\evidence\AGRO-FND-001\runtime-map.json'
with open(path_runtime, 'r', encoding='utf-8') as f:
    data_rt = json.load(f)

# Rename 'catalog' to 'national-catalog' in modules if it exists
for m in data_rt['modules']:
    if m.get('moduleId') == 'catalog':
        m['moduleId'] = 'national-catalog'
        break

# Also replace in compositionRoots allowedDependencies
for root in data_rt.get('compositionRoots', []):
    deps = root.get('allowedDependencies', [])
    if 'catalog' in deps:
        deps.remove('catalog')
        if 'national-catalog' not in deps:
            deps.append('national-catalog')

with open(path_runtime, 'w', encoding='utf-8') as f:
    json.dump(data_rt, f, indent=2)
