// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

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

    internal class ActionPayload : SubmitActionPayload
    {
    }
}
