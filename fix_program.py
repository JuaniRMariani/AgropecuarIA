import os

path = r'B:\Xenova\AgropecuarIA\apps\AgropecuarIA.Api\Program.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

flag = 'bool applyCatalogMigrations = builder.Configuration.GetValue<bool>("Catalog:ApplyMigrations");'
content = content.replace('WebApplication app = builder.Build();', flag + '\n\nWebApplication app = builder.Build();')

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
