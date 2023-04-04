// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace GitHubPlugin.Test;

public partial class TestHelpers
{
    public static string CreateUniqueFolderName(string prefix)
    {
        // This could potentially be too long of a path name,
        // but should be OK for now. Keep the prefix short.
        return $"{prefix}-{Guid.NewGuid()}";
    }

    public static string GetUniqueFolderPath(string prefix)
    {
        return Path.Combine(Path.GetTempPath(), CreateUniqueFolderName(prefix));
    }
}
