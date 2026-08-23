import json
import os

auth_path = r'B:\Xenova\AgropecuarIA\tasks\evidence\AGRO-FND-001\authorization-register.json'
with open(auth_path, 'r', encoding='utf-8') as f:
    auth_data = json.load(f)

auth_data["GET /api/catalog/diff"] = {
    "module": "catalog",
    "permission": "catalog.editorial.read",
    "schemes": ["Identity.Cookie"],
    "description": "Calculates the editorial diff of the catalog."
}
auth_data["POST /api/catalog/ingest"] = {
    "module": "catalog",
    "permission": "catalog.editorial.write",
    "schemes": ["Identity.Cookie", "pat-bearer"],
    "description": "Ingests a source into the catalog staging area."
}
auth_data["POST /api/organizations/{organizationId}/fields/{fieldId}/archive"] = {
    "module": "productive-core",
    "permission": "productive-core.field.write",
    "schemes": ["Identity.Cookie"],
    "description": "Archives a field draft."
}

with open(auth_path, 'w', encoding='utf-8') as f:
    json.dump(auth_data, f, indent=2)

runtime_path = r'B:\Xenova\AgropecuarIA\tasks\evidence\AGRO-FND-001\runtime-map.json'
with open(runtime_path, 'r', encoding='utf-8') as f:
    runtime_data = json.load(f)

# Find the "productModules" section and add the new module
runtime_data["productModules"]["src/AgropecuarIA.Catalog/AgropecuarIA.Catalog.csproj"] = {
    "allowedDependencies": [
        "src/AgropecuarIA.Identity/AgropecuarIA.Identity.csproj"
    ]
}

with open(runtime_path, 'w', encoding='utf-8') as f:
    json.dump(runtime_data, f, indent=2)
