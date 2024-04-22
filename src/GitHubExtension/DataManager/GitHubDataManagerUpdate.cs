// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.DataManager;
using GitHubExtension.DataModel;

namespace GitHubExtension;

public partial class GitHubDataManager
{
    // This is how frequently the DataStore update occurs.
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromMinutes(5);
    private static DateTime lastUpdateTime = DateTime.MinValue;

    public static async Task Update()
    {
        // Only update per the update interval.
        // This is intended to be dynamic in the future.
        if (DateTime.Now - lastUpdateTime < UpdateInterval)
        {
            return;
        }

        try
        {
            await UpdateDeveloperPullRequests();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Update failed unexpectedly.");
        }

        lastUpdateTime = DateTime.Now;
    }

    private static async Task UpdateDeveloperPullRequests()
    {
        Log.Debug($"Executing UpdateDeveloperPullRequests");
        using var dataManager = CreateInstance() ?? throw new DataStoreInaccessibleException("GitHubDataManager is null.");
        await dataManager.UpdatePullRequestsForLoggedInDeveloperIdsAsync();

        // Show any new notifications that were created from the pull request update.
        var notifications = dataManager.GetNotifications();
        foreach (var notification in notifications)
        {
            // Show notifications for failed checkruns for Developer users.
            if (notification.Type == NotificationType.CheckRunFailed && notification.User.IsDeveloper)
            {
                notification.ShowToast();
            }

            // Show notifications for new reviews.
            if (notification.Type == NotificationType.NewReview)
            {
                notification.ShowToast();
            }
        }
    }

    private static void SendDeveloperUpdateEvent(object? source)
    {
        SendUpdateEvent(source, DataManagerUpdateKind.Developer, null, null);
    }

    private static void SendRepositoryUpdateEvent(object? source, string fullName, string[] context)
    {
        SendUpdateEvent(source, DataManagerUpdateKind.Repository, fullName, context);
    }

    private static void SendUpdateEvent(object? source, DataManagerUpdateKind kind, string? info = null, string[]? context = null)
    {
        if (OnUpdate != null)
        {
            info ??= string.Empty;
            context ??= Array.Empty<string>();
            Log.Information($"Sending Update Event: {kind}  Info: {info}  Context: {string.Join(",", context)}");
            OnUpdate.Invoke(source, new DataManagerUpdateEventArgs(kind, info, context));
        }
    }
}
