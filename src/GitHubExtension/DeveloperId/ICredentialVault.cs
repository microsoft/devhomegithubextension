﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Security;
using Windows.Security.Credentials;

namespace GitHubExtension.DeveloperId;

internal interface ICredentialVault
{
    PasswordCredential? GetCredentials(string loginId);

    void RemoveCredentials(string loginId);

    void SaveCredentials(string loginId, SecureString? accessToken);

    IEnumerable<string> GetAllCredentials();

    void RemoveAllCredentials();
}
