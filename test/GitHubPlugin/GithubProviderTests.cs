// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.Windows.DevHome.SDK;

namespace GitHubPlugin.Test;
[TestClass]
public partial class GithubProviderTests
{
#pragma warning disable SA1310 // Field names should not contain underscore
    private const string WASDK_URL = "https://github.com/microsoft/windowsappsdk";
#pragma warning restore SA1310 // Field names should not contain underscore
#pragma warning disable SA1310 // Field names should not contain underscore
    private const string WASDK_CLONE_URL = "https://github.com/microsoft/WindowsAppSDK.git";
#pragma warning restore SA1310 // Field names should not contain underscore
#pragma warning disable SA1310 // Field names should not contain underscore
    private const string CLONE_LOCATION = "CloneToMe";
#pragma warning restore SA1310 // Field names should not contain underscore

    [TestInitialize]
    public void SetUpRepoTests()
    {
        if (Directory.Exists(CLONE_LOCATION))
        {
            Directory.Delete(CLONE_LOCATION, true);
        }

        Directory.CreateDirectory(CLONE_LOCATION);
    }

    [TestCleanup]
    public void RemoveCloneLocation()
    {
        if (Directory.Exists(CLONE_LOCATION))
        {
            Directory.Delete(CLONE_LOCATION, true);
        }
    }

    [TestMethod]
    public void ValidateCanGetProvider()
    {
        var manualResetEvent = new ManualResetEvent(false);
        var githubPlugin = new GitHubPlugin(manualResetEvent);
        var repositoryProvider = githubPlugin.GetProvider(ProviderType.Repository);
        Assert.IsNotNull(repositoryProvider);
        Assert.IsNotNull(repositoryProvider as IRepositoryProvider);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void CanParseGoodURL()
    {
        var manualResetEvent = new ManualResetEvent(false);
        var githubPlugin = new GitHubPlugin(manualResetEvent);
        var repositoryObject = githubPlugin.GetProvider(ProviderType.Repository);
        Assert.IsNotNull(repositoryObject);

        var repositoryProvider = repositoryObject as IRepositoryProvider;
        Assert.IsNotNull(repositoryProvider);

        // Get via URL
        var repository = repositoryProvider.ParseRepositoryFromUrlAsync(new Uri(WASDK_URL)).AsTask().Result;
        Assert.IsNotNull(repository);

        // Get via URL with .git extension
        repository = repositoryProvider.ParseRepositoryFromUrlAsync(new Uri(WASDK_CLONE_URL)).AsTask().Result;
        Assert.IsNotNull(repository);
    }

    [TestMethod]
    [TestCategory("Unit")]
    [Ignore("LibGit2Sharp can't clone to long paths.  Ignore till we found another location to clone")]
    public void CanClone()
    {
        var manualResetEvent = new ManualResetEvent(false);
        var githubPlugin = new GitHubPlugin(manualResetEvent);
        var repositoryObject = githubPlugin.GetProvider(ProviderType.Repository);
        Assert.IsNotNull(repositoryObject);

        var repositoryProvider = repositoryObject as IRepositoryProvider;
        Assert.IsNotNull(repositoryProvider);

        // Clone via URL
        var repository = repositoryProvider.ParseRepositoryFromUrlAsync(new Uri(WASDK_URL)).AsTask().Result;
        Assert.IsNotNull(repository);

        repository.CloneRepositoryAsync(CLONE_LOCATION).AsTask().Wait();
        Assert.IsTrue(Directory.Exists(CLONE_LOCATION));
        Assert.IsTrue(Directory.EnumerateFileSystemEntries(CLONE_LOCATION).Any());

        Directory.Delete(CLONE_LOCATION, true);
        Assert.IsFalse(Directory.EnumerateFileSystemEntries(CLONE_LOCATION).Any());

        // Clone via URL with .git extension
        repository = repositoryProvider.ParseRepositoryFromUrlAsync(new Uri(WASDK_CLONE_URL)).AsTask().Result;
        Assert.IsNotNull(repository);

        Assert.IsTrue(Directory.Exists(CLONE_LOCATION));
        Assert.IsTrue(Directory.EnumerateFileSystemEntries(CLONE_LOCATION).Any());

        Directory.Delete(CLONE_LOCATION, true);
        Assert.IsFalse(Directory.EnumerateFileSystemEntries(CLONE_LOCATION).Any());
    }

    [TestMethod]
    [TestCategory("Unit")]
    [Ignore("Ignoring right now until mock repo is implemeneted")]
    public void CloneViaMakingRepoObject()
    {
        IRepository mockRepo = new Mocks.MockRepository();
        mockRepo.CloneRepositoryAsync(CLONE_LOCATION, null).AsTask().Wait();
        Assert.IsTrue(Directory.Exists(CLONE_LOCATION));
        Assert.IsTrue(Directory.EnumerateFileSystemEntries(CLONE_LOCATION).Any());

        Directory.Delete(CLONE_LOCATION, true);
        Assert.IsFalse(Directory.EnumerateFileSystemEntries(CLONE_LOCATION).Any());

        mockRepo.CloneRepositoryAsync(CLONE_LOCATION).AsTask().Wait();
        Assert.IsTrue(Directory.Exists(CLONE_LOCATION));
        Assert.IsTrue(Directory.EnumerateFileSystemEntries(CLONE_LOCATION).Any());

        Directory.Delete(CLONE_LOCATION, true);
        Assert.IsFalse(Directory.EnumerateFileSystemEntries(CLONE_LOCATION).Any());
    }
}
