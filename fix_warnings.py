import sys

path = r'B:\Xenova\AgropecuarIA\src\AgropecuarIA.Identity\Application\MfaApplicationService.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Fix unused variable
content = content.replace('int bitIndex = 0;\n', '')

# Fix CA1305 (CultureInfo)
content = content.replace('using System.Text;', 'using System.Text;\nusing System.Globalization;')
content = content.replace('return code.ToString("D6");', 'return code.ToString("D6", CultureInfo.InvariantCulture);')

# Fix CA5350 (Weak Crypto for TOTP)
content = content.replace('using var hmac = new HMACSHA1(secret);', '#pragma warning disable CA5350\n        using var hmac = new HMACSHA1(secret);\n#pragma warning restore CA5350')

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
