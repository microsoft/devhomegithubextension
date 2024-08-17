// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.Client;

namespace GitHubExtension.Providers;

// Microsoft.Windows.DevHome.SDK.IRepository Implementation
public class DevHomeRepository : Microsoft.Windows.DevHome.SDK.IRepository
{
    private readonly string _name;

    private readonly Uri _cloneUrl;

    private readonly bool _isPrivate;

    private readonly DateTimeOffset _lastUpdated;

    string Microsoft.Windows.DevHome.SDK.IRepository.DisplayName => _name;

    public string OwningAccountName => Validation.ParseOwnerFromGitHubURL(_cloneUrl);

    public bool IsPrivate => _isPrivate;

    public DateTimeOffset LastUpdated => _lastUpdated;

    public Uri RepoUri => _cloneUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="DevHomeRepository"/> class.
    /// </summary>
    /// <param name="octokitRepository">The repository received from octokit</param>
    public DevHomeRepository(Octokit.Repository octokitRepository)
    {
        _name = octokitRepository.Name;
        _cloneUrl = new Uri(octokitRepository.CloneUrl);

        _lastUpdated = octokitRepository.UpdatedAt;
        _isPrivate = octokitRepository.Private;
    }
}
