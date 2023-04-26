// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json.Nodes;
using GitHubPlugin.Client;
using GitHubPlugin.DataManager;
using GitHubPlugin.Helpers;
using Octokit;

namespace GitHubPlugin.Widgets;
internal class SshWalletWidget : GithubWidget
{
    private static Dictionary<string, string> Templates { get; set; } = new ();

    protected static readonly new string Name = nameof(SshWalletWidget);

    public SshWalletWidget()
        : base()
    {
        GitHubDataManager.OnUpdate += DataManagerUpdateHandler;
    }

    ~SshWalletWidget()
    {
        GitHubDataManager.OnUpdate -= DataManagerUpdateHandler;
    }

    public override void DeleteWidget(string widgetId, string customState)
    {
        // Remove event handler
        GitHubDataManager.OnUpdate -= DataManagerUpdateHandler;
        base.DeleteWidget(widgetId, customState);
    }

    public override void RequestContentData()
    {
        if (RepositoryUrl == string.Empty)
        {
            // Nothing to request.
            return;
        }

        // Throttle protection against a widget trigging rapid data updates.
        if (DateTime.Now - LastUpdated < WidgetDataRequestMinTime)
        {
            Log.Logger()?.ReportDebug(Name, ShortId, "Data request too soon, skipping.");
        }

        try
        {
            Log.Logger()?.ReportDebug(Name, ShortId, $"Requesting data update for {GetOwner()}/{GetRepo()}");
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
            _ = dataManager?.UpdatePullRequestsForRepositoryAsync(GetOwner(), GetRepo(), requestOptions);
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
        if (RepositoryUrl == string.Empty)
        {
            ContentData = string.Empty;
            DataState = WidgetDataState.Okay;
            return;
        }

        Log.Logger()?.ReportDebug(Name, ShortId, "Getting Data for Pull Requests");

        try
        {
            using var dataManager = GitHubDataManager.CreateInstance();
            var repository = dataManager!.GetRepository(GetOwner(), GetRepo());
            var pulls = repository is null ? Enumerable.Empty<DataModel.PullRequest>() : repository.PullRequests;

            var pullsData = new JsonObject();
            var pullsArray = new JsonArray();
            foreach (var pullItem in pulls)
            {
                var pull = new JsonObject
                {
                    { "title", pullItem.Title },
                    { "url", pullItem.HtmlUrl },
                    { "number", pullItem.Number },
                    { "user", pullItem.Author.Login },
                    { "avatar", pullItem.Author.AvatarUrl },
                };

                ((IList<JsonNode?>)pullsArray).Add(pull);
            }

            pullsData.Add("pulls", pullsArray);
            pullsData.Add("selected_repo", repository?.FullName ?? string.Empty);

            LastUpdated = DateTime.Now;
            ContentData = pullsData.ToJsonString();
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
            WidgetPageState.Configure => @"Widgets\Templates\SshWalletConfigurationTemplate.json",
            WidgetPageState.Content => @"Widgets\Templates\SshWalletTemplate.json",
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
            if (fullName == e.Repository && e.Context.Contains("PullRequests"))
            {
                Log.Logger()?.ReportInfo(Name, ShortId, $"Received matching repository update event.");
                LoadContentData();
                UpdateActivityState();
            }
        }
    }
}
