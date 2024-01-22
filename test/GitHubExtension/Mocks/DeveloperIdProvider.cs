// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Security;
using GitHubExtension.DeveloperId;
using Microsoft.UI;
using Microsoft.Windows.DevHome.SDK;
using Octokit;
using Windows.Foundation;

namespace GitHubExtension.Test.Mocks;

public class MockDeveloperIdProvider : IDeveloperIdProviderInternal
{
    private static MockDeveloperIdProvider? instance;

    public string DisplayName => throw new NotImplementedException();

    public event TypedEventHandler<IDeveloperIdProvider, IDeveloperId> Changed;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public AuthenticationExperienceKind GetAuthenticationExperienceKind() => throw new NotImplementedException();

    public AuthenticationState GetDeveloperIdState(IDeveloperId developerId) => throw new NotImplementedException();

    public DeveloperIdsResult GetLoggedInDeveloperIds() => new (new List<DeveloperId.DeveloperId>());

    public AdaptiveCardSessionResult GetLoginAdaptiveCardSession() => throw new NotImplementedException();

    public IAsyncOperation<IDeveloperId> LoginNewDeveloperIdAsync()
    {
        return Task.Run(() =>
        {
            return (IDeveloperId)new DeveloperId.DeveloperId(string.Empty, string.Empty, string.Empty, string.Empty, new GitHubClient(new ProductHeaderValue("Test")));
        }).AsAsyncOperation();
    }

    public DeveloperId.DeveloperId LoginNewDeveloperIdWithPAT(Uri hostAddress, SecureString personalAccessToken)
    {
        var pat = new System.Net.NetworkCredential(string.Empty, personalAccessToken).Password;
        if (pat == DeveloperIdTests.LoginUITestData.BadPAT)
        {
            throw new InvalidOperationException("Invalid PAT");
        }

        // This is a mock method, so we don't need to do anything here. Using Changed to avoid build warning.
        _ = Changed.GetInvocationList();
        return new DeveloperId.DeveloperId(string.Empty, string.Empty, string.Empty, string.Empty, new GitHubClient(new ProductHeaderValue("Test")));
    }

    public ProviderOperationResult LogoutDeveloperId(IDeveloperId developerId) => throw new NotImplementedException();

    public IAsyncOperation<DeveloperIdResult> ShowLogonSession(WindowId windowHandle) => throw new NotImplementedException();

    private MockDeveloperIdProvider()
    {
        Changed += (sender, args) => { };
    }

    public static MockDeveloperIdProvider GetInstance()
    {
        instance ??= new MockDeveloperIdProvider();
        return instance;
    }

    public IEnumerable<DeveloperId.DeveloperId> GetLoggedInDeveloperIdsInternal() => new List<DeveloperId.DeveloperId>();
}

public class MockExtensionAdaptiveCard : IExtensionAdaptiveCard
{
    private int updateCount;

    public int UpdateCount
    {
        get => updateCount;
        set => updateCount = value;
    }

    public MockExtensionAdaptiveCard(string templateJson, string dataJson, string state)
    {
        TemplateJson = templateJson;
        DataJson = dataJson;
        State = state;
    }

    public string DataJson
    {
        get; set;
    }

    public string State
    {
        get; set;
    }

    public string TemplateJson
    {
        get; set;
    }

    public ProviderOperationResult Update(string templateJson, string dataJson, string state)
    {
        UpdateCount++;
        TemplateJson = templateJson;
        DataJson = dataJson;
        State = state;
        return new ProviderOperationResult(ProviderOperationStatus.Success, null, "Update() succeeded", "Update() succeeded");
    }
}
