// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubPlugin.Client;
using GitHubPlugin.DeveloperId;
using Microsoft.Windows.DevHome.SDK;
using Octokit;
using Windows.Foundation;

namespace GitHubPlugin.Providers;

public class RepositoryProvider : IRepositoryProvider
{
    public RepositoryProvider()
    {
    }

    public string DisplayName => "GitHub";

    public IAsyncOperation<IRepository?> ParseRepositoryFromUrlAsync(Uri uri)
    {
        return Task.Run(() =>
        {
            try
            {
                if (Validation.IsValidGitHubURL(uri.OriginalString))
                {
                    var client = GitHubClientProvider.Instance.GetClient();
                    var ocktoKitRepo = client.Repository.Get(Validation.ParseOwnerFromGitHubURL(uri), Validation.ParseRepositoryFromGitHubURL(uri)).Result;
                    return new DevHomeRepository(ocktoKitRepo) as IRepository;
                }
            }
            catch (Exception)
            {
                Log.Logger()?.ReportDebug("Github extension could not parse the url: " + uri.OriginalString);
                return null;
            }

            return null;
        }).AsAsyncOperation();
    }

    public IAsyncOperation<IEnumerable<IRepository>> GetRepositoriesAsync(IDeveloperId developerId)
    {
        return Task.Run(async () =>
        {
            // This is fetching all repositories available to the specified DevId
            // We are not using the datastore cache for this query, it will always go to GitHub directly.
            var repositoryList = new List<IRepository>();
            try
            {
                ApiOptions apiOptions = new ();
                apiOptions.PageSize = 50;
                apiOptions.PageCount = 1;

                // Authenticate as the specified developer Id
                var client = DeveloperIdProvider.GetInstance().GetDeveloperIdInternal(developerId).GitHubClient;
                RepositoryRequest request = new RepositoryRequest();
                request.Sort = RepositorySort.Updated;
                request.Direction = SortDirection.Descending;
                request.Affiliation = RepositoryAffiliation.Owner;

                // Gets only public repos for the owned repos.
                request.Visibility = RepositoryRequestVisibility.Public;
                var getPublicReposTask = client.Repository.GetAllForUser(developerId.LoginId(), apiOptions);

                // this is getting private org and user repos
                request.Visibility = RepositoryRequestVisibility.Private;
                var getPrivateReposTask = client.Repository.GetAllForCurrent(request, apiOptions);

                // This gets all org repos
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
}
