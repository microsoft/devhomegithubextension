// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Dapper.Contrib.Extensions;
using DevHome.Logging;
using GitHubExtension.DataModel;
using GitHubExtension.Helpers;

namespace GitHubExtension.Test;

public partial class DataStoreTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void DateTimeExtension()
    {
        using var log = new Logger("TestStore", TestOptions.LogOptions);
        var testListener = new TestListener("TestListener", TestContext!);
        log.AddListener(testListener);
        Log.Attach(log);

        var now = DateTime.Now;
        TestContext?.WriteLine($"Now: {now}");
        var nowAsInteger = now.ToDataStoreInteger();
        TestContext?.WriteLine($"NowAsDataStoreInteger: {nowAsInteger}");
        var nowFromInteger = nowAsInteger.ToDateTime();
        TestContext?.WriteLine($"NowFromDataStoreInteger: {nowFromInteger}");

        // We should not lose precision in the conversion to/from datastore format.
        Assert.AreEqual(now, nowFromInteger);
        Assert.AreEqual(now, now.ToDataStoreInteger().ToDateTime());
        Assert.AreEqual(now, now.ToDataStoreString().ToDateTime());

        // Working with the value should be as easy as working with dates, converting to numbers,
        // and using them in queries.
        var thirtyDays = new TimeSpan(30, 0, 0);
        TestContext?.WriteLine($"ThirtyDays: {thirtyDays}");
        var thirtyDaysAgo = now.Subtract(thirtyDays);
        TestContext?.WriteLine($"ThirtyDaysAgo: {thirtyDaysAgo}");
        var thirtyDaysAgoAsInteger = thirtyDaysAgo.ToDataStoreInteger();
        TestContext?.WriteLine($"ThirtyDaysAgoAsInteger: {thirtyDaysAgoAsInteger}");
        TestContext?.WriteLine($"ThirtyDays Ticks: {thirtyDays.Ticks}");
        TestContext?.WriteLine($"IntegerDiff: {nowAsInteger - thirtyDaysAgoAsInteger}");

        // Doing some timespan manipulation should still result in the same tick difference.
        // Also verify TimeSpan converters.
        Assert.AreEqual(thirtyDays.Ticks, nowAsInteger - thirtyDaysAgoAsInteger);
        Assert.AreEqual(thirtyDays, thirtyDays.ToDataStoreInteger().ToTimeSpan());
        Assert.AreEqual(thirtyDays, thirtyDays.ToDataStoreString().ToTimeSpan());

        // Test adding metadata time as string to the datastore.
        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        Assert.IsNotNull(dataStore);
        dataStore.Create();
        Assert.IsNotNull(dataStore.Connection);
        MetaData.AddOrUpdate(dataStore, "Now", now.ToDataStoreString());
        MetaData.AddOrUpdate(dataStore, "ThirtyDays", thirtyDays.ToDataStoreString());
        var nowFromMetaData = MetaData.Get(dataStore, "Now");
        Assert.IsNotNull(nowFromMetaData);
        var thirtyDaysFromMetaData = MetaData.Get(dataStore, "ThirtyDays");
        Assert.IsNotNull(thirtyDaysFromMetaData);
        Assert.AreEqual(now, nowFromMetaData.ToDateTime());
        Assert.AreEqual(thirtyDays, thirtyDaysFromMetaData.ToTimeSpan());

        testListener.PrintEventCounts();
        Assert.AreEqual(false, testListener.FoundErrors());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ReadAndWriteMetaData()
    {
        using var log = new Logger("TestStore", TestOptions.LogOptions);
        var testListener = new TestListener("TestListener", TestContext!);
        log.AddListener(testListener);
        Log.Attach(log);

        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        Assert.IsNotNull(dataStore);
        dataStore.Create();
        Assert.IsNotNull(dataStore.Connection);

        var metadata = new List<MetaData>
        {
            { new MetaData { Key = "Kittens", Value = "Cute" } },
            { new MetaData { Key = "Puppies", Value = "LotsOfWork" } },
        };

        using var tx = dataStore.Connection!.BeginTransaction();
        dataStore.Connection.Insert(metadata[0]);
        dataStore.Connection.Insert(metadata[1]);
        tx.Commit();

        // Verify retrieval and input into data objects.
        var dataStoreMetaData = dataStore.Connection.GetAll<MetaData>().ToList();
        Assert.AreEqual(dataStoreMetaData.Count, 2);
        foreach (var metaData in dataStoreMetaData)
        {
            TestContext?.WriteLine($"  Id: {metaData.Id}  Key: {metaData.Key}  Value: {metaData.Value}");

            Assert.IsTrue(metaData.Id == 1 || metaData.Id == 2);

            if (metaData.Id == 1)
            {
                Assert.AreEqual("Kittens", metaData.Key);
                Assert.AreEqual("Cute", metaData.Value);
            }

            if (metaData.Id == 2)
            {
                Assert.AreEqual("Puppies", metaData.Key);
                Assert.AreEqual("LotsOfWork", metaData.Value);
            }
        }

        // Verify direct add and retrieval.
        MetaData.AddOrUpdate(dataStore, "Puppies", "WorthIt!");
        MetaData.AddOrUpdate(dataStore, "Spiders", "Nope");
        Assert.AreEqual("Cute", MetaData.Get(dataStore, "Kittens"));
        Assert.AreEqual("WorthIt!", MetaData.Get(dataStore, "Puppies"));
        Assert.AreEqual("Nope", MetaData.Get(dataStore, "Spiders"));
        dataStoreMetaData = dataStore.Connection.GetAll<MetaData>().ToList();
        foreach (var metaData in dataStoreMetaData)
        {
            TestContext?.WriteLine($"  Id: {metaData.Id}  Key: {metaData.Key}  Value: {metaData.Value}");
        }

        testListener.PrintEventCounts();
        Assert.AreEqual(false, testListener.FoundErrors());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ReadAndWriteUser()
    {
        using var log = new Logger("TestStore", TestOptions.LogOptions);
        var testListener = new TestListener("TestListener", TestContext!);
        log.AddListener(testListener);
        Log.Attach(log);

        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        Assert.IsNotNull(dataStore);
        dataStore.Create();
        Assert.IsNotNull(dataStore.Connection);

        var users = new List<User>
        {
            { new User { Login = "Kittens", InternalId = 16, AvatarUrl = "https://www.microsoft.com", Type = "Cat" } },
            { new User { Login = "Liberty", InternalId = 7, AvatarUrl = "https://www.microsoft.com", Type = "Dog" } },
        };

        using var tx = dataStore.Connection!.BeginTransaction();
        dataStore.Connection.Insert(users[0]);
        dataStore.Connection.Insert(users[1]);
        tx.Commit();

        // Verify retrieval and input into data objects.
        var dataStoreUsers = dataStore.Connection.GetAll<User>().ToList();
        Assert.AreEqual(dataStoreUsers.Count, 2);
        foreach (var user in dataStoreUsers)
        {
            TestContext?.WriteLine($"  User: {user.Id}: {user.Login} - {user.InternalId} - {user.AvatarUrl} - {user.Type}");

            Assert.IsTrue(user.Id == 1 || user.Id == 2);

            if (user.Id == 1)
            {
                Assert.AreEqual("Kittens", user.Login);
                Assert.AreEqual(16, user.InternalId);
                Assert.AreEqual("Cat", user.Type);
                Assert.AreEqual("https://www.microsoft.com", user.AvatarUrl);
            }

            if (user.Id == 2)
            {
                Assert.AreEqual("Liberty", user.Login);
                Assert.AreEqual(7, user.InternalId);
                Assert.AreEqual("Dog", user.Type);
                Assert.AreEqual("https://www.microsoft.com", user.AvatarUrl);
            }
        }

        testListener.PrintEventCounts();
        Assert.AreEqual(false, testListener.FoundErrors());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ReadAndWriteRepository()
    {
        using var log = new Logger("TestStore", TestOptions.LogOptions);
        var testListener = new TestListener("TestListener", TestContext!);
        log.AddListener(testListener);
        Log.Attach(log);

        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        Assert.IsNotNull(dataStore);
        dataStore.Create();
        Assert.IsNotNull(dataStore.Connection);

        using var tx = dataStore.Connection.BeginTransaction();

        // Add User record
        dataStore.Connection.Insert(new User { Login = "Kittens", InternalId = 16, AvatarUrl = "https://www.microsoft.com", Type = "Cat" });

        // Add repositories
        var repositories = new List<Repository>
        {
            { new Repository { OwnerId = 1, InternalId = 47, Name = "TestRepo1", Description = "Short Desc", HtmlUrl = "https://www.microsoft.com", DefaultBranch = "main" } },
            { new Repository { OwnerId = 1, InternalId = 117, Name = "TestRepo2", Description = "Short Desc", HtmlUrl = "https://www.microsoft.com", DefaultBranch = "main" } },
        };
        dataStore.Connection.Insert(repositories[0]);
        dataStore.Connection.Insert(repositories[1]);
        tx.Commit();

        // Verify retrieval and input into data objects.
        var dataStoreRepositories = dataStore.Connection.GetAll<Repository>().ToList();
        Assert.AreEqual(dataStoreRepositories.Count, 2);
        foreach (var repo in dataStoreRepositories)
        {
            // Get User for the repo
            var user = dataStore.Connection.Get<User>(repo.OwnerId);
            TestContext?.WriteLine($"  User: {user.Login}  Repo: {repo.Name} - {repo.InternalId} - {repo.Description}");
            Assert.AreEqual("Kittens", user.Login);
            Assert.IsTrue(repo.Id == 1 || repo.Id == 2);

            if (repo.Id == 1)
            {
                Assert.AreEqual("Kittens", user.Login);
                Assert.AreEqual(47, repo.InternalId);
                Assert.AreEqual("TestRepo1", repo.Name);
                Assert.AreEqual("https://www.microsoft.com", repo.HtmlUrl);
            }

            if (repo.Id == 2)
            {
                Assert.AreEqual("Kittens", user.Login);
                Assert.AreEqual(117, repo.InternalId);
                Assert.AreEqual("TestRepo2", repo.Name);
                Assert.AreEqual("https://www.microsoft.com", repo.HtmlUrl);
            }
        }

        testListener.PrintEventCounts();
        Assert.AreEqual(false, testListener.FoundErrors());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ReadAndWriteIssue()
    {
        using var log = new Logger("TestStore", TestOptions.LogOptions);
        var testListener = new TestListener("TestListener", TestContext!);
        log.AddListener(testListener);
        Log.Attach(log);

        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        Assert.IsNotNull(dataStore);
        dataStore.Create();
        Assert.IsNotNull(dataStore.Connection);

        using var tx = dataStore.Connection!.BeginTransaction();

        // Add User record
        dataStore.Connection.Insert(new User { Login = "Kittens", InternalId = 16, AvatarUrl = "https://www.microsoft.com", Type = "Cat" });

        // Add repository record
        dataStore.Connection.Insert(new Repository { OwnerId = 1, InternalId = 47, Name = "TestRepo1", Description = "Short Desc", HtmlUrl = "https://www.microsoft.com", DefaultBranch = "main", HasIssues = 1 });

        var issues = new List<Issue>
        {
            { new Issue { AuthorId = 1, Number = 1111, InternalId = 18, Title = "No worky", Body = "This feature doesn't work.", HtmlUrl = "https://www.microsoft.com", RepositoryId = 1 } },
            { new Issue { AuthorId = 1, Number = 47, InternalId = 20, Title = "Missing Tests", Body = "More tests needed.", HtmlUrl = "https://www.microsoft.com", RepositoryId = 1 } },
        };
        dataStore.Connection.Insert(issues[0]);
        dataStore.Connection.Insert(issues[1]);
        tx.Commit();

        // Verify retrieval and input into data objects.
        var dataStoreIssues = dataStore.Connection.GetAll<Issue>().ToList();
        Assert.AreEqual(dataStoreIssues.Count, 2);
        foreach (var issue in dataStoreIssues)
        {
            // Get User  and Repo info
            var user = dataStore.Connection.Get<User>(issue.AuthorId);
            var repo = dataStore.Connection.Get<Repository>(issue.RepositoryId);

            TestContext?.WriteLine($"  User: {user.Login}  Repo: {repo.Name} - {issue.Number} - {issue.Title}");
            Assert.AreEqual("Kittens", user.Login);
            Assert.AreEqual("TestRepo1", repo.Name);
            Assert.IsTrue(issue.Id == 1 || issue.Id == 2);

            if (issue.Id == 1)
            {
                Assert.AreEqual("Kittens", user.Login);
                Assert.AreEqual(1111, issue.Number);
                Assert.AreEqual("TestRepo1", repo.Name);
                Assert.AreEqual("No worky", issue.Title);
            }

            if (issue.Id == 2)
            {
                Assert.AreEqual("Kittens", user.Login);
                Assert.AreEqual(47, issue.Number);
                Assert.AreEqual("TestRepo1", repo.Name);
                Assert.AreEqual("Missing Tests", issue.Title);
            }
        }

        testListener.PrintEventCounts();
        Assert.AreEqual(false, testListener.FoundErrors());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ReadAndWritePullRequest()
    {
        using var log = new Logger("TestStore", TestOptions.LogOptions);
        var testListener = new TestListener("TestListener", TestContext!);
        log.AddListener(testListener);
        Log.Attach(log);

        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        Assert.IsNotNull(dataStore);
        dataStore.Create();
        Assert.IsNotNull(dataStore.Connection);

        using var tx = dataStore.Connection!.BeginTransaction();

        // Add User record
        dataStore.Connection.Insert(new User { Login = "Kittens", InternalId = 16, AvatarUrl = "https://www.microsoft.com", Type = "Cat" });

        // Add repository record
        dataStore.Connection.Insert(new Repository { OwnerId = 1, InternalId = 47, Name = "TestRepo1", Description = "Short Desc", HtmlUrl = "https://www.microsoft.com", DefaultBranch = "main", HasIssues = 1 });

        var prs = new List<PullRequest>
        {
            { new PullRequest { AuthorId = 1, Number = 12, InternalId = 4, Title = "Fix no worky", Body = "This feature doesn't work.", HtmlUrl = "https://www.microsoft.com", RepositoryId = 1 } },
            { new PullRequest { AuthorId = 1, Number = 85, InternalId = 22, Title = "Implement Tests", Body = "More tests needed.", HtmlUrl = "https://www.microsoft.com", RepositoryId = 1 } },
        };
        dataStore.Connection.Insert(prs[0]);
        dataStore.Connection.Insert(prs[1]);
        tx.Commit();

        // Verify retrieval and input into data objects.
        var dataStorePulls = dataStore.Connection.GetAll<PullRequest>().ToList();
        Assert.AreEqual(dataStorePulls.Count, 2);
        foreach (var pull in dataStorePulls)
        {
            // Get User  and Repo info
            var user = dataStore.Connection.Get<User>(pull.AuthorId);
            var repo = dataStore.Connection.Get<Repository>(pull.RepositoryId);

            TestContext?.WriteLine($"  User: {user.Login}  Repo: {repo.Name} - {pull.Number} - {pull.Title}");
            Assert.AreEqual("Kittens", user.Login);
            Assert.AreEqual("TestRepo1", repo.Name);
            Assert.IsTrue(pull.Id == 1 || pull.Id == 2);

            if (pull.Id == 1)
            {
                Assert.AreEqual("Kittens", user.Login);
                Assert.AreEqual(12, pull.Number);
                Assert.AreEqual("TestRepo1", repo.Name);
                Assert.AreEqual("Fix no worky", pull.Title);
            }

            if (pull.Id == 2)
            {
                Assert.AreEqual("Kittens", user.Login);
                Assert.AreEqual(85, pull.Number);
                Assert.AreEqual("TestRepo1", repo.Name);
                Assert.AreEqual("Implement Tests", pull.Title);
            }

            // Datastore is not linked, therefore Checks should be Unknown and empty collections.
            Assert.AreEqual(CheckConclusion.Unknown, pull.ChecksConclusion);
            Assert.AreEqual(CheckStatus.Unknown, pull.ChecksStatus);
            Assert.AreEqual(0, pull.Checks.Count());
            Assert.AreEqual(0, pull.FailedChecks.Count());
        }

        testListener.PrintEventCounts();
        Assert.AreEqual(false, testListener.FoundErrors());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ReadAndWriteCheckRun()
    {
        using var log = new Logger("TestStore", TestOptions.LogOptions);
        var testListener = new TestListener("TestListener", TestContext!);
        log.AddListener(testListener);
        Log.Attach(log);

        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        Assert.IsNotNull(dataStore);
        dataStore.Create();
        Assert.IsNotNull(dataStore.Connection);

        using var tx = dataStore.Connection!.BeginTransaction();

        var checks = new List<CheckRun>
        {
            { new CheckRun { HeadSha = "1234abcd", Name = "Build x86", InternalId = 16, ConclusionId = 1, StatusId = 3 } },
            { new CheckRun { HeadSha = "1234abcd", Name = "Build x64", InternalId = 7, ConclusionId = 7, StatusId = 3 } },
        };

        dataStore.Connection.Insert(checks[0]);
        dataStore.Connection.Insert(checks[1]);

        // Add User record
        dataStore.Connection.Insert(new User { Login = "Kittens", InternalId = 16, AvatarUrl = "https://www.microsoft.com", Type = "Cat" });

        // Add repository record
        dataStore.Connection.Insert(new Repository { OwnerId = 1, InternalId = 47, Name = "TestRepo1", Description = "Short Desc", HtmlUrl = "https://www.microsoft.com", DefaultBranch = "main", HasIssues = 1 });

        // Add PullRequest record
        dataStore.Connection.Insert(new PullRequest { Title = "Fix the things", InternalId = 42, HeadSha = "1234abcd", AuthorId = 1, RepositoryId = 1 });

        tx.Commit();

        // Verify retrieval and input into data objects.
        var dataStoreCheckRuns = dataStore.Connection.GetAll<CheckRun>().ToList();
        Assert.AreEqual(2, dataStoreCheckRuns.Count);
        foreach (var run in dataStoreCheckRuns)
        {
            TestContext?.WriteLine($"  CheckRun: {run.Id}: {run.Name} - {run.Status} - {run.Conclusion}");

            Assert.IsTrue(run.Id == 1 || run.Id == 2);

            if (run.Id == 1)
            {
                Assert.AreEqual("Build x86", run.Name);
                Assert.AreEqual(16, run.InternalId);
                Assert.AreEqual(CheckConclusion.Failure, run.Conclusion);
                Assert.AreEqual(CheckStatus.Completed, run.Status);
            }

            if (run.Id == 2)
            {
                Assert.AreEqual("Build x64", run.Name);
                Assert.AreEqual(7, run.InternalId);
                Assert.AreEqual(CheckConclusion.Success, run.Conclusion);
                Assert.AreEqual(CheckStatus.Completed, run.Status);
            }
        }

        // Verify objects work.
        var pullRequest = DataModel.PullRequest.GetById(dataStore, 1);
        Assert.IsNotNull(pullRequest);
        var checksForPullRequest = pullRequest.Checks;
        Assert.IsNotNull(checksForPullRequest);
        Assert.AreEqual(2, checksForPullRequest.Count());
        foreach (var check in checksForPullRequest)
        {
            TestContext?.WriteLine($"  PR 1 - CheckRun: {check.Id}: {check.Name} - {check.Status} - {check.Conclusion}");
            Assert.IsTrue(check.Completed);
        }

        var failedChecks = pullRequest.FailedChecks;
        Assert.IsNotNull(failedChecks);
        Assert.AreEqual(1, failedChecks.Count());

        Assert.AreEqual(CheckStatus.Completed, pullRequest.ChecksStatus);
        Assert.AreEqual(CheckConclusion.Failure, pullRequest.ChecksConclusion);

        testListener.PrintEventCounts();
        Assert.AreEqual(false, testListener.FoundErrors());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ReadAndWriteStatusAndNotification()
    {
        // Also includes notifications.
        using var log = new Logger("TestStore", TestOptions.LogOptions);
        var testListener = new TestListener("TestListener", TestContext!);
        log.AddListener(testListener);
        Log.Attach(log);

        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        Assert.IsNotNull(dataStore);
        dataStore.Create();
        Assert.IsNotNull(dataStore.Connection);

        // Add CheckRuns.
        var checks = new List<CheckRun>
        {
            { new CheckRun { HeadSha = "1234abcd", Name = "Build x86", InternalId = 16, ConclusionId = 1, StatusId = 3, DetailsUrl = "https://link/to/failed/build" } },
            { new CheckRun { HeadSha = "1234abcd", Name = "Build x64", InternalId = 7, ConclusionId = 7, StatusId = 3 } },
        };

        dataStore.Connection.Insert(checks[0]);
        dataStore.Connection.Insert(checks[1]);

        var checkSuite = new CheckSuite { HeadSha = "1234abcd", Name = "Azure Pipelines Build", InternalId = 1, ConclusionId = 1, StatusId = 3, HtmlUrl = "https://link/to/failed/build" };
        dataStore.Connection.Insert(checkSuite);

        // Add User record
        dataStore.Connection.Insert(new User { Login = "Kittens", InternalId = 16, AvatarUrl = "https://www.microsoft.com", Type = "Cat" });

        // Add repository record
        dataStore.Connection.Insert(new Repository { OwnerId = 1, InternalId = 47, Name = "TestRepo1", Description = "Short Desc", HtmlUrl = "https://www.microsoft.com", DefaultBranch = "main", HasIssues = 1 });

        // Add PullRequest record
        dataStore.Connection.Insert(new PullRequest { Title = "Fix the things", InternalId = 42, Number = 5, HeadSha = "1234abcd", AuthorId = 1, RepositoryId = 1 });

        // Add PullRequestStatus
        var pullRequest = DataModel.PullRequest.GetById(dataStore, 1);
        Assert.IsNotNull(pullRequest);
        var prStatus = PullRequestStatus.Add(dataStore, pullRequest);

        TestContext?.WriteLine($"  PR: {prStatus.PullRequest.Number} Status: {prStatus.Id}: {prStatus.Conclusion} - {prStatus.DetailsUrl}");
        Assert.AreEqual("https://link/to/failed/build", prStatus.DetailsUrl);
        Assert.AreEqual(1, prStatus.ConclusionId);

        // Create notification from PR Status
        var notification = Notification.Create(dataStore, prStatus, NotificationType.CheckRunFailed);
        Assert.IsNotNull(notification);
        Assert.AreEqual("Fix the things", notification.Title);
        Assert.AreEqual(1, notification.RepositoryId);
        Assert.AreEqual("Kittens", notification.User.Login);
        Assert.AreEqual("https://link/to/failed/build", notification.DetailsUrl);
        TestContext?.WriteLine($"  {notification.Title}");
        TestContext?.WriteLine($"  {notification.Description}");
        TestContext?.WriteLine($"  {notification.DetailsUrl}");

        testListener.PrintEventCounts();
        Assert.AreEqual(false, testListener.FoundErrors());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void ReadAndWriteReview()
    {
        using var log = new Logger("TestStore", TestOptions.LogOptions);
        var testListener = new TestListener("TestListener", TestContext!);
        log.AddListener(testListener);
        Log.Attach(log);

        using var dataStore = new DataStore("TestStore", TestHelpers.GetDataStoreFilePath(TestOptions), TestOptions.DataStoreOptions.DataStoreSchema!);
        Assert.IsNotNull(dataStore);
        dataStore.Create();
        Assert.IsNotNull(dataStore.Connection);

        using var tx = dataStore.Connection!.BeginTransaction();

        var reviews = new List<Review>
        {
            { new Review { PullRequestId = 1, AuthorId = 2, Body = "Review 1", InternalId = 16, State = "Approved" } },
            { new Review { PullRequestId = 1, AuthorId = 3, Body = "Review 2", InternalId = 47, State = "Rejected" } },
        };

        dataStore.Connection.Insert(reviews[0]);
        dataStore.Connection.Insert(reviews[1]);

        // Add User record
        dataStore.Connection.Insert(new User { Login = "Kittens", InternalId = 6, AvatarUrl = "https://www.microsoft.com", Type = "Cat" });
        dataStore.Connection.Insert(new User { Login = "Doggos", InternalId = 83, AvatarUrl = "https://www.microsoft.com", Type = "Dog" });
        dataStore.Connection.Insert(new User { Login = "Lizards", InternalId = 3, AvatarUrl = "https://www.microsoft.com", Type = "Reptile" });

        // Add repository record
        dataStore.Connection.Insert(new Repository { OwnerId = 1, InternalId = 47, Name = "TestRepo1", Description = "Short Desc", HtmlUrl = "https://www.microsoft.com", DefaultBranch = "main", HasIssues = 1 });

        // Add PullRequest record
        dataStore.Connection.Insert(new PullRequest { Title = "Fix the things", InternalId = 42, HeadSha = "1234abcd", AuthorId = 1, RepositoryId = 1 });

        tx.Commit();

        // Verify retrieval and input into data objects.
        var dataStoreReviews = dataStore.Connection.GetAll<Review>().ToList();
        Assert.AreEqual(2, dataStoreReviews.Count);
        foreach (var review in dataStoreReviews)
        {
            TestContext?.WriteLine($"  Review: {review.Id}: {review.Body} - {review.State}");

            Assert.IsTrue(review.Id == 1 || review.Id == 2);
            Assert.AreEqual(review.Body, $"Review {review.Id}");
            Assert.IsTrue(review.AuthorId == review.Id + 1);
        }

        // Verify objects work.
        var pullRequest = PullRequest.GetById(dataStore, 1);
        Assert.IsNotNull(pullRequest);
        var reviewsForPullRequest = pullRequest.Reviews;
        Assert.IsNotNull(reviewsForPullRequest);
        Assert.AreEqual(2, reviewsForPullRequest.Count());
        foreach (var review in reviewsForPullRequest)
        {
            TestContext?.WriteLine($"  PR 1 - Review: {review}");
            Assert.IsTrue(review.PullRequestId == 1);
        }

        testListener.PrintEventCounts();
        Assert.AreEqual(false, testListener.FoundErrors());
    }
}
