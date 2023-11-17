// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using GitHubExtension.Helpers;
using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.DevHome.SDK;
using Octokit;
using Windows.Foundation;

namespace GitHubExtension.DeveloperId;
internal class LoginUIController : IExtensionAdaptiveCardSession
{
    private IExtensionAdaptiveCard? _loginUI;
    private static readonly LoginUITemplate _loginUITemplate = new ();
    private Uri? _hostAddress;

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
            if (_loginUI == null)
            {
                Log.Logger()?.ReportError($"_loginUI is null");
                return new ProviderOperationResult(ProviderOperationStatus.Failure, null, "_loginUI is null", "_loginUI is null");
            }

            ProviderOperationResult operationResult;
            Log.Logger()?.ReportInfo($"OnAction() called with state:{_loginUI.State}");
            Log.Logger()?.ReportDebug($"action: {action}");

            switch (_loginUI.State)
            {
                case LoginUIState.LoginPage:
                {
                    try
                    {
                        // If there is already a developer id, we should block another login.
                        /*if (DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIdsInternal().Any())
                        {
                            Log.Logger()?.ReportInfo($"DeveloperId {DeveloperIdProvider.GetInstance().GetLoggedInDeveloperIdsInternal().First().LoginId} already exists. Blocking login.");
                            _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.LoginFailedPage), null, LoginUIState.LoginFailedPage);
                            operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Only one DeveloperId can be logged in at a time", "One DeveloperId already exists");
                            break;
                        }*/

                        // Inputs are validated at this point.
                        _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.WaitingPage), null, LoginUIState.WaitingPage);
                        var loginPageActionPayload = JsonSerializer.Deserialize<LoginPageActionPayload>(action, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                        }) ?? throw new InvalidOperationException("Invalid action");

                        if (loginPageActionPayload?.Id == "Enterprise")
                        {
                            Log.Logger()?.ReportInfo($"Show Enterprise Page");

                            // Update UI with Enterprise Server page and return.
                            operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.EnterpriseServerPage), null, LoginUIState.EnterpriseServerPage);
                            break;
                        }

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

                case LoginUIState.EnterpriseServerPage:
                {
                    // Check if the user clicked on Cancel button.
                    var enterprisePageActionPayload = JsonSerializer.Deserialize<EnterprisePageActionPayload>(action, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    }) ?? throw new InvalidOperationException("Invalid action");

                    if (enterprisePageActionPayload?.Id == "Cancel")
                    {
                        Log.Logger()?.ReportInfo($"Cancel clicked");
                        operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.LoginPage), null, LoginUIState.LoginPage);
                        break;
                    }

                    // Otherwise user clicked on Next button. We should validate the inputs and update the UI with PAT page.
                    Log.Logger()?.ReportDebug($"inputs: {inputs}");
                    var enterprisePageInputPayload = JsonSerializer.Deserialize<EnterprisePageInputPayload>(inputs, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    }) ?? throw new InvalidOperationException("Invalid inputs");
                    Log.Logger()?.ReportInfo($"EnterpriseServer: {enterprisePageInputPayload?.EnterpriseServer}");

                    if (enterprisePageInputPayload?.EnterpriseServer == null)
                    {
                        Log.Logger()?.ReportError($"EnterpriseServer is null");
                        operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "EnterpriseServer is null", "EnterpriseServer is null");

                        // TODO: replace this with UI Update within Enterprise page
                        break;
                    }

                    try
                    {
                        _hostAddress = new Uri(enterprisePageInputPayload.EnterpriseServer);
                        var gitHubClient = new GitHubClient(new ProductHeaderValue(Constants.DEV_HOME_APPLICATION_NAME), _hostAddress);

                        // set timeout to 1 second
                        gitHubClient.Connection.SetRequestTimeout(System.TimeSpan.FromSeconds(1));
                        var enterpriseVersion = (await gitHubClient.Meta.GetMetadata()).InstalledVersion ?? throw new InvalidOperationException();

                        // If we are able to get the version, we can assume that the endpoint is valid.
                        Log.Logger()?.ReportInfo($"Enterprise Server version: {enterpriseVersion}");
                    }
                    catch (Octokit.NotFoundException nfe)
                    {
                        Log.Logger()?.ReportError($"{_hostAddress?.OriginalString} isn't a valid GHES endpoint");
                        operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, nfe, $"Octokit client could not be created with {_hostAddress?.OriginalString}", nfe.Message);

                        // TODO: replace this with UI Update within Enterprise page
                        break;
                    }
                    catch (UriFormatException ufe)
                    {
                        Log.Logger()?.ReportError($"Error: {ufe}");
                        operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, ufe, $"{enterprisePageInputPayload.EnterpriseServer} isn't a valid URI", ufe.Message);

                        // TODO: replace this with UI Update within Enterprise page
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.Logger()?.ReportError($"Error: {ex}");
                        operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, ex, $"Octokit client could not be created with {_hostAddress?.OriginalString}", ex.Message);
                        break;
                    }

                    operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.EnterpriseServerPATPage), null, LoginUIState.EnterpriseServerPATPage);
                    break;
                }

                case LoginUIState.EnterpriseServerPATPage:
                {
                    // Check if the user clicked on Cancel button.
                    var enterprisePATPageActionPayload = JsonSerializer.Deserialize<EnterprisePATPageActionPayload>(action, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    }) ?? throw new InvalidOperationException("Invalid action");

                    if (enterprisePATPageActionPayload?.Id == "Cancel")
                    {
                        Log.Logger()?.ReportInfo($"Cancel clicked");

                        // TODO: Replace Hostaddress in template with the one entered by user in Enterprise page already
                        operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.EnterpriseServerPage), null, LoginUIState.EnterpriseServerPage);
                        break;
                    }

                    Log.Logger()?.ReportDebug($"inputs: {inputs}");
                    var enterprisePATPageInputPayload = JsonSerializer.Deserialize<EnterprisePATPageInputPayload>(inputs, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    }) ?? throw new InvalidOperationException("Invalid inputs");
                    Log.Logger()?.ReportInfo($"PAT Received");

                    if (enterprisePATPageInputPayload?.PAT == null)
                    {
                        Log.Logger()?.ReportError($"PAT is null");
                        operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "PAT is null", "PAT is null");

                        // TODO: replace this with UI Update within Enterprise page
                        break;
                    }

                    if (_hostAddress == null)
                    {
                        // This should never happen.
                        Log.Logger()?.ReportError($"Host address is null");
                        operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Host address is null", "Host address is null");
                        break;
                    }

                    var securePAT = new NetworkCredential(null, enterprisePATPageInputPayload?.PAT).SecurePassword;

                    try
                    {
                        var devId = DeveloperIdProvider.GetInstance().LoginNewDeveloperIdWithPAT(_hostAddress, securePAT);

                        if (devId != null)
                        {
                            var resourceLoader = new ResourceLoader(ResourceLoader.GetDefaultResourceFilePath(), "GitHubExtension/Resources");
                            operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.LoginSucceededPage).Replace("${message}", $"{devId.LoginId} {resourceLoader.GetString("LoginUI_LoginSucceededPage_text")}"), null, LoginUIState.LoginSucceededPage);
                        }
                        else
                        {
                            Log.Logger()?.ReportError($"PAT doesn't work for GHES endpoint {_hostAddress}");
                            operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Developer Id could not be created", "Developer Id could not be created");

                            // TODO: replace this with UI Update within PAT page
                            _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.LoginFailedPage), null, LoginUIState.LoginFailedPage);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger()?.ReportError($"Error: {ex}");
                        operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, ex, "Error occurred in login page", ex.Message);

                        // TODO: replace this with UI Update within PAT page
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
                    Log.Logger()?.ReportError($"Unexpected state:{_loginUI.State}");
                    operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, $"Error occurred in :{_loginUI.State}", $"Error occurred in :{_loginUI.State}");
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
                                            ""type"": ""Action.Submit"",
                                            ""title"": """ + $"{loader.GetString("LoginUI_LoginPage_Button2Text")}" + @""",
                                            ""isEnabled"": true,
                                            ""tooltip"": """ + $"{loader.GetString("LoginUI_LoginPage_Button2ToolTip")}" + @""",
                                            ""id"": ""Enterprise""
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
            ""id"": ""PAT"",
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

    private class ButtonClickActionPayload
    {
        public string? Id
        {
            get; set;
        }

        public string? Style
        {
            get; set;
        }

        public string? ToolTip
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

    private class LoginPageActionPayload : ButtonClickActionPayload
    {
    }

    private class EnterprisePageActionPayload : ButtonClickActionPayload
    {
    }

    private class EnterprisePATPageActionPayload : ButtonClickActionPayload
    {
    }

    private class EnterprisePageInputPayload
    {
        public string? EnterpriseServer
        {
            get; set;
        }
    }

    private class EnterprisePATPageInputPayload
    {
        public string? PAT
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
