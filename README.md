![image](https://github.com/microsoft/devhomegithubextension/blob/main/src/GithubPluginServer/Assets/StoreDisplay-150.png)

# Welcome to the Dev Home GitHub Extension repo


This repository contains the source code for:

* [Dev Home GitHub Extension](https://aka.ms/devhomegithubextension)
* Dev Home GitHub widgets

Related repositories include:

* [Dev Home](https://github.com/microsoft/devhome)

## Installing and running Dev Home GitHub Extension

> **Note**: The Dev Home GitHub Extension requires Dev Home. Dev Home requires Windows 11 21H2 (build 22000) or later.

### Microsoft Store [Recommended]

Install [Dev Home from the Microsoft Store](https://aka.ms/devhome) and the Dev Home GitHub Extension will automatically be installed on first launch of Dev Home.
This allows you to always be on the latest version when we release new builds with automatic upgrades.

This is our preferred method.

You can also install the Dev Home GitHub Extension from its own [Microsoft Store listing](https://aka.ms/devhomegithubextension).

### Other install methods

#### Via GitHub

For users who are unable to install the Dev Home GitHub Extension from the Microsoft Store, released builds can be manually downloaded from this repository's [Releases page](https://github.com/microsoft/devhomegithubextension/releases).

---

## Dev Home GitHub Extension overview

Please take a few minutes to review the overview below before diving into the code:

### Widgets

The Dev Home GitHub Extension provides widgets for Dev Home's dashboard, which is built as a Windows widget renderer. These widgets are built using the Windows widget platform, which relies on Adaptive Cards.

### Machine configuration repository recommendations

The machine configuration tool utilizes the Dev Home GitHub Extension to recommend repositories to clone, but isn't required to clone and install apps. The app installation tool is powered by winget.

---

## Documentation

Documentation for the Dev Home GitHub Extension can be found at https://aka.ms/devhomedocs.

---

## Contributing

We are excited to work alongside you, our amazing community, to build and enhance the Dev Home GitHub Extension!

***BEFORE you start work on a feature/fix***, please read & follow our [Contributor's Guide](https://github.com/microsoft/devhomegithubextension/blob/main/CONTRIBUTING.md) to help avoid any wasted or duplicate effort.

## Communicating with the team

The easiest way to communicate with the team is via GitHub issues.

Please file new issues, feature requests and suggestions, but **DO search for similar open/closed preexisting issues before creating a new issue.**

If you would like to ask a question that you feel doesn't warrant an issue (yet), please reach out to us via Twitter:

* Kayla Cinnamon, Product Manager: [@cinnamon_msft](https://twitter.com/cinnamon_msft)
* Clint Rutkas, Senior Product Manager: [@crutkas](https://twitter.com/crutkas)
* Ujjwal Chadha, Developer: [@ujjwalscript](https://twitter.com/ujjwalscript)

## Developer guidance

* You must be running Windows 11 21H2 (build >= 10.0.22000.0) to run Dev Home
* You must [enable Developer Mode in the Windows Settings app](https://docs.microsoft.com/en-us/windows/uwp/get-started/enable-your-device-for-development)

## Building the code

* Clone the repository
* Uninstall the Preview version of the Dev Home GitHub Extension (Dev Home has a hard time choosing which extension to use if two versions exist)
* Open the GITServices.sln in Visual Studio 2022 or later, and build from the IDE, or run build.ps1 from a Visual Studio command prompt.

### OAuth App
Since secrets cannot be checked in to the repository, developers must create their own test OAuth app for local tests.

Follow this link https://docs.github.com/en/developers/apps/building-oauth-apps/creating-an-oauth-app to create a Git OAuth app (with RedirectUri = "devhome://oauth_redirect_uri/").

The OAuth App ClientId and ClientSecret can be added as environment variables using the following instructions:

How to set the environment variables:

    On an elevated cmd window:
        setx GITHUB_CLIENT_ID "Your OAuth App's ClientId" /m
        setx GITHUB_CLIENT_SECRET "Your OAuth App's ClientSecret" /m

---

## Code of conduct

We welcome contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.
