// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Windows.DevHome.SDK;
using ResourceLoader = Microsoft.Windows.ApplicationModel.Resources.ResourceLoader;

namespace GitHubExtension.DeveloperId.LoginUI;

internal class LoginSucceededPage : LoginUIPage
{
    public LoginSucceededPage(IDeveloperId developerId)
        : base(LoginUIState.LoginSucceededPage)
    {
        var resourceLoader = new ResourceLoader(ResourceLoader.GetDefaultResourceFilePath(), "GitHubExtension/Resources");
        Data = new LoginSucceededPageData()
        {
            Message = $"{developerId.LoginId} {resourceLoader.GetString("LoginUI_LoginSucceededPage_text")}",
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
