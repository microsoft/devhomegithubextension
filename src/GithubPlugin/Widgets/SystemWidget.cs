// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using GitHubPlugin.Client;
using GitHubPlugin.DataManager;
using GitHubPlugin.DeveloperId;
using GitHubPlugin.Helpers;
using GitHubPlugin.Widgets.Enums;
using Microsoft.Windows.DevHome.SDK;
using Microsoft.Windows.Widgets.Providers;

namespace GitHubPlugin.Widgets;

internal class SystemWidget : WidgetImpl
{
    protected static readonly TimeSpan WidgetDataRequestMinTime = TimeSpan.FromSeconds(30);
    protected static readonly TimeSpan WidgetRefreshRate = TimeSpan.FromMinutes(5);
    protected static readonly string EmptyJson = new JsonObject().ToJsonString();

    private DateTime lastUpdateRequest = DateTime.MinValue;

    protected static readonly string Name = nameof(GithubWidget);

    protected WidgetActivityState ActivityState { get; set; } = WidgetActivityState.Unknown;

    protected WidgetDataState DataState { get; set; } = WidgetDataState.Unknown;

    protected WidgetPageState Page { get; set; } = WidgetPageState.Unknown;

    protected string ContentData { get; set; } = EmptyJson;

    protected bool Enabled
    {
        get; set;
    }

    public string WidgetState
    {
        get;
        private set;
    }

    protected Dictionary<WidgetPageState, string> Template { get; set; } = new ();

    protected DateTime LastUpdated { get; set; } = DateTime.MinValue;

    protected DataUpdater DataUpdater
    {
        get; set;
    }

    public SystemWidget()
    {
        DataUpdater = new DataUpdater(PeriodicUpdate);
        WidgetState = string.Empty;
    }

    ~SystemWidget()
    {
        DataUpdater?.Dispose();
    }

    public virtual void RequestContentData()
    {
        throw new NotImplementedException();
    }

    public virtual void LoadContentData()
    {
        throw new NotImplementedException();
    }

    public override void CreateWidget(WidgetContext widgetContext, string state)
    {
        Id = widgetContext.Id;
        Enabled = widgetContext.IsActive;
        WidgetState = state;
        UpdateActivityState();
    }

    public override void Activate(WidgetContext widgetContext)
    {
        Enabled = true;
        UpdateActivityState();
    }

    public override void Deactivate(string widgetId)
    {
        Enabled = false;
        UpdateActivityState();
    }

    public override void DeleteWidget(string widgetId, string customState)
    {
        Enabled = false;
        SetDeleted();
    }

    public override void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        Enabled = contextChangedArgs.WidgetContext.IsActive;
        UpdateActivityState();
    }

    public override void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        var verb = GetWidgetActionForVerb(actionInvokedArgs.Verb);
        Log.Logger()?.ReportDebug(Name, ShortId, $"ActionInvoked: {verb}");

        /*
        switch (verb)
        {
            case WidgetAction.CheckUrl:
                HandleCheckUrl(actionInvokedArgs);
                break;

            case WidgetAction.SignIn:
                _ = HandleSignIn();
                break;

            case WidgetAction.Unknown:
                Log.Logger()?.ReportError(Name, ShortId, $"Unknown verb: {actionInvokedArgs.Verb}");
                break;
        }
        */
    }

    private WidgetAction GetWidgetActionForVerb(string verb)
    {
        try
        {
            return Enum.Parse<WidgetAction>(verb);
        }
        catch (Exception)
        {
            // Invalid verb.
            Log.Logger()?.ReportError($"Unknown WidgetAction verb: {verb}");
            return WidgetAction.Unknown;
        }
    }

    public void UpdateActivityState()
    {
        if (Enabled)
        {
            SetActive();
            return;
        }

        SetInactive();
    }

    public void UpdateWidget()
    {
        WidgetUpdateRequestOptions updateOptions = new (Id)
        {
            Data = GetData(Page),
            Template = GetTemplateForPage(Page),
            CustomState = WidgetState,
        };

        Log.Logger()?.ReportDebug(Name, ShortId, $"Updating widget for {Page}");
        WidgetManager.GetDefault().UpdateWidget(updateOptions);
    }

    public virtual string GetTemplatePath(WidgetPageState page)
    {
        return string.Empty;
    }

    public virtual string GetData(WidgetPageState page)
    {
        return string.Empty;
    }

    protected string GetTemplateForPage(WidgetPageState page)
    {
        if (Template.ContainsKey(page))
        {
            Log.Logger()?.ReportDebug(Name, ShortId, $"Using cached template for {page}");
            return Template[page];
        }

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, GetTemplatePath(page));
            var template = File.ReadAllText(path, Encoding.Default) ?? throw new FileNotFoundException(path);
            template = Resources.ReplaceIdentifers(template, Resources.GetWidgetResourceIdentifiers(), Log.Logger());
            Log.Logger()?.ReportDebug(Name, ShortId, $"Caching template for {page}");
            Template[page] = template;
            return template;
        }
        catch (Exception e)
        {
            Log.Logger()?.ReportError(Name, ShortId, "Error getting template.", e);
            return string.Empty;
        }
    }

    protected string GetCurrentState()
    {
        return $"State: {ActivityState}  Page: {Page}  Data: {DataState}  Repository: {WidgetState}";
    }

    protected void LogCurrentState()
    {
        Log.Logger()?.ReportDebug(Name, ShortId, GetCurrentState());
    }

    private void SetActive()
    {
        ActivityState = WidgetActivityState.Active;
        Page = WidgetPageState.Content;
        if (ContentData == EmptyJson)
        {
            LoadContentData();
        }

        _ = DataUpdater.Start();
        LogCurrentState();
        UpdateWidget();
    }

    private void SetInactive()
    {
        ActivityState = WidgetActivityState.Inactive;
        DataUpdater.Stop();

        // No need to update when we are inactive.
        LogCurrentState();
    }

    private void SetDeleted()
    {
        // If this widget is deleted, disable the data updater, clear the state, set to Unknown.
        DataUpdater.Stop();
        SetState(string.Empty);
        ActivityState = WidgetActivityState.Unknown;
        LogCurrentState();
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task PeriodicUpdate()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        // Only update per the update interval.
        // This is intended to be dynamic in the future.
        if (DateTime.Now - lastUpdateRequest < WidgetRefreshRate)
        {
            return;
        }

        try
        {
            RequestContentData();
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(Name, ShortId, "Failed Requesting Update", ex);
        }

        lastUpdateRequest = DateTime.Now;
    }
}
