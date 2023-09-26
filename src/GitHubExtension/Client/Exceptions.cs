// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace GitHubExtension.Client;
public class InvalidUrlException : Exception
{
    public InvalidUrlException()
    {
    }

    public InvalidUrlException(string message)
        : base(message)
    {
    }
}

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

public class InvalidApiException : Exception
{
    public InvalidApiException()
    {
    }

    public InvalidApiException(string message)
        : base(message)
    {
    }
}
