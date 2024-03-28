// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json.Nodes;
using GitHubExtension.DataManager;
using GitHubExtension.DeveloperId;
using GitHubExtension.Helpers;
using GitHubExtension.Widgets.Enums;
using Microsoft.Windows.DevHome.SDK;
using Microsoft.Windows.Widgets.Providers;

namespace GitHubExtension.Widgets;

public abstract class GitHubWidget : WidgetImpl
{
    protected static readonly TimeSpan WidgetDataRequestMinTime = TimeSpan.FromSeconds(30);
    protected static readonly TimeSpan WidgetRefreshRate = TimeSpan.FromMinutes(5);
    protected static readonly string EmptyJson = new JsonObject().ToJsonString();

    private DateTime lastUpdateRequest = DateTime.MinValue;

    protected static readonly string Name = nameof(GitHubWidget);

    protected WidgetActivityState ActivityState { get; set; } = WidgetActivityState.Unknown;

    protected WidgetDataState DataState { get; set; } = WidgetDataState.Unknown;

    protected WidgetPageState Page { get; set; } = WidgetPageState.Unknown;

    protected string ContentData { get; set; } = EmptyJson;

    protected bool Enabled { get; set; }

    protected bool Saved { get; set; }

    protected Dictionary<WidgetPageState, string> Template { get; set; } = new();

    protected string ConfigurationData
    {
        get => State();

        set => SetState(value);
    }

    protected string SavedConfigurationData { get; set; } = string.Empty;

    protected DateTime LastUpdated { get; set; } = DateTime.MinValue;

    protected DataUpdater DataUpdater { get; set; }

    public GitHubWidget()
    {
        DataUpdater = new DataUpdater(PeriodicUpdate);
        DeveloperIdProvider.GetInstance().Changed += HandleDeveloperIdChange;
    }

    ~GitHubWidget()
    {
        DataUpdater?.Dispose();
        DeveloperIdProvider.GetInstance().Changed -= HandleDeveloperIdChange;
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
        ConfigurationData = state;

        // If there is a state, it is being retrieved from the widget service, so
        // this widget was pinned before.
        if (state.Length != 0)
        {
            ResetWidgetInfoFromState();
            Saved = true;
        }

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

        switch (verb)
        {
            case WidgetAction.SignIn:
                _ = HandleSignIn();
                break;

            case WidgetAction.Save:
                // Set loading page while we swap out the data.
                Page = WidgetPageState.Loading;

                // It might take some time to get the new data, so
                // set data state to "unknown" so that loading page is shown.
                DataState = WidgetDataState.Unknown;
                UpdateWidget();

                SavedConfigurationData = string.Empty;
                LoadContentData();

                // Reset the throttle time and force an immediate data update request.
                LastUpdated = DateTime.MinValue;
                RequestContentData();

                SetActive();
                Saved = true;
                break;

            case WidgetAction.Cancel:
                ConfigurationData = SavedConfigurationData;
                ResetWidgetInfoFromState();
                SetActive();
                Saved = true;
                break;

            case WidgetAction.Unknown:
                Log.Logger()?.ReportError(Name, ShortId, $"Unknown verb: {actionInvokedArgs.Verb}");
                break;
        }
    }

    protected abstract void ResetWidgetInfoFromState();

    public override void OnCustomizationRequested(WidgetCustomizationRequestedArgs customizationRequestedArgs)
    {
        SavedConfigurationData = ConfigurationData;
        Saved = false;
        SetConfigure();
    }

    private async Task HandleSignIn()
    {
        Log.Logger()?.ReportInfo($"WidgetAction invoked for user sign in");
        var authProvider = DeveloperIdProvider.GetInstance();
        await authProvider.LoginNewDeveloperIdAsync();
        UpdateActivityState();
        Log.Logger()?.ReportInfo($"User sign in successful from WidgetAction invocation");
    }

    protected WidgetAction GetWidgetActionForVerb(string verb)
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

    public string GetSignIn()
    {
        var signInData = new JsonObject
        {
            { "message", Resources.GetResource(@"Widget_Template/SignInRequired", Log.Logger()) },
        };

        return signInData.ToString();
    }

    public bool IsUserLoggedIn()
    {
        DeveloperIdProvider authProvider = DeveloperIdProvider.GetInstance();
        return authProvider.GetLoggedInDeveloperIds().DeveloperIds.Any();
    }

    public virtual void UpdateActivityState()
    {
        // State logic for the Widget:
        // Signed in -> Valid Repository Url -> Active / Inactive per widget host.
        if (!IsUserLoggedIn())
        {
            SetSignIn();
            return;
        }

        if (!Saved)
        {
            SetConfigure();
            return;
        }

        if (Enabled)
        {
            SetActive();
            return;
        }

        SetInactive();
    }

    public void UpdateWidget()
    {
        WidgetUpdateRequestOptions updateOptions = new(Id)
        {
            Data = GetData(Page),
            Template = GetTemplateForPage(Page),
            CustomState = ConfigurationData,
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
        if (Template.TryGetValue(page, out var pageTemplate))
        {
            Log.Logger()?.ReportDebug(Name, ShortId, $"Using cached template for {page}");
            return pageTemplate;
        }

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, GetTemplatePath(page));
            var template = File.ReadAllText(path, Encoding.Default) ?? throw new FileNotFoundException(path);
            template = Resources.ReplaceIdentifiers(template, Resources.GetWidgetResourceIdentifiers(), Log.Logger());
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
        return $"State: {ActivityState}  Page: {Page}  Data: {DataState}";
    }

    protected void LogCurrentState()
    {
        Log.Logger()?.ReportDebug(Name, ShortId, GetCurrentState());
    }

    protected void SetActive()
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

    protected void SetLoading()
    {
        ActivityState = WidgetActivityState.Active;
        Page = WidgetPageState.Loading;

        _ = DataUpdater.Start();
        LogCurrentState();
        UpdateWidget();
    }

    protected void SetInactive()
    {
        ActivityState = WidgetActivityState.Inactive;
        DataUpdater.Stop();

        // No need to update when we are inactive.
        LogCurrentState();
    }

    protected void SetConfigure()
    {
        // If moving to configure, reset the throttle so when we update to Active, the first update
        // will not get throttled.
        DataUpdater.Stop();
        lastUpdateRequest = DateTime.MinValue;
        ActivityState = WidgetActivityState.Configure;
        Page = WidgetPageState.Configure;
        LogCurrentState();
        UpdateWidget();
    }

    protected void SetSignIn()
    {
        Page = WidgetPageState.SignIn;
        ActivityState = WidgetActivityState.SignIn;
        LogCurrentState();
        UpdateWidget();
    }

    private void SetDeleted()
    {
        // If this widget is deleted, disable the data updater, clear the state, set to Unknown.
        DeveloperIdProvider.GetInstance().Changed -= HandleDeveloperIdChange;
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

        if (ActivityState == WidgetActivityState.Configure)
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

    private void HandleDeveloperIdChange(object? sender, IDeveloperId e)
    {
        Log.Logger()?.ReportInfo(Name, ShortId, $"Change in Developer Id,  Updating widget state.");
        UpdateActivityState();
    }
}
