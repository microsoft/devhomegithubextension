// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using GitHubExtension.DeveloperId.LoginUI;
using Microsoft.Windows.DevHome.SDK;
using ResourceLoader = Microsoft.Windows.ApplicationModel.Resources.ResourceLoader;

namespace GitHubExtension.DeveloperId;

internal class LoginUIPage
{
    private readonly string _template;
    private readonly LoginUIState _state;
    private ILoginUIPageData? _data;

    public interface ILoginUIPageData
    {
        public abstract string GetJson();
    }

    protected ILoginUIPageData Data
    {
        get => _data ?? throw new InvalidOperationException();
        set => _data = value;
    }

    public LoginUIPage(LoginUIState state)
    {
        _template = GetTemplate(state);
        _state = state;
    }

    public ProviderOperationResult UpdateExtensionAdaptiveCard(IExtensionAdaptiveCard adaptiveCard)
    {
        if (adaptiveCard == null)
        {
            throw new ArgumentNullException(nameof(adaptiveCard));
        }

        var x = _data?.GetJson();
        return adaptiveCard.Update(_template, x, Enum.GetName(typeof(LoginUIState), _state));
    }

    private string GetTemplate(LoginUIState loginUIState)
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
                    ""text"": """ + $"{loader.GetString("LoginUI_EnterprisePATPage_Text")} " + @"""
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

    internal class SubmitActionPayload
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
}
