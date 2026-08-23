import json
import os

path = r'B:\Xenova\AgropecuarIA\tasks\evidence\AGRO-FND-001\runtime-map.json'
with open(path, 'r', encoding='utf-8') as f:
    data = json.load(f)

data["modules"].append({
    "moduleId": "catalog",
    "projectPath": "src/AgropecuarIA.Catalog/AgropecuarIA.Catalog.csproj",
    "databaseSchema": "catalog",
    "contracts": []
})

data["compositionRoots"][0]["allowedModuleIds"].append("catalog")

with open(path, 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=2)
