// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.DeveloperId;
using Microsoft.Windows.DevHome.SDK;
using Octokit;
using Serilog;

namespace GitHubExtension.Client;

public class GitHubClientProvider
{
    private static readonly Lazy<ILogger> _log = new(() => Serilog.Log.ForContext("SourceContext", nameof(GitHubClientProvider)));

    private static readonly ILogger Log = _log.Value;

    private readonly GitHubClient publicRepoClient;

    private static readonly object InstanceLock = new();

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
            Log.Information($"No logged in developer, using public GitHub client.");
            client = Instance.GetClient();
        }
        else
        {
            Log.Information($"Using authenticated user: {devIds.First().LoginId}");
            client = devIds.First().GitHubClient;
        }

        if (client == null)
        {
            Log.Error($"Failed creating GitHubClient.");
            return client!;
        }

        if (logRateLimit)
        {
            try
            {
                var miscRateLimit = await client.RateLimit.GetRateLimits();
                Log.Information($"Rate Limit:  Remaining: {miscRateLimit.Resources.Core.Remaining}  Total: {miscRateLimit.Resources.Core.Limit}  Resets: {miscRateLimit.Resources.Core.Reset}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Rate limiting not enabled for server.");
            }
        }

        return client;
    }
}
