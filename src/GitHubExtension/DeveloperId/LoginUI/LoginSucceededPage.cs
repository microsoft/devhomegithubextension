// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using GitHubExtension.Helpers;
using Microsoft.Windows.DevHome.SDK;

namespace GitHubExtension.DeveloperId.LoginUI;

internal class LoginSucceededPage : LoginUIPage
{
    public LoginSucceededPage(IDeveloperId developerId)
        : base(LoginUIState.LoginSucceededPage)
    {
        Data = new LoginSucceededPageData()
        {
            Message = $"{developerId.LoginId} {Resources.GetResource("LoginUI_LoginSucceededPage_text")}",
        };
    }

    internal class LoginSucceededPageData : ILoginUIPageData
    {
        public string? Message { get; set; } = string.Empty;

        public string GetJson()
        {
            return JsonSerializer.Serialize(
            this,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                IncludeFields = true,
            });
        }
    }
}
