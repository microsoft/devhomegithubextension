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

    public IRandomAccessStreamReference Icon
    {
        get; private set;
    }

    public RepositoryProvider(IRandomAccessStreamReference icon)
    {
        Icon = icon;
    }

    public RepositoryProvider()
    {
        Icon = RandomAccessStreamReference.CreateFromUri(new Uri("https://www.GitHub.com/microsoft/devhome"));
    }

    public IAsyncOperation<RepositoryUriSupportResult> IsUriSupportedAsync(Uri uri)
    {
        return IsUriSupportedAsync(uri, null);
    }

    public IAsyncOperation<RepositoryUriSupportResult> IsUriSupportedAsync(Uri uri, IDeveloperId? developerId)
    {
        return Task.Run(() =>
        {
            if (!Validation.IsValidGitHubURL(uri))
            {
                return new RepositoryUriSupportResult(false);
            }

            var owner = Validation.ParseOwnerFromGitHubURL(uri);
            if (string.IsNullOrEmpty(owner))
            {
                return new RepositoryUriSupportResult(false);
            }

            var repoName = Validation.ParseRepositoryFromGitHubURL(uri);
            if (string.IsNullOrEmpty(repoName))
            {
                return new RepositoryUriSupportResult(false);
            }

            return new RepositoryUriSupportResult(true);
        }).AsAsyncOperation();
    }

    IAsyncOperation<RepositoriesResult> IRepositoryProvider.GetRepositoriesAsync(IDeveloperId developerId)
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
                return new RepositoriesResult(ex, $"Something went wrong.  HResult: {ex.HResult}");
            }

            return new RepositoriesResult(repositoryList.AsEnumerable());
        }).AsAsyncOperation();
    }

    public IAsyncOperation<RepositoryResult> GetRepositoryFromUriAsync(Uri uri)
    {
        return GetRepositoryFromUriAsync(uri, null);
    }

    public IAsyncOperation<RepositoryResult> GetRepositoryFromUriAsync(Uri uri, IDeveloperId? developerId)
    {
        return Task.Run(() =>
        {
            if (!Validation.IsValidGitHubURL(uri))
            {
                var exception = new ArgumentException("Uri is invalid.");
                return new RepositoryResult(exception, $"{exception.Message} HResult: {exception.HResult}");
            }

            Octokit.Repository? ocktokitRepo = null;
            var owner = Validation.ParseOwnerFromGitHubURL(uri);
            var repoName = Validation.ParseRepositoryFromGitHubURL(uri);

            try
            {
                GitHubClient gitHubClient;

                if (developerId != null)
                {
                    var loggedInDeveloperId = DeveloperId.DeveloperIdProvider.GetInstance().GetDeveloperIdInternal(developerId);
                    gitHubClient = loggedInDeveloperId.GitHubClient;
                }
                else
                {
                    gitHubClient = GitHubClientProvider.Instance.GetClient();
                }

                ocktokitRepo = gitHubClient.Repository.Get(owner, repoName).Result;
            }
            catch (AggregateException e)
            {
                var innerException = e.InnerException;
                if (innerException is Octokit.NotFoundException)
                {
                    Log.Logger()?.ReportError($"Can't find {owner}/{repoName}");
                    return new RepositoryResult(innerException, $"Can't find {owner}/{repoName}. HResult: {innerException.HResult}");
                }

                if (innerException is Octokit.ForbiddenException)
                {
                    Log.Logger()?.ReportError($"Forbidden access to {owner}/{repoName}");
                    return new RepositoryResult(innerException, $"Forbidden access to {owner}/{repoName}. HResult: {innerException.HResult}");
                }

                if (innerException is Octokit.RateLimitExceededException)
                {
                    Log.Logger()?.ReportError("Rate limit exceeded.", e);
                    return new RepositoryResult(innerException, $"Rate limit exceeded. HResult: {innerException.HResult}");
                }
            }
            catch (Exception e)
            {
                Log.Logger()?.ReportError("Unspecified error.", e);
                return new RepositoryResult(e, $"Unspecified error when cloning a repo. HResult: {e.HResult}");
            }

            if (ocktokitRepo == null)
            {
                return new RepositoryResult(new ArgumentException("Repo is still null"), "Repo is still null");
            }
            else
            {
                return new RepositoryResult(new DevHomeRepository(ocktokitRepo));
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ProviderOperationResult> CloneRepositoryAsync(IRepository repository, string cloneDestination)
    {
        return CloneRepositoryAsync(repository, cloneDestination, null);
    }

    public IAsyncOperation<ProviderOperationResult> CloneRepositoryAsync(IRepository repository, string cloneDestination, IDeveloperId? developerId)
    {
        return Task.Run(() =>
        {
            var cloneOptions = new LibGit2Sharp.CloneOptions
            {
                Checkout = true,
            };

            if (developerId != null)
            {
                var loggedInDeveloperId = DeveloperId.DeveloperIdProvider.GetInstance().GetDeveloperIdInternal(developerId);

                cloneOptions.CredentialsProvider = (url, user, cred) => new LibGit2Sharp.UsernamePasswordCredentials
                {
                    // Password is a PAT unique to GitHub.
                    Username = loggedInDeveloperId.GetCredential().Password,
                    Password = string.Empty,
                };
            }

            try
            {
                // Exceptions happen.
                LibGit2Sharp.Repository.Clone(repository.RepoUri.OriginalString, cloneDestination, cloneOptions);
            }
            catch (LibGit2Sharp.RecurseSubmodulesException recurseException)
            {
                Providers.Log.Logger()?.ReportError("DevHomeRepository", "Could not clone all sub modules", recurseException);
                return new ProviderOperationResult(ProviderOperationStatus.Failure, recurseException, "Could not clone all modules", recurseException.Message);
            }
            catch (LibGit2Sharp.UserCancelledException userCancelledException)
            {
                Providers.Log.Logger()?.ReportError("DevHomeRepository", "The user stoped the clone operation", userCancelledException);
                return new ProviderOperationResult(ProviderOperationStatus.Failure, userCancelledException, "User cancalled the operation", userCancelledException.Message);
            }
            catch (LibGit2Sharp.NameConflictException nameConflictException)
            {
                Providers.Log.Logger()?.ReportError("DevHomeRepository", nameConflictException);
                return new ProviderOperationResult(ProviderOperationStatus.Failure, nameConflictException, "The location exists and is non-empty", nameConflictException.Message);
            }
            catch (LibGit2Sharp.LibGit2SharpException libGitTwoException)
            {
                Providers.Log.Logger()?.ReportError("DevHomeRepository", $"Either no logged in account has access to this repo, or the repo can't be found", libGitTwoException);
                return new ProviderOperationResult(ProviderOperationStatus.Failure, libGitTwoException, "LigGit2 threw an exception", "LibGit2 Threw an exception");
            }
            catch (Exception e)
            {
                Providers.Log.Logger()?.ReportError("DevHomeRepository", "Could not clone the repository", e);
                return new ProviderOperationResult(ProviderOperationStatus.Failure, e, "Something happened when cloning the repo", "something happened when cloning the repo");
            }

            return new ProviderOperationResult(ProviderOperationStatus.Success, new ArgumentException("Nothing wrong"), "Nothing wrong", "Nothing wrong");
        }).AsAsyncOperation();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
