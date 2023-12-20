// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Octokit;

namespace GitHubExtension.Widgets;
internal class GitHubMentionedInWidget : GitHubUserWidget
{
    private static readonly string TitleIconData =
        "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";

    protected static readonly new string Name = nameof(GitHubMentionedInWidget);

    protected override string GetTitleIconData()
    {
        return TitleIconData;
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
