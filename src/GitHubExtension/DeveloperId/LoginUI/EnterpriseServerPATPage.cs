// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using GitHubExtension.Helpers;

namespace GitHubExtension.DeveloperId.LoginUI;

internal class EnterpriseServerPATPage : LoginUIPage
{
    public EnterpriseServerPATPage(Uri hostAddress, string errorText, string? inputPAT)
        : base(LoginUIState.EnterpriseServerPATPage)
    {
        Data = new PageData()
        {
            EnterpriseServerPATPageInputValue = inputPAT ?? string.Empty,
            EnterpriseServerPATPageErrorValue = errorText ?? string.Empty,
            EnterpriseServerPATPageErrorVisible = !string.IsNullOrEmpty(errorText),
            EnterpriseServerPATPageCreatePATUrlValue = hostAddress?.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped) + $"/settings/tokens/new?scopes=read:user,notifications,repo,read:org&description=DevHomePAT",
            EnterpriseServerPATPageServerUrlValue = hostAddress?.OriginalString ?? string.Empty,
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
