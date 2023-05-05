// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using Octokit;
using Windows.Security.Credentials;
using static GitHubPlugin.DeveloperId.CredentialManager;

namespace GitHubPlugin.DeveloperId;
internal static class CredentialVault
{
    private static class CredentialVaultConfiguration
    {
        public const string CredResourceName = "GitHubDevHomeExtension";
    }

    internal static void SaveAccessTokenToVault(string loginId, SecureString? accessToken)
    {
        // Initialize a credential object
        CREDENTIAL credential = new CREDENTIAL
        {
            Type = CRED_TYPE.GENERIC,
            TargetName = CredentialVaultConfiguration.CredResourceName + ": " + loginId,
            UserName = loginId,
            Persist = (int)CRED_PERSIST.LocalMachine,
            AttributeCount = 0,
            Flags = 0,
            Comment = string.Empty,
        };

        try
        {
            if (accessToken != null)
            {
                credential.CredentialBlob = Marshal.SecureStringToCoTaskMemUnicode(accessToken);
                credential.CredentialBlobSize = accessToken.Length * 2;
            }
            else
            {
                Log.Logger()?.ReportInfo($"The access token is null for the loginId provided");
                throw new ArgumentNullException(nameof(accessToken));
            }

            // Store credential under Windows Credentials inside Credential Manager
            var isCredentialSaved = CredWrite(credential, 0);
            if (!isCredentialSaved)
            {
                Log.Logger()?.ReportInfo($"Writing credentials to Credential Manager has failed");
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        finally
        {
            if (credential.CredentialBlob != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(credential.CredentialBlob);
            }
        }
    }

    internal static void RemoveAccessTokenFromVault(string loginId)
    {
        var targetCredentialToDelete = CredentialVaultConfiguration.CredResourceName + ": " + loginId;
        var isCredentialDeleted = CredDelete(targetCredentialToDelete, CRED_TYPE.GENERIC, 0);
        if (!isCredentialDeleted)
        {
            Log.Logger()?.ReportInfo($"Deleting credentials from Credential Manager has failed");
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    internal static PasswordCredential GetCredentialFromLocker(string loginId)
    {
        var credentialNameToRetrieve = CredentialVaultConfiguration.CredResourceName + ": " + loginId;
        IntPtr ptrToCredential = IntPtr.Zero;

        try
        {
            var isCredentialRetrieved = CredRead(credentialNameToRetrieve, CRED_TYPE.GENERIC, 0, out ptrToCredential);
            if (!isCredentialRetrieved)
            {
                Log.Logger()?.ReportInfo($"Retrieving credentials from Credential Manager has failed");
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            CREDENTIAL credentialObject;
            if (ptrToCredential != IntPtr.Zero)
            {
#pragma warning disable CS8605 // Unboxing a possibly null value.
                credentialObject = (CREDENTIAL)Marshal.PtrToStructure(ptrToCredential, typeof(CREDENTIAL));
#pragma warning restore CS8605 // Unboxing a possibly null value.

            }
            else
            {
                Log.Logger()?.ReportInfo("No credentials found for this DeveloperId");
                throw new ArgumentOutOfRangeException(loginId);
            }

            var accessTokenInChars = new char[credentialObject.CredentialBlobSize / 2];
            Marshal.Copy(credentialObject.CredentialBlob, accessTokenInChars, 0, accessTokenInChars.Length);

            SecureString accessToken = new SecureString();
            for (var i = 0; i < accessTokenInChars.Length; i++)
            {
                accessToken.AppendChar(accessTokenInChars[i]);

                // Zero out characters after they are copied over from an unmanaged to managed type
                accessTokenInChars[i] = '\0';
            }

            accessToken.MakeReadOnly();

            PasswordCredential credential = new PasswordCredential(CredentialVaultConfiguration.CredResourceName, loginId, new NetworkCredential(string.Empty, accessToken).Password);
            return credential;
        }
        finally
        {
            if (ptrToCredential != IntPtr.Zero)
            {
                CredFree(ptrToCredential);
            }
        }
    }

    public static IEnumerable<string> GetAllSavedLoginIds()
    {
        IntPtr ptrToCredential = IntPtr.Zero;

        try
        {
            IntPtr[] allCredentials;
            uint count;

            if (CredEnumerate(CredentialVaultConfiguration.CredResourceName + "*", 0, out count, out ptrToCredential) != false)
            {
                allCredentials = new IntPtr[count];
                Marshal.Copy(ptrToCredential, allCredentials, 0, (int)count);
            }
            else
            {
                var error = Marshal.GetLastWin32Error();

                // NotFound is expected and can be ignored
                if (error == 1168)
                {
                    return Enumerable.Empty<string>();
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            if (count is 0)
            {
                return Enumerable.Empty<string>();
            }

            var allLoginIds = new List<string>();
            for (var i = 0; i < allCredentials.Length; i++)
            {
#pragma warning disable CS8605 // Unboxing a possibly null value.
                CREDENTIAL credential = (CREDENTIAL)Marshal.PtrToStructure(allCredentials[i], typeof(CREDENTIAL));
#pragma warning restore CS8605 // Unboxing a possibly null value.
                allLoginIds.Add(credential.UserName);
            }

            return allLoginIds;
        }
        finally
        {
            if (ptrToCredential != IntPtr.Zero)
            {
                CredFree(ptrToCredential);
            }
        }
    }
}
