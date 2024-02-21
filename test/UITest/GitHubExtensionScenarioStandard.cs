// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitHubExtension.Tests.UITest;

[TestClass]
[Ignore]
public class GitHubExtensionScenarioStandard : GitHubExtensionSession
{
    [TestMethod]
    public void GitHubExtensionTest1()
    {
        // Disabling this test because we do not have an App with UI testable in this way.
        // This extension is headless except for the Widgets, which are hosted in Dev Home.
    }

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        // Disabled, see above.
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        // Disabled, see above.
    }
}
