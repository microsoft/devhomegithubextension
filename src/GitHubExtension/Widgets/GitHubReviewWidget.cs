// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using GitHubExtension.DataManager;
using GitHubExtension.Helpers;
using Microsoft.Windows.Widgets.Providers;
using Octokit;

namespace GitHubExtension.Widgets;

internal sealed class GitHubReviewWidget : GitHubUserWidget
{
    protected override string GetTitleIconData()
    {
        return IconLoader.GetIconAsBase64("pulls.png");
    }

    public override void RequestContentData()
    {
        var request = new SearchIssuesRequest($"review-requested:{UserName}");

        RequestContentData(request);
    }

    public override void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        // This widget does not have the ShowCategory
        // property for the user to input,
        // so we always put it to PullRequest here.
        if (actionInvokedArgs.Verb == "Submit")
        {
            var data = actionInvokedArgs.Data;
            var dataObject = JsonNode.Parse(data);

            if (dataObject != null)
            {
                dataObject["showCategory"] = "PullRequests";
                SubmitAction(dataObject.ToString());
            }
        }
        else
        {
            base.OnActionInvoked(actionInvokedArgs);
        }
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
