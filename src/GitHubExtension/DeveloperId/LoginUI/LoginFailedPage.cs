// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubExtension.Helpers;

namespace GitHubExtension.DeveloperId.LoginUI;

internal class LoginFailedPage : LoginUIPage
{
    public LoginFailedPage()
        : base(LoginUIState.LoginFailedPage)
    {
        Data = new LoginFailedPageData();
    }

    internal class LoginFailedPageData : ILoginUIPageData
    {
        public string GetJson()
        {
            return Json.Stringify(this);
        }
    }
}
