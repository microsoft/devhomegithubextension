﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubExtension.DataModel;

public class GitHubDataStoreSchema : IDataStoreSchema
{
    public long SchemaVersion => SchemaVersionValue;

    public List<string> SchemaSqls => _schemaSqlsValue;

    public GitHubDataStoreSchema()
    {
    }

    // Update this anytime incompatible changes happen with a released version.
    private const long SchemaVersionValue = 0x0008;

    private const string MetaData =
    @"CREATE TABLE MetaData (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "Key TEXT NOT NULL COLLATE NOCASE," +
        "Value TEXT NULL COLLATE NOCASE" +
    ");" +
    "CREATE UNIQUE INDEX IDX_MetaData_Key ON MetaData (Key);";

    private const string User =
    @"CREATE TABLE User (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "Login TEXT NOT NULL COLLATE NOCASE," +
        "InternalId INTEGER NOT NULL," +
        "AvatarUrl TEXT NULL COLLATE NOCASE," +
        "TimeUpdated INTEGER NOT NULL," +
        "Type TEXT NULL COLLATE NOCASE" +
    ");" +
    "CREATE UNIQUE INDEX IDX_User_InternalId ON User (InternalId);";

    private const string Repository =
    @"CREATE TABLE Repository (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "OwnerId INTEGER NOT NULL," +
        "Name TEXT NOT NULL COLLATE NOCASE," +
        "InternalId INTEGER NOT NULL," +
        "Description TEXT NOT NULL COLLATE NOCASE," +
        "Private INTEGER NOT NULL," +
        "HtmlUrl TEXT NULL COLLATE NOCASE," +
        "CloneUrl TEXT NULL COLLATE NOCASE," +
        "Fork INTEGER NOT NULL," +
        "DefaultBranch TEXT NULL COLLATE NOCASE," +
        "Visibility TEXT NULL COLLATE NOCASE," +
        "HasIssues INTEGER NOT NULL," +
        "TimeUpdated INTEGER NOT NULL," +
        "TimePushed INTEGER NOT NULL" +
    ");" +
    "CREATE UNIQUE INDEX IDX_Repository_OwnerIdName ON Repository (OwnerId, Name);" +
    "CREATE UNIQUE INDEX IDX_Repository_InternalId ON Repository (InternalId);";

    private const string Label =
    @"CREATE TABLE Label (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "InternalId INTEGER NOT NULL," +
        "IsDefault INTEGER NOT NULL," +
        "Name TEXT NOT NULL COLLATE NOCASE," +
        "Description TEXT NOT NULL COLLATE NOCASE," +
        "TimeUpdated INTEGER NOT NULL," +
        "Color TEXT NOT NULL COLLATE NOCASE" +
    ");" +
    "CREATE UNIQUE INDEX IDX_Label_InternalId ON Label (InternalId);";

    private const string Issue =
    @"CREATE TABLE Issue (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "InternalId INTEGER NOT NULL," +
        "Number INTEGER NOT NULL," +
        "RepositoryId INTEGER NOT NULL," +
        "State TEXT NOT NULL COLLATE NOCASE," +
        "Title TEXT NOT NULL COLLATE NOCASE," +
        "Body TEXT NOT NULL COLLATE NOCASE," +
        "AuthorId INTEGER NOT NULL," +
        "TimeCreated INTEGER NOT NULL," +
        "TimeUpdated INTEGER NOT NULL," +
        "TimeClosed INTEGER NOT NULL," +
        "TimeLastObserved INTEGER NOT NULL," +
        "HtmlUrl TEXT NULL COLLATE NOCASE," +
        "Locked INTEGER NOT NULL," +
        "AssigneeIds TEXT NULL COLLATE NOCASE," +
        "LabelIds TEXT NULL COLLATE NOCASE" +
    ");" +
    "CREATE UNIQUE INDEX IDX_Issue_InternalId ON Issue (InternalId);";

    private const string IssueLabel =
    @"CREATE TABLE IssueLabel (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "Issue INTEGER NOT NULL," +
        "Label INTEGER NOT NULL" +
    ");" +
    "CREATE UNIQUE INDEX IDX_IssueLabel_IssueLabel ON IssueLabel (Issue,Label);";

    private const string IssueAssign =
    @"CREATE TABLE IssueAssign (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "Issue INTEGER NOT NULL," +
        "User INTEGER NOT NULL" +
    ");" +
    "CREATE UNIQUE INDEX IDX_IssueAssign_IssueUser ON IssueAssign (Issue,User);";

    private const string PullRequest =
    @"CREATE TABLE PullRequest (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "InternalId INTEGER NOT NULL," +
        "Number INTEGER NOT NULL," +
        "RepositoryId INTEGER NOT NULL," +
        "State TEXT NOT NULL COLLATE NOCASE," +
        "Title TEXT NOT NULL COLLATE NOCASE," +
        "Body TEXT NOT NULL COLLATE NOCASE," +
        "AuthorId INTEGER NOT NULL," +
        "TimeCreated INTEGER NOT NULL," +
        "TimeUpdated INTEGER NOT NULL," +
        "TimeMerged INTEGER NOT NULL," +
        "TimeClosed INTEGER NOT NULL," +
        "TimeLastObserved INTEGER NOT NULL," +
        "HtmlUrl TEXT NULL COLLATE NOCASE," +
        "Locked INTEGER NOT NULL," +
        "Draft INTEGER NOT NULL," +
        "HeadSha TEXT NULL COLLATE NOCASE," +
        "Merged INTEGER NOT NULL," +
        "Mergeable INTEGER NOT NULL," +
        "MergeableState TEXT NULL COLLATE NOCASE," +
        "CommitCount INTEGER NOT NULL," +
        "AssigneeIds TEXT NULL COLLATE NOCASE," +
        "LabelIds TEXT NULL COLLATE NOCASE" +
    ");" +
    "CREATE UNIQUE INDEX IDX_PullRequest_InternalId ON PullRequest (InternalId);";

    private const string PullRequestAssign =
    @"CREATE TABLE PullRequestAssign (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "PullRequest INTEGER NOT NULL," +
        "User INTEGER NOT NULL" +
    ");" +
    "CREATE UNIQUE INDEX IDX_PullRequestAssign_PullRequestUser ON PullRequestAssign (PullRequest,User);";

    private const string PullRequestLabel =
    @"CREATE TABLE PullRequestLabel (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "PullRequest INTEGER NOT NULL," +
        "Label INTEGER NOT NULL" +
    ");" +
    "CREATE UNIQUE INDEX IDX_PullRequestLabel_PullRequestLabel ON PullRequestLabel (PullRequest,Label);";

    private const string CheckRun =
    @"CREATE TABLE CheckRun (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "InternalId INTEGER NOT NULL," +
        "HeadSha TEXT NULL COLLATE NOCASE," +
        "Name TEXT NOT NULL COLLATE NOCASE," +
        "ConclusionId INTEGER NOT NULL," +
        "StatusId INTEGER NOT NULL," +
        "Result TEXT NOT NULL COLLATE NOCASE," +
        "DetailsUrl TEXT NULL COLLATE NOCASE," +
        "HtmlUrl TEXT NULL COLLATE NOCASE," +
        "TimeStarted INTEGER NOT NULL," +
        "TimeCompleted INTEGER NOT NULL" +
    ");" +
    "CREATE UNIQUE INDEX IDX_CheckRun_InternalId ON CheckRun (InternalId);";

    private const string CheckSuite =
    @"CREATE TABLE CheckSuite (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "InternalId INTEGER NOT NULL," +
        "HeadSha TEXT NULL COLLATE NOCASE," +
        "Name TEXT NOT NULL COLLATE NOCASE," +
        "ConclusionId INTEGER NOT NULL," +
        "StatusId INTEGER NOT NULL," +
        "HtmlUrl TEXT NULL COLLATE NOCASE" +
    ");" +
    "CREATE UNIQUE INDEX IDX_CheckSuite_InternalId ON CheckSuite (InternalId);";

    private const string CommitCombinedStatus =
    @"CREATE TABLE CommitCombinedStatus (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "StateId INTEGER NOT NULL," +
        "HeadSha TEXT NULL COLLATE NOCASE" +
    ");" +
    "CREATE UNIQUE INDEX IDX_CommitCombinedStatus_HeadSha ON CommitCombinedStatus (HeadSha);";

    private const string PullRequestStatus =
    @"CREATE TABLE PullRequestStatus (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "PullRequestId INTEGER NOT NULL," +
        "HeadSha TEXT NULL COLLATE NOCASE," +
        "ConclusionId INTEGER NOT NULL," +
        "StatusId INTEGER NOT NULL," +
        "StateId INTEGER NOT NULL," +
        "Result TEXT NOT NULL COLLATE NOCASE," +
        "DetailsUrl TEXT NULL COLLATE NOCASE," +
        "HtmlUrl TEXT NULL COLLATE NOCASE," +
        "TimeOccurred INTEGER NOT NULL," +
        "TimeCreated INTEGER NOT NULL" +
    ");";

    private const string Notification =
    @"CREATE TABLE Notification (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "TypeId INTEGER NOT NULL," +
        "UserId INTEGER NOT NULL," +
        "RepositoryId INTEGER NOT NULL," +
        "Title TEXT NOT NULL COLLATE NOCASE," +
        "Description TEXT NOT NULL COLLATE NOCASE," +
        "Identifier TEXT NULL COLLATE NOCASE," +
        "Result TEXT NULL COLLATE NOCASE," +
        "HtmlUrl TEXT NULL COLLATE NOCASE," +
        "DetailsUrl TEXT NULL COLLATE NOCASE," +
        "ToastState INTEGER NOT NULL," +
        "TimeOccurred INTEGER NOT NULL," +
        "TimeCreated INTEGER NOT NULL" +
    ");";

    private const string Search =
    @"CREATE TABLE Search (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "RepositoryId INTEGER NOT NULL," +
        "TimeUpdated INTEGER NOT NULL," +
        "Query TEXT NOT NULL COLLATE NOCASE" +
    ");" +
    "CREATE UNIQUE INDEX IDX_Search_RepositoryIdQuery ON Search (RepositoryId, Query);";

    private const string SearchIssue =
    @"CREATE TABLE SearchIssue (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "TimeUpdated INTEGER NOT NULL," +
        "Search INTEGER NOT NULL," +
        "Issue INTEGER NOT NULL" +
    ");" +
    "CREATE UNIQUE INDEX IDX_SearchIssue_SearchIssue ON SearchIssue (Search, Issue);";

    private const string Review =
    @"CREATE TABLE Review (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "InternalId INTEGER NOT NULL," +
        "PullRequestId INTEGER NOT NULL," +
        "AuthorId INTEGER NOT NULL," +
        "Body TEXT NOT NULL COLLATE NOCASE," +
        "State TEXT NOT NULL COLLATE NOCASE," +
        "HtmlUrl TEXT NULL COLLATE NOCASE," +
        "TimeSubmitted INTEGER NOT NULL," +
        "TimeLastObserved INTEGER NOT NULL" +
    ");" +
    "CREATE UNIQUE INDEX IDX_Review_InternalId ON Review (InternalId);";

    private const string Release =
    @"CREATE TABLE Release (" +
        "Id INTEGER PRIMARY KEY NOT NULL," +
        "InternalId INTEGER NOT NULL," +
        "RepositoryId INTEGER NOT NULL," +
        "Name TEXT NOT NULL COLLATE NOCASE," +
        "TagName TEXT NOT NULL COLLATE NOCASE," +
        "Prerelease INTEGER NOT NULL," +
        "HtmlUrl TEXT NULL COLLATE NOCASE," +
        "TimeCreated INTEGER NOT NULL," +
        "TimePublished INTEGER NOT NULL," +
        "TimeLastObserved INTEGER NOT NULL" +
    ");" +
    "CREATE UNIQUE INDEX IDX_Release_InternalId ON Release (InternalId);";

    // All Sqls together.
    private static readonly List<string> _schemaSqlsValue = new()
    {
        MetaData,
        User,
        Repository,
        Label,
        Issue,
        IssueLabel,
        IssueAssign,
        PullRequest,
        PullRequestAssign,
        PullRequestLabel,
        PullRequestStatus,
        CheckRun,
        CheckSuite,
        CommitCombinedStatus,
        Notification,
        Search,
        SearchIssue,
        Review,
        Release,
    };
}
