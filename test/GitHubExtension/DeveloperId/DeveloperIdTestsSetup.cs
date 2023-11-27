// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

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

    private TestOptions testOptions = new ();

    private TestOptions TestOptions
    {
        get => testOptions;
        set => testOptions = value;
    }

    [TestInitialize]
    public void TestInitialize()
    {
        using var log = new DevHome.Logging.Logger("TestStore", TestOptions.LogOptions);
        var testListener = new TestListener("TestListener", TestContext!);
        log.AddListener(testListener);
        DataModel.Log.Attach(log);
        DeveloperId.Log.Attach(log);
        TestOptions = TestHelpers.SetupTempTestOptions(TestContext!);

        TestContext?.WriteLine("DeveloperIdTests may use the same credential store as Dev Home Github Extension");

        // Remove any existing credentials
        var credentialVault = new CredentialVault();
        credentialVault.RemoveAllCredentials();
    }

    [TestCleanup]
    public void Cleanup()
    {
        TestHelpers.CleanupTempTestOptions(TestOptions, TestContext!);
    }
}
