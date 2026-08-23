import os

files = [
    r'B:\Xenova\AgropecuarIA\src\AgropecuarIA.ProductiveCore\Application\ProductiveCorePorts.cs',
    r'B:\Xenova\AgropecuarIA\src\AgropecuarIA.ProductiveCore\Delivery\ProductiveCoreEndpoints.cs',
    r'B:\Xenova\AgropecuarIA\src\AgropecuarIA.ProductiveCore\Infrastructure\PostgresProductiveCoreUnitOfWork.cs'
]

for file in files:
    with open(file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Python's raw string literal issues from powershell. Let's just fix the exact replacement: '\n' as literal string backslash-n instead of newline.
    content = content.replace('\\n', '\n')
    
    with open(file, 'w', encoding='utf-8') as f:
        f.write(content)
