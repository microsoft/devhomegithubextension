// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.
using DevHome.Logging;

namespace GitHubExtension.Test;
public partial class WidgetTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void UriParsingValidation()
    {
        using var log = new Logger("TestStore", TestOptions.LogOptions);
        var testListener = new TestListener("TestListener", TestContext!);
        log.AddListener(testListener);
        DataModel.Log.Attach(log);

        var testUrisValid = new List<string>
        {
            "https://github.com/owner/repo",
            "https://github.com/owner/repo.git",
            "http://github.com/owner/repo",
            "http://github.com/owner/repo.git",
            "https://github.com/owner/repo/",
            "https://github.com/owner/repo.git/",
            "http://github.com/owner/repo/",
            "http://github.com/owner/repo.git/",
            "https://github.com/owner/repo/pulls/4",
            "http://github.com/owner/repo/pulls/4",
            "github.com/owner/repo",
            "github.com/owner/repo.git",
            "github.com/owner/repo/",
            "github.com/owner/repo.git/",
            "github.com/owner/repo/pulls/4",
            "github.com/owner/repo/pulls/4",
            "owner/repo",
            "https://github.com/owner/repo/issues?q=is%3Aopen+mentions%3A%40me",
            "github.com/owner/repo/issues?q=is%3Aopen+mentions%3A%40me",
        };

        foreach (var uriString in testUrisValid)
        {
            Assert.AreEqual("owner", Client.Validation.ParseOwnerFromGitHubURL(uriString));
            Assert.AreEqual("repo", Client.Validation.ParseRepositoryFromGitHubURL(uriString));
            Assert.AreEqual("owner/repo", Client.Validation.ParseFullNameFromGitHubURL(uriString));
            if (Client.Validation.IsValidGitHubIssueQueryURL(uriString))
            {
                Assert.AreEqual("is%3Aopen+mentions%3A%40me", Client.Validation.ParseIssueQueryFromGitHubURL(uriString));
            }
            else
            {
                Assert.AreEqual(string.Empty, Client.Validation.ParseIssueQueryFromGitHubURL(uriString));
            }

            TestContext?.WriteLine($"Valid: {uriString}");
        }

        var testUrisQuery = new List<string>
        {
            "https://github.com/owner/repo/issues?q=is%3Aopen+mentions%3A%40me",
            "https://github.com/owner/repo/ISSUES?q=is%3Aopen+mentions%3A%40me",
        };

        foreach (var uriString in testUrisQuery)
        {
            Assert.IsTrue(Client.Validation.IsValidGitHubIssueQueryURL(uriString));
            Assert.AreEqual("is%3Aopen+mentions%3A%40me", Client.Validation.ParseIssueQueryFromGitHubURL(uriString));
            TestContext?.WriteLine($"Is query URL: {uriString}");
        }

        var testFailUrisQuery = new List<string>
        {
            "https://github.com/owner/repo/pulls?q=is%3Aopen+mentions%3A%40me",
        };

        foreach (var uriString in testFailUrisQuery)
        {
            Assert.IsFalse(Client.Validation.IsValidGitHubIssueQueryURL(uriString));
            TestContext?.WriteLine($"Is not query URL: {uriString}");
        }

        testListener.PrintEventCounts();
        Assert.AreEqual(false, testListener.FoundErrors());
    }
}
