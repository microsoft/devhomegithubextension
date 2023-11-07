// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Specialized;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Web;
using GitHubExtension.Helpers;
using Octokit;

namespace GitHubExtension.DeveloperId;
internal class OAuthRequest : IDisposable
{
    internal string State { get; private set; }

    internal SecureString? AccessToken { get; private set; }

    internal DateTime StartTime
    {
        get; private set;
    }

    internal OAuthRequest()
    {
        gitHubClient = new (new ProductHeaderValue(Constants.DEV_HOME_APPLICATION_NAME));
        oAuthCompleted = new (0);
        State = string.Empty;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            oAuthCompleted.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void AwaitCompletion()
    {
        oAuthCompleted?.Wait();
    }

    private Uri CreateOauthRequestUri()
    {
        State = GetRandomNumber();

        var request = new OauthLoginRequest(OauthConfiguration.GetClientId())
        {
            Scopes = { "user", "notifications", "repo", "read:org" },
            State = State,
            RedirectUri = new Uri(OauthConfiguration.RedirectUri),
        };

        return gitHubClient.Oauth.GetGitHubLoginUrl(request);
    }

    internal void BeginOAuthRequest()
    {
        var options = new Windows.System.LauncherOptions();
        var uri = CreateOauthRequestUri();
        var browserLaunch = false;
        StartTime = DateTime.Now;

        Task.Run(async () =>
        {
            // Launch GitHub login page on Browser.
            browserLaunch = await Windows.System.Launcher.LaunchUriAsync(uri, options);

            if (browserLaunch)
            {
                Log.Logger()?.ReportInfo($"Uri Launched - Check browser");
            }
            else
            {
                Log.Logger()?.ReportError($"Uri Launch failed");
            }
        });
    }

    internal async Task CompleteOAuthAsync(Uri authorizationResponse)
    {
        // Gets URI from navigation parameters.
        var queryString = authorizationResponse.Query;

        // Parse the query string variables into a NameValueCollection.
        var queryStringCollection = HttpUtility.ParseQueryString(queryString);

        if (!string.IsNullOrEmpty(queryStringCollection.Get("error")))
        {
            Log.Logger()?.ReportError($"OAuth authorization error: {queryStringCollection.Get("error")}");
            throw new UriFormatException();
        }

        if (string.IsNullOrEmpty(queryStringCollection.Get("code")))
        {
            Log.Logger()?.ReportError($"Malformed authorization response: {queryString}");
            throw new UriFormatException();
        }

        // Gets the Authorization code
        var code = queryStringCollection.Get("code");

        try
        {
            var request = new OauthTokenRequest(OauthConfiguration.GetClientId(), OauthConfiguration.GetClientSecret(), code);
            var token = await gitHubClient.Oauth.CreateAccessToken(request);
            AccessToken = new NetworkCredential(string.Empty, token.AccessToken).SecurePassword;
            gitHubClient.Credentials = new Credentials(token.AccessToken);
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError($"Authorization code exchange failed: {ex}");
            throw;
        }

        Log.Logger()?.ReportInfo($"Authorization code exchange completed");
        oAuthCompleted.Release();
    }

    internal DeveloperId RetrieveDeveloperId()
    {
        if (AccessToken is null)
        {
            Log.Logger()?.ReportError($"RetrieveDeveloperIdData called before AccessToken is set");
            throw new InvalidOperationException("RetrieveDeveloperIdData called before AccessToken is set");
        }

        var newUser = gitHubClient.User.Current().Result;
        DeveloperId developerId = new (newUser.Login, newUser.Name, newUser.Email, newUser.Url, gitHubClient);

        return developerId;
    }

    internal static string RetrieveState(Uri authorizationResponse)
    {
        // Gets URI from navigation parameters.
        var queryString = authorizationResponse.Query;

        // Parse the query string variables into a NameValueCollection.
        var queryStringCollection = HttpUtility.ParseQueryString(queryString);

        var state = queryStringCollection.Get("state");

        if (string.IsNullOrEmpty(state))
        {
            Log.Logger()?.ReportError($"Authorization code exchange failed: ResponseString:{queryString}");
            throw new UriFormatException();
        }

        return state;
    }

    private static string GetRandomNumber()
    {
        var randomNumber = RandomNumberGenerator.GetInt32(int.MaxValue);
        return randomNumber.ToStringInvariant();
    }

    private readonly SemaphoreSlim oAuthCompleted;
    private readonly GitHubClient gitHubClient;
}
