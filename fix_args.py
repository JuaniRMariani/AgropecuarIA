import os

path = r'B:\Xenova\AgropecuarIA\src\AgropecuarIA.ProductiveCore\Application\ProductiveCoreArchiveApplicationService.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Fix the first new ArchivedManagementUnitResult
# Replace empty lines and field.UnitType with field.DisplayName, field.UnitType
content = content.replace(
'''            field.OrganizationId,
            
            field.UnitType,''',
'''            field.OrganizationId,
            field.DisplayName,
            field.UnitType,'''
)

# Fix the second new ArchivedManagementUnitResult at the end of the file
content = content.replace(
'''        return new ArchivedManagementUnitResult(
            field.Id,
            field.OrganizationId,
            
            field.UnitType,''',
'''        return new ArchivedManagementUnitResult(
            field.Id,
            field.OrganizationId,
            field.DisplayName,
            field.UnitType,'''
)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
