﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using Windows.Security.Credentials;
using static GitHubExtension.DeveloperId.CredentialManager;

namespace GitHubExtension.DeveloperId;
public class CredentialVault : ICredentialVault
{
    private readonly string _credentialResourceName;

    private static class CredentialVaultConfiguration
    {
        public const string CredResourceName = "GitHubDevHomeExtension";
    }

    // Win32 Error codes
    public const int Win32ErrorNotFound = 1168;

    public CredentialVault(string applicationName = "")
    {
        _credentialResourceName = string.IsNullOrEmpty(applicationName) ? CredentialVaultConfiguration.CredResourceName : applicationName;
    }

    private string AddCredentialResourceNamePrefix(string loginId) => _credentialResourceName + ": " + loginId;

    public void SaveCredentials(string loginId, SecureString? accessToken)
    {
        // Initialize a credential object.
        var credential = new CREDENTIAL
        {
            Type = CRED_TYPE.GENERIC,
            TargetName = AddCredentialResourceNamePrefix(loginId),
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
        var credentialNameToRetrieve = AddCredentialResourceNamePrefix(loginId);
        var ptrToCredential = IntPtr.Zero;

        try
        {
            var isCredentialRetrieved = CredRead(credentialNameToRetrieve, CRED_TYPE.GENERIC, 0, out ptrToCredential);
            if (!isCredentialRetrieved)
            {
                var error = Marshal.GetLastWin32Error();
                Log.Logger()?.ReportError($"Retrieving credentials from Credential Manager has failed for {loginId} with {error}");

                // NotFound is expected and can be ignored.
                if (error == Win32ErrorNotFound)
                {
                    return null;
                }

                throw new Win32Exception(error);
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
                Log.Logger()?.ReportError($"No credentials found for this DeveloperId : {loginId}");
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

            var credential = new PasswordCredential(_credentialResourceName, loginId, accessTokenString);
            return credential;
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError($"Retrieving credentials from Credential Manager has failed unexpectedly: {loginId} : ", ex);
            throw;
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
        var targetCredentialToDelete = AddCredentialResourceNamePrefix(loginId);
        var isCredentialDeleted = CredDelete(targetCredentialToDelete, CRED_TYPE.GENERIC, 0);
        if (!isCredentialDeleted)
        {
            Log.Logger()?.ReportError($"Deleting credentials from Credential Manager has failed for {loginId}");
        }
    }

    public IEnumerable<string> GetAllCredentials()
    {
        var ptrToCredential = IntPtr.Zero;

        try
        {
            IntPtr[] allCredentials;
            uint count;

            if (CredEnumerate(_credentialResourceName + "*", 0, out count, out ptrToCredential) != false)
            {
                allCredentials = new IntPtr[count];
                Marshal.Copy(ptrToCredential, allCredentials, 0, (int)count);
            }
            else
            {
                var error = Marshal.GetLastWin32Error();

                // NotFound is expected and can be ignored.
                if (error == Win32ErrorNotFound)
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
            try
            {
                RemoveCredentials(credential);
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError($"Deleting credentials from Credential Manager has failed unexpectedly: {credential} : ", ex);
            }
        }
    }
}
