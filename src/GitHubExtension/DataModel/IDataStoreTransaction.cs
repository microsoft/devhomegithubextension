// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubExtension.DataModel;

public interface IDataStoreTransaction : IDisposable
{
    void Commit();

    void Rollback();
}
