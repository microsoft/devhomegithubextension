// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitHubExtension.Client;
using Microsoft.Windows.DevHome.SDK;
using Octokit;

namespace GitHubExtension.Providers.Codespaces;

internal sealed class CodespaceHelper
{
    public static CodespacesCollection GetCodespacesForDeveloperId(IDeveloperId developerId)
    {
        GitHubClientProvider gitHubClientProvider = new GitHubClientProvider();
        GitHubClient? gitHubClient = gitHubClientProvider.GetClient(developerId);

        if (gitHubClient == null)
        {
            throw new InvalidOperationException();
        }

        /*
        CodespacesCollection codespaces;
        List<Octokit.Codespace> codespaceList = new List<Octokit.Codespace>();

        var test = gitHubClient.Codespaces.GetForRepository("Aaron-Junker", "windows-uwp");
        codespaceList.AddRange(test.GetAwaiter().GetResult().Codespaces.AsEnumerable());

        foreach (var repo in gitHubClient.Repository.GetAllForCurrent().GetAwaiter().GetResult())
        {
            try
            {
                codespaceList.AddRange(gitHubClient.Codespaces.GetForRepository(repo.Owner.Login, repo.Name).GetAwaiter().GetResult().Codespaces.AsEnumerable());
            }
            catch
            {
            }
        }

        codespaces = new CodespacesCollection(codespaceList, codespaceList.Count);*/

        return gitHubClient.Codespaces.GetAll().GetAwaiter().GetResult();
    }
}
