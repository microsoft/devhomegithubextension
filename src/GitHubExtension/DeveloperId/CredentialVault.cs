// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using Octokit;
using Windows.Security.Credentials;
using static GitHubExtension.DeveloperId.CredentialManager;

namespace GitHubExtension.DeveloperId;
public class CredentialVault : ICredentialVault
{
    private static readonly object CredentialVaultLock = new ();

    // CredentialVault uses singleton pattern.
    private static CredentialVault? singletonCredentialVault;

    public static CredentialVault GetInstance()
    {
        lock (CredentialVaultLock)
        {
            singletonCredentialVault ??= new CredentialVault();
        }

        return singletonCredentialVault;
    }

    private static class CredentialVaultConfiguration
    {
        public const string CredResourceName = "GitHubDevHomeExtension";
    }

    public void SaveCredentials(string loginId, SecureString? accessToken)
    {
        // Initialize a credential object.
        var credential = new CREDENTIAL
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

            // Store credential under Windows Credentials inside Credential Manager.
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

    public PasswordCredential? GetCredentials(string loginId)
    {
        var credentialNameToRetrieve = CredentialVaultConfiguration.CredResourceName + ": " + loginId;
        var ptrToCredential = IntPtr.Zero;

        try
        {
            var isCredentialRetrieved = CredRead(credentialNameToRetrieve, CRED_TYPE.GENERIC, 0, out ptrToCredential);
            if (!isCredentialRetrieved)
            {
                Log.Logger()?.ReportInfo($"Retrieving credentials from Credential Manager has failed");
                return null;
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
                return null;
            }

            var accessTokenInChars = new char[credentialObject.CredentialBlobSize / 2];
            Marshal.Copy(credentialObject.CredentialBlob, accessTokenInChars, 0, accessTokenInChars.Length);

            // convert accessTokenInChars to string
            string accessTokenString = new (accessTokenInChars);

            for (var i = 0; i < accessTokenInChars.Length; i++)
            {
                // Zero out characters after they are copied over from an unmanaged to managed type.
                accessTokenInChars[i] = '\0';
            }

            var credential = new PasswordCredential(CredentialVaultConfiguration.CredResourceName, loginId, accessTokenString);
            return credential;
        }
        catch (Exception)
        {
            Log.Logger()?.ReportInfo($"Retrieving credentials from Credential Manager has failed unexpectedly");
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally
        {
            if (ptrToCredential != IntPtr.Zero)
            {
                CredFree(ptrToCredential);
            }
        }
    }

    public void RemoveCredentials(string loginId)
    {
        var targetCredentialToDelete = CredentialVaultConfiguration.CredResourceName + ": " + loginId;
        var isCredentialDeleted = CredDelete(targetCredentialToDelete, CRED_TYPE.GENERIC, 0);
        if (!isCredentialDeleted)
        {
            Log.Logger()?.ReportInfo($"Deleting credentials from Credential Manager has failed");
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public IEnumerable<string> GetAllCredentials()
    {
        var ptrToCredential = IntPtr.Zero;

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

                // NotFound is expected and can be ignored.
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
                var credential = (CREDENTIAL)Marshal.PtrToStructure(allCredentials[i], typeof(CREDENTIAL));
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

    public void RemoveAllCredentials()
    {
        var allCredentials = GetAllCredentials();
        foreach (var credential in allCredentials)
        {
            RemoveCredentials(credential);
        }
    }
}
