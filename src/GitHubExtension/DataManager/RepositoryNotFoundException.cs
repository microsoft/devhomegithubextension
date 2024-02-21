// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubExtension.DataManager;

public class RepositoryNotFoundException : ApplicationException
{
    public RepositoryNotFoundException()
    {
    }

    public RepositoryNotFoundException(string message)
        : base(message)
    {
    }
}
