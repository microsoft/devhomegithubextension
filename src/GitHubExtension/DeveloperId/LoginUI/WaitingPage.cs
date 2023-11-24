// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

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
