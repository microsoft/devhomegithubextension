// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension;

internal class Program
{
    private static void Main(string[] args)
    {
        using var dataManager = GitHubDataManager.CreateInstance();
        Task.Run(async () =>
        {
            var parameters = new RequestOptions
            {
                SearchIssuesRequest = new Octokit.SearchIssuesRequest(args[0]),
            };
            await dataManager!.UpdateIssuesForRepositoryAsync("microsoft", "devhome", parameters);
        }).Wait();

        var repository = dataManager!.GetRepository("microsoft/devhome");
        if (repository == null)
        {
            Console.WriteLine("No results");
            return;
        }

        foreach (var issue in repository.GetIssuesForQuery(args[0]))
        {
            Console.WriteLine($"  Issue: {issue.Number} {issue.Author.Login} {issue.Title}");
        }
    }
}
