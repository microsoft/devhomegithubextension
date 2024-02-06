// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Logging;

namespace GitHubExtension.Test;

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
