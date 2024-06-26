﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Web;
using GitHubExtension.Helpers;
using Octokit;
using Serilog;

namespace GitHubExtension.DeveloperId;

internal sealed class OAuthRequest : IDisposable
{
    private static readonly Lazy<ILogger> _logger = new(() => Serilog.Log.ForContext("SourceContext", nameof(OAuthRequest)));

    private static readonly ILogger _log = _logger.Value;

    internal string State { get; private set; }

    internal SecureString? AccessToken { get; private set; }

    internal DateTime StartTime
    {
        get; private set;
    }

    internal OAuthRequest()
    {
        _gitHubClient = new(new ProductHeaderValue(Constants.DEV_HOME_APPLICATION_NAME));
        _oAuthCompleted = new(0);
        State = string.Empty;
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _oAuthCompleted.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void AwaitCompletion()
    {
        _oAuthCompleted?.Wait();
    }

    private Uri CreateOauthRequestUri()
    {
        State = GetRandomNumber();

        var request = new OauthLoginRequest(OauthConfiguration.GetClientId())
        {
            Scopes = { "read:user", "notifications", "repo", "read:org" },
            State = State,
            RedirectUri = new Uri(OauthConfiguration.RedirectUri),
        };

        return _gitHubClient.Oauth.GetGitHubLoginUrl(request);
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
                _log.Information($"Uri Launched - Check browser");
            }
            else
            {
                _log.Error($"Uri Launch failed");
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
            _log.Error($"OAuth authorization error: {queryStringCollection.Get("error")}");
            throw new UriFormatException();
        }

        if (string.IsNullOrEmpty(queryStringCollection.Get("code")))
        {
            _log.Error($"Malformed authorization response: {queryString}");
            throw new UriFormatException();
        }

        // Gets the Authorization code
        var code = queryStringCollection.Get("code");

        try
        {
            var request = new OauthTokenRequest(OauthConfiguration.GetClientId(), OauthConfiguration.GetClientSecret(), code);
            var token = await _gitHubClient.Oauth.CreateAccessToken(request);
            AccessToken = new NetworkCredential(string.Empty, token.AccessToken).SecurePassword;
            _gitHubClient.Credentials = new Credentials(token.AccessToken);
        }
        catch (Exception ex)
        {
            _log.Error($"Authorization code exchange failed: {ex}");
            throw;
        }

        _log.Information($"Authorization code exchange completed");
        _oAuthCompleted.Release();
    }

    internal DeveloperId RetrieveDeveloperId()
    {
        if (AccessToken is null)
        {
            _log.Error($"RetrieveDeveloperIdData called before AccessToken is set");
            throw new InvalidOperationException("RetrieveDeveloperIdData called before AccessToken is set");
        }

        var newUser = _gitHubClient.User.Current().Result;
        DeveloperId developerId = new(newUser.Login, newUser.Name, newUser.Email, newUser.Url, _gitHubClient);

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
            _log.Error($"Authorization code exchange failed: ResponseString:{queryString}");
            throw new UriFormatException();
        }

        return state;
    }

    private static string GetRandomNumber()
    {
        var randomNumber = RandomNumberGenerator.GetInt32(int.MaxValue);
        return randomNumber.ToStringInvariant();
    }

    private readonly SemaphoreSlim _oAuthCompleted;
    private readonly GitHubClient _gitHubClient;
}
