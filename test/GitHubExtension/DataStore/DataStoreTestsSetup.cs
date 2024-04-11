// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubExtension.Test;

[TestClass]
public partial class DataStoreTests
{
    public TestContext? TestContext
    {
        get;
        set;
    }

    private TestOptions testOptions = new();

    private TestOptions TestOptions
    {
        get => testOptions;
        set => testOptions = value;
    }

    [TestInitialize]
    public void TestInitialize()
    {
        TestOptions = TestHelpers.SetupTempTestOptions(TestContext!);
        TestHelpers.ConfigureTestLog(TestOptions, TestContext!);
    }

    [TestCleanup]
    public void Cleanup()
    {
        TestHelpers.CloseTestLog();
        TestHelpers.CleanupTempTestOptions(TestOptions, TestContext!);
    }
}
