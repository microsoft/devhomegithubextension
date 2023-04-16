// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.Windows.DevHome.SDK;
using Octokit;

namespace GitHubPlugin.DeveloperId;

public class DeveloperId : IDeveloperId
{
    // _
    // Members
    // _
    public string LoginId { get; private set; }

    public string DisplayName { get; private set; }

    public string Email { get; private set; }

    public string Url { get; private set; }

    public DateTime CredentialExpiryTime { get; set; }

    public GitHubClient GitHubClient { get; private set; }

    // _
    // Constructors, Destructors
    // _
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

        // TODO: Ensure token removed From Credential Manager here
        return;
    }

    // _
    // IDeveloperId interface functions
    // _
    string IDeveloperId.LoginId() => LoginId;

    string IDeveloperId.Url() => Url;

    // _
    // IDeveloperIdInternal interface
    // _
    public Windows.Security.Credentials.PasswordCredential GetCredential(bool refreshIfExpired = false)
    {
        if (refreshIfExpired && (CredentialExpiryTime < DateTime.Now))
        {
            return RefreshDeveloperId();
        }

        return CredentialVault.GetCredentialFromLocker(LoginId);
    }

    public Windows.Security.Credentials.PasswordCredential RefreshDeveloperId()
    {
        // Setting to MaxValue, since GitHub doesn't forcibly expire tokens currently
        CredentialExpiryTime = DateTime.MaxValue;

        // TODO: Use Refresh token to get new access code and populate Credential vault
        DeveloperIdProvider.GetInstance().RefreshDeveloperId(this);

        var credential = CredentialVault.GetCredentialFromLocker(LoginId);
        GitHubClient.Credentials = new (credential.Password);
        return credential;
    }
}
