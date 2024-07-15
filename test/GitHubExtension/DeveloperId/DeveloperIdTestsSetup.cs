// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubExtension.DeveloperId;

namespace GitHubExtension.Test;

[TestClass]
public partial class DeveloperIdTests
{
    public class RuntimeDataRow
    {
        public string? InitialState
        {
            get; set;
        }

        public string? Actions
        {
            get; set;
        }

        public string? Inputs
        {
            get; set;
        }

        public string? FinalState
        {
            get; set;
        }

        public string? HostAddress
        {
            get; set;
        }
    }

    public TestContext? TestContext
    {
        get;
        set;
    }

    private TestOptions _testOptions = new();

    private TestOptions TestOptions
    {
        get => _testOptions;
        set => _testOptions = value;
    }

    [TestInitialize]
    public void TestInitialize()
    {
        TestOptions = TestHelpers.SetupTempTestOptions(TestContext!);
        TestHelpers.ConfigureTestLog(TestOptions, TestContext!);

        TestContext?.WriteLine("DeveloperIdTests may use the same credential store as Dev Home Github Extension");

        // Remove any existing credentials
        var credentialVault = new CredentialVault();
        credentialVault.RemoveAllCredentials();
    }

    [TestCleanup]
    public void Cleanup()
    {
        TestHelpers.CloseTestLog();
        TestHelpers.CleanupTempTestOptions(TestOptions, TestContext!);
    }
}
