// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace GitHubExtension.DeveloperId;
internal class LoginUIController : IExtensionAdaptiveCardSession
{
    private IExtensionAdaptiveCard? _loginUI;
    private static readonly LoginUITemplate _loginUITemplate = new ();

    public LoginUIController()
    {
    }

    public void Dispose()
    {
        Log.Logger()?.ReportDebug($"Dispose");
        _loginUI?.Update(null, null, null);
    }

    public ProviderOperationResult Initialize(IExtensionAdaptiveCard extensionUI)
    {
        Log.Logger()?.ReportDebug($"Initialize");
        _loginUI = extensionUI;
        var operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.LoginPage), null, LoginUIState.LoginPage);
        return operationResult;
    }

    public IAsyncOperation<ProviderOperationResult> OnAction(string action, string inputs)
    {
        return Task.Run(async () =>
        {
            ProviderOperationResult operationResult;
            Log.Logger()?.ReportInfo($"OnAction() called with state:{_loginUI?.State}");
            Log.Logger()?.ReportDebug($"action: {action}");

            switch (_loginUI?.State)
            {
                case LoginUIState.LoginPage:
                {
                    try
                    {
                        // If there is already a developer id, we should block another login.
                        if (DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIdsInternal().Any())
                        {
                            Log.Logger()?.ReportInfo($"DeveloperId {DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIdsInternal().First().LoginId} already exists. Blocking login.");
                            _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.LoginFailedPage), null, LoginUIState.LoginFailedPage);
                            operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Only one DeveloperId can be logged in at a time", "One DeveloperId already exists");
                            break;
                        }

                        // Inputs are validated at this point.
                        _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.WaitingPage), null, LoginUIState.WaitingPage);
                        Log.Logger()?.ReportDebug($"inputs: {inputs}");

                        var devId = await DeveloperIdProvider.GetInstance().LoginNewDeveloperIdAsync();
                        if (devId != null)
                        {
                            var resourceLoader = new ResourceLoader(ResourceLoader.GetDefaultResourceFilePath(), "GitHubExtension/Resources");
                            operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.LoginSucceededPage).Replace("${message}", $"{devId.LoginId} {resourceLoader.GetString("LoginUI_LoginSucceededPage_text")}"), null, LoginUIState.LoginSucceededPage);
                        }
                        else
                        {
                            Log.Logger()?.ReportError($"Unable to create DeveloperId");
                            operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Developer Id could not be created", "Developer Id could not be created");
                            _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.LoginFailedPage), null, LoginUIState.LoginFailedPage);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger()?.ReportError($"Error: {ex}");
                        operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, ex, "Error occurred in login page", ex.Message);
                        _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.LoginFailedPage), null, LoginUIState.LoginFailedPage);
                    }

                    break;
                }

                // These pages only have close actions.
                case LoginUIState.LoginSucceededPage:
                case LoginUIState.LoginFailedPage:
                {
                    Log.Logger()?.ReportInfo($"State:{_loginUI.State}");
                    operationResult = _loginUI.Update(null, null, LoginUIState.End);
                    break;
                }

                // These pages do not have any actions. We should never be here.
                case LoginUIState.WaitingPage:
                default:
                {
                    Log.Logger()?.ReportError($"Unexpected state:{_loginUI?.State}");
                    operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, $"Error occurred in :{_loginUI?.State}", $"Error occurred in :{_loginUI?.State}");
                    break;
                }
            }

            return operationResult;
        }).AsAsyncOperation();
    }

    // Adaptive Card Templates for LoginUI.
    private class LoginUITemplate
    {
        internal string GetLoginUITemplate(string loginUIState)
        {
            var loader = new ResourceLoader(ResourceLoader.GetDefaultResourceFilePath(), "GitHubExtension/Resources");

            var loginPage = @"
{
    ""type"": ""AdaptiveCard"",
    ""body"": [
        {
            ""type"": ""ColumnSet"",
            ""spacing"": ""Large"",
            ""columns"": [
                {
                    ""type"": ""Column"",
                    ""items"": [
                        {
                            ""type"": ""Image"",
                            ""style"": ""Person"",
                            ""url"": ""https://github.githubassets.com/images/modules/logos_page/GitHub-Mark.png"",
                            ""size"": ""small"",
                            ""horizontalAlignment"": ""Center"",
                            ""spacing"": ""None""
                        },
                        {
                            ""type"": ""TextBlock"",
                            ""weight"": ""Bolder"",
                            ""text"": """ + $"{loader.GetString("LoginUI_LoginPage_Heading")}" + @""",
                            ""wrap"": true,
                            ""horizontalAlignment"": ""Center"",
                            ""spacing"": ""Small"",
                            ""size"": ""large""
                        },
                        {
                            ""type"": ""TextBlock"",
                            ""text"": """ + $"{loader.GetString("LoginUI_LoginPage_Subheading")}" + @""",
                            ""wrap"": true,
                            ""horizontalAlignment"": ""Center"",
                            ""spacing"": ""None"",
                            ""size"": ""small""
                        },
                        {
                            ""type"": ""TextBlock"",
                            ""text"": """",
                            ""wrap"": true,
                            ""spacing"": ""Large"",
                            ""horizontalAlignment"": ""Center"",
                            ""isSubtle"": true
                        }
                    ],
                    ""width"": ""stretch"",
                    ""separator"": true,
                    ""spacing"": ""Medium""
                }
            ]
        },
        {
            ""type"": ""Table"",
            ""columns"": [
                {
                    ""width"": 1
                }
            ],
            ""rows"": [
                {
                    ""type"": ""TableRow"",
                    ""cells"": [
                        {
                            ""type"": ""TableCell"",
                            ""items"": [
                                {
                                    ""type"": ""ActionSet"",
                                    ""actions"": [
                                        {
                                            ""type"": ""Action.Submit"",
                                            ""title"": """ + $"{loader.GetString("LoginUI_LoginPage_Button1Text")}" + @""",
                                            ""tooltip"": """ + $"{loader.GetString("LoginUI_LoginPage_Button1ToolTip")}" + @""",
                                            ""style"": ""positive"",
                                            ""isEnabled"": true,
                                            ""id"": ""Personal""
                                        }
                                    ],
                                    ""horizontalAlignment"": ""Center"",
                                    ""spacing"": ""None""
                                }
                            ],
                            ""isVisible"": true,
                            ""verticalContentAlignment"": ""Center"",
                            ""height"": ""stretch"",
                            ""spacing"": ""None"",
                            ""horizontalAlignment"": ""Center""
                        }
                    ],
                    ""horizontalAlignment"": ""Center"",
                    ""height"": ""stretch"",
                    ""horizontalCellContentAlignment"": ""Center"",
                    ""verticalCellContentAlignment"": ""Center"",
                    ""spacing"": ""None""
                },
                {
                    ""type"": ""TableRow"",
                    ""cells"": [
                        {
                            ""type"": ""TableCell"",
                            ""items"": [
                                {
                                    ""type"": ""ActionSet"",
                                    ""actions"": [
                                        {
                                            ""type"": ""Action.ShowCard"",
                                            ""title"": """ + $"{loader.GetString("LoginUI_LoginPage_Button2Text")}" + @""",
                                            ""isEnabled"": false,
                                            ""tooltip"": """ + $"{loader.GetString("LoginUI_LoginPage_Button2ToolTip")}" + @""",
                                            ""id"": ""Enterprise"",
                                            ""card"": {
                                                ""type"": ""AdaptiveCard"",
                                                ""body"": [
                                                    {
                                                        ""type"": ""Input.Text"",
                                                        ""placeholder"": """ + $"{loader.GetString("LoginUI_LoginPage_Button2Flyout_Text_PlaceHolder")}" + @""",
                                                        ""style"": ""Url"",
                                                        ""isRequired"": true,
                                                        ""id"": ""Enterprise.server"",
                                                        ""label"": """ + $"{loader.GetString("LoginUI_LoginPage_Button2Flyout_Text_Label")}" + @""",
                                                        ""value"": ""github.com"",
                                                        ""errorMessage"": """ + $"{loader.GetString("LoginUI_LoginPage_Button2Flyout_Text_ErrorMessage")}" + @"""
                                                    },
                                                    {
                                                        ""type"": ""ActionSet"",
                                                        ""actions"": [
                                                            {
                                                                ""type"": ""Action.Submit"",
                                                                ""title"": """ + $"{loader.GetString("LoginUI_LoginPage_Button2Flyout_Button")}" + @""",
                                                                ""style"": ""positive"",
                                                                ""associatedInputs"": ""auto""
                                                            }
                                                        ]
                                                    }
                                                ]
                                            }
                                        }
                                    ],
                                    ""spacing"": ""None"",
                                    ""horizontalAlignment"": ""Center""
                                }
                            ],
                            ""verticalContentAlignment"": ""Center"",
                            ""spacing"": ""None"",
                            ""horizontalAlignment"": ""Center""
                        }
                    ],
                    ""spacing"": ""None"",
                    ""horizontalAlignment"": ""Center"",
                    ""horizontalCellContentAlignment"": ""Center"",
                    ""verticalCellContentAlignment"": ""Center""
                }
            ],
            ""firstRowAsHeaders"": false,
            ""spacing"": ""Medium"",
            ""horizontalAlignment"": ""Center"",
            ""horizontalCellContentAlignment"": ""Center"",
            ""verticalCellContentAlignment"": ""Center"",
            ""showGridLines"": false
        }
    ],
    ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
    ""version"": ""1.5"",
    ""minHeight"": ""350px"",
    ""verticalContentAlignment"": ""Top"",
    ""rtl"": false
}
";

            var enterpriseServerPage = @"
{
    ""type"": ""AdaptiveCard"",
    ""body"": [
        {
            ""type"": ""ColumnSet"",
            ""spacing"": ""Large"",
            ""columns"": [
                {
                    ""type"": ""Column"",
                    ""items"": [
                        {
                            ""type"": ""Image"",
                            ""style"": ""Person"",
                            ""url"": ""https://github.githubassets.com/images/modules/logos_page/GitHub-Mark.png"",
                            ""size"": ""Small"",
                            ""horizontalAlignment"": ""Center"",
                            ""spacing"": ""None""
                        },
                        {
                            ""type"": ""TextBlock"",
                            ""weight"": ""Bolder"",
                            ""text"": ""GitHub"",
                            ""wrap"": true,
                            ""horizontalAlignment"": ""Center"",
                            ""spacing"": ""Small"",
                            ""size"": ""Large""
                        },
                        {
                            ""type"": ""TextBlock"",
                            ""text"": ""Enterprise Server"",
                            ""wrap"": true,
                            ""horizontalAlignment"": ""Center"",
                            ""spacing"": ""None"",
                            ""size"": ""Small""
                        },
                        {
                            ""type"": ""TextBlock"",
                            ""text"": """",
                            ""wrap"": true,
                            ""spacing"": ""Large"",
                            ""horizontalAlignment"": ""Center"",
                            ""isSubtle"": true
                        }
                    ],
                    ""width"": ""stretch"",
                    ""separator"": true,
                    ""spacing"": ""Medium""
                }
            ]
        },
        {
            ""type"": ""Input.Text"",
            ""placeholder"": ""Enter server address here"",
            ""id"": ""EnterpriseServer"",
            ""style"": ""Url"",
            ""isRequired"": true,
            ""spacing"": ""ExtraLarge""
        },
        {
            ""type"": ""ColumnSet"",
            ""horizontalAlignment"": ""Center"",
            ""height"": ""stretch"",
            ""columns"": [
                {
                    ""type"": ""Column"",
                    ""width"": ""stretch"",
                    ""items"": [
                        {
                            ""type"": ""ActionSet"",
                            ""actions"": [
                                {
                                    ""type"": ""Action.Submit"",
                                    ""title"": ""Cancel"",
                                    ""id"": ""Cancel"",
                                    ""role"": ""Button""
                                }
                            ]
                        }
                    ]
                },
                {
                    ""type"": ""Column"",
                    ""width"": ""stretch"",
                    ""items"": [
                        {
                            ""type"": ""ActionSet"",
                            ""actions"": [
                                {
                                    ""type"": ""Action.Submit"",
                                    ""title"": ""Next"",
                                    ""id"": ""Next"",
                                    ""style"": ""positive"",
                                    ""role"": ""Button""
                                }
                            ]
                        }
                    ]
                }
            ],
            ""spacing"": ""Small""
        }
    ],
    ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
    ""version"": ""1.5"",
    ""minHeight"": ""350px"",
    ""verticalContentAlignment"": ""Top"",
    ""rtl"": false
}
";

            var enterpriseServerPATPage = @"
{
    ""type"": ""AdaptiveCard"",
    ""body"": [
        {
            ""type"": ""ColumnSet"",
            ""spacing"": ""Large"",
            ""columns"": [
                {
                    ""type"": ""Column"",
                    ""items"": [
                        {
                            ""type"": ""Image"",
                            ""style"": ""Person"",
                            ""url"": ""https://github.githubassets.com/images/modules/logos_page/GitHub-Mark.png"",
                            ""size"": ""Small"",
                            ""horizontalAlignment"": ""Center"",
                            ""spacing"": ""None""
                        },
                        {
                            ""type"": ""TextBlock"",
                            ""weight"": ""Bolder"",
                            ""text"": ""GitHub"",
                            ""wrap"": true,
                            ""horizontalAlignment"": ""Center"",
                            ""spacing"": ""Small"",
                            ""size"": ""Large""
                        },
                        {
                            ""type"": ""TextBlock"",
                            ""text"": ""Enterprise Server"",
                            ""wrap"": true,
                            ""horizontalAlignment"": ""Center"",
                            ""spacing"": ""None"",
                            ""size"": ""Small""
                        }
                    ],
                    ""width"": ""stretch"",
                    ""separator"": true,
                    ""spacing"": ""Medium""
                }
            ]
        },
        {
            ""type"": ""RichTextBlock"",
            ""inlines"": [
                {
                    ""type"": ""TextRun"",
                    ""text"": ""Please enter your Personal Access Token (PAT) to connect to <server>. To create a new PAT, ""
                },
                {
                    ""type"": ""TextRun"",
                    ""text"": ""click here."",
                    ""selectAction"": {
                        ""type"": ""Action.OpenUrl"",
                        ""url"": ""https://adaptivecards.io""
                    }
                }
            ]
        },
        {
            ""type"": ""Input.Text"",
            ""placeholder"": ""Enter personal access token"",
            ""id"": ""EnterpriseServer"",
            ""style"": ""Url"",
            ""isRequired"": true,
            ""spacing"": ""Large"",
            ""errorMessage"": ""Invalid Url""
        },
        {
            ""type"": ""ColumnSet"",
            ""horizontalAlignment"": ""Center"",
            ""height"": ""stretch"",
            ""columns"": [
                {
                    ""type"": ""Column"",
                    ""width"": ""stretch"",
                    ""items"": [
                        {
                            ""type"": ""ActionSet"",
                            ""actions"": [
                                {
                                    ""type"": ""Action.Submit"",
                                    ""title"": ""Cancel"",
                                    ""id"": ""Cancel"",
                                    ""role"": ""Button""
                                }
                            ]
                        }
                    ]
                },
                {
                    ""type"": ""Column"",
                    ""width"": ""stretch"",
                    ""items"": [
                        {
                            ""type"": ""ActionSet"",
                            ""actions"": [
                                {
                                    ""type"": ""Action.Submit"",
                                    ""title"": ""Connect"",
                                    ""id"": ""Connect"",
                                    ""style"": ""positive"",
                                    ""role"": ""Button""
                                }
                            ]
                        }
                    ]
                }
            ],
            ""spacing"": ""Small""
        }
    ],
    ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
    ""version"": ""1.5"",
    ""minHeight"": ""350px"",
    ""verticalContentAlignment"": ""Top"",
    ""rtl"": false
}
";

            var waitingPage = @"
{
    ""type"": ""AdaptiveCard"",
    ""body"": [
        {
            ""type"": ""TextBlock"",
            ""text"": """ + $"{loader.GetString("LoginUI_WaitingPage_Text")}" + @""",
            ""isSubtle"": false,
            ""wrap"": true,
            ""horizontalAlignment"": ""Center"",
            ""spacing"": ""ExtraLarge"",
            ""size"": ""Large"",
            ""weight"": ""Lighter"",
            ""height"": ""stretch"",
            ""style"": ""heading""
        },
        {
            ""type"" : ""TextBlock"",
            ""text"": """ + $"{loader.GetString("LoginUI_WaitingPageBrowserLaunch_Text")}" + @""",
            ""isSubtle"": false,
            ""horizontalAlignment"": ""Center"",
            ""weight"": ""Lighter""
        }
    ],
    ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
    ""version"": ""1.5"",
    ""minHeight"": ""100px""
}
";

            var loginSucceededPage = @"
{
    ""type"": ""AdaptiveCard"",
    ""body"": [
        {
            ""type"": ""TextBlock"",
            ""text"": ""${message}"",
            ""isSubtle"": false,
            ""wrap"": true,
            ""horizontalAlignment"": ""Center"",
            ""spacing"": ""ExtraLarge"",
            ""size"": ""Large"",
            ""weight"": ""Lighter"",
            ""style"": ""heading""
        }
    ],
    ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
    ""version"": ""1.5"",
    ""minHeight"": ""200px""
}
";

            var loginFailedPage = @"
{
    ""type"": ""AdaptiveCard"",
    ""body"": [
        {
            ""type"": ""TextBlock"",
            ""text"": """ + $"{loader.GetString("LoginUI_LoginFailedPage_text1")}" + @""",
            ""isSubtle"": false,
            ""wrap"": true,
            ""horizontalAlignment"": ""Center"",
            ""spacing"": ""ExtraLarge"",
            ""size"": ""Large"",
            ""weight"": ""Lighter"",
            ""style"": ""heading""
        },
        {
            ""type"": ""TextBlock"",
            ""text"": """ + $"{loader.GetString("LoginUI_LoginFailedPage_text2")}" + @""",
            ""isSubtle"": true,
            ""wrap"": true,
            ""horizontalAlignment"": ""Center"",
            ""size"": ""medium"",
            ""weight"": ""Lighter""
        }
    ],
    ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
    ""version"": ""1.5"",
    ""minHeight"": ""200px""
}
";

            switch (loginUIState)
            {
                case LoginUIState.LoginPage:
                {
                    return loginPage;
                }

                case LoginUIState.EnterpriseServerPage:
                {
                    return enterpriseServerPage;
                }

                case LoginUIState.EnterpriseServerPATPage:
                {
                    return enterpriseServerPATPage;
                }

                case LoginUIState.WaitingPage:
                {
                    return waitingPage;
                }

                case LoginUIState.LoginFailedPage:
                {
                    return loginFailedPage;
                }

                case LoginUIState.LoginSucceededPage:
                {
                    return loginSucceededPage;
                }

                default:
                {
                        throw new InvalidOperationException();
                }
            }
        }
    }

    private class LoginPageActionPayload
    {
        public string? Style
        {
            get; set;
        }

        public string? Title
        {
            get; set;
        }

        public string? Type
        {
            get; set;
        }
    }

    private class LoginPageInputPayload
    {
        public string? EnterpriseServer
        {
            get; set;
        }
    }

    // This class cannot be an enum, since we are passing this to the core app as State parameter.
    private class LoginUIState
    {
        internal const string LoginPage = "LoginPage";
        internal const string EnterpriseServerPage = "EnterpriseServerPage";
        internal const string EnterpriseServerPATPage = "EnterpriseServerPATPage";
        internal const string WaitingPage = "WaitingPage";
        internal const string LoginFailedPage = "LoginFailedPage";
        internal const string LoginSucceededPage = "LoginSucceededPage";
        internal const string End = "End";
    }
}
