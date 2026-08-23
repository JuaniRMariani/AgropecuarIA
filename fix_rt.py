import sys
import json

path_rt = r'B:\Xenova\AgropecuarIA\tasks\evidence\AGRO-FND-001\runtime-map.json'
with open(path_rt, 'r', encoding='utf-8') as f:
    rt = json.load(f)

for m in rt['modules']:
    if m['moduleId'] == 'national-catalog':
        m['projectPath'] = 'src/AgropecuarIA.Catalog/AgropecuarIA.Catalog.csproj'
        m['databaseSchema'] = 'catalog'
        m['contracts'] = []

for root in rt.get('compositionRoots', []):
    deps = root.get('allowedDependencies', [])
    if 'catalog' in deps:
        deps.remove('catalog')
        if 'national-catalog' not in deps:
            deps.append('national-catalog')

with open(path_rt, 'w', encoding='utf-8') as f:
    json.dump(rt, f, indent=2)

with open(path_rt, 'r', encoding='utf-8') as f:
    content = f.read().replace('\r\n', '\n')
with open(path_rt, 'w', encoding='utf-8') as f:
    f.write(content)
