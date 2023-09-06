// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Logging.Helpers;
using GitHubExtension.DeveloperId;
using Microsoft.Windows.DevHome.SDK;
using Octokit;

namespace GitHubExtension.Client;

public class GitHubClientProvider
{
    private readonly GitHubClient publicRepoClient;

    private static readonly object InstanceLock = new ();

    private static GitHubClientProvider? _instance;

    public static GitHubClientProvider Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (InstanceLock)
                {
                    _instance = new GitHubClientProvider();
                }
            }

            return _instance;
        }
    }

    public GitHubClientProvider()
    {
        publicRepoClient = new GitHubClient(new ProductHeaderValue(Constants.DEV_HOME_APPLICATION_NAME));
    }

    public GitHubClient? GetClient(IDeveloperId devId)
    {
        var devIdInternal = DeveloperIdProvider.GetInstance().GetDeveloperIdInternal(devId) ?? throw new ArgumentException(devId.LoginId);
        return devIdInternal.GitHubClient;
    }

    public GitHubClient GetClient(string url)
    {
        var devIdInternal = DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIdsInternal().Where(i => i.Url.Equals(url, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        if (devIdInternal == null)
        {
            return publicRepoClient;
        }

        return devIdInternal.GitHubClient;
    }

    public GitHubClient GetClient()
    {
        return publicRepoClient;
    }

    public async Task<GitHubClient> GetClientForLoggedInDeveloper(bool logRateLimit = false)
    {
        var authProvider = DeveloperIdProvider.GetInstance();
        var devIds = authProvider.GetLoggedInDeveloperIdsInternal();
        GitHubClient client;
        if (devIds == null || !devIds.Any())
        {
            Log.Logger()?.ReportInfo($"No logged in developer, using public GitHub client.");
            client = Instance.GetClient();
        }
        else
        {
            Log.Logger()?.ReportInfo($"Using authenticated user: {devIds.First().LoginId}");
            client = devIds.First().GitHubClient;
        }

        if (client == null)
        {
            Log.Logger()?.ReportError($"Failed creating GitHubClient.");
            return client!;
        }

        if (logRateLimit)
        {
            var miscRateLimit = await client.RateLimit.GetRateLimits();
            Log.Logger()?.ReportInfo($"Rate Limit:  Remaining: {miscRateLimit.Resources.Core.Remaining}  Total: {miscRateLimit.Resources.Core.Limit}  Resets: {miscRateLimit.Resources.Core.Reset.ToStringInvariant()}");
        }

        return client;
    }
}
