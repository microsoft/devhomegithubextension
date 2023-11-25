// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.Windows.DevHome.SDK;
using Octokit;

namespace GitHubExtension.DeveloperId;

public class DeveloperId : IDeveloperId
{
    public string LoginId { get; private set; }

    public string DisplayName { get; private set; }

    public string Email { get; private set; }

    public string Url { get; private set; }

    public DateTime CredentialExpiryTime { get; set; }

    public GitHubClient GitHubClient { get; private set; }

    public DeveloperId()
    {
        LoginId = string.Empty;
        DisplayName = string.Empty;
        Email = string.Empty;
        Url = string.Empty;
        GitHubClient = new (new ProductHeaderValue(Constants.DEV_HOME_APPLICATION_NAME));
    }

    public DeveloperId(string loginId, string displayName, string email, string url, GitHubClient gitHubClient)
    {
        LoginId = loginId;
        DisplayName = displayName;
        Email = email;
        Url = url;
        GitHubClient = gitHubClient;
    }

    ~DeveloperId()
    {
        LoginId = string.Empty;
        DisplayName = string.Empty;
        Email = string.Empty;
        Url = string.Empty;
        return;
    }

    // IDeveloperIdInternal interface.
    public Windows.Security.Credentials.PasswordCredential GetCredential(bool refreshIfExpired = false)
    {
        if (refreshIfExpired && (CredentialExpiryTime < DateTime.Now))
        {
            return RefreshDeveloperId();
        }

        var credential = CredentialVault.GetInstance().GetCredentials(Url) ?? throw new InvalidOperationException("Invalid credential present for valid DeveloperId");
        return credential;
    }

    public Windows.Security.Credentials.PasswordCredential RefreshDeveloperId()
    {
        // Setting to MaxValue, since GitHub doesn't forcibly expire tokens currently.
        CredentialExpiryTime = DateTime.MaxValue;
        DeveloperIdProvider.GetInstance().RefreshDeveloperId(this);
        var credential = CredentialVault.GetInstance().GetCredentials(Url) ?? throw new InvalidOperationException("Invalid credential present for valid DeveloperId");
        GitHubClient.Credentials = new (credential.Password);
        return credential;
    }

    public Uri GetHostAddress()
    {
        return GitHubClient.BaseAddress;
    }
}
