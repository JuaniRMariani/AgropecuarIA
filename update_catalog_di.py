import os

path = r'B:\Xenova\AgropecuarIA\src\AgropecuarIA.Catalog\CatalogServiceCollectionExtensions.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Add usings
usings = '''using AgropecuarIA.Catalog.Application;
'''
content = content.replace('using AgropecuarIA.Catalog.Infrastructure;', usings + 'using AgropecuarIA.Catalog.Infrastructure;')

# Add registrations
regs = '''        services.AddScoped<CatalogIngestionApplicationService>();
        services.AddScoped<CatalogDiffApplicationService>();'''
content = content.replace('return services;', regs + '\n\n        return services;')

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
