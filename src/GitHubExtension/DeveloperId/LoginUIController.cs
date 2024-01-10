// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using GitHubExtension.Client;
using GitHubExtension.DeveloperId.LoginUI;
using GitHubExtension.Helpers;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace GitHubExtension.DeveloperId;
public class LoginUIController : IExtensionAdaptiveCardSession
{
    private readonly IDeveloperIdProviderInternal _developerIdProvider;
    private IExtensionAdaptiveCard? _loginUI;
    private Uri? _hostAddress;

    // This variable is used to store the host address from EnterpriseServerPage. It is used in EnterpriseServerPATPage.
    public Uri HostAddress
    {
        get => _hostAddress ?? throw new InvalidOperationException("HostAddress is null");
        set => _hostAddress = value;
    }

    public LoginUIController(IDeveloperIdProviderInternal developerIdProvider)
    {
        _developerIdProvider = developerIdProvider;
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
        return new LoginPage().UpdateExtensionAdaptiveCard(_loginUI);
    }

    public IAsyncOperation<ProviderOperationResult> OnAction(string action, string inputs)
    {
        return Task.Run(async () =>
        {
            if (_loginUI == null)
            {
                Log.Logger()?.ReportError($"OnAction() called with invalid state of LoginUI");
                return new ProviderOperationResult(ProviderOperationStatus.Failure, null, "OnAction() called with invalid state of LoginUI", "_loginUI is null");
            }

            ProviderOperationResult operationResult;
            Log.Logger()?.ReportInfo($"OnAction() called with state:{_loginUI.State}");
            Log.Logger()?.ReportDebug($"action: {action}");

            switch (_loginUI.State)
            {
                case nameof(LoginUIState.LoginPage):
                    {
                        try
                        {
                            // If there is already a developer id, we should block another login.
                            if (_developerIdProvider.GetLoggedInDeveloperIdsInternal().Any())
                            {
                                Log.Logger()?.ReportInfo($"DeveloperId {_developerIdProvider.GetLoggedInDeveloperIdsInternal().First().LoginId} already exists. Blocking login.");
                                new LoginFailedPage().UpdateExtensionAdaptiveCard(_loginUI);
                                operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Only one DeveloperId can be logged in at a time", "One DeveloperId already exists");
                                break;
                            }

                            var loginPageActionPayload = Json.ToObject<LoginPage.ActionPayload>(action) ?? throw new InvalidOperationException("Invalid action");

                            if (!loginPageActionPayload.IsSubmitAction())
                            {
                                Log.Logger()?.ReportError($"Invalid action performed on LoginUI: {loginPageActionPayload.Id}");
                                operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Invalid action performed on LoginUI", "Invalid action performed on LoginUI");
                                break;
                            }

                            if (loginPageActionPayload.IsEnterprise())
                            {
                                Log.Logger()?.ReportInfo($"Show Enterprise Page");
                                operationResult = new EnterpriseServerPage(hostAddress: string.Empty, errorText: string.Empty).UpdateExtensionAdaptiveCard(_loginUI);
                                break;
                            }

                            // Display Waiting page before Browser launch in LoginNewDeveloperIdAsync()
                            new WaitingPage().UpdateExtensionAdaptiveCard(_loginUI);
                            var devId = await _developerIdProvider.LoginNewDeveloperIdAsync();
                            if (devId != null)
                            {
                                operationResult = new LoginSucceededPage(devId).UpdateExtensionAdaptiveCard(_loginUI);
                            }
                            else
                            {
                                Log.Logger()?.ReportError($"Unable to create DeveloperId");
                                new LoginFailedPage().UpdateExtensionAdaptiveCard(_loginUI);
                                operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Developer Id could not be created", "Developer Id could not be created");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Logger()?.ReportError($"Error: {ex}");
                            new LoginFailedPage().UpdateExtensionAdaptiveCard(_loginUI);
                            operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, ex, "Error occurred in login page", ex.Message);
                        }

                        break;
                    }

                case nameof(LoginUIState.EnterpriseServerPage):
                    {
                        // Check if the user clicked on Cancel button.
                        var enterprisePageActionPayload = Json.ToObject<EnterpriseServerPage.ActionPayload>(action)
                                                    ?? throw new InvalidOperationException("Invalid action");

                        if (enterprisePageActionPayload.IsCancelAction())
                        {
                            Log.Logger()?.ReportInfo($"Cancel clicked on EnterpriseServerPage");
                            operationResult = new LoginPage().UpdateExtensionAdaptiveCard(_loginUI);
                            break;
                        }

                        if (!enterprisePageActionPayload.IsSubmitAction())
                        {
                            Log.Logger()?.ReportError($"Invalid action performed on LoginUI: {enterprisePageActionPayload.Id}");
                            operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Invalid action performed on LoginUI", "Invalid action");
                            break;
                        }

                        // Otherwise user clicked on Next button. We should validate the inputs and update the UI with PAT page.
                        var enterprisePageInputPayload = Json.ToObject<EnterpriseServerPage.InputPayload>(inputs) ?? throw new InvalidOperationException("Invalid inputs");
                        Log.Logger()?.ReportInfo($"EnterpriseServer: {enterprisePageInputPayload?.EnterpriseServer}");

                        if (enterprisePageInputPayload?.EnterpriseServer == null)
                        {
                            Log.Logger()?.ReportError($"EnterpriseServer is null");
                            operationResult = new EnterpriseServerPage(hostAddress: string.Empty, errorText: $"{Resources.GetResource("LoginUI_EnterprisePage_NullErrorText")}").UpdateExtensionAdaptiveCard(_loginUI);
                            break;
                        }

                        try
                        {
                            // Probe for Enterprise Server instance
                            _hostAddress = new Uri(enterprisePageInputPayload.EnterpriseServer);
                            if (!Validation.IsReachableGitHubEnterpriseServerURL(_hostAddress))
                            {
                                operationResult = new EnterpriseServerPage(hostAddress: _hostAddress, errorText: $"{Resources.GetResource("LoginUI_EnterprisePage_UnreachableErrorText")}").UpdateExtensionAdaptiveCard(_loginUI);
                                break;
                            }
                        }
                        catch (UriFormatException ufe)
                        {
                            Log.Logger()?.ReportError($"Error: {ufe}");
                            operationResult = new EnterpriseServerPage(hostAddress: enterprisePageInputPayload.EnterpriseServer, errorText: $"{Resources.GetResource("LoginUI_EnterprisePage_UriErrorText")}").UpdateExtensionAdaptiveCard(_loginUI);
                            break;
                        }
                        catch (Exception ex)
                        {
                            Log.Logger()?.ReportError($"Error: {ex}");
                            operationResult = new EnterpriseServerPage(hostAddress: enterprisePageInputPayload.EnterpriseServer, errorText: $"{Resources.GetResource("LoginUI_EnterprisePage_GenericErrorText")} : {ex}").UpdateExtensionAdaptiveCard(_loginUI);
                            break;
                        }

                        try
                        {
                            operationResult = new EnterpriseServerPATPage(hostAddress: _hostAddress, errorText: string.Empty, inputPAT: new NetworkCredential(null, string.Empty).SecurePassword).UpdateExtensionAdaptiveCard(_loginUI);
                        }
                        catch (Exception ex)
                        {
                            Log.Logger()?.ReportError($"Error: {ex}");
                            operationResult = new LoginFailedPage().UpdateExtensionAdaptiveCard(_loginUI);
                        }

                        break;
                    }

                case nameof(LoginUIState.EnterpriseServerPATPage):
                    {
                        if (_hostAddress == null)
                        {
                            // This should never happen.
                            Log.Logger()?.ReportError($"Host address is null");
                            operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Host address is null", "Host address is null");
                            break;
                        }

                        // Check if the user clicked on Cancel button.
                        var enterprisePATPageActionPayload = Json.ToObject<EnterpriseServerPATPage.ActionPayload>(action) ?? throw new InvalidOperationException("Invalid action");

                        if (enterprisePATPageActionPayload.IsCancelAction())
                        {
                            Log.Logger()?.ReportInfo($"Cancel clicked");
                            operationResult = new EnterpriseServerPage(hostAddress: _hostAddress, errorText: string.Empty).UpdateExtensionAdaptiveCard(_loginUI);
                            break;
                        }

                        if (enterprisePATPageActionPayload.IsUrlAction())
                        {
                            Log.Logger()?.ReportInfo($"Create PAT Link clicked");

                            try
                            {
                                Uri uri = new Uri(enterprisePATPageActionPayload?.URL ?? string.Empty);

                                var browserLaunch = false;

                                _ = Task.Run(async () =>
                                {
                                    // Launch GitHub login page on Browser.
                                    browserLaunch = await Windows.System.Launcher.LaunchUriAsync(uri);

                                    if (browserLaunch)
                                    {
                                        Log.Logger()?.ReportInfo($"Uri Launched to {uri.AbsoluteUri} - Check browser");
                                    }
                                    else
                                    {
                                        Log.Logger()?.ReportError($"Uri Launch failed to {uri.AbsoluteUri}");
                                    }
                                });
                            }
                            catch (UriFormatException ufe)
                            {
                                Log.Logger()?.ReportError($"Error: {ufe}");
                                operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, $"Error: {ufe}", $"Error: {ufe}");
                                break;
                            }
                            catch (Exception ex)
                            {
                                Log.Logger()?.ReportError($"Error: {ex}");
                                operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, $"Error: {ex}", $"Error: {ex}");
                                break;
                            }

                            operationResult = new ProviderOperationResult(ProviderOperationStatus.Success, null, string.Empty, string.Empty);
                            break;
                        }

                        if (!enterprisePATPageActionPayload.IsSubmitAction())
                        {
                            Log.Logger()?.ReportError($"Invalid action performed on LoginUI");
                            operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Invalid action performed on LoginUI", "Invalid action");
                            break;
                        }

                        // Otherwise user clicked on Next button. We should validate the inputs and update the UI with PAT page.
                        var enterprisePATPageInputPayload = Json.ToObject<EnterpriseServerPATPage.InputPayload>(inputs) ?? throw new InvalidOperationException("Invalid inputs");

                        if (string.IsNullOrEmpty(enterprisePATPageInputPayload.PAT))
                        {
                            Log.Logger()?.ReportError($"PAT is null");
                            operationResult = new EnterpriseServerPATPage(hostAddress: _hostAddress, errorText: $"{Resources.GetResource("LoginUI_EnterprisePATPage_NullErrorText")}", inputPAT: new NetworkCredential(null, enterprisePATPageInputPayload.PAT).SecurePassword).UpdateExtensionAdaptiveCard(_loginUI);
                            break;
                        }

                        Log.Logger()?.ReportInfo($"PAT Received");
                        var securePAT = new NetworkCredential(null, enterprisePATPageInputPayload.PAT).SecurePassword;
                        enterprisePATPageInputPayload.PAT = string.Empty;
                        enterprisePATPageInputPayload = null;

                        try
                        {
                            var devId = _developerIdProvider.LoginNewDeveloperIdWithPAT(_hostAddress, securePAT);

                            if (devId != null)
                            {
                                operationResult = new LoginSucceededPage(devId).UpdateExtensionAdaptiveCard(_loginUI);
                                break;
                            }
                            else
                            {
                                Log.Logger()?.ReportError($"PAT doesn't work for GHES endpoint {_hostAddress.OriginalString}");
                                operationResult = new EnterpriseServerPATPage(hostAddress: _hostAddress, errorText: $"{Resources.GetResource("LoginUI_EnterprisePATPage_BadCredentialsErrorText")} {_hostAddress.OriginalString}", inputPAT: new NetworkCredential(null, enterprisePATPageInputPayload?.PAT).SecurePassword).UpdateExtensionAdaptiveCard(_loginUI);
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("Bad credentials") || ex.Message.Contains("Not Found"))
                            {
                                Log.Logger()?.ReportError($"Unauthorized Error: {ex}");
                                operationResult = new EnterpriseServerPATPage(hostAddress: _hostAddress, errorText: $"{Resources.GetResource("LoginUI_EnterprisePATPage_BadCredentialsErrorText")} {_hostAddress.OriginalString}", inputPAT: new NetworkCredential(null, enterprisePATPageInputPayload?.PAT).SecurePassword).UpdateExtensionAdaptiveCard(_loginUI);
                                break;
                            }

                            Log.Logger()?.ReportError($"Error: {ex}");
                            operationResult = new EnterpriseServerPATPage(hostAddress: _hostAddress, errorText: $"{Resources.GetResource("LoginUI_EnterprisePATPage_GenericErrorPrefix")} {ex}", inputPAT: new NetworkCredential(null, enterprisePATPageInputPayload?.PAT).SecurePassword).UpdateExtensionAdaptiveCard(_loginUI);
                            break;
                        }
                    }

                // These pages only have close actions.
                case nameof(LoginUIState.LoginSucceededPage):
                case nameof(LoginUIState.LoginFailedPage):
                    {
                        Log.Logger()?.ReportInfo($"State:{_loginUI.State}");
                        operationResult = new EndPage().UpdateExtensionAdaptiveCard(_loginUI);
                        break;
                    }

                // These pages do not have any actions. We should never be here.
                case nameof(LoginUIState.WaitingPage):
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
}
