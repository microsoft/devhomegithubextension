// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubExtension.Test;

[TestClass]
public partial class WidgetTests
{
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
    }

    [TestCleanup]
    public void Cleanup()
    {
        TestHelpers.CloseTestLog();
        TestHelpers.CleanupTempTestOptions(TestOptions, TestContext!);
    }
}
