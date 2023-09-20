﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using GitHubExtension.Tests.UITest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Remote;
using static System.Collections.Specialized.BitVector32;

namespace GitHubExtension.Tests.UITest;

[TestClass]
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
