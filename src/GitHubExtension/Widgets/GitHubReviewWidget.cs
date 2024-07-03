// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using GitHubExtension.DataManager;
using GitHubExtension.Helpers;
using Microsoft.Windows.Widgets.Providers;
using Octokit;

namespace GitHubExtension.Widgets;

internal class GitHubReviewWidget : GitHubUserWidget
{
    public GitHubReviewWidget()
        : base()
    {
        // This widget does not allow customization, so this value will not change.
        ShowCategory = SearchCategory.PullRequests;
    }

    public override void RequestContentData()
    {
        var request = new SearchIssuesRequest($"review-requested:{UserName}");

        RequestContentData(request);
    }

    protected override string GetTitleIconData()
    {
        return IconLoader.GetIconAsBase64("pulls.png");
    }

    // This widget does not have "ShowCategory" as a variable.
    // So we override this method to not care about this data.
    protected override void ResetWidgetInfoFromState()
    {
        base.ResetWidgetInfoFromState();
        ShowCategory = SearchCategory.PullRequests;
    }

    // Overriding this method because this widget only cares about the account.
    public override void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        if (actionInvokedArgs.Verb == "Submit")
        {
            var data = actionInvokedArgs.Data;
            var dataObject = JsonNode.Parse(data);

            if (dataObject == null)
            {
                return;
            }

            DeveloperLoginId = dataObject["account"]?.GetValue<string>() ?? string.Empty;
            UpdateTitle(dataObject);

            ConfigurationData = data;

            // If we got here during the customization flow, we need to LoadContentData again
            // so we can show the loading page rather than stale data.
            LoadContentData();
            UpdateActivityState();
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
