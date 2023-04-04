# Introduction 
This project contains tools for simplifiying use of common GIT-based services like ADO and GitHub by providing Windows shell extensions such as widgets and notifications. 

# Getting Started
TODO: Guide users through getting your code up and running on their own system. In this section you can talk about:
1.	Installation process
2.	Software dependencies
3.	Latest releases
4.	API references

# Build and Test
Open the GITServices.sln in Visual Studio 2022 or later, and build from the IDE, or run build.ps1 from a Visual Studio command prompt.

### OAuth App
Since secrets cannot be checked in to the repository, developers must create their own test OAuth app for local tests.

Follow this link https://docs.github.com/en/developers/apps/building-oauth-apps/creating-an-oauth-app
to creare a Git Oauth app (with RedirectUri = "devhome://oauth_redirect_uri/").

The OAuth App ClientId and ClientSecret can be added as environment variables using the following instructions:

How to set the environment variables:

    On an elevated cmd window:
        setx GITHUB_CLIENT_ID "Your OAuth App's ClientId" /m
        setx GITHUB_CLIENT_SECRET "Your OAuth App's ClientSecret" /m

# Contribute
TODO: Explain how other users and developers can contribute to make your code better. 

If you want to learn more about creating good readme files then refer the following [guidelines](https://docs.microsoft.com/en-us/azure/devops/repos/git/create-a-readme?view=azure-devops). You can also seek inspiration from the below readme files:
- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)
- [Chakra Core](https://github.com/Microsoft/ChakraCore)