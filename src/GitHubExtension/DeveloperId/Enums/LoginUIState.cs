// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubExtension.DeveloperId;

public enum LoginUIState
{
    // Login UI starts on the LoginPage.
    LoginPage,

    // Enterprise Server is selected on the LoginPage
    EnterpriseServerPage,

    // User has entered a server URL on the EnterpriseServerPage
    EnterpriseServerPATPage,

    // The user has clicked the "Sign in with GitHub" button and is waiting for the GitHub login page to load.
    WaitingPage,

    // Login has failed and the user is shown the LoginFailedPage.
    LoginFailedPage,

    // Login has succeeded and the user is shown the LoginSucceededPage.
    LoginSucceededPage,

    // LoginUI is not visible and is in the process of being disposed.
    End,
}
