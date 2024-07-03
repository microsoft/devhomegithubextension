// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security;
using GitHubExtension.Helpers;

namespace GitHubExtension.DeveloperId.LoginUI;

internal class EnterpriseServerPATPage : LoginUIPage
{
    public EnterpriseServerPATPage(Uri hostAddress, string errorText, SecureString inputPAT)
        : base(LoginUIState.EnterpriseServerPATPage)
    {
        Data = new PageData()
        {
            EnterpriseServerPATPageInputValue = new System.Net.NetworkCredential(string.Empty, inputPAT).Password ?? string.Empty,
            EnterpriseServerPATPageErrorValue = errorText ?? string.Empty,
            EnterpriseServerPATPageErrorVisible = !string.IsNullOrEmpty(errorText),
            EnterpriseServerPATPageCreatePATUrlValue = hostAddress?.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped)
                                                       + $"/settings/tokens/new?scopes=read:user,notifications,repo,read:org&description=DevHomeGitHubExtension",
            EnterpriseServerPATPageServerUrlValue = hostAddress?.Host ?? string.Empty,
        };
    }

    internal class PageData : ILoginUIPageData
    {
        public string EnterpriseServerPATPageInputValue { get; set; } = string.Empty;

        public bool EnterpriseServerPATPageErrorVisible { get; set; }

        public string EnterpriseServerPATPageErrorValue { get; set; } = string.Empty;

        public string EnterpriseServerPATPageCreatePATUrlValue { get; set; } = "https://github.com/";

        public string EnterpriseServerPATPageServerUrlValue { get; set; } = "https://github.com/";

        public string GetJson()
        {
            return Json.Stringify(this);
        }
    }

    internal class ActionPayload : SubmitActionPayload
    {
        public string? URL
        {
            get; set;
        }
    }

    internal class InputPayload
    {
        public string? PAT
        {
            get; set;
        }
    }
}
