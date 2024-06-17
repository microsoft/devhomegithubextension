// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitHubExtension.Providers.Codespaces;
using Microsoft.Windows.DevHome.SDK;
using Octokit;
using Windows.Foundation;

namespace GitHubExtension.Providers;

internal sealed class ComputeSystemProvider : IComputeSystemProvider
{
    public string DisplayName => "GitHub Codespaces";

    public Uri Icon => new(Constants.ProviderIcon);

    public string Id => "Microsoft.GitHub.Codespaces";

    public ComputeSystemProviderOperations SupportedOperations => ComputeSystemProviderOperations.CreateComputeSystem;

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForComputeSystem(IComputeSystem computeSystem, ComputeSystemAdaptiveCardKind sessionKind) => throw new NotImplementedException();

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForDeveloperId(IDeveloperId developerId, ComputeSystemAdaptiveCardKind sessionKind) => throw new NotImplementedException();

    public ICreateComputeSystemOperation CreateCreateComputeSystemOperation(IDeveloperId developerId, string inputJson) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemsResult> GetComputeSystemsAsync(IDeveloperId developerId)
    {
        return Task.Run(() =>
        {
            ArgumentNullException.ThrowIfNull(developerId);
            CodespacesCollection codespaces = CodespaceHelper.GetCodespacesForDeveloperId(developerId);
            ArgumentException.ThrowIfNullOrEmpty(developerId.LoginId);

            IEnumerable<IComputeSystem> codespacesList = codespaces.Codespaces.Select(c => new Codespaces.Codespace(c) { AssociatedDeveloperId = developerId });

            var computeSystems = new ComputeSystemsResult(codespacesList);
            return new ComputeSystemsResult(computeSystems.ComputeSystems);
        }).AsAsyncOperation();
    }
}
