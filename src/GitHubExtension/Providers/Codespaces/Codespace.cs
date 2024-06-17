// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using GitHubExtension.Client;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Octokit;
using Windows.Foundation;

namespace GitHubExtension.Providers.Codespaces;

public sealed class Codespace(Octokit.Codespace codespace) : IComputeSystem2
{
    public IDeveloperId? AssociatedDeveloperId { get; set; }

    public string AssociatedProviderId => "Microsoft.GitHub.Codespaces";

    public string? DisplayName => codespace.Name;

    public string Id => codespace.Id.ToString(CultureInfo.InvariantCulture);

    public string SupplementalDisplayName => codespace.Repository.FullName;

    public ComputeSystemOperations SupportedOperations => ComputeSystemOperations.Start | ComputeSystemOperations.Terminate;

#pragma warning disable CS0067
    public event TypedEventHandler<IComputeSystem, ComputeSystemState>? StateChanged;
#pragma warning restore CS0067

    public async Task StartAsync()
    {
        ArgumentNullException.ThrowIfNull(AssociatedDeveloperId);

        GitHubClientProvider gitHubClientProvider = new GitHubClientProvider();
        GitHubClient? gitHubClient = gitHubClientProvider.GetClient(AssociatedDeveloperId);

        if (gitHubClient == null)
        {
            throw new InvalidOperationException();
        }

        await gitHubClient.Codespaces.Start(codespace.Name);
    }

    public async Task StopAsync()
    {
        ArgumentNullException.ThrowIfNull(AssociatedDeveloperId);

        GitHubClientProvider gitHubClientProvider = new GitHubClientProvider();
        GitHubClient? gitHubClient = gitHubClientProvider.GetClient(AssociatedDeveloperId);

        if (gitHubClient == null)
        {
            throw new InvalidOperationException();
        }

        await gitHubClient.Codespaces.Stop(codespace.Name);
    }

    public IAsyncOperation<ComputeSystemOperationResult> ConnectAsync(string options)
    {
        return Task.Run(async () =>
        {
            StateChanged?.Invoke(this, ComputeSystemState.Starting);
            await StartAsync();
            StateChanged?.Invoke(this, ComputeSystemState.Running);
            new Process() { StartInfo = new ProcessStartInfo(codespace.WebUrl) { UseShellExecute = true } }.Start();
            return new ComputeSystemOperationResult();
        }).AsAsyncOperation();
    }

    public IApplyConfigurationOperation CreateApplyConfigurationOperation(string configuration) => throw new InvalidOperationException();

    public IAsyncOperation<ComputeSystemOperationResult> CreateSnapshotAsync(string options) => throw new InvalidOperationException();

    public IAsyncOperation<ComputeSystemOperationResult> DeleteAsync(string options) => throw new InvalidOperationException();

    public IAsyncOperation<ComputeSystemOperationResult> DeleteSnapshotAsync(string options) => throw new InvalidOperationException();

    public IAsyncOperation<IEnumerable<ComputeSystemProperty>> GetComputeSystemPropertiesAsync(string options)
    {
        return Task.Run(() =>
        {
            return new List<ComputeSystemProperty>()
            {
                ComputeSystemProperty.Create(ComputeSystemPropertyKind.CpuCount, codespace.Machine.CpuCount.ToString(CultureInfo.InvariantCulture)),
                ComputeSystemProperty.Create(ComputeSystemPropertyKind.AssignedMemorySizeInBytes, codespace.Machine.MemoryInBytes.ToString(CultureInfo.InvariantCulture)),
                ComputeSystemProperty.Create(ComputeSystemPropertyKind.StorageSizeInBytes, codespace.Machine.StorageInBytes.ToString(CultureInfo.InvariantCulture)),
                ComputeSystemProperty.CreateCustom(codespace.Machine.OperatingSystem, "Operating system", new Uri("ms-appx://invalid")),
            }.AsEnumerable();
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemThumbnailResult> GetComputeSystemThumbnailAsync(string options)
    {
        throw new NotImplementedException();
    }

    public IAsyncOperation<ComputeSystemPinnedResult> GetIsPinnedToStartMenuAsync() => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemPinnedResult> GetIsPinnedToTaskbarAsync() => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemStateResult> GetStateAsync()
    {
        return Task<ComputeSystemStateResult>.Run(() =>
        {
            if (!codespace.State.TryParse(out CodespaceState state))
            {
                return new ComputeSystemStateResult(ComputeSystemState.Unknown);
            }

            return new ComputeSystemStateResult(state switch
            {
                CodespaceState.Starting => ComputeSystemState.Starting,
                CodespaceState.Unknown => ComputeSystemState.Unknown,
                CodespaceState.Created => ComputeSystemState.Created,
                CodespaceState.Queued => ComputeSystemState.Starting,
                CodespaceState.Provisioning => ComputeSystemState.Creating,
                CodespaceState.Available => ComputeSystemState.Running,
                CodespaceState.Awaiting => ComputeSystemState.Running,
                CodespaceState.Unavailable => ComputeSystemState.Unknown,
                CodespaceState.Deleted => ComputeSystemState.Deleted,
                CodespaceState.Moved => ComputeSystemState.Unknown,
                CodespaceState.Shutdown => ComputeSystemState.Stopped,
                CodespaceState.Archived => ComputeSystemState.Unknown,
                CodespaceState.ShuttingDown => ComputeSystemState.Stopping,
                CodespaceState.Failed => ComputeSystemState.Unknown,
                CodespaceState.Exporting => ComputeSystemState.Unknown,
                CodespaceState.Updating => ComputeSystemState.Unknown,
                CodespaceState.Rebuilding => ComputeSystemState.Restarting,
                _ => ComputeSystemState.Unknown,
            });
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> ModifyPropertiesAsync(string inputJson) => throw new InvalidOperationException();

    public IAsyncOperation<ComputeSystemOperationResult> PauseAsync(string options) => throw new InvalidOperationException();

    public IAsyncOperation<ComputeSystemOperationResult> PinToStartMenuAsync() => throw new InvalidOperationException();

    public IAsyncOperation<ComputeSystemOperationResult> PinToTaskbarAsync() => throw new InvalidOperationException();

    public IAsyncOperation<ComputeSystemOperationResult> RestartAsync(string options) => throw new InvalidOperationException();

    public IAsyncOperation<ComputeSystemOperationResult> ResumeAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> RevertSnapshotAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> SaveAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> ShutDownAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> StartAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> TerminateAsync(string options)
    {
        return Task.Run(async () =>
        {
            StateChanged?.Invoke(this, ComputeSystemState.Stopping);
            await StopAsync();
            StateChanged?.Invoke(this, ComputeSystemState.Stopped);
            await StopAsync();
            return new ComputeSystemOperationResult();
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> UnpinFromStartMenuAsync() => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> UnpinFromTaskbarAsync() => throw new NotImplementedException();
}
