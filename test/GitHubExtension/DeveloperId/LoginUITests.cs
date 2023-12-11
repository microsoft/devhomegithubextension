// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using GitHubExtension.DeveloperId;
using GitHubExtension.Test.Mocks;
using Microsoft.Windows.DevHome.SDK;

namespace GitHubExtension.Test;

// Unit Tests for LoginUIController and LoginUI
public partial class DeveloperIdTests
{
    public const string EMPTYJSON = "{}";

    public struct LoginUITestData
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
        public const string BadPAT = "BadPAT";
        public const string BadPATInput = "{\"PAT\":\"" + BadPAT + "\"}";
        public static readonly string GoodPATEnterpriseServerPATInput = "{\"PAT\":\"" + Environment.GetEnvironmentVariable("DEV_HOME_TEST_GITHUB_ENTERPRISE_SERVER_PAT") + "\"}";
        public static readonly string GoodPATGithubComPATInput = "{\"PAT\":\"" + Environment.GetEnvironmentVariable("DEV_HOME_TEST_GITHUB_COM_PAT") + "\"}";
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void LoginUI_ControllerInitializeTest()
    {
        var testExtensionAdaptiveCard = new MockExtensionAdaptiveCard(string.Empty, string.Empty, string.Empty);
        Assert.AreEqual(0, testExtensionAdaptiveCard.UpdateCount);

        // Create a LoginUIController and initialize it with the testExtensionAdaptiveCard.
        var controller = new LoginUIController(MockDeveloperIdProvider.GetInstance());
        Assert.AreEqual(ProviderOperationStatus.Success, controller.Initialize(testExtensionAdaptiveCard).Status);

        // Verify that the initial state is the login page.
        Assert.IsTrue(testExtensionAdaptiveCard.State == Enum.GetName(typeof(LoginUIState), LoginUIState.LoginPage));
        Assert.AreEqual(1, testExtensionAdaptiveCard.UpdateCount);

        controller.Dispose();
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow("LoginPage", LoginUITestData.GithubButtonAction, LoginUITestData.GithubButtonInput, "LoginSucceededPage", 3)]
    [DataRow("LoginPage", LoginUITestData.GithubEnterpriseButtonAction, LoginUITestData.GithubEnterpriseButtonInput, "EnterpriseServerPage")]
    [DataRow("EnterpriseServerPage", LoginUITestData.NextButtonAction, LoginUITestData.BadUrlEnterpriseServerInput, "EnterpriseServerPage")]
    [DataRow("EnterpriseServerPage", LoginUITestData.NextButtonAction, LoginUITestData.UnreachableUrlEnterpriseServerInput, "EnterpriseServerPage")]
    [DataRow("EnterpriseServerPage", LoginUITestData.CancelButtonAction, LoginUITestData.CancelButtonInput, "LoginPage")]
    [DataRow("EnterpriseServerPATPage", LoginUITestData.CancelButtonAction, LoginUITestData.CancelButtonInput, "EnterpriseServerPage")]
    [DataRow("EnterpriseServerPATPage", LoginUITestData.ClickHereUrlAction, LoginUITestData.ClickHereUrlInput, "EnterpriseServerPATPage", 1)]
    [DataRow("EnterpriseServerPATPage", LoginUITestData.ConnectButtonAction, LoginUITestData.NullPATInput, "EnterpriseServerPATPage")]
    [DataRow("EnterpriseServerPATPage", LoginUITestData.ConnectButtonAction, LoginUITestData.BadPATInput, "EnterpriseServerPATPage")]
    [DataRow("EnterpriseServerPATPage", LoginUITestData.ConnectButtonAction, EMPTYJSON, "EnterpriseServerPATPage")]
    public async Task LoginUI_ControllerTestSuccess(
        string initialState, string actions, string inputs, string finalState, int numOfFinalUpdates = 2)
    {
        var testExtensionAdaptiveCard = new MockExtensionAdaptiveCard(string.Empty, string.Empty, string.Empty);
        Assert.AreEqual(0, testExtensionAdaptiveCard.UpdateCount);

        // Create a LoginUIController and initialize it with the testExtensionAdaptiveCard.
        var controller = new LoginUIController(MockDeveloperIdProvider.GetInstance());
        Assert.AreEqual(ProviderOperationStatus.Success, controller.Initialize(testExtensionAdaptiveCard).Status);
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
        Assert.AreEqual(ProviderOperationStatus.Success, (await controller.OnAction(actions, inputs)).Status);

        // Verify the final state
        Assert.AreEqual(finalState, testExtensionAdaptiveCard.State);
        Assert.AreEqual(numOfFinalUpdates, testExtensionAdaptiveCard.UpdateCount);

        controller.Dispose();
    }

    [TestMethod]
    [TestCategory("Unit")]
    [DataRow("WaitingPage", EMPTYJSON, EMPTYJSON, "WaitingPage", 1)]
    [DataRow("PageDoesn'tExist", EMPTYJSON, EMPTYJSON, "PageDoesn'tExist", 1)]
    [DataRow("LoginPage", EMPTYJSON, EMPTYJSON, "LoginPage", 1)]
    [DataRow("EnterpriseServerPATPage", EMPTYJSON, EMPTYJSON, "EnterpriseServerPATPage", 1)]
    [DataRow("EnterpriseServerPage", EMPTYJSON, EMPTYJSON, "EnterpriseServerPage", 1)]
    public async Task LoginUI_ControllerTestFailure(
        string initialState, string actions, string inputs, string finalState, int numOfFinalUpdates = 2)
    {
        var testExtensionAdaptiveCard = new MockExtensionAdaptiveCard(string.Empty, string.Empty, string.Empty);
        Assert.AreEqual(0, testExtensionAdaptiveCard.UpdateCount);

        // Create a LoginUIController and initialize it with the testExtensionAdaptiveCard.
        var controller = new LoginUIController(MockDeveloperIdProvider.GetInstance());
        Assert.AreEqual(ProviderOperationStatus.Success, controller.Initialize(testExtensionAdaptiveCard).Status);
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
        Assert.AreEqual(ProviderOperationStatus.Failure, (await controller.OnAction(actions, inputs)).Status);

        // Verify the final state
        Assert.AreEqual(finalState, testExtensionAdaptiveCard.State);
        Assert.AreEqual(numOfFinalUpdates, testExtensionAdaptiveCard.UpdateCount);

        controller.Dispose();
    }

    /* This test requires the following environment variables to be set:
     * DEV_HOME_TEST_GITHUB_ENTERPRISE_SERVER : The host address of the GitHub Enterprise Server to test against
     * DEV_HOME_TEST_GITHUB_COM_PAT : A valid Personal Access Token for GitHub.com (with at least repo_public permissions)
     * DEV_HOME_TEST_GITHUB_ENTERPRISE_SERVER_PAT : A valid Personal Access Token for the GitHub Enterprise Server set in DEV_HOME_TEST_GITHUB_ENTERPRISE_SERVER (with at least repo_public permissions)
     */
    [TestMethod]
    [TestCategory("LiveData")]
    public async Task LoginUI_ControllerPATLoginTest_Success()
    {
        // Create DataRows during Runtime since these need Env vars
        RuntimeDataRow[] dataRows =
                                {
                                    new RuntimeDataRow()
                                    {
                                        InitialState = "EnterpriseServerPATPage",
                                        Actions = LoginUITestData.ConnectButtonAction,
                                        Inputs = LoginUITestData.GoodPATEnterpriseServerPATInput,
                                        FinalState = "LoginSucceededPage",
                                        HostAddress = Environment.GetEnvironmentVariable("DEV_HOME_TEST_GITHUB_ENTERPRISE_SERVER"),
                                    },
                                    new RuntimeDataRow()
                                    {
                                        InitialState = "EnterpriseServerPATPage",
                                        Actions = LoginUITestData.ConnectButtonAction,
                                        Inputs = LoginUITestData.GoodPATGithubComPATInput,
                                        FinalState = "LoginSucceededPage",
                                        HostAddress = "https://api.github.com",
                                    },
                                }
            ;

        foreach (RuntimeDataRow dataRow in dataRows)
        {
            var testExtensionAdaptiveCard = new MockExtensionAdaptiveCard(string.Empty, string.Empty, string.Empty);
            Assert.AreEqual(0, testExtensionAdaptiveCard.UpdateCount);

            // Create a LoginUIController and initialize it with the testExtensionAdaptiveCard.
            var controller = new LoginUIController(MockDeveloperIdProvider.GetInstance());
            Assert.AreEqual(ProviderOperationStatus.Success, controller.Initialize(testExtensionAdaptiveCard).Status);
            Assert.AreEqual(1, testExtensionAdaptiveCard.UpdateCount);

            // Set the initial state.
            testExtensionAdaptiveCard.State = dataRow.InitialState ?? string.Empty;
            Assert.AreEqual(dataRow.InitialState, testExtensionAdaptiveCard.State);

            // Set HostAddress for EnterpriseServerPATPage to make this a valid state
            if (dataRow.InitialState == "EnterpriseServerPATPage")
            {
                controller.HostAddress = new Uri(dataRow.HostAddress ?? string.Empty);
                Assert.AreEqual(dataRow.HostAddress, controller.HostAddress.OriginalString);
            }

            // Call OnAction() with the actions and inputs.
            Assert.AreEqual(ProviderOperationStatus.Success, (await controller.OnAction(dataRow.Actions ?? string.Empty, dataRow.Inputs ?? string.Empty)).Status);

            // Verify the final state
            Assert.AreEqual(dataRow.FinalState, testExtensionAdaptiveCard.State);
            Assert.AreEqual(2, testExtensionAdaptiveCard.UpdateCount);

            controller.Dispose();
        }
    }
}
