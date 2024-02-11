// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubExtension.Client;

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
