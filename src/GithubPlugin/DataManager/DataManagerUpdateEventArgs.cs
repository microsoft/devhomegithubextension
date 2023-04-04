// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace GitHubPlugin.DataManager;

public delegate void DataManagerUpdateEventHandler(object? source, DataManagerUpdateEventArgs e);

public enum DataManagerUpdateKind
{
    Repository,     // Single repository was updated.
    Developer,      // Developer content was updated, a thin slice of the data across multiple repositories.
}

public class DataManagerUpdateEventArgs : EventArgs
{
    private readonly string _repository;
    private readonly string[] _context;
    private readonly DataManagerUpdateKind _kind;

    public DataManagerUpdateEventArgs(DataManagerUpdateKind updateKind, string updateRepository, string[] updateContext)
    {
        _kind = updateKind;
        _repository = updateRepository;
        _context = updateContext;
    }

    public DataManagerUpdateKind Kind => _kind;

    public string Repository => _repository;

    public string[] Context => _context;
}
