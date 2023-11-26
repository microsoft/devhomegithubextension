// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Security;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace GitHubExtension.DeveloperId;
public interface IDeveloperIdProviderInternal : IDeveloperIdProvider
{
    public IAsyncOperation<IDeveloperId> LoginNewDeveloperIdAsync();

    public DeveloperId LoginNewDeveloperIdWithPAT(Uri hostAddress, SecureString personalAccessToken);

    public IEnumerable<DeveloperId> GetLoggedInDeveloperIdsInternal();
}
