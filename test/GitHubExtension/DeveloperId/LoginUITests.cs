// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubExtension.DeveloperId;
using Microsoft.Windows.DevHome.SDK;

namespace GitHubExtension.Test;

// Unit Tests for LoginUIController and LoginUI
public partial class DeveloperIdTests
{
    public const string EMPTYJSON = "{}";

    private struct LoginUITestData
    {
        public const string GithubButtonAction = "{\"id\":\"Personal\",\"style\":\"positive\",\"title\":\"Sign in to github.com\",\"tooltip\":\"Opens the browser to log you into GitHub\",\"type\":\"Action.Submit\"}";
        public const string GithubButtonInput = EMPTYJSON;

        public const string GithubEnterpriseButtonAction = "{\"id\":\"Enterprise\",\"title\":\"Sign in to GitHub Enterprise Server\",\"tooltip\":\"Lets you enter the host address of your GitHub Enterprise Server\",\"type\":\"Action.Submit\"}";
        public const string GithubEnterpriseButtonInput = EMPTYJSON;

        public const string CancelButtonAction = "{\"id\":\"Cancel\",\"title\":\"Cancel\",\"type\":\"Action.Submit\"}";
        public const string CancelButtonInput = EMPTYJSON;

        public const string NextButtonAction = "{\"id\":\"Next\",\"style\":\"positive\",\"title\":\"Next\",\"type\":\"Action.Submit\"}";
        public const string BadUrlEnterpriseServerInput = "{\"EnterpriseServer\":\"badUrlEnterpriseServer\"}";
        public const string UnreachableUrlEnterpriseServerInput = "{\"EnterpriseServer\":\"https://www.bing.com\"}";
        public static readonly string GoodUrlEnterpriseServerInput = "{\"EnterpriseServer\":\"" + Environment.GetEnvironmentVariable("DEV_HOME_TEST_GITHUB_ENTERPRISE_SERVER") + "\"}";
        public const string GithubUrlEnterpriseServerInput = "{\"EnterpriseServer\":\"https://www.github.com\"}";

        public const string ClickHereUrlAction = "{\"role\":\"Link\",\"type\":\"Action.OpenUrl\",\"url\":\"https://www.bing.com\"}";
        public const string ClickHereUrlInput = EMPTYJSON;

        public const string ConnectButtonAction = "{\"id\":\"Connect\",\"style\":\"positive\",\"title\":\"Connect\",\"type\":\"Action.Submit\"}";
        public const string NullPATInput = "{\"PAT\":\"\"}";
        public const string BadPATInput = "{\"PAT\":\"Enterprise\"}";
        public static readonly string GoodPATEnterpriseServerInput = "{\"PAT\":\"" + Environment.GetEnvironmentVariable("DEV_HOME_TEST_GITHUB_ENTERPRISE_SERVER_PAT") + "\"}";
        public static readonly string GoodPATGithubComInput = "{\"PAT\":\"" + Environment.GetEnvironmentVariable("DEV_HOME_TEST_GITHUB_COM_PAT") + "\"}";
    }

    public class TestExtensionAdaptiveCard : IExtensionAdaptiveCard
    {
        private int updateCount;

        public int UpdateCount
        {
            get => updateCount;
            set => updateCount = value;
        }

        public TestExtensionAdaptiveCard(string templateJson, string dataJson, string state)
        {
            TemplateJson = templateJson;
            DataJson = dataJson;
            State = state;
        }

        public string DataJson
        {
            get; set;
        }

        public string State
        {
            get; set;
        }

        public string TemplateJson
        {
            get; set;
        }

        public ProviderOperationResult Update(string templateJson, string dataJson, string state)
        {
            UpdateCount++;
            TemplateJson = templateJson;
            DataJson = dataJson;
            State = state;
            return new ProviderOperationResult(ProviderOperationStatus.Success, null, "Update() succeeded", "Update() succeeded");
        }
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void LoginUIControllerInitializeTest()
    {
        var testExtensionAdaptiveCard = new TestExtensionAdaptiveCard(string.Empty, string.Empty, string.Empty);
        Assert.AreEqual(0, testExtensionAdaptiveCard.UpdateCount);

        // Create a LoginUIController and initialize it with the testExtensionAdaptiveCard.
        var controller = new LoginUIController();
        controller.Initialize(testExtensionAdaptiveCard);

        // Verify that the initial state is the login page.
        Assert.IsTrue(testExtensionAdaptiveCard.State == Enum.GetName(typeof(LoginUIState), LoginUIState.LoginPage));
        Assert.AreEqual(1, testExtensionAdaptiveCard.UpdateCount);

        controller.Dispose();
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow("LoginPage", LoginUITestData.GithubEnterpriseButtonAction, LoginUITestData.GithubEnterpriseButtonInput, "EnterpriseServerPage")]
    [DataRow("LoginPage", LoginUITestData.GithubEnterpriseButtonAction, LoginUITestData.GithubEnterpriseButtonInput, "EnterpriseServerPage")]
    [DataRow("EnterpriseServerPage", LoginUITestData.NextButtonAction, LoginUITestData.BadUrlEnterpriseServerInput, "EnterpriseServerPage")]
    [DataRow("EnterpriseServerPage", LoginUITestData.NextButtonAction, LoginUITestData.UnreachableUrlEnterpriseServerInput, "EnterpriseServerPage")]
    [DataRow("EnterpriseServerPage", LoginUITestData.CancelButtonAction, LoginUITestData.CancelButtonInput, "LoginPage")]
    [DataRow("EnterpriseServerPATPage", LoginUITestData.CancelButtonAction, LoginUITestData.CancelButtonInput, "EnterpriseServerPage")]
    [DataRow("EnterpriseServerPATPage", LoginUITestData.ClickHereUrlAction, LoginUITestData.ClickHereUrlInput, "EnterpriseServerPATPage", 1)]
    [DataRow("EnterpriseServerPATPage", LoginUITestData.ConnectButtonAction, LoginUITestData.NullPATInput, "EnterpriseServerPATPage")]
    [DataRow("EnterpriseServerPATPage", LoginUITestData.ConnectButtonAction, LoginUITestData.BadPATInput, "EnterpriseServerPATPage")]

    // Cannot test with LoginPAge since that launches browser
    // [DataRow("LoginPage", LoginUITestData.GithubButtonAction, LoginUITestData.GithubButtonInput, "WaitingPage", 2)]
    public async Task LoginUIControllerTest(
        string initialState, string actions, string inputs, string finalState, int numOfFinalUpdates = 2)
    {
        var testExtensionAdaptiveCard = new TestExtensionAdaptiveCard(string.Empty, string.Empty, string.Empty);
        Assert.AreEqual(0, testExtensionAdaptiveCard.UpdateCount);

        // Create a LoginUIController and initialize it with the testExtensionAdaptiveCard.
        var controller = new LoginUIController();
        controller.Initialize(testExtensionAdaptiveCard);
        Assert.AreEqual(1, testExtensionAdaptiveCard.UpdateCount);

        // Set the initial state.
        testExtensionAdaptiveCard.State = initialState;
        Assert.AreEqual(initialState, testExtensionAdaptiveCard.State);

        // Set HostAddress for EnterpriseServerPATPage to make this a valid state
        if (initialState == "EnterpriseServerPATPage")
        {
            controller.HostAddress = new Uri("https://www.github.com");
            Assert.AreEqual("https://www.github.com", controller.HostAddress.OriginalString);
        }

        // Call OnAction() with the actions and inputs.
        await controller.OnAction(actions, inputs);

        // Verify the final state
        Assert.AreEqual(finalState, testExtensionAdaptiveCard.State);
        Assert.AreEqual(numOfFinalUpdates, testExtensionAdaptiveCard.UpdateCount);

        controller.Dispose();
    }
}
