// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubExtension.Client;

public class InvalidGitHubUrlException : Exception
{
    public InvalidGitHubUrlException()
    {
    }

    public InvalidGitHubUrlException(string message)
        : base(message)
    {
    }
}
