// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Drawing.Drawing2D;
using System.Net;
using System.Text.Json;
using GitHubExtension.Client;
using Microsoft.Windows.DevHome.SDK;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using ResourceLoader = Microsoft.Windows.ApplicationModel.Resources.ResourceLoader;

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
                            var loginPageActionPayload = JsonSerializer.Deserialize<LoginPageActionPayload>(action, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true,
                            }) ?? throw new InvalidOperationException("Invalid action");

                            if (loginPageActionPayload?.Id == "Enterprise")
                            {
                                Log.Logger()?.ReportInfo($"Show Enterprise Page");

                                // Update UI with Enterprise Server page and return.
                                var pageData = new EnterpriseServerPageData()
                                {
                                    EnterpriseServerInputValue = string.Empty,
                                    EnterpriseServerPageErrorValue = string.Empty,
                                    EnterpriseServerPageErrorVisible = false,
                                };
                                operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.EnterpriseServerPage), JsonSerializer.Serialize(pageData), LoginUIState.EnterpriseServerPage);
                                break;
                            }

                            // Display Waiting page before Browser launch in LoginNewDeveloperIdAsync()
                            _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.WaitingPage), null, LoginUIState.WaitingPage);
                            var devId = await DeveloperIdProvider.GetInstance().LoginNewDeveloperIdAsync();
                            if (devId != null)
                            {
                                var resourceLoader = new ResourceLoader(ResourceLoader.GetDefaultResourceFilePath(), "GitHubExtension/Resources");
                                var pageData = new LoginSucceededPageData
                                {
                                    Message = $"{devId.LoginId} {resourceLoader.GetString("LoginUI_LoginSucceededPage_text")}",
                                };
                                operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.LoginSucceededPage), JsonSerializer.Serialize(pageData), LoginUIState.LoginSucceededPage);
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
                            var pageData = new EnterpriseServerPageData()
                            {
                                EnterpriseServerInputValue = string.Empty,
                                EnterpriseServerPageErrorValue = "EnterpriseServer is null",
                                EnterpriseServerPageErrorVisible = true,
                            };

                            operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.EnterpriseServerPage), JsonSerializer.Serialize(pageData), LoginUIState.EnterpriseServerPage);
                            break;
                        }

                        try
                        {
                            // Probe for Enterprise Server instance
                            _hostAddress = new Uri(enterprisePageInputPayload.EnterpriseServer);
                            if (!Validation.IsReachableGitHubEnterpriseServerURL(_hostAddress))
                            {
                                var pageData = new EnterpriseServerPageData()
                                {
                                    EnterpriseServerInputValue = enterprisePageInputPayload.EnterpriseServer,
                                    EnterpriseServerPageErrorValue = "Enterprise Server is not reachable",
                                    EnterpriseServerPageErrorVisible = true,
                                };

                                operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.EnterpriseServerPage), JsonSerializer.Serialize(pageData), LoginUIState.EnterpriseServerPage);
                                break;
                            }
                        }
                        catch (UriFormatException ufe)
                        {
                            Log.Logger()?.ReportError($"Error: {ufe}");
                            var pageData = new EnterpriseServerPageData()
                            {
                                EnterpriseServerInputValue = enterprisePageInputPayload.EnterpriseServer,
                                EnterpriseServerPageErrorValue = "Enterprise Server URL is invalid",
                                EnterpriseServerPageErrorVisible = true,
                            };
                            operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.EnterpriseServerPage), JsonSerializer.Serialize(pageData), LoginUIState.EnterpriseServerPage);
                            break;
                        }
                        catch (Exception ex)
                        {
                            Log.Logger()?.ReportError($"Error: {ex}");
                            var pageData = new EnterpriseServerPageData()
                            {
                                EnterpriseServerInputValue = enterprisePageInputPayload.EnterpriseServer,
                                EnterpriseServerPageErrorValue = $"Somthing went wrong: {ex}",
                                EnterpriseServerPageErrorVisible = true,
                            };
                            operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.EnterpriseServerPage), JsonSerializer.Serialize(pageData), LoginUIState.EnterpriseServerPage);
                            break;
                        }

                        var pageData1 = new EnterpriseServerPATPageData()
                        {
                            EnterpriseServerPATPageErrorValue = string.Empty,
                            EnterpriseServerPATPageErrorVisible = false,
                            EnterpriseServerPATPageInputValue = string.Empty,
                            EnterpriseServerPATPageCreatePATUrlValue = _hostAddress.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped) + $"/settings/tokens/new?scopes=read:user,notifications,repo,read:org&description=DevHomePAT",
                            EnterpriseServerPATPageServerUrlValue = _hostAddress.OriginalString,
                        };
                        try
                        {
                            operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.EnterpriseServerPATPage), JsonSerializer.Serialize(pageData1), LoginUIState.EnterpriseServerPATPage);
                        }
                        catch (Exception ex)
                        {
                            Log.Logger()?.ReportError($"Error: {ex}");
                            operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.LoginFailedPage), null, LoginUIState.LoginFailedPage);
                        }

                        break;
                    }

                case LoginUIState.EnterpriseServerPATPage:
                    {
                        if (_hostAddress == null)
                        {
                            // This should never happen.
                            Log.Logger()?.ReportError($"Host address is null");
                            operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Host address is null", "Host address is null");
                            break;
                        }

                        // Check if the user clicked on Cancel button.
                        var enterprisePATPageActionPayload = JsonSerializer.Deserialize<EnterprisePATPageActionPayload>(action, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                        }) ?? throw new InvalidOperationException("Invalid action");

                        if (enterprisePATPageActionPayload?.Id == "Cancel")
                        {
                            Log.Logger()?.ReportInfo($"Cancel clicked");

                            operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.EnterpriseServerPage), JsonSerializer.Serialize(new EnterpriseServerPageData()), LoginUIState.EnterpriseServerPage);
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
                            var pageData = new EnterpriseServerPATPageData
                            {
                                EnterpriseServerPATPageInputValue = enterprisePATPageInputPayload?.PAT,
                                EnterpriseServerPATPageErrorValue = $"Please enter the PAT",
                                EnterpriseServerPATPageErrorVisible = true,
                                EnterpriseServerPATPageCreatePATUrlValue = _hostAddress.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped) + $"/settings/tokens/new?scopes=read:user,notifications,repo,read:org&description=DevHomePAT",
                                EnterpriseServerPATPageServerUrlValue = _hostAddress.OriginalString,
                            };
                            operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.EnterpriseServerPATPage), JsonSerializer.Serialize(pageData), LoginUIState.EnterpriseServerPATPage);

                            break;
                        }

                        var securePAT = new NetworkCredential(null, enterprisePATPageInputPayload?.PAT).SecurePassword;

                        try
                        {
                            var devId = DeveloperIdProvider.GetInstance().LoginNewDeveloperIdWithPAT(_hostAddress, securePAT);

                            if (devId != null)
                            {
                                var resourceLoader = new ResourceLoader(ResourceLoader.GetDefaultResourceFilePath(), "GitHubExtension/Resources");
                                var pageData = new LoginSucceededPageData()
                                {
                                    Message = $"{devId.LoginId} {resourceLoader.GetString("LoginUI_LoginSucceededPage_text")}",
                                };
                                operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.LoginSucceededPage), JsonSerializer.Serialize(pageData), LoginUIState.LoginSucceededPage);
                                break;
                            }
                            else
                            {
                                Log.Logger()?.ReportError($"PAT doesn't work for GHES endpoint {_hostAddress.OriginalString}");
                                var pageData = new EnterpriseServerPATPageData
                                {
                                    EnterpriseServerPATPageInputValue = enterprisePATPageInputPayload?.PAT,
                                    EnterpriseServerPATPageErrorValue = $"PAT doesn't work for GHES endpoint {_hostAddress.OriginalString}",
                                    EnterpriseServerPATPageErrorVisible = true,
                                    EnterpriseServerPATPageCreatePATUrlValue = _hostAddress.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped) + $"/settings/tokens/new?scopes=read:user,notifications,repo,read:org&description=DevHomePAT",
                                    EnterpriseServerPATPageServerUrlValue = _hostAddress.OriginalString,
                                };
                                operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.EnterpriseServerPATPage), JsonSerializer.Serialize(pageData), LoginUIState.EnterpriseServerPATPage);
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Logger()?.ReportError($"Error: {ex}");
                            operationResult = _loginUI.Update(_loginUITemplate.GetLoginUITemplate(LoginUIState.LoginFailedPage), null, LoginUIState.LoginFailedPage);
                            break;
                        }
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
                            ""text"": """ + $"{loader.GetString("LoginUI_EnterprisePage_Heading")}" + @""",
                            ""wrap"": true,
                            ""horizontalAlignment"": ""Center"",
                            ""spacing"": ""Small"",
                            ""size"": ""Large""
                        },
                        {
                            ""type"": ""TextBlock"",
                            ""text"": """ + $"{loader.GetString("LoginUI_EnterprisePage_Subheading")}" + @""",
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
            ""placeholder"": """ + $"{loader.GetString("LoginUI_EnterprisePage_InputText_PlaceHolder")}" + @""",
            ""id"": ""EnterpriseServer"",
            ""style"": ""Url"",
            ""spacing"": ""ExtraLarge"",
            ""value"": ""${EnterpriseServerInputValue}""
        },
        {
            ""type"": ""TextBlock"",
            ""text"": ""${EnterpriseServerPageErrorValue}"",
            ""wrap"": true,
            ""horizontalAlignment"": ""Left"",
            ""spacing"": ""small"",
            ""size"": ""small"",
            ""color"": ""attention"",
            ""isVisible"": ""${EnterpriseServerPageErrorVisible}""
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
                                    ""title"": """ + $"{loader.GetString("LoginUI_EnterprisePage_Button_Cancel")}" + @""",
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
                                    ""title"": """ + $"{loader.GetString("LoginUI_EnterprisePage_Button_Next")}" + @""",
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
                            ""text"": """ + $"{loader.GetString("LoginUI_EnterprisePage_Heading")}" + @""",
                            ""wrap"": true,
                            ""horizontalAlignment"": ""Center"",
                            ""spacing"": ""Small"",
                            ""size"": ""Large""
                        },
                        {
                            ""type"": ""TextBlock"",
                            ""text"": """ + $"{loader.GetString("LoginUI_EnterprisePage_Subheading")}" + @""",
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
                    ""text"": """ + $"{loader.GetString("LoginUI_EnterprisePATPage_Text")}" + @"""
                },
                {
                    ""type"": ""TextRun"",
                    ""text"": """ + $"{loader.GetString("LoginUI_EnterprisePATPage_HighlightedText")}" + @""",
                    ""selectAction"": {
                        ""type"": ""Action.OpenUrl"",
                        ""url"": ""${EnterpriseServerPATPageCreatePATUrlValue}""
                    }
                }
            ]
        },
        {
            ""type"": ""Input.Text"",
            ""placeholder"": """ + $"{loader.GetString("LoginUI_EnterprisePATPage_InputText_PlaceHolder")}" + @""",
            ""id"": ""PAT"",
            ""spacing"": ""Large"",
            ""value"": ""${EnterpriseServerPATPageInputValue}""
        },
        {
            ""type"": ""TextBlock"",
            ""text"": ""${EnterpriseServerPATPageErrorValue}"",
            ""wrap"": true,
            ""horizontalAlignment"": ""Left"",
            ""spacing"": ""small"",
            ""size"": ""small"",
            ""color"": ""attention"",
            ""isVisible"": ""${EnterpriseServerPATPageErrorVisible}""
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
                                    ""title"": """ + $"{loader.GetString("LoginUI_EnterprisePATPage_Button_Cancel")}" + @""",
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
                                    ""title"": """ + $"{loader.GetString("LoginUI_EnterprisePATPage_Button_Connect")}" + @""",
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

    private class PageData
    {
    }

    private class LoginSucceededPageData : PageData
    {
        public string? Message { get; set; } = string.Empty;
    }

    private class EnterpriseServerPageData : PageData
    {
        public string EnterpriseServerInputValue { get; set; } = string.Empty;

        // Default is false
        public bool EnterpriseServerPageErrorVisible
        {
            get; set;
        }

        public string EnterpriseServerPageErrorValue { get; set; } = string.Empty;
    }

    private class EnterpriseServerPATPageData : PageData
    {
        public string? EnterpriseServerPATPageInputValue { get; set; } = string.Empty;

        public bool? EnterpriseServerPATPageErrorVisible
        {
            get; set;
        }

        public string? EnterpriseServerPATPageErrorValue { get; set; } = string.Empty;

        public string? EnterpriseServerPATPageCreatePATUrlValue { get; set; } = "https://github.com/";

        public string? EnterpriseServerPATPageServerUrlValue { get; set; } = "https://github.com/";
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
