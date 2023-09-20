// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace GitHubPlugin.DataModel;

public interface IDataStoreSchema
{
    public long SchemaVersion
    {
        get;
    }

    public List<string> SchemaSqls
    {
        get;
    }
}
