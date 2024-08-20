// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using static GitHubExtension.DeveloperId.LoginUI.EnterpriseServerPage;
using static GitHubExtension.DeveloperId.LoginUI.EnterpriseServerPATPage;
using static GitHubExtension.DeveloperId.LoginUI.LoginFailedPage;
using static GitHubExtension.DeveloperId.LoginUI.LoginPage;
using static GitHubExtension.DeveloperId.LoginUI.LoginSucceededPage;
using static GitHubExtension.DeveloperId.LoginUI.WaitingPage;

namespace GitHubExtension.DeveloperId;

[JsonSerializable(typeof(EnterpriseServerPageData))]
[JsonSerializable(typeof(EnterpriseServerPATPageData))]
[JsonSerializable(typeof(LoginFailedPageData))]
[JsonSerializable(typeof(LoginPageData))]
[JsonSerializable(typeof(LoginSucceededPageData))]
[JsonSerializable(typeof(WaitingPageData))]
internal sealed partial class JsonSourceGenerationContext : JsonSerializerContext
{
}
