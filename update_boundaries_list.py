import json
import os

path = r'B:\Xenova\AgropecuarIA\tasks\evidence\AGRO-FND-001\module-boundaries.json'
with open(path, 'r', encoding='utf-8') as f:
    data = json.load(f)

data["modules"].append({
    "id": "catalog",
    "projectPath": "src/AgropecuarIA.Catalog/AgropecuarIA.Catalog.csproj",
    "databaseSchema": "catalog",
    "allowedDependencies": [
        "src/AgropecuarIA.Identity/AgropecuarIA.Identity.csproj"
    ]
})

with open(path, 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=2)
