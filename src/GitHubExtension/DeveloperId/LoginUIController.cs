// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using GitHubExtension.Client;
using GitHubExtension.DeveloperId.LoginUI;
using GitHubExtension.Helpers;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;

namespace GitHubExtension.DeveloperId;

public class LoginUIController : IExtensionAdaptiveCardSession
{
    private static readonly Lazy<ILogger> _logger = new(() => Serilog.Log.ForContext("SourceContext", nameof(LoginUIController)));

    private static readonly ILogger _log = _logger.Value;

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
        _log.Debug($"Dispose");
        _loginUI?.Update(null, null, null);
    }

    public ProviderOperationResult Initialize(IExtensionAdaptiveCard extensionUI)
    {
        _log.Debug($"Initialize");
        _loginUI = extensionUI;
        return new LoginPage().UpdateExtensionAdaptiveCard(_loginUI);
    }

    public IAsyncOperation<ProviderOperationResult> OnAction(string action, string inputs)
    {
        return Task.Run(async () =>
        {
            if (_loginUI == null)
            {
                _log.Error($"OnAction() called with invalid state of LoginUI");
                return new ProviderOperationResult(ProviderOperationStatus.Failure, null, "OnAction() called with invalid state of LoginUI", "_loginUI is null");
            }

            ProviderOperationResult operationResult;
            _log.Information($"OnAction() called with state:{_loginUI.State}");
            _log.Debug($"action: {action}");

            switch (_loginUI.State)
            {
                case nameof(LoginUIState.LoginPage):
                    {
                        try
                        {
                            var loginPageActionPayload = Json.ToObject<LoginPage.ActionPayload>(action) ?? throw new InvalidOperationException("Invalid action");

                            if (!loginPageActionPayload.IsSubmitAction())
                            {
                                _log.Error($"Invalid action performed on LoginUI: {loginPageActionPayload.Id}");
                                operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Invalid action performed on LoginUI", "Invalid action performed on LoginUI");
                                break;
                            }

                            if (loginPageActionPayload.IsEnterprise())
                            {
                                _log.Information($"Show Enterprise Page");
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
                                _log.Error($"Unable to create DeveloperId");
                                new LoginFailedPage().UpdateExtensionAdaptiveCard(_loginUI);
                                operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Developer Id could not be created", "Developer Id could not be created");
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, $"Error: ");
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
                            _log.Information($"Cancel clicked on EnterpriseServerPage");
                            operationResult = new LoginPage().UpdateExtensionAdaptiveCard(_loginUI);
                            break;
                        }

                        if (!enterprisePageActionPayload.IsSubmitAction())
                        {
                            _log.Error($"Invalid action performed on LoginUI: {enterprisePageActionPayload.Id}");
                            operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Invalid action performed on LoginUI", "Invalid action");
                            break;
                        }

                        // Otherwise user clicked on Next button. We should validate the inputs and update the UI with PAT page.
                        var enterprisePageInputPayload = Json.ToObject<EnterpriseServerPage.InputPayload>(inputs) ?? throw new InvalidOperationException("Invalid inputs");
                        _log.Information($"EnterpriseServer: {enterprisePageInputPayload?.EnterpriseServer}");

                        if (enterprisePageInputPayload?.EnterpriseServer == null)
                        {
                            _log.Error($"EnterpriseServer is null");
                            operationResult = new EnterpriseServerPage(hostAddress: string.Empty, errorText: $"{Resources.GetResource("LoginUI_EnterprisePage_NullErrorText")}").UpdateExtensionAdaptiveCard(_loginUI);
                            break;
                        }

                        try
                        {
                            // Probe for Enterprise Server instance
                            _hostAddress = new Uri(enterprisePageInputPayload.EnterpriseServer);
                            if (!await Validation.IsReachableGitHubEnterpriseServerURL(_hostAddress))
                            {
                                operationResult = new EnterpriseServerPage(hostAddress: _hostAddress, errorText: $"{Resources.GetResource("LoginUI_EnterprisePage_UnreachableErrorText")}").UpdateExtensionAdaptiveCard(_loginUI);
                                break;
                            }
                        }
                        catch (UriFormatException ufe)
                        {
                            _log.Error(ufe, $"Error: ");
                            operationResult = new EnterpriseServerPage(hostAddress: enterprisePageInputPayload.EnterpriseServer, errorText: $"{Resources.GetResource("LoginUI_EnterprisePage_UriErrorText")}").UpdateExtensionAdaptiveCard(_loginUI);
                            break;
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, $"Error: ");
                            operationResult = new EnterpriseServerPage(hostAddress: enterprisePageInputPayload.EnterpriseServer, errorText: $"{Resources.GetResource("LoginUI_EnterprisePage_GenericErrorText")} : {ex}").UpdateExtensionAdaptiveCard(_loginUI);
                            break;
                        }

                        try
                        {
                            operationResult = new EnterpriseServerPATPage(hostAddress: _hostAddress, errorText: string.Empty, inputPAT: new NetworkCredential(null, string.Empty).SecurePassword).UpdateExtensionAdaptiveCard(_loginUI);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, $"Error: ");
                            operationResult = new LoginFailedPage().UpdateExtensionAdaptiveCard(_loginUI);
                        }

                        break;
                    }

                case nameof(LoginUIState.EnterpriseServerPATPage):
                    {
                        if (_hostAddress == null)
                        {
                            // This should never happen.
                            _log.Error($"Host address is null");
                            operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Host address is null", "Host address is null");
                            break;
                        }

                        // Check if the user clicked on Cancel button.
                        var enterprisePATPageActionPayload = Json.ToObject<EnterpriseServerPATPage.ActionPayload>(action) ?? throw new InvalidOperationException("Invalid action");

                        if (enterprisePATPageActionPayload.IsCancelAction())
                        {
                            _log.Information($"Cancel clicked");
                            operationResult = new EnterpriseServerPage(hostAddress: _hostAddress, errorText: string.Empty).UpdateExtensionAdaptiveCard(_loginUI);
                            break;
                        }

                        if (enterprisePATPageActionPayload.IsUrlAction())
                        {
                            _log.Information($"Create PAT Link clicked");

                            try
                            {
                                var uri = new Uri(enterprisePATPageActionPayload?.URL ?? string.Empty);

                                var browserLaunch = false;

                                _ = Task.Run(async () =>
                                {
                                    // Launch GitHub login page on Browser.
                                    browserLaunch = await Windows.System.Launcher.LaunchUriAsync(uri);

                                    if (browserLaunch)
                                    {
                                        _log.Information($"Uri Launched to {uri.AbsoluteUri} - Check browser");
                                    }
                                    else
                                    {
                                        _log.Error($"Uri Launch failed to {uri.AbsoluteUri}");
                                    }
                                });
                            }
                            catch (UriFormatException ufe)
                            {
                                _log.Error(ufe, $"Error: ");
                                operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, $"Error: {ufe}", $"Error: {ufe}");
                                break;
                            }
                            catch (Exception ex)
                            {
                                _log.Error(ex, $"Error: ");
                                operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, $"Error: {ex}", $"Error: {ex}");
                                break;
                            }

                            operationResult = new ProviderOperationResult(ProviderOperationStatus.Success, null, string.Empty, string.Empty);
                            break;
                        }

                        if (!enterprisePATPageActionPayload.IsSubmitAction())
                        {
                            _log.Error($"Invalid action performed on LoginUI");
                            operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, "Invalid action performed on LoginUI", "Invalid action");
                            break;
                        }

                        // Otherwise user clicked on Next button. We should validate the inputs and update the UI with PAT page.
                        var enterprisePATPageInputPayload = Json.ToObject<EnterpriseServerPATPage.InputPayload>(inputs) ?? throw new InvalidOperationException("Invalid inputs");

                        if (string.IsNullOrEmpty(enterprisePATPageInputPayload.PAT))
                        {
                            _log.Error($"PAT is null");
                            operationResult = new EnterpriseServerPATPage(hostAddress: _hostAddress, errorText: $"{Resources.GetResource("LoginUI_EnterprisePATPage_NullErrorText")}", inputPAT: new NetworkCredential(null, enterprisePATPageInputPayload.PAT).SecurePassword).UpdateExtensionAdaptiveCard(_loginUI);
                            break;
                        }

                        _log.Information($"PAT Received");
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
                                _log.Error($"PAT doesn't work for GHES endpoint {_hostAddress.OriginalString}");
                                operationResult = new EnterpriseServerPATPage(hostAddress: _hostAddress, errorText: $"{Resources.GetResource("LoginUI_EnterprisePATPage_BadCredentialsErrorText")} {_hostAddress.OriginalString}", inputPAT: new NetworkCredential(null, enterprisePATPageInputPayload?.PAT).SecurePassword).UpdateExtensionAdaptiveCard(_loginUI);
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("Bad credentials") || ex.Message.Contains("Not Found"))
                            {
                                _log.Error(ex, $"Unauthorized Error: ");
                                operationResult = new EnterpriseServerPATPage(hostAddress: _hostAddress, errorText: $"{Resources.GetResource("LoginUI_EnterprisePATPage_BadCredentialsErrorText")} {_hostAddress.OriginalString}", inputPAT: new NetworkCredential(null, enterprisePATPageInputPayload?.PAT).SecurePassword).UpdateExtensionAdaptiveCard(_loginUI);
                                break;
                            }

                            _log.Error(ex, $"Error: ");
                            operationResult = new EnterpriseServerPATPage(hostAddress: _hostAddress, errorText: $"{Resources.GetResource("LoginUI_EnterprisePATPage_GenericErrorPrefix")} {ex}", inputPAT: new NetworkCredential(null, enterprisePATPageInputPayload?.PAT).SecurePassword).UpdateExtensionAdaptiveCard(_loginUI);
                            break;
                        }
                    }

                // These pages only have close actions.
                case nameof(LoginUIState.LoginSucceededPage):
                case nameof(LoginUIState.LoginFailedPage):
                    {
                        _log.Information($"State:{_loginUI.State}");
                        operationResult = new EndPage().UpdateExtensionAdaptiveCard(_loginUI);
                        break;
                    }

                // These pages do not have any actions. We should never be here.
                case nameof(LoginUIState.WaitingPage):
                default:
                    {
                        _log.Error($"Unexpected state:{_loginUI.State}");
                        operationResult = new ProviderOperationResult(ProviderOperationStatus.Failure, null, $"Error occurred in :{_loginUI.State}", $"Error occurred in :{_loginUI.State}");
                        break;
                    }
            }

            return operationResult;
        }).AsAsyncOperation();
    }
}
