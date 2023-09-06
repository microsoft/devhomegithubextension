// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubExtension.Client;
using GitHubExtension.DeveloperId;
using LibGit2Sharp;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

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

    public Uri RepoUri => throw new NotImplementedException();

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
    /// Clones the repository.
    /// </summary>
    /// <param name="cloneDestination">The location to clone to</param>
    /// <param name="developerId">The account to use to clone repos.  If null plugin will iterate through all logged in accounts and try to clone
    /// with the credentials.  If developerId is null and no users are logged in the repo will be cloned without using credentials.</param>
    /// <returns>A action to await on</returns>
    /// <remarks>
    /// Cloning can throw.  Please catch any exceptions.
    /// </remarks>
    private IAsyncAction CloneRepositoryAsyncImpl(string cloneDestination, IDeveloperId? developerId = null)
    {
        return Task.Run(() =>
        {
            var cloneOptions = new CloneOptions
            {
                Checkout = true,
            };

            List<DeveloperId.DeveloperId> internalDeveloperIdsToUse = new ();

            if (developerId != null)
            {
                internalDeveloperIdsToUse.Add(DeveloperIdProvider.GetInstance().GetDeveloperIdInternal(developerId));
            }
            else
            {
                internalDeveloperIdsToUse.AddRange(DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIdsInternal());
            }

            var clonedRepo = false;

            if (internalDeveloperIdsToUse.Any())
            {
                foreach (var internalDeveloperId in internalDeveloperIdsToUse)
                {
                    cloneOptions.CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials
                    {
                        // Password is a PAT unique to GitHub.
                        Username = internalDeveloperId.GetCredential().Password,
                        Password = string.Empty,
                    };

                    try
                    {
                        // Exceptions happen.
                        Repository.Clone(cloneUrl.OriginalString, cloneDestination, cloneOptions);
                        clonedRepo = true;
                        break;
                    }
                    catch (RecurseSubmodulesException recurseException)
                    {
                        Providers.Log.Logger()?.ReportError("DevHomeRepository", "Could not clone all sub modules", recurseException);
                        continue;
                    }
                    catch (UserCancelledException userCancelledException)
                    {
                        Providers.Log.Logger()?.ReportError("DevHomeRepository", "The user stoped the clone operation", userCancelledException);
                        continue;
                    }
                    catch (NameConflictException nameConflictException)
                    {
                        Providers.Log.Logger()?.ReportError("DevHomeRepository", nameConflictException);
                        continue;
                    }
                    catch (LibGit2SharpException libGitTwoException)
                    {
                        Providers.Log.Logger()?.ReportError("DevHomeRepository", $"Either no logged in account has access to this repo, or the repo can't be found", libGitTwoException);
                        continue;
                    }
                    catch (Exception e)
                    {
                        Providers.Log.Logger()?.ReportError("DevHomeRepository", "Could not clone the repository", e);
                        continue;
                    }
                }
            }
            else
            {
                try
                {
                    LibGit2Sharp.Repository.Clone(cloneUrl.OriginalString, cloneDestination, cloneOptions);
                    clonedRepo = true;
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
                catch (LibGit2SharpException libGitTwoException)
                {
                    Providers.Log.Logger()?.ReportError("DevHomeRepository", $"Either no logged in account has access to this repo, or the repo can't be found", libGitTwoException);
                    throw;
                }
                catch (Exception e)
                {
                    Providers.Log.Logger()?.ReportError("DevHomeRepository", "Could not clone the repository", e);
                    throw;
                }
            }

            if (!clonedRepo)
            {
                Providers.Log.Logger()?.ReportError("DevHomeRepository", $"Either no logged in accounts could clone repo {name} or the repo could not be found");
                throw new LibGit2SharpException($"Either no logged in accounts could clone repo {name} or the repo could not be found");
            }
        }).AsAsyncAction();
    }
}
