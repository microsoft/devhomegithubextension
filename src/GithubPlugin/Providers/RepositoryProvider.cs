// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Xml.Linq;
using GitHubPlugin.Client;
using GitHubPlugin.DeveloperId;
using GitHubPlugin.Helpers;
using Microsoft.Windows.DevHome.SDK;
using Octokit;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace GitHubPlugin.Providers;

public class RepositoryProvider : IRepositoryProvider
{
    public string DisplayName => Resources.GetResource(@"RepositoryProviderDisplayName");

    private Dictionary<string, Repository> _repositories;

    public IRandomAccessStreamReference Icon
    {
        get; private set;
    }

    public RepositoryProvider(IRandomAccessStreamReference icon)
    {
        Icon = icon;
        _repositories = new ();
    }

    public RepositoryProvider()
    {
        _repositories = new ();
        Icon = RandomAccessStreamReference.CreateFromUri(new Uri("https://www.github.com/microsoft/devhome"));
    }

    public IAsyncOperation<RepositoryUriSupportResult> IsUriSupportedAsync(Uri uri)
    {
        return Task.Run(() =>
        {
            if (!Validation.IsValidGitHubURL(uri))
            {
                return new RepositoryUriSupportResult(false);
            }

            Octokit.Repository? ocktokitRepo = null;
            var owner = Validation.ParseOwnerFromGitHubURL(uri);
            var repoName = Validation.ParseRepositoryFromGitHubURL(uri);

            if (_repositories.ContainsKey(Path.Join(owner, repoName)))
            {
                return new RepositoryUriSupportResult(true);
            }

            try
            {
                ocktokitRepo = GitHubClientProvider.Instance.GetClient().Repository.Get(owner, repoName).Result;
                _repositories.Add(Path.Join(owner, repoName), ocktokitRepo);
            }
            catch (Exception e)
            {
                if (e is Octokit.NotFoundException)
                {
                    Log.Logger()?.ReportError($"Can't find {owner}/{repoName}");
                    return new RepositoryUriSupportResult(e, $"Can't find {owner}/{repoName}");
                }

                if (e is Octokit.ForbiddenException)
                {
                    Log.Logger()?.ReportError($"Forbidden access to {owner}/{repoName}");
                    return new RepositoryUriSupportResult(e, $"Forbidden access to {owner}/{repoName}");
                }

                if (e is Octokit.RateLimitExceededException)
                {
                    Log.Logger()?.ReportError("Rate limit exceeded.", e);
                    return new RepositoryUriSupportResult(e, "Rate limit exceeded.");
                }
            }

            return new RepositoryUriSupportResult(true);
        }).AsAsyncOperation();
    }

    public IAsyncOperation<RepositoryUriSupportResult> IsUriSupportedAsync(Uri uri, IDeveloperId developerId) => throw new NotImplementedException();

    /// <summary>
    /// Tries to parse the uri to check if it is a valid Github URL and if the current account can find it.
    /// </summary>
    /// <param name="uri">The uri to check.</param>
    /// <returns>null repo if url isn't a github URL. Otherwise the repo that the URL points to.</returns>
    /// <exception cref="RepositoryNotFoundException">If no account has access to the repo or the repo can't be found.</exception>
    public IAsyncOperation<IRepository?> ParseRepositoryFromUrlAsync(Uri uri)
    {
        return Task.Run(() =>
        {
            if (!Validation.IsValidGitHubURL(uri))
            {
                return null;
            }

            // One of the logged in accounts could have access to the repo.
            // Go through all of them.  If no accounts have access
            // Throw to notify DevHome that no account has access to the repo.
            // Mostly because either the repo does not exist or is private.
            Octokit.Repository? ocktokitRepo = null;
            var owner = Validation.ParseOwnerFromGitHubURL(uri);
            var repoName = Validation.ParseRepositoryFromGitHubURL(uri);

            var loggedInDeveloperIds = DeveloperId.DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIdsInternal();

            if (!loggedInDeveloperIds.Any())
            {
                try
                {
                    ocktokitRepo = GitHubClientProvider.Instance.GetClient().Repository.Get(owner, repoName).Result;
                }
                catch (Exception e)
                {
                    if (e is Octokit.NotFoundException)
                    {
                        Log.Logger()?.ReportError($"Can't find {owner}/{repoName}");
                    }

                    if (e is Octokit.ForbiddenException)
                    {
                        Log.Logger()?.ReportError($"Forbidden access to {owner}/{repoName}");
                    }

                    if (e is Octokit.RateLimitExceededException)
                    {
                        Log.Logger()?.ReportError("Rate limit exceeded.", e);
                    }

                    throw;
                }
            }

            foreach (var loggedInDeveloperId in loggedInDeveloperIds)
            {
                try
                {
                    ocktokitRepo = loggedInDeveloperId.GitHubClient.Repository.Get(owner, repoName).Result;
                    break;
                }
                catch (Exception e)
                {
                    if (e is Octokit.NotFoundException)
                    {
                        Log.Logger()?.ReportError($"DeveloperId {loggedInDeveloperId.LoginId} did not find {owner}/{repoName}");
                        continue;
                    }

                    if (e is Octokit.ForbiddenException)
                    {
                        Log.Logger()?.ReportError($"DeveloperId {loggedInDeveloperId.LoginId} has forbidden access to {owner}/{repoName}");
                        continue;
                    }

                    if (e is Octokit.RateLimitExceededException)
                    {
                        Log.Logger()?.ReportError($"DeveloperId {loggedInDeveloperId.LoginId} rate limit exceeded.", e);
                        throw;
                    }
                }
            }

            if (ocktokitRepo != null)
            {
                return new DevHomeRepository(ocktokitRepo) as IRepository;
            }
            else
            {
                throw new RepositoryNotFoundException($"The repository {owner}/{repoName} could not be accessed by any available developer accounts.");
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<IEnumerable<IRepository>> GetRepositoriesAsync(IDeveloperId developerId)
    {
        return Task.Run(async () =>
        {
            // This is fetching all repositories available to the specified DevId.
            // We are not using the datastore cache for this query, it will always go to GitHub directly.
            var repositoryList = new List<IRepository>();
            try
            {
                ApiOptions apiOptions = new ();
                apiOptions.PageSize = 50;
                apiOptions.PageCount = 1;

                // Authenticate as the specified developer Id.
                var client = DeveloperIdProvider.GetInstance().GetDeveloperIdInternal(developerId).GitHubClient;
                RepositoryRequest request = new RepositoryRequest();
                request.Sort = RepositorySort.Updated;
                request.Direction = SortDirection.Descending;
                request.Affiliation = RepositoryAffiliation.Owner;

                // Gets only public repos for the owned repos.
                request.Visibility = RepositoryRequestVisibility.Public;
                var getPublicReposTask = client.Repository.GetAllForUser(developerId.LoginId, apiOptions);

                // this is getting private org and user repos.
                request.Visibility = RepositoryRequestVisibility.Private;
                var getPrivateReposTask = client.Repository.GetAllForCurrent(request, apiOptions);

                // This gets all org repos.
                request.Visibility = RepositoryRequestVisibility.All;
                request.Affiliation = RepositoryAffiliation.CollaboratorAndOrganizationMember;
                var getAllOrgReposTask = client.Repository.GetAllForCurrent(request, apiOptions);

                var publicRepos = await getPublicReposTask;
                var privateRepos = await getPrivateReposTask;
                var orgRepos = await getAllOrgReposTask;

                var allRepos = publicRepos.Union(privateRepos).Union(orgRepos);

                foreach (var repository in allRepos.OrderByDescending(x => x.UpdatedAt))
                {
                    repositoryList.Add(new DevHomeRepository(repository));
                }
            }
            catch (Exception ex)
            {
                // Any failures should be thrown so the core app can catch the failures.
                Providers.Log.Logger()?.ReportError("RepositoryProvider", "Failed getting list of repositories.", ex);
                throw;
            }

            return repositoryList.AsEnumerable();
        }).AsAsyncOperation();
    }

    IAsyncOperation<RepositoriesResult> IRepositoryProvider.GetRepositoriesAsync(IDeveloperId developerId) => throw new NotImplementedException();

    public IAsyncOperation<RepositoriesResult> GetRepositoryFromUriAsync(Uri uri) => throw new NotImplementedException();

    public IAsyncOperation<RepositoriesResult> GetRepositoryFromUriAsync(Uri uri, IDeveloperId developerId) => throw new NotImplementedException();

    public IAsyncOperation<ProviderOperationResult> CloneRepositoryAsync(IRepository repository, string cloneDestination) => throw new NotImplementedException();

    public IAsyncOperation<ProviderOperationResult> CloneRepositoryAsync(IRepository repository, string cloneDestination, IDeveloperId developerId) => throw new NotImplementedException();

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
