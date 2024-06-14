// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.Helpers;

namespace GitHubExtension.DeveloperId.LoginUI;

internal sealed class LoginPage : LoginUIPage
{
    public LoginPage()
        : base(LoginUIState.LoginPage)
    {
        Data = new PageData();
    }

    internal sealed class PageData : ILoginUIPageData
    {
        public string GetJson()
        {
            return Json.Stringify(this);
        }
    }

    internal sealed class ActionPayload : SubmitActionPayload
    {
        public bool IsEnterprise()
        {
            return this.Id == "Enterprise";
        }
    }
}
