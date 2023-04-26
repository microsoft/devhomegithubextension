// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GitHubPlugin.Client;
using GitHubPlugin.DataManager;
using GitHubPlugin.DataModel;
using GitHubPlugin.Helpers;
using GitHubPlugin.Widgets.Enums;
using Microsoft.Windows.Widgets.Providers;
using Octokit;

namespace GitHubPlugin.Widgets;
internal class GithubMentionedInWidget : GithubWidget
{
    private static Dictionary<string, string> Templates { get; set; } = new ();

    protected static readonly new string Name = nameof(GithubMentionedInWidget);

    private string ShowCategory
    {
        get; set;
    }

    private string mentionedName = string.Empty;

    private string MentionedName
    {
        get
        {
            if (string.IsNullOrEmpty(mentionedName))
            {
                GetMentionedName();
            }

            return mentionedName;
        }
        set => mentionedName = value;
    }

    public GithubMentionedInWidget()
        : base()
    {
        GitHubDataManager.OnUpdate += DataManagerUpdateHandler;
        ShowCategory = "Issues PRs";
    }

    ~GithubMentionedInWidget()
    {
        GitHubDataManager.OnUpdate -= DataManagerUpdateHandler;
    }

    private void GetMentionedName()
    {
        var devIds = DeveloperId.DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIdsInternal();
        if ((devIds != null) && devIds.Any())
        {
            mentionedName = devIds.First().LoginId;
        }
    }

    public override void DeleteWidget(string widgetId, string customState)
    {
        // Remove event handler
        GitHubDataManager.OnUpdate -= DataManagerUpdateHandler;
        base.DeleteWidget(widgetId, customState);
    }

    public override void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        Enabled = contextChangedArgs.WidgetContext.IsActive;
        UpdateActivityState(true);
    }

    public override void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        if (actionInvokedArgs.Verb == "Submit")
        {
            var dataObject = JsonSerializer.Deserialize(actionInvokedArgs.Data, SourceGenerationContext.Default.DataPayload);
            if (dataObject != null && dataObject.ShowCategory != null)
            {
                ShowCategory = dataObject.ShowCategory;
            }
        }
        else
        {
            base.OnActionInvoked(actionInvokedArgs);
        }
    }

    public override void RequestContentData()
    {
        // Throttle protection against a widget trigging rapid data updates.
        if (DateTime.Now - LastUpdated < WidgetDataRequestMinTime)
        {
            Log.Logger()?.ReportDebug(Name, ShortId, "Data request too soon, skipping.");
        }

        try
        {
            Log.Logger()?.ReportInfo(Name, ShortId, $"Requesting data update for {GetOwner()}/{GetRepo()}");
            var requestOptions = new RequestOptions
            {
                PullRequestRequest = new PullRequestRequest
                {
                    State = ItemStateFilter.Open,
                    SortProperty = PullRequestSort.Updated,
                    SortDirection = SortDirection.Descending,
                },
                ApiOptions = new ApiOptions
                {
                    PageSize = 10,
                    PageCount = 1,
                    StartPage = 1,
                },
                UsePublicClientAsFallback = true,
            };

            var dataManager = GitHubDataManager.CreateInstance();
            _ = dataManager?.UpdateMentionedInAsync(GetOwner(), GetRepo(), requestOptions);
            Log.Logger()?.ReportInfo(Name, ShortId, $"Requested data update for {GetOwner()}/{GetRepo()}");
            DataState = WidgetDataState.Requested;
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(Name, ShortId, "Failed requesting data update.", ex);
        }
    }

    public override void LoadContentData()
    {
        Log.Logger()?.ReportDebug(Name, ShortId, "Getting Data for Mentioned in");

        try
        {
            using var dataManager = GitHubDataManager.CreateInstance();
            var issuesData = new JsonObject();
            var issuesArray = new JsonArray();

            var issue = new JsonObject
                {
                    { "title", "Sing in the rain" },
                    { "url", "https://github.com/microsoft/PowerToys" },
                    { "number", 12 },
                    { "date", "2023-04-15" },
                    { "user", "Frank Sinatra" },
                    { "iconUrl", "https://learn.microsoft.com/en-us/windows/apps/design/style/images/segoe-mdl/e877.png" },
                };
            var issueLabels = new JsonArray();
            var issueLabel = new JsonObject
            {
                        { "name", "Loud" },
                        { "color", "warning" },
            };
            ((IList<JsonNode?>)issueLabels).Add(issueLabel);
            issueLabel = new JsonObject
            {
                        { "name", "C mol" },
                        { "color", "good" },
            };
            ((IList<JsonNode?>)issueLabels).Add(issueLabel);
            issue.Add("labels", issueLabels);
            ((IList<JsonNode?>)issuesArray).Add(issue);
            issue = new JsonObject
                {
                    { "title", "win WC" },
                    { "url", "https://github.com/microsoft/PowerToys" },
                    { "number", 34 },
                    { "date", "2023-03-25" },
                    { "user", "Leonel Messi" },
                    { "iconUrl", "https://learn.microsoft.com/en-us/windows/apps/design/style/images/segoe-mdl/e958.png" },
                };
            issue.Add("labels", new JsonArray());
            ((IList<JsonNode?>)issuesArray).Add(issue);

            /*
            foreach (var issueItem in issues)
            {
                var issue = new JsonObject
                {
                    { "title", issueItem.Title },
                    { "url", issueItem.HtmlUrl },
                    { "number", issueItem.Number },
                    { "date", issueItem.CreatedAt.ToLocalTime().ToStringInvariant() },
                    { "user", issueItem.Author.Login },
                    { "avatar", issueItem.Author.AvatarUrl },
                };

                var labels = issueItem.Labels.ToList();
                var issueLabels = new JsonArray();
                StringBuilder labelsString = new ();
                foreach (var label in labels)
                {
                    var issueLabel = new JsonObject
                    {
                        { "name", label.Name },
                        { "color", label.Color },
                    };

                    ((IList<JsonNode?>)issueLabels).Add(issueLabel);

                    if (labelsString.Length != 0)
                    {
                        labelsString.Append("  ");
                    }

                    labelsString.Append(label.Name);
                }

                issue.Add("labels", issueLabels);
                issue.Add("labelsString", labelsString.ToString());

                ((IList<JsonNode?>)issuesArray).Add(issue);
            }
            */
            issuesData.Add("items", issuesArray);
            issuesData.Add("assignedName", MentionedName);
            issuesData.Add("openCount", "2");

            // issuesData.Add("selected_repo", repository?.FullName ?? string.Empty);
            LastUpdated = DateTime.Now;
            ContentData = issuesData.ToJsonString();
            DataState = WidgetDataState.Okay;
        }
        catch (Exception e)
        {
            Log.Logger()?.ReportError(Name, ShortId, "Error retrieving data.", e);
            DataState = WidgetDataState.Failed;
            return;
        }
    }

    public override string GetTemplatePath(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.SignIn => @"Widgets\Templates\GitHubSignInTemplate.json",
            WidgetPageState.Configure => @"Widgets\Templates\GitHubMentionedInConfigurationTemplate.json",
            WidgetPageState.Content => @"Widgets\Templates\GitHubMentionedInTemplate.json",
            WidgetPageState.Loading => @"Widgets\Templates\GithubLoadingTemplate.json",
            _ => throw new NotImplementedException(),
        };
    }

    public override string GetData(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.SignIn => new JsonObject { { "message", Resources.GetResource(@"Widget_Template/SignInRequired", Log.Logger()) } }.ToJsonString(),
            WidgetPageState.Configure => GetConfiguration(RepositoryUrl),
            WidgetPageState.Content => ContentData,
            WidgetPageState.Loading => EmptyJson,
            _ => throw new NotImplementedException(Page.GetType().Name),
        };
    }

    private void DataManagerUpdateHandler(object? source, DataManagerUpdateEventArgs e)
    {
        Log.Logger()?.ReportDebug(Name, ShortId, $"Data Update Event: Kind={e.Kind} Info={e.Repository} Context={string.Join(",", e.Context)}");
        if (e.Kind == DataManagerUpdateKind.Repository && !string.IsNullOrEmpty(RepositoryUrl))
        {
            var fullName = Validation.ParseFullNameFromGitHubURL(RepositoryUrl);
            if (fullName == e.Repository && e.Context.Contains("MentionedIn"))
            {
                Log.Logger()?.ReportInfo(Name, ShortId, $"Received matching repository update event.");
                LoadContentData();
                UpdateActivityState();
            }
        }
    }
}
