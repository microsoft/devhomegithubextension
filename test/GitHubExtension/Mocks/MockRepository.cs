// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace GitHubExtension.Test.Mocks;

public class MockRepository : IRepository
{
    public string DisplayName => "Mock Repository";

    public bool IsPrivate => false;

    public DateTimeOffset LastUpdated => new DateTime(2023, 04, 11);

    public string OwningAccountName => "Local Microsoft";

    public Uri RepoUri => throw new NotImplementedException();

    public IAsyncAction CloneRepositoryAsync(string cloneDestination, IDeveloperId developerId)
    {
        throw new NotImplementedException();
    }

    public IAsyncAction CloneRepositoryAsync(string cloneDestination)
    {
        throw new NotImplementedException();
    }
}
