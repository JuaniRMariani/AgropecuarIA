import sys

path = r'B:\Xenova\AgropecuarIA\src\AgropecuarIA.Identity\Domain\IdentityModel.cs'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

new_classes = '''
public sealed class UserTotpCredential
{
    private UserTotpCredential() { }

    public UserTotpCredential(Guid userId, string protectedSecret)
    {
        UserId = userId;
        ProtectedSecret = protectedSecret;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }
    public string ProtectedSecret { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
}

public sealed class UserPasskeyCredential
{
    private UserPasskeyCredential() { }

    public UserPasskeyCredential(
        Guid userId,
        byte[] credentialId,
        byte[] publicKey,
        uint signCount,
        Guid aaguid)
    {
        UserId = userId;
        CredentialId = credentialId;
        PublicKey = publicKey;
        SignCount = signCount;
        Aaguid = aaguid;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }
    public byte[] CredentialId { get; private set; } = [];
    public byte[] PublicKey { get; private set; } = [];
    public uint SignCount { get; private set; }
    public Guid Aaguid { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void UpdateSignCount(uint signCount)
    {
        SignCount = signCount;
    }
}

public sealed class UserRecoveryCode
{
    private UserRecoveryCode() { }

    public UserRecoveryCode(Guid userId, string codeHash)
    {
        UserId = userId;
        CodeHash = codeHash;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UsedAtUtc { get; private set; }

    public void MarkAsUsed(DateTimeOffset timestamp)
    {
        UsedAtUtc = timestamp;
    }
}
'''

content += new_classes

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
