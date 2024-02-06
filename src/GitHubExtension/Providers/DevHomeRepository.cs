// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.Client;

namespace GitHubExtension.Providers;

// Microsoft.Windows.DevHome.SDK.IRepository Implementation
public class DevHomeRepository : Microsoft.Windows.DevHome.SDK.IRepository
{
    private readonly string name;

    private readonly Uri cloneUrl;

    private readonly bool _isPrivate;

    private readonly DateTimeOffset _lastUpdated;

    string Microsoft.Windows.DevHome.SDK.IRepository.DisplayName => name;

    public string OwningAccountName => Validation.ParseOwnerFromGitHubURL(this.cloneUrl);

    public bool IsPrivate => _isPrivate;

    public DateTimeOffset LastUpdated => _lastUpdated;

    public Uri RepoUri => cloneUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="DevHomeRepository"/> class.
    /// </summary>
    /// <param name="ocktokitRepository">The repository received from ocktokit</param>
    public DevHomeRepository(Octokit.Repository ocktokitRepository)
    {
        this.name = ocktokitRepository.Name;
        this.cloneUrl = new Uri(ocktokitRepository.CloneUrl);

        _lastUpdated = ocktokitRepository.UpdatedAt;
        _isPrivate = ocktokitRepository.Private;
    }
}
