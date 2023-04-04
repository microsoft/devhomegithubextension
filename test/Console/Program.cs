// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubPlugin;
using GitHubPlugin.DataModel;

internal class Program
{
    private static void Main(string[] args)
    {
        using var dataManager = GitHubDataManager.CreateInstance();
        Task.Run(async () =>
        {
            await dataManager!.UpdatePullRequestsForRepositoryAsync(args[0]);
        }).Wait();

        // Show any new notifications that were created from the pull request update.
        var notifications = dataManager!.GetNotifications();
        foreach (var notification in notifications)
        {
            notification.ShowToast();
        }
    }
}
