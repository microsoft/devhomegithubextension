// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Octokit;

namespace GitHubExtension.Widgets;

internal class GitHubAssignedWidget : GitHubUserWidget
{
    private static readonly string TitleIconData =
        "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";

    protected static readonly new string Name = nameof(GitHubAssignedWidget);

    protected override string GetTitleIconData()
    {
        return TitleIconData;
    }

    public override void RequestContentData()
    {
        var request = new SearchIssuesRequest()
        {
            Assignee = UserName,
        };

        RequestContentData(request);
    }

    public override string GetTemplatePath(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.SignIn => @"Widgets\Templates\GitHubSignInTemplate.json",
            WidgetPageState.Configure => @"Widgets\Templates\GitHubAssignedConfigurationTemplate.json",
            WidgetPageState.Content => @"Widgets\Templates\GitHubAssignedTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\GitHubLoadingTemplate.json",
            _ => throw new NotImplementedException(),
        };
    }
}
