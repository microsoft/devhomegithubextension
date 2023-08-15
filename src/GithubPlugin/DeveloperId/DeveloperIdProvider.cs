// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.UI;
using Microsoft.Windows.DevHome.SDK;
using Octokit;
using Windows.Foundation;

namespace GitHubPlugin.DeveloperId;

public class DeveloperIdProvider : IDeveloperIdProvider
{
    // Locks to control access to Singleton class members.
    private static readonly object DeveloperIdsLock = new ();

    private static readonly object OAuthRequestsLock = new ();

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

    public AuthenticationState DeveloperIdState
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    // DeveloperIdProvider uses singleton pattern.
    private static DeveloperIdProvider? singletonDeveloperIdProvider;

    public event EventHandler<IDeveloperId>? LoggedIn;

    public event EventHandler<IDeveloperId>? LoggedOut;

    public event EventHandler<IDeveloperId>? Updated;

    private readonly AuthenticationExperienceKind authenticationExperienceForGithubPlugin = AuthenticationExperienceKind.CardSession;

    // Private constructor for Singleton class.
    private DeveloperIdProvider()
    {
        Log.Logger()?.ReportInfo($"Creating DeveloperIdProvider singleton instance");

        lock (OAuthRequestsLock)
        {
            OAuthRequests ??= new List<OAuthRequest>();
        }

        lock (DeveloperIdsLock)
        {
            DeveloperIds ??= new List<DeveloperId>();

            // Retrieve and populate Logged in DeveloperIds from previous launch.
            RestoreDeveloperIds(CredentialVault.GetAllSavedLoginIds());
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

    // IDeveloperIdProvider interface functions.
    public string GetName()
    {
        return "GitHub";
    }

    public IEnumerable<IDeveloperId> GetLoggedInDeveloperIds()
    {
        List<IDeveloperId> iDeveloperIds = new ();
        lock (DeveloperIdsLock)
        {
            iDeveloperIds.AddRange(DeveloperIds);
        }

        return iDeveloperIds;
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

            var devId = CreateOrUpdateDeveloperId(oauthRequest);
            oauthRequest.Dispose();

            Log.Logger()?.ReportInfo($"New DeveloperId logged in");

            return devId as IDeveloperId;
        }).AsAsyncOperation();
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

    public void LogoutDeveloperId(IDeveloperId developerId)
    {
        DeveloperId? developerIdToLogout;
        lock (DeveloperIdsLock)
        {
            developerIdToLogout = DeveloperIds?.Find(e => e.LoginId == developerId.LoginId());
            if (developerIdToLogout == null)
            {
                Log.Logger()?.ReportError($"Unable to find DeveloperId to logout");
                throw new ArgumentNullException(nameof(developerId));
            }

            CredentialVault.RemoveAccessTokenFromVault(developerIdToLogout.LoginId);
            DeveloperIds?.Remove(developerIdToLogout);
        }

        try
        {
            LoggedOut?.Invoke(this as IDeveloperIdProvider, developerIdToLogout as IDeveloperId);
        }
        catch (Exception error)
        {
            Log.Logger()?.ReportError($"LoggedOut event signalling failed: {error}");
        }
    }

    // IAuthenticationProviderInternal interface functions.
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
        var devIdInternal = devIds.Where(i => i.LoginId.Equals(devId.LoginId(), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

        return devIdInternal ?? throw new ArgumentException(devId.LoginId());
    }

    // Internal Functions.
    private DeveloperId CreateOrUpdateDeveloperId(OAuthRequest oauthRequest)
    {
        // Query necessary data and populate Developer Id.
        var newDeveloperId = oauthRequest.RetrieveDeveloperId();
        var duplicateDeveloperIds = DeveloperIds.Where(d => d.Url.Equals(newDeveloperId.Url, StringComparison.OrdinalIgnoreCase));

        if (duplicateDeveloperIds.Any())
        {
            Log.Logger()?.ReportInfo($"DeveloperID already exists! Updating accessToken");
            try
            {
                // Save the credential to Credential Vault.
                CredentialVault.SaveAccessTokenToVault(duplicateDeveloperIds.Single().LoginId, oauthRequest.AccessToken);

                try
                {
                    Updated?.Invoke(this as IDeveloperIdProvider, duplicateDeveloperIds.Single() as IDeveloperId);
                }
                catch (Exception error)
                {
                    Log.Logger()?.ReportError($"Updated event signalling failed: {error}");
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

            CredentialVault.SaveAccessTokenToVault(newDeveloperId.LoginId, oauthRequest.AccessToken);

            try
            {
                LoggedIn?.Invoke(this as IDeveloperIdProvider, newDeveloperId as IDeveloperId);
            }
            catch (Exception error)
            {
                Log.Logger()?.ReportError($"LoggedIn event signalling failed: {error}");
            }
        }

        return newDeveloperId;
    }

    private void RestoreDeveloperIds(IEnumerable<string> loginIds)
    {
        foreach (var loginId in loginIds)
        {
            var gitHubClient = new GitHubClient(new ProductHeaderValue(Constants.DEV_HOME_APPLICATION_NAME))
            {
                Credentials = new Credentials(CredentialVault.GetCredentialFromLocker(loginId).Password),
            };
            var user = gitHubClient.User.Current().Result;

            DeveloperId developerId = new (user.Login, user.Email, user.Url, user.Name, gitHubClient);

            lock (DeveloperIdsLock)
            {
                DeveloperIds.Add(developerId);
            }

            Log.Logger()?.ReportInfo($"Restored DeveloperId");
        }

        return;
    }

    internal void RefreshDeveloperId(IDeveloperId developerIdInternal)
    {
        Updated?.Invoke(this as IDeveloperIdProvider, developerIdInternal as IDeveloperId);
    }

    public IPluginAdaptiveCardController GetAdaptiveCardController(string[] args)
    {
        var loginEntryPoint = string.Empty;
        if (args is not null && args.Length != 0)
        {
            loginEntryPoint = args[0];
        }

        Log.Logger()?.ReportInfo($"GetAdaptiveCardController");
        return new LoginUIController(loginEntryPoint);
    }

    public AuthenticationExperienceKind GetAuthenticationExperienceKind()
    {
        return authenticationExperienceForGithubPlugin;
    }

    public event TypedEventHandler<IDeveloperIdProvider, object>? Changed
    {
        add { } remove { }
    }

    public IAsyncOperation<IDeveloperId> ShowLogonSession(WindowId windowHandle) => throw new NotImplementedException();
}
