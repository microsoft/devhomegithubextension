// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.Helpers;
using Microsoft.Windows.DevHome.SDK;

namespace GitHubExtension.DeveloperId.LoginUI;

internal sealed class LoginSucceededPage : LoginUIPage
{
    public LoginSucceededPage(IDeveloperId developerId)
        : base(LoginUIState.LoginSucceededPage)
    {
        Data = new LoginSucceededPageData()
        {
            Message = $"{Resources.GetResource("LoginUI_LoginSucceededPage_text").Replace("{User}", developerId.LoginId)}",
        };
    }

    internal sealed class LoginSucceededPageData : ILoginUIPageData
    {
        public string? Message { get; set; } = string.Empty;

        public string GetJson()
        {
            return Json.Stringify(this, _optionsWithContext);
        }
    }
}
