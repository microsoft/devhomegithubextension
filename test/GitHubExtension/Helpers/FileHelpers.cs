// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubExtension.Test;

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
