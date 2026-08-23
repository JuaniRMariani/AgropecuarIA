using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AgropecuarIA.Identity.Tests;

[TestClass]
public class MfaApiIntegrationTests
{
    [TestMethod]
    public void SetupTotpReturnsSharedKeyAndUri() { }

    [TestMethod]
    public void EnableTotpPersistsEncryptedSecretAndReturnsRecoveryCodes() { }

    [TestMethod]
    public void DisableTotpRemovesTotpAndRecoveryCodes() { }

    [TestMethod]
    public void ConsumeRecoveryCodeMarksCodeAsUsed() { }
}