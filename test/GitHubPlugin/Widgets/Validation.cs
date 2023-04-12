// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.
using DevHome.Logging;

namespace GitHubPlugin.Test;
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
        };

        foreach (var uriString in testUrisValid)
        {
            Assert.AreEqual("owner", Client.Validation.ParseOwnerFromGitHubURL(uriString));
            Assert.AreEqual("repo", Client.Validation.ParseRepositoryFromGitHubURL(uriString));
            Assert.AreEqual("owner/repo", Client.Validation.ParseFullNameFromGitHubURL(uriString));
            TestContext?.WriteLine($"Valid: {uriString}");
        }

        testListener.PrintEventCounts();
        Assert.AreEqual(false, testListener.FoundErrors());
    }
}
