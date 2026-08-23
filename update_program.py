import os
import re

path = r'B:\Xenova\AgropecuarIA\apps\AgropecuarIA.Api\Program.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Add usings
usings = '''using AgropecuarIA.Catalog;
using AgropecuarIA.Catalog.Delivery;
using AgropecuarIA.Catalog.Infrastructure;'''
content = content.replace('using AgropecuarIA.Territory.Infrastructure;', 'using AgropecuarIA.Territory.Infrastructure;\n' + usings)

# Add migrations flag
flag = 'bool applyCatalogMigrations = builder.Configuration.GetValue<bool>("ApplyMigrations:Catalog");'
content = content.replace('bool applyProductiveCoreMigrations = builder.Configuration.GetValue<bool>("ApplyMigrations:ProductiveCore");', 'bool applyProductiveCoreMigrations = builder.Configuration.GetValue<bool>("ApplyMigrations:ProductiveCore");\n' + flag)

# Add module registration
reg = 'builder.Services.AddCatalogModule(builder.Configuration);'
content = content.replace('builder.Services.AddProductiveCoreModule(builder.Configuration);', 'builder.Services.AddProductiveCoreModule(builder.Configuration);\n' + reg)

# Add apply migrations block
migrations = '''if (applyCatalogMigrations)
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    CatalogDbContext catalogDbContext =
        scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await catalogDbContext.Database.MigrateAsync();
}'''
content = content.replace('app.UseExceptionHandler();', migrations + '\n\napp.UseExceptionHandler();')

# Add MapCatalogEndpoints
map_endpoints = 'app.MapCatalogEndpoints();'
content = content.replace('app.MapProductiveCoreEndpoints();', 'app.MapProductiveCoreEndpoints();\n' + map_endpoints)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
