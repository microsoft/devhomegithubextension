// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.
using GitHubPlugin.DataModel;

namespace GitHubPlugin.Client;

// Validation layer to help parsing github url.
public static class Validation
{
    private static bool IsValidHttpUri(string uriString, out Uri? uri)
    {
        return Uri.TryCreate(uriString, UriKind.Absolute, out uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    public static bool IsValidGitHubURL(Uri uri)
    {
        // Valid github Uri has three segments.  The first is /
        if (uri.Segments.Length < 3 || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            Log.Logger()?.ReportDebug($"{uri.OriginalString} is not a valid github uri");
            return false;
        }

        return true;
    }

    // Ensure it is a GitHub repo url.
    public static bool IsValidGitHubURL(string url)
    {
        Uri? parsedUri;

        // https://github.com/dotnet/runtime/issues/72632
        // IsWellFormedUriString returnes false with a github url.
        // Above link shows a work around.
        if (!IsValidHttpUri(url, out parsedUri) || url == null || parsedUri == null)
        {
            Log.Logger()?.ReportDebug($"{url} is not a valid http uri");
            return false;
        }

        return IsValidGitHubURL(parsedUri);
    }

    public static Uri GetUriFromGitHubUrlString(string url)
    {
        if (!IsValidGitHubURL(url))
        {
            // Try adding a protocol to support just "github.com/owner/repo" type inputs.
            var urlWithProtocol = AddProtocolToString(url);
            if (!IsValidGitHubURL(urlWithProtocol))
            {
                throw new InvalidGitHubUrlException($"{url} is invalid.");
            }

            url = urlWithProtocol;
        }

        return new Uri(url);
    }

    public static string ParseOwnerFromGitHubURL(string url)
    {
        // Check if url string provided as just the repository FullName.
        var fullNameSplit = GetNameAndRepoFromFullName(url);
        if (fullNameSplit is not null)
        {
            return fullNameSplit[0];
        }

        return ParseOwnerFromGitHubURL(GetUriFromGitHubUrlString(url));
    }

    public static string ParseOwnerFromGitHubURL(Uri url)
    {
        // For some reason Segments is returning trailing '/', even though the documentation
        // remarks state that it strips out the separator. This is a fix for that which will
        // work even if/when that issue is fixed.
        return url.Segments[1].Replace("/", string.Empty);
    }

    public static string ParseRepositoryFromGitHubURL(string url)
    {
        // Check if url string provided as just the repository FullName.
        var fullNameSplit = GetNameAndRepoFromFullName(url);
        if (fullNameSplit is not null)
        {
            return fullNameSplit[1];
        }

        return ParseRepositoryFromGitHubURL(GetUriFromGitHubUrlString(url));
    }

    public static string ParseRepositoryFromGitHubURL(Uri url)
    {
        // Replace .git because Ocktokit does not want .git.
        var repoName = url.Segments[2].Replace("/", string.Empty);

        if (repoName.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            var locationOfLastDotGit = repoName.LastIndexOf(".git", StringComparison.OrdinalIgnoreCase);
            repoName = repoName.Remove(locationOfLastDotGit);
        }

        return repoName;
    }

    public static string ParseFullNameFromGitHubURL(string url)
    {
        // Check if url string provided as just the repository FullName.
        var fullNameSplit = GetNameAndRepoFromFullName(url);
        if (fullNameSplit is not null)
        {
            return url;
        }

        return ParseFullNameFromGitHubURL(GetUriFromGitHubUrlString(url));
    }

    public static string ParseFullNameFromGitHubURL(Uri url)
    {
        // Need to account for the presence or absence of a trailing '/' in the segements, and
        // ensure there is exactly one slash separator in the full name.
        return $"{url.Segments[1].Replace("/", string.Empty)}/{url.Segments[2].Replace("/", string.Empty)}";
    }

    // Adds a protocol to a string to allow for protocol-less Uris.
    private static string AddProtocolToString(string s)
    {
        return "https://" + s;
    }

    private static string[]? GetNameAndRepoFromFullName(string s)
    {
        var n = s.Split(new[] { '/' });

        // This should be exactly two results with no empty strings.
        if (n.Length != 2 || string.IsNullOrEmpty(n[0]) || string.IsNullOrEmpty(n[1]))
        {
            return null;
        }

        return n;
    }
}
