// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Net;
using System.Security;
using Microsoft.UI;
using Microsoft.Windows.DevHome.SDK;
using Octokit;
using Windows.Foundation;
using Windows.Security.Credentials;

namespace GitHubExtension.DeveloperId;

public class DeveloperIdProvider : IDeveloperIdProviderInternal
{
    // Locks to control access to Singleton class members.
    private static readonly object DeveloperIdsLock = new ();

    private static readonly object OAuthRequestsLock = new ();

    private static readonly object CredentialVaultLock = new ();

    private static readonly object AuthenticationProviderLock = new ();

    // DeveloperId list containing all Logged in Ids.
    private List<DeveloperId> DeveloperIds
    {
        get; set;
    }

    // List of currently active Oauth Request sessions.
    private List<OAuthRequest> OAuthRequests
    {
        get; set;
    }

    private readonly CredentialVault credentialVault;

    // DeveloperIdProvider uses singleton pattern.
    private static DeveloperIdProvider? singletonDeveloperIdProvider;

    public event TypedEventHandler<IDeveloperIdProvider, IDeveloperId>? Changed;

    private readonly AuthenticationExperienceKind authenticationExperienceForGitHubExtension = AuthenticationExperienceKind.CardSession;

    public string DisplayName => "GitHub";

    // Private constructor for Singleton class.
    private DeveloperIdProvider()
    {
        Log.Logger()?.ReportInfo($"Creating DeveloperIdProvider singleton instance");

        lock (CredentialVaultLock)
        {
            credentialVault ??= new CredentialVault();
        }

        lock (OAuthRequestsLock)
        {
            OAuthRequests ??= new List<OAuthRequest>();
        }

        lock (DeveloperIdsLock)
        {
            DeveloperIds ??= new List<DeveloperId>();
            try
            {
                // Retrieve and populate Logged in DeveloperIds from previous launch.
                RestoreDeveloperIds(credentialVault.GetAllCredentials());
            }
            catch (Exception error)
            {
                Log.Logger()?.ReportError($"Error while restoring DeveloperIds: {error.Message}. Proceeding without restoring.");
            }
        }
    }

    public static DeveloperIdProvider GetInstance()
    {
        lock (AuthenticationProviderLock)
        {
            singletonDeveloperIdProvider ??= new DeveloperIdProvider();
        }

        return singletonDeveloperIdProvider;
    }

    public DeveloperIdsResult GetLoggedInDeveloperIds()
    {
        List<IDeveloperId> iDeveloperIds = new ();
        lock (DeveloperIdsLock)
        {
            iDeveloperIds.AddRange(DeveloperIds);
        }

        var developerIdsResult = new DeveloperIdsResult(iDeveloperIds);

        return developerIdsResult;
    }

    public IAsyncOperation<IDeveloperId> LoginNewDeveloperIdAsync()
    {
        return Task.Run(() =>
        {
            var oauthRequest = LoginNewDeveloperId();
            if (oauthRequest is null)
            {
                Log.Logger()?.ReportError($"Invalid OAuthRequest");
                throw new InvalidOperationException();
            }

            oauthRequest.AwaitCompletion();

            var devId = CreateOrUpdateDeveloperIdFromOauthRequest(oauthRequest);
            oauthRequest.Dispose();

            Log.Logger()?.ReportInfo($"New DeveloperId logged in");

            return devId as IDeveloperId;
        }).AsAsyncOperation();
    }

    public DeveloperId LoginNewDeveloperIdWithPAT(Uri hostAddress, SecureString personalAccessToken)
    {
        try
        {
            GitHubClient gitHubClient = new (new ProductHeaderValue(Constants.DEV_HOME_APPLICATION_NAME), hostAddress);
            var credentials = new Credentials(new System.Net.NetworkCredential(string.Empty, personalAccessToken).Password);
            gitHubClient.Credentials = credentials;
            var newUser = gitHubClient.User.Current().Result;
            DeveloperId developerId = new (newUser.Login, newUser.Name, newUser.Email, newUser.Url, gitHubClient);
            SaveOrOverwriteDeveloperId(developerId, personalAccessToken);

            Log.Logger()?.ReportInfo($"{developerId.LoginId} logged in with PAT flow to {developerId.GetHostAddress()}");

            return developerId;
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError($"Error while logging in with PAT to {hostAddress.AbsoluteUri} : {ex.Message}");
            throw;
        }
    }

    private OAuthRequest? LoginNewDeveloperId()
    {
        OAuthRequest oauthRequest = new ();

        lock (OAuthRequestsLock)
        {
            OAuthRequests.Add(oauthRequest);
            try
            {
                oauthRequest.BeginOAuthRequest();
                return oauthRequest;
            }
            catch (Exception error)
            {
                OAuthRequests.Remove(oauthRequest);
                Log.Logger()?.ReportError($"Unable to complete OAuth request: {error.Message}");
            }
        }

        return null;
    }

    public ProviderOperationResult LogoutDeveloperId(IDeveloperId developerId)
    {
        DeveloperId? developerIdToLogout;
        lock (DeveloperIdsLock)
        {
            developerIdToLogout = DeveloperIds?.Find(e => e.LoginId == developerId.LoginId);
            if (developerIdToLogout == null)
            {
                Log.Logger()?.ReportError($"Unable to find DeveloperId to logout");
                return new ProviderOperationResult(ProviderOperationStatus.Failure, new ArgumentNullException(nameof(developerId)), "The developer account to log out does not exist", "Unable to find DeveloperId to logout");
            }

            credentialVault.RemoveCredentials(developerIdToLogout.Url);
            DeveloperIds?.Remove(developerIdToLogout);
        }

        try
        {
            Changed?.Invoke(this as IDeveloperIdProvider, developerIdToLogout as IDeveloperId);
        }
        catch (Exception error)
        {
            Log.Logger()?.ReportError($"LoggedOut event signaling failed: {error}");
        }

        return new ProviderOperationResult(ProviderOperationStatus.Success, null, "The developer account has been logged out successfully", "LogoutDeveloperId succeeded");
    }

    public void HandleOauthRedirection(Uri authorizationResponse)
    {
        OAuthRequest? oAuthRequest = null;

        lock (OAuthRequestsLock)
        {
            if (OAuthRequests is null)
            {
                throw new InvalidOperationException();
            }

            if (OAuthRequests.Count is 0)
            {
                Log.Logger()?.ReportWarn($"No saved OAuth requests to match OAuth response");
                throw new InvalidOperationException();
            }

            var state = OAuthRequest.RetrieveState(authorizationResponse);

            oAuthRequest = OAuthRequests.Find(r => r.State == state);

            if (oAuthRequest == null)
            {
                Log.Logger()?.ReportWarn($"Unable to find valid request for received OAuth response");
                return;
            }
            else
            {
                OAuthRequests.Remove(oAuthRequest);
            }
        }

        oAuthRequest.CompleteOAuthAsync(authorizationResponse).Wait();
    }

    public IEnumerable<DeveloperId> GetLoggedInDeveloperIdsInternal()
    {
        List<DeveloperId> iDeveloperIds = new ();
        lock (DeveloperIdsLock)
        {
            iDeveloperIds.AddRange(DeveloperIds);
        }

        return iDeveloperIds;
    }

    // Convert devID to internal devID.
    public DeveloperId GetDeveloperIdInternal(IDeveloperId devId)
    {
        var devIds = GetInstance().GetLoggedInDeveloperIdsInternal();
        var devIdInternal = devIds.Where(i => i.LoginId.Equals(devId.LoginId, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

        return devIdInternal ?? throw new ArgumentException(devId.LoginId);
    }

    // Internal Functions.
    private void SaveOrOverwriteDeveloperId(DeveloperId newDeveloperId, SecureString accessToken)
    {
        var duplicateDeveloperIds = DeveloperIds.Where(d => d.Url.Equals(newDeveloperId.Url, StringComparison.OrdinalIgnoreCase));

        if (duplicateDeveloperIds.Any())
        {
            Log.Logger()?.ReportInfo($"DeveloperID already exists! Updating accessToken");
            try
            {
                // Save the credential to Credential Vault.
                credentialVault.SaveCredentials(duplicateDeveloperIds.Single().Url, accessToken);

                try
                {
                    Changed?.Invoke(this as IDeveloperIdProvider, duplicateDeveloperIds.Single() as IDeveloperId);
                }
                catch (Exception error)
                {
                    Log.Logger()?.ReportError($"Updated event signaling failed: {error}");
                }
            }
            catch (InvalidOperationException)
            {
                Log.Logger()?.ReportWarn($"Multiple copies of same DeveloperID already exists");
                throw new InvalidOperationException("Multiple copies of same DeveloperID already exists");
            }
        }
        else
        {
            lock (DeveloperIdsLock)
            {
                DeveloperIds.Add(newDeveloperId);
            }

            credentialVault.SaveCredentials(newDeveloperId.Url, accessToken);

            try
            {
                Changed?.Invoke(this as IDeveloperIdProvider, newDeveloperId as IDeveloperId);
            }
            catch (Exception error)
            {
                Log.Logger()?.ReportError($"LoggedIn event signaling failed: {error}");
            }
        }
    }

    private DeveloperId CreateOrUpdateDeveloperIdFromOauthRequest(OAuthRequest oauthRequest)
    {
        // Query necessary data and populate Developer Id.
        var newDeveloperId = oauthRequest.RetrieveDeveloperId();
        var accessToken = oauthRequest.AccessToken;
        if (accessToken is null)
        {
            Log.Logger()?.ReportError($"Invalid AccessToken");
            throw new InvalidOperationException();
        }

        SaveOrOverwriteDeveloperId(newDeveloperId, accessToken);

        Log.Logger()?.ReportInfo($"{newDeveloperId.LoginId} logged in with OAuth flow to {newDeveloperId.GetHostAddress()}");

        return newDeveloperId;
    }

    private void RestoreDeveloperIds(IEnumerable<string> loginIdsAndUrls)
    {
        foreach (var loginIdOrUrl in loginIdsAndUrls)
        {
            var isUrl = loginIdOrUrl.Contains('/');

            // For loginIds without URL, use GitHub.com as default.
            var hostAddress = isUrl ? new Uri(loginIdOrUrl) : new Uri(Constants.GITHUB_COM_URL);

            GitHubClient gitHubClient = new (new ProductHeaderValue(Constants.DEV_HOME_APPLICATION_NAME), hostAddress)
            {
                Credentials = new (credentialVault.GetCredentials(loginIdOrUrl)?.Password),
            };

            var user = gitHubClient.User.Current().Result;

            DeveloperId developerId = new (user.Login, user.Name, user.Email, user.Url, gitHubClient);

            lock (DeveloperIdsLock)
            {
                DeveloperIds.Add(developerId);
            }

            Log.Logger()?.ReportInfo($"Restored DeveloperId {user.Url}");

            // If loginId is currently used to save credential, remove it, and use URL instead.
            if (!isUrl)
            {
                try
                {
                    credentialVault.SaveCredentials(
                        user.Url,
                        new NetworkCredential(string.Empty, credentialVault.GetCredentials(loginIdOrUrl)?.Password).SecurePassword);
                    credentialVault.RemoveCredentials(loginIdOrUrl);
                    Log.Logger()?.ReportInfo($"Replaced {loginIdOrUrl} with {user.Url} in CredentialManager");
                }
                catch (Exception error)
                {
                    Log.Logger()?.ReportError($"Error while replacing {loginIdOrUrl} with {user.Url} in CredentialManager: {error.Message}");
                }
            }
        }

        return;
    }

    internal void RefreshDeveloperId(IDeveloperId developerIdInternal)
    {
        Changed?.Invoke(this as IDeveloperIdProvider, developerIdInternal as IDeveloperId);
    }

    public AuthenticationExperienceKind GetAuthenticationExperienceKind()
    {
        return authenticationExperienceForGitHubExtension;
    }

    public AdaptiveCardSessionResult GetLoginAdaptiveCardSession()
    {
        Log.Logger()?.ReportInfo($"GetAdaptiveCardController");
        return new AdaptiveCardSessionResult(new LoginUIController(this));
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public IAsyncOperation<DeveloperIdResult> ShowLogonSession(WindowId windowHandle) => throw new NotImplementedException();

    public AuthenticationState GetDeveloperIdState(IDeveloperId developerId)
    {
        DeveloperId? developerIdToFind;
        lock (DeveloperIdsLock)
        {
            developerIdToFind = DeveloperIds?.Find(e => e.LoginId == developerId.LoginId);
            if (developerIdToFind == null)
            {
                return AuthenticationState.LoggedOut;
            }
            else
            {
                return AuthenticationState.LoggedIn;
            }
        }
    }

    internal PasswordCredential? GetCredentials(IDeveloperId developerId) => credentialVault.GetCredentials(developerId.Url);
}
