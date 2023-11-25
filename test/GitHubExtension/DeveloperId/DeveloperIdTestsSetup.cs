// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace GitHubExtension.Test;

[TestClass]
public partial class DeveloperIdTests
{
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
        TestOptions = TestHelpers.SetupTempTestOptions(TestContext!);

        TestContext?.WriteLine("DeveloperIdTests use the same credential store as Dev Home Github Extension");
    }

    [TestCleanup]
    public void Cleanup()
    {
        TestHelpers.CleanupTempTestOptions(TestOptions, TestContext!);
    }
}
