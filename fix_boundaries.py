import json
import os

path = r'B:\Xenova\AgropecuarIA\tasks\evidence\AGRO-FND-001\module-boundaries.json'
with open(path, 'r', encoding='utf-8') as f:
    data = json.load(f)

for mod in data["modules"]:
    if mod["id"] == "catalog":
        mod["allowedDependencies"] = ["identity-tenancy"]

with open(path, 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=2)
