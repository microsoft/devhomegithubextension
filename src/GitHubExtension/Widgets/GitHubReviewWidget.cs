// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.Helpers;
using Octokit;

namespace GitHubExtension.Widgets;

internal sealed class GitHubReviewWidget : GitHubUserWidget
{
    protected override string DefaultShowCategory => "PullRequests";

    protected override string GetTitleIconData()
    {
        return IconLoader.GetIconAsBase64("pulls.png");
    }

    public override void RequestContentData()
    {
        var request = new SearchIssuesRequest($"review-requested:{UserName}");

        RequestContentData(request);
    }

    public override string GetTemplatePath(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.SignIn => @"Widgets\Templates\GitHubSignInTemplate.json",
            WidgetPageState.Configure => @"Widgets\Templates\GitHubReviewConfigurationTemplate.json",
            WidgetPageState.Content => @"Widgets\Templates\GitHubReviewTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\GitHubLoadingTemplate.json",
            _ => throw new NotImplementedException(),
        };
    }
}
