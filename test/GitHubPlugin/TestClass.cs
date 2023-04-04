// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
using GitHubPlugin.DeveloperId;
using GitHubPlugin.Providers;
using Microsoft.Windows.DevHome.SDK;

namespace GitHubPlugin.Test;

/*
 * TODO: Write unit tests.
 * https://docs.microsoft.com/visualstudio/test/getting-started-with-unit-testing
 * https://docs.microsoft.com/visualstudio/test/using-microsoft-visualstudio-testtools-unittesting-members-in-unit-tests
 * https://docs.microsoft.com/visualstudio/test/run-unit-tests-with-test-explorer
 */

[TestClass]
public class TestClass : IDisposable
{
#pragma warning disable SA1310 // Field names should not contain underscore
    private const string REPO_NAME = "TestRepos";
#pragma warning restore SA1310 // Field names should not contain underscore

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        AuthenticationEventTriggered.Dispose();
    }

    private static void SetAttributesNormal(DirectoryInfo dir)
    {
        foreach (var subDir in dir.GetDirectories())
        {
            SetAttributesNormal(subDir);
        }

        foreach (var file in dir.GetFiles())
        {
            file.Attributes = FileAttributes.Normal;
        }
    }

    private static void RemoveTestRepo()
    {
        if (Directory.Exists(REPO_NAME))
        {
            SetAttributesNormal(new DirectoryInfo(REPO_NAME));
            Directory.Delete(REPO_NAME, true);
        }
    }

    private static readonly Semaphore AuthenticationEventTriggered = new (initialCount: 0, maximumCount: 1);

    public void AuthenticationEvent(object? sender, IDeveloperId developerId)
    {
        if (developerId.LoginId() is not null)
        {
            AuthenticationEventTriggered.Release();
        }
    }

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        Debug.WriteLine("ClassInitialize");
        RemoveTestRepo();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        Debug.WriteLine("ClassCleanup");
        RemoveTestRepo();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        Debug.WriteLine("TestInitialize");
    }

    [TestCleanup]
    public void TestCleanup()
    {
        Debug.WriteLine("TestCleanup");
    }

    [TestMethod]
    public void TestMethod()
    {
        Assert.IsTrue(true);
    }

    [TestMethod]
    [TestCategory("Manual")]
    [Ignore("Comment out to run this manually.")]
    public void DevId_Manual_LoginLogoutEvents()
    {
        DeveloperIdProvider authProvider = DeveloperIdProvider.GetInstance();

        // Register for Login and Logout Events
        authProvider.LoggedIn += AuthenticationEvent;
        authProvider.LoggedOut += AuthenticationEvent;

        // Start a new login flow. Control flows to browser.
        Task.Run(async () => { await authProvider.LoginNewDeveloperIdAsync(); });

        // Wait 30 secs for user to log in through browser, and trigger the Login event.
        Assert.IsTrue(AuthenticationEventTriggered.WaitOne(30000));

        // Get the list of DeveloperIds
        var devIds = authProvider.GetLoggedInDeveloperIdsInternal();
        Assert.IsNotNull(devIds);
        Assert.AreEqual(devIds.Count(), 1);

        // Logout
        authProvider.LogoutDeveloperId(devIds.First());

        // Wait 1 sec for the Logout event.
        AuthenticationEventTriggered.WaitOne(1000);

        // Get the list of DeveloperIds
        devIds = authProvider.GetLoggedInDeveloperIdsInternal();
        Assert.IsNotNull(devIds);
        Assert.AreEqual(devIds.Count(), 0);
    }
}
