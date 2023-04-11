// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubPlugin.Client;
using LibGit2Sharp;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace GitHubPlugin.Providers;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="DevHomeRepository"/> class.
    /// </summary>
    /// <param name="ocktokitRepository">The repository recived from ocktokit</param>
    public DevHomeRepository(Octokit.Repository ocktokitRepository)
    {
        this.name = ocktokitRepository.Name;
        this.cloneUrl = new Uri(ocktokitRepository.CloneUrl);

        _lastUpdated = ocktokitRepository.UpdatedAt;
        _isPrivate = ocktokitRepository.Private;
    }

    public IAsyncAction CloneRepositoryAsync(string cloneDestination, IDeveloperId developerId)
    {
        return CloneRepositoryAsyncImpl(cloneDestination, developerId);
    }

    public IAsyncAction CloneRepositoryAsync(string cloneDestination)
    {
        return CloneRepositoryAsyncImpl(cloneDestination);
    }

    /// <summary>
    /// Clones the repository
    /// </summary>
    /// <param name="cloneDestination">The location to clone to</param>
    /// <param name="developerId">The account to use to clone private repos</param>
    /// <returns>A action to await on</returns>
    /// <remarks>
    /// Cloning can throw.  Please catch any exceptions.
    /// </remarks>
    private IAsyncAction CloneRepositoryAsyncImpl(string cloneDestination, IDeveloperId? developerId = null)
    {
        return Task.Run(() =>
        {
            if (!string.IsNullOrEmpty(cloneDestination))
            {
                var cloneOptions = new CloneOptions
                {
                    Checkout = true,
                };

                try
                {
                    LibGit2Sharp.Repository.Clone(cloneUrl.OriginalString, cloneDestination, cloneOptions);
                }
                catch (RecurseSubmodulesException recurseException)
                {
                    Providers.Log.Logger()?.ReportError("DevHomeRepository", "Could not clone all sub modules", recurseException);
                    throw;
                }
                catch (UserCancelledException userCancelledException)
                {
                    Providers.Log.Logger()?.ReportError("DevHomeRepository", "The user stoped the clone operation", userCancelledException);
                    throw;
                }
                catch (NameConflictException nameConflictException)
                {
                    Providers.Log.Logger()?.ReportError("DevHomeRepository", nameConflictException);
                    throw;
                }
                catch (Exception e)
                {
                    Providers.Log.Logger()?.ReportError("DevHomeRepository", "Could not clone the repository", e);
                    throw;
                }
            }
        }).AsAsyncAction();
    }
}
