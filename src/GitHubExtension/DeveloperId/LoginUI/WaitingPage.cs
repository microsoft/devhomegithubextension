﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

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
