// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    public void CredentialVault_SaveAndRetrieveCredential(string loginId)
    {
        var credentialVault = new CredentialVault("DevHomeGitHubExtensionTest");
        Assert.IsNotNull(credentialVault);
        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());

        var nullCredential = credentialVault.GetCredentials(loginId);
        Assert.IsNull(nullCredential);

        var testCredential = "testcredential";

        var credential = new NetworkCredential(null, testCredential).SecurePassword;
        credentialVault.SaveCredentials(loginId, credential);

        var retrievedCredential = credentialVault.GetCredentials(loginId);
        Assert.IsNotNull(retrievedCredential);
        Assert.AreEqual(testCredential, retrievedCredential.Password);

        credentialVault.RemoveAllCredentials();
        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());
    }

    [TestMethod]
    [TestCategory("LiveData")]
    [DataRow("testuser1")]
    [DataRow("https://github.com/testuser2")]
    [DataRow("https://RandomWebServer.example/testuser3")]
    public void CredentialVault_RemoveAndRetrieveCredential(string loginId)
    {
        var credentialVault = new CredentialVault("DevHomeGitHubExtensionTest");
        Assert.IsNotNull(credentialVault);

        var testCredential = "testCredential";

        var credential = new NetworkCredential(null, testCredential).SecurePassword;
        credentialVault.SaveCredentials(loginId, credential);

        var retrievedCredential = credentialVault.GetCredentials(loginId);
        Assert.IsNotNull(retrievedCredential);
        Assert.AreEqual(testCredential, retrievedCredential.Password);

        credentialVault.RemoveCredentials(loginId);

        var nullCredential = credentialVault.GetCredentials(loginId);
        Assert.IsNull(nullCredential);

        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());
    }

    [TestMethod]
    [TestCategory("LiveData")]
    public void CredentialVault_GetAllCredentials()
    {
        var credentialVault = new CredentialVault("DevHomeGitHubExtensionTest");
        Assert.IsNotNull(credentialVault);

        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());

        var testLoginId = "testuser1";
        var testCredential = "testCredential";

        var credential = new NetworkCredential(null, testCredential).SecurePassword;
        credentialVault.SaveCredentials(testLoginId, credential);

        Assert.AreEqual(1, credentialVault.GetAllCredentials().Count());

        credentialVault.RemoveCredentials(testLoginId);

        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());

        var nullCredential = credentialVault.GetCredentials(testLoginId);
        Assert.IsNull(nullCredential);
    }
}
