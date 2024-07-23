// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.Helpers;

namespace GitHubExtension.DeveloperId.LoginUI;

internal sealed class EnterpriseServerPage : LoginUIPage
{
    public EnterpriseServerPage(Uri? hostAddress, string errorText)
        : base(LoginUIState.EnterpriseServerPage)
    {
        Data = new PageData()
        {
            EnterpriseServerInputValue = hostAddress?.ToString() ?? string.Empty,
            EnterpriseServerPageErrorValue = errorText ?? string.Empty,
            EnterpriseServerPageErrorVisible = !string.IsNullOrEmpty(errorText),
        };
    }

    public EnterpriseServerPage(string hostAddress, string errorText)
        : base(LoginUIState.EnterpriseServerPage)
    {
        Data = new PageData()
        {
            EnterpriseServerInputValue = hostAddress,
            EnterpriseServerPageErrorValue = errorText ?? string.Empty,
            EnterpriseServerPageErrorVisible = !string.IsNullOrEmpty(errorText),
        };
    }

    internal sealed class PageData : ILoginUIPageData
    {
        public string EnterpriseServerInputValue { get; set; } = string.Empty;

        // Default is false
        public bool EnterpriseServerPageErrorVisible { get; set; }

        public string EnterpriseServerPageErrorValue { get; set; } = string.Empty;

        public string GetJson()
        {
            return Json.Stringify(this);
        }
    }

    internal sealed class ActionPayload : SubmitActionPayload
    {
    }

    internal sealed class InputPayload
    {
        public string? EnterpriseServer
        {
            get; set;
        }
    }
}
