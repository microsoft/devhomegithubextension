// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Net;
using GitHubExtension.DeveloperId;
using Microsoft.Windows.DevHome.SDK;

namespace GitHubExtension.Test;
public partial class DeveloperIdTests
{
    private CredentialVault SetupCleanCredentialVaultClean()
    {
        var credentialVault = new CredentialVault();
        Assert.IsNotNull(credentialVault);
        credentialVault.RemoveAllCredentials();
        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());

        // Ensure that the DeveloperIdProvider is deleted and recreated on the next GetInstance() call
        DeveloperIdProvider.GetInstance().Dispose();
        return credentialVault;
    }

    /* Tests depend on the following environment variables:
     * DEV_HOME_TEST_GITHUB_COM_USER : Url for a test user on github.com
     * DEV_HOME_TEST_GITHUB_COM_PAT : Personal Access Token for the test user on github.com
     */
    private CredentialVault SetupCredentialVaultWithTestUser()
    {
        var credentialVault = SetupCleanCredentialVaultClean();

        var testLoginId = Environment.GetEnvironmentVariable("DEV_HOME_TEST_GITHUB_COM_USER") ?? string.Empty;
        var testPassword = Environment.GetEnvironmentVariable("DEV_HOME_TEST_GITHUB_COM_PAT");

        var password = new NetworkCredential(null, testPassword).SecurePassword;
        credentialVault.SaveCredentials(testLoginId, password);

        Assert.AreEqual(1, credentialVault.GetAllCredentials().Count());
        return credentialVault;
    }

    /* Tests depend on the following environment variables:
     * DEV_HOME_TEST_GITHUB_ENTERPRISE_SERVER_USER : Url for a test user on GHES
     * DEV_HOME_TEST_GITHUB_ENTERPRISE_SERVER_PAT : Personal Access Token for the test user on the GHES
     */
    private CredentialVault SetupCredentialVaultWithGHESTestUser()
    {
        var credentialVault = SetupCleanCredentialVaultClean();

        var testLoginId = Environment.GetEnvironmentVariable("DEV_HOME_TEST_GITHUB_ENTERPRISE_SERVER_USER") ?? string.Empty;
        var testPassword = Environment.GetEnvironmentVariable("DEV_HOME_TEST_GITHUB_ENTERPRISE_SERVER_PAT");

        var password = new NetworkCredential(null, testPassword).SecurePassword;
        credentialVault.SaveCredentials(testLoginId, password);

        Assert.AreEqual(1, credentialVault.GetAllCredentials().Count());
        return credentialVault;
    }

    private CredentialVault SetupCredentialVaultWithInvalidTestUser()
    {
        var credentialVault = SetupCleanCredentialVaultClean();

        var testLoginId = "dummytestuser1";
        var testPassword = "invalidPAT";

        var password = new NetworkCredential(null, testPassword).SecurePassword;
        credentialVault.SaveCredentials(testLoginId, password);

        Assert.AreEqual(1, credentialVault.GetAllCredentials().Count());
        return credentialVault;
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void DeveloperIdProvider_SingletonTest()
    {
        // Ensure that the DeveloperIdProvider is a singleton
        var devIdProvider1 = DeveloperIdProvider.GetInstance();
        Assert.IsNotNull(devIdProvider1);
        Assert.IsInstanceOfType(devIdProvider1, typeof(DeveloperIdProvider));

        var devIdProvider2 = DeveloperIdProvider.GetInstance();
        Assert.IsNotNull(devIdProvider2);
        Assert.IsInstanceOfType(devIdProvider2, typeof(DeveloperIdProvider));

        Assert.AreSame(devIdProvider1, devIdProvider2);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void DeveloperIdProvider_GetDeveloperIds_Empty()
    {
        // Remove any existing credentials
        var credentialVault = SetupCleanCredentialVaultClean();

        var devIdProvider = DeveloperIdProvider.GetInstance();
        Assert.IsNotNull(devIdProvider);

        var result = devIdProvider.GetLoggedInDeveloperIds();
        Assert.IsNotNull(result);
        Assert.AreEqual(ProviderOperationStatus.Success, result.Result.Status);
        Assert.AreEqual(0, result.DeveloperIds.Count());

        // Cleanup
        credentialVault.RemoveAllCredentials();
        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());
    }

    [TestMethod]
    [TestCategory("LiveData")]
    public void DeveloperIdProvider_RestoreAndGetDeveloperIds()
    {
        // Setup CredentialVault with a dummy testuser and valid PAT for Github.com
        var credentialVault = SetupCredentialVaultWithTestUser();

        // Test whether the DeveloperIdProvider can restore the saved credentials
        var devIdProvider = DeveloperIdProvider.GetInstance();
        Assert.IsNotNull(devIdProvider);
        var result = devIdProvider.GetLoggedInDeveloperIds();

        Assert.IsNotNull(result);
        Assert.AreEqual(ProviderOperationStatus.Success, result.Result.Status);
        Assert.AreEqual(1, result.DeveloperIds.Count());
        Assert.AreNotEqual("dummytestuser1", result.DeveloperIds.First().LoginId);
        Assert.IsNotNull(new Uri(result.DeveloperIds.First().Url));

        // Cleanup
        credentialVault.RemoveAllCredentials();
        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());
    }

    [TestMethod]
    [TestCategory("LiveData")]
    public void DeveloperIdProvider_GetDeveloperIds_InvalidPAT()
    {
        // Setup CredentialVault with a dummy testuser and invalid PAT for Github.com
        var credentialVault = SetupCredentialVaultWithInvalidTestUser();

        // Test whether the DeveloperIdProvider can restore the saved credentials
        var devIdProvider = DeveloperIdProvider.GetInstance();
        Assert.IsNotNull(devIdProvider);
        var result = devIdProvider.GetLoggedInDeveloperIds();

        Assert.IsNotNull(result);
        Assert.AreEqual(ProviderOperationStatus.Success, result.Result.Status);
        Assert.AreEqual(0, result.DeveloperIds.Count());

        // Cleanup
        credentialVault.RemoveAllCredentials();
        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void DeveloperIdProvider_AuthenticationExperienceKind()
    {
           var devIdProvider = DeveloperIdProvider.GetInstance();
           Assert.IsNotNull(devIdProvider);
           Assert.AreEqual(AuthenticationExperienceKind.CardSession, devIdProvider.GetAuthenticationExperienceKind());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void DeveloperIdProvider_GetLoginAdaptiveCardSession()
    {
        var devIdProvider = DeveloperIdProvider.GetInstance();
        Assert.IsNotNull(devIdProvider);
        var result = devIdProvider.GetLoginAdaptiveCardSession();
        Assert.IsNotNull(result);
        Assert.AreEqual(ProviderOperationStatus.Success, result.Result.Status);
        Assert.IsNotNull(result.AdaptiveCardSession);
        Assert.IsInstanceOfType(result.AdaptiveCardSession, typeof(IExtensionAdaptiveCardSession));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void DeveloperIdProvider_ShowLogonSession_NeverImplemented()
    {
        var devIdProvider = DeveloperIdProvider.GetInstance();
        Assert.IsNotNull(devIdProvider);
        DeveloperIdResult? result = null;
        try
        {
            result = devIdProvider.ShowLogonSession(default(Microsoft.UI.WindowId)).GetResults();
        }
        catch (Exception e)
        {
            Assert.IsInstanceOfType(e, typeof(NotImplementedException));
        }

        Assert.IsNull(result);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void DeveloperIdProvider_GetDeveloperIdState_NeverImplemented()
    {
        var devIdProvider = DeveloperIdProvider.GetInstance();
        Assert.IsNotNull(devIdProvider);

        var result = devIdProvider.GetDeveloperIdState(new DeveloperId.DeveloperId());
        Assert.IsNotNull(result);
        Assert.AreEqual(AuthenticationState.LoggedOut, result);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void DeveloperIdProvider_LogoutDeveloperId_InvalidDeveloperId()
    {
        var devIdProvider = DeveloperIdProvider.GetInstance();
        Assert.IsNotNull(devIdProvider);

        var result = devIdProvider.LogoutDeveloperId(new DeveloperId.DeveloperId());
        Assert.IsNotNull(result);
        Assert.AreEqual(ProviderOperationStatus.Failure, result.Status);
    }

    [TestMethod]
    [TestCategory("LiveData")]
    public void DeveloperIdProvider_LogoutDeveloperId_Success()
    {
        // Setup CredentialVault with a dummy testuser and valid PAT for Github.com
        var credentialVault = SetupCredentialVaultWithTestUser();

        // Test whether the DeveloperIdProvider can restore the saved credentials
        var devIdProvider = DeveloperIdProvider.GetInstance();
        Assert.IsNotNull(devIdProvider);
        var resultGetLoggedInDeveloperIds = devIdProvider.GetLoggedInDeveloperIds();

        Assert.IsNotNull(resultGetLoggedInDeveloperIds);
        Assert.AreEqual(ProviderOperationStatus.Success, resultGetLoggedInDeveloperIds.Result.Status);
        Assert.AreEqual(1, resultGetLoggedInDeveloperIds.DeveloperIds.Count());
        var devId = resultGetLoggedInDeveloperIds.DeveloperIds.First();
        Assert.IsNotNull(devId);

        var resultLogoutDeveloperId = devIdProvider.LogoutDeveloperId(devId);
        Assert.IsNotNull(resultLogoutDeveloperId);
        Assert.AreEqual(ProviderOperationStatus.Success, resultLogoutDeveloperId.Status);
        Assert.AreEqual(AuthenticationState.LoggedOut, devIdProvider.GetDeveloperIdState(devId));

        credentialVault.RemoveAllCredentials();
        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());
    }

    [TestMethod]
    [TestCategory("LiveData")]
    public void DeveloperIdProvider_LogoutDeveloperId_GHES_Success()
    {
        // Setup CredentialVault with a dummy testuser and valid PAT for Github Enterprise Server
        var credentialVault = SetupCredentialVaultWithGHESTestUser();

        // Test whether the DeveloperIdProvider can restore the saved credentials
        var devIdProvider = DeveloperIdProvider.GetInstance();
        Assert.IsNotNull(devIdProvider);
        var resultGetLoggedInDeveloperIds = devIdProvider.GetLoggedInDeveloperIds();

        Assert.IsNotNull(resultGetLoggedInDeveloperIds);
        Assert.AreEqual(ProviderOperationStatus.Success, resultGetLoggedInDeveloperIds.Result.Status);
        Assert.AreEqual(1, resultGetLoggedInDeveloperIds.DeveloperIds.Count());
        var devId = resultGetLoggedInDeveloperIds.DeveloperIds.First();
        Assert.IsNotNull(devId);

        var resultLogoutDeveloperId = devIdProvider.LogoutDeveloperId(devId);
        Assert.IsNotNull(resultLogoutDeveloperId);
        Assert.AreEqual(ProviderOperationStatus.Success, resultLogoutDeveloperId.Status);
        Assert.AreEqual(AuthenticationState.LoggedOut, devIdProvider.GetDeveloperIdState(devId));

        credentialVault.RemoveAllCredentials();
        Assert.AreEqual(0, credentialVault.GetAllCredentials().Count());
    }
}
