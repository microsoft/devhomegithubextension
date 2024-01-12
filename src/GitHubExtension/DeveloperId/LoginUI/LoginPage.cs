﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubExtension.Helpers;

namespace GitHubExtension.DeveloperId.LoginUI;

internal class LoginPage : LoginUIPage
{
    public LoginPage()
        : base(LoginUIState.LoginPage)
    {
        Data = new PageData();
    }

    internal class PageData : ILoginUIPageData
    {
        public string GetJson()
        {
            return Json.Stringify(this);
        }
    }

    internal class ActionPayload : SubmitActionPayload
    {
        public bool IsEnterprise()
        {
            return this.Id == "Enterprise";
        }
    }
}
