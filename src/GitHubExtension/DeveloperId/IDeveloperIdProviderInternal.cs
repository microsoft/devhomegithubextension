// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace GitHubExtension.DeveloperId;

// This internal interface extends the public interface IDeveloperIdProvider
public interface IDeveloperIdProviderInternal : IDeveloperIdProvider
{
    // This method triggers login flow for a new developer id through the browser.
    // This is called when the user clicks on the "Sign in" button in LoginUI popup.
    public IAsyncOperation<IDeveloperId> LoginNewDeveloperIdAsync();

    // This method triggers login flow for a new developer id using the provided personal access token.
    // This will not open the browser. This is used when the user selects the "Sign in with GHES"
    // option in LoginUI popup.
    public DeveloperId LoginNewDeveloperIdWithPAT(Uri hostAddress, SecureString personalAccessToken);

    // This method returns the list of developer id objects that are currently logged in.
    public IEnumerable<DeveloperId> GetLoggedInDeveloperIdsInternal();
}
