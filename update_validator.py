import os

path = r'B:\Xenova\AgropecuarIA\tasks\evidence\AGRO-FND-001\fitness\src\AgropecuarIA.ArchitectureFitness\AuthorizationSurfaceValidator.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

injection = '''        openApi.UnionWith(ExtractOpenApiOperations(
            Path.Combine(repositoryRoot, "contracts", "productive-core.openapi.yaml")));
        openApi.UnionWith(ExtractOpenApiOperations(
            Path.Combine(repositoryRoot, "contracts", "catalog.openapi.yaml")));'''

content = content.replace('        openApi.UnionWith(ExtractOpenApiOperations(\n            Path.Combine(repositoryRoot, "contracts", "productive-core.openapi.yaml")));', injection)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
