// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using GITServices.Tests.UITest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Remote;
using static System.Collections.Specialized.BitVector32;

namespace GITServices.Tests.UITest;

[TestClass]
public class GITServicesScenarioStandard : GITServicesSession
{
    [TestMethod]
    public void GITServicesTest1()
    {
        // Disabling this test because we do not have an App with UI testable in this way.
        // This plugin is headless except for the Widgets, which are hosted in Dev Home.
        // Assert.AreEqual("GITServices", session.Title);
    }

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        // Disabled, see above.
        // Setup(context);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        // Disabled, see above.
        // TearDown();
    }
}
