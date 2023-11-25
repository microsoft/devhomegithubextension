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
        var credentialVault1 = CredentialVault.GetInstance();
        Assert.IsNotNull(credentialVault1);

        var credentialVault2 = CredentialVault.GetInstance();
        Assert.IsNotNull(credentialVault2);

        Assert.AreEqual(credentialVault1, credentialVault2);

        credentialVault1.RemoveAllCredentials();
        Assert.AreEqual(0, credentialVault1.GetAllCredentials().Count());
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow("testuser1")]
    [DataRow("https://github.com/testuser2")]
    [DataRow("https://RandomWebServer.example/testuser3")]
    public void CredentialVault_SaveAndRetrieveCredential(string loginId)
    {
        var credentialVault = CredentialVault.GetInstance();
        Assert.IsNotNull(credentialVault);
        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());

        var nullPassword = credentialVault.GetCredentials(loginId);
        Assert.IsNull(nullPassword);

        var testPassword = "testpassword";

        var password = new NetworkCredential(null, testPassword).SecurePassword;
        credentialVault.SaveCredentials(loginId, password);

        var retrievedPassword = credentialVault.GetCredentials(loginId);
        Assert.IsNotNull(retrievedPassword);
        Assert.AreEqual(testPassword, retrievedPassword.Password);

        credentialVault.RemoveAllCredentials();
        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow("testuser1")]
    [DataRow("https://github.com/testuser2")]
    [DataRow("https://RandomWebServer.example/testuser3")]
    public void CredentialVault_RemoveAndRetrieveCredential(string loginId)
    {
        var credentialVault = CredentialVault.GetInstance();
        Assert.IsNotNull(credentialVault);

        var testPassword = "testpassword";

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
    [TestCategory("Unit")]
    public void CredentialVault_GetAllCredentials()
    {
        var credentialVault = CredentialVault.GetInstance();
        Assert.IsNotNull(credentialVault);

        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());

        var testLoginId = "testuser1";
        var testPassword = "testpassword";

        var password = new NetworkCredential(null, testPassword).SecurePassword;
        credentialVault.SaveCredentials(testLoginId, password);

        Assert.AreEqual(1, credentialVault.GetAllCredentials().Count());

        credentialVault.RemoveCredentials(testLoginId);

        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());

        var nullPassword = credentialVault.GetCredentials(testLoginId);
        Assert.IsNull(nullPassword);
    }
}
