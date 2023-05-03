// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubPlugin.DataManager;
using GitHubPlugin.DeveloperId;
using Microsoft.Windows.DevHome.SDK;

namespace GitHubPlugin;
public class DataStoreOperationParameters
{
    // parameters for updating the data store.
    public string? Owner { get; set; }

    public string? RepositoryName { get; set; }

    public OperationName OperationName { get; set; } = OperationName.Unknown;

    public string? Query { get; set; }

    public IEnumerable<IDeveloperId> DeveloperIds { get; set; } = Enumerable.Empty<IDeveloperId>();

    public RequestOptions? RequestOptions { get; set; }

    public DataStoreOperationParameters()
    {
    }

    public override string ToString()
    {
        return $"{OperationName}  {Owner}/{RepositoryName} - {RequestOptions}";
    }
}
