// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.Helpers;
using Octokit;

namespace GitHubExtension.Widgets;

internal class GitHubMentionedInWidget : GitHubUserWidget
{
    protected override string GetTitleIconData()
    {
        return IconData.MentionedInWidgetTitleIconData;
    }

    public override void RequestContentData()
    {
        var request = new SearchIssuesRequest()
        {
            Mentions = UserName,
        };

        RequestContentData(request);
    }

    public override string GetTemplatePath(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.SignIn => @"Widgets\Templates\GitHubSignInTemplate.json",
            WidgetPageState.Configure => @"Widgets\Templates\GitHubMentionedInConfigurationTemplate.json",
            WidgetPageState.Content => @"Widgets\Templates\GitHubMentionedInTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\GitHubLoadingTemplate.json",
            _ => throw new NotImplementedException(),
        };
    }
}
