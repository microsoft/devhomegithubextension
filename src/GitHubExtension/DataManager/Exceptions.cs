// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace GitHubExtension;
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

public class DataStoreInaccessibleException : ApplicationException
{
    public DataStoreInaccessibleException()
    {
    }

    public DataStoreInaccessibleException(string message)
        : base(message)
    {
    }
}
