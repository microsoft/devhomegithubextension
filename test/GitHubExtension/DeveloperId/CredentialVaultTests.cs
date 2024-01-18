// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Net;
using GitHubExtension.DeveloperId;

namespace GitHubExtension.Test;

// Unit Tests for CredentialVault
public partial class DeveloperIdTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void CredentialVault_CreateSingleton()
    {
        var credentialVault1 = new CredentialVault("DevHomeGitHubExtensionTest");
        Assert.IsNotNull(credentialVault1);

        var credentialVault2 = new CredentialVault("DevHomeGitHubExtensionTest");
        Assert.IsNotNull(credentialVault2);

        Assert.AreNotEqual(credentialVault1, credentialVault2);

        credentialVault1.RemoveAllCredentials();
        Assert.AreEqual(0, credentialVault1.GetAllCredentials().Count());
    }

    [TestMethod]
    [TestCategory("LiveData")]
    [DataRow("testuser1")]
    [DataRow("https://github.com/testuser2")]
    [DataRow("https://RandomWebServer.example/testuser3")]
    [Ignore("testPassword needs to be added in a way that doesn't trigger credScan")]
    public void CredentialVault_SaveAndRetrieveCredential(string loginId)
    {
        var credentialVault = new CredentialVault("DevHomeGitHubExtensionTest");
        Assert.IsNotNull(credentialVault);
        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());

        var nullPassword = credentialVault.GetCredentials(loginId);
        Assert.IsNull(nullPassword);

        var testPassword = string.Empty;

        var password = new NetworkCredential(null, testPassword).SecurePassword;
        credentialVault.SaveCredentials(loginId, password);

        var retrievedPassword = credentialVault.GetCredentials(loginId);
        Assert.IsNotNull(retrievedPassword);
        Assert.AreEqual(testPassword, retrievedPassword.Password);

        credentialVault.RemoveAllCredentials();
        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());
    }

    [TestMethod]
    [TestCategory("LiveData")]
    [DataRow("testuser1")]
    [DataRow("https://github.com/testuser2")]
    [DataRow("https://RandomWebServer.example/testuser3")]
    [Ignore("testPassword needs to be added in a way that doesn't trigger credScan")]
    public void CredentialVault_RemoveAndRetrieveCredential(string loginId)
    {
        var credentialVault = new CredentialVault("DevHomeGitHubExtensionTest");
        Assert.IsNotNull(credentialVault);

        var testPassword = string.Empty;

        var password = new NetworkCredential(null, testPassword).SecurePassword;
        credentialVault.SaveCredentials(loginId, password);

        var retrievedPassword = credentialVault.GetCredentials(loginId);
        Assert.IsNotNull(retrievedPassword);
        Assert.AreEqual(testPassword, retrievedPassword.Password);

        credentialVault.RemoveCredentials(loginId);

        var nullPassword = credentialVault.GetCredentials(loginId);
        Assert.IsNull(nullPassword);

        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());
    }

    [TestMethod]
    [TestCategory("LiveData")]
    [Ignore("testPassword needs to be added in a way that doesn't trigger credScan")]
    public void CredentialVault_GetAllCredentials()
    {
        var credentialVault = new CredentialVault("DevHomeGitHubExtensionTest");
        Assert.IsNotNull(credentialVault);

        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());

        var testLoginId = "testuser1";
        var testPassword = string.Empty;

        var password = new NetworkCredential(null, testPassword).SecurePassword;
        credentialVault.SaveCredentials(testLoginId, password);

        Assert.AreEqual(1, credentialVault.GetAllCredentials().Count());

        credentialVault.RemoveCredentials(testLoginId);

        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());

        var nullPassword = credentialVault.GetCredentials(testLoginId);
        Assert.IsNull(nullPassword);
    }
}
