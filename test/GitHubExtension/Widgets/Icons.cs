// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubExtension.Test;

public partial class WidgetTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void IconsTest()
    {
        var icon = Helpers.IconLoader.GetIconAsBase64("arrow.png");

        Assert.IsNotNull(icon);
        Assert.IsTrue(icon.Length != 0);
    }
}
