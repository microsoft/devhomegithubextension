// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

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
