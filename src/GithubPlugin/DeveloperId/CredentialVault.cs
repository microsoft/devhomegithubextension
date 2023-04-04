// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Net;
using System.Security;
using Windows.Security.Credentials;

namespace GitHubPlugin.DeveloperId;
internal static class CredentialVault
{
    private static class CredentialVaultConfiguration
    {
        public const string CredResourceName = "GitHubDevHomeExtension";
    }

    internal static void SaveAccessTokenToVault(string loginId, SecureString? accessToken)
    {
        // TODO: Encryption can be added here
        var vault = new PasswordVault();
        vault.Add(new PasswordCredential(CredentialVaultConfiguration.CredResourceName, loginId, new NetworkCredential(string.Empty, accessToken).Password));
    }

    internal static void RemoveAccessTokenFromVault(string loginId)
    {
        var vault = new PasswordVault();
        vault.Remove(vault.Retrieve(CredentialVaultConfiguration.CredResourceName, loginId));
        Log.Logger()?.ReportInfo($"Removing DeveloperId credentials: {loginId}");
    }

    internal static PasswordCredential GetCredentialFromLocker(string loginId)
    {
        // Decryption can be added here
        PasswordCredential credential;
        var vault = new PasswordVault();

        credential = vault.Retrieve(CredentialVaultConfiguration.CredResourceName, loginId);
        if (credential is null)
        {
            Log.Logger()?.ReportInfo("No credentials found for this DeveloperId: " + loginId);
            throw new ArgumentOutOfRangeException(loginId);
        }

        return credential;
    }

    public static IEnumerable<string> GetAllSavedLoginIds()
    {
        var vault = new PasswordVault();
        IReadOnlyList<PasswordCredential> credentialList;
        try
        {
            credentialList = vault.FindAllByResource(CredentialVaultConfiguration.CredResourceName);
        }
        catch (Exception ex)
        {
            // NotFound is expected and can be ignored
            if (ex.HResult != -2147023728)
            {
                throw new InvalidOperationException();
            }

            return Enumerable.Empty<string>();
        }

        if (credentialList.Count is 0)
        {
            return Enumerable.Empty<string>();
        }

        return credentialList.Select(credential => credential.UserName).ToList();
    }
}
