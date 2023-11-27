// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubExtension.DeveloperId;
using Microsoft.Windows.DevHome.SDK;

namespace GitHubExtension.Test;
public partial class DeveloperIdTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task FunctionalTest_RestoreAndRetrieveRepositoriesAsync()
    {
        var credentialVault = SetupCredentialVaultWithTestUser();

        var devIdProvider = DeveloperIdProvider.GetInstance();
        Assert.IsNotNull(devIdProvider);

        var devIds = devIdProvider.GetLoggedInDeveloperIds().DeveloperIds;
        Assert.IsNotNull(devIds);
        Assert.AreEqual(1, devIds.Count());

        var devId = devIds.First();
        Assert.IsNotNull(devId);

        // Get Repository Provider
        var manualResetEvent = new ManualResetEvent(false);
        var githubExtension = new GitHubExtension(manualResetEvent);
        var repositoryObject = githubExtension.GetProvider(ProviderType.Repository);
        Assert.IsNotNull(repositoryObject);

        var repositoryProvider = repositoryObject as IRepositoryProvider;
        Assert.IsNotNull(repositoryProvider);

        var result = await repositoryProvider.GetRepositoriesAsync(devId);
        Assert.IsNotNull(result);
        Assert.AreEqual(ProviderOperationStatus.Success, result.Result.Status);
        Assert.IsNotNull(result.Repositories);
        Assert.IsTrue(result.Repositories.Count() > 1);

        credentialVault.RemoveAllCredentials();
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task FunctionalTest_GHES_RestoreAndRetrieveRepositoriesAsync()
    {
        var credentialVault = SetupCredentialVaultWithGHESTestUser();

        var devIdProvider = DeveloperIdProvider.GetInstance();
        Assert.IsNotNull(devIdProvider);

        var devIds = devIdProvider.GetLoggedInDeveloperIds().DeveloperIds;
        Assert.IsNotNull(devIds);
        Assert.AreEqual(1, devIds.Count());

        var devId = devIds.First();
        Assert.IsNotNull(devId);

        // Get Repository Provider
        var manualResetEvent = new ManualResetEvent(false);
        var githubExtension = new GitHubExtension(manualResetEvent);
        var repositoryObject = githubExtension.GetProvider(ProviderType.Repository);
        Assert.IsNotNull(repositoryObject);

        var repositoryProvider = repositoryObject as IRepositoryProvider;
        Assert.IsNotNull(repositoryProvider);

        var result = await repositoryProvider.GetRepositoriesAsync(devId);
        Assert.IsNotNull(result);
        Assert.AreEqual(ProviderOperationStatus.Success, result.Result.Status);
        Assert.IsNotNull(result.Repositories);
        Assert.IsTrue(result.Repositories.Count() > 1);

        credentialVault.RemoveAllCredentials();
    }
}
