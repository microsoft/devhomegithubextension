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
