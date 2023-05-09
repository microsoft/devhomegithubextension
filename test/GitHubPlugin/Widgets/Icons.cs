// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.
using DevHome.Logging;

namespace GitHubPlugin.Test;
public partial class WidgetTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void IconsTest()
    {
        using var log = new Logger("TestStore", TestOptions.LogOptions);
        var testListener = new TestListener("TestListener", TestContext!);
        log.AddListener(testListener);
        DataModel.Log.Attach(log);

        var icon = Helpers.IconLoader.GetIconAsBase64("arrow.png");

        Assert.IsNotNull(icon);
        Assert.IsTrue(icon.Length != 0);
    }
}
