// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.Helpers;

namespace GitHubExtension.DeveloperId.LoginUI;

internal sealed class WaitingPage : LoginUIPage
{
    public WaitingPage()
        : base(LoginUIState.WaitingPage)
    {
        Data = new WaitingPageData();
    }

    internal sealed class WaitingPageData : ILoginUIPageData
    {
        public string GetJson()
        {
            return Json.Stringify(this);
        }
    }
}
