// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.Helpers;

namespace GitHubExtension.DeveloperId.LoginUI;

internal sealed class LoginPage : LoginUIPage
{
    public LoginPage()
        : base(LoginUIState.LoginPage)
    {
        Data = new LoginPageData();
    }

    internal sealed class LoginPageData : ILoginUIPageData
    {
        public string GetJson()
        {
            return Json.Stringify(this, _optionsWithContext);
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
