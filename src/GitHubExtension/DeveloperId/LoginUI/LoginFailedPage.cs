// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.Helpers;

namespace GitHubExtension.DeveloperId.LoginUI;

internal sealed class LoginFailedPage : LoginUIPage
{
    public LoginFailedPage()
        : base(LoginUIState.LoginFailedPage)
    {
        Data = new LoginFailedPageData();
    }

    internal sealed class LoginFailedPageData : ILoginUIPageData
    {
        public string GetJson()
        {
            return Json.Stringify(this);
        }
    }
}
