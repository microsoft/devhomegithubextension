// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    private static readonly object _developerIdsLock = new();

    private static readonly object _oAuthRequestsLock = new();

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

    private readonly Lazy<CredentialVault> _credentialVault;

    public event TypedEventHandler<IDeveloperIdProvider, IDeveloperId>? Changed;

    private readonly AuthenticationExperienceKind authenticationExperienceForGitHubExtension = AuthenticationExperienceKind.CardSession;

    public string DisplayName => "GitHub";

    // DeveloperIdProvider uses singleton pattern.
    private static Lazy<DeveloperIdProvider> _singletonDeveloperIdProvider = new(() => new DeveloperIdProvider());

    public static DeveloperIdProvider GetInstance()
    {
        return _singletonDeveloperIdProvider.Value;
    }

    // Private constructor for Singleton class.
    private DeveloperIdProvider()
    {
        Log.Logger()?.ReportInfo($"Creating DeveloperIdProvider singleton instance");

        _credentialVault = new(() => new CredentialVault());

        lock (_oAuthRequestsLock)
        {
            OAuthRequests ??= new List<OAuthRequest>();
        }

        lock (_developerIdsLock)
        {
            DeveloperIds ??= new List<DeveloperId>();
        }

        try
        {
            // Retrieve and populate Logged in DeveloperIds from previous launch.
            RestoreDeveloperIds(_credentialVault.Value.GetAllCredentials());
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError($"Error while restoring DeveloperIds: {ex.Message}. Proceeding without restoring.", ex);
        }
    }

    public DeveloperIdsResult GetLoggedInDeveloperIds()
    {
        List<IDeveloperId> iDeveloperIds = new();
        lock (_developerIdsLock)
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
            GitHubClient gitHubClient = new(new ProductHeaderValue(Constants.DEV_HOME_APPLICATION_NAME), hostAddress);
            var credentials = new Credentials(new System.Net.NetworkCredential(string.Empty, personalAccessToken).Password);
            gitHubClient.Credentials = credentials;
            var newUser = gitHubClient.User.Current().Result;
            DeveloperId developerId = new(newUser.Login, newUser.Name, newUser.Email, newUser.Url, gitHubClient);
            SaveOrOverwriteDeveloperId(developerId, personalAccessToken);

            Log.Logger()?.ReportInfo($"{developerId.LoginId} logged in with PAT flow to {developerId.GetHostAddress()}");

            return developerId;
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError($"Error while logging in with PAT to {hostAddress.AbsoluteUri} : ", ex);
            throw;
        }
    }

    private OAuthRequest? LoginNewDeveloperId()
    {
        OAuthRequest oauthRequest = new();

        lock (_oAuthRequestsLock)
        {
            OAuthRequests.Add(oauthRequest);
            try
            {
                oauthRequest.BeginOAuthRequest();
                return oauthRequest;
            }
            catch (Exception ex)
            {
                OAuthRequests.Remove(oauthRequest);
                Log.Logger()?.ReportError($"Unable to complete OAuth request: ", ex);
            }
        }

        return null;
    }

    public ProviderOperationResult LogoutDeveloperId(IDeveloperId developerId)
    {
        DeveloperId? developerIdToLogout;
        lock (_developerIdsLock)
        {
            developerIdToLogout = DeveloperIds?.Find(e => e.LoginId == developerId.LoginId);
            if (developerIdToLogout == null)
            {
                Log.Logger()?.ReportError($"Unable to find DeveloperId to logout");
                return new ProviderOperationResult(ProviderOperationStatus.Failure, new ArgumentNullException(nameof(developerId)), "The developer account to log out does not exist", "Unable to find DeveloperId to logout");
            }

            _credentialVault.Value.RemoveCredentials(developerIdToLogout.Url);
            DeveloperIds?.Remove(developerIdToLogout);
        }

        try
        {
            Changed?.Invoke(this as IDeveloperIdProvider, developerIdToLogout as IDeveloperId);
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError($"LoggedOut event signaling failed: ", ex);
        }

        return new ProviderOperationResult(ProviderOperationStatus.Success, null, "The developer account has been logged out successfully", "LogoutDeveloperId succeeded");
    }

    public void HandleOauthRedirection(Uri authorizationResponse)
    {
        OAuthRequest? oAuthRequest = null;

        lock (_oAuthRequestsLock)
        {
            if (OAuthRequests is null)
            {
                throw new InvalidOperationException();
            }

            if (OAuthRequests.Count is 0)
            {
                // This could happen if the user refreshes the redirected browser window
                // causing the OAuth response to be received again.
                Log.Logger()?.ReportWarn($"No saved OAuth requests to match OAuth response");
                return;
            }

            var state = OAuthRequest.RetrieveState(authorizationResponse);

            oAuthRequest = OAuthRequests.Find(r => r.State == state);

            if (oAuthRequest == null)
            {
                // This could happen if the user refreshes a previously redirected browser window instead of using
                // the new browser window for the response. Log the warning and return.
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
        List<DeveloperId> iDeveloperIds = new();
        lock (_developerIdsLock)
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
                _credentialVault.Value.SaveCredentials(duplicateDeveloperIds.Single().Url, accessToken);

                try
                {
                    Changed?.Invoke(this as IDeveloperIdProvider, duplicateDeveloperIds.Single() as IDeveloperId);
                }
                catch (Exception ex)
                {
                    Log.Logger()?.ReportError($"Updated event signaling failed: ", ex);
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
            lock (_developerIdsLock)
            {
                DeveloperIds.Add(newDeveloperId);
            }

            _credentialVault.Value.SaveCredentials(newDeveloperId.Url, accessToken);

            try
            {
                Changed?.Invoke(this as IDeveloperIdProvider, newDeveloperId as IDeveloperId);
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError($"LoggedIn event signaling failed: ", ex);
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
        // We take loginIds or Urls here because in older versions of DevHome, we used loginIds to save credentials.
        // In newer versions, we use Urls to save credentials.
        // So, we need to check if loginId is currently used to save credential, and if so, replace it with URL.
        // This is a temporary fix, and we should replace this logic once we are sure that most users have updated to newer versions of DevHome.
        foreach (var loginIdOrUrl in loginIdsAndUrls)
        {
            // Since GitHub loginIds cannot contain /, and URLs would, this is sufficient to differentiate between
            // loginIds and URLs. We could alternatively use TryCreate, but there could be some GHES urls that we miss.
            var isUrl = loginIdOrUrl.Contains('/');

            // For loginIds without URL, use GitHub.com as default.
            var hostAddress = isUrl ? new Uri(loginIdOrUrl) : new Uri(Constants.GITHUB_COM_URL);

            GitHubClient gitHubClient = new(new ProductHeaderValue(Constants.DEV_HOME_APPLICATION_NAME), hostAddress)
            {
                Credentials = new(_credentialVault.Value.GetCredentials(loginIdOrUrl)?.Password),
            };

            try
            {
                var user = gitHubClient.User.Current().Result;
                DeveloperId developerId = new(user.Login, user.Name, user.Email, user.Url, gitHubClient);
                lock (_developerIdsLock)
                {
                    DeveloperIds.Add(developerId);
                }

                Log.Logger()?.ReportInfo($"Restored DeveloperId {user.Url}");

                // If loginId is currently used to save credential, remove it, and use URL instead.
                if (!isUrl)
                {
                    ReplaceSavedLoginIdWithUrl(developerId);
                }
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError($"Error while restoring DeveloperId {loginIdOrUrl} : ", ex);

                // If we are unable to restore a DeveloperId, remove it from CredentialManager to avoid
                // the same error next time, and to force the user to login again
                _credentialVault.Value.RemoveCredentials(loginIdOrUrl);
            }
        }

        return;
    }

    private void ReplaceSavedLoginIdWithUrl(DeveloperId developerId)
    {
        try
        {
            _credentialVault.Value.SaveCredentials(
                developerId.Url,
                new NetworkCredential(string.Empty, _credentialVault.Value.GetCredentials(developerId.LoginId)?.Password).SecurePassword);
            _credentialVault.Value.RemoveCredentials(developerId.LoginId);
            Log.Logger()?.ReportInfo($"Replaced {developerId.LoginId} with {developerId.Url} in CredentialManager");
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError($"Error while replacing {developerId.LoginId} with {developerId.Url} in CredentialManager: ", ex);
        }
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

    // This function is to be used for testing purposes only.
    public static void ResetInstanceForTests()
    {
        _singletonDeveloperIdProvider = new(() => new DeveloperIdProvider());
    }

    public IAsyncOperation<DeveloperIdResult> ShowLogonSession(WindowId windowHandle) => throw new NotImplementedException();

    public AuthenticationState GetDeveloperIdState(IDeveloperId developerId)
    {
        DeveloperId? developerIdToFind;
        lock (_developerIdsLock)
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

    internal PasswordCredential? GetCredentials(IDeveloperId developerId) => _credentialVault.Value.GetCredentials(developerId.Url);
}
