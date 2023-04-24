// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Runtime.InteropServices;
using GitHubPlugin.Client;
using GitHubPlugin.DeveloperId;
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

            var accessDenied = false;
            var cantCloneAllSubmodules = false;
            var didUserStopCloning = false;
            var clonedRepo = false;
            var isPathNotEmpty = false;

            var cloningExceptions = new List<Exception>();
            if (internalDeveloperIdsToUse.Any())
            {
                foreach (var internalDeveloperId in internalDeveloperIdsToUse)
                {
                    cloneOptions.CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials
                    {
                        // Password is a PAT unique to github.
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
                        Log.Logger()?.ReportError("DevHomeRepository", "Could not clone all sub modules", recurseException);
                        cloningExceptions.Add(recurseException);
                        cantCloneAllSubmodules = true;
                    }
                    catch (UserCancelledException userCancelledException)
                    {
                        Log.Logger()?.ReportError("DevHomeRepository", "The user stoped the clone operation", userCancelledException);
                        cloningExceptions.Add(userCancelledException);
                        didUserStopCloning = true;
                    }
                    catch (NameConflictException nameConflictException)
                    {
                        Log.Logger()?.ReportError("DevHomeRepository", nameConflictException);
                        cloningExceptions.Add(nameConflictException);
                        isPathNotEmpty = true;
                    }
                    catch (LibGit2SharpException libGitTwoException)
                    {
                        Log.Logger()?.ReportError("DevHomeRepository", $"Either no logged in account has access to this repo, or the repo can't be found", libGitTwoException);
                        cloningExceptions.Add(libGitTwoException);
                        accessDenied = true;
                    }
                    catch (Exception e)
                    {
                        Log.Logger()?.ReportError("DevHomeRepository", "Could not clone the repository", e);
                        cloningExceptions.Add(e);
                    }
                }
            }
            else
            {
                try
                {
                    Repository.Clone(cloneUrl.OriginalString, cloneDestination, cloneOptions);
                    clonedRepo = true;
                }
                catch (RecurseSubmodulesException recurseException)
                {
                    Log.Logger()?.ReportError("DevHomeRepository", "Could not clone all sub modules", recurseException);
                    cloningExceptions.Add(recurseException);
                    cantCloneAllSubmodules = true;
                }
                catch (UserCancelledException userCancelledException)
                {
                    Log.Logger()?.ReportError("DevHomeRepository", "The user stoped the clone operation", userCancelledException);
                    cloningExceptions.Add(userCancelledException);
                    didUserStopCloning = true;
                }
                catch (NameConflictException nameConflictException)
                {
                    Log.Logger()?.ReportError("DevHomeRepository", nameConflictException);
                    cloningExceptions.Add(nameConflictException);
                    isPathNotEmpty = true;
                }
                catch (LibGit2SharpException libGitTwoException)
                {
                    Log.Logger()?.ReportError("DevHomeRepository", $"Either no logged in account has access to this repo, or the repo can't be found", libGitTwoException);
                    cloningExceptions.Add(libGitTwoException);
                    accessDenied = true;
                }
                catch (Exception e)
                {
                    Log.Logger()?.ReportError("DevHomeRepository", "Could not clone the repository", e);
                    cloningExceptions.Add(e);
                }
            }

            // Because all devIds are looped over multiple exceptions can happen.
            // This is a rough generalization of the exceptions so they can be communicated to dev home.
            // WIll need to think of a better way of communication exceptions later.
            if (!clonedRepo)
            {
                if (isPathNotEmpty)
                {
                    throw new DirectoryNotFoundException("This is my message");
                }
                else if (accessDenied)
                {
                    throw new UnauthorizedAccessException("This is my message");
                }
                else if (cantCloneAllSubmodules)
                {
                    throw new FileNotFoundException("This is my message");
                }
                else if (didUserStopCloning)
                {
                    throw new OperationCanceledException("This is my message");
                }
                else
                {
                    // If the repo was not cloned and none of the known exceptions were thrown
                    throw new InvalidOperationException("This is my message");
                }
            }
        }).AsAsyncAction();
    }
}
