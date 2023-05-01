// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json.Nodes;
using GitHubPlugin.Client;
using GitHubPlugin.DataManager;
using GitHubPlugin.Helpers;
using Octokit;

namespace GitHubPlugin.Widgets;
internal class GithubPullsWidget : GithubWidget
{
    public static readonly string PullsIconData =
        "iVBORw0KGgoAAAANSUhEUgAAABQAAAAUCAYAAACNiR0NAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv" +
        "8YQUAAAG5SURBVHgB1VRLUsJAFHwzuBdvgCfQAyBiuUB04XADPYFyAvAEwAnkBDqsAF0Q1GIL3gBPQFYupJKxX0yoBPLBKjf2JpN5PZ2eed" +
        "MR5KPYV2Up6ZLHrku9t6q2aA2lJ3UjlzS2LvSMEiA94lA1cjl6FIbygqiQkzQqPatGRAwc1JpYYVMKBDtjAeHSvlXVc54s91XB5GgqHKphz" +
        "grEXEN1Y2iGHdiTBJeiNFBtPAsvZ1pFHPVVlwTZ0qGu2aFpzNo5hO9eq7q7sWV8fbHxJUm7/OTzgvtrHhsSV+OKFuKT9uC+gzO/Pxqo24ig" +
        "NKShqHibwaQ/Vl6NReGCRQWZLmplq6Zt61y3DRxKQZGzFr5A00ivoGHDhm+FBnWsim6GySzGW12d9UjlzRctljuyMDl9+FgJBmSItrxJl+p" +
        "WzLWJw/FQGfBPAr4MCjyBLbzD4WxbsThI+mP8I0FODO7eAdp0WPzpZiqYgwR5TXSwJiIYZJkbgtd5XJbDCGcfrxp3sRXwt8ryurM0voS6Qu" +
        "B7QZHBY+NQz0Vt3V0WPzPLcUjjb5XliMMM/q+yHBJI5EeyjB+o8s9JZ8Uvif8N+rQImUfqheMAAAAASUVORK5CYII=";

    private static Dictionary<string, string> Templates { get; set; } = new ();

    protected static readonly new string Name = nameof(GithubPullsWidget);

    public GithubPullsWidget()
        : base()
    {
        GitHubDataManager.OnUpdate += DataManagerUpdateHandler;
    }

    ~GithubPullsWidget()
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
            pullsData.Add("is_loading_data", DataState == WidgetDataState.Unknown);
            pullsData.Add("pulls_icon_data", PullsIconData);

            LastUpdated = DateTime.Now;
            DataState = WidgetDataState.Okay;
            ContentData = pullsData.ToJsonString();
        }
        catch (Exception e)
        {
            Log.Logger()?.ReportError(Name, ShortId, "Error retrieving data.", e);
            DataState = WidgetDataState.Failed;
            return;
        }
    }

#if false
    /// <summary>
    /// This method converts an image to a Base64 string that can be used to embed an image in an Adaptive Card.
    /// If any of the following images change, change the image property "Copy to Output Directotry" to "Always".
    /// Then run this method and replace the existing string with the output.
    /// <list type="bullet">
    /// <item>pulls.png</item>
    /// <item>issues.png</item>
    /// </list>
    /// </summary>
    /// <returns>The converted base64 image.</returns>
    public static string ConvertIconToDataString(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "../../GitHubPlugin/Widgets/Assets/", fileName);
        var imageData = Convert.ToBase64String(File.ReadAllBytes(path.ToString()));
        return imageData;
    }
#endif

    public override string GetTemplatePath(WidgetPageState page)
    {
        return page switch
        {
            WidgetPageState.SignIn => @"Widgets\Templates\GitHubSignInTemplate.json",
            WidgetPageState.Configure => @"Widgets\Templates\GitHubPullsConfigurationTemplate.json",
            WidgetPageState.Content => @"Widgets\Templates\GitHubPullsTemplate.json",
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
        Log.Logger()?.ReportDebug(Name, ShortId, $"Data Update Event: Kind={e.Kind} Info={e.Description} Context={string.Join(",", e.Context)}");
        if (e.Kind == DataManagerUpdateKind.Repository && !string.IsNullOrEmpty(RepositoryUrl))
        {
            var fullName = Validation.ParseFullNameFromGitHubURL(RepositoryUrl);
            if (fullName == e.Description && e.Context.Contains("PullRequests"))
            {
                Log.Logger()?.ReportInfo(Name, ShortId, $"Received matching repository update event.");
                LoadContentData();
                UpdateActivityState();
            }
        }
    }
}
