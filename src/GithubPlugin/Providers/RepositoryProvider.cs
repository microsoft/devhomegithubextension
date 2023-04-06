// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubPlugin.Client;
using GitHubPlugin.DeveloperId;
using Microsoft.Windows.DevHome.SDK;
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
        return Task.Run(() =>
        {
            // This is fetching all repositories available to the specified DevId
            // We are not using the datastore cache for this query, it will always go to GitHub directly.
            var repositoryList = new List<IRepository>();
            try
            {
                // Authenticate as the specified developer Id
                var client = DeveloperIdProvider.GetInstance().GetDeveloperIdInternal(developerId).GitHubClient;
                var repositories = client.Repository.GetAllForCurrent().Result;
                foreach (var repository in repositories)
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
