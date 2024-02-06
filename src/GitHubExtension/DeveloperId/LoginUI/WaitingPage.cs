// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.Helpers;

namespace GitHubExtension.DeveloperId.LoginUI;

internal class WaitingPage : LoginUIPage
{
    public WaitingPage()
        : base(LoginUIState.WaitingPage)
    {
        Data = new WaitingPageData();
    }

    internal class WaitingPageData : ILoginUIPageData
    {
        public string GetJson()
        {
            return Json.Stringify(this);
        }
    }
}
