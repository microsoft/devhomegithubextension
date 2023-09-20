﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Dapper;
using Dapper.Contrib.Extensions;
using GitHubExtension.Helpers;

namespace GitHubExtension.DataModel;

[Table("Label")]
public class Label
{
    // This is the time between seeing a potential updated label record and updating it.
    // This value / 2 is the average time between label changing on GitHub and having
    // it reflected in the datastore.
    private static readonly long UpdateThreshold = TimeSpan.FromHours(4).Ticks;

    [Key]
    public long Id { get; set; } = DataStore.NoForeignKey;

    public long InternalId { get; set; } = DataStore.NoForeignKey;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public long IsDefault { get; set; } = DataStore.NoForeignKey;

    public string Color { get; set; } = string.Empty;

    public long TimeUpdated { get; set; } = DataStore.NoForeignKey;

    public override string ToString() => Name;

    private static Label CreateFromOctokitLabel(Octokit.Label label)
    {
        return new Label
        {
            InternalId = label.Id,
            Name = label.Name ?? string.Empty,
            Description = label.Description ?? string.Empty,
            IsDefault = label.Default ? 1 : 0,
            Color = label.Color is not null ? label.Color : string.Empty,
            TimeUpdated = DateTime.Now.ToDataStoreInteger(),
        };
    }

    private static Label AddOrUpdateLabel(DataStore dataStore, Label label)
    {
        // Check for existing label data.
        var existing = GetByInternalId(dataStore, label.InternalId);
        if (existing is not null)
        {
            // Many of the same label records will be created on a sync, and to
            // avoid unnecessary updating and database operations for data that
            // is extremely unlikely to have changed in any significant way, we
            // will only update every UpdateThreshold amount of time.
            if ((label.TimeUpdated - existing.TimeUpdated) > UpdateThreshold)
            {
                label.Id = existing.Id;
                dataStore.Connection!.Update(label);
                return label;
            }
            else
            {
                return existing;
            }
        }

        // No existing pull request, add it.
        label.Id = dataStore.Connection!.Insert(label);
        return label;
    }

    public static Label? GetById(DataStore dataStore, long id)
    {
        return dataStore.Connection!.Get<Label>(id);
    }

    public static Label? GetByInternalId(DataStore dataStore, long internalId)
    {
        var sql = @"SELECT * FROM Label WHERE InternalId = @InternalId;";
        var param = new
        {
            InternalId = internalId,
        };

        return dataStore.Connection!.QueryFirstOrDefault<Label>(sql, param, null);
    }

    public static Label GetOrCreateByOctokitLabel(DataStore dataStore, Octokit.Label label)
    {
        var newUser = CreateFromOctokitLabel(label);
        return AddOrUpdateLabel(dataStore, newUser);
    }
}
