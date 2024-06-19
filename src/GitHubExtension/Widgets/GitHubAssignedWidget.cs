// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.Helpers;
using Octokit;

namespace GitHubExtension.Widgets;

internal class GitHubAssignedWidget : GitHubUserWidget
{
    protected static readonly new string Name = nameof(GitHubAssignedWidget);

    protected override string GetTitleIconData()
    {
        return IconData.AssignedWidgetTitleIconData;
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
