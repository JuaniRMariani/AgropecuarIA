import sys
import json

path = r'B:\Xenova\AgropecuarIA\tasks\evidence\AGRO-SEC-002\authorization-surface-register.json'
with open(path, 'r', encoding='utf-8') as f:
    data = json.load(f)

new_endpoints = [
    {
      "id": "identity.mfa.totp.setup",
      "method": "POST",
      "path": "/api/identity/mfa/totp/setup",
      "boundary": "authenticated-platform",
      "resource": "own-totp-credential",
      "action": "create",
      "authentication": "session-cookie+csrf",
      "actorSource": "cookie",
      "tenantSource": "none",
      "applicationAuthorization": "recent verified session",
      "storageBoundary": "identity-platform",
      "neutralErrors": "totp already enabled",
      "owner": "Identity",
      "tests": [
        "tests/AgropecuarIA.Identity.Tests/MfaApiIntegrationTests.cs#SetupTotpReturnsSharedKeyAndUri"
      ]
    },
    {
      "id": "identity.mfa.totp.enable",
      "method": "POST",
      "path": "/api/identity/mfa/totp/enable",
      "boundary": "authenticated-platform",
      "resource": "own-totp-credential",
      "action": "activate",
      "authentication": "session-cookie+csrf",
      "actorSource": "cookie",
      "tenantSource": "none",
      "applicationAuthorization": "recent verified session",
      "storageBoundary": "identity-platform",
      "neutralErrors": "totp already enabled, invalid code",
      "owner": "Identity",
      "tests": [
        "tests/AgropecuarIA.Identity.Tests/MfaApiIntegrationTests.cs#EnableTotpPersistsEncryptedSecretAndReturnsRecoveryCodes"
      ]
    },
    {
      "id": "identity.mfa.totp.disable",
      "method": "POST",
      "path": "/api/identity/mfa/totp/disable",
      "boundary": "authenticated-platform",
      "resource": "own-totp-credential",
      "action": "delete",
      "authentication": "session-cookie+csrf",
      "actorSource": "cookie",
      "tenantSource": "none",
      "applicationAuthorization": "recent verified session",
      "storageBoundary": "identity-platform",
      "neutralErrors": "totp not enabled",
      "owner": "Identity",
      "tests": [
        "tests/AgropecuarIA.Identity.Tests/MfaApiIntegrationTests.cs#DisableTotpRemovesTotpAndRecoveryCodes"
      ]
    },
    {
      "id": "identity.mfa.recovery.consume",
      "method": "POST",
      "path": "/api/identity/mfa/recovery/consume",
      "boundary": "authenticated-platform",
      "resource": "own-recovery-code",
      "action": "consume",
      "authentication": "session-cookie+csrf",
      "actorSource": "cookie",
      "tenantSource": "none",
      "applicationAuthorization": "recent verified session",
      "storageBoundary": "identity-platform",
      "neutralErrors": "invalid recovery code",
      "owner": "Identity",
      "tests": [
        "tests/AgropecuarIA.Identity.Tests/MfaApiIntegrationTests.cs#ConsumeRecoveryCodeMarksCodeAsUsed"
      ]
    }
]

data['operations'].extend(new_endpoints)

with open(path, 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=2)
