﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Dapper;
using Dapper.Contrib.Extensions;
using GitHubPlugin.DeveloperId;
using GitHubPlugin.Helpers;

namespace GitHubPlugin.DataModel;

[Table("User")]
public class User
{
    // This is the time between seeing a potential updated user record and updating it.
    // This value / 2 is the average time between user updating their user data and having
    // it reflected in the datastore.
    private static readonly long UpdateThreshold = TimeSpan.FromHours(4).Ticks;

    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    public string Login { get; set; } = string.Empty;

    public long InternalId { get; set; } = DataStore.NoForeignKey;

    public string AvatarUrl { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public long TimeUpdated { get; set; } = DataStore.NoForeignKey;

    [Write(false)]
    private DataStore? DataStore { get; set; }

    [Write(false)]
    [Computed]
    public bool IsDeveloper => IsLoginIdDeveloper(Login);

    [Write(false)]
    [Computed]
    public IEnumerable<PullRequest> PullRequests
    {
        get
        {
            if (DataStore == null)
            {
                return Enumerable.Empty<PullRequest>();
            }
            else
            {
                return PullRequest.GetAllForUser(DataStore, this) ?? Enumerable.Empty<PullRequest>();
            }
        }
    }

    public override string ToString() => Login;

    private static User CreateFromOctokitUser(Octokit.User user)
    {
        return new User
        {
            InternalId = user.Id,
            Login = user.Login,
            AvatarUrl = user.AvatarUrl ?? string.Empty,
            Type = user.Type.HasValue ? user.Type.Value.ToString() : string.Empty,
            TimeUpdated = DateTime.Now.ToDataStoreInteger(),
        };
    }

    public static User AddOrUpdateUser(DataStore dataStore, User user)
    {
        // Check for existing user data.
        var existingUser = GetByInternalId(dataStore, user.InternalId);
        if (existingUser is not null)
        {
            // Many of the same user records will be created on a sync, and to
            // avoid unnecessary updating and database operations for data that
            // is extremely unlikely to have changed in any significant way, we
            // will only update every UpdateThreshold amount of time.
            if ((user.TimeUpdated - existingUser.TimeUpdated) > UpdateThreshold)
            {
                user.Id = existingUser.Id;
                dataStore.Connection!.Update(user);
                user.DataStore = dataStore;
                return user;
            }
            else
            {
                return existingUser;
            }
        }

        // No existing pull request, add it.
        user.Id = dataStore.Connection!.Insert(user);
        user.DataStore = dataStore;
        return user;
    }

    public static User? GetById(DataStore dataStore, long id)
    {
        var user = dataStore.Connection!.Get<User>(id);
        if (user != null)
        {
            user.DataStore = dataStore;
        }

        return user;
    }

    public static User? GetByInternalId(DataStore dataStore, long internalId)
    {
        var sql = @"SELECT * FROM User WHERE InternalId = @InternalId;";
        var param = new
        {
            InternalId = internalId,
        };

        var user = dataStore.Connection!.QueryFirstOrDefault<User>(sql, param, null);
        if (user != null)
        {
            user.DataStore = dataStore;
        }

        return user;
    }

    public static User GetOrCreateByOctokitUser(DataStore dataStore, Octokit.User user)
    {
        var newUser = CreateFromOctokitUser(user);
        return AddOrUpdateUser(dataStore, newUser);
    }

    public static List<string> GetDeveloperLoginIds()
    {
        var idList = new List<string>();
        var devIds = DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIdsInternal();
        foreach (var devId in devIds)
        {
            idList.Add(devId.LoginId);
        }

        return idList;
    }

    // Returns list of User records that match the Logged-in Developer IDs.
    public static IEnumerable<User> GetDeveloperUsers(DataStore dataStore)
    {
        var sql = @"SELECT * FROM User WHERE Login IN @DeveloperIds;";
        var param = new
        {
            DeveloperIds = GetDeveloperLoginIds(),
        };

        Log.Logger()?.ReportDebug(DataStore.GetSqlLogMessage(sql, param));
        var users = dataStore.Connection!.Query<User>(sql, param) ?? Enumerable.Empty<User>();
        foreach (var user in users)
        {
            user.DataStore = dataStore;
        }

        return users;
    }

    private static bool IsLoginIdDeveloper(string login)
    {
        return GetDeveloperLoginIds().Contains(login);
    }
}
