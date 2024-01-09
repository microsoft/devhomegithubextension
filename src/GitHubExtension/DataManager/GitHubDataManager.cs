// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubExtension.Client;
using GitHubExtension.DataManager;
using GitHubExtension.DataModel;
using GitHubExtension.Helpers;
using Windows.Storage;

namespace GitHubExtension;
public partial class GitHubDataManager : IGitHubDataManager, IDisposable
{
    public static event DataManagerUpdateEventHandler? OnUpdate;

    private static readonly string LastUpdatedKeyName = "LastUpdated";
    private static readonly TimeSpan NotificationRetentionTime = TimeSpan.FromDays(7);
    private static readonly TimeSpan SearchRetentionTime = TimeSpan.FromDays(7);
    private static readonly TimeSpan PullRequestStaleTime = TimeSpan.FromDays(1);

    // It is possible different widgets have queries which touch the same pull requests.
    // We want to keep this window large enough that we don't delete data being used by
    // other clients which simply haven't been updated yet but will in the near future.
    // This is a conservative time period to check for pruning and give time for other
    // consumers using the data to update its freshness before we remove it.
    private static readonly TimeSpan LastObservedDeleteSpan = TimeSpan.FromMinutes(6);
    private static readonly long CheckSuiteIdDependabot = 29110;

    private static readonly string Name = nameof(GitHubDataManager);

    private DataStore DataStore { get; set; }

    public DataStoreOptions DataStoreOptions { get; private set; }

    public static IGitHubDataManager? CreateInstance(DataStoreOptions? options = null)
    {
        options ??= DefaultOptions;

        try
        {
            return new GitHubDataManager(options);
        }
        catch (Exception e)
        {
            Log.Logger()?.ReportError(Name, "Failed creating GitHubDataManager", e);
            Environment.FailFast(e.Message, e);
            return null;
        }
    }

    public GitHubDataManager(DataStoreOptions dataStoreOptions)
    {
        if (dataStoreOptions.DataStoreSchema == null)
        {
            throw new ArgumentNullException(nameof(dataStoreOptions), "DataStoreSchema cannot be null.");
        }

        DataStoreOptions = dataStoreOptions;

        DataStore = new DataStore(
            "DataStore",
            Path.Combine(dataStoreOptions.DataStoreFolderPath, dataStoreOptions.DataStoreFileName),
            dataStoreOptions.DataStoreSchema);
        DataStore.Create();
    }

    public DateTime LastUpdated
    {
        get
        {
            ValidateDataStore();
            var lastUpdated = MetaData.Get(DataStore, LastUpdatedKeyName);
            if (lastUpdated == null)
            {
                return DateTime.MinValue;
            }

            return lastUpdated.ToDateTime();
        }
    }

    public async Task UpdateAllDataForRepositoryAsync(string owner, string name, RequestOptions? options = null)
    {
        ValidateDataStore();
        var parameters = new DataStoreOperationParameters
        {
            Owner = owner,
            RepositoryName = name,
            RequestOptions = options,
            OperationName = "UpdateAllDataForRepositoryAsync",
        };

        await UpdateDataForRepositoryAsync(
            parameters,
            async (parameters, devId) =>
            {
                var repository = await UpdateRepositoryAsync(parameters.Owner!, parameters.RepositoryName!, devId.GitHubClient);
                await UpdateIssuesAsync(repository, devId.GitHubClient, parameters.RequestOptions);
                await UpdatePullRequestsAsync(repository, devId.GitHubClient, parameters.RequestOptions);
            });

        SendRepositoryUpdateEvent(this, GetFullNameFromOwnerAndRepository(owner, name), new string[] { "Issues", "PullRequests" });
    }

    public async Task UpdateAllDataForRepositoryAsync(string fullName, RequestOptions? options = null)
    {
        ValidateDataStore();
        var nameSplit = GetOwnerAndRepositoryNameFromFullName(fullName);
        await UpdateAllDataForRepositoryAsync(nameSplit[0], nameSplit[1], options);
    }

    public async Task UpdatePullRequestsForRepositoryAsync(string owner, string name, RequestOptions? options = null)
    {
        ValidateDataStore();
        var parameters = new DataStoreOperationParameters
        {
            Owner = owner,
            RepositoryName = name,
            RequestOptions = options,
            OperationName = "UpdatePullRequestsForRepositoryAsync",
        };

        await UpdateDataForRepositoryAsync(
            parameters,
            async (parameters, devId) =>
            {
                var repository = await UpdateRepositoryAsync(parameters.Owner!, parameters.RepositoryName!, devId.GitHubClient);
                await UpdatePullRequestsAsync(repository, devId.GitHubClient, parameters.RequestOptions);
            });

        SendRepositoryUpdateEvent(this, GetFullNameFromOwnerAndRepository(owner, name), new string[] { "PullRequests" });
    }

    public async Task UpdatePullRequestsForRepositoryAsync(string fullName, RequestOptions? options = null)
    {
        ValidateDataStore();
        var nameSplit = GetOwnerAndRepositoryNameFromFullName(fullName);
        await UpdatePullRequestsForRepositoryAsync(nameSplit[0], nameSplit[1], options);
    }

    public async Task UpdateIssuesForRepositoryAsync(string owner, string name, RequestOptions? options = null)
    {
        ValidateDataStore();
        var parameters = new DataStoreOperationParameters
        {
            Owner = owner,
            RepositoryName = name,
            RequestOptions = options,
            OperationName = "UpdateIssuesForRepositoryAsync",
        };

        await UpdateDataForRepositoryAsync(
            parameters,
            async (parameters, devId) =>
            {
                var repository = await UpdateRepositoryAsync(parameters.Owner!, parameters.RepositoryName!, devId.GitHubClient);
                await UpdateIssuesAsync(repository, devId.GitHubClient, parameters.RequestOptions);
            });

        SendRepositoryUpdateEvent(this, GetFullNameFromOwnerAndRepository(owner, name), new string[] { "Issues" });
    }

    public async Task UpdateIssuesForRepositoryAsync(string fullName, RequestOptions? options = null)
    {
        ValidateDataStore();
        var nameSplit = GetOwnerAndRepositoryNameFromFullName(fullName);
        await UpdateIssuesForRepositoryAsync(nameSplit[0], nameSplit[1], options);
    }

    public async Task UpdatePullRequestsForLoggedInDeveloperIdsAsync()
    {
        ValidateDataStore();
        var parameters = new DataStoreOperationParameters
        {
            OperationName = "UpdatePullRequestsForLoggedInDeveloperIdsAsync",
        };
        await UpdateDataStoreAsync(parameters, UpdatePullRequestsForLoggedInDeveloperIdsAsync);
        SendDeveloperUpdateEvent(this);
    }

    public IEnumerable<Repository> GetRepositories()
    {
        ValidateDataStore();
        return Repository.GetAll(DataStore);
    }

    public Repository? GetRepository(string owner, string name)
    {
        ValidateDataStore();
        return Repository.Get(DataStore, owner, name);
    }

    public Repository? GetRepository(string fullName)
    {
        ValidateDataStore();
        return Repository.Get(DataStore, fullName);
    }

    public IEnumerable<User> GetDeveloperUsers()
    {
        ValidateDataStore();
        return User.GetDeveloperUsers(DataStore);
    }

    public IEnumerable<Notification> GetNotifications(DateTime? since = null, bool includeToasted = false)
    {
        ValidateDataStore();
        return Notification.Get(DataStore, since, includeToasted);
    }

    // Wrapper for database operations for consistent handling.
    private async Task UpdateDataStoreAsync(DataStoreOperationParameters parameters, Func<DataStoreOperationParameters, Task> asyncAction)
    {
        parameters.RequestOptions ??= RequestOptions.RequestOptionsDefault();
        parameters.DeveloperIds = DeveloperId.DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIdsInternal();
        using var tx = DataStore.Connection!.BeginTransaction();

        try
        {
            // Do the action on the repository for the client.
            await asyncAction(parameters);

            // Clean datastore and set last updated after updating.
            PruneObsoleteData();
            SetLastUpdatedInMetaData();
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(Name, $"Failed Updating DataStore for: {parameters}", ex);
            tx.Rollback();

            // Rethrow so clients can catch/display an error UX.
            throw;
        }

        tx.Commit();
        Log.Logger()?.ReportInfo(Name, $"Updated datastore: {parameters}");
    }

    // Wrapper for the targeted repository update pattern.
    // This is where we are querying specific data.
    private async Task UpdateDataForRepositoryAsync(DataStoreOperationParameters parameters, Func<DataStoreOperationParameters, DeveloperId.DeveloperId, Task> asyncAction)
    {
        parameters.RequestOptions ??= RequestOptions.RequestOptionsDefault();
        parameters.DeveloperIds = DeveloperId.DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIdsInternal();

        ValidateRepositoryOwnerAndName(parameters.Owner!, parameters.RepositoryName!);
        if (parameters.RequestOptions.UsePublicClientAsFallback)
        {
            // Append the public client to the list of developer accounts. This will have us try the public client as a fallback.
            parameters.DeveloperIds = parameters.DeveloperIds.Concat(new[] { new DeveloperId.DeveloperId() });
        }

        using var tx = DataStore.Connection!.BeginTransaction();
        try
        {
            var found = false;

            // We only need to get the information from one account which has access.
            foreach (var devId in parameters.DeveloperIds)
            {
                try
                {
                    // Try the action for the passed in developer Id.
                    await asyncAction(parameters, DeveloperId.DeveloperIdProvider.GetInstance().GetDeveloperIdInternal(devId));

                    // We can stop when the action is executed without exceptions.
                    found = true;
                    break;
                }
                catch (Exception ex)
                {
                    if (ex is Octokit.ForbiddenException)
                    {
                        // This can happen most commonly with SAML-enabled organizations.
                        Log.Logger()?.ReportDebug(Name, $"DeveloperId {devId.LoginId} was forbidden access to {parameters.Owner}/{parameters.RepositoryName}");
                        continue;
                    }

                    if (ex is Octokit.NotFoundException)
                    {
                        // A private repository can come back as "not found" by the GitHub API when an unauthorized account cannot even view it.
                        Log.Logger()?.ReportDebug(Name, $"DeveloperId {devId.LoginId} did not find {parameters.Owner}/{parameters.RepositoryName}");
                        continue;
                    }

                    if (ex is Octokit.RateLimitExceededException)
                    {
                        Log.Logger()?.ReportError(Name, $"DeveloperId {devId.LoginId} rate limit exceeded.", ex);
                        throw;
                    }

                    throw;
                }
            }

            if (!found)
            {
                throw new RepositoryNotFoundException($"The repository {parameters.Owner}/{parameters.RepositoryName} could not be accessed by any available developer accounts.");
            }

            // Clean datastore and set last updated after updating.
            PruneObsoleteData();
            SetLastUpdatedInMetaData();
        }
        catch (Exception ex)
        {
            // This is for catching any other unexpected error as well as any we throw.
            Log.Logger()?.ReportError(Name, $"Failed trying update data for repository: {parameters.Owner}/{parameters.RepositoryName}", ex);
            tx.Rollback();
            throw;
        }

        tx.Commit();
        Log.Logger()?.ReportInfo(Name, $"Updated datastore: {parameters}");
    }

    // Find all pull requests the developer, and update them.
    // This is intended to be called from within InternalUpdateDataStoreAsync.
    private async Task UpdatePullRequestsForLoggedInDeveloperIdsAsync(DataStoreOperationParameters parameters)
    {
        var devIds = DeveloperId.DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIdsInternal();
        if (devIds is null || !devIds.Any())
        {
            // This may not be an error if the user has not yet logged in with a DevId.
            Log.Logger()?.ReportInfo(Name, "Called to update all pull requests for a user with no logged in developer.");
            return;
        }

        // Get pull requests for each logged in developer Id.
        foreach (var devId in devIds)
        {
            // Get the list of all of a user's open pull requests for the logged in developer.
            var searchIssuesRequest = new Octokit.SearchIssuesRequest()
            {
                Author = devId.LoginId,
                State = Octokit.ItemState.Open,
                Type = Octokit.IssueTypeQualifier.PullRequest,
            };

            var searchResult = await devId.GitHubClient.Search.SearchIssues(searchIssuesRequest);
            var repositoryDict = new Dictionary<string, bool>();
            foreach (var issue in searchResult.Items)
            {
                // The Issue search result does not give us enough information to collect
                // what we need about pull requests. So, instead of trying to convert
                // these results into incomplete data objects, we will get the repositories
                // to which these pull requests belong, and then do a Repository pull
                // request query which will have all of the information needed.
                repositoryDict[Validation.ParseFullNameFromGitHubURL(issue.HtmlUrl)] = true;
            }

            // Set request options to get all open PRs for this user.
            var requestOptions = new RequestOptions
            {
                PullRequestRequest = new Octokit.PullRequestRequest
                {
                    State = Octokit.ItemStateFilter.Open,
                },
                ApiOptions = new Octokit.ApiOptions
                {
                    // Use default, which will get all.
                },
            };

            foreach (var repoFullName in repositoryDict.Keys)
            {
                var repoName = GetOwnerAndRepositoryNameFromFullName(repoFullName);
                Octokit.Repository octoRepository;
                try
                {
                    octoRepository = await devId.GitHubClient.Repository.Get(repoName[0], repoName[1]);
                }
                catch (Octokit.ForbiddenException ex)
                {
                    // The list of pull requests produced from the issues search does not account
                    // for SAML enforcement. Skip this repository if Forbidden. This may be
                    // opportunity to prompt user to fix the issue, but not if it is a background
                    // update. Consider placing these errors in an AccessDenied table and allowing
                    // the UI to query it to attempt to resolve any issues discovered in the main
                    // app UX, such as via a user prompt that tells them not all of their data
                    // could be accessed.
                    Log.Logger()?.ReportWarn(Name, $"Forbidden exception while trying to query repository {repoFullName}: {ex.Message}");
                    continue;
                }

                var dsRepository = Repository.GetOrCreateByOctokitRepository(DataStore, octoRepository);
                var octoPullRequests = await devId.GitHubClient.PullRequest.GetAllForRepository(repoName[0], repoName[1], requestOptions.PullRequestRequest, requestOptions.ApiOptions);
                foreach (var octoPull in octoPullRequests)
                {
                    // We only care about pulls where the user is the Author.
                    if (octoPull.User.Login != devId.LoginId)
                    {
                        continue;
                    }

                    // Update this pull request and associated CheckRuns and CheckSuites.
                    var dsPullRequest = PullRequest.GetOrCreateByOctokitPullRequest(DataStore, octoPull, dsRepository.Id);
                    CheckRun.DeleteAllForPullRequest(DataStore, dsPullRequest);
                    var octoCheckRunResponse = await devId.GitHubClient.Check.Run.GetAllForReference(repoName[0], repoName[1], dsPullRequest.HeadSha);
                    foreach (var run in octoCheckRunResponse.CheckRuns)
                    {
                        CheckRun.GetOrCreateByOctokitCheckRun(DataStore, run);
                    }

                    try
                    {
                        CheckSuite.DeleteAllForPullRequest(DataStore, dsPullRequest);
                        var octoCheckSuiteResponse = await devId.GitHubClient.Check.Suite.GetAllForReference(repoName[0], repoName[1], dsPullRequest.HeadSha);
                        foreach (var suite in octoCheckSuiteResponse.CheckSuites)
                        {
                            // Skip Dependabot, as it is not part of a pull request's blocking suites.
                            if (suite.App.Id == CheckSuiteIdDependabot)
                            {
                                continue;
                            }

                            Log.Logger()?.ReportDebug($"Suite: {suite.App.Name} - {suite.App.Id} - {suite.App.Owner.Login}  Conclusion: {suite.Conclusion}  Status: {suite.Status}");
                            CheckSuite.GetOrCreateByOctokitCheckSuite(DataStore, suite);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Logger()?.ReportError($"Check suite error for Pull Request #{octoPull.Number}: " + e.Message);
                    }

                    var commitCombinedStatus = await devId.GitHubClient.Repository.Status.GetCombined(dsRepository.InternalId, dsPullRequest.HeadSha);
                    CommitCombinedStatus.GetOrCreate(DataStore, commitCombinedStatus);

                    CreatePullRequestStatus(dsPullRequest);
                }

                Log.Logger()?.ReportDebug(Name, $"Updated developer pull requests for {repoFullName}.");
            }

            // After we update for this developer, remove all pull requests for this developer that
            // were not observed recently.
            PullRequest.DeleteAllByDeveloperLoginAndLastObservedBefore(DataStore, devId.LoginId, DateTime.UtcNow - LastObservedDeleteSpan);
        }
    }

    // Internal method to update a repository.
    // DataStore transaction is assumed to be wrapped around this in the public method.
    private async Task<Repository> UpdateRepositoryAsync(string owner, string repositoryName, Octokit.GitHubClient? client = null)
    {
        client ??= await GitHubClientProvider.Instance.GetClientForLoggedInDeveloper(true);
        Log.Logger()?.ReportInfo(Name, $"Updating repository: {owner}/{repositoryName}");
        var octokitRepository = await client.Repository.Get(owner, repositoryName);
        return Repository.GetOrCreateByOctokitRepository(DataStore, octokitRepository);
    }

    // Internal method to update pull requests. Assumes Repository has already been populated and
    // created. DataStore transaction is assumed to be wrapped around this in the public method.
    private async Task UpdatePullRequestsAsync(Repository repository, Octokit.GitHubClient? client = null, RequestOptions? options = null)
    {
        options ??= RequestOptions.RequestOptionsDefault();
        client ??= await GitHubClientProvider.Instance.GetClientForLoggedInDeveloper(true);
        Log.Logger()?.ReportInfo(Name, $"Updating pull requests for: {repository.FullName}");
        var octoPulls = await client.PullRequest.GetAllForRepository(repository.InternalId, options.PullRequestRequest, options.ApiOptions);
        foreach (var pull in octoPulls)
        {
            var dsPullRequest = PullRequest.GetOrCreateByOctokitPullRequest(DataStore, pull, repository.Id);
            CheckRun.DeleteAllForPullRequest(DataStore, dsPullRequest);
            var octoCheckRunResponse = await client.Check.Run.GetAllForReference(repository.InternalId, dsPullRequest.HeadSha);
            foreach (var run in octoCheckRunResponse.CheckRuns)
            {
                CheckRun.GetOrCreateByOctokitCheckRun(DataStore, run);
            }

            try
            {
                CheckSuite.DeleteAllForPullRequest(DataStore, dsPullRequest);
                var octoCheckSuiteResponse = await client.Check.Suite.GetAllForReference(repository.InternalId, dsPullRequest.HeadSha);

                foreach (var suite in octoCheckSuiteResponse.CheckSuites)
                {
                    // Skip Dependabot, as it is not part of a pull request's blocking suites.
                    if (suite.App.Id == CheckSuiteIdDependabot)
                    {
                        continue;
                    }

                    Log.Logger()?.ReportDebug($"Suite: {suite.App.Name} - {suite.App.Id} - {suite.App.Owner.Login}  Conclusion: {suite.Conclusion}  Status: {suite.Status}");
                    CheckSuite.GetOrCreateByOctokitCheckSuite(DataStore, suite);
                }
            }
            catch (Exception e)
            {
                Log.Logger()?.ReportError($"Check suite error for Pull Request #{pull.Number}: " + e.Message);
            }

            var commitCombinedStatus = await client.Repository.Status.GetCombined(repository.InternalId, dsPullRequest.HeadSha);
            CommitCombinedStatus.GetOrCreate(DataStore, commitCombinedStatus);

            CreatePullRequestStatus(dsPullRequest);
        }

        // Remove unobserved pull requests from this repository.
        PullRequest.DeleteLastObservedBefore(DataStore, repository.Id, DateTime.UtcNow - LastObservedDeleteSpan);
    }

    private void CreatePullRequestStatus(PullRequest pullRequest)
    {
        // Get the previous status for comparison.
        var prevStatus = PullRequestStatus.Get(DataStore, pullRequest);

        // Create the new status.
        var curStatus = PullRequestStatus.Add(DataStore, pullRequest);

        if (ShouldCreateCheckFailureNotification(curStatus, prevStatus))
        {
            Log.Logger()?.ReportInfo(Name, "Notifications", $"Creating CheckRunFailure Notification for {curStatus}");
            var notification = Notification.Create(curStatus, NotificationType.CheckRunFailed);
            Notification.Add(DataStore, notification);
        }

        if (ShouldCreateCheckSucceededNotification(curStatus, prevStatus))
        {
            Log.Logger()?.ReportDebug(Name, "Notifications", $"Creating CheckRunSuccess Notification for {curStatus}");
            var notification = Notification.Create(curStatus, NotificationType.CheckRunSucceeded);
            Notification.Add(DataStore, notification);
        }
    }

    public bool ShouldCreateCheckFailureNotification(PullRequestStatus curStatus, PullRequestStatus? prevStatus)
    {
        // If the pull request is not recent, do not create a notification for it.
        if ((DateTime.Now - curStatus.PullRequest.UpdatedAt) > PullRequestStaleTime)
        {
            return false;
        }

        // Compare pull request status.
        if (prevStatus is null || prevStatus.HeadSha != curStatus.HeadSha)
        {
            // No previous status for this commit, assume new PR or freshly pushed commit with
            // checks likely running. Any check failures here are assumed to be notification worthy.
            if (curStatus.Failed)
            {
                return true;
            }
        }
        else
        {
            // A failure isn't necessarily notification worthy if we've already seen it.
            if (curStatus.Failed)
            {
                // If the previous status was not failed, or the failure was for a different
                // reason (example, it moved from ActionRequired -> Failure), that is
                // notification worthy.
                if (!prevStatus.Failed || curStatus.Conclusion != prevStatus.Conclusion)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool ShouldCreateCheckSucceededNotification(PullRequestStatus curStatus, PullRequestStatus? prevStatus)
    {
        // If the pull request is not recent, do not create a notification for it.
        if ((DateTime.Now - curStatus.PullRequest.UpdatedAt) > PullRequestStaleTime)
        {
            return false;
        }

        // Compare pull request status.
        if (prevStatus is null || prevStatus.HeadSha != curStatus.HeadSha)
        {
            // No previous status for this commit, assume new PR or freshly pushed commit that was
            // successful between our syncs.
            if (curStatus.Succeeded)
            {
                return true;
            }
        }
        else
        {
            // Only post success notifications if it wasn't previously successful.
            if (curStatus.Succeeded && !prevStatus.Succeeded)
            {
                return true;
            }
        }

        return false;
    }

    // Internal method to update issues. Assumes Repository has already been populated and created.
    // DataStore transaction is assumed to be wrapped around this in the public method.
    private async Task UpdateIssuesAsync(Repository repository, Octokit.GitHubClient? client = null, RequestOptions? options = null)
    {
        options ??= RequestOptions.RequestOptionsDefault();
        client ??= await GitHubClientProvider.Instance.GetClientForLoggedInDeveloper(true);
        Log.Logger()?.ReportInfo(Name, $"Updating issues for: {repository.FullName}");

        // Since we are only interested in issues and for a specific repository, we will override
        // these two properties. All other properties the caller can specify however they see fit.
        options.SearchIssuesRequest.Type = Octokit.IssueTypeQualifier.Issue;
        options.SearchIssuesRequest.Repos = new Octokit.RepositoryCollection { repository.FullName };

        var issuesResult = await client.Search.SearchIssues(options.SearchIssuesRequest);
        if (issuesResult == null)
        {
            Log.Logger()?.ReportDebug($"No issues found.");
            return;
        }

        // Associate search term if one was provided.
        Search? search = null;
        if (!string.IsNullOrEmpty(options.SearchIssuesRequest.Term))
        {
            Log.Logger()?.ReportDebug($"Term = {options.SearchIssuesRequest.Term}");
            search = Search.GetOrCreate(DataStore, options.SearchIssuesRequest.Term, repository.Id);
        }

        Log.Logger()?.ReportDebug(Name, $"Results contain {issuesResult.Items.Count} issues.");
        foreach (var issue in issuesResult.Items)
        {
            var dsIssue = Issue.GetOrCreateByOctokitIssue(DataStore, issue, repository.Id);
            if (search is not null)
            {
                SearchIssue.AddIssueToSearch(DataStore, dsIssue, search);
            }
        }

        if (search is not null)
        {
            // If this is associated with a search and there are existing issues in the search that
            // were not recently updated (within the last minute), remove them from the search result.
            SearchIssue.DeleteBefore(DataStore, search, DateTime.Now - TimeSpan.FromMinutes(1));
        }

        // Remove issues from this repository that were not observed recently.
        Issue.DeleteLastObservedBefore(DataStore, repository.Id, DateTime.UtcNow - LastObservedDeleteSpan);
    }

    // Removes unused data from the datastore.
    private void PruneObsoleteData()
    {
        CheckRun.DeleteUnreferenced(DataStore);
        CheckSuite.DeleteUnreferenced(DataStore);
        CommitCombinedStatus.DeleteUnreferenced(DataStore);
        PullRequestStatus.DeleteUnreferenced(DataStore);
        Notification.DeleteBefore(DataStore, DateTime.Now - NotificationRetentionTime);
        Search.DeleteBefore(DataStore, DateTime.Now - SearchRetentionTime);
        SearchIssue.DeleteUnreferenced(DataStore);
    }

    // Sets a last-updated in the MetaData.
    private void SetLastUpdatedInMetaData()
    {
        MetaData.AddOrUpdate(DataStore, LastUpdatedKeyName, DateTime.Now.ToDataStoreString());
    }

    // Converts fullName -> owner, name.
    private string[] GetOwnerAndRepositoryNameFromFullName(string fullName)
    {
        var nameSplit = fullName.Split(new[] { '/' });
        if (nameSplit.Length != 2 || string.IsNullOrEmpty(nameSplit[0]) || string.IsNullOrEmpty(nameSplit[1]))
        {
            Log.Logger()?.ReportError(Name, $"Invalid repository full name: {fullName}");
            throw new ArgumentException($"Invalid repository full name: {fullName}");
        }

        return nameSplit;
    }

    private string GetFullNameFromOwnerAndRepository(string owner, string repository)
    {
        return $"{owner}/{repository}";
    }

    private void ValidateRepositoryOwnerAndName(string owner, string repositoryName)
    {
        if (string.IsNullOrEmpty(owner))
        {
            throw new ArgumentNullException(nameof(owner));
        }

        if (string.IsNullOrEmpty(repositoryName))
        {
            throw new ArgumentNullException(nameof(repositoryName));
        }
    }

    private void ValidateDataStore()
    {
        if (DataStore is null || !DataStore.IsConnected)
        {
            throw new DataStoreInaccessibleException("DataStore is not available.");
        }
    }

    // Making the default options a singleton to avoid repeatedly calling the storage APIs and
    // creating a new GitHubDataStoreSchema when not necessary.
    private static readonly Lazy<DataStoreOptions> LazyDataStoreOptions = new (DefaultOptionsInit);

    private static DataStoreOptions DefaultOptions => LazyDataStoreOptions.Value;

    private static DataStoreOptions DefaultOptionsInit()
    {
        return new DataStoreOptions
        {
            DataStoreFolderPath = ApplicationData.Current.LocalFolder.Path,
            DataStoreSchema = new GitHubDataStoreSchema(),
        };
    }

    public override string ToString() => "GitHubDataManager";

    private bool disposed; // To detect redundant calls.

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            Log.Logger()?.ReportDebug(Name, "Disposing of all Disposable resources.");

            if (disposing)
            {
                if (DataStore != null)
                {
                    try
                    {
                        DataStore.Dispose();
                    }
                    catch
                    {
                    }
                }
            }

            disposed = true;
        }
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
